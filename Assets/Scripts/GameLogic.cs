using Fun;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// protobuf
using funapi.network.fun_message;
using pong_messages;

public class GameLogic : Singleton<GameLogic>
{
    public Menu menu;
    public Text textLabel;

    public GameObject gameRoot;
    public MyBar myBar;
    public OppBar oppBar;
    public Ball ball;
    public float ballSpeed = 1.5f;

    // 메뉴 화면
    public void ShowMenu()
    {
        state = GAME_STATE.MENU;
        menu.SetActive(true);

        if (loginType == LOGIN_TYPE.SINGLE)
        {
            menu.OnSinglePlayMainMenu();
        }
        else
        {
            menu.OnMultiplayMainMenu();
        }

        gameRoot.SetActive(false);
        setStatusText("");
    }

    public void WaitMenu()
    {
        menu.WaitMenu();
    }

    public void SinglePlayLogin()
    {
        loginType = LOGIN_TYPE.SINGLE;
        ShowMenu();
    }

    public void GuestLogin()
    {
        loginType = LOGIN_TYPE.MULTI_GUEST;
        NetworkManager.Instance.Connect();
    }

    public void FBLogin()
    {
        loginType = LOGIN_TYPE.MULTI_FACEBOOK;
        NetworkManager.Instance.Connect();
    }

    public void SetMatchRecord(int winCount, int loseCount, int curRecord)
    {
        winCount_ = winCount;
        loseCount_ = loseCount;
        curRecord_ = curRecord;
        menu.SetMatchRecord(winCount, loseCount, curRecord);
    }

    public void RequestRankList()
    {
        NetworkManager.Instance.Send("ranklist");
    }

    public void StartSingleGamePlay()
    {
        setReadyToPlay();
        StartPlay();
    }

    // 게임 시작
    public void StartPlay()
    {
        state = GAME_STATE.GAME;
        lastBarTimeSeq = 0;

        setStatusText("");

        if (loginType == LOGIN_TYPE.SINGLE || bRoomMaster)
        {
            // 공의 첫 움직임
            float vx = (ballSpeed + UnityEngine.Random.value) * (UnityEngine.Random.value < 0.5f ? -1 : 1);
            float vy = (ballSpeed + UnityEngine.Random.value) * (UnityEngine.Random.value < 0.5f ? -1 : 1);
            ball.SetProperties(0, 0, vx, vy);

            if (loginType != LOGIN_TYPE.SINGLE)
                ball.SendProperties();
        }
    }

    public void RequestMatching()
    {
        state = GAME_STATE.MATCHING;
        setStatusText("매칭 중입니다");

        // 매치 요청
        NetworkManager.Instance.Send("match");
    }

    // 매칭 결과 처리
    public void OnMatch(bool bMaster)
    {
        bRoomMaster = bMaster;
    }

    public void OnReady()
    {
        if (state != GAME_STATE.MATCHING)
            return;

        setReadyToPlay();

        NetworkManager.Instance.Send("ready");
    }

    public void RequestCancelMatching()
    {
        state = GAME_STATE.MENU;
        NetworkManager.Instance.Send("cancelmatch");
    }

    private void FixedUpdate()
    {
        switch (state)
        {
            case GAME_STATE.INIT:
                if (NetworkManager.Instance.IsReady)
                {
                    // 세션 생성, 로그인 완료, 메뉴로
                    ShowMenu();
                }
                break;

            case GAME_STATE.GAME:
                if (loginType != LOGIN_TYPE.SINGLE)
                {
                    // 승패 처리, 패배시에만 보고함
                    // 패배 판정은 나의 'bar'보다 공이 아래쪽으로 많이 지나간 경우
                    if (ball.transform.localPosition.y < myBar.transform.localPosition.y - kOutOfBounds)
                    {
                        if (NetworkManager.Instance.GetEncoding() == Fun.FunEncoding.kJson)
                        {
                            Dictionary<string, object> message = new Dictionary<string, object>();
                            message["result"] = "lose";
                            NetworkManager.Instance.Send("result", message);
                        }
                        else
                        {
                            GameResultMessage msg = new GameResultMessage();
                            msg.result = "lose";

                            FunMessage fun_msg = FunapiMessage.CreateFunMessage(msg, MessageType.game_result);
                            NetworkManager.Instance.Send("result", fun_msg);
                        }

                        state = GAME_STATE.WAIT;
                    }
                }
                else
                {
                    if (ball.transform.localPosition.y > oppBar.transform.localPosition.y + kOutOfBounds ||
                        ball.transform.localPosition.y < myBar.transform.localPosition.y - kOutOfBounds)
                    {
                        state = GAME_STATE.RESULT;
                        gameRoot.SetActive(false);

                        ModalWindow.Instance.Open("PONG", "게임종료!", ShowMenu);
                    }
                }
                break;
        }
    }

    private void OnApplicationPause(bool isPaused)
    {
        if (loginType == LOGIN_TYPE.SINGLE && isPaused)
        {
            AppUtil.Quit();
            return;
        }
    }

    private void OnApplicationQuit()
    {
        NetworkManager.Instance.Stop();
    }

    // 게임 중 정보 업데이트
    public void RelayMessageReceived(FunEncoding encoding, object body)
    {
        bool update_ball = false;
        bool update_bar = false;
        float x = 0.0f, y = 0.0f;
        float vx = 0.0f, vy = 0.0f;
        float barTimeSeq = 0.0f;
        float barX = 0.0f;

        if (encoding == FunEncoding.kJson)
        {
            Dictionary<string, object> message = body as Dictionary<string, object>;

            // 'ball'의 정보가 업데이트됨
            if (message.ContainsKey("ballX") && message.ContainsKey("ballY") && message.ContainsKey("ballVX") && message.ContainsKey("ballVY"))
            {
                x = Convert.ToSingle(message["ballX"]);
                y = Convert.ToSingle(message["ballY"]);
                vx = Convert.ToSingle(message["ballVX"]);
                vy = Convert.ToSingle(message["ballVY"]);
                update_ball = true;
            }

            // 상대 'bar'의 위치가 변경됨
            if (message.ContainsKey("barX") && message.ContainsKey("timeSeq"))
            {
                barTimeSeq = Convert.ToSingle(message["timeSeq"]);
                barX = Convert.ToSingle(message["barX"]);
                update_bar = true;
            }
        }
        else
        {
            FunMessage fun_msg = body as FunMessage;
            GameRelayMessage message = FunapiMessage.GetMessage<GameRelayMessage>(fun_msg, MessageType.game_relay);
            if (message == null)
            {
                ModalWindow.Instance.Open("Error!", "Invalid protobuf message", GameLogic.Instance.ShowMenu);
                return;
            }

            // 'ball'의 정보가 업데이트됨
            if (message.ballXSpecified && message.ballYSpecified && message.ballVXSpecified && message.ballVYSpecified)
            {
                x = message.ballX;
                y = message.ballY;
                vx = message.ballVX;
                vy = message.ballVY;
                update_ball = true;
            }

            // 상대 'bar'의 위치가 변경됨
            if (message.barXSpecified && message.timeSeqSpecified)
            {
                barX = message.barX;
                barTimeSeq = message.timeSeq;
                update_bar = true;
            }
        }

        if (update_ball)
        {
            // 서로 화면을 뒤집어 보기 때문에, 전부 -로 변환필요
            ball.SetProperties(-x, -y, -vx, -vy);
        }

        if (update_bar)
        {
            if (barTimeSeq > lastBarTimeSeq)
            {
                lastBarTimeSeq = barTimeSeq;
                oppBar.SetPosX(-barX);
            }
        }
    }

    // 서버의 게임 결과 응답 처리
    public void ResultMessageReceived(FunEncoding encoding, object body)
    {
        if (state != GAME_STATE.WAIT && state != GAME_STATE.GAME)
            return;

        // 게임 결과 화면
        state = GAME_STATE.RESULT;
        gameRoot.SetActive(false);

        bool win = false;

        if (encoding == FunEncoding.kJson)
        {
            Dictionary<string, object> message = body as Dictionary<string, object>;

            if (message["result"].Equals("win"))
                win = true;
            else
                win = false;
        }
        else if (encoding == FunEncoding.kProtobuf)
        {
            FunMessage fun_msg = body as FunMessage;
            GameResultMessage message = FunapiMessage.GetMessage<GameResultMessage>(fun_msg, MessageType.game_result);
            if (message == null)
            {
                ModalWindow.Instance.Open("Error!", "Invalid protobuf message", GameLogic.Instance.ShowMenu);
                return;
            }
            if (message.result == "win")
                win = true;
            else
                win = false;
        }

        if (win)
        {
            SetMatchRecord(winCount_ + 1, loseCount_, curRecord_ + 1);
            ModalWindow.Instance.Open("결과", "승리했습니다!", ShowMenu);
        }
        else
        {
            SetMatchRecord(winCount_, loseCount_ + 1, 0);
            ModalWindow.Instance.Open("결과", "패배했습니다!", ShowMenu);
        }

    }

    public void RecordlistMessageReceived(FunEncoding encoding, object body)
    {
        menu.SetRecordBoard(encoding, body);
    }

    private void setReadyToPlay()
    {
        state = GAME_STATE.READY;

        menu.SetActive(false);
        gameRoot.SetActive(true);

        // 위치 초기화

        var isMultiplay_ = isMultiplay();

        myBar.Ready(isMultiplay_);
        oppBar.Ready(isMultiplay_);
        ball.Reset(isMultiplay_);

        // ready
        setStatusText("준비!");
    }

    private void setStatusText(string text)
    {
        textLabel.text = text;
    }

    private bool isMultiplay()
    {
        if (loginType == LOGIN_TYPE.SINGLE)
        {
            return false;
        }
        return true;
    }

    private enum GAME_STATE
    {
        INIT,       // init. game
        MENU,       // wait user input (click match button)
        MATCHING,   // matching..
        READY,      // ready for game
        GAME,       // playing pong
        END,        // end play
        RESULT,     // result
        WAIT        // wait response for only one request transmission
    }

    private GAME_STATE state = GAME_STATE.INIT;

    public enum LOGIN_TYPE
    {
        SINGLE,
        MULTI_GUEST,
        MULTI_FACEBOOK
    }

    public LOGIN_TYPE loginType { get; private set; }
    private bool bRoomMaster = false;
    private const float kOutOfBounds = 60f;
    private float lastBarTimeSeq = 0;
    private int winCount_;
    private int loseCount_;
    private int curRecord_;
}

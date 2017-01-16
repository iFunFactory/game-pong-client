using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

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

        if(loginType == LOGIN_TYPE.SINGLE)
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

    public void SetMatchRecord(int winCount, int loseCount)
    {
        winCount_ = winCount;
        loseCount_ = loseCount;
        menu.SetMatchRecord(winCount, loseCount);
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
        setReadyToPlay();

        // 준비 완료 메세지 송신
        Invoke("sendReady", 1);
    }

    // 준비 완료 메세지 송신
    private void sendReady()
    {
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
                        Dictionary<string, object> message = new Dictionary<string, object>();
                        message["result"] = "lose";
                        NetworkManager.Instance.Send("result", message);
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
    public void RelayMessageReceived(Dictionary<string, object> message)
    {
        // 'ball'의 정보가 업데이트됨
        if (message.ContainsKey("ballX") && message.ContainsKey("ballY") && message.ContainsKey("ballVX") && message.ContainsKey("ballVY"))
        {
            float x = Convert.ToSingle(message["ballX"]);
            float y = Convert.ToSingle(message["ballY"]);
            float vx = Convert.ToSingle(message["ballVX"]);
            float vy = Convert.ToSingle(message["ballVY"]);
            // 서로 화면을 뒤집어 보기 때문에, 전부 -로 변환필요
            ball.SetProperties(-x, -y, -vx, -vy);
        }

        // 상대 'bar'의 위치가 변경됨
        if (message.ContainsKey("barX") && message.ContainsKey("timeSeq"))
        {
            float barTimeSeq = Convert.ToSingle(message["timeSeq"]);
            if (barTimeSeq > lastBarTimeSeq)
            {
                lastBarTimeSeq = barTimeSeq;
                float barX = Convert.ToSingle(message["barX"]);
                oppBar.SetPosX(-barX);
            }
        }
    }

    // 서버의 게임 결과 응답 처리
    public void ResultMessageReceived(Dictionary<string, object> message)
    {
        if (state != GAME_STATE.WAIT && state != GAME_STATE.GAME)
            return;

        // 게임 결과 화면
        state = GAME_STATE.RESULT;

        gameRoot.SetActive(false);

        if (message["result"].Equals("win"))
        {
            SetMatchRecord(winCount_ + 1, loseCount_);
            ModalWindow.Instance.Open("결과", "승리했습니다!", ShowMenu);
        }
        else
        {
            SetMatchRecord(winCount_ , loseCount_ + 1);
            ModalWindow.Instance.Open("결과", "패배했습니다!", ShowMenu);
        }
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

    private GAME_STATE state;

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
}
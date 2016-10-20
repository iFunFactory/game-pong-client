using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;


public class GameLogic : Singleton<GameLogic>
{
    public Menu menu;
    public Text textLabel;

    public GameObject gameRoot;
    public MyBar myBar;
    public OppBar oppBar;
    public Ball ball;
    public float ballSpeed = 1.5f;

    public bool isMultiPlay = false;


    // 메뉴 화면
    public void ShowMenu ()
    {
        state = GAME_STATE.MENU;

        menu.SetActive(true);

        if (isMultiPlay)
            menu.EnableMatchingButton();

        gameRoot.SetActive(false);
        setStatusText("");
    }

    public void StartGame ()
    {
        if (isMultiPlay)
        {
            state = GAME_STATE.INIT;
            NetworkManager.Instance.Connect();
        }
        else
        {
            setReadyToPlay();
            StartPlay();
        }
    }

    // 게임 시작
    public void StartPlay ()
    {
        state = GAME_STATE.GAME;
        lastBarTimeSeq = 0;

        setStatusText("");

        if (!isMultiPlay || bRoomMaster)
        {
            // 공의 첫 움직임
            float vx = (ballSpeed + UnityEngine.Random.value) * (UnityEngine.Random.value < 0.5f ? -1 : 1);
            float vy = (ballSpeed + UnityEngine.Random.value) * (UnityEngine.Random.value < 0.5f ? -1 : 1);
            ball.SetProperties(0, 0, vx, vy);

            if (isMultiPlay)
                ball.SendProperties();
        }
    }

    public void RequestMatching ()
    {
        state = GAME_STATE.MATCHING;
        setStatusText("매칭 중입니다");

        // 매치 요청
        NetworkManager.Instance.Send("match");
    }


    void FixedUpdate ()
    {
        switch (state)
        {
        case GAME_STATE.INIT:
            if (NetworkManager.Instance.IsReady)
            {
                // 세션 생성, 로그인 완료, 메뉴로
                ShowMenu();
                menu.OnConnected();
            }
            break;

        case GAME_STATE.GAME:
            if (isMultiPlay)
            {
                // 승패 처리, 패배시에만 보고함
                // 패배 판정은 나의 'bar'보다 공이 아래쪽으로 많이 지나간 경우
                if (ball.transform.localPosition.y < myBar.transform.localPosition.y - kOutOfBounds)
                {
                    Dictionary<string, object> message = new Dictionary<string, object>();
                    message["result"] = "lose";
                    NetworkManager.Instance.Send("result", message);
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

    void OnApplicationPause (bool isPaused)
    {
        if (!isMultiPlay && isPaused)
        {
            Application.Quit();
            return;
        }
    }

    void OnApplicationQuit ()
    {
        NetworkManager.Instance.Stop();
    }


    // 매칭 결과 처리
    public void OnMatch (bool bMaster)
    {
        bRoomMaster = bMaster;

        setReadyToPlay();

        // 준비 완료 메세지 송신
        Invoke("sendReady", 1);
    }

    // 게임 중 정보 업데이트
    public void RelayMessageReceived (Dictionary<string, object> message)
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
    public void ResultMessageReceived (Dictionary<string, object> message)
    {
        if (state != GAME_STATE.GAME)
            return;

        // 게임 결과 화면
        state = GAME_STATE.RESULT;
        gameRoot.SetActive(false);

        if (message["result"].Equals("win"))
            ModalWindow.Instance.Open("결과", "승리했습니다!", ShowMenu);
        else
            ModalWindow.Instance.Open("결과", "패배했습니다!", ShowMenu);
    }

    void setReadyToPlay ()
    {
        state = GAME_STATE.READY;

        menu.SetActive(false);
        gameRoot.SetActive(true);

        // 위치 초기화
        myBar.Ready(isMultiPlay);
        oppBar.Ready(isMultiPlay);
        ball.Reset(isMultiPlay);

        // ready
        setStatusText("준비!");
    }

    void setStatusText (string text)
    {
        textLabel.text = text;
    }

    // 준비 완료 메세지 송신
    void sendReady ()
    {
        NetworkManager.Instance.Send("ready");
    }


    const float kOutOfBounds = 60f;

    enum GAME_STATE
    {
        INIT,       // init. game
        MENU,       // wait user input (click match button)
        MATCHING,   // matching..
        READY,      // ready for game
        GAME,       // playing pong
        END,        // end play
        RESULT,     // result
    }

    GAME_STATE state;
    bool bRoomMaster = false;
    float lastBarTimeSeq = 0;
}

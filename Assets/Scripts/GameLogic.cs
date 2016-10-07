using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;

public class GameLogic : Singleton<GameLogic>
{
    public Menu menu;
    public Text textLabel;

    public GameObject gameRoot;
    public GameObject myBar;
    public GameObject oppBar;
    public Ball ball;

    public bool isNetworkEnabled = true;

    public enum GAME_STATE {
        INIT,       // init. game
        MENU,       // wait user input (click match button)
        MATCHING,   // matching..
        READY,      // ready for game
        GAME,       // playing pong
        RESULT,     // result
    }
    GAME_STATE state;

    bool upsideDown = false;
    //string opponentId = "";

    float lastBarTimeSeq = 0;


    void Update()
    {
        switch (state)
        {
        case GAME_STATE.INIT:
            if (NetworkManager.Instance.IsReady)
            {
                // 세션 생성, 로그인 완료, 메뉴로
                Menu();
                menu.OnConnected();
            }
            break;

        case GAME_STATE.GAME:
            // 승패 처리, 패배시에만 보고함
            // 패배 판정은 나의 'bar'보다 공이 아래쪽으로 많이 지나간 경우
            if (ball.gameObject.transform.localPosition.y < myBar.transform.localPosition.y - 100)
            {
                // 패배
                Dictionary<string, object> message = new Dictionary<string, object>();
                message["result"] = "lose";
                NetworkManager.Instance.Send("result", message);
            }
            break;
        }
    }

    // application delegates
    #region APP_DELEGATES
    void OnApplicationPause (bool isPaused)
    {
        if (!isNetworkEnabled && isPaused)
        {
            Application.Quit();
            return;
        }

        if (isPaused)
        {
            menu.OnDisonnected();
            NetworkManager.Instance.Stop();
        }
    }

    void OnApplicationQuit()
    {
        NetworkManager.Instance.Stop();
    }
    #endregion


    public void Connect()
    {
        if (isNetworkEnabled)
        {
            state = GAME_STATE.INIT;
            NetworkManager.Instance.Init();
        }
        else
        {
            gameRoot.SetActive(true);
            StartGame();
        }
    }

    public void RequestMatching()
    {
        state = GAME_STATE.MATCHING;
        setStatusMessage("매칭 중입니다");

        // 매치 요청
        NetworkManager.Instance.Send("match");
    }

    // 매칭 결과 처리
    public void OnMatch(Dictionary<string, object> message)
    {
        if (message["result"].Equals("Success"))
        {
            // 매칭 성공
            state = GAME_STATE.READY;

            menu.SetActive(false);
            setStatusMessage("");

            // 위치 초기화
            myBar.gameObject.transform.localPosition = new Vector3(0, myBar.gameObject.transform.localPosition.y);
            oppBar.gameObject.transform.localPosition = new Vector3(0, oppBar.gameObject.transform.localPosition.y);
            ball.Reset();
            gameRoot.SetActive(true);

            if (message["A"].Equals(NetworkManager.Instance.MyId))
            {
                upsideDown = false;
                //opponentId = message["B"] as string;
            }
            else
            {
                upsideDown = true;
                //opponentId = message["A"] as string;
            }

            // ready
            setStatusMessage("준비!");

            // 준비 완료 메세지 송신
            Invoke("SendReady", 1);
        }
        else
        {
            // 매칭 실패
            // TODO: 상황에따른 예외처리
            ModalWindow.Instance.Open("매칭 실패", "매칭에 실패했습니다.", Menu);
        }
    }

    // 준비 완료 메세지 송신
    void SendReady()
    {
        NetworkManager.Instance.Send("ready");
    }

    // 메뉴 화면
    void Menu ()
    {
        state = GAME_STATE.MENU;

        menu.SetActive(true);
        menu.EnableMatchingButton();

        gameRoot.SetActive(false);
        setStatusMessage("");
    }

    // 게임 시작
    public void StartGame()
    {
        state = GAME_STATE.GAME;
        lastBarTimeSeq = 0;

        setStatusMessage("");

        // 공의 첫 움직임
        // TODO: 랜덤하게 만들자
        if (upsideDown == true)
            ball.SetBallProperties(0, 0, 1.5f, -1.5f);
        else
            ball.SetBallProperties(0, 0, -1.5f, 1.5f);
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
            ball.SetBallProperties(-x, -y, -vx, -vy);
        }

        // 상대 'bar'의 위치가 변경됨
        if (message.ContainsKey("barX") && message.ContainsKey("timeSeq"))
        {
            float barTimeSeq = Convert.ToSingle(message["timeSeq"]);
            if (barTimeSeq > lastBarTimeSeq)
            {
                lastBarTimeSeq = barTimeSeq;
                float barX = Convert.ToSingle(message["barX"]);
                oppBar.transform.localPosition = new Vector3(-barX, oppBar.transform.localPosition.y, oppBar.transform.localPosition.z);
            }
        }
    }

    // 서버의 게임 결과 응답 처리
    public void ResultMessageReceived(Dictionary<string, object> message)
    {
        if (state != GAME_STATE.GAME)
            return;

        // 게임 결과 화면
        state = GAME_STATE.RESULT;
        gameRoot.SetActive(false);

        if (message["result"].Equals("win"))
            ModalWindow.Instance.Open("결과", "승리했습니다!", Menu);
        else
            ModalWindow.Instance.Open("결과", "패배했습니다!", Menu);
    }

    void setStatusMessage (string message)
    {
        textLabel.text = message;
    }
}

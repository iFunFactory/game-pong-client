using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;

public class GameLogic : Singleton<GameLogic>
{
    public Button matchButton;
    public Text textLabel;
    public GameObject gameRoot;

    public bool networkEnabled = true;

    public GameObject myBar;
    public GameObject oppBar;
    public Ball ball;

    public enum GAME_STATE {
        INIT,       // init. game
        MENU,       // wait user input (click match button)
        MATCHING,   // matching..
        READY,      // ready for game
        GAME,       // playing pong
        WIN,
        LOSE,
    }
    GAME_STATE state;

    bool upsideDown = false;
    string opponentId = "";

    float lastBarTimeSeq = 0;

    void Start()
    {
        if(networkEnabled)
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

    // 매치 요청 버튼 클릭
    void OnMatchButtonClick()
    {
        // 화면 구성
        matchButton.gameObject.SetActive(false);
        textLabel.text = "매칭 중입니다";
        textLabel.gameObject.SetActive(true);
        state = GAME_STATE.MATCHING;
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
            textLabel.gameObject.SetActive(false);
            // 위치 초기화
            myBar.gameObject.transform.localPosition = new Vector3(0, myBar.gameObject.transform.localPosition.y);
            oppBar.gameObject.transform.localPosition = new Vector3(0, oppBar.gameObject.transform.localPosition.y);
            ball.Reset();
            gameRoot.SetActive(true);

            if (message["A"].Equals(NetworkManager.Instance.MyId))
            {
                upsideDown = false;
                opponentId = message["B"] as string;
            }
            else
            {
                upsideDown = true;
                opponentId = message["A"] as string;
            }

            textLabel.text = "준비!";
            textLabel.gameObject.SetActive(true);

            // 준비 완료 메세지 송신
            Invoke("SendReady", 1);
        }
        else
        {
            // 매칭 실패
            ModalWindow.Instance.Open("매칭 실패", "매칭에 실패했습니다.", RollbackToMenuPhase);
        }
    }

    void SendReady() { NetworkManager.Instance.Send("ready"); }

    // 메뉴로 돌아감
    void RollbackToMenuPhase()
    {
        gameRoot.SetActive(false);
        textLabel.gameObject.SetActive(false);
        matchButton.gameObject.SetActive(true);
        state = GAME_STATE.MENU;
    }

    // 게임 시작
    public void StartGame()
    {
        textLabel.gameObject.SetActive(false);
        matchButton.gameObject.SetActive(false);
        state = GAME_STATE.GAME;

        if (upsideDown == true)
            ball.SetBallProperties(0, 0, 1.5f, -1.5f);
        else
            ball.SetBallProperties(0, 0, -1.5f, 1.5f);
    }

    // 게임 중 정보 업데이트
    public void RelayMessageReceived(Dictionary<string, object> message)
    {
        // ball의 정보가 업데이트됨
        if (message.ContainsKey("ballX") && message.ContainsKey("ballY") && message.ContainsKey("ballVX") && message.ContainsKey("ballVY"))
        {
            float x = Convert.ToSingle(message["ballX"]);
            float y = Convert.ToSingle(message["ballY"]);
            float vx = Convert.ToSingle(message["ballVX"]);
            float vy = Convert.ToSingle(message["ballVY"]);
            ball.SetBallProperties(-x, -y, -vx, -vy);
        }

        // 상대 bar의 위치가 변경됨
        if (message.ContainsKey("barX"))
        {
            if (message.ContainsKey("timeSeq"))
            {
                float barTimeSeq = Convert.ToSingle(message["timeSeq"]);
                if (barTimeSeq < lastBarTimeSeq)
                    return;
                lastBarTimeSeq = barTimeSeq;
            }
            float barX = Convert.ToSingle(message["barX"]);
            oppBar.transform.localPosition = new Vector3(-barX, oppBar.transform.localPosition.y, oppBar.transform.localPosition.z);
        }
    }

    // 서버의 결과 응답을 처리
    public void ResultMessageReceived(Dictionary<string, object> message)
    {
        if (message["result"].Equals("win"))
            Win();
        else
            Lose();
    }

    // 승리 화면
    void Win()
    {
        if (state != GAME_STATE.GAME)
            return;
        gameRoot.SetActive(false);
        ModalWindow.Instance.Open("결과", "승리했습니다!", RollbackToMenuPhase);
        state = GAME_STATE.WIN;
    }

    // 패배 화면
    void Lose()
    {
        if (state != GAME_STATE.GAME)
            return;
        gameRoot.SetActive(false);
        ModalWindow.Instance.Open("결과", "패배했습니다!", RollbackToMenuPhase);
        state = GAME_STATE.LOSE;
    }

    void Update()
    {
        switch (state)
        {
            case GAME_STATE.INIT:
                if (NetworkManager.Instance.state == NetworkManager.STATE.READY)
                {
                    // 세션 생성, 로그인 완료, 메뉴로
                    textLabel.gameObject.SetActive(false);
                    matchButton.gameObject.SetActive(true);
                    state = GAME_STATE.MENU;
                }
                break;
            case GAME_STATE.GAME:
                // 승패 처리
                if (ball.gameObject.transform.localPosition.y < myBar.transform.localPosition.y - 80)
                {
                    // 패배
                    Dictionary<string, object> message = new Dictionary<string, object>();
                    message["result"] = "lose";
                    NetworkManager.Instance.Send("result", message);
                }
                break;
        }
    }



    void OnApplicationPause(bool isPaused)
    {
        if (!networkEnabled)
            return;

        if(isPaused)
        {
            NetworkManager.Instance.Stop();
        }
        else
        {
            state = GAME_STATE.INIT;
            ModalWindow.Instance.Close();
            matchButton.gameObject.SetActive(false);
            gameRoot.SetActive(false);
            textLabel.text = "접속 중입니다.";
            textLabel.gameObject.SetActive(true);

            NetworkManager.Instance.Init();
        }
    }

    public void Quit()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}

using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class GameLogic : Singleton<GameLogic>
{
    public Button matchButton;
    public Text textLabel;
    public GameObject gameRoot;

    public Rigidbody2D ballBody;

    public bool networkEnabled = true;

    public GameObject myBar;
    public GameObject oppBar;

    public enum GAME_STATE {
        INIT,       // init. game
        READY,      // wait user input (click match button)
        MATCHING,   // matching..
        GAME,       // playing game
        RESULT,     // game result

        NONE
    }
    GAME_STATE state;

    bool upsideDown = false;
    string opponentId = "";

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

    // 매치 요청
    void OnMatchButtonClick()
    {
        matchButton.gameObject.SetActive(false);
        textLabel.text = "매칭 중입니다";
        textLabel.gameObject.SetActive(true);
        state = GAME_STATE.MATCHING;

        NetworkManager.Instance.Send("match");
    }

    // 매치 성공
    public void OnMatchSuccess(Dictionary<string, object> message)
    {
        textLabel.gameObject.SetActive(false);
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

        Invoke("SendReady", 1);
    }

    void SendReady()
    {
        NetworkManager.Instance.Send("ready");
    }

    // 매치 실패
    public void OnMatchFailed()
    {
        textLabel.text = "매칭에 실패 했습니다";
        Invoke("RollbackToReadyPhase", 2);
    }

    void RollbackToReadyPhase()
    {
        textLabel.gameObject.SetActive(false);
        matchButton.gameObject.SetActive(true);
        state = GAME_STATE.READY;
    }

    // 게임 시작
    public void StartGame()
    {
        textLabel.gameObject.SetActive(false);
        matchButton.gameObject.SetActive(false);
        state = GAME_STATE.GAME;

        // 위치 초기화
        myBar.gameObject.transform.localPosition = new Vector3(0, myBar.gameObject.transform.localPosition.y);
        oppBar.gameObject.transform.localPosition = new Vector3(0, oppBar.gameObject.transform.localPosition.y);
        ballBody.gameObject.transform.localPosition = new Vector3();
        ballBody.velocity = new Vector2();

        if (upsideDown == true)
            ballBody.velocity = new Vector2(1.5f, -1.5f);
        else
            ballBody.velocity = new Vector2(-1.5f, 1.5f);
    }

    // 게임 중 정보 업데이트
    public void RelayMessageReceived(Dictionary<string, object> message)
    {
        if(message.ContainsKey("barX"))
        {
            // 상대 bar의 위치가 변경됨
            int barX = (int)oppBar.transform.localPosition.x;
            if (int.TryParse(message["barX"] as string, out barX))
                oppBar.transform.localPosition = new Vector3(-barX, oppBar.transform.localPosition.y, oppBar.transform.localPosition.z);
        }

        if(message.ContainsKey("ballX") && message.ContainsKey("ballY"))
        {
            float ballX = ballBody.position.x;
            float ballY = ballBody.position.y;
            if (float.TryParse(message["ballX"] as string, out ballX) && float.TryParse(message["ballY"] as string, out ballY))
                ballBody.position = new Vector2(-ballX, -ballY);
        }

        if (message.ContainsKey("ballVX") && message.ContainsKey("ballVY"))
        {
            float ballVX = ballBody.velocity.x;
            float ballVY = ballBody.velocity.y;
            if (float.TryParse(message["ballVX"] as string, out ballVX) && float.TryParse(message["ballVY"] as string, out ballVY))
                ballBody.velocity = new Vector2(-ballVX, -ballVY);
        }
    }

    public void ResultMessageReceived(Dictionary<string, object> message)
    {
        if (message["result"].Equals("win"))
        {
            // 강제 승리
            Win();
        }
        else
        {
            // 강제 패배
            Lose();
        }
    }

    void Win()
    {
        if (state != GAME_STATE.GAME)
            return;

        gameRoot.SetActive(false);
        textLabel.text = "승리!";
        textLabel.gameObject.SetActive(true);
        state = GAME_STATE.RESULT;

        Invoke("RollbackToReadyPhase", 3);
    }

    void Lose()
    {
        if (state != GAME_STATE.GAME)
            return;

        gameRoot.SetActive(false);
        textLabel.text = "패배";
        textLabel.gameObject.SetActive(true);
        state = GAME_STATE.RESULT;

        Invoke("RollbackToReadyPhase", 3);
    }

    void Update()
    {
        switch (state)
        {
            case GAME_STATE.INIT:
                if (NetworkManager.Instance.state == NetworkManager.STATE.READY)
                {
                    textLabel.gameObject.SetActive(false);
                    matchButton.gameObject.SetActive(true);
                    state = GAME_STATE.READY;
                }
                break;
            case GAME_STATE.GAME:
                // 승패 처리
                if (ballBody.gameObject.transform.localPosition.y > oppBar.transform.localPosition.y + 32)
                {
                    // 승리
                    Dictionary<string, object> message = new Dictionary<string, object>();
                    message["result"] = "win";
                    NetworkManager.Instance.Send("result", message);

                    Win();
                }
                else if (ballBody.gameObject.transform.localPosition.y < myBar.transform.localPosition.y - 32)
                {
                    // 패배
                    Dictionary<string, object> message = new Dictionary<string, object>();
                    message["result"] = "lose";
                    NetworkManager.Instance.Send("result", message);

                    Lose();
                }
                break;
        }
    }

    public void ErrorQuit()
    {
        matchButton.gameObject.SetActive(false);
        gameRoot.SetActive(false);
        textLabel.text = "에러가 발생 했습니다. 게임을 종료합니다.";
        textLabel.gameObject.SetActive(true);
        Invoke("Quit", 3);
    }

    void Quit()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}

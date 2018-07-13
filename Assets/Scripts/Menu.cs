﻿using Fun;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// protobuf
using funapi.network.fun_message;
using pong_messages;

public class Menu : MonoBehaviour
{
    public AnnounceBoard announceBoard;

    private Button btnStart = null;
    private Button btnMatching = null;
    private Button btnCancelMatching = null;
    private Button btnLeaderboard = null;
    private Text matchRecord;
    private GameObject login;
    private GameObject main;
    private GameObject recordBoard;


    private void Awake()
    {
        login = transform.Find("Login").gameObject;
        main = transform.Find("Main").gameObject;
        recordBoard = transform.parent.Find("RecordBoard").gameObject;

        btnStart = main.transform.Find("StartGame").GetComponent<Button>();
        btnMatching = main.transform.Find("StartMatching").GetComponent<Button>();
        btnCancelMatching = main.transform.Find("CancelMatching").GetComponent<Button>();
        btnLeaderboard = main.transform.Find("LeaderBoard").GetComponent<Button>();
        matchRecord = main.transform.Find("MatchRecord").GetComponent<Text>();

        OnLoginMenu();
    }

    /// <summary>
    /// login menu's button events to move main menu
    /// </summary>
    public void OnSinglePlayClicked()
    {
        GameLogic.Instance.SinglePlayLogin();
    }

    public void OnGuestLoggedInClicked()
    {
        GameLogic.Instance.GuestLogin();
    }

    public void OnFBLoggedInClicked()
    {
        GameLogic.Instance.FBLogin();
    }

    public void WaitMenu()
    {
        foreach (var button in login.GetComponentsInChildren<Button>())
        {
            button.interactable = false;
        }
    }

    public void OnLoginMenu()
    {
        login.SetActive(true);
        main.SetActive(false);
        recordBoard.SetActive(false);
    }

    public void OnSinglePlayMainMenu()
    {
        OnDefaultMainMenu();
        btnStart.interactable = true;
        btnMatching.interactable = false;
        btnLeaderboard.interactable = false;
    }

    public void OnMultiplayMainMenu()
    {
        OnDefaultMainMenu();
        btnStart.interactable = false;
        btnMatching.interactable = true;
        btnLeaderboard.interactable = true;
    }

    private void OnDefaultMainMenu()
    {
        login.SetActive(false);
        main.SetActive(true);
        btnCancelMatching.gameObject.SetActive(false);
    }

    /// <summary>
    /// main menu's button events
    /// </summary>
    public void OnAnnounceClicked()
    {
        NetworkManager.AnnouncementServerSetting setting = NetworkManager.Instance.announcementServer;
        if (string.IsNullOrEmpty(setting.url))
        {
            ModalWindow.Instance.Open("공지 서버", "서버 주소가 설정되어 있지 않습니다.");
            return;
        }

        gameObject.SetActive(false);
        announceBoard.Show(setting.url);
    }

    public void OnLeaderBoardClicked()
    {
        gameObject.SetActive(false);
        recordBoard.SetActive(true);
        GameLogic.Instance.RequestRankList();
    }

    public void OnStartClicked()
    {
        GameLogic.Instance.StartSingleGamePlay();
    }

    public void OnMatchingClicked()
    {
        btnMatching.interactable = false;
        btnCancelMatching.gameObject.SetActive(true);

        GameLogic.Instance.RequestMatching();
    }

    public void OnCancelMatchingClicked()
    {
        GameLogic.Instance.RequestCancelMatching();
    }

    public void OnQuitClicked()
    {
        AppUtil.Quit();
    }

    public void SetActive(bool enable)
    {
        gameObject.SetActive(enable);
    }

    public void SetMatchRecord(int winCount, int loseCount, int curRecord)
    {
        var recordText = string.Format("총 전적 : {0}승 {1}패 | 현재 {2} 연승", winCount, loseCount, curRecord);
        matchRecord.text = recordText;
    }

    public void SetRecordBoard(FunEncoding encoding, object body)
    {
        Transform usersTransform = recordBoard.transform.Find("Users");

        if (encoding == FunEncoding.kJson)
        {
            Dictionary<string, object> message = body as Dictionary<string, object>;
            Dictionary<string, object> rank = message["ranks"] as Dictionary<string, object>;
            var count = rank.Count;

            for (int i = 0; i < count; i++)
            {
                Dictionary<string, object> subMessage = rank[i.ToString()] as Dictionary<string, object>;
                string gameObjectName = string.Format("User{0}", i + 1);
                Text textComponent = usersTransform.Find(gameObjectName).transform.GetComponentInChildren<Text>();

                textComponent.text = string.Format("{0}위 : {1}연승\nid: {2} ",
                    subMessage["rank"],
                    subMessage["score"],
                    subMessage["id"]);
            }
        }
        else
        {
            FunMessage fun_msg = body as FunMessage;
            LobbyRankListReply message = FunapiMessage.GetMessage<LobbyRankListReply>(fun_msg, MessageType.lobby_rank_list_repl);
            if (message == null)
            {
                ModalWindow.Instance.Open("Error!", "Invalid protobuf message", GameLogic.Instance.ShowMenu);
                return;
            }

            int i = 0;
            foreach (LobbyRankListReply.RankElement subMessage in message.rank)
            {
                string gameObjectName = string.Format("User{0}", i+1);
                Text textComponent = usersTransform.Find(gameObjectName).transform.GetComponentInChildren<Text>();

                textComponent.text = string.Format("{0}위 : {1}연승\nid: {2} ",
                    subMessage.rank,
                    subMessage.score,
                    subMessage.id);
                ++i;
            }
        }
    }
}

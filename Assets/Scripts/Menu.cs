using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Menu : MonoBehaviour
{
    public AnnounceBoard announceBoard;

    private Button btnStart = null;
    private Button btnMatching = null;
    private Button btnCancelMatching = null;
    private Text matchRecord;
    private GameObject login;
    private GameObject main;
    private GameObject recordBoard;

    private void Awake()
    {
        login = transform.FindChild("Login").gameObject;
        main = transform.FindChild("Main").gameObject;
        recordBoard = transform.parent.FindChild("RecordBoard").gameObject;

        btnStart = main.transform.FindChild("StartGame").GetComponent<Button>();
        btnMatching = main.transform.FindChild("StartMatching").GetComponent<Button>();
        btnCancelMatching = main.transform.FindChild("CancelMatching").GetComponent<Button>();
        matchRecord = main.transform.FindChild("MatchRecord").GetComponent<Text>();
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
    }

    public void OnMultiplayMainMenu()
    {
        OnDefaultMainMenu();
        btnStart.interactable = false;
        btnMatching.interactable = true;
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
        gameObject.SetActive(false);
        announceBoard.Show();
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

    public void SetRecordBoard(Dictionary<string, object> message)
    {
        var count = message.Count;
        Transform usersTransform = recordBoard.transform.FindChild("Users");

        for (int i = 0; i < count; i++)
        {
            Dictionary<string, object> subMessage = message[i.ToString()] as Dictionary<string, object>;
            string gameObjectName = string.Format("User{0}", i + 1);
            Text textComponent = usersTransform.FindChild(gameObjectName).transform.GetComponentInChildren<Text>();

            textComponent.text = string.Format("{0}위 : {1}연승\nid: {2} ",
                subMessage["rank"],
                subMessage["score"],
                subMessage["id"]);
        }
    }
}
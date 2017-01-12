using UnityEngine;
using UnityEngine.UI;

public class Menu : MonoBehaviour
{
    public AnnounceBoard announceBoard;

    private Button btnStart = null;
    private Button btnMatching = null;
    private Button btnCancelMatching = null;

    private GameObject login;
    private GameObject main;

    private void Awake()
    {
        login = transform.FindChild("Login").gameObject;
        main = transform.FindChild("Main").gameObject;

        btnStart = main.transform.FindChild("StartGame").GetComponent<Button>();
        btnMatching = main.transform.FindChild("StartMatching").GetComponent<Button>();
        btnCancelMatching = main.transform.FindChild("CancelMatching").GetComponent<Button>();

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

    public void OnLoginMenu()
    {
        login.SetActive(true);
        main.SetActive(false);
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
}
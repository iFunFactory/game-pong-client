using System.Collections;
using UnityEngine;
using UnityEngine.UI;


public class Menu : MonoBehaviour
{
    void Awake ()
    {
        btnStart = transform.FindChild("StartGame").GetComponent<Button>();
        btnMatching = transform.FindChild("StartMatching").GetComponent<Button>();
        btnCancelMatching = transform.FindChild("CancelMatching").GetComponent<Button>();

        btnMatching.interactable = false;
        OnDisableCancelMatchingBtn();
    }

    public void OnAnnounceClicked ()
    {
        gameObject.SetActive(false);
        announceBoard.Show();
    }

    public void OnStartClicked ()
    {
        GameLogic.Instance.StartGame();
    }

    public void OnMatchingClicked ()
    {
        btnMatching.interactable = false;
        btnCancelMatching.gameObject.SetActive(true);

        GameLogic.Instance.RequestMatching();
    }

    public void OnCancelMatchingClicked()
    {
        GameLogic.Instance.RequestCancelMatching();
    }

    public void OnQuitClicked ()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    public void SetActive (bool enable)
    {
        gameObject.SetActive(enable);
    }

    public void EnableMatchingButton ()
    {
        btnMatching.interactable = true;
    }

    public void OnConnected ()
    {
        btnStart.interactable = false;
    }

    public void OnDisonnected ()
    {
        btnStart.interactable = true;
    }

    public void OnDisableCancelMatchingBtn()
    {
        btnCancelMatching.gameObject.SetActive(false);
    }

    // Member variables.
    public AnnounceBoard announceBoard;

    Button btnStart = null;
    Button btnMatching = null;
    Button btnCancelMatching = null;
}

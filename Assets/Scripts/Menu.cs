using System.Collections;
using UnityEngine;
using UnityEngine.UI;


public class Menu : MonoBehaviour
{
    void Awake ()
    {
        btnConnect = transform.FindChild("ConnectToServer").GetComponent<Button>();
        btnMatching = transform.FindChild("StartMatching").GetComponent<Button>();

        btnMatching.interactable = false;
    }

    public void OnAnnounceClicked ()
    {
        gameObject.SetActive(false);
        announceBoard.Show();
    }

    public void OnConnectClicked ()
    {
        GameLogic.Instance.Connect();
    }

    public void OnMatchingClicked ()
    {
        btnMatching.interactable = false;
        GameLogic.Instance.RequestMatching();
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
        btnConnect.interactable = false;
    }

    public void OnDisonnected ()
    {
        btnConnect.interactable = true;
    }


    // Member variables.
    public AnnounceBoard announceBoard;

    Button btnConnect = null;
    Button btnMatching = null;
}

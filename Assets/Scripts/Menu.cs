using System.Collections;
using UnityEngine;
using UnityEngine.UI;


public class Menu : MonoBehaviour
{
    void Awake ()
    {
        btnStart = transform.FindChild("StartGame").GetComponent<Button>();
        btnMatching = transform.FindChild("StartMatching").GetComponent<Button>();

        btnMatching.interactable = false;
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
        btnStart.interactable = false;
    }

    public void OnDisonnected ()
    {
        btnStart.interactable = true;
    }


    // Member variables.
    public AnnounceBoard announceBoard;

    Button btnStart = null;
    Button btnMatching = null;
}

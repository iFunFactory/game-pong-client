using UnityEngine;
using UnityEngine.UI;


// 확인 팝업으로 사용할 modal window
public class ModalWindow : MonoBehaviour
{
    public Button okButton;
    public Text titleText;
    public Text contentsText;
    public GameObject panel;

    public delegate void OnClickButton();
    OnClickButton onClickButtonCallback = null;

    private static ModalWindow _instance = null;
    public static ModalWindow Instance
    {
        get
        {
            if (_instance == null)
                _instance = FindObjectOfType<ModalWindow>();
            return _instance;
        }
        private set { return; }
    }

    void Start()
    {
        // bring to front
        transform.SetAsLastSibling();
    }

    public void Open(string title, string contents, OnClickButton onClickButtonCallback = null)
    {
        // set title
        titleText.text = title;
        // set contents
        contentsText.text = contents;
        // set button callback
        this.onClickButtonCallback = onClickButtonCallback;
        // show!
        panel.gameObject.SetActive(true);
    }

    public void Close()
    {
        panel.gameObject.SetActive(false);
    }

    public void OnClickOkButton()
    {
        if (onClickButtonCallback != null)
            onClickButtonCallback();
        Close();
    }
}

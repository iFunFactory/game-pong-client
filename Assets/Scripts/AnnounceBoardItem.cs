using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class AnnounceBoardItem : MonoBehaviour
{
    void Awake ()
    {
        item_rect_ = transform.GetComponent<RectTransform>();

        summary_ = transform.FindChild("Summary").gameObject;
        summary_rect_ = summary_.GetComponent<RectTransform>();

        details_ = transform.FindChild("Details").gameObject;
        details_text_ = details_.GetComponent<Text>();
        details_rect_ = details_.GetComponent<RectTransform>();

        activeItem(false);
    }

    public void SetData (Dictionary<string, object> data, AnnounceBoard board)
    {
        board_ = board;

        // Summary
        Text summary = summary_.GetComponentInChildren<Text>();
        summary.text = data["subject"] as string;

        // Details
        string details = data["message"] as string;
        if (details[details.Length - 1] != '\n')
            details += "\n";
        details_text_.text = details;

        item_rect_.sizeDelta = new Vector2(0f, kItemHeight);
    }

    public void OnSelect ()
    {
        activeItem(true);

        item_height_ = details_text_.preferredHeight;
        item_rect_.sizeDelta = new Vector2(0f, item_height_);
        details_rect_.offsetMin = new Vector2(8f, -item_height_);

        board_.SelectItem(this);
    }

    public void OnDeselect ()
    {
        item_height_ = kItemHeight;
        item_rect_.sizeDelta = new Vector2(0f, item_height_);

        if (summary_rect_.rect.height < kItemHeight)
            summary_rect_.offsetMin = new Vector2(0f, kItemHeight);

        activeItem(false);
    }

    public float height
    {
        get { return item_height_; }
    }

    void activeItem (bool active)
    {
        summary_.SetActive(!active);
        details_.SetActive(active);
    }


    public const float kItemHeight = 60f;

    // Member variables.
    AnnounceBoard board_ = null;
    float item_height_ = kItemHeight;

    GameObject summary_ = null;
    GameObject details_ = null;
    Text details_text_ = null;
    RectTransform item_rect_ = null;
    RectTransform summary_rect_ = null;
    RectTransform details_rect_ = null;
}

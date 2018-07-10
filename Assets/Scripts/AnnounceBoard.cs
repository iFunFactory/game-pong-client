using Fun;
using System.Collections.Generic;
using UnityEngine;

public class AnnounceBoard : MonoBehaviour
{
    private const string kServerAddr = "127.0.0.1";
    private const ushort kServerPort = 8080;

    private void Awake()
    {
        GameObject content = GameObject.FindWithTag("Content");
        if (content != null)
        {
            content_transform_ = content.transform;
            content_rect_ = content.GetComponent<RectTransform>();
        }

        announce_ = new FunapiAnnouncement();
        announce_.ResultCallback += onAnnouncementResult;
        announce_.Init(string.Format("http://{0}:{1}", kServerAddr, kServerPort));
    }

    private void Update()
    {
        event_.Update(Time.deltaTime);
    }

    public void Show()
    {
        gameObject.SetActive(true);
        announce_.UpdateList(10);
    }

    public void SelectItem(AnnounceBoardItem item)
    {
        if (selected_item_ != null)
            selected_item_.OnDeselect();

        selected_item_ = item;

        measureContentSize();
    }

    private void OnClose()
    {
        gameObject.SetActive(false);
    }

    private void onAnnouncementResult(AnnounceResult result)
    {
        if (result != AnnounceResult.kSucceeded)
            return;

        event_.Add(() => updateList());
    }

    private void updateList()
    {
        if (content_transform_ == null)
            return;

        removeAllItems();

        float cur_pos_y = 0;
        for (int i = 0; i < announce_.ListCount; ++i)
        {
            Dictionary<string, object> data = announce_.GetAnnouncement(i);
            GameObject item = GameObject.Instantiate(Resources.Load("Prefabs/AnnounceItem")) as GameObject;
            if (item != null)
            {
                item.name = "Item";
                item.transform.SetParent(content_transform_);
                item.transform.localScale = new Vector3(1, 1, 1);
                item.transform.localPosition = new Vector3(0, cur_pos_y, 0);
                cur_pos_y -= AnnounceBoardItem.kItemHeight;

                AnnounceBoardItem script = item.GetComponent<AnnounceBoardItem>();
                script.SetData(data, this);
                item_list_.Add(script);
            }
        }

        if (content_rect_ != null)
        {
            content_rect_.sizeDelta = new Vector2(0f, Mathf.Abs(cur_pos_y));
            content_rect_.localPosition = new Vector3();
        }
    }

    private void removeAllItems()
    {
        foreach (Transform t in content_transform_)
        {
            GameObject.Destroy(t.gameObject);
        }
        item_list_.Clear();
    }

    private void measureContentSize()
    {
        if (content_rect_ == null)
            return;

        float cur_pos_y = 0f;
        float target_pos_y = 0f;

        foreach (AnnounceBoardItem item in item_list_)
        {
            if (cur_pos_y > 0f)
                item.transform.localPosition = new Vector3(0f, -cur_pos_y);

            if (item == selected_item_)
                target_pos_y = cur_pos_y;

            cur_pos_y += item.height;
        }

        if (target_pos_y > cur_pos_y - kViewHeight)
            target_pos_y = cur_pos_y - kViewHeight;

        content_rect_.sizeDelta = new Vector2(0f, cur_pos_y);
        content_rect_.localPosition = new Vector3(0f, target_pos_y);
    }

    private const float kViewHeight = 470f;

    // Member variables.
    private FunapiAnnouncement announce_ = null;

    private PostEventList event_ = new PostEventList();

    private RectTransform content_rect_ = null;
    private Transform content_transform_ = null;
    private AnnounceBoardItem selected_item_ = null;
    private List<AnnounceBoardItem> item_list_ = new List<AnnounceBoardItem>();
}
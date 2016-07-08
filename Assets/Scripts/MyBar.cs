using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class MyBar : Singleton<MyBar>, IDragHandler
{
    float lastBarX;

    void Start()
    {
        lastBarX = gameObject.transform.localPosition.x;
    }

    public void OnDrag(PointerEventData eventData)
    {
        gameObject.transform.localPosition += new Vector3(eventData.delta.x, 0, 0);
    }

    void Update()
    {
        if (Mathf.Abs(lastBarX - gameObject.transform.localPosition.x) > 1)
        {
            lastBarX = gameObject.transform.localPosition.x;

            Dictionary<string, object> message = new Dictionary<string, object>();
            message["barX"] = ((int)gameObject.transform.localPosition.x).ToString();
            NetworkManager.Instance.Send("relay", message);
        }
    }
}

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class MyBar : Singleton<MyBar>, IDragHandler
{
    public CanvasScaler canvasScaler;
    float lastBarX;

    void Start()
    {
        lastBarX = gameObject.transform.localPosition.x;
    }

    public void OnDrag(PointerEventData eventData)
    {
        float deltaScaler = canvasScaler.referenceResolution.y / Screen.height;
        gameObject.transform.localPosition += new Vector3(eventData.delta.x * deltaScaler, 0, 0);
    }

    void Update()
    {
        // 일정 이상 bar의 위치가 이동되었으면, 상대에게 알린다
        if (Mathf.Abs(lastBarX - gameObject.transform.localPosition.x) > 1)
        {
            lastBarX = gameObject.transform.localPosition.x;

            Dictionary<string, object> message = new Dictionary<string, object>();
            message["barX"] = ((int)gameObject.transform.localPosition.x).ToString();
            NetworkManager.Instance.Send("relay", message);
        }
    }
}

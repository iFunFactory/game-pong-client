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
        Debug.Log("canvasScaler.scaleFactor: " + canvasScaler.scaleFactor);
        lastBarX = gameObject.transform.localPosition.x;
    }

    public void OnDrag(PointerEventData eventData)
    {
        float deltaScaler = canvasScaler.referenceResolution.y / Screen.height;
        gameObject.transform.localPosition += new Vector3(eventData.delta.x * deltaScaler, 0, 0);
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

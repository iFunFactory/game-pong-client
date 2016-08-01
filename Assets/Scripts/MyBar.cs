using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class MyBar : Singleton<MyBar>, IDragHandler
{
    public CanvasScaler canvasScaler;
    float lastBarX = 0;

    // 터치 드래그시 이동
    public void OnDrag(PointerEventData eventData)
    {
        // 해상도에 따른 보정
        float deltaScaler = canvasScaler.referenceResolution.y / Screen.height;
        gameObject.transform.localPosition += new Vector3(eventData.delta.x * deltaScaler, 0, 0);
    }

    void Update()
    {
        // bar의 위치가 이동되었으면, 상대에게 알린다
        if (Mathf.Abs(lastBarX - gameObject.transform.localPosition.x) > 1)
        {
            lastBarX = gameObject.transform.localPosition.x;

            Dictionary<string, object> message = new Dictionary<string, object>();
            // udp를 활용하므로, time sequencer를 추가
            message["timeSeq"] = Time.realtimeSinceStartup;
            message["barX"] = gameObject.transform.localPosition.x;
            NetworkManager.Instance.Send("relay", message, Fun.TransportProtocol.kUdp);
        }
    }
}

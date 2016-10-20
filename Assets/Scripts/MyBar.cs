using UnityEngine;
using System.Collections.Generic;


public class MyBar : DragBar
{
    public void Ready (bool multiPlay)
    {
        isMultiPlay = multiPlay;
        SetPosX(0f);
    }

    void Update()
    {
        if (!isMultiPlay)
            return;

        // bar의 위치가 이동되었으면, 상대에게 알린다
        if (Mathf.Abs(lastBarX - transform.localPosition.x) > 1)
        {
            lastBarX = transform.localPosition.x;

            Dictionary<string, object> message = new Dictionary<string, object>();
            // udp를 활용하므로, time sequencer를 추가
            message["timeSeq"] = Time.realtimeSinceStartup;
            message["barX"] = transform.localPosition.x;
            NetworkManager.Instance.Send("relay", message, Fun.TransportProtocol.kUdp);
        }
    }


    bool isMultiPlay = false;
    float lastBarX = 0;
}

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;


public class DragBar : MonoBehaviour, IDragHandler
{
    void Awake ()
    {
        CanvasScaler canvasScaler = GameObject.FindWithTag("Canvas").GetComponent<CanvasScaler>();
        deltaScaler = canvasScaler.referenceResolution.y / Screen.height;
    }

    public void OnDrag (PointerEventData eventData)
    {
        float px = transform.localPosition.x + eventData.delta.x * deltaScaler;
        px = Mathf.Max(Mathf.Min(px, kEndPosX), -kEndPosX);
        transform.localPosition = new Vector3(px, transform.localPosition.y);
    }

    public void SetPosX (float px)
    {
        transform.localPosition = new Vector3(px, transform.localPosition.y);
    }


    const float kEndPosX = 137f;
    float deltaScaler = 1f;
}

using UnityEngine;
using UnityEngine.UI;


public class OppBar : DragBar
{
    void Awake ()
    {
        barImage = GetComponent<Image>();
        barCollider = GetComponent<BoxCollider2D>();
    }

    public void Ready (bool multiPlay)
    {
        barImage.raycastTarget = !multiPlay;
        barCollider.enabled = !multiPlay;

        SetPosX(0f);
    }


    Image barImage = null;
    BoxCollider2D barCollider = null;
}

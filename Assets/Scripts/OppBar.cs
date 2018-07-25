using UnityEngine;
using UnityEngine.UI;


public class OppBar : DragBar
{
    void Awake ()
    {
        barImage = GetComponent<Image>();
        barCollider = GetComponent<BoxCollider2D>();

        ball = transform.parent.Find("Ball").gameObject;
        ballRigidbody = ball.GetComponent<Rigidbody2D>();

        barWidth = GetComponent<RectTransform>().rect.width;

        leftWallPosition = transform.parent.Find("WallLeft").localPosition;
        rightWallPosition = transform.parent.Find("WallRight").localPosition;
    }

    public void Ready (bool _multiPlay)
    {
        multiPlay = _multiPlay;
        barImage.raycastTarget = !multiPlay;
        barCollider.enabled = !multiPlay;

        SetPosX(0f);

        //싱글 플레이인 경우 사용 될 opp bar 이동 속도 레벨 랜덤 생성
        level = Random.Range(0.8f, 1f);
    }

    void FixedUpdate()
    {
        //싱글 플레이인 경우 opp bar 움직임 처리
        if(!multiPlay)
        {
            //공이 opp bar 에게 오는 상황에서만 움직임
            if(ballRigidbody.velocity.y < 0)
            {
                return;
            }

            SetPosX(nextPosition());
        }
    }

    private float nextPosition()
    {
        float pos = Mathf.Lerp(transform.localPosition.x, ball.transform.localPosition.x,
                              ballRigidbody.velocity.magnitude * level * Time.deltaTime);

        //bar가 wall을 벗어나는 경우 처리
        float offset =  barWidth / 2f;

        if (pos - offset < leftWallPosition.x)
        {
            pos = leftWallPosition.x + offset;
        }
        else if (pos + offset > rightWallPosition.x)
        {
            pos = rightWallPosition.x - offset;
        }

        return pos;
    }

    Image barImage = null;
    BoxCollider2D barCollider = null;
    float barWidth;
    GameObject ball = null;
    Rigidbody2D ballRigidbody = null;
    Vector3 leftWallPosition;
    Vector3 rightWallPosition;
    bool multiPlay = false;
    float level = 1f;

}

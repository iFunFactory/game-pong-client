using UnityEngine;
using System.Collections.Generic;

public class Ball : MonoBehaviour
{
    public Rigidbody2D rigidBody;

    // collision detection, 충돌이 일어났을 때의 위치와 방향을 동기화
    void OnCollisionEnter2D(Collision2D coll)
    {
        // 상대 방향쪽으로(=위로) 가는 경우에만 정보를 보냄
        if (rigidBody.velocity.y > 0)
        {
            Dictionary<string, object> message = new Dictionary<string, object>();
            message["ballX"] = gameObject.transform.localPosition.x;
            message["ballY"] = gameObject.transform.localPosition.y;
            message["ballVX"] = rigidBody.velocity.x;
            message["ballVY"] = rigidBody.velocity.y;
            NetworkManager.Instance.Send("relay", message);
        }
    }

    // ball의 위치와 속도 초기화
    public void Reset()
    {
        gameObject.transform.localPosition = new Vector3();
        rigidBody.velocity = new Vector2();
    }

    // 상대가 보낸 ball의 정보를 설정
    public void SetBallProperties(float x, float y, float vx, float vy)
    {
        // 보내온 y좌표가 현재 y좌표보다 작다 = 상대의 진행이 더 빠르다 > 좌표 보정
        if (y < gameObject.transform.localPosition.y)
            gameObject.transform.localPosition = new Vector3(x, y);
        rigidBody.velocity = new Vector2(vx, vy);
    }
}

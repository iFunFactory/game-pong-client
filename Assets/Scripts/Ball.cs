using UnityEngine;
using System.Collections.Generic;

public class Ball : MonoBehaviour
{
    public Rigidbody2D rigidBody;

    void OnCollisionEnter2D(Collision2D coll)
    {
        if (!coll.gameObject.name.Equals("OppBar") && rigidBody.velocity.y > 0)
        {
            Dictionary<string, object> message = new Dictionary<string, object>();
            message["ballX"] = gameObject.transform.localPosition.x;
            message["ballY"] = gameObject.transform.localPosition.y;
            message["ballVX"] = rigidBody.velocity.x;
            message["ballVY"] = rigidBody.velocity.y;
            NetworkManager.Instance.Send("relay", message);
        }
    }

    public void Reset()
    {
        gameObject.transform.localPosition = new Vector3();
        rigidBody.velocity = new Vector2();
    }

    public void SetBallProperties(float x, float y, float vx, float vy)
    {
        if (y < gameObject.transform.localPosition.y)
            gameObject.transform.localPosition = new Vector3(x, y);
        rigidBody.velocity = new Vector2(vx, vy);
    }
}

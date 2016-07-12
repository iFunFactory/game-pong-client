using UnityEngine;
using System.Collections.Generic;

public class Ball : MonoBehaviour
{
    Rigidbody2D rigidBody;

    Vector2 lastCollisionPosition = new Vector2();
    Vector2 lastCollisionVelocity = new Vector2();

    void Start()
    {
        rigidBody = gameObject.GetComponent<Rigidbody2D>();
    }

    void OnCollisionEnter2D(Collision2D coll)
    {
        if (rigidBody.velocity.y > 0)
        {
            Dictionary<string, object> message = new Dictionary<string, object>();
            message["ballX"] = rigidBody.position.x.ToString();
            message["ballY"] = rigidBody.position.y.ToString();
            message["ballVX"] = rigidBody.velocity.x.ToString();
            message["ballVY"] = rigidBody.velocity.y.ToString();
            NetworkManager.Instance.Send("relay", message);
        }

        lastCollisionPosition = rigidBody.position;
        lastCollisionVelocity = rigidBody.velocity;
    }

    public void Reset()
    {
        gameObject.transform.localPosition = new Vector3();
        rigidBody.velocity = new Vector2();
        lastCollisionPosition = new Vector2();
        lastCollisionVelocity = new Vector2();
    }

    public void SetBallProperties(float x, float y, float vx, float vy)
    {
        if ((lastCollisionPosition - new Vector2(x, y)).magnitude < 0.1f && (lastCollisionVelocity - new Vector2(vx, vy)).magnitude < 0.1f)
            return;
        rigidBody.position = new Vector2(x, y);
        rigidBody.velocity = new Vector2(vx, vy);
    }
}

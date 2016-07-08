using UnityEngine;
using System.Collections.Generic;

public class Ball : MonoBehaviour
{
    Rigidbody2D rigidBody;

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
    }
}

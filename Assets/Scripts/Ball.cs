﻿using Fun;
using UnityEngine;
using System.Collections.Generic;

// protobuf
using funapi.network.fun_message;
using pong_messages;

public class Ball : MonoBehaviour
{
    void Awake ()
    {
        rigidBody = GetComponent<Rigidbody2D>();
    }

    // ball의 위치와 속도 초기화
    public void Reset (bool multiPlay)
    {
        isMultiPlay = multiPlay;
        transform.localPosition = new Vector3();
        rigidBody.velocity = new Vector2();
    }

    // 상대가 보낸 ball의 정보를 설정
    public void SetProperties (float x, float y, float vx, float vy)
    {
        // 보내온 y좌표가 현재 y좌표보다 작다 = 상대의 진행이 더 빠르다 > 좌표 보정
        if (y < transform.localPosition.y)
            transform.localPosition = new Vector3(x, y);

        rigidBody.velocity = new Vector2(vx, vy);
    }

    public void SendProperties ()
    {
        if (NetworkManager.Instance.GetEncoding() == Fun.FunEncoding.kJson)
        {
            Dictionary<string, object> message = new Dictionary<string, object>();
            message["ballX"] = transform.localPosition.x;
            message["ballY"] = transform.localPosition.y;
            message["ballVX"] = rigidBody.velocity.x;
            message["ballVY"] = rigidBody.velocity.y;
            NetworkManager.Instance.Send("relay", message);
        }
        else
        {
            GameRelayMessage msg = new GameRelayMessage();
            msg.ballX = transform.localPosition.x;
            msg.ballY = transform.localPosition.y;
            msg.ballVX = rigidBody.velocity.x;
            msg.ballVY = rigidBody.velocity.y;

            FunMessage fun_msg = FunapiMessage.CreateFunMessage(msg, MessageType.game_relay);
            NetworkManager.Instance.Send("relay", fun_msg);
        }
    }

    // collision detection, 충돌이 일어났을 때의 위치와 방향을 동기화
    void OnCollisionEnter2D (Collision2D coll)
    {
        //ball min/max velocity 제한
        float y = (Mathf.Abs(rigidBody.velocity.y) < minVY) ? Mathf.Sign(rigidBody.velocity.y) * minVY : rigidBody.velocity.y;
        rigidBody.velocity = new Vector2(Vector2.ClampMagnitude(rigidBody.velocity, maxV).x, y);

        if (!isMultiPlay)
            return;

        // 상대 방향쪽으로(=위로) 가는 경우에만 정보를 보냄
        if (rigidBody.velocity.y > 0)
            SendProperties();
    }


    bool isMultiPlay = false;
    Rigidbody2D rigidBody;
    float maxV = 40f;
    float minVY = 1f;
}

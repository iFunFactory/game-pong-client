using System;
using UnityEngine;
using Fun;
using System.Collections.Generic;

public class NetworkManager : Singleton<NetworkManager>
{
    public string server_addr = "carlos-vm";
    public UInt16 server_tcp_port = 8012;
    public UInt16 server_udp_port = 8013;

    public enum STATE
    {
        START,      // session is started (not initialized, not connected, not logined)
        INITED,     // session initialized
        READY,      // session is ready
        CLOSED,     // session closed
        ERROR,      // error occurred

        UNKNOWN,    // unknown state
    }
    public STATE state { get; private set; }

    public string MyId;

    private FunapiSession session;
    private MessageHelper messageHelper;

    // 네트워크 초기화
    public void Init()
    {
        // uid를 구한다
#if UNITY_EDITOR
        MyId = SystemInfo.deviceUniqueIdentifier + "_Editor";
#else
        MyId = SystemInfo.deviceUniqueIdentifier + "_" + SystemInfo.deviceType;
#endif
        // 세션 생성
        state = STATE.START;
        session = FunapiSession.Create(server_addr, false);
        session.SessionEventCallback += OnSessionEvent;         // 세션 이벤트 처리
        session.TransportEventCallback += OnTransportEvent;     // Transport 이벤트 처리
        session.ReceivedMessageCallback += OnReceive;           // 메세지 핸들러
        // tcp connect
        session.Connect(TransportProtocol.kTcp, FunEncoding.kJson, server_tcp_port, new TcpTransportOption());
        // udp connect
        session.Connect(TransportProtocol.kUdp, FunEncoding.kJson, server_udp_port, new TransportOption());
    }

    void OnSessionEvent(SessionEventType type, string session_id)
    {
        FunDebug.Log("[EVENT] Session {0}.", type.ToString().ToLower().Substring(1));

        switch(type)
        {
            case SessionEventType.kOpened:
                // login
                Dictionary<string, object> body = new Dictionary<string, object>();
                body["id"] = MyId;
                state = STATE.INITED;
                session.SendMessage("login", body);
                break;
        }
    }

    void OnTransportEvent(TransportProtocol protocol, TransportEventType type)
    {
        FunDebug.Log("[EVENT] Transport {0}.", type.ToString().ToLower().Substring(1));

        switch(type)
        {
            case TransportEventType.kDisconnected:
                // reconnect
                session.Connect(protocol);
                break;
        }
    }

    void OnReceive(string msg_type, object body)
    {
        Dictionary<string, object> message = body as Dictionary<string, object>;
        switch(msg_type)
        {
            case "login":
                if(message["result"].Equals("ok"))
                {
                    // login ok
                    state = STATE.READY;
                }
                else
                {
                    // login failure
                    state = STATE.ERROR;
                }
                break;
            case "match":
                if(message["result"].Equals("Success"))
                    GameLogic.Instance.OnMatchSuccess(message);
                else
                    GameLogic.Instance.OnMatchFailed();
                break;
            case "start":
                GameLogic.Instance.StartGame();
                break;
            case "relay":
                GameLogic.Instance.RelayMessageReceived(message);
                break;
            case "result":
                GameLogic.Instance.ResultMessageReceived(message);
                break;
        }
    }

    public void Send(string messageType, Dictionary<string, object> body = null, TransportProtocol protocol = TransportProtocol.kDefault)
    {
        if (!GameLogic.Instance.networkEnabled)
            return;
        if (body == null)
            body = new Dictionary<string, object>();
        session.SendMessage(messageType, body, EncryptionType.kDefaultEncryption, protocol);
    }

    void OnSessionClosed()
    {
        state = STATE.CLOSED;
    }
}

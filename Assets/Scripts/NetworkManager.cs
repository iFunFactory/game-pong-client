using System;
using UnityEngine;
using Fun;
using System.Collections.Generic;

public class NetworkManager : Singleton<NetworkManager>
{
    // 서버 주소를 수정하세요.
    public string server_addr = "carlos-vm";
    // 서버 포트 정보
    public ushort server_tcp_port = 8012;
    public ushort server_udp_port = 8013;

    public enum STATE
    {
        START,      // session is started (not initialized, not connected, not logined)
        INITED,     // session initialized
        READY,      // session is ready
        CLOSED,     // session closed
        ERROR,      // error occurred
    }
    public STATE state { get; private set; }

    public string MyId;

    private FunapiSession session;

    // 네트워크 초기화
    public void Init()
    {
        // uid를 구해서 ID로 쓴다
#if UNITY_EDITOR
        MyId = SystemInfo.deviceUniqueIdentifier + "_Editor";   // 에디터용
#else
        MyId = SystemInfo.deviceUniqueIdentifier + "_" + SystemInfo.deviceType;
#endif
        state = STATE.START;
        session = FunapiSession.Create(server_addr, false);
        session.SessionEventCallback += OnSessionEvent;
        session.TransportEventCallback += OnTransportEvent;
        session.ReceivedMessageCallback += OnReceive;

        session.Connect(TransportProtocol.kTcp, FunEncoding.kJson, server_tcp_port, new TcpTransportOption());
        session.Connect(TransportProtocol.kUdp, FunEncoding.kJson, server_udp_port, new TransportOption());
    }

    // session 이벤트 처리
    void OnSessionEvent(SessionEventType type, string session_id)
    {
        Debug.Log("OnSessionEvent: " + type);
        switch(type)
        {
            case SessionEventType.kOpened:
                // 세션이 생성되면, 바로 로그인 한다.
                Dictionary<string, object> body = new Dictionary<string, object>();
                body["id"] = MyId;
                state = STATE.INITED;
                session.SendMessage("login", body);
                break;
            case SessionEventType.kClosed:
                state = STATE.CLOSED;
                break;
        }
    }

    // transport 이벤트 처리
    void OnTransportEvent(TransportProtocol protocol, TransportEventType type)
    {
        Debug.Log("OnTransportEvent: " + protocol + " : " + type);
        switch (type)
        {
            case TransportEventType.kDisconnected:
                // 연결이 끊기면 재연결
                session.Connect(protocol);
                break;
            case TransportEventType.kConnectionFailed:
            case TransportEventType.kConnectionTimedOut:
                // 연결에 실패함
                ModalWindow.Instance.Open("연결 실패", "서버 연결에 실패했습니다.\n게임을 다시 시작해 주세요.\n" + type.ToString(), Application.Quit);
                break;
        }
    }

    // 메세지 핸들러
    void OnReceive(string msg_type, object body)
    {
        Dictionary<string, object> message = body as Dictionary<string, object>;
        switch(msg_type)
        {
            case "login":
                if(message["result"].Equals("ok"))
                    state = STATE.READY;
                else
                {
                    // 로그인 실패
                    state = STATE.ERROR;
                    ModalWindow.Instance.Open("로그인 실패", "로그인에 실패했습니다.\n게임을 다시 시작해 주세요.", Application.Quit);
                }
                break;
            case "match":
                GameLogic.Instance.OnMatch(message);
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
        if (!GameLogic.Instance.isNetworkEnabled)
            return;
        if (body == null)
            body = new Dictionary<string, object>();
        session.SendMessage(messageType, body, protocol);
    }

    public void Stop()
    {
        session.Close();
    }
}

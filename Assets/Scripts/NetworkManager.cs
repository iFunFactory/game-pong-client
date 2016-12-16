using Fun;
using System;
using System.Collections.Generic;
using UnityEngine;


public class NetworkManager : Singleton<NetworkManager>
{
    // 서버 포트 정보
    const ushort kServerTcpPort = 8012;
    const ushort kServerUdpPort = 8013;


    void Awake ()
    {
        // uid를 구해서 ID로 쓴다
#if UNITY_EDITOR
        myId = SystemInfo.deviceUniqueIdentifier + "_Editor";   // 에디터용
#else
        myId = SystemInfo.deviceUniqueIdentifier + "_" + SystemInfo.deviceType;
#endif
    }

    // 네트워크 초기화
    public void Connect ()
    {
        state = STATE.START;

        if (session == null)
        {
            session = FunapiSession.Create(GameLogic.Instance.serverAddr, false);
            session.SessionEventCallback += OnSessionEvent;
            session.TransportEventCallback += OnTransportEvent;
            session.ReceivedMessageCallback += OnReceive;

            session.Connect(TransportProtocol.kTcp, FunEncoding.kJson, kServerTcpPort, new TcpTransportOption());
            session.Connect(TransportProtocol.kUdp, FunEncoding.kJson, kServerUdpPort, new TransportOption());
        }
        else
        {
            session.Connect(TransportProtocol.kTcp);
            session.Connect(TransportProtocol.kUdp);
        }
    }

    public bool IsReady
    {
        get { return state == STATE.READY; }
    }

    public void Stop ()
    {
        if (session != null)
            session.Close();
    }

    public void Send (string messageType, Dictionary<string, object> body = null,
                      TransportProtocol protocol = TransportProtocol.kDefault)
    {
        if (!GameLogic.Instance.isMultiPlay)
            return;

        if (body == null)
            body = new Dictionary<string, object>();

        session.SendMessage(messageType, body, protocol);
    }


    // session 이벤트 처리
    void OnSessionEvent (SessionEventType type, string session_id)
    {
        Debug.Log("OnSessionEvent: " + type);

        switch (type)
        {
        case SessionEventType.kOpened:
            state = STATE.INITED;

            // 세션이 생성되면, 바로 로그인 한다.
            Dictionary<string, object> body = new Dictionary<string, object>();
            body["id"] = myId;
            session.SendMessage("login", body);
            break;

        case SessionEventType.kClosed:
            state = STATE.CLOSED;
            break;
        }
    }

    // transport 이벤트 처리
    void OnTransportEvent (TransportProtocol protocol, TransportEventType type)
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
    void OnReceive (string msg_type, object body)
    {
        Dictionary<string, object> message = body as Dictionary<string, object>;

        switch (msg_type)
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
            {
                if (message["result"].Equals("Success"))
                {
                    // 매칭 성공

                    bool bRoomMaster = false;
                    if (message["A"].Equals(myId))
                        bRoomMaster = true;

                    GameLogic.Instance.OnMatch(bRoomMaster);
                }
                else
                {
                    // 매칭 실패
                    ModalWindow.Instance.Open("매칭 실패", "매칭에 실패했습니다.", GameLogic.Instance.ShowMenu);
                }
            }
            break;
        case "start":
            GameLogic.Instance.StartPlay();
            break;
        case "relay":
            GameLogic.Instance.RelayMessageReceived(message);
            break;
        case "result":
            GameLogic.Instance.ResultMessageReceived(message);
            break;
        }
    }


    enum STATE
    {
        START,      // session is started (not initialized, not connected, not logined)
        INITED,     // session initialized
        READY,      // session is ready
        CLOSED,     // session closed
        ERROR,      // error occurred
    }

    STATE state;
    string myId = "";
    FunapiSession session = null;
}

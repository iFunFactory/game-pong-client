using Fun;
using System;
using System.Collections.Generic;
using UnityEngine;

public class NetworkManager : Singleton<NetworkManager>
{
    // 서버 주소를 수정하세요.
    public string kServerAddr = "127.0.0.1";

    // 서버 포트 정보
    private const ushort kServerTcpPort = 8012;

    private const ushort kServerUdpPort = 8013;

    // Options
    public bool sessionReliability = false;

    public bool sequenceValidation = false;
    public int sendingCount = 10;

    [Header("TCP Option")]
    public EncryptionType tcpEncryption = EncryptionType.kDefaultEncryption;

    public bool autoReconnect = false;
    public bool disableNagle = false;
    public bool usePing = false;

    [Header("UDP Option")]
    public EncryptionType udpEncryption = EncryptionType.kDefaultEncryption;

    [Header("HTTP Option")]
    public EncryptionType httpEncryption = EncryptionType.kDefaultEncryption;

    public bool useWWW = false;

    private enum STATE
    {
        START,      // session is started (not initialized, not connected, not logined)
        INITED,     // session initialized
        READY,      // session is ready
        CLOSED,     // session closed
        ERROR,      // error occurred
    }

    private STATE state;
    private string deviceId = "";
    private string myId = "";
    private FunapiSession session = null;

    private void Awake()
    {
        // uid를 구해서 ID로 쓴다
#if UNITY_EDITOR
        deviceId = SystemInfo.deviceUniqueIdentifier + "_Editor";   // 에디터용
#else
        deviceId = SystemInfo.deviceUniqueIdentifier + "_" + SystemInfo.deviceType;
#endif
    }

    // 네트워크 초기화
    public void Connect()
    {
        state = STATE.START;

        if (session == null)
        {
            session = FunapiSession.Create(kServerAddr, sessionReliability);
            session.SessionEventCallback += OnSessionEvent;
            session.TransportEventCallback += OnTransportEvent;
            session.ReceivedMessageCallback += OnReceive;

            tryConnect(TransportProtocol.kTcp);
            tryConnect(TransportProtocol.kUdp);
        }
        else
        {
            session.Connect(TransportProtocol.kTcp);
            session.Connect(TransportProtocol.kUdp);
        }
    }

    private void tryConnect(TransportProtocol protocol)
    {
        TransportOption option = makeOption(protocol);
        ushort port = getPort(protocol, FunEncoding.kJson);

        session.Connect(protocol, FunEncoding.kJson, port, option);
    }

    private ushort getPort(TransportProtocol protocol, FunEncoding encoding)
    {
        ushort port = 0;
        if (protocol == TransportProtocol.kTcp)
            port = (ushort)(encoding == FunEncoding.kJson ? 8012 : 8022);
        else if (protocol == TransportProtocol.kUdp)
            port = (ushort)(encoding == FunEncoding.kJson ? 8013 : 8023);
        else if (protocol == TransportProtocol.kHttp)
            port = (ushort)(encoding == FunEncoding.kJson ? 8018 : 8028);

        return port;
    }

    private TransportOption makeOption(TransportProtocol protocol)
    {
        TransportOption option = null;

        if (protocol == TransportProtocol.kTcp)
        {
            TcpTransportOption tcp_option = new TcpTransportOption();
            tcp_option.Encryption = tcpEncryption;
            tcp_option.AutoReconnect = autoReconnect;
            tcp_option.DisableNagle = disableNagle;

            if (usePing)
                tcp_option.SetPing(1, 20, true);

            option = tcp_option;
        }
        else if (protocol == TransportProtocol.kUdp)
        {
            option = new TransportOption();
            option.Encryption = udpEncryption;
        }
        else if (protocol == TransportProtocol.kHttp)
        {
            HttpTransportOption http_option = new HttpTransportOption();
            http_option.Encryption = httpEncryption;
            http_option.UseWWW = useWWW;

            option = http_option;
        }

        option.ConnectionTimeout = 10f;
        option.SequenceValidation = sequenceValidation;

        return option;
    }

    public bool IsReady
    {
        get { return state == STATE.READY; }
    }

    public void Stop()
    {
        if (session != null)
            session.Stop();
    }

    public void Send(string messageType, Dictionary<string, object> body = null,
                      TransportProtocol protocol = TransportProtocol.kDefault)
    {
        if (GameLogic.Instance.loginType == GameLogic.LOGIN_TYPE.SINGLE) return;

        if (body == null)
            body = new Dictionary<string, object>();

        session.SendMessage(messageType, body, protocol);
    }

    // session 이벤트 처리
    private void OnSessionEvent(SessionEventType type, string session_id)
    {
        switch (type)
        {
            case SessionEventType.kOpened:
                state = STATE.INITED;
                GameLogic.Instance.WaitMenu();

                // 게스트 로그인일 경우 세션이 생성되면, 바로 로그인 한다.
                if (GameLogic.Instance.loginType == GameLogic.LOGIN_TYPE.MULTI_GUEST)
                {
                    Dictionary<string, object> body = new Dictionary<string, object>();
                    body["id"] = deviceId;
                    body["type"] = "guest";
                    session.SendMessage("login", body);
                }
                else if (GameLogic.Instance.loginType == GameLogic.LOGIN_TYPE.MULTI_FACEBOOK)
                {
                    FacebookManager.Instance.login();
                }
                break;

            case SessionEventType.kClosed:
                state = STATE.CLOSED;
                break;
        }
    }

    // transport 이벤트 처리
    private void OnTransportEvent(TransportProtocol protocol, TransportEventType type)
    {
        switch (type)
        {
            case TransportEventType.kDisconnected:
                // 연결이 끊기면 재연결
                session.Connect(protocol);
                break;

            case TransportEventType.kConnectionFailed:
            case TransportEventType.kConnectionTimedOut:
                // 연결에 실패함
                ModalWindow.Instance.Open("연결 실패", "서버 연결에 실패했습니다.\n게임을 다시 시작해 주세요.\n" + type.ToString(), AppUtil.Quit);
                break;
        }
    }

    // 메세지 핸들러
    private void OnReceive(string msg_type, object body)
    {
        Dictionary<string, object> message = body as Dictionary<string, object>;

        switch (msg_type)
        {
            case "login":
                if (message["result"].Equals("ok"))
                {
                    myId = message["id"].ToString();
                    state = STATE.READY;
                    int winCount;
                    int loseCount;
                    int curRecord;
                    Int32.TryParse(message["winCount"].ToString(), out winCount);
                    Int32.TryParse(message["loseCount"].ToString(), out loseCount);
                    Int32.TryParse(message["curRecord"].ToString(), out curRecord);
                    GameLogic.Instance.SetMatchRecord(winCount, loseCount, curRecord);
                }
                else
                {
                    // 로그인 실패
                    state = STATE.ERROR;

                    ModalWindow.Instance.Open("로그인 실패", message["msg"].ToString(), AppUtil.Quit);
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
                        var modalTitle = "매칭 실패";
                        var modalContent = "매칭에 실패했습니다.";

                        // 매칭 취소
                        if (message["result"].Equals("Cancel"))
                        {
                            modalTitle = "매칭 취소";
                            modalContent = "매칭을 취소했습니다.";
                        }
                        ModalWindow.Instance.Open(modalTitle, modalContent, GameLogic.Instance.ShowMenu);
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

            case "ranklist":
                GameLogic.Instance.RecordlistMessageReceived(message);
                break;
        }
    }
}
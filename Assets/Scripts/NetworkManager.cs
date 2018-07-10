using Fun;
using System;
using System.Collections.Generic;
using UnityEngine;

using funapi.network.fun_message;


public class NetworkManager : Singleton<NetworkManager>
{
    // 로비 서버 주소를 수정하세요.
    // 게임 서버의 주소는 매치메이킹 이후에 로비 서버가 패킷으로 전달하기 때문에 별도로 기재하지 않습니다.
    public string kLobbyServerAddr = "";

    // 로비 서버 포트 정보를 수정하세요.
    // 서버측 MANIFEST.lobby.json 상에서 활성화된 포트 번호를 여기 기재합니다.
    // 게임 서버의 포트는 매치메이킹 이후에 로비 서버가 패킷으로 전달하기 때문에 별도로 기재하지 않습니다.
    private const ushort kLobbyServerPort = 8012;

    // 로비 서버에 접속할 프로토콜을 지정하세요.
    // kTcp, kUdp, kHttp 가 가능합니다.
    // 게임 서버의 경우는 매치메이킹 이후 로비 서버가 패킷으로 전달하기 때문에 별도로 기재하지 않습니다.
    // 여기의 값을 수정하게 될 경우, MANIFEST.lobby.json 의 포트 정보 역시 수정해야됩니다.
    // (예, TCP 에 JSON 을 쓸 경우 "tcp_json_port" 값을 0 이 아닌 값으로 지정)
    private TransportProtocol kLobbyServerProtocol = TransportProtocol.kTcp;

    // 클라이언트-서버 통신에 사용될 메시지 포맷을 지정하세요.
    // kJson 과 kProtobuf 가 가능합니다.
    // 게임 서버의 경우는 매치메이킹 이후 로비 서버가 패킷으로 전달하기 때문에 별도로 기재하지 않습니다.
    // 여기의 값을 수정하게 될 경우, MANIFEST.lobby.json 의 포트 정보 역시 수정해야됩니다.
    // (예, TCP 에 JSON 을 쓸 경우 "tcp_json_port" 값을 0 이 아닌 값으로 지정)
    public FunEncoding kLobbyServerEncoding = FunEncoding.kJson;

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
            if (kLobbyServerAddr == "")
            {
                ModalWindow.Instance.Open("Network Error", "Server address was not given.", AppUtil.Quit);
            }

            SessionOption option = new SessionOption();
            option.sessionReliability = sessionReliability;

            session = FunapiSession.Create(kLobbyServerAddr, option);
            session.SessionEventCallback += OnSessionEvent;
            session.TransportEventCallback += OnTransportEvent;
	    session.TransportOptionCallback += OnTransportOption;
            session.ReceivedMessageCallback += OnReceive;
        }
        TransportOption transport_opt = OnTransportOption("lobby", kLobbyServerProtocol);
	    session.Connect(kLobbyServerProtocol, kLobbyServerEncoding, kLobbyServerPort, transport_opt);
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

    public FunEncoding GetEncoding(TransportProtocol protocol = TransportProtocol.kDefault)
    {
      return session.GetEncoding(protocol);
    }

    public void Send(string messageType, TransportProtocol protocol = TransportProtocol.kDefault)
    {
        FunEncoding encoding = session.GetEncoding(protocol);
        if (encoding == FunEncoding.kJson)
        {
            Dictionary<string, object> body = new Dictionary<string, object>();
            Send(messageType, body, protocol);
        }
        else if (encoding == FunEncoding.kProtobuf)
        {
            FunMessage body = new FunMessage();
            Send(messageType, body, protocol);
        }
        else
        {
            FunDebug.Assert(false);
        }
    }

    // Json 버전 Send
    public void Send(string messageType, Dictionary<string, object> body,
                      TransportProtocol protocol = TransportProtocol.kDefault)
    {
        if (GameLogic.Instance.loginType == GameLogic.LOGIN_TYPE.SINGLE) return;

        session.SendMessage(messageType, body, protocol);
    }

    public void Send(string messageType, FunMessage body,
                     TransportProtocol protocol = TransportProtocol.kDefault)
    {
        if (GameLogic.Instance.loginType == GameLogic.LOGIN_TYPE.SINGLE) return;

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
        if (type == TransportEventType.kStopped)
        {
            TransportError.Type error = session.GetLastError(protocol);

            switch (error)
            {
            case TransportError.Type.kDisconnected:
                // 연결이 끊기면 재연결
                session.Connect(protocol);
                break;

            case TransportError.Type.kStartingFailed:
            case TransportError.Type.kConnectionTimeout:
                // 연결에 실패함
                ModalWindow.Instance.Open("연결 실패", "서버 연결에 실패했습니다.\n게임을 다시 시작해 주세요.\n" + type.ToString(), AppUtil.Quit);
                break;

            default:
                if (error != TransportError.Type.kNone)
                    ModalWindow.Instance.Open("연결 끊김", "서버와의 연결이 끊겼습니다.\n게임을 다시 시작해 주세요.\n" + type.ToString(), AppUtil.Quit);
                break;
            }
        }
    }

    // transport 별 옵션 처리
    private TransportOption OnTransportOption(string server_flavor, TransportProtocol protocol)
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

            case "error":
                ModalWindow.Instance.Open("Error!", message["msg"].ToString(), GameLogic.Instance.ShowMenu);
                break;
        }
    }
}

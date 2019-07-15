using Fun;
using System;
using System.Collections.Generic;
using UnityEngine;

// protobuf
using funapi.network.fun_message;
using pong_messages;


public class NetworkManager : Singleton<NetworkManager>
{
    [System.Serializable]
    public class AnnouncementServerSetting
    {
        // 샘플용 공지 서버 주소입니다.
        // 공지 서버를 따로 구성했다면 이 주소를 변경해 주세요.
        public string url = "http://example.ifunfactory.com";
    }

    [System.Serializable]
    public class LobbyServerSetting
    {
        // 로비 서버 주소를 수정하세요.
        // 게임 서버의 주소는 매치메이킹 이후에 로비 서버가 패킷으로 전달하기 때문에 별도로 기재하지 않습니다.
        public string address = "";

        // 로비 서버 포트 정보를 수정하세요.
        // 서버측 MANIFEST.lobby.json 상에서 활성화된 포트 번호를 여기 기재합니다.
        // 게임 서버의 포트는 매치메이킹 이후에 로비 서버가 패킷으로 전달하기 때문에 별도로 기재하지 않습니다.
        public ushort port = 8012;

        // 로비 서버에 접속할 프로토콜을 지정하세요.
        // kTcp, kUdp, kHttp 가 가능합니다.
        // 게임 서버의 경우는 매치메이킹 이후 로비 서버가 패킷으로 전달하기 때문에 별도로 기재하지 않습니다.
        // 여기의 값을 수정하게 될 경우, MANIFEST.lobby.json 의 포트 정보 역시 수정해야됩니다.
        // (예, TCP 에 JSON 을 쓸 경우 "tcp_json_port" 값을 0 이 아닌 값으로 지정)
        public TransportProtocol protocol = TransportProtocol.kTcp;

        // 클라이언트-서버 통신에 사용될 메시지 포맷을 지정하세요.
        // kJson 과 kProtobuf 가 가능합니다.
        // 게임 서버의 경우는 매치메이킹 이후 로비 서버가 패킷으로 전달하기 때문에 별도로 기재하지 않습니다.
        // 여기의 값을 수정하게 될 경우, MANIFEST.lobby.json 의 포트 정보 역시 수정해야됩니다.
        // (예, TCP 에 JSON 을 쓸 경우 "tcp_json_port" 값을 0 이 아닌 값으로 지정)
        public FunEncoding encoding = FunEncoding.kJson;

        // Options
        public bool sessionReliability = false;

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

        [Header("Websocket Option")]
        public EncryptionType websocketEncryption = EncryptionType.kDefaultEncryption;
    }

    public AnnouncementServerSetting announcementServer;

    [Space(5)]
    public LobbyServerSetting lobbyServer;


    private enum STATE
    {
        START,      // session is started (not initialized, not connected, not logined)
        INITED,     // session initialized
        READY,      // session is ready
        STOPPED,    // session stopped
        CLOSED,     // session closed
        ERROR,      // error occurred
    }

    private STATE state;
    private string deviceId = "";
    private string myId = "";
    private FunapiSession session = null;

    private void Awake()
    {
        // guid를 구해서 ID로 쓴다
        string guid = Guid.NewGuid().ToString("N");
        if (guid.Length > 6)
            guid = guid.Substring(0, 6);
        deviceId = "Player_" + guid;
    }

    // 네트워크 초기화
    public void Connect()
    {
        state = STATE.START;

        if (session == null)
        {
            if (lobbyServer.address == "")
            {
                ModalWindow.Instance.Open("Network Error", "Server address was not given.", AppUtil.Quit);
            }

            SessionOption option = new SessionOption();
            option.sessionReliability = lobbyServer.sessionReliability;

            session = FunapiSession.Create(lobbyServer.address, option);
            session.SessionEventCallback += OnSessionEvent;
            session.TransportEventCallback += OnTransportEvent;
            session.TransportOptionCallback += OnTransportOption;
            session.ReceivedMessageCallback += OnReceive;
        }
        TransportOption transport_opt = OnTransportOption("lobby", lobbyServer.protocol);
        session.Connect(lobbyServer.protocol, lobbyServer.encoding, lobbyServer.port, transport_opt);
    }

    public void Stop()
    {
        if(session != null)
        {
            session.Stop();
        }
    }

    public bool IsReady
    {
        get { return state == STATE.READY; }
    }

    public bool Stopped
    {
        get { return state == STATE.STOPPED; }
    }

    void OnApplicationQuit()
    {
        Stop();
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
        session.SendMessage(messageType, body, protocol);
    }

    public void Send(string messageType, FunMessage body,
                     TransportProtocol protocol = TransportProtocol.kDefault)
    {
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
                    if (session.GetEncoding(TransportProtocol.kDefault) == FunEncoding.kJson)
                    {
                        Dictionary<string, object> body = new Dictionary<string, object>();
                        body["id"] = deviceId;
                        body["type"] = "guest";
                        session.SendMessage("login", body);
                    } else {
                        LobbyLoginRequest msg = new LobbyLoginRequest();
                        msg.id = deviceId;
                        msg.type = "guest";
                        FunMessage fun_msg = FunapiMessage.CreateFunMessage(msg, MessageType.lobby_login_req);
                        session.SendMessage("login", fun_msg);
                    }
                }
                else if (GameLogic.Instance.loginType == GameLogic.LOGIN_TYPE.MULTI_FACEBOOK)
                {
                    FacebookManager.Instance.login();
                }
                break;

            case SessionEventType.kStopped:
                FunapiSession.Destroy(session);
                session = null;
                state = STATE.STOPPED;
                break;

            case SessionEventType.kClosed:
                state = STATE.CLOSED;
                break;

            case SessionEventType.kRedirectSucceeded:
                GameLogic.Instance.OnReady();
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
            tcp_option.Encryption = lobbyServer.tcpEncryption;
            tcp_option.AutoReconnect = lobbyServer.autoReconnect;
            tcp_option.DisableNagle = lobbyServer.disableNagle;

            if (lobbyServer.usePing)
                tcp_option.SetPing(1, 20, true);

            option = tcp_option;
        }
        else if (protocol == TransportProtocol.kUdp)
        {
            option = new TransportOption();
            option.Encryption = lobbyServer.udpEncryption;
        }
        else if (protocol == TransportProtocol.kHttp)
        {
            HttpTransportOption http_option = new HttpTransportOption();
            http_option.Encryption = lobbyServer.httpEncryption;
            http_option.UseWWW = lobbyServer.useWWW;

            option = http_option;
        }
        else if (protocol == TransportProtocol.kWebsocket)
        {
            option = new TransportOption();
            option.Encryption = lobbyServer.websocketEncryption;
        }


        option.ConnectionTimeout = 10f;

        return option;
    }

    // 메세지 핸들러
    private void OnReceive(string msg_type, object body)
    {
        FunEncoding encoding = session.GetEncoding(TransportProtocol.kDefault);

        switch (msg_type)
        {
            case "login":
                {
                    string result = "";
                    string msg = "";
                    int winCount = 0;
                    int loseCount = 0;
                    int curRecord = 0;
                    int singleWinCount = 0;
                    int singleLoseCount = 0;
                    int singleCurRecord = 0;

                    if (encoding == FunEncoding.kJson)
                    {
                        Dictionary<string, object> message = body as Dictionary<string, object>;
                        result = message["result"].ToString();
                        if (result == "ok")
                        {
                            myId = message["id"].ToString();
                            Int32.TryParse(message["winCount"].ToString(), out winCount);
                            Int32.TryParse(message["loseCount"].ToString(), out loseCount);
                            Int32.TryParse(message["curRecord"].ToString(), out curRecord);
                            Int32.TryParse(message["singleWinCount"].ToString(), out singleWinCount);
                            Int32.TryParse(message["singleLoseCount"].ToString(), out singleLoseCount);
                            Int32.TryParse(message["singleCurRecord"].ToString(), out singleCurRecord);
                        }
                        else
                        {
                            msg = message["msg"].ToString();
                        }
                    }
                    else if (encoding == FunEncoding.kProtobuf)
                    {
                        FunMessage fun_msg = body as FunMessage;
                        LobbyLoginReply message = FunapiMessage.GetMessage<LobbyLoginReply>(fun_msg, MessageType.lobby_login_repl);
                        if (message == null)
                        {
                            ModalWindow.Instance.Open("Error!", "Invalid protobuf message", GameLogic.Instance.ShowMenu);
                            return;
                        }
                        result = message.result;
                        if (result == "ok")
                        {
                            myId = message.id;
                            winCount = message.win_count;
                            loseCount = message.lose_count;
                            curRecord = message.cur_record;
                            singleWinCount = message.win_count_single;
                            singleLoseCount = message.lose_count_single;
                            singleCurRecord = message.cur_record_single;
                        }
                        else
                        {
                            msg = message.msg;
                        }
                    }

                    if (result == "ok")
                    {
                        state = STATE.READY;
                        GameLogic.Instance.SetMatchRecord(
                            winCount, loseCount, curRecord, singleWinCount, singleLoseCount, singleCurRecord);
                    }
                    else
                    {
                        // 로그인 실패
                        state = STATE.ERROR;
                        ModalWindow.Instance.Open("로그인 실패", msg, AppUtil.Quit);
                    }
                }
                break;

            case "match":
                {
                    string result = "";
                    bool bRoomMaster = false;

                    if (encoding == FunEncoding.kJson)
                    {
                        Dictionary<string, object> message = body as Dictionary<string, object>;
                        result = message["result"].ToString();
                        if (result == "Success")
                        {
                            // 매칭 성공
                            // A player 가 방장이 되어 공을 제어한다.
                            if (message["A"].Equals(myId))
                                bRoomMaster = true;
                        }
                    }
                    else if (encoding == FunEncoding.kProtobuf)
                    {
                        FunMessage fun_msg = body as FunMessage;
                        LobbyMatchReply message = FunapiMessage.GetMessage<LobbyMatchReply>(fun_msg, MessageType.lobby_match_repl);
                        if (message == null)
                        {
                            ModalWindow.Instance.Open("Error!", "Invalid protobuf message", GameLogic.Instance.ShowMenu);
                            return;
                        }
                        result = message.result;
                        if (result == "Success")
                        {
                            // 매칭 성공
                            // A player 가 방장이 되어 공을 제어한다.
                            if (message.player1 == myId)
                                bRoomMaster = true;
                        }
                    }

                    if (result == "Success")
                    {
                        GameLogic.Instance.OnMatch(bRoomMaster);
                    }
                    else
                    {
                        // 매칭 실패
                        var modalTitle = "매칭 실패";
                        var modalContent = "매칭에 실패했습니다.";

                        // 매칭 취소
                        if (result == "Cancel")
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
                GameLogic.Instance.RelayMessageReceived(encoding, body);
                break;

            case "result":
                GameLogic.Instance.ResultMessageReceived(encoding, body);
                break;

            case "ranklist":
            case "ranklist_single":
                GameLogic.Instance.RecordlistMessageReceived(encoding, body);
                break;

            case "error":
                {
                    string msg = "";
                    if (encoding == FunEncoding.kJson)
                    {
                        Dictionary<string, object> message = body as Dictionary<string, object>;
                        msg = message["msg"].ToString();
                    }
                    else
                    {
                        FunMessage fun_msg = body as FunMessage;
                        PongErrorMessage message = FunapiMessage.GetMessage<PongErrorMessage>(fun_msg, MessageType.pong_error);
                        if (message == null)
                        {
                            ModalWindow.Instance.Open("Error!", "Invalid protobuf message", GameLogic.Instance.ShowMenu);
                            return;
                        }
                        msg = message.msg;
                    }
                    ModalWindow.Instance.Open("Error!", msg, GameLogic.Instance.ShowMenu);
                }
                break;
        }
    }
}

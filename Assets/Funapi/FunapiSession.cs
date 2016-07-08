// Copyright 2013-2016 iFunFactory Inc. All Rights Reserved.
//
// This work is confidential and proprietary to iFunFactory Inc. and
// must not be used, disclosed, copied, or distributed without the prior
// consent of iFunFactory Inc.

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
#if !NO_UNITY
using UnityEngine;
#endif

// Protobuf
using ProtoBuf;
using funapi.network.fun_message;


namespace Fun
{
    public enum SessionEventType
    {
        kOpened,
        kClosed,
        kChanged
    };

    public enum TransportEventType
    {
        kStarted,
        kStopped,
        kConnectFailed,
        kConnectTimedout,
        kDisconnected
    };


    public class FunapiSession : FunapiUpdater
    {
        //
        // Create an instance of FunapiSession.
        //
        public static FunapiSession Create (string hostname_or_ip, bool session_reliability)
        {
            return new FunapiSession(hostname_or_ip, session_reliability);
        }

        private FunapiSession (string hostname_or_ip, bool session_reliability)
        {
            state_ = State.kUnknown;
            server_address_ = hostname_or_ip;
            SessionReliability = session_reliability;

            InitSession();
        }

        //
        // Public functions.
        //
        public void Connect (TransportProtocol protocol, FunEncoding encoding,
                             UInt16 port, TransportOption option)
        {
            FunapiTransport transport = CreateTransport(protocol, encoding, port, option);
            if (transport == null)
                return;

            Connect(protocol);
        }

        public void Connect (TransportProtocol protocol)
        {
            if (!Started)
            {
                FunDebug.Log("Starting a network module.");

                lock (state_lock_)
                {
                    state_ = State.kStarted;
                }

                CreateUpdater();
            }

            event_list.Add (delegate
            {
                FunapiTransport transport = GetTransport(protocol);
                if (transport == null)
                    return;

                if (transport.Protocol == TransportProtocol.kHttp)
                    ((FunapiHttpTransport)transport).mono = mono;

                FunDebug.Log("Starting {0} transport.", transport.str_protocol);
                transport.Start();
            });
        }

        public void Stop ()
        {
            StopSession();
        }

        public void Stop (TransportProtocol protocol)
        {
            FunapiTransport transport = GetTransport(protocol);
            if (transport == null)
            {
                FunDebug.Log("FunapiSession.Stop - Can't find {0} transport.", ConvertString(protocol));
                return;
            }

#if !NO_UNITY
            mono.StartCoroutine(TryToStopTransport(transport));
#else
            mono.StartCoroutine(() => TryToStopTransport(transport));
#endif
        }

        public void SendMessage (MessageType msg_type, object message,
                                 EncryptionType encryption = EncryptionType.kDefaultEncryption,
                                 TransportProtocol protocol = TransportProtocol.kDefault,
                                 string expected_reply_type = null, float expected_reply_time = 0f,
                                 TimeoutEventHandler onReplyMissed = null)
        {
            string _msg_type = MessageTable.Lookup(msg_type);
            SendMessage(_msg_type, message, encryption, protocol, expected_reply_type, expected_reply_time, onReplyMissed);
        }

        public void SendMessage (MessageType msg_type, object message, TransportProtocol protocol,
                                 string expected_reply_type, float expected_reply_time, TimeoutEventHandler onReplyMissed)
        {
            string _msg_type = MessageTable.Lookup(msg_type);
            SendMessage(_msg_type, message, EncryptionType.kDefaultEncryption, protocol,
                        expected_reply_type, expected_reply_time, onReplyMissed);
        }

        public void SendMessage (string msg_type, object message, TransportProtocol protocol,
                                 string expected_reply_type, float expected_reply_time, TimeoutEventHandler onReplyMissed)
        {
            SendMessage(msg_type, message, EncryptionType.kDefaultEncryption, protocol,
                        expected_reply_type, expected_reply_time, onReplyMissed);
        }

        public void SendMessage (string msg_type, object message,
                                 EncryptionType encryption = EncryptionType.kDefaultEncryption,
                                 TransportProtocol protocol = TransportProtocol.kDefault,
                                 string expected_reply_type = null, float expected_reply_time = 0f,
                                 TimeoutEventHandler onReplyMissed = null)
        {
            if (protocol == TransportProtocol.kDefault)
                protocol = default_protocol_;

            bool reliable_transport = SessionReliability && protocol == TransportProtocol.kTcp;

            // Invalidates session id if it is too stale.
            if (last_received_.AddSeconds(kFunapiSessionTimeout) < DateTime.Now)
            {
                FunDebug.Log("Session is too stale. The server might have invalidated my session. Resetting.");
                session_id_ = "";
            }

            FunapiTransport transport = GetTransport(protocol);
            if (transport != null && transport.state == FunapiTransport.State.kEstablished &&
                (reliable_transport == false || unsent_queue_.Count <= 0))
            {
                FunapiMessage fun_msg = null;

                bool sending_sequence = transport.SequenceNumberValidation &&
                    (protocol == TransportProtocol.kTcp || protocol == TransportProtocol.kHttp);

                if (transport.Encoding == FunEncoding.kJson)
                {
                    fun_msg = new FunapiMessage(protocol, msg_type, FunapiMessage.JsonHelper.Clone(message), encryption);

                    // Encodes a messsage type
                    FunapiMessage.JsonHelper.SetStringField(fun_msg.message, kMsgTypeBodyField, msg_type);

                    // Encodes a session id, if any.
                    if (session_id_.Length > 0)
                    {
                        FunapiMessage.JsonHelper.SetStringField(fun_msg.message, kSessionIdBodyField, session_id_);
                    }

                    if (reliable_transport || sending_sequence)
                    {
                        UInt32 seq = GetNextSeq(protocol);
                        FunapiMessage.JsonHelper.SetIntegerField(fun_msg.message, kSeqNumberField, seq);

                        if (reliable_transport)
                            send_queue_.Enqueue(fun_msg);

                        FunDebug.DebugLog("{0} send message - msgtype:{1} seq:{2}", transport.str_protocol, msg_type, seq);
                    }
                    else
                    {
                        FunDebug.DebugLog("{0} send message - msgtype:{1}", transport.str_protocol, msg_type);
                    }
                }
                else if (transport.Encoding == FunEncoding.kProtobuf)
                {
                    fun_msg = new FunapiMessage(protocol, msg_type, message, encryption);

                    FunMessage pbuf = fun_msg.message as FunMessage;
                    pbuf.msgtype = msg_type;

                    // Encodes a session id, if any.
                    if (session_id_.Length > 0)
                    {
                        pbuf.sid = session_id_;
                    }

                    if (reliable_transport || sending_sequence)
                    {
                        pbuf.seq = GetNextSeq(protocol);

                        if (reliable_transport)
                            send_queue_.Enqueue(fun_msg);

                        FunDebug.DebugLog("{0} send message - msgtype:{1} seq:{2}",
                                          protocol, msg_type, pbuf.seq);
                    }
                    else
                    {
                        FunDebug.DebugLog("{0} send message - msgtype:{1}", transport.str_protocol, msg_type);
                    }
                }

                if (expected_reply_type != null && expected_reply_type.Length > 0)
                {
                    AddExpectedReply(fun_msg, expected_reply_type, expected_reply_time, onReplyMissed);
                }

                transport.SendMessage(fun_msg);
            }
            else if (transport != null &&
                     (reliable_transport || transport.state == FunapiTransport.State.kEstablished))
            {
                if (transport.Encoding == FunEncoding.kJson)
                {
                    if (transport == null)
                        unsent_queue_.Enqueue(new FunapiMessage(protocol, msg_type, message, encryption));
                    else
                        unsent_queue_.Enqueue(new FunapiMessage(protocol, msg_type,
                                                                FunapiMessage.JsonHelper.Clone(message), encryption));
                }
                else if (transport.Encoding == FunEncoding.kProtobuf)
                {
                    unsent_queue_.Enqueue(new FunapiMessage(protocol, msg_type, message, encryption));
                }

                FunDebug.Log("SendMessage - '{0}' message queued.", msg_type);
            }
            else
            {
                StringBuilder strlog = new StringBuilder();
                strlog.AppendFormat("SendMessage - '{0}' message skipped.", msg_type);
                if (transport == null)
                    strlog.AppendFormat(" There's no {0} transport.", ConvertString(protocol));
                else if (transport.state != FunapiTransport.State.kEstablished)
                    strlog.AppendFormat(" Transport's state is '{0}'.", transport.state);

                FunDebug.Log(strlog.ToString());
            }
        }


        //
        // Properties
        //
        public bool SessionReliability { private set; get; }

        public TransportProtocol DefaultProtocol
        {
            get
            {
                return default_protocol_;
            }
            set
            {
                default_protocol_ = value;
                FunDebug.Log("The default protocol is '{0}'", value);
            }
        }

        public bool Started
        {
            get
            {
                lock (state_lock_)
                {
                    return state_ != State.kUnknown && state_ != State.kStopped;
                }
            }
        }

        public bool Connected
        {
            get
            {
                lock (state_lock_)
                {
                    return state_ == State.kConnected;
                }
            }
        }


        //
        // Get/Set functions
        //
        public FunEncoding GetEncoding (TransportProtocol protocol)
        {
            FunapiTransport transport = GetTransport(protocol);
            if (transport == null)
                return FunEncoding.kNone;

            return transport.Encoding;
        }

        public bool HasTransport (TransportProtocol protocol)
        {
            lock (transports_lock_)
            {
                if (transports_.ContainsKey(protocol))
                    return true;
            }

            return false;
        }

        public FunapiTransport GetTransport (TransportProtocol protocol)
        {
            lock (transports_lock_)
            {
                if (transports_.ContainsKey(protocol))
                    return transports_[protocol];
            }

            return null;
        }


        //
        // Derived function from FunapiUpdater
        //
        protected override bool Update (float deltaTime)
        {
            if (!base.Update(deltaTime))
                return false;

            lock (transports_lock_)
            {
                if (transports_.Count > 0)
                {
                    foreach (FunapiTransport transport in transports_.Values)
                    {
                        if (transport != null)
                            transport.Update(deltaTime);
                    }
                }
            }

            lock (message_lock_)
            {
                if (message_buffer_.Count > 0)
                {
                    FunDebug.DebugLog("Update messages. count: {0}", message_buffer_.Count);

                    foreach (FunapiMessage message in message_buffer_)
                    {
                        ProcessMessage(message);
                    }

                    message_buffer_.Clear();
                }
            }

            lock (state_lock_)
            {
                if (state_ == State.kUnknown || state_ == State.kStopped)
                {
                    lock (message_lock_)
                    {
                        if (message_buffer_.Count > 0)
                            message_buffer_.Clear();
                    }

                    lock (expected_reply_lock)
                    {
                        if (expected_replies_.Count > 0)
                            expected_replies_.Clear();
                    }
                    return true;
                }
            }

            lock (expected_reply_lock)
            {
                if (expected_replies_.Count > 0)
                {
                    List<string> remove_list = new List<string>();
                    Dictionary<string, List<FunapiMessage>> exp_list = expected_replies_;
                    expected_replies_ = new Dictionary<string, List<FunapiMessage>>();

                    foreach (var item in exp_list)
                    {
                        int remove_count = 0;
                        foreach (FunapiMessage exp in item.Value)
                        {
                            exp.reply_timeout -= deltaTime;
                            if (exp.reply_timeout <= 0f)
                            {
                                FunDebug.Log("'{0}' message waiting time has been exceeded.", exp.msg_type);
                                exp.timeout_callback(exp.msg_type);
                                ++remove_count;
                            }
                        }

                        if (remove_count > 0)
                        {
                            if (item.Value.Count <= remove_count)
                                remove_list.Add(item.Key);
                            else
                                item.Value.RemoveRange(0, remove_count);
                        }
                    }

                    if (remove_list.Count > 0)
                    {
                        foreach (string key in remove_list)
                        {
                            exp_list.Remove(key);
                        }
                    }

                    if (exp_list.Count > 0)
                    {
                        Dictionary<string, List<FunapiMessage>> added_list = expected_replies_;
                        expected_replies_ = exp_list;

                        if (added_list.Count > 0)
                        {
                            foreach (var item in added_list)
                            {
                                if (expected_replies_.ContainsKey(item.Key))
                                    expected_replies_[item.Key].AddRange(item.Value);
                                else
                                    expected_replies_.Add(item.Key, item.Value);
                            }
                            added_list = null;
                        }
                    }
                }
            }

            return true;
        }

        protected override void OnQuit ()
        {
            StopSession(true);
        }


        // Convert to protocol string
        string ConvertString (TransportProtocol protocol)
        {
            if (protocol == TransportProtocol.kTcp)
                return "TCP";
            else if (protocol == TransportProtocol.kUdp)
                return "UDP";
            else if (protocol == TransportProtocol.kHttp)
                return "HTTP";

            return "";
        }


        //
        // Session-related functions
        //
        void InitSession()
        {
            session_id_ = "";

            if (SessionReliability)
            {
                seq_recvd_ = 0;
                send_queue_.Clear();
                first_receiving_ = true;
            }

            tcp_seq_ = (UInt32)rnd_.Next() + (UInt32)rnd_.Next();
            http_seq_ = (UInt32)rnd_.Next() + (UInt32)rnd_.Next();
        }

        UInt32 GetNextSeq (TransportProtocol protocol)
        {
            if (protocol == TransportProtocol.kTcp) {
                return ++tcp_seq_;
            }
            else if (protocol == TransportProtocol.kHttp) {
                return ++http_seq_;
            }

            FunDebug.Assert(false);
            return 0;
        }

        void PrepareSession (string session_id)
        {
            if (session_id_.Length == 0)
            {
                FunDebug.Log("New session id: {0}", session_id);
                OpenSession(session_id);

                if (SessionEventCallback != null)
                    SessionEventCallback(SessionEventType.kOpened, session_id_);
            }

            if (session_id_ != session_id)
            {
                FunDebug.Log("Session id changed: {0} => {1}", session_id_, session_id);

                CloseSession();
                OpenSession(session_id);

                if (SessionEventCallback != null)
                    SessionEventCallback(SessionEventType.kChanged, session_id_);
            }
        }

        void OpenSession (string session_id)
        {
            FunDebug.Assert(session_id_.Length == 0);

            lock (state_lock_)
            {
                state_ = State.kConnected;
            }

            session_id_ = session_id;
            first_receiving_ = true;

            lock (transports_lock_)
            {
                foreach (FunapiTransport transport in transports_.Values)
                {
                    transport.session_id_ = session_id;

                    if (transport.state == FunapiTransport.State.kWaitForSession)
                    {
                        SetTransportStarted(transport, false);
                    }
                }
            }

            if (unsent_queue_.Count > 0)
            {
                SendUnsentMessages();
            }
        }

        void CloseSession ()
        {
            lock (state_lock_)
            {
                state_ = State.kUnknown;
            }

            if (session_id_.Length == 0)
                return;

            if (SessionEventCallback != null)
                SessionEventCallback(SessionEventType.kClosed, session_id_);

            InitSession();
        }

        void StopSession (bool force_stop = false)
        {
            FunDebug.Log("Stopping a network module.");

            if (force_stop)
            {
                // Stops all transport
                lock (transports_lock_)
                {
                    foreach (FunapiTransport transport in transports_.Values)
                    {
                        StopTransport(transport);
                    }
                }
            }
            else
            {
                lock (transports_lock_)
                {
                    foreach (FunapiTransport transport in transports_.Values)
                    {
#if !NO_UNITY
                        mono.StartCoroutine(TryToStopTransport(transport));
#else
                        mono.StartCoroutine(() => TryToStopTransport(transport));
#endif
                    }
                }
            }
        }

#if !NO_UNITY
        private IEnumerator TryToStopTransport (FunapiTransport transport)
#else
        private void TryToStopTransport (FunapiTransport transport)
#endif
        {
            if (transport == null)
                yield return null;

            // Checks transport's state.
            if (!transport.CheckForStop())
            {
                lock (state_lock_)
                {
                    FunDebug.Log("{0} Stop waiting... ({1})", transport.str_protocol,
                                 transport.HasUnsentMessages ? "sending" : "0");
                    yield return new WaitForSeconds(0.1f);
                }
            }

            StopTransport(transport);
        }


        //
        // Transport-related functions
        //
        FunapiTransport CreateTransport (TransportProtocol protocol, FunEncoding encoding,
                                         UInt16 port, TransportOption option)
        {
            FunapiTransport transport = null;
            if (protocol == TransportProtocol.kTcp)
            {
                FunapiTcpTransport tcp_transport = new FunapiTcpTransport(server_address_, port, encoding);
                transport = tcp_transport;

                TcpTransportOption tcp_option = option as TcpTransportOption;
                tcp_transport.AutoReconnect = tcp_option.AutoReconnect;
                tcp_transport.DisableNagle = tcp_option.DisableNagle;
                tcp_transport.EnablePing = tcp_option.EnablePing;
            }
            else if (protocol == TransportProtocol.kUdp)
            {
                transport = new FunapiUdpTransport(server_address_, port, encoding);
            }
            else if (protocol == TransportProtocol.kHttp)
            {
                FunapiHttpTransport http_transport = new FunapiHttpTransport(server_address_, port, false, encoding);
                transport = http_transport;

                HttpTransportOption http_option = option as HttpTransportOption;
                http_transport.UseWWW = http_option.UseWWW;
            }
            else
            {
                FunDebug.LogError("Create a {0} transport failed.", ConvertString(protocol));
                return null;
            }

            transport.ConnectTimeout = option.ConnectTimeout;
            transport.SequenceNumberValidation = option.SequenceValidation;

            if (option.EncType != EncryptionType.kDefaultEncryption)
                transport.SetEncryption(option.EncType);

            if (option.ExtraAddress != null)
                transport.AddServerList(option.ExtraAddress);

            lock (transports_lock_)
            {
                if (transports_.ContainsKey(transport.Protocol))
                {
                    StringBuilder strlog = new StringBuilder();
                    strlog.AppendFormat("AttachTransport - {0} transport already exists. ", transport.str_protocol);
                    strlog.Append("You should call DetachTransport first.");
                    FunDebug.LogWarning(strlog.ToString());
                    return null;
                }

                // Callback functions
                transport.ConnectTimeoutCallback += OnConnectTimeout;
                transport.DisconnectedCallback += OnTransportDisconnected;
                transport.ConnectFailureCallback += OnTransportConnectFailure;
                transport.MessageFailureCallback += OnTransportMessageFailure;
                transport.FailureCallback += OnTransportFailure;

                transport.StartedInternalCallback += OnTransportStarted;
                transport.StoppedCallback += OnTransportStopped;
                transport.ReceivedCallback += OnTransportReceived;

                transports_[transport.Protocol] = transport;

                if (default_protocol_ == TransportProtocol.kDefault)
                {
                    default_protocol_ = transport.Protocol;
                    FunDebug.Log("The default protocol is '{0}'", transport.str_protocol);
                }

                FunDebug.DebugLog("{0} transport added.", transport.str_protocol);
            }

            return transport;
        }

        void StartTransport (FunapiTransport transport)
        {
            if (transport == null)
                return;

            FunDebug.Log("Starting {0} transport.", transport.str_protocol);

            if (transport.Protocol == TransportProtocol.kHttp)
            {
                ((FunapiHttpTransport)transport).mono = mono;
            }

            transport.Start();
        }

        void StopTransport (FunapiTransport transport)
        {
            if (transport == null || transport.state == FunapiTransport.State.kUnknown)
                return;

            FunDebug.Log("Stopping {0} transport.", transport.str_protocol);

            transport.Stop();
        }

        void OnTransportEvent (TransportProtocol protocol, TransportEventType type)
        {
            if (TransportEventCallback != null)
                TransportEventCallback(protocol, type);
        }

        void SetTransportStarted (FunapiTransport transport, bool send_unsent = true)
        {
            if (transport == null)
                return;

            transport.OnStarted();

            OnTransportEvent(transport.Protocol, TransportEventType.kStarted);

            if (send_unsent && unsent_queue_.Count > 0)
            {
                SendUnsentMessages();
            }
        }

        void CheckTransportConnection (TransportProtocol protocol)
        {
            lock (state_lock_)
            {
                if (state_ == State.kStopped)
                    return;

                if (state_ == State.kWaitForSession && protocol == session_protocol_)
                {
                    FunapiTransport other = FindOtherTransport(protocol);
                    if (other != null)
                    {
                        other.state = FunapiTransport.State.kWaitForSession;
                        SendEmptyMessage(other.Protocol);
                    }
                    else
                    {
                        state_ = State.kStarted;
                    }
                }

                lock (transports_lock_)
                {
                    bool all_stopped = true;
                    foreach (FunapiTransport t in transports_.Values)
                    {
                        if (t.IsReconnecting || t.Started)
                        {
                            all_stopped = false;
                            break;
                        }
                    }

                    if (all_stopped)
                    {
                        ReleaseUpdater();

                        lock (state_lock_)
                        {
                            if (SessionReliability)
                                state_ = State.kStopped;
                            else
                                state_ = State.kUnknown;
                        }
                    }
                }
            }
        }

        FunapiTransport FindOtherTransport (TransportProtocol protocol)
        {
            lock (transports_lock_)
            {
                if (protocol == TransportProtocol.kDefault || transports_.Count <= 0)
                    return null;

                foreach (FunapiTransport transport in transports_.Values)
                {
                    if (transport.Protocol != protocol && transport.Started)
                    {
                        return transport;
                    }
                }
            }

            return null;
        }


        //
        // Transport-related callback functions
        //
        void OnTransportConnectFailure (TransportProtocol protocol)
        {
            FunDebug.Log("{0} transport connect failed.", ConvertString(protocol));

            CheckTransportConnection(protocol);
            OnTransportEvent(protocol, TransportEventType.kConnectFailed);
        }

        void OnConnectTimeout (TransportProtocol protocol)
        {
            StopTransport(GetTransport(protocol));

            OnTransportEvent(protocol, TransportEventType.kConnectTimedout);
        }

        void OnTransportDisconnected (TransportProtocol protocol)
        {
            FunDebug.Log("{0} transport disconnected.", ConvertString(protocol));

            CheckTransportConnection(protocol);
            OnTransportEvent(protocol, TransportEventType.kDisconnected);
        }

        void OnTransportStarted (TransportProtocol protocol)
        {
            FunapiTransport transport = GetTransport(protocol);
            FunDebug.Assert(transport != null);
            FunDebug.Log("{0} transport started.", transport.str_protocol);

            lock (state_lock_)
            {
                if (session_id_.Length > 0)
                {
                    state_ = State.kConnected;

                    if (SessionReliability && protocol == TransportProtocol.kTcp && seq_recvd_ != 0)
                    {
                        transport.state = FunapiTransport.State.kWaitForAck;
                        SendAck(transport, seq_recvd_ + 1);
                    }
                    else
                    {
                        SetTransportStarted(transport);
                    }
                }
                else if (state_ == State.kStarted || state_ == State.kStopped)
                {
                    state_ = State.kWaitForSession;
                    transport.state = FunapiTransport.State.kWaitForSession;

                    // To get a session id
                    SendEmptyMessage(protocol);
                }
                else if (state_ == State.kWaitForSession)
                {
                    transport.state = FunapiTransport.State.kWaitForSession;
                }
            }
        }

        void OnTransportStopped (TransportProtocol protocol)
        {
            FunapiTransport transport = GetTransport(protocol);
            FunDebug.Assert(transport != null);
            FunDebug.Log("{0} transport stopped.", transport.str_protocol);

            CheckTransportConnection(protocol);
            OnTransportEvent(protocol, TransportEventType.kStopped);
        }

        void OnTransportMessageFailure (TransportProtocol protocol, FunapiMessage fun_msg)
        {
            if (fun_msg == null || fun_msg.reply_type.Length <= 0)
                return;

            DeleteExpectedReply(fun_msg.reply_type);
        }

        void OnTransportFailure (TransportProtocol protocol)
        {
            FunapiTransport transport = GetTransport(protocol);
            FunDebug.Assert(transport != null);
            FunDebug.Log("{0} transport error has occurred.", transport.str_protocol);
        }


        //
        // Sending-related functions
        //
        void SendEmptyMessage (TransportProtocol protocol)
        {
            FunapiTransport transport = GetTransport(protocol);
            if (transport == null)
            {
                FunDebug.Log("SendEmptyMessage - transport is null.");
                return;
            }

            session_protocol_ = protocol;
            FunDebug.DebugLog("{0} send empty message", transport.str_protocol);

            if (transport.Encoding == FunEncoding.kJson)
            {
                object msg = FunapiMessage.Deserialize("{}");
                transport.SendMessage(new FunapiMessage(transport.Protocol, "", msg));
            }
            else if (transport.Encoding == FunEncoding.kProtobuf)
            {
                FunMessage msg = new FunMessage();
                transport.SendMessage(new FunapiMessage(transport.Protocol, "", msg));
            }
        }

        void SendAck (FunapiTransport transport, UInt32 ack)
        {
            FunDebug.Assert(SessionReliability);
            if (transport == null)
            {
                FunDebug.Log("SendAck - transport is null.");
                return;
            }

            if (state_ != State.kConnected)
                return;

            FunDebug.DebugLog("{0} send ack message - ack:{1}", transport.str_protocol, ack);

            if (transport.Encoding == FunEncoding.kJson)
            {
                object ack_msg = FunapiMessage.Deserialize("{}");
                FunapiMessage.JsonHelper.SetStringField(ack_msg, kSessionIdBodyField, session_id_);
                FunapiMessage.JsonHelper.SetIntegerField(ack_msg, kAckNumberField, ack);
                transport.SendMessage(new FunapiMessage(transport.Protocol, "", ack_msg));
            }
            else if (transport.Encoding == FunEncoding.kProtobuf)
            {
                FunMessage ack_msg = new FunMessage();
                ack_msg.sid = session_id_;
                ack_msg.ack = ack;
                transport.SendMessage(new FunapiMessage(transport.Protocol, "", ack_msg));
            }
        }

        void SendUnsentMessages()
        {
            if (unsent_queue_.Count <= 0)
                return;

            FunDebug.Log("SendUnsentMessages - {0} unsent messages.", unsent_queue_.Count);

            foreach (FunapiMessage msg in unsent_queue_)
            {
                FunapiTransport transport = GetTransport(msg.protocol);
                if (transport == null || transport.state != FunapiTransport.State.kEstablished)
                {
                    FunDebug.Log("SendUnsentMessages - {0} isn't a valid transport. Message skipped.", msg.protocol);
                    continue;
                }

                bool reliable_transport = SessionReliability && transport.Protocol == TransportProtocol.kTcp;
                bool sending_sequence = transport.SequenceNumberValidation &&
                    (transport.Protocol == TransportProtocol.kTcp || transport.Protocol == TransportProtocol.kHttp);

                if (transport.Encoding == FunEncoding.kJson)
                {
                    object json = msg.message;

                    // Encodes a messsage type
                    FunapiMessage.JsonHelper.SetStringField(json, kMsgTypeBodyField, msg.msg_type);

                    if (session_id_.Length > 0)
                        FunapiMessage.JsonHelper.SetStringField(json, kSessionIdBodyField, session_id_);

                    if (reliable_transport || sending_sequence)
                    {
                        UInt32 seq = GetNextSeq(transport.Protocol);
                        FunapiMessage.JsonHelper.SetIntegerField(json, kSeqNumberField, seq);

                        if (reliable_transport)
                            send_queue_.Enqueue(msg);

                        FunDebug.Log("{0} send unsent message - msgtype:{1} seq:{2}",
                                     transport.Protocol, msg.msg_type, seq);
                    }
                    else
                    {
                        FunDebug.Log("{0} send unsent message - msgtype:{1}",
                                     transport.Protocol, msg.msg_type);
                    }
                }
                else if (transport.Encoding == FunEncoding.kProtobuf)
                {
                    FunMessage pbuf = msg.message as FunMessage;
                    pbuf.msgtype = msg.msg_type;

                    if (session_id_.Length > 0)
                        pbuf.sid = session_id_;

                    if (reliable_transport || sending_sequence)
                    {
                        pbuf.seq = GetNextSeq(transport.Protocol);

                        if (reliable_transport)
                            send_queue_.Enqueue(msg);

                        FunDebug.Log("{0} send unsent message - msgtype:{1} seq:{2}",
                                     transport.Protocol, msg.msg_type, pbuf.seq);
                    }
                    else
                    {
                        FunDebug.Log("{0} send unsent message - msgtype:{1}",
                                     transport.Protocol, msg.msg_type);
                    }
                }

                if (msg.reply_type != null && msg.reply_type.Length > 0)
                {
                    AddExpectedReply(msg, msg.reply_type, msg.reply_timeout, msg.timeout_callback);
                }

                transport.SendMessage(msg);
            }

            unsent_queue_.Clear();
        }


        //
        // Receiving-related functions
        //
        void OnTransportReceived (FunapiMessage message)
        {
            FunDebug.DebugLog("OnTransportReceived invoked.");
            last_received_ = DateTime.Now;

            lock (message_lock_)
            {
                message_buffer_.Add(message);
            }
        }

        void OnProcessMessage (string msg_type, object message)
        {
            if (msg_type == kNewSessionMessageType)
            {
            }
            else if (msg_type == kSessionClosedMessageType)
            {
                FunDebug.Log("Session timed out. Resetting session id.");
                StopSession();
                CloseSession();
            }
            else
            {
                if (ReceivedMessageCallback != null)
                    ReceivedMessageCallback(msg_type, message);
            }
        }

        void ProcessMessage (FunapiMessage msg)
        {
            FunapiTransport transport = GetTransport(msg.protocol);
            if (transport == null)
                return;

            object message = msg.message;
            if (message == null)
            {
                FunDebug.Log("ProcessMessage - '{0}' message is null.", msg.msg_type);
                return;
            }

            string msg_type = msg.msg_type;
            string session_id = "";

            if (transport.Encoding == FunEncoding.kJson)
            {
                try
                {
                    FunDebug.Assert(FunapiMessage.JsonHelper.GetStringField(message, kSessionIdBodyField) is string);
                    string session_id_node = FunapiMessage.JsonHelper.GetStringField(message, kSessionIdBodyField) as string;
                    session_id = session_id_node;
                    FunapiMessage.JsonHelper.RemoveStringField(message, kSessionIdBodyField);

                    PrepareSession(session_id);

                    if (SessionReliability && msg.protocol == TransportProtocol.kTcp)
                    {
                        if (FunapiMessage.JsonHelper.HasField(message, kAckNumberField))
                        {
                            UInt32 ack = (UInt32)FunapiMessage.JsonHelper.GetIntegerField(message, kAckNumberField);
                            OnAckReceived(transport, ack);
                            // Does not support piggybacking.
                            FunDebug.Assert(!FunapiMessage.JsonHelper.HasField(message, kMsgTypeBodyField));
                            return;
                        }

                        if (FunapiMessage.JsonHelper.HasField(message, kSeqNumberField))
                        {
                            UInt32 seq = (UInt32)FunapiMessage.JsonHelper.GetIntegerField(message, kSeqNumberField);
                            if (!OnSeqReceived(transport, seq))
                                return;
                            FunapiMessage.JsonHelper.RemoveStringField(message, kSeqNumberField);
                        }
                    }
                }
                catch (Exception e)
                {
                    FunDebug.Log("Failure in ProcessMessage: {0}", e.ToString());
                    StopTransport(transport);
                    return;
                }

                if (msg_type.Length > 0)
                {
                    DeleteExpectedReply(msg_type);
                    OnProcessMessage(msg_type, message);
                }
            }
            else if (transport.Encoding == FunEncoding.kProtobuf)
            {
                FunMessage funmsg = message as FunMessage;

                try
                {
                    session_id = funmsg.sid;
                    PrepareSession(session_id);

                    if (SessionReliability && msg.protocol == TransportProtocol.kTcp)
                    {
                        if (funmsg.ackSpecified)
                        {
                            OnAckReceived(transport, funmsg.ack);
                            // Does not support piggybacking.
                            return;
                        }

                        if (funmsg.seqSpecified)
                        {
                            if (!OnSeqReceived(transport, funmsg.seq))
                                return;
                        }
                    }
                }
                catch (Exception e)
                {
                    FunDebug.Log("Failure in ProcessMessage: {0}", e.ToString());
                    StopTransport(transport);
                    return;
                }

                if (msg_type.Length > 0)
                {
                    DeleteExpectedReply(msg_type);
                    OnProcessMessage(msg_type, funmsg);
                }
            }
            else
            {
                FunDebug.Log("Invalid message type. type: {0}", transport.Encoding);
                FunDebug.Assert(false);
                return;
            }

            if (transport.state == FunapiTransport.State.kWaitForAck && session_id_.Length > 0)
            {
                SetTransportStarted(transport);
            }
        }

        void AddExpectedReply (FunapiMessage fun_msg, string reply_type,
                               float reply_time, TimeoutEventHandler onReplyMissed)
        {
            lock (expected_reply_lock)
            {
                if (!expected_replies_.ContainsKey(reply_type))
                {
                    expected_replies_[reply_type] = new List<FunapiMessage>();
                }

                fun_msg.SetReply(reply_type, reply_time, onReplyMissed);
                expected_replies_[reply_type].Add(fun_msg);
                FunDebug.Log("Adds expected reply message - {0} > {1} ({2})",
                             fun_msg.msg_type, reply_type, reply_time);
            }
        }

        void DeleteExpectedReply (string reply_type)
        {
            lock (expected_reply_lock)
            {
                if (expected_replies_.ContainsKey(reply_type))
                {
                    List<FunapiMessage> list = expected_replies_[reply_type];
                    if (list.Count > 0)
                    {
                        list.RemoveAt(0);
                        FunDebug.Log("Deletes expected reply message - {0}", reply_type);
                    }

                    if (list.Count <= 0)
                        expected_replies_.Remove(reply_type);
                }
            }
        }

        //
        // Serial-number-related callback functions
        //
        bool OnSeqReceived (FunapiTransport transport, UInt32 seq)
        {
            if (transport == null)
            {
                FunDebug.LogWarning("OnSeqReceived - transport is null.");
                return false;
            }

            if (first_receiving_)
            {
                first_receiving_ = false;
            }
            else
            {
                if (!SeqLess(seq_recvd_, seq))
                {
                    FunDebug.Log("Last sequence number is {0} but {1} received. Skipping message.", seq_recvd_, seq);
                    return false;
                }
                else if (seq != seq_recvd_ + 1)
                {
                    FunDebug.LogError("Received wrong sequence number {0}. {1} expected.", seq, seq_recvd_ + 1);
                    StopTransport(transport);
                    return false;
                }
            }

            seq_recvd_ = seq;
            SendAck(transport, seq_recvd_ + 1);

            return true;
        }

        void OnAckReceived (FunapiTransport transport, UInt32 ack)
        {
            if (transport == null)
            {
                FunDebug.LogWarning("OnAckReceived - transport is null.");
                return;
            }

            if (state_ != State.kConnected)
                return;

            FunDebug.DebugLog("received ack message - ack:{0}", ack);

            UInt32 seq = 0;
            while (send_queue_.Count > 0)
            {
                FunapiMessage last_msg = send_queue_.Peek();
                if (transport.Encoding == FunEncoding.kJson)
                {
                    seq = (UInt32)FunapiMessage.JsonHelper.GetIntegerField(last_msg.message, kSeqNumberField);
                }
                else if (transport.Encoding == FunEncoding.kProtobuf)
                {
                    seq = (last_msg.message as FunMessage).seq;
                }
                else
                {
                    FunDebug.Assert(false);
                    seq = 0;
                }

                if (SeqLess(seq, ack))
                {
                    send_queue_.Dequeue();
                }
                else
                {
                    break;
                }
            }

            if (transport.state == FunapiTransport.State.kWaitForAck)
            {
                if (send_queue_.Count > 0)
                {
                    foreach (FunapiMessage msg in send_queue_)
                    {
                        if (transport.Encoding == FunEncoding.kJson)
                        {
                            seq = (UInt32)FunapiMessage.JsonHelper.GetIntegerField(msg.message, kSeqNumberField);
                        }
                        else if (transport.Encoding == FunEncoding.kProtobuf)
                        {
                            seq = (msg.message as FunMessage).seq;
                        }
                        else
                        {
                            FunDebug.Assert(false);
                            seq = 0;
                        }

                        if (seq == ack || SeqLess(ack, seq))
                        {
                            transport.SendMessage(msg);
                        }
                        else
                        {
                            FunDebug.LogWarning("OnAckReceived({0}) - wrong sequence number {1}. ", ack, seq);
                        }
                    }

                    FunDebug.Log("Resend {0} messages.", send_queue_.Count);
                }

                SetTransportStarted(transport);
            }
        }

        // Serial-number arithmetic
        bool SeqLess (UInt32 x, UInt32 y)
        {
            // 아래 참고
            //  - http://en.wikipedia.org/wiki/Serial_number_arithmetic
            //  - RFC 1982
            return (Int32)(y - x) > 0;
        }


        // Delegates
        public delegate void SessionEventHandler (SessionEventType type, string session_id);
        public delegate void TransportEventHandler (TransportProtocol protocol, TransportEventType type);
        public delegate void ReceivedMessageHandler (string msg_type, object message);

        // Funapi message-related events.
        public event SessionEventHandler SessionEventCallback;
        public event TransportEventHandler TransportEventCallback;
        public event ReceivedMessageHandler ReceivedMessageCallback;

        // Message-type-related constants.
        const float kFunapiSessionTimeout = 3600.0f;
        const string kMsgTypeBodyField = "_msgtype";
        const string kSessionIdBodyField = "_sid";
        const string kSeqNumberField = "_seq";
        const string kAckNumberField = "_ack";
        const string kNewSessionMessageType = "_session_opened";
        const string kSessionClosedMessageType = "_session_closed";

        enum State
        {
            kUnknown = 0,
            kStarted,
            kConnected,
            kWaitForSession,
            kStopped
        };

        State state_;
        object state_lock_ = new object();
        string server_address_ = "";
        System.Random rnd_ = new System.Random();

        // Session-related variables.
        string session_id_ = "";
        TransportProtocol session_protocol_;

        // Serial-number-related variables.
        UInt32 seq_recvd_ = 0;
        UInt32 tcp_seq_ = 0;
        UInt32 http_seq_ = 0;
        bool first_receiving_ = false;

        // Transport-related variables.
        object transports_lock_ = new object();
        TransportProtocol default_protocol_ = TransportProtocol.kDefault;
        Dictionary<TransportProtocol, FunapiTransport> transports_ = new Dictionary<TransportProtocol, FunapiTransport>();

        // Message-related variables.
        FunMessageSerializer serializer_;
        object message_lock_ = new object();
        object expected_reply_lock = new object();
        DateTime last_received_ = DateTime.Now;
        Queue<FunapiMessage> send_queue_ = new Queue<FunapiMessage>();
        Queue<FunapiMessage> unsent_queue_ = new Queue<FunapiMessage>();
        Dictionary<string, List<FunapiMessage>> expected_replies_ = new Dictionary<string, List<FunapiMessage>>();
        List<FunapiMessage> message_buffer_ = new List<FunapiMessage>();
    }
}

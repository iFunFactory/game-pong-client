// PLEASE ADD YOUR EVENT HANDLER DECLARATIONS HERE.

#include "event_handlers.h"

#include <funapi.h>
#include <glog/logging.h>

#include "pong_loggers.h"
#include "pong_messages.pb.h"


namespace pong {

////////////////////////////////////////////////////////////////////////////////
// Session open/close handlers
////////////////////////////////////////////////////////////////////////////////

	void OnSessionOpened(const Ptr<Session> &session) {
		logger::SessionOpened(to_string(session->id()), WallClock::Now());
	}

	void OnSessionClosed(const Ptr<Session> &session, SessionCloseReason reason) {
		logger::SessionClosed(to_string(session->id()), WallClock::Now());

		if (reason == kClosedForServerDid) {
		  // Server has called session->Close().
		}
		else if (reason == kClosedForIdle) {
		  // The session has been idle for long time.
		}
		else if (reason == kClosedForUnknownSessionId) {
		  // The session was invalid.
		}

		LOG(INFO) << "OnSessionClosed : " + to_string(session->id());
	}
	
	void MatchingCancelledByTransportDetaching(const string &id, MatchmakingClient::CancelResult result) {
		LOG(INFO) << "MatchingCancelledByTransportDetaching : " + id;
	}

	void OnTransportTcpDetached(const Ptr<Session> &session) {
		LOG(INFO) << "OnTransportTcpDetached : " + to_string(session->id())+" : " + AccountManager::FindLocalAccount(session);
		string opponentId;
		session->GetFromContext("opponent", &opponentId);
		if (!opponentId.empty())
		{
			LOG(INFO) << "opponentId : " << opponentId;
			Ptr<Session> opponentSession = AccountManager::FindLocalSession(opponentId);
			if (opponentSession && opponentSession->IsTransportAttached())
			{
				Json message;
				message["result"] = "win";
				opponentSession->SendMessage("result", message, kDefaultEncryption, kTcp);
			}
		}
		string matchingContext;
		session->GetFromContext("matching", &matchingContext);
		if (!matchingContext.empty() && matchingContext == "doing")
		{
			MatchmakingClient::CancelMatchmaking(0, AccountManager::FindLocalAccount(session), MatchingCancelledByTransportDetaching);
		}
		AccountManager::SetLoggedOut(AccountManager::FindLocalAccount(session));
		session->Close();
	}

	////////////////////////////////////////////////////////////////////////////////
	// Client message handlers.
	//
	// (Just for your reference. Please replace with your own.)
	////////////////////////////////////////////////////////////////////////////////

	void OnAccountLogin(const Ptr<Session> &session, const Json &message) {
		string id = message["id"].GetString();
		Json response;
		if (AccountManager::CheckAndSetLoggedIn(id, session))
		{
			logger::PlayerLoggedIn(to_string(session->id()), id, WallClock::Now());
			response["result"] = "ok";
			LOG(INFO) << "login success : " + id;
		}
		else
		{
			response["result"] = "nop";
			LOG(INFO) << "login failed! : " + id;
			// 일단은 그냥 기존 접속을 끊어준다
			AccountManager::SetLoggedOut(id);
		}
		session->SendMessage("login", response);
	}
	
	// 매치가 성사되면 호출됩니다.
	void OnMatched(const string &id, const MatchmakingClient::Match &match, MatchmakingClient::MatchResult result)
	{
		Ptr<Session> session = AccountManager::FindLocalSession(id);
		if (!session)
			return;
		Json response;
		if (result == MatchmakingClient::kMRSuccess) {
			response["result"] = "Success";
			response["A"] = match.context["A"];
			response["B"] = match.context["B"];
			if (match.context["A"].GetString().compare(id) == 0)
				session->AddToContext("opponent", match.context["B"].GetString());
			else
				session->AddToContext("opponent", match.context["A"].GetString());
			session->AddToContext("matching", "done");
			session->AddToContext("ready", 0);
		}
		else if (result == MatchmakingClient::kMRAlreadyRequested) {
			// 이미 Matchmaking 요청을 했습니다.
			response["result"] = "AlreadyRequested";
			session->AddToContext("matching", "failed");
		}
		else if (result == MatchmakingClient::kMRTimeout) {
		  // 지정된 시간안에 Match 가 성사되지 않았습니다.
			response["result"] = "Timeout";
			session->AddToContext("matching", "failed");
		}
		else {
		  // 오류가 발생 하였습니다. 로그를 참고 합니다.
			response["result"] = "Error";
			session->AddToContext("matching", "failed");
		}
		
		LOG(INFO) << "OnMatched : " + response["result"].ToString() + " : " + match.context.ToString();
		session->SendMessage("match", response, kDefaultEncryption, kTcp);
	}
	
	void MatchingCancelledByClientTimeout(const string &id, MatchmakingClient::CancelResult result) {
		LOG(INFO) << "MatchingCancelledByClientTimeout : " + id;
	
		Ptr<Session> session = AccountManager::FindLocalSession(id);
		if (!session) {
			LOG(INFO) << "MatchingCancelledByClientTimeout : Session is NULL!!!!!!";
			return;
		}
		Json response;
		response["result"] = "Timeout";
		session->SendMessage("match", response, kDefaultEncryption, kTcp);
	}

	void OnMatchmakingRequested(const Ptr<Session> &session, const Json &message) {
		Json context;
		context["dummy"] = 1;
		session->AddToContext("matching", "doing");
		// timeout
		WallClock::Duration timeout = WallClock::FromMsec(10 * 1000);
		MatchmakingClient::StartMatchmaking(0, AccountManager::FindLocalAccount(session), context, OnMatched, MatchmakingClient::kNullProgressCallback, timeout);
		
		// 매치메이킹이 알 수 없는 이유로 지나치게 오래 걸리는 경우를 대비해야한
		WallClock::Duration clientTimeout = timeout + WallClock::FromMsec(5 * 1000);
		Timer::ExpireAfter(clientTimeout,
			[session](const Timer::Id &timer_id, const WallClock::Value &clock) {
				if (!session->IsTransportAttached())
					return;
				string matchingState;
				session->GetFromContext("matching", &matchingState);
				// 매치메이킹이 아직 진행중인 경우
				if (matchingState == "doing") {
					// 매치메이킹 취소
					MatchmakingClient::CancelMatchmaking(0, AccountManager::FindLocalAccount(session), MatchingCancelledByClientTimeout);
				}
			});
	}

	// 매치가 취소되면 호출됩니다.
	void OnCancelled(const string &id, MatchmakingClient::CancelResult result) {
		Ptr<Session> session = AccountManager::FindLocalSession(id);
		if (!session)
			return;
		Json response;
		if (result == MatchmakingClient::kCRNoRequest) {
			// Matchmaking 을 요청이 없었습니다. (취소할 Matchmaking 이 없습니다)
			response["result"] = "NoRequest";
			session->AddToContext("matching", "failed");
		}
		else if (result == MatchmakingClient::kCRError) {
			// 오류가 발생 하였습니다. 로그를 참고 합니다.
			response["result"] = "Error";
			session->AddToContext("matching", "failed");
		}
		else
		{
			// Matchmaking 요청이 취소되었습니다.
			response["result"] = "Cancel";
			session->AddToContext("matching", "cancelled");
		}

		// Matchmaking 요청을 취소 했습니다.
		LOG(INFO) << "match cancelled : " + id;
		// 클라이언트에 응답을 보내는 작업 등의 후속처리를 합니다.
		session->SendMessage("match", response, kDefaultEncryption, kTcp);
	}

	// 매치 취소를 요청하는 핸들러입니다.
	// 클라이언트로부터 매치 취소 요청 메시지가 오면 이 핸들러가 호출된다고
	// 가정하겠습니다.
	void OnCancelRequested(const Ptr<Session> &session, const Json &message) {
		session->AddToContext("matching", "cancel");
		// matchmaking 취소를 요청합니다.
		// matchmaking 취소 처리가 완료되면 OnCancelled 함수가 호출됩니다.
		MatchmakingClient::CancelMatchmaking(0, AccountManager::FindLocalAccount(session), OnCancelled);
	}

	// 매칭 성공 후, 게임을 플레이할 준비가 되면 클라이언트는 ready를 보냅니다.
	void OnReadySignal(const Ptr<Session> &session, const Json &message) {
		session->AddToContext("ready", 1);
		string opponentId;
		session->GetFromContext("opponent", &opponentId);
		Ptr<Session> opponentSession = AccountManager::FindLocalSession(opponentId);
		if (opponentSession && opponentSession->IsTransportAttached()) {
			int64_t is_opponent_ready = 0;
			opponentSession->GetFromContext("ready", &is_opponent_ready);
			if (is_opponent_ready == 1)
			{
				Json response;
				response["result"] = "ok";
				session->SendMessage("start", response);
				opponentSession->SendMessage("start", response);
			}
		}
		else {
			// 상대가 접속을 종료했음
			Json response;
			response["result"] = "opponent disconnected";
			session->SendMessage("match", response, kDefaultEncryption, kTcp);
			return;
		}
	}

	void OnRelayRequested(const Ptr<Session> &session, const Json &message) {
		string opponentId;
		session->GetFromContext("opponent", &opponentId);
		Ptr<Session> opponentSession = AccountManager::FindLocalSession(opponentId);
		if (opponentSession && opponentSession->IsTransportAttached())
			opponentSession->SendMessage("relay", message);
	}
	
	void OnResultRequested(const Ptr<Session> &session, const Json &message) {
		// 패배한 쪽만 result를 보낸다.
		// 상대방에게 이겼다는 메세지를 보낸다.
		string opponentId;
		session->GetFromContext("opponent", &opponentId);
		Ptr<Session> opponentSession = AccountManager::FindLocalSession(opponentId);
		if (opponentSession && opponentSession->IsTransportAttached()) {
			Json winMessage;
			winMessage["result"] = "win";
			opponentSession->SendMessage("result", winMessage);
		}
		// 패배 확인 메세지를 보낸다.
		session->SendMessage("result", message);
	}

	////////////////////////////////////////////////////////////////////////////////
	// Extend the function below with your handlers.
	////////////////////////////////////////////////////////////////////////////////

	void RegisterEventHandlers() {
	  /*
	   * Registers handlers for session close/open events.
	   */
		{
			HandlerRegistry::Install2(OnSessionOpened, OnSessionClosed);
			HandlerRegistry::RegisterTcpTransportDetachedHandler(OnTransportTcpDetached);
		}


		  /*
		   * Registers handlers for messages from the client.
		   *
		   * Handlers below are just for you reference.
		   * Feel free to delete them and replace with your own.
		   */
		{
		  // 1. Registering a JSON message named "login" with its JSON schema.
		  //    With json schema, Engine validates input messages in JSON.
		  //    before entering a handler.
		  //    You can specify a JSON schema like below, or you can also use
		  //    auxiliary files in src/json_protocols directory.
			JsonSchema login_msg(JsonSchema::kObject,
				JsonSchema("id", JsonSchema::kString, true));
			HandlerRegistry::Register("login", OnAccountLogin, login_msg);
			HandlerRegistry::Register("match", OnMatchmakingRequested);
			HandlerRegistry::Register("ready", OnReadySignal);
			HandlerRegistry::Register("relay", OnRelayRequested);
			HandlerRegistry::Register("result", OnResultRequested);
		}
	}

}  // namespace pong

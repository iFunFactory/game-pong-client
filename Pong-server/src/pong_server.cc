// PLEASE ADD YOUR EVENT HANDLER DECLARATIONS HERE.

#include <boost/bind.hpp>
#include <funapi.h>
#include <gflags/gflags.h>

#include "event_handlers.h"
#include "pong_object.h"


// You can differentiate game server flavors.
DECLARE_string(app_flavor);

// Adding gflags. In your code, you can refer to them as FLAGS_example_arg3, ...
DEFINE_string(example_arg3, "default_val", "example flag");
DEFINE_int32(example_arg4, 100, "example flag");
DEFINE_bool(example_arg5, false, "example flag");


namespace {

const WallClock::Duration kOneSecond = WallClock::FromMsec(1000);

class PongServer : public Component {
 public:
  static bool Install(const ArgumentMap &arguments) {
    LOG(INFO) << "Built using Engine version: " << FUNAPI_BUILD_IDENTIFIER;

    // Kickstarts the Engine's ORM.
    // Do not touch this, unless you fully understand what you are doing.
    pong::ObjectModelInit();

    /*
     * Parameters specified in the "arguments" section in your MANIFEST.json
     * will be passed in the variable "arguments".
     * So, you can give configuration data to your game server.
     *
     * Example:
     *
     * We have in MANIFEST.json "example_arg1" and "example_arg2" that
     * have a string value and an integer value, respectively.
     * So, you can access the arguments like below:
     */
    string arg1 = arguments.FindStringArgument("example_arg1");
    LOG(INFO) << "example_arg1: " << arg1;

    int64_t arg2 = arguments.FindIntegerArgument("example_arg2");
    LOG(INFO) << "example_arg2: " << arg2;

     // You can override gflag like this: ./pong-local --example_arg3=hahaha
    LOG(INFO) << "example_arg3: " << FLAGS_example_arg3;

    /*
     * Registers various handlers.
     * You may be interesed in this function and handlers in it.
     * Please see "event_handlers.cc"
     */
    pong::RegisterEventHandlers();

	// MatchmakingServer 역할을 하는 서버에서 Start 함수를 호출하여
	// MatchmakingServer 를 시작합니다. 다음 4 개의 함수를 인자로 전달합니다.
	MatchmakingServer::Start(CheckMatch, CheckCompletion, OnJoined, OnLeft);

    return true;
  }

  // player 가 match 에 참여해도 되는지 검사합니다.
  static bool CheckMatch(const MatchmakingServer::Player &player, const MatchmakingServer::Match &match)
  {
	  LOG(INFO) << "CheckMatch " + player.id;
	  return true;
  }

  // JoinMatch 함수가 불린 후 호출됩니다. 해당 매치가 성사 되었는지 판단합니다.
  static MatchmakingServer::MatchState CheckCompletion(const MatchmakingServer::Match &match)
  {
	  LOG(INFO) << "CheckCompletion " + match.players.size();
	  if (match.players.size() == 2)
		  return MatchmakingServer::kMatchComplete;
	  else
		  return MatchmakingServer::kMatchNeedMorePlayer;
  }

  // CheckMatch 함수에서 조건에 만족하여 true 가 반환되면 이 함수가 호출됩니다. 이제 플레이어는 match 에 참여하게 되었습니다.
  // match 의 context 를 저장할 수 있습니다.)
  static void OnJoined(const MatchmakingServer::Player &player, MatchmakingServer::Match *match)
  {
	  LOG(INFO) << "OnJoined " + player.id;
	  if (match->context.IsNull())
	  {
		  match->context.SetObject();
		  match->context["A"] = player.id;
	  }
	  else
	  {
		  match->context["B"] = player.id;
	  }
  }

  static void OnLeft(const MatchmakingServer::Player &player, MatchmakingServer::Match *match)
  {
  }

  static bool Start() {
    return true;
  }

  static bool Uninstall() {
    return true;
  }
};

}  // unnamed namespace


REGISTER_STARTABLE_COMPONENT(PongServer, PongServer)

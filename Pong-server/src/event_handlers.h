// PLEASE ADD YOUR EVENT HANDLER DECLARATIONS HERE.

#ifndef SRC_EVENT_HANDLERS_H_
#define SRC_EVENT_HANDLERS_H_

#include <funapi.h>

#include "pong_messages.pb.h"
#include "pong_object.h"
#include "pong_rpc_messages.pb.h"
#include "pong_types.h"


namespace pong {

void RegisterEventHandlers();

}  // namespace pong

#endif  // SRC_EVENT_HANDLERS_H_

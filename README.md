Pong Game Client
========================

이 프로젝트는 iFun Engine을 사용하는 Unity3d 사용자를 위한 샘플 게임 클라이언트입니다.

## 목차
* [다운로드](#다운로드)
* [게임 오브젝트들](#게임-오브젝트들)
* [게임플레이](#게임플레이)
* [서버 설정](#서버-설정)
* [공지사항](#공지사항)
* [Funapi Unity Plugin](#funapi-unity-plugin)

## 요약
* 고전 게임 Pong 구현
* 싱글 플레이
* 1:1 대전 (Matchmaking)
* 공지사항 팝업


## 다운로드

**Pong Client** 프로젝트를 **git clone** 으로 다운 받거나 **zip 파일** 을 다운 받아 주세요.

```bash
git clone https://github.com/iFunFactory/game-pong.git
```

다운로드가 완료되면 유니티를 실행시키고 해당 프로젝트를 불러와 봅시다. 이후 `Assets` 폴더의 루트에 있는 **Main** Scene을 로드하면 됩니다.

* **Pong** 서버 설치는 [여기](https://github.com/iFunFactory/game-pong-server)를 참고해 주세요.

## 게임 오브젝트들

### GameLogic

``GameLogic`` 오브젝트는 전반적인 게임 로직을 담당하는 오브젝트입니다. Unity Editor Inspector에서 ``Ball Speed``와 ``Is Multi Play`` 프로퍼티 값을 변경하여 게임 옵션을 변경할 수 있습니다.

* ``Ball Speed`` : 게임 시작 시 공의 최초 속도를 나타내며 1 ~ 3 정도의 값으로 설정하면 테스트하기에 적당합니다.
* ``Is Multi Play`` : 활성화되어 있으면 서버에 접속한 다른 유저들 중에서 매칭 시스템을 통해 만난 상대와 함께 플레이하게 됩니다. 그렇지 않으면 싱글 플레이로 진행됩니다.

### NetworkManager

``NetworkManager`` 오브젝트는 Pong 게임 서버와 통신을 담당하는 오브젝트입니다. 이 오브젝트에서는 통신할 서버의 주소, Session Reliability 여부, TCP, UDP, HTTP 옵션 등을 지정할 수 있습니다.

* ``Server Addr`` : 통신할 게임서버의 ip주소를 입력하는 프로퍼티입니다. 기본값은 localhost인 127.0.0.1로 설정되어 있습니다.
* ``Session Reliability`` : 서버와 disconnect된 후에 재연결 시 동일한 세션으로 통신하도록 하는 옵션입니다. 자세한 내용은 세션 [메뉴얼](https://www.ifunfactory.com/engine/documents/reference/ko/client-plugin.html#session-reliability)을 참고해 주세요.
* ``Sequence Validation`` : 메시지에 Sequence Number를 붙여서 메시지의 유효성을 보장해줍니다. 자세한 내용은 Transport 옵션 [메뉴얼](https://www.ifunfactory.com/engine/documents/reference/ko/client-plugin.html#transport)을 참고해 주세요.

#### TCP Option
TCP Transport 옵션입니다. 자세한 내용은 TcpTransport 옵션 [메뉴얼](https://www.ifunfactory.com/engine/documents/reference/ko/client-plugin.html#transport)을 참고해 주세요.

* ``Tcp Encryption`` : 서버와 TCP로 메시지를 주고받을 때 메시지를 암호화하는 방법을 정의합니다.
* ``Auto Reconnect`` : 처음 서버에 연결할 때 실패하면 3~4회 정도 재연결을 시도합니다.
* ``Disable Nagle`` : 활성화 시키면 네이글 알고리즘을 사용하지 않습니다.
* ``Use Ping`` : 클라이언트 핑값을 로그로 표시합니다.

#### UDP Option

* ``Udp Encryption`` : 서버와 UDP로 메시지를 주고받을 때 메시지를 암호화하는 방법을 정의합니다. 자세한 내용은 메시지 암호화 [메뉴얼](https://www.ifunfactory.com/engine/documents/reference/ko/client-plugin.html#client-plugin-encryption) 을 참고해 주세요.


#### HTTP Option
HTTP Transport 옵션입니다. 자세한 내용은 HttpTransport 옵션 [메뉴얼](https://www.ifunfactory.com/engine/documents/reference/ko/client-plugin.html#transport)을 참고해 주세요.

* ``Http Encryption`` : 서버와 HTTP로 메시지를 주고받을 때 메시지를 암호화하는 방법을 정의합니다.
* ``Use WWW`` : UnityEngine.WWW 클래스 사용 여부를 결정하는 옵션입니다. 


## 게임플레이

**Main** Scene을 로드하여 Unity Editor로 실행하거나 기기에 넣어서 실행하면 됩니다.

### 싱글 플레이
``GameLogic`` 게임 오브젝트의 ``Is Multi Play``프로퍼티가 ``false``인 경우, **[게임시작]** 버튼을 누르면 싱글 플레이로 게임이 진행됩니다.

### 멀티 플레이
``GameLogic`` 게임 오브젝트의 ``Is Multi Play``프로퍼티가 ``true``인 경우, **[게임시작]** 버튼을 누르면 서버에 접속하게 됩니다. 서버와의 연결이 성공하면 **[대전시작]** 버튼이 활성화 됩니다. **[대전시작]** 버튼을 누르면 매칭을 기다렸다가 완료되면 게임이 시작됩니다.


## 서버 설정

서버 주소의 기본값 및 Port 번호는 *Scripts/NetworkManager.cs* 파일 상단의 **kServerAddr**, **kServerTcpPort**, **kServerUdpPort** 값을 수정하면 됩니다.

```csharp
// 서버 주소
public string kServerAddr = "127.0.0.1";

// 서버 포트
const ushort kServerTcpPort = 8012;
const ushort kServerUdpPort = 8013;
```

## 공지사항

공지사항을 확인하려면 공지사항 서버를 별도로 띄워야 합니다. 게임서버가 공지사항 서버의 기능을 하지는
않습니다. 공지사항 서버 설정에 대한 자세한 설명은 [여기](https://www.ifunfactory.com/engine/documents/reference/ko/announcer.html)를 참고해 주세요.

공지사항 서버를 띄웠다면 *AnnounceBoard.cs* 파일에서 공지사항 서버의 주소와 Port 번호를 수정하면 됩니다.

```csharp
const string kServerAddr = "127.0.0.1";
const UInt16 kServerPort = 8080;
```


## Funapi Unity Plugin

클라이언트 플러그인에 대한 자세한 정보는 [여기](https://github.com/iFunFactory/engine-plugin-unity3d)를 참고해 주세요.
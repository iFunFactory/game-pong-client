Pong Game Client
========================

이 프로젝트는 iFun Engine을 사용하는 Unity3d 사용자를 위한 샘플 게임 클라이언트입니다.

* **Pong** 서버 설치는 [여기](https://github.com/iFunFactory/game-pong-server)를 참고해 주세요.


## 목차
* [다운로드](#다운로드)
* [게임 오브젝트](#게임-오브젝트)
* [테스트](#테스트)
  - [싱글플레이](#싱글플레이)
  - [멀티플레이](#멀티플레이)
* [Funapi Unity Plugin](#funapi-unity-plugin)
* [Protobuf 버전](#Protobuf-버전)


## 요약
* 고전 게임 Pong 구현
* 싱글 플레이
* 멀티 플레이
    - 로그인
    - Daily 랭킹 조회 (Leaderboard)
    - 1 : 1 대전 (Matchmaking)
* 공지사항 팝업


## 다운로드

**Pong Client** 프로젝트를 **git clone** 으로 다운 받거나 **zip 파일** 을 다운 받아 주세요.

```bash
git clone https://github.com/iFunFactory/game-pong.git
```

다운로드가 완료되면 zip 파일의 경우는 압축을 해제하고, 유니티 에디터에서 폴더를 열어주세요.
에디터로 프로젝트가 열리면 `Assets` 폴더의 루트에 있는 **Main** Scene을 로드하면 됩니다.


## 게임 오브젝트

### GameLogic

`GameLogic` 오브젝트는 전반적인 게임 로직을 담당하는 오브젝트입니다. Unity Editor Inspector에서는 `Ball Speed` 프로퍼티 값을 변경하여 초기 공의 속도를 변경할 수 있습니다.

* `Ball Speed` : 게임 시작 시 공의 최초 속도를 나타내며 1 ~ 3 정도의 값으로 설정하면 테스트하기에 적당합니다.

### NetworkManager

`NetworkManager` : 오브젝트는 Pong 게임 서버와 통신을 담당하는 오브젝트입니다. 이 오브젝트에서는 통신할 서버의 주소, Session Reliability 여부, TCP, UDP, HTTP 옵션 등을 지정할 수 있습니다.

* **Announcement Server** : 공지사항을 수신할 서버의 정보를 설정합니다.
	- `Url` : 서버의 웹 서비스 URL을 입력합니다. (`Announcements/`는 생략합니다)

* **Lobby Server** : 접속할 로비 서버 정보를 설정합니다.
	- `Address` : 서버의 ip주소를 입력하는 프로퍼티입니다. 기본값은 *localhost*인 *127.0.0.1*로 설정되어 있습니다.
	- `Port` : 서버의 포트 정보를 입력합니다.
	- `Protocol` : 서버와 통신할 프로토콜을 선택합니다. (본 프로젝트는 TCP만 지원합니다)
	- `Encoding` : 서버와 메시지를 주고 받을 때 사용할 인코딩 방식을 선택합니다. 
	- `Session Reliability` : 서버와 disconnect된 후에 재연결 시 동일한 세션으로 통신하도록 하는 옵션입니다. 자세한 내용은 세션 [메뉴얼](https://www.ifunfactory.com/engine/documents/reference/ko/client-plugin.html#session-reliability)을 참고해 주세요.
	
#### TCP Option

TCP Transport 옵션입니다. 자세한 내용은 TcpTransport 옵션 [메뉴얼](https://www.ifunfactory.com/engine/documents/reference/ko/client-plugin.html#transport)을 참고해 주세요.

* `Tcp Encryption` : 서버와 TCP로 메시지를 주고받을 때 메시지를 암호화하는 방법을 정의합니다.
* `Auto Reconnect` : 처음 서버에 연결할 때 실패하면 3~4회 정도 재연결을 시도합니다.
* `Disable Nagle` : 활성화 시키면 네이글 알고리즘을 사용하지 않습니다.
* `Use Ping` : 클라이언트 핑값을 로그로 표시합니다.

#### UDP Option

* `Udp Encryption` : 서버와 UDP로 메시지를 주고받을 때 메시지를 암호화하는 방법을 정의합니다. 자세한 내용은 메시지 암호화 [메뉴얼](https://www.ifunfactory.com/engine/documents/reference/ko/client-plugin.html#client-plugin-encryption) 을 참고해 주세요.


#### HTTP Option

HTTP Transport 옵션입니다. 자세한 내용은 HttpTransport 옵션 [메뉴얼](https://www.ifunfactory.com/engine/documents/reference/ko/client-plugin.html#transport)을 참고해 주세요.

* `Http Encryption` : 서버와 HTTP로 메시지를 주고받을 때 메시지를 암호화하는 방법을 정의합니다.
* `Use WWW` : UnityEngine.WWW 클래스 사용 여부를 결정하는 옵션입니다.


## 플레이 실행

**Main** Scene을 로드하여 Unity Editor에서 바로 실행하거나 컴파일 후 기기에 설치해서 실행해도 됩니다.

### 로그인

첫 화면에서 **[게스트 로그인]**을 선택하면 서버와 통신해서 로그인을 수행하고, 플레이 메뉴 화면으로 진행합니다.

### 싱글 플레이

플레이 메뉴 화면에서 **[혼자하기]**를 선택하면 싱글 플레이 모드로 게임을 시작합니다.

### 멀티 플레이

플레이 메뉴 화면에서 **[같이하기]**를 선택하면, 대전 상대와 매칭을 기다리기 시작합니다.
또 하나의 클라이언트가 게임을 시작하고 매칭에 성공하면, 대전 상대와 대결하는 2인 플레이가 시작됩니다.

pong game server 설치 및 설정에 대한 내용은 [여기](https://github.com/iFunFactory/game-pong-server)를 참고해주세요.


#### [로그인]

* **[게스트 로그인]** : NetworkManager.cs의 deviceId 을 ID값으로 정하여 로그인하게 됩니다.  
* **[페이스북 로그인]** : Facebook에서 발급받은 Access Token을 통해 페이스북 인증 절차를 밟게되고, 인증이 정상적으로 이루어지면 Token ID값을 통해 페이스북 로그인이 이루어집니다.

#### [공지사항]

공지사항을 확인하려면 공지사항 서버를 별도로 띄워야 합니다. 게임서버가 공지사항 서버의 기능을 하지는
않습니다. 공지사항 서버 설정에 대한 자세한 설명은 [여기](https://www.ifunfactory.com/engine/documents/reference/ko/announcer.html)를 참고해 주세요.


#### [순위]

**[순위]**버튼을 선택하여 랭킹을 조회할 수 있습니다. 랭킹은 **일간 최다 연승 수**를 기준으로 매겨지며, 매일 `05:00:00`에 초기화되도록 설정되어 있습니다. 해당 기능은 리더보드 에이전트가 활성화 되어야 테스트 가능하며, 자세한 내용은 pong server의 [리더보드 에이전트](https://github.com/iFunFactory/game-pong-server)를 참고해 주세요.

## Funapi Unity Plugin

클라이언트 플러그인에 대한 자세한 정보는 [여기](https://github.com/iFunFactory/engine-plugin-unity3d)를 참고해 주세요.

## Protobuf 버전

### 클라이언트와 로비서버의 통신을 JSON 에서 Protobuf 로 바꿀 때

* 클라이언트 설정: Editor 세팅에서 Encoding type 을 JSON 에서 Protobuf 로 변경합니다. (NetworkManager.cs 안의 값은 Unity Editor 의 값으로 오버라이드 되기 때문에 반드시 Editor 를 통해서 업데이트 해주세요)

* 서버 설정: **MANIFEST.lobby.json** 에서 **tcp_json_port** 부분의 값을 0으로 바꾸고 대신 **tcp_protobuf_port** 의 값을 0이 아닌 값으로 세팅합니다. 만일 TCP 가 아니라 UDP 를 사용하는 경우 각각 **udp_json_port**, **udp_protobuf_port** 를 수정해야됩니다.

### 클라이언트와 게임 서버의 통신을 JSON 에서 Protobuf 로 바꿀 때

* 클라이언트 설정: 클라이언트는 로비서버에 의해 redirect 메시지를 받아서 게임 서버에 접속하게 됩니다. 이 때 로비 서버는 게임 서버의 통신 프로토콜과 인코딩 타입을 클라이언트에 전송하고 아이펀 엔진의 클라이언트 플러그인이 이를 자동으로 처리합니다. 따라서 게임 서버의 통신을 JSON 에서 Protobuf 로 바꾸기 위해서는 별도의 클라이언트 설정 변경은 필요하지 않습니다.

* 서버 설정: **MANIFEST.game.json** 에서 **tcp_json_port** 부분의 값을 0으로 바꾸고 대신 **tcp_protobuf_port** 의 값을 0이 아닌 값으로 세팅합니다. 만일 TCP 가 아니라 UDP 를 사용하는 경우 각각 **udp_json_port**, **udp_protobuf_port** 를 수정해야됩니다.


### Protobuf 파일을 수정한 경우

아이펀 엔진은 .proto 파일을 클라이언트/서버가 각각 관리하고 컴파일 해야되는 번거로움을 없애기 위해서, 서버측의 .proto 파일로 Unity 용 dll 을 생성해줍니다.
proto message 를 수정하기 위해서는 서버측 **src/pong_messages.proto** 파일을 수정합니다.
그리고 빌드를 한 후, 서버측 빌드 디렉토리 아래 **unity_dll** 이라는 서브디렉토리가 있는데, 거기에 있는 dll 파일들을 Unity client 의 **Assets/Protobuf** 라는 서브디렉토리에 덮어씁니다.

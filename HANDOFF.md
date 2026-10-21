# 인수인계 메모 (Mac → Windows 집 PC)

작성: 2026-10-14. 처음엔 Windows에서 Mac으로 넘어왔고, 이번에는 Mac에서 집 Windows PC로 이어가기 위한 메모입니다.
Mac에서는 코드 변경 없이 프로젝트 분석만 했고, 실기기 검증은 하지 못했습니다. 상태는 아래와 같습니다.
현재 변경(change)은 `openspec/changes/bowling-core-loop`(단계 A: 코어 플레이 루프)입니다.

## 한눈에 보는 상태

- **구현은 끝났고, 남은 것은 대부분 Quest 3S 실기기 검증입니다.** 자동 테스트는 모두 통과했습니다.
  - EditMode 162개, PlayMode 88개 통과 (`Window > General > Test Runner`)
  - Android APK 빌드 성공 (약 51MB)
- 작업 진행률: `tasks.md` 기준 **19/30 완료**. 남은 11개는 모두 아래의 실기기 검증입니다.
- 실기기가 없어서 "실기기에서 확인한다"가 검증 기준인 작업은 구현·시뮬레이션 테스트까지 했지만 **체크하지 않았습니다.**

### 이후에 추가된 것 (실기기 확인 전)

실기기 검증 표와 별개로, 학교에서 함께 확인할 새 기능이 있습니다. 모두 에디터와 자동 테스트로만 확인했고 헤드셋에서는 보지 못했습니다.

- **홀**: 원본 볼링장 FBX를 씬에 다시 넣었습니다(레인 4개, 천장, 테이블·의자, TV). 삼각형이 약 52만 개로 Quest 3S에 무거울 수 있습니다. 부족하면 `BuildHallPrefab`에서 `Cylinder003`(22만 삼각형 천장 패널)과 찻주전자 조명, `Line0*`을 빼세요. 홀을 옆으로 밀어 고른 레인이 정면에 오게 합니다(`LaneSelector`).
- **정식 점수(10프레임)**: `Scorecard`, 두 번째 공에서는 서 있는 핀이 유지됩니다(`PinRack.ClearFallenAndRespot`).
- **1~4인 번갈아 치기**: `Match`, `Player`. 선택 화면의 PLAYERS 행. 한 기기에서 돌려 쓰는 방식입니다.
- **네트워크 멀티(같은 와이파이, 뼈대만)**: Netcode for GameObjects 2.13을 추가했습니다. 각자 자기 레인에서 자기 물리를 돌리고, 이름·레인·던진 핀 수 목록만 서로 알려 줍니다(`NetPlayer`, `NetworkSession`, `NetworkBridge`, 코어의 `Match.Replay`). 호스트가 레인을 한 칸씩 배정하고 최대 4명입니다. 자동 테스트로 호스트와 접속 기기 사이 복제를 확인했습니다. **아직 없는 것**: 호스트 열기/접속 주소 입력 화면(UI), 상대 레인의 공·핀 표시, 아바타, 음성, 인터넷 연결(Relay). 안드로이드 빌드를 위해 인터넷 권한을 켰습니다(`forceInternetPermission`).
- **판정**: 넘어짐 또는 덱 이탈(구덩이·레인 밖·거터)만 셉니다. 서 있는 채 밀려난 핀은 세지 않습니다.
- **핀 물리**: 평평한 바닥의 실제 핀 모양, 무게중심 14.6cm, 반발력과 마찰, 공 6.8kg. 시뮬레이션으로만 맞췄으니 4.7에서 손맛을 확인하세요.
- **화면**: 반투명 UI, 왼쪽 벽의 점수판, 레인 위 TV, 시선 왼쪽 위의 미러 화면(핀+TV를 보여 주는 두 번째 카메라: 2.4 프로파일링 때 던지는 동안 프레임을 꼭 보세요), 앉을 수 있는 의자.

### 실기기 검증이 남은 작업 (구현은 되어 있음)

| 작업 | 확인할 것 |
|---|---|
| 1.1 | APK가 Quest 3S에서 실행됨 |
| 1.2 | 머리와 두 컨트롤러가 움직임을 따라감 |
| 2.3 | 조명이 어둡거나 번쩍이지 않고 읽힘 |
| 2.4 | 프레임 시간·드로우콜·삼각형 수 기록, 72Hz 이상 유지 |
| 3.7 | 굴러가는 소리, 핀 충돌음, 스트라이크음이 들림 |
| 4.1 | 그립으로 손 근처에서만 잡히고 멀면 반응 없음 |
| 4.4 | 던지면 공이 레인 방향으로 굴러감 |
| 4.7 | 네 보정 단계를 각각 던져 보고 수치 조정 |
| 5.3 | 컨트롤러 레이로 보정 선택·시작이 됨 |
| 5.4 | 정위치에서 전광판 글자가 선명하게 읽힘 |
| 6.1 | 선택 → 던지기 → 판정 → 표시 → 리셋 → 재시작을 10회 이상 반복 |

## Windows(집 PC)에서 시작하기

1. 저장소를 클론하거나 기존 폴더에서 `git pull`을 합니다: `git clone https://github.com/KJY-1204/VRB.git`
2. Unity Hub에서 **Unity `6000.3.24f1`** 을 설치합니다. 모듈은 **Android Build Support**(OpenJDK, Android SDK & NDK Tools 포함)를 선택합니다.
3. 프로젝트를 엽니다. `Library/`는 저장소에 없으므로 클론했다면 **첫 임포트에 시간이 걸립니다.**
4. `File > Build Profiles`에서 **Android로 전환**합니다. (`UserSettings/`, `Library/`는 저장소에 없습니다.)
5. 열린 씬은 `Assets/Scenes/VRB.unity`입니다. 빌드 씬 목록에도 이것만 들어 있습니다.
6. APK 설치용 adb는 Unity에 포함돼 있습니다. 경로 예시(설치 위치에 맞게 수정):
   `"C:\Program Files\Unity\Hub\Editor\6000.3.24f1\Editor\Data\PlaybackEngines\AndroidPlayer\SDK\platform-tools\adb.exe" install -r <APK 경로>`
   Quest는 개발자 모드를 켜고 USB로 연결합니다.

### 환경 관련 참고

- Windows에서는 `.vsconfig`를 Visual Studio 설치 구성에 쓸 수 있습니다.
- 줄바꿈은 `.gitattributes`로 LF로 통일돼 있어 별도 설정이 필요 없습니다.
- **OpenSpec CLI(`openspec`)와 Unity MCP(`unity-editor-mcp`)는 저장소에 포함되지 않은 PC별 도구입니다.** `/opsx:*` 명령이나 에디터 자동화를 쓰려면 집 PC에서 확인하세요. `.claude/commands`와 `.claude/skills`는 저장소에 있습니다.
- Claude의 대화 메모리는 PC별이라 넘어오지 않습니다. 필요한 결정은 이 문서와 `openspec/`에 적어 두었습니다.
- Mac에서 Android로 전환하면 `Assets/XR/Settings/OpenXRPackageSettings.asset`이 자동으로 다시 직렬화되어(`Standalone` → `Android` 이름 등) 변경으로 잡힙니다. 기능 변경이 아니므로 커밋하지 않았습니다. 집 PC에서도 비슷한 변경이 생기면 무시하거나 `git checkout`으로 되돌리세요.

## 이어서 할 일 (집: VR 없음 / 학교: Quest 3S 있음)

집 PC에는 VR 기기가 없고 **학교에 Quest 3S가 있습니다.** 그래서 작업을 장소별로 나눕니다.

### 집에서 (기기 없이 가능)

1. `git pull` 후 프로젝트를 열고 Android로 전환합니다.
2. **EditMode/PlayMode 테스트가 그대로 통과하는지** 확인합니다. (`Window > General > Test Runner`)
3. **APK를 미리 빌드해 둡니다.** 학교에서는 설치만 하면 되도록 USB 메모리·클라우드에 복사해 가져갑니다. (학교 PC에 Unity 환경이 없을 수 있음) 학교 노트북이나 PC에 adb(또는 SideQuest/Meta Quest Developer Hub)가 있는지도 확인하세요.
4. 기기 없이 가능한 일을 진행합니다.
   - 다음 큰 작업은 **네트워크 멀티플레이**입니다(각자 자기 레인에서 물리를 돌리고 점수만 공유하는 방식을 검토 중). 점수와 차례는 이미 플레이어별로 분리돼 있습니다(`Match`).
   - 에디터 Play 모드 확인(XR Device Simulator 등)은 보조일 뿐 실기기 검증(아래 표)을 대체하지 못합니다. 체크박스는 채우지 마세요.

### 학교에서 (Quest 3S 필요)

1. 미리 빌드한 APK를 설치합니다. (학교 PC에서 Quest 개발자 모드·USB 디버깅 허용 필요. 개발자 모드는 Meta 계정/앱에서 켭니다.)
2. 위 "실기기 검증이 남은 작업" 표를 진행하고, 끝난 항목은 `tasks.md`에 체크합니다.
3. **4.7**: 던져 보면서 `Assets/VRB/Config/BowlingConfig.asset`의 값을 조정합니다. 값을 바꾸려면 Unity 에디터와 재빌드가 필요하니, 학교에 Unity가 없으면 **튜닝 값을 메모해 두었다가 집에서 반영**하세요.
   - `throwAssist`: `weakPull`(0), `mediumPull`(0.3), `strongPull`(0.6), `averagingWindowSeconds`(0.1), `maxUpwardSpeed`(1)
   - `pinJudge`: 쓰러짐 각도(45°), 덱 이탈 기준(레인 반폭 `deckHalfWidth`, 거터 높이 `deckFloorHeight`), 정지 속도, 정지 유지 시간(1.5초), 최대 대기(10초)
4. **2.4**: 프로파일링 결과 예산이 남으면 볼링장 FBX의 가벼운 장식을 추가합니다. (현재는 핀·공 메시만 사용)

### 검증을 모두 마친 뒤

체크박스를 채우고 `/opsx:archive`로 이 change를 아카이브한 뒤, 단계 B change를 새로 시작합니다.

## 알아 둘 설계 결정과 주의점

- **씬은 에디터 스크립트로 재생성할 수 있습니다.** 메뉴 `VRB > Build All` (`Assets/Editor/BowlingSceneBuilder.cs`). 레인, 핀, 공, UI, 게임 컨트롤러, 조명을 다시 만듭니다. 수동으로 씬을 고쳤다면 빌더를 다시 돌리기 전에 확인하세요.
- **홀은 FBX를 씬에 넣어 두고, 물리는 직접 만든 미터법 레인으로 돌립니다.** 직접 만든 레인 박스는 충돌체로만 남기고 렌더러는 껐습니다(홀 메시가 눈에 보입니다). 핀·공 메시는 실제 치수(핀 0.381m, 공 0.218m)로 재조정해 씁니다. 게임은 항상 `x=0` 레인에서 돌고, 레인을 고르면 홀이 옆으로 밀립니다.
- **홀 메시를 편집해서 씁니다.** 레인 4개가 메시 하나에 구워져 있어서, 2번 레인의 핀 데크 끝부분을 나머지 레인에 복사해 가림막을 열었습니다(`BowlingHallPlane.asset`, `OpenMaskingUnits`). 홀은 FBX의 헤드핀 자리가 게임 헤드핀과 맞도록 레인 방향으로 약 4% 줄여서 놓습니다.
- **`NetworkManager`는 장면이 바뀌어도 살아남습니다(Netcode가 항상 `DontDestroyOnLoad`).** 그래서 장면을 다시 불러오는 테스트는 시작 전에 남은 `NetworkManager`를 지워야 합니다(`NetworkTests`의 `LoadScene` 참고). 장면 안의 오브젝트를 가리키는 `NetworkBridge`는 `Network` 오브젝트와 따로 둬서 장면과 함께 사라지게 했습니다. `NetworkSession`은 프리펩 등록을 `Awake`가 아니라 시작할 때 하는데, 같은 오브젝트의 컴포넌트는 `Awake` 순서가 보장되지 않기 때문입니다.
- **테스트 도구 주의**: 직전 테스트 실행이 씬을 "수정됨"으로 남기면 다음 PlayMode 실행이 시작 직후 중단되고 상태만 "running"으로 남습니다. 실행 전에 씬을 저장하고, 400 오류가 나면 취소 후 다시 실행하세요. 중단된 실행이 `Assets/InitTestScene*.unity` 임시 파일을 남기면 지워도 됩니다.
- **이동(걷기/텔레포트)을 껐습니다.** 리그의 `Locomotion`을 비활성화해 플레이어가 레인으로 들어가지 않게 했습니다. 플레이어는 파울 라인 뒤(z=-1.6)에서 시작합니다.
- **요청하지 않았지만 바꾼 것**: 애플리케이션 ID를 `com.DefaultCompany.VRTemplate` → `com.vrb.bowling`, 회사명 `VRB`.
- **기본 보정은 Weak.** 단계 선택은 게임 시작 전에만 가능하고, 확정하기 전에는 공을 잡을 수 없습니다.
- **판정에 약 10초가 걸릴 수 있습니다.** 쓰러진 핀이 굴러서 정지하는 데 시간이 걸립니다. 누운 핀에 추가 회전 감쇠(`Pin.m_LyingAngularDamping`=2)를 줘서 줄였지만(15초 → 약 11.5초), 체감이 느리면 `pinJudge`의 정지 기준과 최대 대기를 줄이세요.
- **효과음**: `Bowling pin hit.wav`는 핀 무더기가 쓰러지는 5.85초짜리 소리입니다. 공이 처음 닿을 때 전체를, 이후 핀끼리 부딪힐 때는 짧은 조각을 재생합니다. 소리가 어색하면 `PinSounds`의 값을 조정하세요.
- **UI는 영문·숫자만 씁니다.** 기본 TMP 글꼴이 한글을 지원하지 않아서입니다. 한글이 필요하면 한글 글꼴 에셋을 추가해야 합니다.
- **빌드 때 콘솔에 `NullReferenceException`이 한 번 나옵니다.** OpenXR 패키지의 사전 검증 규칙(`MetaQuestFeature.cs`)에서 나는 것이고 빌드는 성공합니다. 무시해도 됩니다.
- **`Lane/FoulZone` 오브젝트는 현재 쓰이지 않습니다.** 파울 판정은 놓는 순간 손의 위치(z > 0)로 합니다.
- **PlayMode 테스트는 `VRB` 씬이 빌드 설정에 들어 있어야 돕니다.** 테스트를 돌릴 때 에디터 연결이 잠깐 끊기는 것은 도메인 리로드 때문입니다.
- 테스트의 손은 XRI의 `SelectEnter`/`SelectExit`로 흉내 낸 것이라, 실제 컨트롤러의 트래킹 노이즈는 반영돼 있지 않습니다. 그래서 4.7의 실기기 튜닝이 중요합니다.

## 라이선스 참고

- `Assets/VRB/bowling-club`은 CC BY 4.0(tiunov.se)이며 README에 출처를 표기했습니다.
- 소스 코드 라이선스는 아직 정하지 않았습니다.

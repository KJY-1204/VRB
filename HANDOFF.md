# 인수인계 메모 (Windows → Mac)

작성: 2026-10-14. Windows PC에서 진행하던 작업을 Mac에서 이어가기 위한 메모입니다.
현재 변경(change)은 `openspec/changes/bowling-core-loop`(단계 A: 코어 플레이 루프)입니다.

## 한눈에 보는 상태

- **구현은 끝났고, 남은 것은 대부분 Quest 3S 실기기 검증입니다.** 자동 테스트는 모두 통과했습니다.
  - EditMode 59개, PlayMode 25개 통과 (`Window > General > Test Runner`)
  - Android APK 빌드 성공 (약 51MB)
- 작업 진행률: `tasks.md` 기준 **18/30 완료**. 남은 12개 중 11개가 실기기 검증이고, 1개(6.3)는 푸시 확인입니다.
- 실기기가 없어서 "실기기에서 확인한다"가 검증 기준인 작업은 구현·시뮬레이션 테스트까지 했지만 **체크하지 않았습니다.**

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

## Mac에서 시작하기

1. 저장소를 클론합니다: `git clone https://github.com/KJY-1204/VRB.git`
2. Unity Hub에서 **Unity `6000.3.24f1`** 을 설치합니다. 모듈은 **Android Build Support**(OpenJDK, Android SDK & NDK Tools 포함)를 선택합니다.
3. 프로젝트를 엽니다. `Library/`는 저장소에 없으므로 **첫 임포트에 시간이 걸립니다.**
4. 이 프로젝트는 Windows에서 Android 타깃으로 전환해 두었지만, 그 설정(`UserSettings/`, `Library/`)은 저장소에 없으므로 Mac에서 **`File > Build Profiles`에서 Android로 한 번 전환**해야 합니다.
5. 열린 씬은 `Assets/Scenes/VRB.unity`입니다. 빌드 씬 목록에도 이것만 들어 있습니다.
6. APK 설치용 adb는 Unity에 포함돼 있습니다.
   `/Applications/Unity/Hub/Editor/6000.3.24f1/PlaybackEngines/AndroidPlayer/SDK/platform-tools/adb install -r <APK 경로>`
   Quest는 개발자 모드를 켜고 USB로 연결합니다.

### Mac에서 달라지는 점

- `.vsconfig`는 Windows의 Visual Studio용 설정이라 Mac에서는 무시해도 됩니다. Rider나 VS Code를 쓰세요.
- 줄바꿈은 `.gitattributes`로 LF로 통일돼 있어 별도 설정이 필요 없습니다.
- **OpenSpec CLI(`openspec`)와 Unity MCP(`unity-editor-mcp`)는 저장소에 포함되지 않은 PC별 도구입니다.** 이어서 `/opsx:*` 명령이나 에디터 자동화를 쓰려면 Mac에도 설치·연결해야 합니다. `.claude/commands`와 `.claude/skills`는 저장소에 있습니다.
- Claude의 대화 메모리는 PC별이라 넘어오지 않습니다. 필요한 결정은 이 문서와 `openspec/`에 적어 두었습니다.

## 이어서 할 일 (제안 순서)

1. Quest 3S용 APK를 빌드해 설치하고 위 표의 실기기 검증을 진행합니다.
2. **4.7**: 던져 보면서 `Assets/VRB/Config/BowlingConfig.asset`의 값을 조정합니다.
   - `throwAssist`: `weakPull`(0), `mediumPull`(0.3), `strongPull`(0.6), `averagingWindowSeconds`(0.1), `maxUpwardSpeed`(1)
   - `pinJudge`: 쓰러짐 각도(45°), 이동 거리(0.35m), 정지 속도, 정지 유지 시간(1.5초), 최대 대기(10초)
3. **2.4**: 프로파일링 결과 예산이 남으면 볼링장 FBX의 가벼운 장식을 추가합니다. (현재는 핀·공 메시만 사용)
4. 검증이 끝나면 체크박스를 채우고 `/opsx:archive`로 이 change를 아카이브한 뒤, 단계 B(정통 점수 규칙) change를 새로 시작합니다.

## 알아 둘 설계 결정과 주의점

- **씬은 에디터 스크립트로 재생성할 수 있습니다.** 메뉴 `VRB > Build All` (`Assets/Editor/BowlingSceneBuilder.cs`). 레인, 핀, 공, UI, 게임 컨트롤러, 조명을 다시 만듭니다. 수동으로 씬을 고쳤다면 빌더를 다시 돌리기 전에 확인하세요.
- **원본 볼링장 FBX 인스턴스는 씬에서 뺐습니다.** FBX 에셋(`Assets/VRB/bowling-club`)은 그대로 있고, 핀·공 메시만 실제 치수(핀 0.381m, 공 0.218m)로 재조정해 씀. 전체 홀은 삼각형 약 52만 개로 Quest 3S에 무겁습니다. 대신 미터법 한 레인을 직접 만들었습니다.
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

# VRB — VR Bowling

Meta Quest 3S 컨트롤러로 즐기는 VR 볼링 게임입니다. Unity 6 + OpenXR + XR Interaction Toolkit으로 만들고 있으며,
현재는 공을 집어 던지고 핀이 쓰러지는 기본 플레이 루프(단계 A)에 더해, 정식 10프레임 점수 규칙(단계 B)과
한 레인에서 돌아가며 치는 1~4인 플레이를 구현한 상태입니다. (네트워크 멀티플레이는 아직 없습니다.)

- 기기: Meta Quest 3S (컨트롤러), 완전 VR (MR 미지원)
- 엔진: Unity `6000.3.24f1`, URP 17.3, OpenXR 1.17, XR Interaction Toolkit 3.5
- 진행 순서: A(코어 루프) ✔ → B(정통 볼링 규칙) ✔ → 로컬 멀티(차례·점수 분리) ✔ → 네트워크 멀티 → C(스핀·공 종류 등 게임성)

## 플레이 방법

1. 시작 화면에서 **레인(1~4), 인원(1~4명), 던지기 보정 단계**를 고르고 `START`를 누릅니다. (컨트롤러 레이로 가리키고 트리거)
2. 공 거치대의 공 가까이(약 35cm 이내)에서 **그립**을 누른 채 집어 듭니다.
3. 파울 라인(빨간 선) **뒤에서** 레인 방향으로 휘두르며 그립을 놓으면 공이 던져집니다.
4. 핀이 멈추면 결과가 표시됩니다. 첫 공 뒤에는 쓰러진 핀만 치워지고 서 있는 핀이 남아 두 번째 공을 던집니다.
   스트라이크이거나 프레임이 끝나면 새 핀이 세팅되고, 여럿이 칠 때는 다음 사람 차례가 됩니다.
5. 10프레임(보너스 공 포함)이 끝나면 `GAME OVER`와 승자가 표시됩니다. `RESTART`로 다시 시작하면 인원과 레인은 유지되고 기록만 지워집니다.

**화면**
- **점수판**: 고개를 왼쪽으로 돌리면 벽에 걸려 있습니다. 플레이어별로 10프레임의 마크(`X`, `7 /`, `-`)와 누적 점수, 합계가 나옵니다.
- **TV**: 레인 끝 핀 위의 TV에 지금 차례인 사람의 이름과 총점이 나옵니다.
- **미러 화면**: 시선 왼쪽 위에 핀과 TV 점수를 함께 보여 주는 작은 화면이 떠 있습니다.
- **의자**: 레인 옆 의자를 레이로 가리키고 트리거를 누르면 앉고, 다시 누르면 돌아옵니다.

**규칙 메모**: 넘어졌거나 덱 밖(구덩이, 레인 밖, 거터)으로 나간 핀만 세고, 밀려났어도 서 있는 핀은 세지 않습니다.

| 보정 단계 | 동작 |
|---|---|
| Raw Physics | 놓는 순간의 손 속도를 그대로 사용 |
| Weak (기본) | 놓기 직전 0.1초 평균으로 손 떨림만 완화 |
| Medium | Weak + 레인 방향으로 약하게 보정 |
| Strong | Weak + 레인 방향으로 강하게 보정 |

보정 수치는 `Assets/VRB/Config/BowlingConfig.asset`에서 조정합니다.

## 열기와 빌드

1. Unity Hub에서 Unity `6000.3.24f1`(Android Build Support 포함)로 이 폴더를 엽니다.
2. `Assets/Scenes/VRB.unity`가 빌드 대상 씬입니다.
3. `File > Build Profiles`에서 **Android**를 선택하고 `Switch Platform`을 누른 뒤 `Build`로 APK를 만듭니다.
   (ARM64, IL2CPP, Vulkan, OpenXR의 Meta Quest 기능은 이미 설정돼 있습니다.)
4. Quest를 개발자 모드로 USB 연결하고 설치합니다.

   ```
   adb install -r <빌드한 APK 경로>.apk
   ```

씬 구성(레인, 핀, 공, 홀, UI, 의자, 미러)은 에디터 스크립트로 재생성할 수 있습니다: 메뉴 `VRB > Build All`.

## 테스트

`Window > General > Test Runner`에서 실행합니다.

- **EditMode**: 점수·상태 흐름, 핀 배치와 판정, 던지기 속도 계산 등 Unity와 무관한 로직 (`Assets/Scripts/Core`)
- **PlayMode**: 실제 씬에서 핀 물리, 던지기, 효과음, 전체 게임 루프

## 프로젝트 구조

```
Assets/
  Scripts/Core/      Unity에 의존하지 않는 순수 C# 로직 (점수, 상태 흐름, 던지기 계산, 핀 판정)
  Scripts/Runtime/   씬과 XR을 연결하는 컴포넌트 (공, 핀, UI, 게임 컨트롤러)
  Editor/            씬/프리팹 빌더
  Tests/             EditMode / PlayMode 테스트
  VRB/               프리팹, 머티리얼, 설정, 오디오, 서드파티 모델
openspec/            변경 계획(OpenSpec): proposal, specs, design, tasks
```

## 크레딧과 라이선스

- 3D 모델 **"Bowling Club"** ([Sketchfab](https://sketchfab.com/3d-models/bowling-club-0b8fae45fcda4fe78f93bb2a899401a6)) by **tiunov.se**,
  [Creative Commons Attribution 4.0](http://creativecommons.org/licenses/by/4.0/) 라이선스.
  이 프로젝트에서는 홀 전체(레인 4개, 천장, 테이블과 의자, TV 등)를 배경으로 쓰고, 볼링핀·볼링공 메시는 실제 치수에 맞게 크기를 조정해 게임용으로 씁니다.
  홀은 레인 방향으로 늘이고 일부 메시를 복사해 편집했습니다. (원본 라이선스 문구: `Assets/VRB/bowling-club/license.txt`)
- `Assets/Samples`와 `Assets/VRTemplateAssets`는 Unity 패키지(XR Interaction Toolkit, XR Hands, VR 템플릿)에서 가져온 샘플입니다.
- 소스 코드의 라이선스는 아직 지정하지 않았습니다.

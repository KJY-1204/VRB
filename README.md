# VRB — VR Bowling

Meta Quest 3S 컨트롤러로 즐기는 VR 볼링 게임입니다. Unity 6 + OpenXR + XR Interaction Toolkit으로 만들고 있으며,
현재는 공을 집어 던지고 핀이 쓰러지는 기본 플레이 루프(단계 A)를 구현한 상태입니다.

- 기기: Meta Quest 3S (컨트롤러), 완전 VR (MR 미지원)
- 엔진: Unity `6000.3.24f1`, URP 17.3, OpenXR 1.17, XR Interaction Toolkit 3.5
- 진행 순서: A(코어 루프) → B(정통 볼링 규칙) → C(스핀·공 종류 등 게임성) → 이후 멀티플레이 검토

## 플레이 방법

1. 시작 화면에서 **던지기 보정 단계**를 고르고 `Start`를 누릅니다. (컨트롤러 레이로 가리키고 트리거)
2. 공 거치대의 공 가까이(약 35cm 이내)에서 **그립**을 누른 채 집어 듭니다.
3. 파울 라인(빨간 선) **뒤에서** 레인 방향으로 휘두르며 그립을 놓으면 공이 던져집니다.
4. 핀이 멈추면 쓰러진 핀 수가 전광판에 표시되고, 핀과 공이 자동으로 다시 준비됩니다.
5. 전광판의 `Restart`를 누르면 기록이 초기화되고 보정 단계를 다시 고를 수 있습니다.

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

씬 구성(레인, 핀, 공, UI)은 에디터 스크립트로 재생성할 수 있습니다: 메뉴 `VRB > Build All`.

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
  이 프로젝트에서는 모델의 볼링핀·볼링공 메시를 실제 치수에 맞게 크기를 조정해 사용했습니다. (원본 라이선스 문구: `Assets/VRB/bowling-club/license.txt`)
- `Assets/Samples`와 `Assets/VRTemplateAssets`는 Unity 패키지(XR Interaction Toolkit, XR Hands, VR 템플릿)에서 가져온 샘플입니다.
- 소스 코드의 라이선스는 아직 지정하지 않았습니다.

# Spec Delta

## Purpose

게임 시작 전 보정 단계 선택, 투구 결과 표시, 재시작으로 이어지는 기본 플레이 루프를 정의한다. 정통 점수 규칙은 다루지 않고 쓰러진 핀 수만 보여 준다.

## ADDED Requirements

### Requirement: 시작 전 보정 단계 선택 화면
The system SHALL show a selection screen before the first throw where the player can choose one of the four throw correction levels, with weak preselected.

#### Scenario: 게임을 켠다
- **WHEN** 플레이어가 게임을 실행한다
- **THEN** 순수 물리, 약함, 중간, 강함이 표시되고 약함이 선택된 상태이다

#### Scenario: 단계를 바꾼다
- **WHEN** 플레이어가 컨트롤러로 다른 단계를 선택한다
- **THEN** 선택이 그 단계로 바뀌어 표시된다

### Requirement: 선택 확정 후 게임 시작
The system SHALL start the game with the chosen correction level once the player confirms the selection, and SHALL NOT allow throwing before confirmation.

#### Scenario: 확정 전 던지기 시도
- **WHEN** 플레이어가 선택을 확정하기 전에 공을 집어 던지려 한다
- **THEN** 던지기는 시작되지 않는다

#### Scenario: 확정
- **WHEN** 플레이어가 선택을 확정한다
- **THEN** 게임이 시작되고 공을 집어 던질 수 있다

### Requirement: 투구 결과 표시
The system SHALL display the number of pins fallen by each throw after the result is determined, in a place visible from the throwing position.

#### Scenario: 일부 핀이 쓰러짐
- **WHEN** 투구 결과로 7개의 핀이 쓰러진 것이 확정된다
- **THEN** 던지는 위치에서 보이는 표시에 7이 나타난다

#### Scenario: 하나도 못 쓰러뜨림
- **WHEN** 공이 핀을 하나도 쓰러뜨리지 못한다
- **THEN** 표시에 0이 나타난다

### Requirement: 누적 투구 기록
The system SHALL keep a record of each throw in the current game, containing the throw order and the number of pins fallen, and SHALL show the total of fallen pins.

#### Scenario: 여러 번 던진다
- **WHEN** 플레이어가 세 번 던져 각각 7, 0, 10개를 쓰러뜨린다
- **THEN** 기록에 세 투구가 순서대로 남고 총합 17이 표시된다

### Requirement: 점수 계산과 표시의 분리
The system MUST compute the throw record and totals independently of input and rendering, using only the fallen pin count of each throw as input.

#### Scenario: 입력 없이 결과 주입
- **WHEN** 핀 개수 결과만 순서대로 주어진다
- **THEN** 컨트롤러나 화면 없이도 같은 기록과 총합이 계산된다

### Requirement: 재시작
The system SHALL let the player restart the game, which resets the throw record and returns to the pin state at the start of a game.

#### Scenario: 재시작한다
- **WHEN** 플레이어가 재시작을 선택한다
- **THEN** 투구 기록과 총합이 0으로 돌아가고 핀 10개가 처음 배치로 서 있다

#### Scenario: 보정 단계 다시 고르기
- **WHEN** 플레이어가 재시작 후 다시 시작 전 화면으로 돌아간다
- **THEN** 보정 단계를 다시 고를 수 있다

### Requirement: 가독성 있는 VR UI
The system SHALL present all menus and score displays so they are readable at the distance and angle of the player's normal standing position in VR.

#### Scenario: 정위치에서 읽기
- **WHEN** 플레이어가 던지는 위치에 서서 화면을 본다
- **THEN** 선택 화면과 결과 표시의 글자가 흐리거나 잘리지 않고 읽힌다

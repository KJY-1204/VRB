# Spec Delta

## Purpose

플레이어가 VR 컨트롤러로 볼링공을 집어 레인을 향해 던지는 상호작용을 정의한다. 릴리스 속도의 평균화와 4단계 던지기 보정으로 손 떨림을 줄이면서도 실력이 결과에 반영되게 한다.

## ADDED Requirements

### Requirement: 컨트롤러로 공 집기
The system SHALL let the player grab a bowling ball with a VR controller grip input and hold it in hand while the input is held.

#### Scenario: 공을 집는다
- **WHEN** 플레이어가 공 가까이에서 컨트롤러의 그립 입력을 누른다
- **THEN** 공이 손에 붙어 컨트롤러를 따라 움직이고 다른 물체와 물리적으로 충돌하지 않는다

#### Scenario: 공에서 멀리 있을 때
- **WHEN** 플레이어가 공에서 손이 닿지 않는 거리에서 그립 입력을 누른다
- **THEN** 공은 움직이지 않는다

### Requirement: 릴리스 시 공 던지기
The system SHALL release the ball with a velocity derived from the controller motion when the player releases the grip input, and the ball SHALL then follow physics.

#### Scenario: 레인 방향으로 던진다
- **WHEN** 플레이어가 컨트롤러를 레인 방향으로 휘두르며 그립을 놓는다
- **THEN** 공은 놓는 순간의 손 속도 방향으로 레인 위를 굴러간다

#### Scenario: 정지 상태에서 놓는다
- **WHEN** 플레이어가 컨트롤러를 거의 움직이지 않은 채 그립을 놓는다
- **THEN** 공은 거의 속도 없이 그 자리에서 떨어진다

### Requirement: 릴리스 속도 평균화
The system SHALL compute the release velocity from the controller motion over the final 0.1 seconds before release, not from the single instant of release, for every correction level except raw physics.

#### Scenario: 손 떨림이 있는 던지기
- **WHEN** 릴리스 직전 0.1초 동안 컨트롤러 속도가 짧게 크게 튄다
- **THEN** 공의 초기 속도는 그 구간의 평균에서 계산되어 튄 값의 영향이 줄어든다

### Requirement: 던지기 보정 4단계
The system SHALL provide four throw correction levels: raw physics, weak, medium, and strong. Raw physics SHALL apply the controller velocity at the moment of release unchanged. Weak SHALL apply only velocity averaging. Medium and strong SHALL additionally pull the ball velocity toward the lane direction, with strong pulling more than medium.

#### Scenario: 순수 물리
- **WHEN** 보정 단계가 순수 물리이고 플레이어가 공을 놓는다
- **THEN** 공의 초기 속도는 놓는 순간의 컨트롤러 속도와 같다

#### Scenario: 중간과 강함의 차이
- **WHEN** 같은 방향과 같은 속도로 비스듬히 던진다
- **THEN** 강함에서의 공 진행 방향이 중간에서보다 레인 방향에 더 가깝다

#### Scenario: 보정은 속력을 키우지 않는다
- **WHEN** 중간 또는 강함 단계에서 공을 던진다
- **THEN** 공의 초기 속력은 보정 전의 속력을 넘지 않는다

### Requirement: 기본 보정 단계
The system SHALL use the weak correction level by default when the player has not chosen a level.

#### Scenario: 처음 실행한다
- **WHEN** 플레이어가 보정 단계를 고르지 않고 게임을 시작한다
- **THEN** 던지기 보정은 약함으로 적용된다

### Requirement: 보정 단계는 시작 전에 고른다
The system SHALL apply the correction level chosen before the game starts to every throw in that game.

#### Scenario: 단계를 고르고 시작
- **WHEN** 플레이어가 시작 전에 강함을 고르고 게임을 시작한다
- **THEN** 그 게임의 모든 던지기에 강함이 적용된다

### Requirement: 던지기 위치 제한
The system SHALL allow the ball to be released only from behind the foul line and SHALL prevent the player from reaching the pins by hand.

#### Scenario: 파울 라인을 넘어 놓는다
- **WHEN** 플레이어의 손이 파울 라인을 넘은 상태에서 공을 놓는다
- **THEN** 공은 레인에 던져지지 않고 플레이어 쪽 공 반환 위치로 돌아간다

### Requirement: 던진 공의 재공급
The system SHALL make a ball available to grab again after each throw is resolved.

#### Scenario: 투구가 끝난다
- **WHEN** 한 번의 투구 결과가 확정된다
- **THEN** 플레이어가 집을 수 있는 공이 던지는 위치 근처에 다시 나타난다

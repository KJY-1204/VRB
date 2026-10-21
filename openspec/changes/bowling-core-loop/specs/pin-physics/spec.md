# Spec Delta

## Purpose

핀 10개의 배치와 물리 거동, 투구 후 쓰러진 핀 수 판정을 정의한다. 실제 볼링과 비슷한 충돌 느낌을 주고, 판정은 핀이 안정된 뒤에 한 번만 확정한다.

## ADDED Requirements

### Requirement: 핀 10개 정삼각형 배치
The system SHALL place ten pins at the end of the lane in the standard triangular formation, with one pin at the front and rows of two, three, and four pins behind it.

#### Scenario: 새 게임 시작
- **WHEN** 게임이 시작된다
- **THEN** 핀 10개가 삼각형으로 서 있고 모두 움직이지 않는 상태이다

### Requirement: 실제 볼링에 가까운 치수와 질량비
The system SHALL size and weigh the pins and ball in proportion to real bowling equipment so that a rolling ball scatters pins plausibly.

#### Scenario: 정면으로 맞는다
- **WHEN** 공이 맨 앞 핀의 정면을 충분한 속도로 맞힌다
- **THEN** 앞 핀이 쓰러지고 뒤쪽 핀들에 충돌이 전달된다

#### Scenario: 약하게 맞는다
- **WHEN** 공이 매우 느린 속도로 핀에 닿는다
- **THEN** 핀이 약간 흔들리거나 쓰러지지 않을 수 있다

### Requirement: 빠른 공의 통과 방지
The system MUST NOT let a fast-moving ball pass through a pin without colliding.

#### Scenario: 빠르게 던진다
- **WHEN** 플레이어가 빠른 속도로 공을 던져 핀 위치에 도달한다
- **THEN** 공은 핀과 충돌하며 그냥 통과하지 않는다

### Requirement: 핀이 멈춘 뒤 판정
The system SHALL determine the number of fallen pins only after all pins have come to rest, or after a maximum wait time has elapsed, and SHALL determine it once per throw.

#### Scenario: 모든 핀이 정지
- **WHEN** 투구 후 모든 핀의 움직임이 일정 시간 동안 기준 이하로 유지된다
- **THEN** 시스템은 쓰러진 핀 수를 확정한다

#### Scenario: 핀이 계속 움직임
- **WHEN** 최대 대기 시간이 지났는데도 일부 핀이 계속 움직인다
- **THEN** 시스템은 그 시점의 상태로 쓰러진 핀 수를 확정한다

### Requirement: 쓰러짐 기준
The system SHALL count a pin as fallen when it is tilted beyond a threshold angle from upright, or when it has left the pin deck (dropped into the pit, gone past either edge of the lane, or ended down in a gutter). A pin that was only pushed along the deck and is still standing SHALL NOT be counted, however far it moved.

#### Scenario: 완전히 눕는다
- **WHEN** 핀이 기준 각도보다 많이 기울어진 채 멈춘다
- **THEN** 그 핀은 쓰러진 것으로 센다

#### Scenario: 기대어 서 있다
- **WHEN** 핀이 기준 각도 이내로 기울어진 채 서 있다
- **THEN** 그 핀은 서 있는 것으로 센다

#### Scenario: 피트로 떨어진다
- **WHEN** 핀이 레인 끝의 피트 영역에 들어간다
- **THEN** 그 핀은 쓰러진 것으로 센다

#### Scenario: 레인 밖으로 나간다
- **WHEN** 핀이 서 있는 채로 레인 가장자리를 벗어나거나 거터 안으로 들어간다
- **THEN** 그 핀은 쓰러진 것으로 센다

#### Scenario: 밀려났지만 서 있다
- **WHEN** 핀이 원래 자리에서 멀리 밀려났지만 덱 위에 서 있다
- **THEN** 그 핀은 서 있는 것으로 센다

### Requirement: 투구 후 핀 리셋
The system SHALL, after each throw result has been shown, set up the pins for the next throw as a pinsetter does: a new frame (or a fresh rack in the tenth frame) gets all ten pins on their initial spots, and between the two balls of a frame the fallen pins are cleared away while the standing ones are put back upright on their spots.

#### Scenario: 새 프레임
- **WHEN** 스트라이크이거나 한 프레임의 마지막 공의 결과가 표시된 뒤 다음 투구를 준비한다
- **THEN** 10개 핀이 모두 처음 배치로 돌아가 정지 상태가 된다

#### Scenario: 같은 프레임의 두 번째 공
- **WHEN** 첫 공이 핀을 일부만 쓰러뜨린 결과가 표시된 뒤 다음 투구를 준비한다
- **THEN** 쓰러진 핀은 치워지고, 서 있던 핀은 원래 자리에 똑바로 선 채 남는다

### Requirement: 충돌 효과음
The system SHALL play a sound effect when the ball rolls on the lane and when pins are hit.

#### Scenario: 공이 굴러간다
- **WHEN** 공이 레인 위를 굴러간다
- **THEN** 굴러가는 소리가 난다

#### Scenario: 핀이 맞는다
- **WHEN** 공이 핀에 부딪히거나 핀끼리 부딪힌다
- **THEN** 핀이 맞는 소리가 난다

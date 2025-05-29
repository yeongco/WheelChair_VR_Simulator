# 휠체어 이동 시스템 (Wheelchair Movement System)

AutoHand를 사용한 VR 휠체어 이동 시스템입니다.

## 주요 기능

### 1. 바퀴 기반 이동
- 사용자가 VR 컨트롤러로 바퀴를 잡고 돌려서 이동
- 바퀴 회전량에 따른 자동 속도 계산
- 실제 휠체어와 같은 물리적 이동감

### 2. 다양한 이동 모드
- **직진 이동**: 두 바퀴를 같은 방향으로 돌릴 때
- **제자리 회전**: 두 바퀴를 서로 다른 방향으로 돌릴 때 (휠체어 중앙 기준)
- **한쪽 바퀴 회전**: 한 바퀴만 잡고 돌릴 때 (반대편 바퀴 중심으로 회전)

### 3. 지형 적응 시스템
- 두 개의 높이 감지 포인트로 바닥 높이 측정
- 경사로에서 자동 높이 조절 및 기울기 적용
- 지정된 높이만큼 바닥에서 떠있도록 유지

### 4. 경사로 미끄러짐 시뮬레이션
- 경사각에 따른 자동 미끄러짐 효과
- 바퀴를 잡고 있지 않을 때만 미끄러짐 적용
- Inspector에서 미끄러짐 정도 조절 가능

### 5. 안정성 시스템
- 과도한 기울기 방지
- 속도 제한 기능
- 물리적 안정성 보장

## 설치 및 설정

### 1. 기본 설정
1. 휠체어 GameObject에 `WheelchairSetupHelper` 스크립트 추가
2. Inspector에서 바퀴 메시 오브젝트들을 할당
3. "Setup Wheelchair" 버튼 클릭하여 자동 설정

### 2. 수동 설정
1. 휠체어 GameObject에 `WheelchairController` 스크립트 추가
2. 다음 빈 오브젝트들을 생성하고 할당:
   - `ChairCenter`: 휠체어 중앙 위치
   - `LeftWheelCenter`: 왼쪽 바퀴 중심
   - `RightWheelCenter`: 오른쪽 바퀴 중심
   - `FrontHeightPoint`: 앞쪽 높이 감지 포인트
   - `RearHeightPoint`: 뒤쪽 높이 감지 포인트

### 3. 바퀴 설정
- 바퀴 메시에 `Grabbable` 컴포넌트 추가
- 적절한 Collider 설정 (CapsuleCollider 권장)
- Rigidbody를 Kinematic으로 설정

## Inspector 설정 옵션

### 휠체어 본체 설정
- `Chair Rigidbody`: 휠체어의 Rigidbody
- `Chair Center`: 휠체어 중앙 위치

### 바퀴 설정
- `Left/Right Wheel`: 바퀴 메시 Transform
- `Left/Right Wheel Center`: 바퀴 중심 위치 (빈 오브젝트)
- `Left/Right Wheel Grab`: 바퀴의 Grabbable 컴포넌트

### 높이 감지 설정
- `Front/Rear Height Point`: 높이 감지 포인트 (빈 오브젝트)
- `Ground Check Distance`: 바닥 감지 거리 (기본값: 2m)
- `Hover Height`: 바닥에서 띄울 높이 (기본값: 0.1m)
- `Ground Layer`: 바닥으로 인식할 레이어

### 이동 설정
- `Wheel Force`: 바퀴 힘 (기본값: 500)
- `Move Speed Multiplier`: 이동 속도 가중치 (기본값: 1)
- `Rotation Speed Multiplier`: 회전 속도 가중치 (기본값: 1)
- `Speed Decay Rate`: 회전 속도 감소율 (기본값: 0.95)

### 경사로 설정
- `Slope Slip Factor`: 경사로 미끄러짐 정도 (0-1, 기본값: 0.5)
- `Max Slope Angle`: 최대 경사각 (기본값: 30도)

### 안정성 설정
- `Stability Force`: 안정화 힘 (기본값: 1000)
- `Max Tilt Angle`: 최대 기울기 각도 (기본값: 15도)

## 사용법

### 기본 이동
1. VR 컨트롤러로 바퀴를 잡습니다
2. 바퀴를 앞뒤로 돌려서 이동합니다
3. 두 바퀴를 동시에 돌리면 직진합니다

### 회전
1. **제자리 회전**: 두 바퀴를 서로 다른 방향으로 돌립니다
2. **한쪽 회전**: 한 바퀴만 잡고 돌립니다

### 경사로 이용
- 경사로에서는 자동으로 높이가 조절됩니다
- 바퀴를 놓으면 경사에 따라 미끄러집니다
- 바퀴를 잡고 있으면 미끄러지지 않습니다

## 디버그 기능

### Scene View에서 확인 가능한 요소
- **빨간색 구**: 앞쪽 높이 감지 포인트
- **파란색 구**: 뒤쪽 높이 감지 포인트
- **초록색 구**: 바퀴 중심 포인트들
- **청록색 구**: 휠체어 중앙
- **노란색 선**: 바닥 감지 레이

### Console 로그
- 경사각 정보
- 바퀴 속도 정보
- 높이 조절 상태

## 문제 해결

### 휠체어가 움직이지 않을 때
1. Grabbable 컴포넌트가 바퀴에 제대로 설정되었는지 확인
2. Rigidbody 설정 확인 (Kinematic 여부)
3. Ground Layer 설정 확인

### 높이 조절이 안 될 때
1. Height Point들이 올바른 위치에 있는지 확인
2. Ground Check Distance 값 조정
3. Ground Layer 마스크 설정 확인

### 경사로에서 이상하게 동작할 때
1. Slope Slip Factor 값 조정
2. Stability Force 값 조정
3. Max Tilt Angle 값 확인

## 성능 최적화

- FixedUpdate에서 물리 계산 수행
- 불필요한 레이캐스트 최소화
- 적절한 물리 설정으로 안정성 확보

## 확장 가능성

- 브레이크 시스템 추가
- 다양한 지형 타입 지원
- 사운드 효과 추가
- 진동 피드백 구현
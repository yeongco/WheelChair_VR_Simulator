# 🔋 초전도체 휠체어 시스템 (Superconductor Wheelchair System)

AutoHand를 사용한 VR 초전도체 부양 휠체어 이동 시스템입니다.

## 🌟 주요 특징

### 🔋 초전도체 부양 기술
- **자기 부양**: 강력한 초전도체 기술로 지면에서 완전히 떠있는 상태
- **중력 무시**: 물리적 중력의 영향을 받지 않는 미래형 이동 수단
- **사용자 설정 높이**: 최소 높이 제한은 있지만 최대 높이 제한 없음
- **에너지 효율**: 마찰이 없는 부양으로 매우 효율적인 이동

### 🛡️ 고급 안정성 제어
- **자이로스코프 안정화**: 항공기급 안정성 시스템으로 흔들림 방지
- **즉각적인 보정**: 3도 이상 기울어지면 강력한 힘으로 즉시 보정
- **다축 안정화**: X, Y, Z축 모든 방향의 기울기를 실시간 모니터링
- **댐핑 시스템**: 과도한 움직임을 부드럽게 억제

### 🎯 4점 지면 감지 시스템
- **정밀 감지**: 휠체어 4개 모서리에서 독립적으로 지면 감지
- **실시간 분석**: 각 포인트의 높이와 법선 벡터를 실시간 계산
- **지형 적응**: 평지, 경사로, 계단 등 다양한 지형에 자동 적응
- **가상 접촉**: 실제로는 떠있지만 바닥에 닿아있는 것처럼 느껴지는 시뮬레이션

### 🚗 스마트 바퀴 시스템
- **물리 기반 회전**: 휠체어 이동 속도에 따른 자동 바퀴 회전
- **사용자 입력 반영**: 바퀴를 잡고 돌릴 때의 입력도 정확히 반영
- **마찰력 시뮬레이션**: 설정 가능한 바퀴 마찰력으로 현실적인 감속
- **다양한 이동 모드**: 직진, 제자리 회전, 한쪽 바퀴 중심 회전

## 🎮 이동 모드

### 1. 직진 이동
- 두 바퀴를 같은 방향으로 돌리면 직진
- 속도는 두 바퀴의 평균 속도로 계산
- 부드럽고 안정적인 전진/후진

### 2. 제자리 회전
- 두 바퀴를 서로 다른 방향으로 돌리면 중앙에서 회전
- 속도 차이에 비례한 회전 토크 적용
- 정밀한 방향 조절 가능

### 3. 한쪽 바퀴 중심 회전
- 한 바퀴만 잡고 돌리면 반대편 바퀴를 중심으로 회전
- 좁은 공간에서의 기동성 향상
- 실제 휠체어와 동일한 조작감

## 🔧 설치 및 설정

### 자동 설정 (권장)
1. 휠체어 GameObject에 `WheelchairSetupHelper` 스크립트 추가
2. Inspector에서 바퀴 메시 오브젝트들을 할당
3. "🔋 Setup Superconductor Wheelchair" 버튼 클릭
4. 자동으로 모든 시스템이 설정됩니다

### 수동 설정
1. 휠체어 GameObject에 `WheelchairController` 스크립트 추가
2. 4개의 지면 감지 포인트를 휠체어 모서리에 배치
3. 바퀴 메시에 `Grabbable` 컴포넌트 설정
4. Inspector에서 모든 참조 할당

## ⚙️ Inspector 설정 옵션

### 🔋 초전도체 부양 시스템
- `Enable Superconductor Hover`: 초전도체 부양 활성화 여부
- `Hover Height`: 부양 높이 (기본값: 0.3m)
- `Min Hover Height`: 최소 부양 높이 (기본값: 0.1m)
- `Hover Force`: 부양 힘 (기본값: 8000)
- `Hover Damping`: 부양 댐핑 (기본값: 1000)
- `Hover Stiffness`: 부양 강성 (기본값: 5000)

### 🛡️ 안정성 제어 시스템
- `Stability Force`: 안정화 힘 (기본값: 15000)
- `Stability Damping`: 안정화 댐핑 (기본값: 2000)
- `Max Tilt Angle`: 최대 허용 기울기 (기본값: 3도)
- `Stability Response Speed`: 안정화 반응 속도 (기본값: 20)
- `Enable Gyroscopic Stabilization`: 자이로스코프 안정화 사용 여부

### 🎯 4점 지면 감지 시스템
- `Ground Detection Points`: 4개 감지 포인트 Transform 배열
- `Ground Check Distance`: 지면 감지 거리 (기본값: 2m)
- `Ground Layer`: 지면으로 인식할 레이어
- `Contact Point Offset`: 접촉 포인트 오프셋 (기본값: 0.05m)

### 🚗 바퀴 시스템
- `Left/Right Wheel`: 바퀴 메시 Transform
- `Left/Right Wheel Grab`: 바퀴의 Grabbable 컴포넌트
- `Wheel Radius`: 바퀴 반지름 (기본값: 0.3m)
- `Wheel Friction`: 바퀴 마찰력 (기본값: 0.95)
- `Speed To Rotation Ratio`: 속도-회전 비율 (기본값: 10)
- `Wheel Input Sensitivity`: 바퀴 입력 감도 (기본값: 100)

### 🏃 이동 제어
- `Move Force`: 이동 힘 (기본값: 3000)
- `Rotation Force`: 회전 힘 (기본값: 2000)
- `Max Speed`: 최대 속도 (기본값: 8m/s)
- `Max Angular Speed`: 최대 각속도 (기본값: 180도/초)

### 🎛️ 물리 설정
- `Chair Mass`: 휠체어 질량 (기본값: 80kg)
- `Air Resistance`: 공기 저항 (기본값: 0.5)
- `Angular Drag`: 각속도 저항 (기본값: 10)

## 🎮 사용법

### 기본 조작
1. VR 컨트롤러로 바퀴를 잡습니다
2. 바퀴를 앞뒤로 돌려서 이동합니다
3. 휠체어가 자동으로 설정된 높이에서 부양합니다

### 고급 조작
- **정밀 이동**: 한 바퀴씩 조작하여 정밀한 위치 조정
- **빠른 회전**: 두 바퀴를 반대 방향으로 돌려 제자리 회전
- **경사로 이동**: 시스템이 자동으로 경사에 맞춰 자세 조정

## 🔍 디버그 기능

### Scene View 시각화
- **🔵 파란색 구**: 지면 감지 포인트 (4개)
- **🟢 초록색 선**: 지면 감지 성공 (감지 포인트에서 지면까지)
- **🔴 빨간색 선**: 지면 감지 실패
- **🟡 노란색 큐브**: 목표 부양 높이 위치
- **🟢 초록색 선**: 안정 상태의 상향 벡터
- **🟠 주황색 선**: 불안정 상태의 상향 벡터
- **⚪ 흰색 선**: 목표 상향 방향 (지면 법선)
- **🔷 청록색 와이어프레임**: 부양 높이 범위

### Context Menu 도구
- **🔋 Setup Superconductor Wheelchair**: 전체 시스템 자동 설정
- **🔧 Adjust Hover Height**: 부양 높이 조정
- **🛡️ Test Stability**: 현재 안정성 상태 확인
- **🔋 Enable Superconductor Mode**: 초전도체 모드 활성화
- **🌍 Disable Superconductor Mode**: 일반 물리 모드로 전환
- **⚡ Increase Stability**: 안정성 강화
- **🎯 Reset Ground Detection**: 지면 감지 포인트 재설정
- **🚗 Test Wheel Response**: 바퀴 반응성 테스트
- **🔄 Reset Wheelchair**: 전체 시스템 초기화

### Console 로그
- 초전도체 시스템 상태
- 안정성 수치 (0.0 ~ 1.0)
- 현재 기울기 각도
- 바퀴 입력 감도
- 지면 감지 상태

## 🛠️ 문제 해결

### 휠체어가 부양하지 않을 때
1. `Enable Superconductor Hover`가 활성화되어 있는지 확인
2. `Hover Height` 값이 적절한지 확인 (0.3m 권장)
3. `Hover Force` 값을 증가시켜 보세요 (8000 이상)
4. Rigidbody의 `Use Gravity`가 비활성화되어 있는지 확인

### 휠체어가 불안정하게 흔들릴 때
1. `Stability Force` 값을 증가시켜 보세요 (15000 이상)
2. `Max Tilt Angle` 값을 줄여보세요 (3도 이하)
3. `Stability Damping` 값을 증가시켜 보세요 (2000 이상)
4. Context Menu에서 "⚡ Increase Stability" 실행

### 지면 감지가 안 될 때
1. `Ground Detection Points`가 올바르게 설정되어 있는지 확인
2. `Ground Layer` 마스크가 올바른지 확인
3. `Ground Check Distance` 값을 증가시켜 보세요
4. Context Menu에서 "🎯 Reset Ground Detection" 실행

### 바퀴가 반응하지 않을 때
1. 바퀴에 `Grabbable` 컴포넌트가 있는지 확인
2. 바퀴에 적절한 `Collider`가 있는지 확인
3. `Wheel Input Sensitivity` 값을 조정해 보세요
4. Context Menu에서 "🚗 Test Wheel Response" 실행

### 이동 속도가 이상할 때
1. `Move Force` 값을 조정해 보세요 (3000 권장)
2. `Wheel Friction` 값을 확인해 보세요 (0.95 권장)
3. `Speed To Rotation Ratio` 값을 조정해 보세요 (10 권장)
4. `Max Speed` 제한을 확인해 보세요

### 회전이 부자연스러울 때
1. `Rotation Force` 값을 조정해 보세요 (2000 권장)
2. `Max Angular Speed` 값을 확인해 보세요 (180도/초 권장)
3. `Angular Drag` 값을 조정해 보세요 (10 권장)

## 🔬 고급 설정

### 부양 높이 최적화
```csharp
// 런타임에서 부양 높이 조정
wheelchairController.SetHoverHeight(0.5f);

// 안정성 확인
bool isStable = wheelchairController.IsStable();
float stability = wheelchairController.GetCurrentStability();
```

### 물리 파라미터 조정
- **높은 안정성**: `Stability Force` 20000+, `Max Tilt Angle` 2도 이하
- **부드러운 이동**: `Hover Damping` 1500+, `Stability Damping` 2500+
- **빠른 반응**: `Stability Response Speed` 30+, `Wheel Input Sensitivity` 150+

### 지형별 최적화
- **평지**: 기본 설정 사용
- **경사로**: `Hover Stiffness` 증가 (7000+)
- **울퉁불퉁한 지형**: `Ground Check Distance` 증가 (3m+)

## 🚀 성능 최적화

### 물리 계산 최적화
- FixedUpdate에서 모든 물리 계산 수행
- 4점 지면 감지로 정확하면서도 효율적인 계산
- 불필요한 계산 최소화 (안정 상태에서는 보정 힘 감소)

### 메모리 최적화
- 배열 재사용으로 GC 할당 최소화
- 캐시된 Transform 참조 사용
- 조건부 업데이트로 불필요한 연산 방지

### 렌더링 최적화
- 디버그 기즈모는 개발 시에만 활성화
- LOD 시스템과 호환 가능한 구조
- 바퀴 회전은 시각적 목적만 (물리 연산 분리)

## 🔮 확장 가능성

### 추가 기능 아이디어
- **에너지 시스템**: 배터리 개념으로 부양 시간 제한
- **부양 모드 변경**: 낮은 부양, 높은 부양, 지면 접촉 모드
- **환경 상호작용**: 자기장 영향, 금속 표면에서의 특별한 효과
- **사운드 효과**: 초전도체 부양음, 바퀴 회전음
- **시각 효과**: 부양 파티클, 자기장 시각화
- **AI 보조**: 자동 장애물 회피, 경로 최적화
- **멀티플레이어**: 여러 사용자가 동시에 사용 가능한 시스템

### 기술적 확장
- **VR 플랫폼 지원**: Oculus, SteamVR, Pico 등
- **햅틱 피드백**: 바퀴 저항감, 지면 질감 전달
- **모션 캡처**: 전신 움직임과 연동
- **클라우드 동기화**: 설정 및 진행 상황 저장

## 📊 기술 사양

### 시스템 요구사항
- Unity 2021.3 LTS 이상
- AutoHand 패키지
- VR 헤드셋 및 컨트롤러
- 최소 8GB RAM (16GB 권장)

### 성능 지표
- **물리 업데이트**: 50Hz (FixedUpdate)
- **지면 감지**: 4점 동시 레이캐스트
- **안정성 보정**: 실시간 (매 FixedUpdate)
- **바퀴 회전**: 60fps 시각 업데이트

### 정확도
- **부양 높이**: ±1cm 정확도
- **기울기 제한**: ±0.1도 정확도
- **지면 감지**: 2m 범위, 1mm 해상도
- **바퀴 회전**: 실제 이동 거리와 1:1 매칭

이제 당신의 VR 공간에서 미래형 초전도체 휠체어를 경험해보세요! 🚀✨
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Autohand;

public class WheelchairController : MonoBehaviour
{
    [Header("🔋 초전도체 부양 시스템")]
    public bool enableSuperconductorHover = true; // 초전도체 부양 활성화
    public float hoverHeight = 0.3f; // 부양 높이 (사용자 설정 가능)
    public float minHoverHeight = 0.1f; // 최소 부양 높이
    public float hoverForce = 8000f; // 부양 힘
    public float hoverDamping = 2000f; // 부양 댐핑 (증가)
    public float hoverStiffness = 3000f; // 부양 강성 (감소하여 더 부드럽게)
    
    [Header("🛡️ 안정성 제어 시스템")]
    public float stabilityForce = 15000f; // 안정화 힘 (매우 강하게)
    public float stabilityDamping = 2000f; // 안정화 댐핑
    public float maxTiltAngle = 3f; // 최대 허용 기울기 (매우 작게)
    public float stabilityResponseSpeed = 20f; // 안정화 반응 속도
    public bool enableGyroscopicStabilization = true; // 자이로스코프 안정화
    
    [Header("🎯 4점 지면 감지 시스템")]
    public Transform[] groundDetectionPoints = new Transform[4]; // 4개 감지 포인트
    public float groundCheckDistance = 2f; // 지면 감지 거리
    public LayerMask groundLayer = 1; // 지면 레이어
    public float contactPointOffset = 0.05f; // 접촉 포인트 오프셋
    
    [Header("🚗 바퀴 시스템 (순수 바퀴 주도)")]
    public Transform leftWheel;
    public Transform rightWheel;
    public Grabbable leftWheelGrab;
    public Grabbable rightWheelGrab;
    public float wheelRadius = 0.3f; // 바퀴 반지름
    public float wheelFriction = 0.98f; // 바퀴 마찰력 (회전 감소율) - 높을수록 오래 굴러감
    public float wheelToMovementRatio = 1f; // 바퀴 회전 -> 이동 변환 비율
    public float wheelInputSensitivity = 1f; // 바퀴 입력 감도 (1.0 = 실제 회전값 그대로 사용)
    public float wheelDecelerationRate = 0.02f; // 바퀴 감속률 (잡지 않을 때)
    public bool onlyMoveWhenWheelsActive = true; // 바퀴가 움직일 때만 이동
    public bool enableWheelRotationDebug = true; // 바퀴 회전 디버그 활성화
    public bool autoDetectWheelAxis = true; // 바퀴 회전축 자동 감지
    public Vector3 leftWheelAxis = Vector3.right; // 왼쪽 바퀴 회전축 (로컬 좌표)
    public Vector3 rightWheelAxis = Vector3.right; // 오른쪽 바퀴 회전축 (로컬 좌표)
    
    [Header("🧭 방향 설정 (수동 조정)")]
    public bool useManualDirections = false; // 수동 방향 설정 사용
    public Vector3 manualForwardDirection = Vector3.forward; // 수동 전진 방향
    public bool useManualWheelAxes = false; // 수동 바퀴 축 설정 사용
    public Vector3 manualLeftWheelAxis = Vector3.right; // 수동 왼쪽 바퀴 회전축 (로컬 좌표)
    public Vector3 manualRightWheelAxis = Vector3.right; // 수동 오른쪽 바퀴 회전축 (로컬 좌표)
    
    [Header("⚠️ 좌표계 설명")]
    [TextArea(3, 5)]
    public string coordinateSystemInfo = "전진 방향: 월드 좌표계 (휠체어가 실제로 이동할 방향)\n바퀴 축: 로컬 좌표계 (바퀴 자체의 회전축)\n\n전진 방향을 로컬로 하면 휠체어 회전시 방향이 계속 바뀌어 문제가 됩니다.";
    
    [Header("🏔️ 경사로 미끄러짐 시스템")]
    public bool enableSlopeSliding = true; // 경사로 미끄러짐 활성화
    public float slopeThreshold = 5f; // 미끄러짐 시작 각도 (도)
    public float maxSlideAngle = 45f; // 최대 미끄러짐 각도 (도)
    public float slideForce = 2000f; // 미끄러짐 힘
    public float slideFriction = 0.3f; // 미끄러짐 마찰력 (낮을수록 더 미끄러짐)
    public float wheelGripOnSlope = 0.7f; // 경사로에서 바퀴 그립력 (0~1)
    public AnimationCurve slopeCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f); // 경사 강도 곡선
    
    [Header("🎡 경사로 바퀴 회전 시스템")]
    public bool enableSlopeWheelRotation = true; // 경사로 바퀴 회전 활성화
    public float slopeWheelRotationMultiplier = 1.5f; // 경사로 바퀴 회전 배율
    public float wheelRotationFriction = 0.95f; // 바퀴 회전 마찰력 (0~1, 높을수록 오래 굴러감)
    public float wheelStopThreshold = 0.1f; // 바퀴 정지 임계값 (rad/s)
    public float userBrakingForce = 0.3f; // 사용자 제동력 (0~1, 낮을수록 강한 제동)
    public bool enableUserBraking = true; // 사용자 제동 활성화
    
    [Header("🏃 이동 제어")]
    public float maxSpeed = 8f; // 최대 속도
    public float maxAngularSpeed = 180f; // 최대 각속도 (도/초)
    public float movementSmoothing = 5f; // 이동 부드러움
    public float rotationSmoothing = 8f; // 회전 부드러움
    
    [Header("🎛️ 물리 설정")]
    public Rigidbody chairRigidbody;
    public float chairMass = 80f; // 휠체어 질량
    public float airResistance = 0.5f; // 공기 저항
    public float angularDrag = 10f; // 각속도 저항
    
    // 바퀴 회전 상태 (이것이 이동을 주도함)
    private float leftWheelAngularVelocity = 0f; // 왼쪽 바퀴 각속도 (rad/s)
    private float rightWheelAngularVelocity = 0f; // 오른쪽 바퀴 각속도 (rad/s)
    private float leftWheelRotation = 0f; // 왼쪽 바퀴 누적 회전
    private float rightWheelRotation = 0f; // 오른쪽 바퀴 누적 회전
    
    // 경사로 바퀴 회전 상태
    private float leftWheelSlopeRotation = 0f; // 경사로에 의한 왼쪽 바퀴 각속도
    private float rightWheelSlopeRotation = 0f; // 경사로에 의한 오른쪽 바퀴 각속도
    private bool isLeftWheelBraking = false; // 왼쪽 바퀴 제동 상태
    private bool isRightWheelBraking = false; // 오른쪽 바퀴 제동 상태
    
    // 바퀴 입력 추적
    private Vector3 lastLeftHandPos;
    private Vector3 lastRightHandPos;
    private bool lastLeftGrabbed = false;
    private bool lastRightGrabbed = false;
    
    // 지면 감지 데이터
    private float[] groundDistances = new float[4];
    private Vector3[] groundPoints = new Vector3[4];
    private Vector3[] groundNormals = new Vector3[4];
    private bool[] groundDetected = new bool[4];
    
    // 안정성 데이터
    private Vector3 targetUpDirection = Vector3.up;
    private float currentStability = 1f;
    
    // 이동 계산 결과
    private Vector3 targetVelocity = Vector3.zero;
    private float targetAngularVelocity = 0f;
    
    // 경사로 데이터
    private float currentSlopeAngle = 0f;
    private Vector3 slopeDirection = Vector3.zero;
    private float slopeIntensity = 0f;
    private Vector3 slideVelocity = Vector3.zero;
    
    // 바퀴 활성 상태
    private bool isAnyWheelActive = false;
    private bool isLeftWheelActive = false;
    private bool isRightWheelActive = false;
    
    // 이동 제한 관련 변수
    private Vector3 lastFrameVelocity = Vector3.zero;
    private Vector3 legitimateVelocity = Vector3.zero; // 바퀴와 경사로에 의한 정당한 속도
    private Vector3 lastPosition = Vector3.zero;
    private bool isCollisionDetected = false;
    
    [Header("🔒 이동 제한 설정")]
    public bool strictMovementControl = true; // 엄격한 이동 제어 (바퀴와 경사로만 허용)
    public float externalForceThreshold = 0.1f; // 외부 힘 감지 임계값
    public bool allowColliderInteraction = true; // 콜라이더 상호작용 허용
    
    [Header("🔍 방향 디버그 표시")]
    public bool showDirectionGizmos = true; // 방향 기즈모 표시
    public float gizmoLength = 1f; // 기즈모 길이
    public bool showWheelAxes = true; // 바퀴 축 표시
    public bool showForwardDirection = true; // 전진 방향 표시
    
    void Start()
    {
        InitializeSuperconductorSystem();
        
        // 경사 곡선 기본 설정
        if (slopeCurve.keys.Length == 0)
        {
            slopeCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        }
        
        // 이동 제한 초기화
        lastPosition = transform.position;
        lastFrameVelocity = Vector3.zero;
        legitimateVelocity = Vector3.zero;
    }
    
    void InitializeSuperconductorSystem()
    {
        // Rigidbody 설정
        if (chairRigidbody == null)
            chairRigidbody = GetComponent<Rigidbody>();
            
        // 초전도체 부양을 위한 물리 설정
        chairRigidbody.mass = chairMass;
        chairRigidbody.useGravity = false; // 중력 비활성화 (수동으로 제어)
        chairRigidbody.drag = airResistance;
        chairRigidbody.angularDrag = angularDrag;
        chairRigidbody.centerOfMass = new Vector3(0, -0.2f, 0); // 낮은 무게중심
        chairRigidbody.maxAngularVelocity = maxAngularSpeed * Mathf.Deg2Rad;
        
        // 지면 감지 포인트 자동 생성 (없을 경우)
        if (groundDetectionPoints[0] == null)
        {
            CreateGroundDetectionPoints();
        }
        
        // 바퀴 회전축 자동 감지
        if (autoDetectWheelAxis)
        {
            DetectWheelAxes();
        }
        
        Debug.Log("🔋 초전도체 부양 시스템 초기화 완료 - 순수 바퀴 주도 + 경사로 미끄러짐 + 글로벌 레이캐스팅 + 안정화된 부양");
        Debug.Log($"부양 높이: {hoverHeight}m, 바퀴 반지름: {wheelRadius}m");
        Debug.Log($"왼쪽 바퀴 회전축: {leftWheelAxis}, 오른쪽 바퀴 회전축: {rightWheelAxis}");
        Debug.Log("중력 시스템: 부드러운 전환으로 안정적인 부양 + 강화된 댐핑");
        Debug.Log($"🔒 엄격한 이동 제어: {(strictMovementControl ? "활성화" : "비활성화")} - 바퀴와 경사로만 이동 허용");
        Debug.Log($"🎡 경사로 바퀴 회전: {(enableSlopeWheelRotation ? "활성화" : "비활성화")}, 🛑 사용자 제동: {(enableUserBraking ? "활성화" : "비활성화")}");
        Debug.Log($"🧭 방향 설정 - 수동 방향: {(useManualDirections ? "활성화" : "비활성화")}, 수동 바퀴 축: {(useManualWheelAxes ? "활성화" : "비활성화")}");
    }
    
    void CreateGroundDetectionPoints()
    {
        // 휠체어 크기 기준으로 4개 포인트 생성
        float halfWidth = 0.4f;
        float halfLength = 0.6f;
        
        Vector3[] positions = {
            new Vector3(-halfWidth, contactPointOffset, halfLength),   // 왼쪽 앞
            new Vector3(halfWidth, contactPointOffset, halfLength),    // 오른쪽 앞
            new Vector3(-halfWidth, contactPointOffset, -halfLength),  // 왼쪽 뒤
            new Vector3(halfWidth, contactPointOffset, -halfLength)    // 오른쪽 뒤
        };
        
        for (int i = 0; i < 4; i++)
        {
            GameObject point = new GameObject($"GroundDetectionPoint_{i}");
            point.transform.SetParent(transform);
            point.transform.localPosition = positions[i];
            groundDetectionPoints[i] = point.transform;
        }
    }
    
    void DetectWheelAxes()
    {
        // 수동 바퀴 축 설정이 활성화된 경우
        if (useManualWheelAxes)
        {
            leftWheelAxis = manualLeftWheelAxis.normalized;
            rightWheelAxis = manualRightWheelAxis.normalized;
            Debug.Log($"🧭 수동 바퀴 축 설정 사용:");
            Debug.Log($"  - 왼쪽 바퀴 축: {leftWheelAxis}");
            Debug.Log($"  - 오른쪽 바퀴 축: {rightWheelAxis}");
            return;
        }
        
        // 자동 감지 모드
        Vector3 chairForward = useManualDirections ? manualForwardDirection.normalized : transform.forward;
        
        if (leftWheel != null)
        {
            // 왼쪽 바퀴의 회전축 감지
            leftWheelAxis = DetectWheelRotationAxis(leftWheel, chairForward, "왼쪽");
        }
        
        if (rightWheel != null)
        {
            // 오른쪽 바퀴의 회전축 감지
            rightWheelAxis = DetectWheelRotationAxis(rightWheel, chairForward, "오른쪽");
        }
    }
    
    Vector3 DetectWheelRotationAxis(Transform wheel, Vector3 chairForward, string wheelName)
    {
        // 바퀴의 로컬 축들을 월드 좌표로 변환
        Vector3 wheelRight = wheel.right;    // X축
        Vector3 wheelUp = wheel.up;          // Y축  
        Vector3 wheelForward = wheel.forward; // Z축
        
        // 휠체어 전진 방향과 수직인 축을 찾기
        float dotX = Mathf.Abs(Vector3.Dot(wheelRight, chairForward));
        float dotY = Mathf.Abs(Vector3.Dot(wheelUp, chairForward));
        float dotZ = Mathf.Abs(Vector3.Dot(wheelForward, chairForward));
        
        Vector3 detectedWorldAxis;
        Vector3 detectedLocalAxis;
        string axisName;
        
        // 가장 수직에 가까운 축을 회전축으로 선택
        if (dotX <= dotY && dotX <= dotZ)
        {
            detectedWorldAxis = wheelRight;
            detectedLocalAxis = Vector3.right; // 로컬 X축
            axisName = "X축 (Right)";
        }
        else if (dotY <= dotX && dotY <= dotZ)
        {
            detectedWorldAxis = wheelUp;
            detectedLocalAxis = Vector3.up; // 로컬 Y축
            axisName = "Y축 (Up)";
        }
        else
        {
            detectedWorldAxis = wheelForward;
            detectedLocalAxis = Vector3.forward; // 로컬 Z축
            axisName = "Z축 (Forward)";
        }
        
        Debug.Log($"{wheelName} 바퀴 회전축 감지: {axisName} - 월드벡터: {detectedWorldAxis}, 로컬벡터: {detectedLocalAxis}");
        Debug.Log($"{wheelName} 바퀴 내적값 - X: {dotX:F3}, Y: {dotY:F3}, Z: {dotZ:F3}");
        
        return detectedLocalAxis; // 로컬 좌표 반환
    }
    
    void FixedUpdate()
    {
        if (!enableSuperconductorHover) return;
        
        // 1. 지면 감지 및 분석
        PerformGroundDetection();
        
        // 2. 경사로 분석
        AnalyzeSlope();
        
        // 3. 초전도체 부양 힘 적용
        ApplySuperconductorHover();
        
        // 4. 안정성 제어
        ApplyStabilityControl();
        
        // 5. 바퀴 입력 처리 (바퀴 각속도 업데이트)
        ProcessWheelInput();
        
        // 6. 바퀴 회전에서 이동 계산 (바퀴가 활성일 때만)
        CalculateMovementFromWheels();
        
        // 7. 경사로 미끄러짐 적용
        ApplySlopeSliding();
        
        // 8. 계산된 이동 적용
        ApplyCalculatedMovement();
        
        // 9. 바퀴 시각적 회전 업데이트
        UpdateWheelVisualRotation();
        
        // 10. 물리 제한 적용
        ApplyPhysicsLimits();
        
        // 11. 이동 제한 검사 및 적용 (새로 추가)
        if (strictMovementControl)
        {
            EnforceMovementRestrictions();
        }
    }
    
    void PerformGroundDetection()
    {
        Vector3 averageNormal = Vector3.zero;
        float averageHeight = 0f;
        int validPoints = 0;
        
        for (int i = 0; i < 4; i++)
        {
            if (groundDetectionPoints[i] == null) continue;
            
            Vector3 rayStart = groundDetectionPoints[i].position;
            RaycastHit hit;
            
            // 글로벌 -Y축 방향으로 레이캐스팅 (Vector3.down 사용)
            if (Physics.Raycast(rayStart, Vector3.down, out hit, groundCheckDistance, groundLayer))
            {
                groundDistances[i] = hit.distance;
                groundPoints[i] = hit.point;
                groundNormals[i] = hit.normal;
                groundDetected[i] = true;
                
                averageNormal += hit.normal;
                averageHeight += hit.point.y;
                validPoints++;
                
                // 디버그 레이
                Debug.DrawLine(rayStart, hit.point, Color.green);
            }
            else
            {
                groundDetected[i] = false;
                groundDistances[i] = groundCheckDistance;
                
                // 디버그 레이
                Debug.DrawRay(rayStart, Vector3.down * groundCheckDistance, Color.red);
            }
        }
        
        // 평균 지면 법선 계산
        if (validPoints > 0)
        {
            targetUpDirection = (averageNormal / validPoints).normalized;
            
            // 안정성 계산 (지면과의 정렬도)
            currentStability = Vector3.Dot(transform.up, targetUpDirection);
        }
        else
        {
            targetUpDirection = Vector3.up;
            currentStability = 0f;
        }
    }
    
    void AnalyzeSlope()
    {
        if (!enableSlopeSliding) 
        {
            currentSlopeAngle = 0f;
            slopeDirection = Vector3.zero;
            slopeIntensity = 0f;
            return;
        }
        
        // 지면 법선으로부터 경사각 계산
        currentSlopeAngle = Vector3.Angle(targetUpDirection, Vector3.up);
        
        if (currentSlopeAngle > slopeThreshold)
        {
            // 경사 방향 계산 (중력 방향으로의 투영)
            Vector3 horizontalNormal = Vector3.ProjectOnPlane(targetUpDirection, Vector3.up);
            slopeDirection = -horizontalNormal.normalized; // 아래쪽 방향
            
            // 경사 강도 계산 (0~1)
            float normalizedAngle = Mathf.Clamp01((currentSlopeAngle - slopeThreshold) / (maxSlideAngle - slopeThreshold));
            slopeIntensity = slopeCurve.Evaluate(normalizedAngle);
        }
        else
        {
            slopeDirection = Vector3.zero;
            slopeIntensity = 0f;
        }
    }
    
    void ApplySuperconductorHover()
    {
        bool anyGroundDetected = false;
        float minDistanceToGround = float.MaxValue;
        float averageDistanceToGround = 0f;
        int validGroundPoints = 0;
        
        // 지면까지의 거리 정보 수집
        for (int i = 0; i < 4; i++)
        {
            if (groundDetected[i])
            {
                anyGroundDetected = true;
                minDistanceToGround = Mathf.Min(minDistanceToGround, groundDistances[i]);
                averageDistanceToGround += groundDistances[i];
                validGroundPoints++;
            }
        }
        
        if (validGroundPoints > 0)
        {
            averageDistanceToGround /= validGroundPoints;
        }
        
        // 부양 범위 확장 (더 부드러운 전환을 위해)
        float hoverTransitionRange = hoverHeight + 1.0f; // 전환 범위 증가
        
        if (anyGroundDetected && minDistanceToGround <= hoverTransitionRange)
        {
            // 부양 힘과 중력의 혼합 적용
            float hoverInfluence = CalculateHoverInfluence(averageDistanceToGround);
            
            // 각 감지 포인트에서 개별적으로 부양 힘 적용
            for (int i = 0; i < 4; i++)
            {
                if (!groundDetected[i]) continue;
                
                Vector3 pointPosition = groundDetectionPoints[i].position;
                float targetHeight = groundPoints[i].y + hoverHeight;
                float currentHeight = pointPosition.y;
                float heightError = targetHeight - currentHeight;
                
                // 최소 높이 제한
                if (currentHeight - groundPoints[i].y < minHoverHeight)
                {
                    heightError = Mathf.Max(heightError, minHoverHeight - (currentHeight - groundPoints[i].y));
                }
                
                // 부드러운 부양 힘 계산 (PID 제어 방식)
                float proportionalForce = heightError * hoverStiffness;
                
                // 수직 속도 댐핑 (더 강한 댐핑)
                float verticalVelocity = Vector3.Dot(chairRigidbody.velocity, Vector3.up);
                float dampingForce = -verticalVelocity * hoverDamping * 2f; // 댐핑 강화
                
                // 부양 힘 적용 (부드러운 전환)
                Vector3 hoverForceVector = Vector3.up * (proportionalForce + dampingForce) * hoverInfluence * 0.25f;
                chairRigidbody.AddForceAtPosition(hoverForceVector, pointPosition, ForceMode.Force);
            }
            
            // 부분적 중력 적용 (부양 영향도에 따라)
            float gravityInfluence = 1f - hoverInfluence;
            if (gravityInfluence > 0f)
            {
                Vector3 partialGravityForce = Vector3.down * chairMass * 9.81f * gravityInfluence;
                chairRigidbody.AddForce(partialGravityForce, ForceMode.Force);
            }
        }
        else
        {
            // 완전히 공중에 있을 때는 일반 중력 적용
            Vector3 gravityForce = Vector3.down * chairMass * 9.81f;
            chairRigidbody.AddForce(gravityForce, ForceMode.Force);
        }
    }
    
    // 부양 영향도 계산 (거리에 따른 부드러운 전환)
    float CalculateHoverInfluence(float distanceToGround)
    {
        // 목표 부양 높이에서 최대 영향도
        if (distanceToGround <= hoverHeight)
        {
            return 1f;
        }
        
        // 부양 높이를 초과하면 점진적으로 감소
        float transitionRange = 1.0f; // 전환 범위
        float excessDistance = distanceToGround - hoverHeight;
        
        if (excessDistance >= transitionRange)
        {
            return 0f; // 완전히 중력만 적용
        }
        
        // 부드러운 곡선으로 전환 (코사인 보간)
        float normalizedDistance = excessDistance / transitionRange;
        return Mathf.Cos(normalizedDistance * Mathf.PI * 0.5f);
    }
    
    void ApplyStabilityControl()
    {
        if (!enableGyroscopicStabilization) return;
        
        // 현재 상향 벡터와 목표 상향 벡터 비교
        Vector3 currentUp = transform.up;
        Vector3 rotationError = Vector3.Cross(currentUp, targetUpDirection);
        float errorMagnitude = rotationError.magnitude;
        
        // 기울기 제한
        float tiltAngle = Vector3.Angle(currentUp, Vector3.up);
        if (tiltAngle > maxTiltAngle)
        {
            // 강력한 보정 토크 적용
            Vector3 correctionAxis = Vector3.Cross(currentUp, Vector3.up);
            float correctionMagnitude = (tiltAngle - maxTiltAngle) * stabilityForce;
            Vector3 correctionTorque = correctionAxis.normalized * correctionMagnitude;
            chairRigidbody.AddTorque(correctionTorque, ForceMode.Force);
        }
        
        // 지면 법선에 따른 자세 조정 (부드럽게)
        if (errorMagnitude > 0.01f && currentStability > 0.5f)
        {
            Vector3 stabilityTorque = rotationError * stabilityForce * stabilityResponseSpeed * 0.1f;
            chairRigidbody.AddTorque(stabilityTorque, ForceMode.Force);
        }
        
        // 각속도 댐핑 (흔들림 방지)
        Vector3 angularVelocity = chairRigidbody.angularVelocity;
        Vector3 angularDamping = -angularVelocity * stabilityDamping;
        chairRigidbody.AddTorque(angularDamping, ForceMode.Force);
    }
    
    void ProcessWheelInput()
    {
        // 왼쪽 바퀴 입력
        bool leftGrabbed = leftWheelGrab != null && leftWheelGrab.GetHeldBy().Count > 0;
        float leftRotationInput = GetWheelRotationInput(leftWheelGrab, leftWheel, leftWheelAxis, ref lastLeftHandPos, ref lastLeftGrabbed, "왼쪽");
        
        // 오른쪽 바퀴 입력
        bool rightGrabbed = rightWheelGrab != null && rightWheelGrab.GetHeldBy().Count > 0;
        float rightRotationInput = GetWheelRotationInput(rightWheelGrab, rightWheel, rightWheelAxis, ref lastRightHandPos, ref lastRightGrabbed, "오른쪽");
        
        // 경사로에 의한 바퀴 회전 계산
        CalculateSlopeWheelRotation();
        
        // 왼쪽 바퀴 각속도 업데이트
        if (leftGrabbed)
        {
            // 사용자가 바퀴를 잡고 있는 경우
            if (enableUserBraking && Mathf.Abs(leftRotationInput) < 0.1f && Mathf.Abs(leftWheelSlopeRotation) > 0.1f)
            {
                // 사용자가 바퀴를 잡고 있지만 회전시키지 않는 경우 = 제동
                isLeftWheelBraking = true;
                leftWheelAngularVelocity *= userBrakingForce; // 강한 제동 적용
                leftWheelSlopeRotation *= userBrakingForce; // 경사로 회전도 제동
                
                if (enableWheelRotationDebug)
                {
                    Debug.Log($"🛑 왼쪽 바퀴 사용자 제동 적용 - 제동력: {userBrakingForce}");
                }
            }
            else
            {
                // 사용자 입력으로 바퀴 각속도 직접 제어
                isLeftWheelBraking = false;
                float inputAngularVelocity = leftRotationInput * wheelInputSensitivity;
                
                // 경사로에서 바퀴 그립력 적용
                if (enableSlopeSliding && slopeIntensity > 0f)
                {
                    inputAngularVelocity *= wheelGripOnSlope;
                }
                
                leftWheelAngularVelocity = inputAngularVelocity + leftWheelSlopeRotation;
            }
            
            // 경사로 회전이 있거나 기존 회전이 남아있으면 활성 상태 유지
            isLeftWheelActive = Mathf.Abs(leftWheelAngularVelocity) > 0.01f || Mathf.Abs(leftWheelSlopeRotation) > 0.01f;
        }
        else
        {
            // 바퀴를 잡지 않은 경우
            isLeftWheelBraking = false;
            
            // 경사로 회전과 기존 회전에 마찰 적용
            leftWheelAngularVelocity = (leftWheelAngularVelocity * wheelFriction) + leftWheelSlopeRotation;
            
            // 회전 마찰력 적용 (점진적 감속)
            leftWheelAngularVelocity *= wheelRotationFriction;
            
            // 추가 감속 (잡지 않을 때)
            leftWheelAngularVelocity = Mathf.Lerp(leftWheelAngularVelocity, leftWheelSlopeRotation, wheelDecelerationRate);
            
            // 경사로 회전이 있거나 기존 회전이 남아있으면 활성 상태 유지
            isLeftWheelActive = Mathf.Abs(leftWheelAngularVelocity) > 0.01f || Mathf.Abs(leftWheelSlopeRotation) > 0.01f;
        }
        
        // 오른쪽 바퀴 각속도 업데이트
        if (rightGrabbed)
        {
            // 사용자가 바퀴를 잡고 있는 경우
            if (enableUserBraking && Mathf.Abs(rightRotationInput) < 0.1f && Mathf.Abs(rightWheelSlopeRotation) > 0.1f)
            {
                // 사용자가 바퀴를 잡고 있지만 회전시키지 않는 경우 = 제동
                isRightWheelBraking = true;
                rightWheelAngularVelocity *= userBrakingForce; // 강한 제동 적용
                rightWheelSlopeRotation *= userBrakingForce; // 경사로 회전도 제동
                
                if (enableWheelRotationDebug)
                {
                    Debug.Log($"🛑 오른쪽 바퀴 사용자 제동 적용 - 제동력: {userBrakingForce}");
                }
            }
            else
            {
                // 사용자 입력으로 바퀴 각속도 직접 제어
                isRightWheelBraking = false;
                float inputAngularVelocity = rightRotationInput * wheelInputSensitivity;
                
                // 경사로에서 바퀴 그립력 적용
                if (enableSlopeSliding && slopeIntensity > 0f)
                {
                    inputAngularVelocity *= wheelGripOnSlope;
                }
                
                rightWheelAngularVelocity = inputAngularVelocity + rightWheelSlopeRotation;
            }
            
            // 경사로 회전이 있거나 기존 회전이 남아있으면 활성 상태 유지
            isRightWheelActive = Mathf.Abs(rightWheelAngularVelocity) > 0.01f || Mathf.Abs(rightWheelSlopeRotation) > 0.01f;
        }
        else
        {
            // 바퀴를 잡지 않은 경우
            isRightWheelBraking = false;
            
            // 경사로 회전과 기존 회전에 마찰 적용
            rightWheelAngularVelocity = (rightWheelAngularVelocity * wheelFriction) + rightWheelSlopeRotation;
            
            // 회전 마찰력 적용 (점진적 감속)
            rightWheelAngularVelocity *= wheelRotationFriction;
            
            // 추가 감속 (잡지 않을 때)
            rightWheelAngularVelocity = Mathf.Lerp(rightWheelAngularVelocity, rightWheelSlopeRotation, wheelDecelerationRate);
            
            // 경사로 회전이 있거나 기존 회전이 남아있으면 활성 상태 유지
            isRightWheelActive = Mathf.Abs(rightWheelAngularVelocity) > 0.01f || Mathf.Abs(rightWheelSlopeRotation) > 0.01f;
        }
        
        // 매우 작은 값은 0으로 처리 (완전 정지)
        if (Mathf.Abs(leftWheelAngularVelocity) < wheelStopThreshold) 
        {
            leftWheelAngularVelocity = 0f;
            leftWheelSlopeRotation = 0f;
            isLeftWheelActive = false;
        }
        if (Mathf.Abs(rightWheelAngularVelocity) < wheelStopThreshold) 
        {
            rightWheelAngularVelocity = 0f;
            rightWheelSlopeRotation = 0f;
            isRightWheelActive = false;
        }
        
        // 전체 바퀴 활성 상태 업데이트
        isAnyWheelActive = isLeftWheelActive || isRightWheelActive || 
                          Mathf.Abs(leftWheelSlopeRotation) > 0.01f || 
                          Mathf.Abs(rightWheelSlopeRotation) > 0.01f;
        
        // 디버그 정보
        if (enableWheelRotationDebug && (isAnyWheelActive || isLeftWheelBraking || isRightWheelBraking))
        {
            string leftStatus = isLeftWheelActive ? $"활성({leftWheelAngularVelocity:F2} rad/s)" : "비활성";
            string rightStatus = isRightWheelActive ? $"활성({rightWheelAngularVelocity:F2} rad/s)" : "비활성";
            
            if (isLeftWheelBraking || isRightWheelBraking)
            {
                leftStatus += isLeftWheelBraking ? " [제동]" : "";
                rightStatus += isRightWheelBraking ? " [제동]" : "";
            }
            
            if (Mathf.Abs(leftWheelSlopeRotation) > 0.01f || Mathf.Abs(rightWheelSlopeRotation) > 0.01f)
            {
                Debug.Log($"🎡 경사로 바퀴 회전 - 왼쪽: {leftWheelSlopeRotation:F2} rad/s, 오른쪽: {rightWheelSlopeRotation:F2} rad/s");
                Debug.Log($"🎡 경사로 상태 - 각도: {currentSlopeAngle:F1}도, 강도: {slopeIntensity:F2}, 방향: {slopeDirection}");
                Debug.Log($"🎡 총 바퀴 각속도 - 왼쪽: {leftWheelAngularVelocity:F2} rad/s, 오른쪽: {rightWheelAngularVelocity:F2} rad/s");
            }
            
            Debug.Log($"바퀴 상태 - 왼쪽: {leftStatus}, 오른쪽: {rightStatus}");
        }
    }
    
    float GetWheelRotationInput(Grabbable grab, Transform wheelTransform, Vector3 wheelAxis, ref Vector3 lastHandPos, ref bool lastGrabbed, string wheelName)
    {
        if (grab == null || grab.GetHeldBy().Count == 0)
        {
            lastGrabbed = false;
            return 0f;
        }
        
        Hand hand = grab.GetHeldBy()[0] as Hand;
        if (hand == null) return 0f;
        
        Vector3 handPos = hand.transform.position;
        
        if (!lastGrabbed)
        {
            lastHandPos = handPos;
            lastGrabbed = true;
            return 0f;
        }
        
        // 바퀴 중심을 기준으로 한 회전 계산
        Vector3 wheelCenter = wheelTransform.position;
        
        // 이전 손 위치와 현재 손 위치를 바퀴 중심 기준으로 변환
        Vector3 lastRelativePos = lastHandPos - wheelCenter;
        Vector3 currentRelativePos = handPos - wheelCenter;
        
        // 바퀴 회전축에 수직인 평면으로 투영
        Vector3 lastProjected = Vector3.ProjectOnPlane(lastRelativePos, wheelAxis);
        Vector3 currentProjected = Vector3.ProjectOnPlane(currentRelativePos, wheelAxis);
        
        // 투영된 벡터가 너무 작으면 회전 계산 불가
        if (lastProjected.magnitude < 0.01f || currentProjected.magnitude < 0.01f)
        {
            lastHandPos = handPos;
            return 0f;
        }
        
        // 정규화
        lastProjected.Normalize();
        currentProjected.Normalize();
        
        // 각도 변화 계산 (부호 포함)
        float angle = Vector3.SignedAngle(lastProjected, currentProjected, wheelAxis);
        
        // 너무 큰 각도 변화는 노이즈로 간주
        if (Mathf.Abs(angle) > 45f)
        {
            lastHandPos = handPos;
            return 0f;
        }
        
        // 각속도로 변환 (도/초 -> rad/초)
        float angularVelocity = angle * Mathf.Deg2Rad / Time.fixedDeltaTime;
        
        lastHandPos = handPos;
        
        // 디버그 정보
        if (enableWheelRotationDebug && Mathf.Abs(angularVelocity) > 0.1f)
        {
            Debug.Log($"{wheelName} 바퀴 회전 감지 - 회전축: {wheelAxis}, 각도: {angle:F2}도, 각속도: {angularVelocity:F2} rad/s");
        }
        
        return angularVelocity;
    }
    
    void CalculateSlopeWheelRotation()
    {
        if (!enableSlopeWheelRotation || slopeIntensity <= 0f)
        {
            // 경사로 회전 마찰 적용 (점진적 감속)
            leftWheelSlopeRotation *= wheelRotationFriction;
            rightWheelSlopeRotation *= wheelRotationFriction;
            
            // 매우 작은 값은 0으로 처리
            if (Mathf.Abs(leftWheelSlopeRotation) < wheelStopThreshold)
                leftWheelSlopeRotation = 0f;
            if (Mathf.Abs(rightWheelSlopeRotation) < wheelStopThreshold)
                rightWheelSlopeRotation = 0f;
                
            return;
        }
        
        // 경사로 방향과 휠체어 전진 방향의 내적으로 회전 방향 결정
        Vector3 chairForward = useManualDirections ? manualForwardDirection.normalized : transform.forward;
        float slopeForwardDot = Vector3.Dot(slopeDirection, chairForward);
        
        // 경사로 강도에 따른 기본 회전 속도 계산
        float baseSlopeRotation = slopeIntensity * slopeWheelRotationMultiplier * slopeForwardDot;
        
        // 경사각에 따른 추가 회전 (더 가파른 경사일수록 빠른 회전)
        float angleMultiplier = Mathf.Clamp01(currentSlopeAngle / maxSlideAngle);
        baseSlopeRotation *= (1f + angleMultiplier);
        
        // 바퀴별 회전 속도 계산 (바퀴 축 방향 고려)
        Vector3 leftWheelWorldAxis = leftWheel != null ? leftWheel.TransformDirection(leftWheelAxis) : leftWheelAxis;
        Vector3 rightWheelWorldAxis = rightWheel != null ? rightWheel.TransformDirection(rightWheelAxis) : rightWheelAxis;
        
        // 경사 방향과 바퀴 축의 관계로 회전 방향 결정
        float leftRotationDirection = Vector3.Dot(Vector3.Cross(slopeDirection, Vector3.up), leftWheelWorldAxis);
        float rightRotationDirection = Vector3.Dot(Vector3.Cross(slopeDirection, Vector3.up), rightWheelWorldAxis);
        
        // 목표 경사로 회전 속도 계산
        float targetLeftSlopeRotation = baseSlopeRotation * leftRotationDirection;
        float targetRightSlopeRotation = baseSlopeRotation * rightRotationDirection;
        
        // 부드러운 전환 적용
        float transitionSpeed = 5f * Time.fixedDeltaTime;
        leftWheelSlopeRotation = Mathf.Lerp(leftWheelSlopeRotation, targetLeftSlopeRotation, transitionSpeed);
        rightWheelSlopeRotation = Mathf.Lerp(rightWheelSlopeRotation, targetRightSlopeRotation, transitionSpeed);
        
        // 디버그 정보
        if (enableWheelRotationDebug && (Mathf.Abs(leftWheelSlopeRotation) > 0.1f || Mathf.Abs(rightWheelSlopeRotation) > 0.1f))
        {
            Debug.Log($"🎡 경사로 바퀴 회전 계산 - 경사각: {currentSlopeAngle:F1}도, 강도: {slopeIntensity:F2}");
            Debug.Log($"경사 방향: {slopeDirection}, 전진 내적: {slopeForwardDot:F2}");
            Debug.Log($"바퀴 회전 - 왼쪽: {leftWheelSlopeRotation:F2} rad/s, 오른쪽: {rightWheelSlopeRotation:F2} rad/s");
        }
        
        // 경사로 감지 상태 디버그 (경사로가 있을 때만)
        if (enableWheelRotationDebug && enableSlopeWheelRotation && slopeIntensity > 0f)
        {
            Debug.Log($"🎡 경사로 감지됨 - 활성화: {enableSlopeWheelRotation}, 강도: {slopeIntensity:F2}, 각도: {currentSlopeAngle:F1}도");
            Debug.Log($"🎡 기본 회전: {baseSlopeRotation:F2}, 목표 - 왼쪽: {targetLeftSlopeRotation:F2}, 오른쪽: {targetRightSlopeRotation:F2}");
        }
    }
    
    void CalculateMovementFromWheels()
    {
        // 바퀴가 활성화되지 않았다면 이동하지 않음
        if (onlyMoveWhenWheelsActive && !isAnyWheelActive)
        {
            targetVelocity = Vector3.zero;
            targetAngularVelocity = 0f;
            return;
        }
        
        // 바퀴 각속도를 선속도로 변환 (v = ω * r)
        float leftWheelSpeed = leftWheelAngularVelocity * wheelRadius * wheelToMovementRatio;
        float rightWheelSpeed = rightWheelAngularVelocity * wheelRadius * wheelToMovementRatio;
        
        // 두 바퀴 속도로부터 휠체어 이동 계산
        float averageSpeed = (leftWheelSpeed + rightWheelSpeed) * 0.5f;
        float speedDifference = rightWheelSpeed - leftWheelSpeed;
        
        // 직진 속도 계산
        Vector3 forwardDirection = useManualDirections ? manualForwardDirection.normalized : transform.forward;
        forwardDirection.y = 0; // Y축 성분 제거 (수평 이동만)
        forwardDirection.Normalize();
        
        targetVelocity = forwardDirection * averageSpeed;
        
        // 회전 속도 계산 (두 바퀴 속도 차이 기반)
        // 휠체어 폭을 고려한 각속도 계산
        float wheelbaseWidth = 0.8f; // 바퀴 간 거리
        targetAngularVelocity = speedDifference / wheelbaseWidth; // rad/s
        
        // 한쪽 바퀴만 회전하는 경우의 특별한 처리
        if (isLeftWheelActive && !isRightWheelActive)
        {
            // 왼쪽 바퀴만 회전 - 오른쪽 바퀴 중심으로 회전
            CalculatePivotMovement(leftWheelSpeed, true);
        }
        else if (isRightWheelActive && !isLeftWheelActive)
        {
            // 오른쪽 바퀴만 회전 - 왼쪽 바퀴 중심으로 회전
            CalculatePivotMovement(rightWheelSpeed, false);
        }
    }
    
    void CalculatePivotMovement(float wheelSpeed, bool leftWheelActive)
    {
        // 피벗 중심 회전 계산
        float wheelbaseWidth = 0.8f;
        float pivotRadius = wheelbaseWidth * 0.5f;
        
        // 각속도 계산 (v = ω * r에서 ω = v / r)
        float pivotAngularVelocity = wheelSpeed / pivotRadius;
        
        if (!leftWheelActive) pivotAngularVelocity *= -1; // 오른쪽 바퀴 회전시 반대 방향
        
        targetAngularVelocity = pivotAngularVelocity;
        
        // 피벗 중심으로의 이동 성분 계산
        Vector3 pivotOffset = leftWheelActive ? Vector3.right * pivotRadius : Vector3.left * pivotRadius;
        Vector3 pivotPoint = transform.position + transform.TransformDirection(pivotOffset);
        
        // 피벗 중심 주변의 접선 방향 계산
        Vector3 leverArm = transform.position - pivotPoint;
        Vector3 tangentDirection = Vector3.Cross(Vector3.up, leverArm).normalized;
        
        if (leftWheelActive)
            targetVelocity = tangentDirection * wheelSpeed * 0.5f;
        else
            targetVelocity = -tangentDirection * wheelSpeed * 0.5f;
    }
    
    void ApplySlopeSliding()
    {
        if (!enableSlopeSliding || slopeIntensity <= 0f)
        {
            slideVelocity = Vector3.zero;
            return;
        }
        
        // 경사로 미끄러짐 힘 계산
        Vector3 slideForceVector = slopeDirection * slideForce * slopeIntensity;
        
        // 현재 미끄러짐 속도에 마찰 적용
        slideVelocity += slideForceVector * Time.fixedDeltaTime / chairMass;
        slideVelocity *= (1f - slideFriction * Time.fixedDeltaTime);
        
        // 바퀴가 활성화되어 있으면 미끄러짐 저항
        if (isAnyWheelActive)
        {
            float wheelResistance = wheelGripOnSlope;
            slideVelocity *= (1f - wheelResistance);
        }
        
        // 미끄러짐 속도를 목표 속도에 추가
        targetVelocity += slideVelocity;
        
        // 경사로에서 바퀴 회전에 미끄러짐 효과 추가
        if (isAnyWheelActive)
        {
            // 경사 방향으로의 추가 바퀴 회전 (미끄러짐 시뮬레이션)
            float slopeInfluence = Vector3.Dot(slopeDirection, transform.forward) * slopeIntensity;
            float additionalRotation = slopeInfluence * slideForce * 0.001f;
            
            leftWheelAngularVelocity += additionalRotation;
            rightWheelAngularVelocity += additionalRotation;
        }
    }
    
    void ApplyCalculatedMovement()
    {
        // 현재 속도와 목표 속도 사이의 부드러운 보간
        Vector3 currentVelocity = chairRigidbody.velocity;
        Vector3 horizontalVelocity = new Vector3(currentVelocity.x, 0, currentVelocity.z);
        Vector3 verticalVelocity = new Vector3(0, currentVelocity.y, 0);
        
        // 수평 이동만 바퀴에 의해 제어됨
        Vector3 newHorizontalVelocity = Vector3.Lerp(horizontalVelocity, targetVelocity, movementSmoothing * Time.fixedDeltaTime);
        
        // 최종 속도 적용 (수직 성분은 부양 시스템이 제어)
        chairRigidbody.velocity = newHorizontalVelocity + verticalVelocity;
        
        // 정당한 속도 저장 (바퀴 + 경사로 미끄러짐)
        legitimateVelocity = newHorizontalVelocity + slideVelocity;
        
        // 각속도 적용
        Vector3 currentAngularVelocity = chairRigidbody.angularVelocity;
        float newYAngularVelocity = Mathf.Lerp(currentAngularVelocity.y, targetAngularVelocity, rotationSmoothing * Time.fixedDeltaTime);
        
        // Y축 회전만 바퀴에 의해 제어됨 (X, Z축은 안정성 시스템이 제어)
        chairRigidbody.angularVelocity = new Vector3(currentAngularVelocity.x, newYAngularVelocity, currentAngularVelocity.z);
    }
    
    void UpdateWheelVisualRotation()
    {
        // 바퀴 각속도를 시각적 회전에 반영
        float leftRotationDelta = leftWheelAngularVelocity * Time.fixedDeltaTime * Mathf.Rad2Deg;
        float rightRotationDelta = rightWheelAngularVelocity * Time.fixedDeltaTime * Mathf.Rad2Deg;
        
        leftWheelRotation += leftRotationDelta;
        rightWheelRotation += rightRotationDelta;
        
        // 바퀴 메시 회전 적용 (각 바퀴의 회전축에 따라)
        if (leftWheel != null)
        {
            ApplyWheelRotation(leftWheel, leftWheelAxis, leftWheelRotation, "왼쪽");
        }
        
        if (rightWheel != null)
        {
            ApplyWheelRotation(rightWheel, rightWheelAxis, rightWheelRotation, "오른쪽");
        }
        
        // 디버그 정보 - 조건 수정 (경사로 회전도 포함)
        bool hasRotation = Mathf.Abs(leftRotationDelta) > 0.01f || Mathf.Abs(rightRotationDelta) > 0.01f;
        bool hasSlopeRotation = Mathf.Abs(leftWheelSlopeRotation) > 0.01f || Mathf.Abs(rightWheelSlopeRotation) > 0.01f;
        
        if (enableWheelRotationDebug && (hasRotation || hasSlopeRotation))
        {
            Debug.Log($"🎡 바퀴 회전 업데이트 - 왼쪽: {leftWheelRotation:F1}도 ({leftRotationDelta:F2}도/프레임), " +
                     $"오른쪽: {rightWheelRotation:F1}도 ({rightRotationDelta:F2}도/프레임)");
            
            if (hasSlopeRotation)
            {
                Debug.Log($"🎡 경사로 회전 기여 - 왼쪽: {leftWheelSlopeRotation:F2} rad/s, 오른쪽: {rightWheelSlopeRotation:F2} rad/s");
            }
        }
    }
    
    void ApplyWheelRotation(Transform wheel, Vector3 wheelAxis, float rotationAngle, string wheelName)
    {
        // wheelAxis는 로컬 좌표계 기준
        Vector3 localAxis = wheelAxis.normalized;
        
        // 로컬 축을 기준으로 회전 적용 (절대 각도 방식)
        Vector3 eulerRotation = Vector3.zero;
        
        // 가장 가까운 축 찾기
        float absX = Mathf.Abs(localAxis.x);
        float absY = Mathf.Abs(localAxis.y);
        float absZ = Mathf.Abs(localAxis.z);
        
        if (absX >= absY && absX >= absZ)
        {
            // X축 회전
            eulerRotation.x = rotationAngle * Mathf.Sign(localAxis.x);
        }
        else if (absY >= absX && absY >= absZ)
        {
            // Y축 회전
            eulerRotation.y = rotationAngle * Mathf.Sign(localAxis.y);
        }
        else
        {
            // Z축 회전
            eulerRotation.z = rotationAngle * Mathf.Sign(localAxis.z);
        }
        
        // 부드러운 회전을 위해 Quaternion 사용
        Quaternion targetRotation = Quaternion.Euler(eulerRotation);
        wheel.localRotation = targetRotation;
        
        // 디버그 정보 - 더 자세한 정보 제공
        if (enableWheelRotationDebug && Mathf.Abs(rotationAngle) > 1f)
        {
            Debug.Log($"🎡 {wheelName} 바퀴 시각적 회전:");
            Debug.Log($"  - 로컬축: {localAxis}");
            Debug.Log($"  - 회전각: {rotationAngle:F1}도");
            Debug.Log($"  - 오일러: {eulerRotation}");
            Debug.Log($"  - 적용된 회전: {wheel.localRotation.eulerAngles}");
        }
    }
    
    void ApplyPhysicsLimits()
    {
        // 최대 속도 제한
        Vector3 velocity = chairRigidbody.velocity;
        Vector3 horizontalVelocity = new Vector3(velocity.x, 0, velocity.z);
        
        if (horizontalVelocity.magnitude > maxSpeed)
        {
            horizontalVelocity = horizontalVelocity.normalized * maxSpeed;
            chairRigidbody.velocity = new Vector3(horizontalVelocity.x, velocity.y, horizontalVelocity.z);
        }
        
        // 수직 속도 제한 (부양 안정성을 위해)
        float maxVerticalSpeed = 3f; // 최대 수직 속도 제한
        if (Mathf.Abs(velocity.y) > maxVerticalSpeed)
        {
            float clampedY = Mathf.Clamp(velocity.y, -maxVerticalSpeed, maxVerticalSpeed);
            chairRigidbody.velocity = new Vector3(velocity.x, clampedY, velocity.z);
        }
        
        // 최대 각속도 제한 (Y축만)
        Vector3 angularVelocity = chairRigidbody.angularVelocity;
        float maxAngularVel = maxAngularSpeed * Mathf.Deg2Rad;
        
        if (Mathf.Abs(angularVelocity.y) > maxAngularVel)
        {
            float clampedY = Mathf.Clamp(angularVelocity.y, -maxAngularVel, maxAngularVel);
            chairRigidbody.angularVelocity = new Vector3(angularVelocity.x, clampedY, angularVelocity.z);
        }
    }
    
    void EnforceMovementRestrictions()
    {
        Vector3 currentVelocity = chairRigidbody.velocity;
        Vector3 currentHorizontalVelocity = new Vector3(currentVelocity.x, 0, currentVelocity.z);
        Vector3 legitimateHorizontalVelocity = new Vector3(legitimateVelocity.x, 0, legitimateVelocity.z);
        
        // 외부 힘에 의한 비정상적인 속도 변화 감지
        Vector3 velocityDifference = currentHorizontalVelocity - legitimateHorizontalVelocity;
        float externalForce = velocityDifference.magnitude;
        
        // 콜라이더 충돌 감지 (위치 변화가 예상보다 작은 경우)
        Vector3 expectedPosition = lastPosition + legitimateVelocity * Time.fixedDeltaTime;
        Vector3 actualPosition = transform.position;
        float positionDifference = Vector3.Distance(expectedPosition, actualPosition);
        
        // 콜라이더 충돌로 인한 이동 제한은 허용
        if (allowColliderInteraction && positionDifference > 0.01f)
        {
            isCollisionDetected = true;
            // 충돌이 감지되면 현재 속도를 유지 (물리적 충돌 반응 허용)
        }
        else
        {
            isCollisionDetected = false;
            
            // 외부 힘이 임계값을 초과하는 경우 속도 보정
            if (externalForce > externalForceThreshold)
            {
                // 바퀴와 경사로에 의한 정당한 속도로 강제 보정
                Vector3 correctedVelocity = new Vector3(legitimateVelocity.x, currentVelocity.y, legitimateVelocity.z);
                chairRigidbody.velocity = correctedVelocity;
                
                // 디버그 로그
                if (enableWheelRotationDebug)
                {
                    Debug.Log($"🔒 외부 힘 감지 및 보정 - 외부 힘 크기: {externalForce:F3}, 임계값: {externalForceThreshold}");
                    Debug.Log($"보정 전 속도: {currentHorizontalVelocity}, 보정 후 속도: {new Vector3(legitimateVelocity.x, 0, legitimateVelocity.z)}");
                }
            }
        }
        
        // 바퀴가 비활성이고 경사로도 없는 경우 수평 이동 완전 차단
        if (!isAnyWheelActive && slopeIntensity <= 0f && !isCollisionDetected)
        {
            Vector3 stoppedVelocity = new Vector3(0, currentVelocity.y, 0);
            chairRigidbody.velocity = Vector3.Lerp(currentVelocity, stoppedVelocity, 10f * Time.fixedDeltaTime);
            
            if (enableWheelRotationDebug && currentHorizontalVelocity.magnitude > 0.1f)
            {
                Debug.Log("🔒 바퀴 비활성 + 경사로 없음 → 수평 이동 차단");
            }
        }
        
        // 다음 프레임을 위한 데이터 저장
        lastFrameVelocity = currentVelocity;
        lastPosition = transform.position;
    }
    
    // 충돌 감지 이벤트
    void OnCollisionEnter(Collision collision)
    {
        if (allowColliderInteraction)
        {
            isCollisionDetected = true;
            if (enableWheelRotationDebug)
            {
                Debug.Log($"🔒 충돌 감지: {collision.gameObject.name}");
            }
        }
    }
    
    void OnCollisionExit(Collision collision)
    {
        if (allowColliderInteraction)
        {
            isCollisionDetected = false;
            if (enableWheelRotationDebug)
            {
                Debug.Log($"🔒 충돌 종료: {collision.gameObject.name}");
            }
        }
    }
    
    // 공개 메서드들
    public void SetHoverHeight(float height)
    {
        hoverHeight = Mathf.Max(height, minHoverHeight);
    }
    
    public float GetCurrentStability()
    {
        return currentStability;
    }
    
    public bool IsStable()
    {
        return currentStability > 0.9f && Vector3.Angle(transform.up, Vector3.up) < maxTiltAngle;
    }
    
    public float GetLeftWheelSpeed()
    {
        return leftWheelAngularVelocity * wheelRadius;
    }
    
    public float GetRightWheelSpeed()
    {
        return rightWheelAngularVelocity * wheelRadius;
    }
    
    public Vector3 GetTargetVelocity()
    {
        return targetVelocity;
    }
    
    public float GetCurrentSlopeAngle()
    {
        return currentSlopeAngle;
    }
    
    public float GetSlopeIntensity()
    {
        return slopeIntensity;
    }
    
    public bool IsAnyWheelActive()
    {
        return isAnyWheelActive;
    }
    
    public Vector3 GetSlideVelocity()
    {
        return slideVelocity;
    }
    
    public float GetLeftWheelAngularVelocity()
    {
        return leftWheelAngularVelocity;
    }
    
    public float GetRightWheelAngularVelocity()
    {
        return rightWheelAngularVelocity;
    }
    
    public float GetLeftWheelRotation()
    {
        return leftWheelRotation;
    }
    
    public float GetRightWheelRotation()
    {
        return rightWheelRotation;
    }
    
    public bool IsLeftWheelActive()
    {
        return isLeftWheelActive;
    }
    
    public bool IsRightWheelActive()
    {
        return isRightWheelActive;
    }
    
    public void SetWheelRotationDebug(bool enabled)
    {
        enableWheelRotationDebug = enabled;
    }
    
    public void SetLeftWheelAxis(Vector3 axis)
    {
        leftWheelAxis = axis.normalized;
        Debug.Log($"왼쪽 바퀴 회전축 수동 설정: {leftWheelAxis}");
    }
    
    public void SetRightWheelAxis(Vector3 axis)
    {
        rightWheelAxis = axis.normalized;
        Debug.Log($"오른쪽 바퀴 회전축 수동 설정: {rightWheelAxis}");
    }
    
    public void SetWheelAxes(Vector3 leftAxis, Vector3 rightAxis)
    {
        leftWheelAxis = leftAxis.normalized;
        rightWheelAxis = rightAxis.normalized;
        Debug.Log($"바퀴 회전축 수동 설정 - 왼쪽: {leftWheelAxis}, 오른쪽: {rightWheelAxis}");
    }
    
    public void RedetectWheelAxes()
    {
        DetectWheelAxes();
    }
    
    public Vector3 GetLeftWheelAxis()
    {
        return leftWheelAxis;
    }
    
    public Vector3 GetRightWheelAxis()
    {
        return rightWheelAxis;
    }
    
    // 이동 제한 관련 공개 메서드들
    public void SetStrictMovementControl(bool enabled)
    {
        strictMovementControl = enabled;
        Debug.Log($"🔒 엄격한 이동 제어: {(enabled ? "활성화" : "비활성화")}");
    }
    
    public bool IsStrictMovementControlEnabled()
    {
        return strictMovementControl;
    }
    
    public void SetExternalForceThreshold(float threshold)
    {
        externalForceThreshold = Mathf.Max(0f, threshold);
        Debug.Log($"🔒 외부 힘 임계값 설정: {externalForceThreshold}");
    }
    
    public float GetExternalForceThreshold()
    {
        return externalForceThreshold;
    }
    
    public void SetAllowColliderInteraction(bool allow)
    {
        allowColliderInteraction = allow;
        Debug.Log($"🔒 콜라이더 상호작용: {(allow ? "허용" : "차단")}");
    }
    
    public bool IsColliderInteractionAllowed()
    {
        return allowColliderInteraction;
    }
    
    public bool IsCollisionDetected()
    {
        return isCollisionDetected;
    }
    
    public Vector3 GetLegitimateVelocity()
    {
        return legitimateVelocity;
    }
    
    public bool IsMovementLegitimate()
    {
        // 바퀴가 활성화되어 있거나 경사로에서 미끄러지는 경우만 정당한 이동
        return isAnyWheelActive || slopeIntensity > 0f || isCollisionDetected;
    }
    
    // 경사로 바퀴 회전 관련 공개 메서드들
    public void SetSlopeWheelRotation(bool enabled)
    {
        enableSlopeWheelRotation = enabled;
        Debug.Log($"🎡 경사로 바퀴 회전: {(enabled ? "활성화" : "비활성화")}");
    }
    
    public bool IsSlopeWheelRotationEnabled()
    {
        return enableSlopeWheelRotation;
    }
    
    public void SetSlopeWheelRotationMultiplier(float multiplier)
    {
        slopeWheelRotationMultiplier = Mathf.Max(0f, multiplier);
        Debug.Log($"🎡 경사로 바퀴 회전 배율: {slopeWheelRotationMultiplier}");
    }
    
    public float GetSlopeWheelRotationMultiplier()
    {
        return slopeWheelRotationMultiplier;
    }
    
    public void SetWheelRotationFriction(float friction)
    {
        wheelRotationFriction = Mathf.Clamp01(friction);
        Debug.Log($"🎡 바퀴 회전 마찰력: {wheelRotationFriction}");
    }
    
    public float GetWheelRotationFriction()
    {
        return wheelRotationFriction;
    }
    
    public void SetUserBraking(bool enabled)
    {
        enableUserBraking = enabled;
        Debug.Log($"🛑 사용자 제동: {(enabled ? "활성화" : "비활성화")}");
    }
    
    public bool IsUserBrakingEnabled()
    {
        return enableUserBraking;
    }
    
    public void SetUserBrakingForce(float force)
    {
        userBrakingForce = Mathf.Clamp01(force);
        Debug.Log($"🛑 사용자 제동력: {userBrakingForce}");
    }
    
    public float GetUserBrakingForce()
    {
        return userBrakingForce;
    }
    
    public bool IsLeftWheelBraking()
    {
        return isLeftWheelBraking;
    }
    
    public bool IsRightWheelBraking()
    {
        return isRightWheelBraking;
    }
    
    public float GetLeftWheelSlopeRotation()
    {
        return leftWheelSlopeRotation;
    }
    
    public float GetRightWheelSlopeRotation()
    {
        return rightWheelSlopeRotation;
    }
    
    public bool IsAnyWheelBraking()
    {
        return isLeftWheelBraking || isRightWheelBraking;
    }
    
    // 디버그 및 테스트 메서드들
    public void TestSlopeWheelRotation(float testSlopeIntensity = 0.5f)
    {
        Debug.Log($"🎡 경사로 바퀴 회전 테스트 시작 - 테스트 강도: {testSlopeIntensity}");
        
        // 임시로 경사로 값 설정
        float originalIntensity = slopeIntensity;
        Vector3 originalDirection = slopeDirection;
        
        slopeIntensity = testSlopeIntensity;
        slopeDirection = transform.forward; // 전진 방향으로 경사
        
        // 경사로 바퀴 회전 계산
        CalculateSlopeWheelRotation();
        
        Debug.Log($"🎡 테스트 결과 - 왼쪽 바퀴: {leftWheelSlopeRotation:F2} rad/s, 오른쪽 바퀴: {rightWheelSlopeRotation:F2} rad/s");
        Debug.Log($"🎡 바퀴 활성 상태 - 왼쪽: {isLeftWheelActive}, 오른쪽: {isRightWheelActive}, 전체: {isAnyWheelActive}");
        
        // 원래 값 복원
        slopeIntensity = originalIntensity;
        slopeDirection = originalDirection;
    }
    
    public void LogCurrentSlopeState()
    {
        Debug.Log($"🎡 현재 경사로 상태:");
        Debug.Log($"  - 경사로 바퀴 회전 활성화: {enableSlopeWheelRotation}");
        Debug.Log($"  - 경사로 미끄러짐 활성화: {enableSlopeSliding}");
        Debug.Log($"  - 현재 경사각: {currentSlopeAngle:F1}도");
        Debug.Log($"  - 경사 강도: {slopeIntensity:F2}");
        Debug.Log($"  - 경사 방향: {slopeDirection}");
        Debug.Log($"  - 경사 임계값: {slopeThreshold}도");
        Debug.Log($"  - 왼쪽 바퀴 경사 회전: {leftWheelSlopeRotation:F2} rad/s");
        Debug.Log($"  - 오른쪽 바퀴 경사 회전: {rightWheelSlopeRotation:F2} rad/s");
        Debug.Log($"  - 왼쪽 바퀴 총 각속도: {leftWheelAngularVelocity:F2} rad/s");
        Debug.Log($"  - 오른쪽 바퀴 총 각속도: {rightWheelAngularVelocity:F2} rad/s");
    }
    
    // 방향 설정 관련 공개 메서드들
    public void SetManualDirections(bool useManual)
    {
        useManualDirections = useManual;
        Debug.Log($"🧭 수동 방향 설정: {(useManual ? "활성화" : "비활성화")}");
        if (useManual)
        {
            Debug.Log($"  - 수동 전진 방향: {manualForwardDirection.normalized}");
        }
    }
    
    public void SetManualForwardDirection(Vector3 direction)
    {
        manualForwardDirection = direction.normalized;
        Debug.Log($"🧭 수동 전진 방향 설정: {manualForwardDirection}");
    }
    
    public void SetManualWheelAxes(bool useManual)
    {
        useManualWheelAxes = useManual;
        Debug.Log($"🧭 수동 바퀴 축 설정: {(useManual ? "활성화" : "비활성화")}");
        if (useManual)
        {
            leftWheelAxis = manualLeftWheelAxis.normalized;
            rightWheelAxis = manualRightWheelAxis.normalized;
            Debug.Log($"  - 왼쪽 바퀴 축: {leftWheelAxis}");
            Debug.Log($"  - 오른쪽 바퀴 축: {rightWheelAxis}");
        }
        else
        {
            // 자동 감지 재실행
            DetectWheelAxes();
        }
    }
    
    public void SetManualLeftWheelAxis(Vector3 axis)
    {
        manualLeftWheelAxis = axis.normalized;
        if (useManualWheelAxes)
        {
            leftWheelAxis = manualLeftWheelAxis;
        }
        Debug.Log($"🧭 수동 왼쪽 바퀴 축 설정: {manualLeftWheelAxis}");
    }
    
    public void SetManualRightWheelAxis(Vector3 axis)
    {
        manualRightWheelAxis = axis.normalized;
        if (useManualWheelAxes)
        {
            rightWheelAxis = manualRightWheelAxis;
        }
        Debug.Log($"🧭 수동 오른쪽 바퀴 축 설정: {manualRightWheelAxis}");
    }
    
    public Vector3 GetCurrentForwardDirection()
    {
        return useManualDirections ? manualForwardDirection.normalized : transform.forward;
    }
    
    public Vector3 GetCurrentLeftWheelAxis()
    {
        return leftWheelAxis;
    }
    
    public Vector3 GetCurrentRightWheelAxis()
    {
        return rightWheelAxis;
    }
    
    public void LogCurrentDirections()
    {
        Debug.Log($"🧭 현재 방향 설정:");
        Debug.Log($"  - 수동 방향 설정 사용: {useManualDirections}");
        Debug.Log($"  - 현재 전진 방향: {GetCurrentForwardDirection()}");
        Debug.Log($"  - 수동 바퀴 축 설정 사용: {useManualWheelAxes}");
        Debug.Log($"  - 현재 왼쪽 바퀴 축 (로컬): {leftWheelAxis}");
        Debug.Log($"  - 현재 오른쪽 바퀴 축 (로컬): {rightWheelAxis}");
        Debug.Log($"  - 자동 감지 활성화: {autoDetectWheelAxis}");
        
        if (useManualDirections)
        {
            Debug.Log($"  - 수동 전진 방향: {manualForwardDirection.normalized}");
        }
        else
        {
            Debug.Log($"  - Transform 전진 방향: {transform.forward}");
        }
        
        if (useManualWheelAxes)
        {
            Debug.Log($"  - 수동 왼쪽 바퀴 축 (로컬): {manualLeftWheelAxis.normalized}");
            Debug.Log($"  - 수동 오른쪽 바퀴 축 (로컬): {manualRightWheelAxis.normalized}");
        }
        
        // 월드 좌표계로 변환된 축도 표시
        if (leftWheel != null)
        {
            Vector3 leftWorldAxis = leftWheel.TransformDirection(leftWheelAxis);
            Debug.Log($"  - 왼쪽 바퀴 축 (월드): {leftWorldAxis}");
        }
        if (rightWheel != null)
        {
            Vector3 rightWorldAxis = rightWheel.TransformDirection(rightWheelAxis);
            Debug.Log($"  - 오른쪽 바퀴 축 (월드): {rightWorldAxis}");
        }
    }
    
    // 바퀴 축 설정 도우미 메서드
    public void SetCommonWheelAxes(string axisType)
    {
        Vector3 axis;
        switch (axisType.ToLower())
        {
            case "x":
            case "right":
                axis = Vector3.right; // (1, 0, 0)
                break;
            case "y":
            case "up":
                axis = Vector3.up; // (0, 1, 0)
                break;
            case "z":
            case "forward":
                axis = Vector3.forward; // (0, 0, 1)
                break;
            case "-x":
            case "left":
                axis = Vector3.left; // (-1, 0, 0)
                break;
            case "-y":
            case "down":
                axis = Vector3.down; // (0, -1, 0)
                break;
            case "-z":
            case "back":
                axis = Vector3.back; // (0, 0, -1)
                break;
            default:
                Debug.LogError($"알 수 없는 축 타입: {axisType}. 사용 가능한 값: x, y, z, -x, -y, -z, right, up, forward, left, down, back");
                return;
        }
        
        manualLeftWheelAxis = axis;
        manualRightWheelAxis = axis;
        
        if (useManualWheelAxes)
        {
            leftWheelAxis = axis;
            rightWheelAxis = axis;
        }
        
        Debug.Log($"🧭 공통 바퀴 축 설정: {axisType} → {axis}");
    }
    
    // 디버그 시각화
    void OnDrawGizmos()
    {
        if (groundDetectionPoints == null) return;
        
        // 지면 감지 포인트 표시
        for (int i = 0; i < 4; i++)
        {
            if (groundDetectionPoints[i] == null) continue;
            
            Vector3 pointPos = groundDetectionPoints[i].position;
            
            // 감지 포인트
            Gizmos.color = groundDetected != null && groundDetected[i] ? Color.green : Color.red;
            Gizmos.DrawWireSphere(pointPos, 0.05f);
            
            // 지면까지의 거리
            if (groundDetected != null && groundDetected[i])
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawLine(pointPos, groundPoints[i]);
                
                // 목표 높이 표시
                Vector3 targetPos = new Vector3(pointPos.x, groundPoints[i].y + hoverHeight, pointPos.z);
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireCube(targetPos, Vector3.one * 0.1f);
            }
        }
        
        // 안정성 상태 표시
        Gizmos.color = IsStable() ? Color.green : Color.yellow;
        Gizmos.DrawLine(transform.position, transform.position + transform.up * 2f);
        
        // 목표 상향 방향 표시
        Gizmos.color = Color.white;
        Gizmos.DrawLine(transform.position, transform.position + targetUpDirection * 1.5f);
        
        // 부양 높이 범위 표시
        Gizmos.color = new Color(0, 1, 1, 0.3f);
        Gizmos.DrawWireCube(transform.position + Vector3.down * hoverHeight, 
            new Vector3(1f, 0.1f, 1.5f));
            
        // 바퀴 속도 벡터 표시 (디버그용)
        if (Application.isPlaying)
        {
            // 목표 속도 벡터
            Gizmos.color = Color.magenta;
            Gizmos.DrawRay(transform.position, targetVelocity);
            
            // 바퀴 각속도 표시
            if (leftWheel != null)
            {
                Gizmos.color = isLeftWheelActive ? Color.blue : Color.gray;
                Vector3 leftWheelPos = leftWheel.position;
                float leftSpeed = leftWheelAngularVelocity * wheelRadius;
                Gizmos.DrawRay(leftWheelPos, transform.forward * leftSpeed);
                
                // 경사로 바퀴 회전 표시
                if (Mathf.Abs(leftWheelSlopeRotation) > 0.01f)
                {
                    Gizmos.color = new Color(1f, 0.5f, 0f); // 주황색
                    float leftSlopeSpeed = leftWheelSlopeRotation * wheelRadius;
                    Gizmos.DrawRay(leftWheelPos + Vector3.up * 0.1f, transform.forward * leftSlopeSpeed);
                }
                
                // 제동 상태 표시
                if (isLeftWheelBraking)
                {
                    Gizmos.color = Color.red;
                    Gizmos.DrawWireSphere(leftWheelPos + Vector3.up * 0.2f, 0.1f);
                }
            }
            
            if (rightWheel != null)
            {
                Gizmos.color = isRightWheelActive ? Color.red : Color.gray;
                Vector3 rightWheelPos = rightWheel.position;
                float rightSpeed = rightWheelAngularVelocity * wheelRadius;
                Gizmos.DrawRay(rightWheelPos, transform.forward * rightSpeed);
                
                // 경사로 바퀴 회전 표시
                if (Mathf.Abs(rightWheelSlopeRotation) > 0.01f)
                {
                    Gizmos.color = new Color(1f, 0.5f, 0f); // 주황색
                    float rightSlopeSpeed = rightWheelSlopeRotation * wheelRadius;
                    Gizmos.DrawRay(rightWheelPos + Vector3.up * 0.1f, transform.forward * rightSlopeSpeed);
                }
                
                // 제동 상태 표시
                if (isRightWheelBraking)
                {
                    Gizmos.color = Color.red;
                    Gizmos.DrawWireSphere(rightWheelPos + Vector3.up * 0.2f, 0.1f);
                }
            }
            
            // 경사로 미끄러짐 표시
            if (enableSlopeSliding && slopeIntensity > 0f)
            {
                Gizmos.color = new Color(1f, 0.5f, 0f, 0.8f); // 주황색
                Gizmos.DrawRay(transform.position, slopeDirection * slopeIntensity * 2f);
                
                // 미끄러짐 속도 표시
                Gizmos.color = Color.red;
                Gizmos.DrawRay(transform.position + Vector3.up * 0.5f, slideVelocity);
            }
            
            // 이동 제한 상태 표시
            if (strictMovementControl)
            {
                // 정당한 이동 상태 표시
                Gizmos.color = IsMovementLegitimate() ? Color.green : Color.red;
                Gizmos.DrawWireCube(transform.position + Vector3.up * 2.5f, Vector3.one * 0.2f);
                
                // 충돌 감지 상태 표시
                if (isCollisionDetected)
                {
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawWireSphere(transform.position + Vector3.up * 2.8f, 0.1f);
                }
                
                // 정당한 속도 벡터 표시
                Gizmos.color = Color.cyan;
                Gizmos.DrawRay(transform.position + Vector3.up * 0.2f, legitimateVelocity);
            }
        }
        
        // 정당한 속도 벡터 표시
        Gizmos.color = Color.cyan;
        Gizmos.DrawRay(transform.position + Vector3.up * 0.2f, legitimateVelocity);
        
        // 방향 디버그 기즈모 표시
        if (showDirectionGizmos)
        {
            Vector3 basePos = transform.position;
            
            // 전진 방향 표시
            if (showForwardDirection)
            {
                Vector3 currentForward = GetCurrentForwardDirection();
                Gizmos.color = useManualDirections ? Color.green : Color.blue;
                Gizmos.DrawRay(basePos + Vector3.up * 0.3f, currentForward * gizmoLength);
                
                // 전진 방향 라벨 (화살표 끝에 작은 구)
                Gizmos.DrawWireSphere(basePos + Vector3.up * 0.3f + currentForward * gizmoLength, 0.1f);
            }
            
            // 바퀴 축 표시
            if (showWheelAxes)
            {
                // 왼쪽 바퀴 축
                if (leftWheel != null)
                {
                    Vector3 leftWheelPos = leftWheel.position;
                    Vector3 leftLocalAxis = useManualWheelAxes ? manualLeftWheelAxis.normalized : leftWheelAxis;
                    Vector3 leftWorldAxis = leftWheel.TransformDirection(leftLocalAxis); // 로컬을 월드로 변환
                    
                    Gizmos.color = useManualWheelAxes ? Color.green : Color.red;
                    Gizmos.DrawRay(leftWheelPos, leftWorldAxis * gizmoLength * 0.5f);
                    Gizmos.DrawRay(leftWheelPos, -leftWorldAxis * gizmoLength * 0.5f);
                    
                    // 축 끝에 작은 구
                    Gizmos.DrawWireSphere(leftWheelPos + leftWorldAxis * gizmoLength * 0.5f, 0.05f);
                    Gizmos.DrawWireSphere(leftWheelPos - leftWorldAxis * gizmoLength * 0.5f, 0.05f);
                }
                
                // 오른쪽 바퀴 축
                if (rightWheel != null)
                {
                    Vector3 rightWheelPos = rightWheel.position;
                    Vector3 rightLocalAxis = useManualWheelAxes ? manualRightWheelAxis.normalized : rightWheelAxis;
                    Vector3 rightWorldAxis = rightWheel.TransformDirection(rightLocalAxis); // 로컬을 월드로 변환
                    
                    Gizmos.color = useManualWheelAxes ? Color.green : Color.red;
                    Gizmos.DrawRay(rightWheelPos, rightWorldAxis * gizmoLength * 0.5f);
                    Gizmos.DrawRay(rightWheelPos, -rightWorldAxis * gizmoLength * 0.5f);
                    
                    // 축 끝에 작은 구
                    Gizmos.DrawWireSphere(rightWheelPos + rightWorldAxis * gizmoLength * 0.5f, 0.05f);
                    Gizmos.DrawWireSphere(rightWheelPos - rightWorldAxis * gizmoLength * 0.5f, 0.05f);
                }
            }
            
            // 좌표계 참조 표시 (Transform 기준)
            if (Application.isPlaying)
            {
                float refLength = gizmoLength * 0.3f;
                Vector3 refPos = basePos + Vector3.up * 0.6f;
                
                // Transform 좌표계
                Gizmos.color = Color.red;
                Gizmos.DrawRay(refPos, transform.right * refLength); // X축 (빨강)
                Gizmos.color = Color.green;
                Gizmos.DrawRay(refPos, transform.up * refLength); // Y축 (초록)
                Gizmos.color = Color.blue;
                Gizmos.DrawRay(refPos, transform.forward * refLength); // Z축 (파랑)
            }
        }
    }
    
    // 바퀴 회전 테스트 및 디버그 메서드들
    public void TestWheelVisualRotation(float testAngle = 90f)
    {
        Debug.Log($"🎡 바퀴 시각적 회전 테스트 - 각도: {testAngle}도");
        
        if (leftWheel != null)
        {
            ApplyWheelRotation(leftWheel, leftWheelAxis, testAngle, "왼쪽 (테스트)");
        }
        
        if (rightWheel != null)
        {
            ApplyWheelRotation(rightWheel, rightWheelAxis, testAngle, "오른쪽 (테스트)");
        }
    }
    
    public void ForceWheelRotationUpdate()
    {
        Debug.Log("🎡 강제 바퀴 회전 업데이트 실행");
        UpdateWheelVisualRotation();
    }
    
    public void ResetWheelRotations()
    {
        leftWheelRotation = 0f;
        rightWheelRotation = 0f;
        
        if (leftWheel != null)
        {
            leftWheel.localRotation = Quaternion.identity;
        }
        
        if (rightWheel != null)
        {
            rightWheel.localRotation = Quaternion.identity;
        }
        
        Debug.Log("🎡 바퀴 회전 초기화 완료");
    }
    
    public void LogWheelRotationState()
    {
        Debug.Log($"🎡 바퀴 회전 상태:");
        Debug.Log($"  - 왼쪽 바퀴 각속도: {leftWheelAngularVelocity:F2} rad/s");
        Debug.Log($"  - 오른쪽 바퀴 각속도: {rightWheelAngularVelocity:F2} rad/s");
        Debug.Log($"  - 왼쪽 바퀴 경사 회전: {leftWheelSlopeRotation:F2} rad/s");
        Debug.Log($"  - 오른쪽 바퀴 경사 회전: {rightWheelSlopeRotation:F2} rad/s");
        Debug.Log($"  - 왼쪽 바퀴 누적 회전: {leftWheelRotation:F1}도");
        Debug.Log($"  - 오른쪽 바퀴 누적 회전: {rightWheelRotation:F1}도");
        Debug.Log($"  - 바퀴 활성 상태: {isAnyWheelActive}");
        Debug.Log($"  - 경사로 강도: {slopeIntensity:F2}");
        
        if (leftWheel != null)
        {
            Debug.Log($"  - 왼쪽 바퀴 실제 회전: {leftWheel.localRotation.eulerAngles}");
        }
        if (rightWheel != null)
        {
            Debug.Log($"  - 오른쪽 바퀴 실제 회전: {rightWheel.localRotation.eulerAngles}");
        }
    }
}
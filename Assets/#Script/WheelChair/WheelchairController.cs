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
    public float hoverDamping = 1000f; // 부양 댐핑
    public float hoverStiffness = 5000f; // 부양 강성
    
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
    public Vector3 leftWheelAxis = Vector3.right; // 왼쪽 바퀴 회전축 (수동 설정시)
    public Vector3 rightWheelAxis = Vector3.right; // 오른쪽 바퀴 회전축 (수동 설정시)
    
    [Header("🏔️ 경사로 미끄러짐 시스템")]
    public bool enableSlopeSliding = true; // 경사로 미끄러짐 활성화
    public float slopeThreshold = 5f; // 미끄러짐 시작 각도 (도)
    public float maxSlideAngle = 45f; // 최대 미끄러짐 각도 (도)
    public float slideForce = 2000f; // 미끄러짐 힘
    public float slideFriction = 0.3f; // 미끄러짐 마찰력 (낮을수록 더 미끄러짐)
    public float wheelGripOnSlope = 0.7f; // 경사로에서 바퀴 그립력 (0~1)
    public AnimationCurve slopeCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f); // 경사 강도 곡선
    
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
    
    void Start()
    {
        InitializeSuperconductorSystem();
        
        // 경사 곡선 기본 설정
        if (slopeCurve.keys.Length == 0)
        {
            slopeCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        }
    }
    
    void InitializeSuperconductorSystem()
    {
        // Rigidbody 설정
        if (chairRigidbody == null)
            chairRigidbody = GetComponent<Rigidbody>();
            
        // 초전도체 부양을 위한 물리 설정
        chairRigidbody.mass = chairMass;
        chairRigidbody.useGravity = false; // 중력 비활성화
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
        
        Debug.Log("🔋 초전도체 부양 시스템 초기화 완료 - 순수 바퀴 주도 + 경사로 미끄러짐");
        Debug.Log($"부양 높이: {hoverHeight}m, 바퀴 반지름: {wheelRadius}m");
        Debug.Log($"왼쪽 바퀴 회전축: {leftWheelAxis}, 오른쪽 바퀴 회전축: {rightWheelAxis}");
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
        // 휠체어의 전진 방향
        Vector3 chairForward = transform.forward;
        
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
        
        Vector3 detectedAxis;
        string axisName;
        
        // 가장 수직에 가까운 축을 회전축으로 선택
        if (dotX <= dotY && dotX <= dotZ)
        {
            detectedAxis = wheelRight;
            axisName = "X축 (Right)";
        }
        else if (dotY <= dotX && dotY <= dotZ)
        {
            detectedAxis = wheelUp;
            axisName = "Y축 (Up)";
        }
        else
        {
            detectedAxis = wheelForward;
            axisName = "Z축 (Forward)";
        }
        
        Debug.Log($"{wheelName} 바퀴 회전축 감지: {axisName} - 벡터: {detectedAxis}");
        Debug.Log($"{wheelName} 바퀴 내적값 - X: {dotX:F3}, Y: {dotY:F3}, Z: {dotZ:F3}");
        
        return detectedAxis;
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
            
            // 부양 힘 계산 (스프링-댐퍼 시스템)
            Vector3 hoverForceVector = groundNormals[i] * heightError * hoverStiffness;
            
            // 수직 속도 댐핑
            float verticalVelocity = Vector3.Dot(chairRigidbody.velocity, groundNormals[i]);
            Vector3 dampingForceVector = -groundNormals[i] * verticalVelocity * hoverDamping;
            
            // 힘 적용
            Vector3 totalForce = (hoverForceVector + dampingForceVector) * 0.25f; // 4개 포인트로 분산
            chairRigidbody.AddForceAtPosition(totalForce, pointPosition, ForceMode.Force);
        }
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
        
        // 바퀴 각속도 업데이트
        if (leftGrabbed)
        {
            // 사용자 입력으로 바퀴 각속도 직접 제어
            float inputAngularVelocity = leftRotationInput * wheelInputSensitivity;
            
            // 경사로에서 바퀴 그립력 적용
            if (enableSlopeSliding && slopeIntensity > 0f)
            {
                inputAngularVelocity *= wheelGripOnSlope;
            }
            
            leftWheelAngularVelocity = inputAngularVelocity;
            isLeftWheelActive = Mathf.Abs(inputAngularVelocity) > 0.01f;
        }
        else
        {
            // 마찰로 각속도 감소
            leftWheelAngularVelocity *= wheelFriction;
            // 추가 감속 (잡지 않을 때)
            leftWheelAngularVelocity = Mathf.Lerp(leftWheelAngularVelocity, 0f, wheelDecelerationRate);
            isLeftWheelActive = false;
        }
        
        if (rightGrabbed)
        {
            // 사용자 입력으로 바퀴 각속도 직접 제어
            float inputAngularVelocity = rightRotationInput * wheelInputSensitivity;
            
            // 경사로에서 바퀴 그립력 적용
            if (enableSlopeSliding && slopeIntensity > 0f)
            {
                inputAngularVelocity *= wheelGripOnSlope;
            }
            
            rightWheelAngularVelocity = inputAngularVelocity;
            isRightWheelActive = Mathf.Abs(inputAngularVelocity) > 0.01f;
        }
        else
        {
            // 마찰로 각속도 감소
            rightWheelAngularVelocity *= wheelFriction;
            // 추가 감속 (잡지 않을 때)
            rightWheelAngularVelocity = Mathf.Lerp(rightWheelAngularVelocity, 0f, wheelDecelerationRate);
            isRightWheelActive = false;
        }
        
        // 매우 작은 값은 0으로 처리 (완전 정지)
        if (Mathf.Abs(leftWheelAngularVelocity) < 0.01f) 
        {
            leftWheelAngularVelocity = 0f;
            isLeftWheelActive = false;
        }
        if (Mathf.Abs(rightWheelAngularVelocity) < 0.01f) 
        {
            rightWheelAngularVelocity = 0f;
            isRightWheelActive = false;
        }
        
        // 전체 바퀴 활성 상태 업데이트
        isAnyWheelActive = isLeftWheelActive || isRightWheelActive;
        
        // 디버그 정보
        if (enableWheelRotationDebug && isAnyWheelActive)
        {
            string leftStatus = isLeftWheelActive ? $"활성({leftWheelAngularVelocity:F2} rad/s)" : "비활성";
            string rightStatus = isRightWheelActive ? $"활성({rightWheelAngularVelocity:F2} rad/s)" : "비활성";
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
        Vector3 forwardDirection = transform.forward;
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
        
        // 디버그 정보
        if (enableWheelRotationDebug && isAnyWheelActive)
        {
            Debug.Log($"바퀴 회전 업데이트 - 왼쪽: {leftWheelRotation:F1}도 ({leftRotationDelta:F2}도/프레임), " +
                     $"오른쪽: {rightWheelRotation:F1}도 ({rightRotationDelta:F2}도/프레임)");
        }
    }
    
    void ApplyWheelRotation(Transform wheel, Vector3 wheelAxis, float rotationAngle, string wheelName)
    {
        // 회전축에 따라 적절한 Euler 각도 적용
        Vector3 eulerRotation = Vector3.zero;
        
        // 월드 좌표계의 회전축을 로컬 좌표계로 변환
        Vector3 localAxis = wheel.InverseTransformDirection(wheelAxis);
        
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
        
        wheel.localRotation = Quaternion.Euler(eulerRotation);
        
        // 디버그 정보
        if (enableWheelRotationDebug && Mathf.Abs(rotationAngle) > 1f)
        {
            Debug.Log($"{wheelName} 바퀴 시각적 회전 - 월드축: {wheelAxis}, 로컬축: {localAxis}, 오일러: {eulerRotation}");
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
        
        // 최대 각속도 제한 (Y축만)
        Vector3 angularVelocity = chairRigidbody.angularVelocity;
        float maxAngularVel = maxAngularSpeed * Mathf.Deg2Rad;
        
        if (Mathf.Abs(angularVelocity.y) > maxAngularVel)
        {
            float clampedY = Mathf.Clamp(angularVelocity.y, -maxAngularVel, maxAngularVel);
            chairRigidbody.angularVelocity = new Vector3(angularVelocity.x, clampedY, angularVelocity.z);
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
            }
            
            if (rightWheel != null)
            {
                Gizmos.color = isRightWheelActive ? Color.red : Color.gray;
                Vector3 rightWheelPos = rightWheel.position;
                float rightSpeed = rightWheelAngularVelocity * wheelRadius;
                Gizmos.DrawRay(rightWheelPos, transform.forward * rightSpeed);
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
        }
    }
}
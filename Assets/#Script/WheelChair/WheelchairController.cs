using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Autohand;
using System.Reflection;

public class WheelchairController : MonoBehaviour
{
    [Header("🔋 초전도체 부양 시스템")]
    public bool enableSuperconductorHover = true;
    public float hoverHeight = 0.3f;
    public float minHoverHeight = 0.1f;
    public float hoverForce = 8000f;
    public float hoverDamping = 2000f;
    public float hoverStiffness = 3000f;
    
    [Header("🛡️ 안정성 제어 시스템")]
    public float stabilityForce = 15000f;
    public float stabilityDamping = 2000f;
    public float maxTiltAngle = 3f;
    public float stabilityResponseSpeed = 20f;
    public bool enableGyroscopicStabilization = true;
    
    [Header("🎯 4점 지면 감지 시스템")]
    public Transform[] groundDetectionPoints = new Transform[4];
    public float groundCheckDistance = 2f;
    public LayerMask groundLayer = 1;
    public float contactPointOffset = 0.05f;
    
    [Header("🚗 바퀴 시스템 (Z 회전 기반)")]
    public Transform leftWheelTransform;  // 왼쪽 바퀴 Transform
    public Transform rightWheelTransform; // 오른쪽 바퀴 Transform
    public Grabbable leftWheelGrabbable;  // 왼쪽 바퀴 Grabbable
    public Grabbable rightWheelGrabbable; // 오른쪽 바퀴 Grabbable
    
    [Header("🔄 바퀴 회전 설정")]
    public float wheelRadius = 0.3f; // 바퀴 반지름
    public float rotationFriction = 0.98f; // 회전 마찰력 (0~1, 높을수록 오래 굴러감)
    public float inputSensitivity = 2f; // 입력 감도
    public float slopeInfluence = 1f; // 경사로 영향력
    
    [Header("🎯 ICC 기반 회전 시스템")]
    [SerializeField, Tooltip("바퀴간 실제 거리 (자동 계산됨 - Transform 위치 기반)")]
    private float wheelbaseWidth = 0.6f; 
    public bool enableICCRotation = true; // ICC 기반 회전 활성화
    public float rotationTorqueMultiplier = 500f; // 회전 토크 배율
    public float minTurningRadius = 0.1f; // 최소 회전 반지름 (제자리 회전 방지)
    public float maxTurningRadius = 50f; // 최대 회전 반지름
    public bool showICCGizmos = true; // ICC 기즈모 표시
    
    [Header("⚡ 회전 반응성 설정")]
    [Range(0.1f, 5.0f), Tooltip("회전 반응성 배율 (높을수록 빠른 회전)")]
    public float rotationResponsiveness = 1.0f; // 회전 반응성 배율
    [Range(0.1f, 3.0f), Tooltip("각속도 직접 배율 (계산된 각속도에 곱해짐)")]
    public float angularVelocityMultiplier = 1.0f; // 각속도 배율
    [Range(1f, 30f), Tooltip("회전 스무딩 속도 (높을수록 즉시 반응)")]
    public float rotationSmoothing = 8f; // 회전 스무딩 (기존 설정 이동)
    [Range(0.1f, 10f), Tooltip("회전 가속도 (회전 시작/정지 시 가속)")]
    public float rotationAcceleration = 2.0f; // 회전 가속도
    
    [Header("🛡️ 회전 안정성 설정")]
    [Range(1f, 20f), Tooltip("ICC 회전 스무딩 (높을수록 부드러운 회전)")]
    public float iccRotationSmoothing = 10f; // ICC 회전 스무딩
    [Range(0.1f, 5f), Tooltip("최대 회전 속도 제한 (도/프레임)")]
    public float maxRotationDeltaPerFrame = 2f; // 프레임당 최대 회전 변화량
    [Range(0.001f, 0.1f), Tooltip("최소 회전 임계값 (이하 무시)")]
    public float minRotationThreshold = 0.01f; // 최소 회전 임계값
    [Tooltip("급격한 회전 변화 감지 및 제한")]
    public bool enableRotationStabilization = true; // 회전 안정화 활성화
    
    [Header("🎮 이동 변환 설정")]
    public float movementScale = 0.1f; // Z 변화량을 이동거리로 변환하는 배율 (0.01에서 0.1로 증가)
    public float forwardSpeedMultiplier = 1f; // 전진 속도 배율
    public float backwardSpeedMultiplier = 0.8f; // 후진 속도 배율 (일반적으로 후진이 느림)
    public float maxSpeed = 8f; // 최대 이동 속도
    public float rotationScale = 0.1f; // 바퀴 차이를 회전속도로 변환하는 배율 (레거시용)
    
    [Header("🏔️ 경사로 미끄러짐 시스템")]
    public bool enableSlopeSliding = true;
    public float slopeThreshold = 5f;
    public float maxSlideAngle = 45f;
    public float slideForce = 2000f;
    public float slideFriction = 0.3f;
    public float slopeZRotationForce = 2f; // 경사로에서 바퀴에 가해지는 Z 회전 힘 (도/초)
    public AnimationCurve slopeCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    
    [Header("🎮 가상 경사로 시스템")]
    public bool enableVirtualSlopes = true;
    public float virtualSlopeMultiplier = 1f; // 가상 경사로 효과 배율
    public float virtualSlopeSmoothing = 5f; // 가상 경사로 힘 적용 부드러움
    
    [Header("🧭 방향 설정")]
    public bool useLocalForwardDirection = true;
    
    [Header("🏃 이동 제어")]
    public float maxAngularSpeed = 180f;
    public float movementSmoothing = 15f; // 5에서 15로 증가 (더 빠른 반응)
    // rotationSmoothing은 위 "회전 반응성 설정"에서 정의됨
    
    [Header("🎛️ 물리 설정")]
    public Rigidbody chairRigidbody;
    public float chairMass = 80f;
    public float airResistance = 0.5f;
    public float angularDrag = 10f;
    
    [Header("🔒 이동 제한 설정")]
    public bool strictMovementControl = false;
    public float externalForceThreshold = 0.1f;
    public bool allowColliderInteraction = true;
    
    [Header("🔍 디버그 표시")]
    public bool enableDebugLog = true;
    public bool showDirectionGizmos = true;
    public float gizmoLength = 1f;
    
    // 바퀴 Z 회전 변화량 (핵심 변수)
    private float leftWheelDeltaZ = 0f;  // 왼쪽 바퀴 Z 변화량 (도/프레임)
    private float rightWheelDeltaZ = 0f; // 오른쪽 바퀴 Z 변화량 (도/프레임)
    
    // 입력 추적용 변수
    private Vector3 lastLeftHandPosition;
    private Vector3 lastRightHandPosition;
    private bool lastLeftGrabbed = false;
    private bool lastRightGrabbed = false;
    
    // 현재 바퀴 Z 회전값
    private float currentLeftWheelZ = 0f;
    private float currentRightWheelZ = 0f;
    
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
    
    // 이동 제한 관련 변수
    private Vector3 lastFrameVelocity = Vector3.zero;
    private Vector3 legitimateVelocity = Vector3.zero;
    private Vector3 lastPosition = Vector3.zero;
    private bool isCollisionDetected = false;
    
    // 가상 경사로 시스템
    private HashSet<object> activeVirtualSlopes = new HashSet<object>();
    private float currentVirtualSlopeForce = 0f;
    
    // ICC 기반 회전 시스템 변수들
    private Vector3 currentICC = Vector3.zero; // 현재 회전 중심점 (Instantaneous Center of Curvature)
    private float currentTurningRadius = float.MaxValue; // 현재 회전 반지름
    private float calculatedAngularVelocity = 0f; // ICC로 계산된 각속도
    private Vector3 rotationPivotPoint = Vector3.zero; // 실제 회전축 점
    private bool isRotating = false; // 현재 회전 중인지
    private float leftWheelLinearVelocity = 0f; // 왼쪽 바퀴 선속도
    private float rightWheelLinearVelocity = 0f; // 오른쪽 바퀴 선속도
    
    // ICC 축 기반 회전 시스템 변수들
    private float iccYRotationDelta = 0f; // ICC 축 기준 Y축 회전 변화량 (도/프레임)
    private float accumulatedIccRotation = 0f; // ICC 축 기준 누적 회전량
    private float smoothedRotationDelta = 0f; // 스무딩된 회전 변화량
    private float lastRotationDelta = 0f; // 이전 프레임 회전 변화량 (급격한 변화 감지용)
    
    void Start()
    {
        InitializeSuperconductorSystem();
        InitializeWheelZRotationSystem();
        
        // 바퀴 거리 자동 계산 초기화
        UpdateWheelbaseWidth();
        
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
        chairRigidbody.useGravity = false;
        chairRigidbody.drag = airResistance;
        chairRigidbody.angularDrag = angularDrag;
        chairRigidbody.centerOfMass = new Vector3(0, -0.2f, 0);
        chairRigidbody.maxAngularVelocity = maxAngularSpeed * Mathf.Deg2Rad;
        
        // 지면 감지 포인트 자동 생성 (없을 경우)
        if (groundDetectionPoints[0] == null)
        {
            CreateGroundDetectionPoints();
        }
        
        Debug.Log("🔋 초전도체 부양 시스템 초기화 완료 - Z 회전 기반 바퀴 시스템");
        Debug.Log($"부양 높이: {hoverHeight}m, 바퀴 반지름: {wheelRadius}m");
    }
    
    void InitializeWheelZRotationSystem()
    {
        // 바퀴 초기 회전값 설정
        if (leftWheelTransform != null)
        {
            currentLeftWheelZ = leftWheelTransform.localEulerAngles.z;
        }
        if (rightWheelTransform != null)
        {
            currentRightWheelZ = rightWheelTransform.localEulerAngles.z;
        }
        
        // 변화량 초기화
        leftWheelDeltaZ = 0f;
        rightWheelDeltaZ = 0f;
        
        Debug.Log("🚗 바퀴 Z 회전 시스템 초기화 완료");
        Debug.Log($"왼쪽 바퀴 초기 Z: {currentLeftWheelZ:F1}도, 오른쪽 바퀴 초기 Z: {currentRightWheelZ:F1}도");
    }
    
    void CreateGroundDetectionPoints()
    {
        // 휠체어 크기 기준으로 4개 포인트 생성
        float halfWidth = 0.4f;
        float halfLength = 0.6f;
        
        Vector3[] positions = {
            new Vector3(-halfWidth, contactPointOffset, halfLength),
            new Vector3(halfWidth, contactPointOffset, halfLength),
            new Vector3(-halfWidth, contactPointOffset, -halfLength),
            new Vector3(halfWidth, contactPointOffset, -halfLength)
        };
        
        for (int i = 0; i < 4; i++)
        {
            GameObject point = new GameObject($"GroundDetectionPoint_{i}");
            point.transform.SetParent(transform);
            point.transform.localPosition = positions[i];
            groundDetectionPoints[i] = point.transform;
        }
    }
    
    void FixedUpdate()
    {
        if (!enableSuperconductorHover) 
        {
            if (enableDebugLog)
            {
                Debug.LogWarning("⚠️ 초전도체 부양 시스템이 비활성화되어 있어 이동 시스템이 실행되지 않습니다!");
            }
            return;
        }
        
        // 1. 지면 감지 및 분석
        PerformGroundDetection();
        
        // 2. 경사로 분석
        AnalyzeSlope();
        
        // 3. 초전도체 부양 힘 적용
        ApplySuperconductorHover();
        
        // 4. 안정성 제어
        ApplyStabilityControl();
        
        // 5. 바퀴 Z 회전 입력 처리
        ProcessWheelZRotationInput();
        
        // 6. 경사로 효과를 Z 변화량에 적용
        ApplySlopeToZRotation();
        
        // 7. 가상 경사로 힘 처리
        if (enableVirtualSlopes)
        {
            ProcessVirtualSlopeForces();
        }
        
        // 8. 회전 마찰력 적용
        ApplyRotationFriction();
        
        // 9. 바퀴 Z 회전 업데이트
        UpdateWheelZRotations();
        
        // 10. Z 변화량으로부터 휠체어 이동 계산
        CalculateMovementFromZRotation();
        
        // 11. 계산된 이동 적용
        ApplyCalculatedMovement();
        
        // 12. 물리 제한 적용
        ApplyPhysicsLimits();
        
        // 13. 이동 제한 검사 및 적용
        if (strictMovementControl)
        {
            EnforceMovementRestrictions();
        }
        
        // 14. 이동 진행 상황 디버그 (활성 바퀴가 있을 때만)
        if (enableDebugLog && (Mathf.Abs(leftWheelDeltaZ) > 0.01f || Mathf.Abs(rightWheelDeltaZ) > 0.01f))
        {
            if (Time.fixedTime % 1f < Time.fixedDeltaTime) // 1초마다 출력
            {
                Debug.Log($"🔄 FixedUpdate 상태 - deltaZ: L{leftWheelDeltaZ:F2}/R{rightWheelDeltaZ:F2}, 목표속도: {targetVelocity.magnitude:F2}m/s, 실제속도: {chairRigidbody.velocity.magnitude:F2}m/s");
            }
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
            
            if (Physics.Raycast(rayStart, Vector3.down, out hit, groundCheckDistance, groundLayer))
            {
                groundDistances[i] = hit.distance;
                groundPoints[i] = hit.point;
                groundNormals[i] = hit.normal;
                groundDetected[i] = true;
                
                averageNormal += hit.normal;
                averageHeight += hit.point.y;
                validPoints++;
                
                Debug.DrawLine(rayStart, hit.point, Color.green);
            }
            else
            {
                groundDetected[i] = false;
                groundDistances[i] = groundCheckDistance;
                Debug.DrawRay(rayStart, Vector3.down * groundCheckDistance, Color.red);
            }
        }
        
        // 평균 지면 법선 계산
        if (validPoints > 0)
        {
            targetUpDirection = (averageNormal / validPoints).normalized;
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
            // 경사 방향 계산 (아래로 향하는 방향)
            Vector3 horizontalNormal = Vector3.ProjectOnPlane(targetUpDirection, Vector3.up);
            slopeDirection = -horizontalNormal.normalized;
            
            // 경사 강도 계산 (0~1)
            float normalizedAngle = Mathf.Clamp01((currentSlopeAngle - slopeThreshold) / (maxSlideAngle - slopeThreshold));
            slopeIntensity = slopeCurve.Evaluate(normalizedAngle);
            
            // 경사로 분석 디버그
            if (enableDebugLog && Time.fixedTime % 2f < Time.fixedDeltaTime) // 2초마다 출력
            {
                Vector3 chairForward = GetCurrentForwardDirection();
                float slopeForwardDot = Vector3.Dot(slopeDirection, chairForward);
                
                Debug.Log($"🏔️ 경사로 분석 - 각도: {currentSlopeAngle:F1}도, 강도: {slopeIntensity:F2}");
                Debug.Log($"    지면 법선: {targetUpDirection}, 경사 방향: {slopeDirection}");
                Debug.Log($"    휠체어 전진: {chairForward}, 내적: {slopeForwardDot:F2}");
                Debug.Log($"    해석: {(slopeForwardDot > 0 ? "경사로 아래 방향" : slopeForwardDot < 0 ? "경사로 위 방향" : "수직 경사")}");
            }
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
        
        float hoverTransitionRange = hoverHeight + 1.0f;
        
        if (anyGroundDetected && minDistanceToGround <= hoverTransitionRange)
        {
            float hoverInfluence = CalculateHoverInfluence(averageDistanceToGround);
            
            // 각 감지 포인트에서 개별적으로 부양 힘 적용
            for (int i = 0; i < 4; i++)
            {
                if (!groundDetected[i]) continue;
                
                Vector3 pointPosition = groundDetectionPoints[i].position;
                float targetHeight = groundPoints[i].y + hoverHeight;
                float currentHeight = pointPosition.y;
                float heightError = targetHeight - currentHeight;
                
                if (currentHeight - groundPoints[i].y < minHoverHeight)
                {
                    heightError = Mathf.Max(heightError, minHoverHeight - (currentHeight - groundPoints[i].y));
                }
                
                float proportionalForce = heightError * hoverStiffness;
                float verticalVelocity = Vector3.Dot(chairRigidbody.velocity, Vector3.up);
                float dampingForce = -verticalVelocity * hoverDamping * 2f;
                
                Vector3 hoverForceVector = Vector3.up * (proportionalForce + dampingForce) * hoverInfluence * 0.25f;
                chairRigidbody.AddForceAtPosition(hoverForceVector, pointPosition, ForceMode.Force);
            }
            
            float gravityInfluence = 1f - hoverInfluence;
            if (gravityInfluence > 0f)
            {
                Vector3 partialGravityForce = Vector3.down * chairMass * 9.81f * gravityInfluence;
                chairRigidbody.AddForce(partialGravityForce, ForceMode.Force);
            }
        }
        else
        {
            Vector3 gravityForce = Vector3.down * chairMass * 9.81f;
            chairRigidbody.AddForce(gravityForce, ForceMode.Force);
        }
    }
    
    float CalculateHoverInfluence(float distanceToGround)
    {
        if (distanceToGround <= hoverHeight)
        {
            return 1f;
        }
        
        float transitionRange = 1.0f;
        float excessDistance = distanceToGround - hoverHeight;
        
        if (excessDistance >= transitionRange)
        {
            return 0f;
        }
        
        float normalizedDistance = excessDistance / transitionRange;
        return Mathf.Cos(normalizedDistance * Mathf.PI * 0.5f);
    }
    
    void ApplyStabilityControl()
    {
        if (!enableGyroscopicStabilization) return;
        
        Vector3 currentUp = transform.up;
        Vector3 rotationError = Vector3.Cross(currentUp, targetUpDirection);
        float errorMagnitude = rotationError.magnitude;
        
        float tiltAngle = Vector3.Angle(currentUp, Vector3.up);
        if (tiltAngle > maxTiltAngle)
        {
            Vector3 correctionAxis = Vector3.Cross(currentUp, Vector3.up);
            float correctionMagnitude = (tiltAngle - maxTiltAngle) * stabilityForce;
            Vector3 correctionTorque = correctionAxis.normalized * correctionMagnitude;
            chairRigidbody.AddTorque(correctionTorque, ForceMode.Force);
        }
        
        if (errorMagnitude > 0.01f && currentStability > 0.5f)
        {
            Vector3 stabilityTorque = rotationError * stabilityForce * stabilityResponseSpeed * 0.1f;
            chairRigidbody.AddTorque(stabilityTorque, ForceMode.Force);
        }
        
        Vector3 angularVelocity = chairRigidbody.angularVelocity;
        Vector3 angularDamping = -angularVelocity * stabilityDamping;
        chairRigidbody.AddTorque(angularDamping, ForceMode.Force);
    }
    
    void ProcessWheelZRotationInput()
    {
        // 왼쪽 바퀴 입력 처리
        bool leftGrabbed = leftWheelGrabbable != null && leftWheelGrabbable.GetHeldBy().Count > 0;
        float leftZInput = GetWheelZRotationInput(leftWheelGrabbable, leftWheelTransform, ref lastLeftHandPosition, ref lastLeftGrabbed, "왼쪽");
        
        // 오른쪽 바퀴 입력 처리  
        bool rightGrabbed = rightWheelGrabbable != null && rightWheelGrabbable.GetHeldBy().Count > 0;
        float rightZInput = GetWheelZRotationInput(rightWheelGrabbable, rightWheelTransform, ref lastRightHandPosition, ref lastRightGrabbed, "오른쪽");
        
        // 입력이 있으면 deltaZ 설정 (각 바퀴의 고유한 방향성 유지)
        if (leftGrabbed && Mathf.Abs(leftZInput) > 0.1f)
        {
            leftWheelDeltaZ = leftZInput * inputSensitivity; // 왼쪽: +Z = 전진
        }
        
        if (rightGrabbed && Mathf.Abs(rightZInput) > 0.1f)
        {
            rightWheelDeltaZ = rightZInput * inputSensitivity; // 오른쪽: -Z = 전진
        }
        
        // 디버그 로그
        if (enableDebugLog && (Mathf.Abs(leftZInput) > 0.1f || Mathf.Abs(rightZInput) > 0.1f))
        {
            Debug.Log($"🎮 바퀴 입력 - 왼쪽: {leftZInput:F2}→{leftWheelDeltaZ:F2} (+Z=전진), 오른쪽: {rightZInput:F2}→{rightWheelDeltaZ:F2} (-Z=전진)");
        }
    }
    
    float GetWheelZRotationInput(Grabbable grabbable, Transform wheelTransform, ref Vector3 lastHandPos, ref bool lastGrabbed, string wheelName)
    {
        if (grabbable == null || grabbable.GetHeldBy().Count == 0 || wheelTransform == null)
        {
            lastGrabbed = false;
            return 0f;
        }
        
        Hand hand = grabbable.GetHeldBy()[0] as Hand;
        if (hand == null) return 0f;
        
        Vector3 handPos = hand.transform.position;
        
        if (!lastGrabbed)
        {
            lastHandPos = handPos;
            lastGrabbed = true;
            return 0f;
        }
        
        // 바퀴 중심 기준으로 손의 원형 이동 계산
        Vector3 wheelCenter = wheelTransform.position;
        Vector3 lastRelative = lastHandPos - wheelCenter;
        Vector3 currentRelative = handPos - wheelCenter;
        
        // 바퀴의 Y축(수직축) 기준으로 회전각 계산
        Vector3 lastProjected = Vector3.ProjectOnPlane(lastRelative, Vector3.up);
        Vector3 currentProjected = Vector3.ProjectOnPlane(currentRelative, Vector3.up);
        
        if (lastProjected.magnitude < 0.01f || currentProjected.magnitude < 0.01f)
        {
            lastHandPos = handPos;
            return 0f;
        }
        
        // Z축 회전량 계산 (도 단위)
        float angle = Vector3.SignedAngle(lastProjected.normalized, currentProjected.normalized, Vector3.up);
        
        // 너무 큰 각도 변화는 노이즈로 간주
        if (Mathf.Abs(angle) > 45f)
        {
            lastHandPos = handPos;
            return 0f;
        }
        
        lastHandPos = handPos;
        
        // 반환값: 양수 = 뒤로(후진), 음수 = 앞으로(전진)
        return -angle; // 일반적인 바퀴 회전 방향과 일치시키기 위해 반전
    }
    
    void ApplySlopeToZRotation()
    {
        if (!enableSlopeSliding || slopeIntensity <= 0f) return;
        
        // 경사로 방향과 휠체어 전진 방향의 관계 계산
        Vector3 chairForward = GetCurrentForwardDirection();
        float slopeForwardDot = Vector3.Dot(slopeDirection, chairForward);
        
        // 경사로에서 아래로 미끄러질 때의 효과 계산
        // 부호를 반전시켜 바퀴가 올바른 방향(아래로 굴러가는 방향)으로 회전하도록 함
        float slopeForwardEffect = -slopeIntensity * slopeInfluence * slopeForwardDot * slopeZRotationForce * Time.fixedDeltaTime;
        
        // 각 바퀴의 전진 방향성에 맞게 적용
        // 경사로에서 아래로 미끄러질 때: 
        // - 왼쪽 바퀴: +Z 방향으로 회전 (전진)
        // - 오른쪽 바퀴: -Z 방향으로 회전 (전진)
        leftWheelDeltaZ += slopeForwardEffect;      // 왼쪽: 전진 방향 효과
        rightWheelDeltaZ += -slopeForwardEffect;    // 오른쪽: 전진 방향 효과
        
        if (enableDebugLog && Mathf.Abs(slopeForwardEffect) > 0.01f)
        {
            Debug.Log($"🏔️ 경사로 효과 적용 (수정된 방향): 왼쪽 +{slopeForwardEffect:F2}, 오른쪽 {-slopeForwardEffect:F2}도/프레임");
            Debug.Log($"    경사 방향 내적: {slopeForwardDot:F2}, 경사 강도: {slopeIntensity:F2}");
            Debug.Log($"    효과: 휠체어가 경사로 아래로 미끄러지며 바퀴가 전진 방향으로 회전");
            Debug.Log($"    총 deltaZ - 왼쪽: {leftWheelDeltaZ:F2}, 오른쪽: {rightWheelDeltaZ:F2}");
        }
    }
    
    void ApplyRotationFriction()
    {
        // 회전 마찰력으로 deltaZ를 0으로 서서히 수렴
        leftWheelDeltaZ *= rotationFriction;
        rightWheelDeltaZ *= rotationFriction;
        
        // 매우 작은 값은 0으로 처리
        if (Mathf.Abs(leftWheelDeltaZ) < 0.01f) leftWheelDeltaZ = 0f;
        if (Mathf.Abs(rightWheelDeltaZ) < 0.01f) rightWheelDeltaZ = 0f;
    }
    
    void UpdateWheelZRotations()
    {
        // 각 프레임마다 Z 변화량을 현재 Z 회전값에 추가
        currentLeftWheelZ += leftWheelDeltaZ;
        currentRightWheelZ += rightWheelDeltaZ;
        
        // 바퀴 Transform에 회전 적용
        if (leftWheelTransform != null)
        {
            Vector3 leftEuler = leftWheelTransform.localEulerAngles;
            leftEuler.z = currentLeftWheelZ;
            leftWheelTransform.localEulerAngles = leftEuler;
        }
        
        if (rightWheelTransform != null)
        {
            Vector3 rightEuler = rightWheelTransform.localEulerAngles;
            rightEuler.z = currentRightWheelZ;
            rightWheelTransform.localEulerAngles = rightEuler;
        }
        
        // 디버그 로그
        if (enableDebugLog && (Mathf.Abs(leftWheelDeltaZ) > 0.1f || Mathf.Abs(rightWheelDeltaZ) > 0.1f))
        {
            Debug.Log($"🔄 바퀴 회전 업데이트 - 왼쪽 Z: {currentLeftWheelZ:F1}도 (+{leftWheelDeltaZ:F2}), 오른쪽 Z: {currentRightWheelZ:F1}도 (+{rightWheelDeltaZ:F2})");
        }
    }
    
    void CalculateMovementFromZRotation()
    {
        // 각 바퀴의 Z 회전 변화량을 선속도로 변환
        // 왼쪽 바퀴: +Z = 전진, -Z = 후진
        // 오른쪽 바퀴: -Z = 전진, +Z = 후진
        
        float leftAngularVel = leftWheelDeltaZ * Mathf.Deg2Rad; // 라디안으로 변환
        float rightAngularVel = -rightWheelDeltaZ * Mathf.Deg2Rad; // 오른쪽은 부호 반전
        
        // 바퀴의 각속도를 선속도로 변환 (v = ωr)
        leftWheelLinearVelocity = leftAngularVel * wheelRadius * movementScale;
        rightWheelLinearVelocity = rightAngularVel * wheelRadius * movementScale;
        
        if (enableICCRotation)
        {
            CalculateICCBasedMovement();
        }
        else
        {
            CalculateLegacyMovement();
        }
        
        // 속도 제한 적용
        if (targetVelocity.magnitude > maxSpeed)
        {
            targetVelocity = targetVelocity.normalized * maxSpeed;
        }
        
        // 정당한 속도 저장 (이동 제한 시스템용)
        legitimateVelocity = targetVelocity;
        
        // 디버그 정보
        if (enableDebugLog && (Mathf.Abs(leftWheelLinearVelocity) > 0.01f || Mathf.Abs(rightWheelLinearVelocity) > 0.01f))
        {
            if (enableICCRotation)
            {
                Debug.Log($"🎯 ICC 기반 이동 - 왼쪽: {leftWheelLinearVelocity:F2}m/s, 오른쪽: {rightWheelLinearVelocity:F2}m/s");
                Debug.Log($"    회전 반지름: {currentTurningRadius:F2}m, 각속도: {calculatedAngularVelocity * Mathf.Rad2Deg:F2}도/초");
                Debug.Log($"    ICC 위치: {currentICC}, 회전 중심: {rotationPivotPoint}");
            }
            else
            {
                Debug.Log($"🚗 레거시 이동 - 왼쪽: {leftWheelLinearVelocity:F2}m/s, 오른쪽: {rightWheelLinearVelocity:F2}m/s");
            }
        }
    }
    
    void CalculateICCBasedMovement()
    {
        float vL = leftWheelLinearVelocity;
        float vR = rightWheelLinearVelocity;
        
        // 실제 바퀴 Transform 위치에서 거리 계산
        float L = GetActualWheelbaseWidth();
        
        // 직진 체크 (속도 차이가 매우 작은 경우)
        if (Mathf.Abs(vR - vL) < 0.001f)
        {
            // 직진 이동
            float averageVelocity = (vL + vR) * 0.5f;
            
            // 속도 배율 적용 (전진/후진 구분)
            float straightSpeedMultiplier = averageVelocity >= 0 ? forwardSpeedMultiplier : backwardSpeedMultiplier;
            averageVelocity *= straightSpeedMultiplier;
            
            Vector3 straightDirection = GetCurrentForwardDirection();
            targetVelocity = straightDirection * averageVelocity;
            targetAngularVelocity = 0f;
            calculatedAngularVelocity = 0f;
            currentTurningRadius = float.MaxValue;
            isRotating = false;
            iccYRotationDelta = 0f;
            
            // 직진 시 회전 관련 변수들 초기화 (떨림 방지)
            smoothedRotationDelta = Mathf.Lerp(smoothedRotationDelta, 0f, iccRotationSmoothing * Time.fixedDeltaTime * 2f);
            lastRotationDelta = 0f;
            
            return;
        }
        
        // ICC 기반 회전 계산
        isRotating = true;
        
        // 회전 반지름 계산: R = L * (vL + vR) / (2 * (vR - vL))
        float speedSum = vL + vR;
        float speedDiff = vR - vL;
        currentTurningRadius = L * speedSum / (2f * speedDiff);
        
        // 각속도 계산: ω = (vR - vL) / L (부호 반전으로 회전 방향 수정)
        calculatedAngularVelocity = -(speedDiff / L);
        
        // 새로운 회전 반응성 설정 적용
        calculatedAngularVelocity *= angularVelocityMultiplier * rotationResponsiveness;
        
        // 회전 반지름 제한 적용 (부호 유지)
        float limitedRadius = Mathf.Clamp(Mathf.Abs(currentTurningRadius), minTurningRadius, maxTurningRadius);
        if (currentTurningRadius < 0) limitedRadius = -limitedRadius;
        currentTurningRadius = limitedRadius;
        
        // ICC 위치 계산 (바퀴 중심점 기준 - 고정된 월드 좌표계 사용)
        Vector3 wheelCenterWorld = GetWheelCenterPoint();
        
        // 바퀴간 벡터를 월드 좌표계에서 계산 (회전에 무관하게 고정)
        Vector3 wheelDirection = (rightWheelTransform.position - leftWheelTransform.position).normalized;
        Vector3 iccOffsetWorld = wheelDirection * (-currentTurningRadius);
        
        currentICC = wheelCenterWorld + iccOffsetWorld;
        rotationPivotPoint = currentICC;
        
        // Y축 각속도 설정
        targetAngularVelocity = calculatedAngularVelocity;
        
        // ICC 축 기준 Y축 회전 변화량 계산
        iccYRotationDelta = calculatedAngularVelocity * Mathf.Rad2Deg * Time.fixedDeltaTime;
        
        // NaN 및 무한값 체크 (안정성)
        if (float.IsNaN(iccYRotationDelta) || float.IsInfinity(iccYRotationDelta))
        {
            iccYRotationDelta = 0f;
            Debug.LogWarning("⚠️ ICC 회전 변화량에서 NaN/Infinity 감지 - 0으로 설정");
        }
        
        // 극도로 작은 값은 0으로 처리 (부동소수점 오차 방지)
        if (Mathf.Abs(iccYRotationDelta) < 0.0001f)
        {
            iccYRotationDelta = 0f;
        }
        
        accumulatedIccRotation += iccYRotationDelta;
        
        // 디버그 정보
        if (enableDebugLog && Time.fixedTime % 1f < Time.fixedDeltaTime)
        {
            string wheelComparison = vL > vR ? "왼쪽 바퀴가 더 빠름" : vR > vL ? "오른쪽 바퀴가 더 빠름" : "양쪽 동일";
            string expectedRotation = vL > vR ? "우회전" : vR > vL ? "좌회전" : "직진";
            string actualRotation = calculatedAngularVelocity > 0 ? "좌회전 (반시계)" : calculatedAngularVelocity < 0 ? "우회전 (시계)" : "회전 없음";
            
            Debug.Log($"🎯 ICC 원형 경로 계산 (강화된 반응성):");
            Debug.Log($"    ICC 위치: {currentICC}, 바퀴 회전반지름: {currentTurningRadius:F2}m");
            Debug.Log($"    강화된 각속도: {calculatedAngularVelocity * Mathf.Rad2Deg:F1}도/초");
            Debug.Log($"    🔄 Y축 회전 변화량: {iccYRotationDelta:F2}도/프레임");
            Debug.Log($"    🔄 누적 ICC 회전량: {accumulatedIccRotation:F1}도");
        }
    }
    
    void CalculateLegacyMovement()
    {
        // 기존 방식 (하위 호환성을 위해 유지) - 이중 스케일링 문제 수정
        float leftForwardAmount = leftWheelLinearVelocity;
        float rightForwardAmount = rightWheelLinearVelocity;
        
        // 전체 휠체어의 전진/후진 계산 (평균)
        float averageForwardAmount = (leftForwardAmount + rightForwardAmount) * 0.5f;
        
        // 회전 계산 (바퀴간 차이)
        float rotationDifference = leftForwardAmount - rightForwardAmount;
        
        // 전진/후진 이동 계산
        Vector3 legacyForwardDirection = GetCurrentForwardDirection();
        float rawSpeed = averageForwardAmount;
        
        // 전진/후진에 따른 속도 배율 적용
        float speedMultiplier = rawSpeed >= 0 ? forwardSpeedMultiplier : backwardSpeedMultiplier;
        float forwardSpeed = rawSpeed * speedMultiplier;
        
        targetVelocity = legacyForwardDirection * forwardSpeed;
        
        // 회전 계산 (라디안으로 변환)
        targetAngularVelocity = rotationDifference * rotationScale;
        
        isRotating = Mathf.Abs(rotationDifference) > 0.1f;
    }
    
    /// <summary>
    /// ICC 축을 중심으로 휠체어를 회전시키는 함수 (떨림 방지 개선)
    /// </summary>
    void ApplyICCRotation()
    {
        if (Mathf.Abs(iccYRotationDelta) < minRotationThreshold) 
        {
            smoothedRotationDelta = 0f;
            return; // 회전 변화량이 너무 작으면 무시
        }
        
        // 회전 안정화 활성화 시 추가 처리
        if (enableRotationStabilization)
        {
            // 급격한 회전 변화 감지 및 제한
            float rotationDeltaChange = Mathf.Abs(iccYRotationDelta - lastRotationDelta);
            float maxAllowedChange = maxRotationDeltaPerFrame * 0.5f; // 급격한 변화 허용 한계
            
            if (rotationDeltaChange > maxAllowedChange)
            {
                // 급격한 변화 시 이전 값과 보간
                float dampingFactor = 0.3f; // 급격한 변화 감쇠 계수
                iccYRotationDelta = Mathf.Lerp(lastRotationDelta, iccYRotationDelta, dampingFactor);
                
                if (enableDebugLog && Time.fixedTime % 1f < Time.fixedDeltaTime)
                {
                    Debug.Log($"⚠️ 급격한 회전 변화 감지 - 감쇠 적용: {rotationDeltaChange:F3}도 → {iccYRotationDelta:F3}도");
                }
            }
        }
        
        // 프레임당 최대 회전 속도 제한
        float clampedRotationDelta = Mathf.Clamp(iccYRotationDelta, -maxRotationDeltaPerFrame, maxRotationDeltaPerFrame);
        if (Mathf.Abs(clampedRotationDelta - iccYRotationDelta) > 0.001f)
        {
            if (enableDebugLog && Time.fixedTime % 1f < Time.fixedDeltaTime)
            {
                Debug.Log($"🛡️ 회전 속도 제한 적용: {iccYRotationDelta:F3}도 → {clampedRotationDelta:F3}도");
            }
            iccYRotationDelta = clampedRotationDelta;
        }
        
        // ICC 회전 스무딩 적용
        smoothedRotationDelta = Mathf.Lerp(smoothedRotationDelta, iccYRotationDelta, iccRotationSmoothing * Time.fixedDeltaTime);
        
        // 스무딩된 회전량이 충분히 클 때만 실제 회전 적용
        if (Mathf.Abs(smoothedRotationDelta) >= minRotationThreshold)
        {
            // Transform.RotateAround를 사용하여 ICC 축을 중심으로 부드러운 회전
            transform.RotateAround(currentICC, Vector3.up, smoothedRotationDelta);
            
            // 누적 회전량 업데이트
            accumulatedIccRotation += smoothedRotationDelta;
        }
        
        // 이전 프레임 값 저장
        lastRotationDelta = iccYRotationDelta;
        
        if (enableDebugLog && Time.fixedTime % 0.5f < Time.fixedDeltaTime && Mathf.Abs(smoothedRotationDelta) > 0.001f)
        {
            Debug.Log($"🌀 ICC 축 기준 안정화된 회전 적용:");
            Debug.Log($"    회전 중심 (ICC): {currentICC}");
            Debug.Log($"    원본 회전량: {iccYRotationDelta:F3}도");
            Debug.Log($"    스무딩된 회전량: {smoothedRotationDelta:F3}도");
            Debug.Log($"    누적 회전: {accumulatedIccRotation:F1}도");
            Debug.Log($"    휠체어 새 위치: {transform.position}");
            Debug.Log($"    휠체어 새 회전: {transform.eulerAngles.y:F1}도");
        }
    }
    
    void ApplyICCBasedMovement()
    {
        Vector3 currentVelocity = chairRigidbody.velocity;
        Vector3 verticalVelocity = new Vector3(0, currentVelocity.y, 0); // Y축 성분 보존 (부양 시스템용)
        
        // ICC 회전 중심을 기준으로 원운동 경로 적용
        if (isRotating && Mathf.Abs(currentTurningRadius) < maxTurningRadius)
        {
            // ICC 축을 중심으로 휠체어 회전 적용
            ApplyICCRotation();
            
            // 회전 중에는 수평 속도를 점진적으로 감소 (회전이 주된 이동 방식)
            Vector3 currentHorizontalVelocity = new Vector3(currentVelocity.x, 0, currentVelocity.z);
            float velocityDampingFactor = 0.1f; // 회전 중 속도 감쇠 계수
            Vector3 dampedHorizontalVelocity = Vector3.Lerp(currentHorizontalVelocity, Vector3.zero, velocityDampingFactor * Time.fixedDeltaTime);
            Vector3 finalVelocity = dampedHorizontalVelocity + verticalVelocity;
            
            chairRigidbody.velocity = finalVelocity;
            
            // Angular velocity는 0으로 설정 (Transform.RotateAround가 회전 처리)
            Vector3 currentAngularVelocity = chairRigidbody.angularVelocity;
            chairRigidbody.angularVelocity = new Vector3(currentAngularVelocity.x, 0f, currentAngularVelocity.z);
            
            if (enableDebugLog && Time.fixedTime % 0.5f < Time.fixedDeltaTime)
            {
                string rotationDir = calculatedAngularVelocity > 0 ? "좌회전" : "우회전";
                Debug.Log($"🎯 ICC 축 기반 회전 적용 - {rotationDir}:");
                Debug.Log($"    ICC 위치: {currentICC}");
                Debug.Log($"    원본 회전량: {iccYRotationDelta:F3}도/프레임");
                Debug.Log($"    스무딩된 회전량: {smoothedRotationDelta:F3}도/프레임");
                Debug.Log($"    수평 속도 감쇠: {dampedHorizontalVelocity.magnitude:F2}m/s");
                Debug.Log($"    휠체어 위치: {transform.position}");
            }
        }
        else
        {
            // 직진 이동 - targetVelocity 사용
            Vector3 currentHorizontalVelocity = new Vector3(currentVelocity.x, 0, currentVelocity.z);
            Vector3 newHorizontalVelocity = Vector3.Lerp(currentHorizontalVelocity, targetVelocity, movementSmoothing * Time.fixedDeltaTime);
            Vector3 finalVelocity = newHorizontalVelocity + verticalVelocity;
            chairRigidbody.velocity = finalVelocity;
            
            // 직진 시에는 Y축 각속도를 부드럽게 0으로
            Vector3 currentAngularVelocity = chairRigidbody.angularVelocity;
            float angularDampingRate = rotationSmoothing * rotationAcceleration * Time.fixedDeltaTime;
            float newYAngularVelocity = Mathf.Lerp(currentAngularVelocity.y, 0f, angularDampingRate);
            chairRigidbody.angularVelocity = new Vector3(currentAngularVelocity.x, newYAngularVelocity, currentAngularVelocity.z);
            
            if (enableDebugLog && targetVelocity.magnitude > 0.01f && Time.fixedTime % 0.5f < Time.fixedDeltaTime)
            {
                Debug.Log($"🎯 ICC 직진 이동 - targetVelocity: {targetVelocity} (크기: {targetVelocity.magnitude:F2}m/s)");
            }
        }
    }
    
    void ApplyCalculatedMovement()
    {
        if (enableICCRotation && isRotating)
        {
            ApplyICCBasedMovement();
        }
        else
        {
            ApplyLegacyMovement();
        }
    }
    
    void ApplyLegacyMovement()
    {
        // 기존 방식 유지
        Vector3 currentVelocity = chairRigidbody.velocity;
        Vector3 horizontalVelocity = new Vector3(currentVelocity.x, 0, currentVelocity.z);
        Vector3 verticalVelocity = new Vector3(0, currentVelocity.y, 0);
        
        // 수평 이동만 바퀴에 의해 제어됨
        Vector3 newHorizontalVelocity = Vector3.Lerp(horizontalVelocity, targetVelocity, movementSmoothing * Time.fixedDeltaTime);
        
        // 최종 속도 적용 (수직 성분은 부양 시스템이 제어)
        Vector3 finalVelocity = newHorizontalVelocity + verticalVelocity;
        chairRigidbody.velocity = finalVelocity;
        
        // 각속도 적용
        Vector3 currentAngularVelocity = chairRigidbody.angularVelocity;
        float newYAngularVelocity = Mathf.Lerp(currentAngularVelocity.y, targetAngularVelocity, rotationSmoothing * Time.fixedDeltaTime);
        
        // Y축 회전만 바퀴에 의해 제어됨 (X, Z축은 안정성 시스템이 제어)
        chairRigidbody.angularVelocity = new Vector3(currentAngularVelocity.x, newYAngularVelocity, currentAngularVelocity.z);
        
        // 속도 적용 디버그 (목표 속도가 있을 때만)
        if (enableDebugLog && targetVelocity.magnitude > 0.01f && Time.fixedTime % 0.5f < Time.fixedDeltaTime)
        {
            Debug.Log($"⚡ 레거시 속도 적용 - 목표: {targetVelocity.magnitude:F3}m/s, 현재 수평: {horizontalVelocity.magnitude:F3}m/s");
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
        float maxVerticalSpeed = 3f;
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
        
        // 콜라이더 충돌 감지
        Vector3 expectedPosition = lastPosition + legitimateVelocity * Time.fixedDeltaTime;
        Vector3 actualPosition = transform.position;
        float positionDifference = Vector3.Distance(expectedPosition, actualPosition);
        
        // 콜라이더 충돌로 인한 이동 제한은 허용
        if (allowColliderInteraction && positionDifference > 0.01f)
        {
            isCollisionDetected = true;
        }
        else
        {
            isCollisionDetected = false;
            
            // 외부 힘이 임계값을 초과하는 경우 속도 보정
            if (externalForce > externalForceThreshold)
            {
                Vector3 correctedVelocity = new Vector3(legitimateVelocity.x, currentVelocity.y, legitimateVelocity.z);
                chairRigidbody.velocity = correctedVelocity;
                
                if (enableDebugLog)
                {
                    Debug.Log($"🔒 외부 힘 감지 및 보정 - 외부 힘 크기: {externalForce:F3}, 임계값: {externalForceThreshold}");
                }
            }
        }
        
        // 바퀴가 비활성이고 경사로도 없는 경우 수평 이동 완전 차단
        bool anyWheelActive = Mathf.Abs(leftWheelDeltaZ) > 0.01f || Mathf.Abs(rightWheelDeltaZ) > 0.1f;
        if (!anyWheelActive && slopeIntensity <= 0f && !isCollisionDetected)
        {
            Vector3 stoppedVelocity = new Vector3(0, currentVelocity.y, 0);
            chairRigidbody.velocity = Vector3.Lerp(currentVelocity, stoppedVelocity, 10f * Time.fixedDeltaTime);
            
            if (enableDebugLog && currentHorizontalVelocity.magnitude > 0.1f)
            {
                Debug.Log("🔒 바퀴 비활성 + 경사로 없음 → 수평 이동 차단");
            }
        }
        
        // 다음 프레임을 위한 데이터 저장
        lastFrameVelocity = currentVelocity;
        lastPosition = transform.position;
    }
    
    Vector3 GetCurrentForwardDirection()
    {
        return useLocalForwardDirection ? transform.forward : Vector3.forward;
    }
    
    // ========== 공개 API 메서드들 ==========
    
    /// <summary>
    /// 왼쪽 바퀴의 Z 변화량을 직접 설정
    /// </summary>
    public void SetLeftWheelDeltaZ(float deltaZ)
    {
        leftWheelDeltaZ = deltaZ;
        Debug.Log($"🔧 왼쪽 바퀴 deltaZ 설정: {deltaZ:F2}도/프레임");
    }
    
    /// <summary>
    /// 오른쪽 바퀴의 Z 변화량을 직접 설정
    /// 참고: 실제 적용 시에는 반전되지 않음 (내부적으로 입력에서만 반전)
    /// </summary>
    public void SetRightWheelDeltaZ(float deltaZ)
    {
        rightWheelDeltaZ = deltaZ;
        Debug.Log($"🔧 오른쪽 바퀴 deltaZ 설정: {deltaZ:F2}도/프레임");
    }
    
    /// <summary>
    /// 양쪽 바퀴의 Z 변화량을 동시에 설정 (직진)
    /// 참고: 전진하려면 양수 입력 (왼쪽 +Z, 오른쪽 -Z로 자동 변환)
    /// </summary>
    public void SetBothWheelsDeltaZ(float forwardAmount)
    {
        leftWheelDeltaZ = forwardAmount;   // 왼쪽: 입력값 그대로 (+가 전진)
        rightWheelDeltaZ = -forwardAmount; // 오른쪽: 반전 (-가 전진)
        Debug.Log($"🔧 양쪽 바퀴 deltaZ 설정: 왼쪽 {leftWheelDeltaZ:F2} (+Z=전진), 오른쪽 {rightWheelDeltaZ:F2} (-Z=전진) → {(forwardAmount >= 0 ? "전진" : "후진")}");
    }
    
    /// <summary>
    /// 바퀴 시스템 정지 (모든 변화량을 0으로)
    /// </summary>
    public void StopWheels()
    {
        leftWheelDeltaZ = 0f;
        rightWheelDeltaZ = 0f;
        Debug.Log("🛑 바퀴 시스템 정지");
    }
    
    /// <summary>
    /// 현재 바퀴 상태 정보 반환
    /// </summary>
    public (float leftDeltaZ, float rightDeltaZ, float leftCurrentZ, float rightCurrentZ) GetWheelStatus()
    {
        return (leftWheelDeltaZ, rightWheelDeltaZ, currentLeftWheelZ, currentRightWheelZ);
    }
    
    /// <summary>
    /// 현재 이동 상태 정보 반환
    /// </summary>
    public (Vector3 velocity, float angularVelocity, float slopeAngle, float stability) GetMovementStatus()
    {
        return (chairRigidbody.velocity, chairRigidbody.angularVelocity.y, currentSlopeAngle, currentStability);
    }
    
    /// <summary>
    /// 바퀴가 잡혀있는지 확인
    /// </summary>
    public (bool leftGrabbed, bool rightGrabbed) GetGrabStatus()
    {
        bool leftGrabbed = leftWheelGrabbable != null && leftWheelGrabbable.GetHeldBy().Count > 0;
        bool rightGrabbed = rightWheelGrabbable != null && rightWheelGrabbable.GetHeldBy().Count > 0;
        return (leftGrabbed, rightGrabbed);
    }
    
    /// <summary>
    /// 현재 deltaZ 값들을 실시간으로 디버그 출력
    /// </summary>
    [ContextMenu("Debug Current DeltaZ")]
    public void DebugCurrentDeltaZ()
    {
        Debug.Log("═══════════════════════════════════════");
        Debug.Log("🔍 실시간 deltaZ 디버그");
        Debug.Log("═══════════════════════════════════════");
        Debug.Log($"왼쪽 바퀴 deltaZ: {leftWheelDeltaZ:F3}도/프레임");
        Debug.Log($"오른쪽 바퀴 deltaZ: {rightWheelDeltaZ:F3}도/프레임");
        Debug.Log($"평균 deltaZ: {(leftWheelDeltaZ + rightWheelDeltaZ) * 0.5f:F3}");
        Debug.Log($"차이 deltaZ: {rightWheelDeltaZ - leftWheelDeltaZ:F3}");
        Debug.Log($"예상 이동 속도: {-(leftWheelDeltaZ + rightWheelDeltaZ) * 0.5f * movementScale:F3}m/s");
        Debug.Log("═══════════════════════════════════════");
    }
    
    // ========== 테스트 메서드들 ==========
    
    /// <summary>
    /// ICC 시스템 상태 디버그
    /// </summary>
    [ContextMenu("Debug ICC System")]
    public void DebugICCSystem()
    {
        Debug.Log("══════════════════════════════════════════════════");
        Debug.Log("🎯 ICC 기반 회전 시스템 상태");
        Debug.Log("══════════════════════════════════════════════════");
        Debug.Log($"🔌 ICC 시스템 활성화: {enableICCRotation}");
        Debug.Log($"🔄 현재 회전 중: {isRotating}");
        
        // 실제 바퀴 거리 정보
        float actualDistance = GetActualWheelbaseWidth();
        Debug.Log($"📏 설정된 바퀴간 거리: {wheelbaseWidth}m");
        Debug.Log($"📐 실제 바퀴간 거리: {actualDistance:F3}m");
        
        if (Mathf.Abs(actualDistance - wheelbaseWidth) > 0.05f)
        {
            Debug.Log($"⚠️  실제 거리와 설정값이 다릅니다! 차이: {Mathf.Abs(actualDistance - wheelbaseWidth):F3}m");
        }
        
        Debug.Log($"⚙️ 바퀴 반지름: {wheelRadius}m");
        
        // 바퀴 위치 정보
        if (leftWheelTransform != null && rightWheelTransform != null)
        {
            Vector3 wheelCenter = GetWheelCenterPoint();
            Debug.Log($"🎯 바퀴 중심점: {wheelCenter}");
            Debug.Log($"🎯 왼쪽 바퀴: {leftWheelTransform.position}");
            Debug.Log($"🎯 오른쪽 바퀴: {rightWheelTransform.position}");
        }
        else
        {
            Debug.LogWarning("⚠️ 바퀴 Transform이 설정되지 않았습니다!");
        }
        
        Debug.Log("─────────────────────────────────────");
        Debug.Log("🚗 바퀴 선속도");
        Debug.Log("─────────────────────────────────────");
        Debug.Log($"왼쪽 바퀴: {leftWheelLinearVelocity:F3}m/s");
        Debug.Log($"오른쪽 바퀴: {rightWheelLinearVelocity:F3}m/s");
        Debug.Log($"속도 차이: {rightWheelLinearVelocity - leftWheelLinearVelocity:F3}m/s");
        
        if (isRotating)
        {
            Debug.Log("─────────────────────────────────────");
            Debug.Log("🎯 ICC 계산 결과");
            Debug.Log("─────────────────────────────────────");
            Debug.Log($"회전 반지름 (바퀴 기준): {currentTurningRadius:F2}m");
            Debug.Log($"각속도: {calculatedAngularVelocity * Mathf.Rad2Deg:F2}도/초");
            Debug.Log($"ICC 위치: {currentICC}");
            Debug.Log($"회전 중심: {rotationPivotPoint}");
            
            // 휠체어 원형 경로 정보 추가
            Vector3 chairToICC = currentICC - transform.position;
            float chairRotationRadius = new Vector3(chairToICC.x, 0, chairToICC.z).magnitude;
            float chairTangentialSpeed = Mathf.Abs(calculatedAngularVelocity) * chairRotationRadius;
            
            Debug.Log($"휠체어 회전 반지름: {chairRotationRadius:F2}m");
            Debug.Log($"휠체어 접선 속도: {chairTangentialSpeed:F2}m/s");
            Debug.Log($"계산된 targetVelocity: {targetVelocity}");
            Debug.Log($"targetVelocity 크기: {targetVelocity.magnitude:F2}m/s");
            
            string rotationDirection = calculatedAngularVelocity > 0 ? "좌회전 (반시계)" : "우회전 (시계)";
            string fasterWheel = rightWheelLinearVelocity > leftWheelLinearVelocity ? "오른쪽" : "왼쪽";
            Debug.Log($"회전 방향: {rotationDirection} ({fasterWheel} 바퀴가 더 빠름)");
            Debug.Log($"바퀴 속도 분석: 왼쪽 {leftWheelLinearVelocity:F3}m/s, 오른쪽 {rightWheelLinearVelocity:F3}m/s");
            
            // ICC 물리학적 설명
            string iccSide = currentTurningRadius > 0 ? "오른쪽" : "왼쪽";
            Debug.Log($"ICC 위치: 바퀴 중심에서 {iccSide}쪽 {Mathf.Abs(currentTurningRadius):F2}m 지점");
            
            // 회전 방향 검증
            bool isCorrectDirection = (rightWheelLinearVelocity > leftWheelLinearVelocity && calculatedAngularVelocity > 0) || 
                                    (leftWheelLinearVelocity > rightWheelLinearVelocity && calculatedAngularVelocity < 0);
            Debug.Log($"회전 방향 정확성: {(isCorrectDirection ? "✅ 정확함" : "❌ 오류")}");
            
            // 실제 거리 기반 계산 검증
            float speedSum = leftWheelLinearVelocity + rightWheelLinearVelocity;
            float speedDiff = rightWheelLinearVelocity - leftWheelLinearVelocity;
            float theoreticalRadius = actualDistance * speedSum / (2f * speedDiff);
            Debug.Log($"이론적 회전 반지름: {theoreticalRadius:F2}m (제한 전)");
            
            Debug.Log("─────────────────────────────────────");
            Debug.Log("🎯 휠체어 원형 경로 이동");
            Debug.Log("─────────────────────────────────────");
            Debug.Log($"휠체어 중심 → ICC 벡터: {chairToICC}");
            Debug.Log($"원형 경로 반지름: {chairRotationRadius:F2}m");
            Debug.Log($"원형 경로 접선 속도: {chairTangentialSpeed:F2}m/s");
            
            // 접선 방향 계산 및 표시
            Vector3 radiusVector = transform.position - currentICC;
            radiusVector.y = 0;
            if (radiusVector.magnitude > 0.001f)
            {
                Vector3 tangentDirection = Vector3.Cross(Vector3.up, radiusVector).normalized;
                if (calculatedAngularVelocity < 0) tangentDirection = -tangentDirection;
                Debug.Log($"접선 방향 벡터: {tangentDirection}");
                Debug.Log($"접선 방향 속도: {targetVelocity}");
                Debug.Log($"원형 이동 검증: v = ω × r = {calculatedAngularVelocity * Mathf.Rad2Deg:F1}° × {chairRotationRadius:F2}m = {chairTangentialSpeed:F2}m/s");
            }
            else
            {
                Debug.Log("ICC와 휠체어가 너무 가까움 - 제자리 회전");
            }
            
            // 토크 계산 정보 (사용하지 않지만 참고용)
            float torqueMagnitude = Mathf.Abs(calculatedAngularVelocity) * rotationTorqueMultiplier * chairRigidbody.mass;
            float distanceFactor = Mathf.Clamp01(maxTurningRadius / Mathf.Abs(currentTurningRadius));
            float finalTorque = torqueMagnitude * distanceFactor;
            
            Debug.Log("─────────────────────────────────────");
            Debug.Log("⚡ 각속도 제어 (직접 적용)");
            Debug.Log("─────────────────────────────────────");
            Debug.Log($"목표 각속도: {calculatedAngularVelocity * Mathf.Rad2Deg:F1}도/초");
            Debug.Log($"현재 Y축 각속도: {chairRigidbody.angularVelocity.y * Mathf.Rad2Deg:F1}도/초");
            Debug.Log($"회전 스무딩: {rotationSmoothing}");
            
            Debug.Log("─────────────────────────────────────");
            Debug.Log("🌀 ICC 축 기반 회전 시스템");
            Debug.Log("─────────────────────────────────────");
            Debug.Log($"Y축 회전 변화량: {iccYRotationDelta:F3}도/프레임");
            Debug.Log($"누적 ICC 회전량: {accumulatedIccRotation:F1}도");
            Debug.Log($"현재 휠체어 Y축 회전: {transform.eulerAngles.y:F1}도");
            Debug.Log($"ICC 축 기반 회전 활성화: {isRotating && Mathf.Abs(currentTurningRadius) < maxTurningRadius}");
            Debug.Log($"Transform.RotateAround 사용: ICC 축을 중심으로 실제 회전");
            Debug.Log($"Rigidbody 각속도: Transform 회전으로 대체됨");
            
            Debug.Log("─────────────────────────────────────");
            Debug.Log("🛡️ 회전 안정성 상태");
            Debug.Log("─────────────────────────────────────");
            Debug.Log($"스무딩된 회전량: {smoothedRotationDelta:F3}도/프레임");
            Debug.Log($"이전 프레임 회전량: {lastRotationDelta:F3}도/프레임");
            Debug.Log($"회전 안정화 활성화: {enableRotationStabilization}");
            Debug.Log($"ICC 회전 스무딩: {iccRotationSmoothing}");
            Debug.Log($"최대 회전 속도 제한: {maxRotationDeltaPerFrame}도/프레임");
            Debug.Log($"최소 회전 임계값: {minRotationThreshold}도");
            
            float rotationStability = Mathf.Abs(iccYRotationDelta - lastRotationDelta);
            string stabilityStatus = rotationStability < 0.1f ? "안정" : rotationStability < 0.5f ? "보통" : "불안정";
            Debug.Log($"회전 안정성: {stabilityStatus} (변화량: {rotationStability:F3}도)");
        }
        else
        {
            Debug.Log("📐 직진 이동 중 (회전 없음)");
        }
        
        Debug.Log("─────────────────────────────────────");
        Debug.Log("🎛️ ICC 시스템 설정");
        Debug.Log("─────────────────────────────────────");
        Debug.Log($"최소 회전반지름: {minTurningRadius}m");
        Debug.Log($"최대 회전반지름: {maxTurningRadius}m");
        Debug.Log($"토크 배율: {rotationTorqueMultiplier}");
        Debug.Log($"기즈모 표시: {showICCGizmos}");
        Debug.Log("══════════════════════════════════════════════════");
    }
    
    /// <summary>
    /// ICC 좌회전 테스트
    /// </summary>
    [ContextMenu("Test ICC Turn Left")]
    public void TestICCTurnLeft()
    {
        if (!enableICCRotation)
        {
            Debug.LogWarning("⚠️ ICC 시스템이 비활성화되어 있습니다!");
            return;
        }
        
        StopAllCoroutines();
        // 오른쪽 바퀴가 더 빠르게 회전 (좌회전) - 양수 각속도
        StartCoroutine(TestMovementCoroutine(1f, -3f, 5f, "ICC 좌회전 (수정됨)"));
        Debug.Log("🎯 ICC 좌회전 테스트 시작 - 오른쪽 바퀴가 더 빠름 → 좌회전 (반시계)");
    }
    
    /// <summary>
    /// ICC 우회전 테스트
    /// </summary>
    [ContextMenu("Test ICC Turn Right")]
    public void TestICCTurnRight()
    {
        if (!enableICCRotation)
        {
            Debug.LogWarning("⚠️ ICC 시스템이 비활성화되어 있습니다!");
            return;
        }
        
        StopAllCoroutines();
        // 왼쪽 바퀴가 더 빠르게 회전 (우회전) - 음수 각속도
        StartCoroutine(TestMovementCoroutine(3f, -1f, 5f, "ICC 우회전 (수정됨)"));
        Debug.Log("🎯 ICC 우회전 테스트 시작 - 왼쪽 바퀴가 더 빠름 → 우회전 (시계)");
    }
    
    /// <summary>
    /// ICC와 레거시 시스템 비교 테스트
    /// </summary>
    [ContextMenu("Compare ICC vs Legacy")]
    public void CompareICCvsLegacy()
    {
        Debug.Log("═══════════════════════════════════════");
        Debug.Log("⚖️ ICC vs 레거시 시스템 비교");
        Debug.Log("═══════════════════════════════════════");
        
        // 현재 상태 저장
        bool originalICCState = enableICCRotation;
        
        // 테스트 입력값
        float testLeftDelta = 2f;
        float testRightDelta = -1f;
        
        // ICC 모드 테스트
        enableICCRotation = true;
        leftWheelDeltaZ = testLeftDelta;
        rightWheelDeltaZ = testRightDelta;
        CalculateMovementFromZRotation();
        
        Vector3 iccTargetVel = targetVelocity;
        float iccAngularVel = targetAngularVelocity;
        
        Debug.Log($"🎯 ICC 모드 결과:");
        Debug.Log($"  선속도: {iccTargetVel.magnitude:F3}m/s");
        Debug.Log($"  각속도: {iccAngularVel * Mathf.Rad2Deg:F2}도/초");
        Debug.Log($"  회전반지름: {currentTurningRadius:F2}m");
        
        // 레거시 모드 테스트
        enableICCRotation = false;
        leftWheelDeltaZ = testLeftDelta;
        rightWheelDeltaZ = testRightDelta;
        CalculateMovementFromZRotation();
        
        Vector3 legacyTargetVel = targetVelocity;
        float legacyAngularVel = targetAngularVelocity;
        
        Debug.Log($"🚗 레거시 모드 결과:");
        Debug.Log($"  선속도: {legacyTargetVel.magnitude:F3}m/s");
        Debug.Log($"  각속도: {legacyAngularVel * Mathf.Rad2Deg:F2}도/초");
        
        // 차이점 분석
        float speedDiff = iccTargetVel.magnitude - legacyTargetVel.magnitude;
        float angularDiff = (iccAngularVel - legacyAngularVel) * Mathf.Rad2Deg;
        
        Debug.Log($"📊 차이점:");
        Debug.Log($"  선속도 차이: {speedDiff:F3}m/s");
        Debug.Log($"  각속도 차이: {angularDiff:F2}도/초");
        
        // 원래 상태 복원
        enableICCRotation = originalICCState;
        StopWheels();
        
        Debug.Log("═══════════════════════════════════════");
    }
    
    /// <summary>
    /// 전진 테스트 (5초간)
    /// </summary>
    public void TestForward()
    {
        StopAllCoroutines();
        StartCoroutine(TestMovementCoroutine(2f, -2f, 5f, "전진")); // 왼쪽 +Z, 오른쪽 -Z (둘 다 전진)
    }
    
    /// <summary>
    /// 후진 테스트 (5초간)
    /// </summary>
    public void TestBackward()
    {
        StopAllCoroutines();
        StartCoroutine(TestMovementCoroutine(-2f, 2f, 5f, "후진")); // 왼쪽 -Z, 오른쪽 +Z (둘 다 후진)
    }
    
    /// <summary>
    /// 좌회전 테스트 (5초간) - 수정된 방향
    /// </summary>
    public void TestTurnLeft()
    {
        StopAllCoroutines();
        StartCoroutine(TestMovementCoroutine(1f, -3f, 5f, "좌회전 (수정됨)")); // 오른쪽이 더 빠르게 전진 → 좌회전
    }
    
    /// <summary>
    /// 우회전 테스트 (5초간) - 수정된 방향
    /// </summary>
    public void TestTurnRight()
    {
        StopAllCoroutines();
        StartCoroutine(TestMovementCoroutine(3f, -1f, 5f, "우회전 (수정됨)")); // 왼쪽이 더 빠르게 전진 → 우회전
    }
    
    /// <summary>
    /// 즉시 전진 테스트 (deltaZ 직접 설정)
    /// </summary>
    [ContextMenu("Test Immediate Forward")]
    public void TestImmediateForward()
    {
        leftWheelDeltaZ = 2f;   // 왼쪽 전진 (+Z)
        rightWheelDeltaZ = -2f; // 오른쪽 전진 (-Z)
        Debug.Log($"🧪 즉시 전진 테스트 - 왼쪽: {leftWheelDeltaZ} (+Z=전진), 오른쪽: {rightWheelDeltaZ} (-Z=전진)");
        DebugCurrentDeltaZ();
    }
    
    /// <summary>
    /// 즉시 후진 테스트 (deltaZ 직접 설정)
    /// </summary>
    [ContextMenu("Test Immediate Backward")]
    public void TestImmediateBackward()
    {
        leftWheelDeltaZ = -2f;  // 왼쪽 후진 (-Z)
        rightWheelDeltaZ = 2f;  // 오른쪽 후진 (+Z)
        Debug.Log($"🧪 즉시 후진 테스트 - 왼쪽: {leftWheelDeltaZ} (-Z=후진), 오른쪽: {rightWheelDeltaZ} (+Z=후진)");
        DebugCurrentDeltaZ();
    }
    
    /// <summary>
    /// 강제 물리 이동 테스트 (Rigidbody에 직접 속도 적용)
    /// </summary>
    [ContextMenu("Test Force Move Forward")]
    public void TestForceMoveForward()
    {
        if (chairRigidbody != null)
        {
            Vector3 forwardDir = GetCurrentForwardDirection();
            chairRigidbody.velocity = new Vector3(forwardDir.x * 2f, chairRigidbody.velocity.y, forwardDir.z * 2f);
            Debug.Log($"🚀 강제 전진 - 속도: {chairRigidbody.velocity}");
        }
    }
    
    /// <summary>
    /// 현재 경사로 상태 즉시 확인
    /// </summary>
    [ContextMenu("Debug Slope Status")]
    public void DebugSlopeStatus()
    {
        Debug.Log("═══════════════════════════════════════");
        Debug.Log("🏔️ 경사로 상태 진단");
        Debug.Log("═══════════════════════════════════════");
        Debug.Log($"📐 현재 경사각: {currentSlopeAngle:F1}도");
        Debug.Log($"📊 경사 강도: {slopeIntensity:F2} (0~1)");
        Debug.Log($"🎯 경사 방향: {slopeDirection}");
        Debug.Log($"🧭 휠체어 전진: {GetCurrentForwardDirection()}");
        
        if (slopeIntensity > 0f)
        {
            Vector3 chairForward = GetCurrentForwardDirection();
            float slopeForwardDot = Vector3.Dot(slopeDirection, chairForward);
            float expectedEffect = -slopeIntensity * slopeInfluence * slopeForwardDot * slopeZRotationForce * Time.fixedDeltaTime;
            
            Debug.Log($"🔄 내적값: {slopeForwardDot:F2}");
            Debug.Log($"⚡ 예상 바퀴 효과: {expectedEffect:F3}도/프레임");
            Debug.Log($"📊 효과 설정: 강도 {slopeIntensity:F2} × 영향력 {slopeInfluence} × 내적 {slopeForwardDot:F2} × 힘 {slopeZRotationForce}");
            
            if (expectedEffect > 0)
            {
                Debug.Log("✅ 바퀴가 전진 방향으로 회전 (정상)");
            }
            else if (expectedEffect < 0)
            {
                Debug.Log("⚠️ 바퀴가 후진 방향으로 회전 (비정상)");
            }
            else
            {
                Debug.Log("🔄 바퀴 효과 없음");
            }
            
            Debug.Log($"🎮 현재 바퀴 deltaZ - 왼쪽: {leftWheelDeltaZ:F3}, 오른쪽: {rightWheelDeltaZ:F3}");
        }
        else
        {
            Debug.Log("📏 경사각이 임계값 이하이거나 경사로 시스템 비활성");
        }
        
        // 가상 경사로 정보 추가
        Debug.Log("─────────────────────────────────────");
        Debug.Log("🎮 가상 경사로 시스템");
        Debug.Log("─────────────────────────────────────");
        Debug.Log($"🔌 가상 경사로 활성: {enableVirtualSlopes}");
        Debug.Log($"📊 활성 가상 경사로 수: {activeVirtualSlopes.Count}");
        Debug.Log($"⚡ 현재 가상 경사로 힘: {currentVirtualSlopeForce:F3}");
        Debug.Log($"🎛️ 가상 경사로 배율: {virtualSlopeMultiplier}");
        
        if (activeVirtualSlopes.Count > 0)
        {
            Debug.Log("📋 활성 가상 경사로들:");
            int index = 0;
            foreach (var virtualSlope in activeVirtualSlopes)
            {
                if (virtualSlope != null)
                {
                    var method = virtualSlope.GetType().GetMethod("CalculateSlopeEffect");
                    if (method != null)
                    {
                        float effect = (float)method.Invoke(virtualSlope, new object[] { transform });
                        Debug.Log($"  • 경사로 {index}: 효과 {effect:F3}");
                    }
                    index++;
                }
            }
        }
        Debug.Log("═══════════════════════════════════════");
    }
    
    /// <summary>
    /// 가상 경사로 시스템 상태 확인
    /// </summary>
    [ContextMenu("Debug Virtual Slopes")]
    public void DebugVirtualSlopes()
    {
        Debug.Log("═══════════════════════════════════════");
        Debug.Log("🎮 가상 경사로 시스템 상태");
        Debug.Log("═══════════════════════════════════════");
        Debug.Log($"🔌 시스템 활성화: {enableVirtualSlopes}");
        Debug.Log($"📊 활성 경사로 수: {activeVirtualSlopes.Count}");
        Debug.Log($"⚡ 현재 총 힘: {currentVirtualSlopeForce:F3}");
        Debug.Log($"🎛️ 힘 배율: {virtualSlopeMultiplier}");
        Debug.Log($"🔄 부드러움: {virtualSlopeSmoothing}");
        
        if (activeVirtualSlopes.Count > 0)
        {
            Debug.Log("📋 개별 경사로 분석:");
            int index = 0;
            foreach (var virtualSlope in activeVirtualSlopes)
            {
                if (virtualSlope != null)
                {
                    var method = virtualSlope.GetType().GetMethod("CalculateSlopeEffect");
                    if (method != null)
                    {
                        float effect = (float)method.Invoke(virtualSlope, new object[] { transform });
                        var gameObject = virtualSlope.GetType().GetProperty("gameObject")?.GetValue(virtualSlope);
                        string name = gameObject?.GetType().GetProperty("name")?.GetValue(gameObject)?.ToString() ?? $"경사로{index}";
                        
                        Debug.Log($"  • {name}: 효과 {effect:F3}");
                    }
                    index++;
                }
            }
            
            float totalEffect = 0f;
            foreach (var virtualSlope in activeVirtualSlopes)
            {
                if (virtualSlope != null)
                {
                    var method = virtualSlope.GetType().GetMethod("CalculateSlopeEffect");
                    if (method != null)
                    {
                        totalEffect += (float)method.Invoke(virtualSlope, new object[] { transform });
                    }
                }
            }
            
            Debug.Log($"📊 총 효과: {totalEffect:F3}");
            Debug.Log($"📊 배율 적용 후: {totalEffect * virtualSlopeMultiplier:F3}");
            Debug.Log($"🔄 현재 적용 중인 힘: {currentVirtualSlopeForce:F3}");
        }
        else
        {
            Debug.Log("⚠️ 활성화된 가상 경사로가 없습니다.");
        }
        Debug.Log("═══════════════════════════════════════");
    }
    
    /// <summary>
    /// 가상 경사로 힘 즉시 테스트
    /// </summary>
    [ContextMenu("Test Virtual Slope Force")]
    public void TestVirtualSlopeForce()
    {
        float testForce = 3f;
        ApplyVirtualSlopeForce(testForce);
        Debug.Log($"🧪 가상 경사로 힘 테스트: {testForce} 적용 완료");
        DebugCurrentDeltaZ();
    }
    
    /// <summary>
    /// 전체 이동 시스템 진단
    /// </summary>
    [ContextMenu("Diagnose Movement System")]
    public void DiagnoseMovementSystem()
    {
        Debug.Log("══════════════════════════════════════════════════");
        Debug.Log("🔍 휠체어 이동 시스템 전체 진단");
        Debug.Log("══════════════════════════════════════════════════");
        
        // 1. 기본 시스템 상태
        Debug.Log($"🔋 초전도체 부양: {enableSuperconductorHover}");
        Debug.Log($"🔒 엄격한 이동 제어: {strictMovementControl}");
        Debug.Log($"📏 이동 스케일: {movementScale}");
        Debug.Log($"⚡ 전진 배율: {forwardSpeedMultiplier}, 후진 배율: {backwardSpeedMultiplier}");
        
        // 2. 현재 deltaZ 상태
        Debug.Log($"🔄 왼쪽 deltaZ: {leftWheelDeltaZ:F3}, 오른쪽 deltaZ: {rightWheelDeltaZ:F3}");
        float averageDelta = (leftWheelDeltaZ + rightWheelDeltaZ) * 0.5f;
        Debug.Log($"📊 평균 deltaZ: {averageDelta:F3}");
        
        // 3. 계산된 이동 값들
        float rawSpeed = -averageDelta * movementScale;
        float speedMultiplier = rawSpeed >= 0 ? forwardSpeedMultiplier : backwardSpeedMultiplier;
        float finalSpeed = rawSpeed * speedMultiplier;
        Debug.Log($"🏃 원시 속도: {rawSpeed:F3}m/s → 최종 속도: {finalSpeed:F3}m/s");
        Debug.Log($"🎯 목표 속도 벡터: {targetVelocity}");
        
        // 4. Rigidbody 상태
        if (chairRigidbody != null)
        {
            Debug.Log($"⚖️ 현재 속도: {chairRigidbody.velocity}");
            Debug.Log($"🔄 현재 각속도: {chairRigidbody.angularVelocity}");
            Debug.Log($"🎯 사용 중력: {chairRigidbody.useGravity}");
            Debug.Log($"🏋️ 질량: {chairRigidbody.mass}");
        }
        
        // 5. 지면 감지 상태
        int groundedCount = 0;
        for (int i = 0; i < 4; i++)
        {
            if (groundDetected[i]) groundedCount++;
        }
        Debug.Log($"🌍 지면 감지: {groundedCount}/4 포인트");
        Debug.Log($"📐 경사각: {currentSlopeAngle:F1}도, 안정성: {currentStability:F2}");
        
        // 6. 이동 제한 상태
        Debug.Log($"🚫 충돌 감지됨: {isCollisionDetected}");
        Debug.Log($"📍 마지막 위치: {lastPosition}");
        Debug.Log($"📍 현재 위치: {transform.position}");
        
        Debug.Log("══════════════════════════════════════════════════");
        
        // 7. 즉시 테스트 수행
        if (Mathf.Abs(averageDelta) < 0.01f)
        {
            Debug.Log("⚠️ deltaZ가 너무 작습니다. 테스트 값을 설정합니다.");
            leftWheelDeltaZ = -2f;
            rightWheelDeltaZ = -2f;
            Debug.Log($"🔧 테스트 deltaZ 설정 완료: {leftWheelDeltaZ}, {rightWheelDeltaZ}");
        }
    }
    
    IEnumerator TestMovementCoroutine(float leftDelta, float rightDelta, float duration, string testName)
    {
        Debug.Log($"🧪 테스트 시작: {testName} ({duration}초간) - 왼쪽: {leftDelta}, 오른쪽: {rightDelta}");
        
        leftWheelDeltaZ = leftDelta;
        rightWheelDeltaZ = rightDelta;
        
        yield return new WaitForSeconds(duration);
        
        StopWheels();
        Debug.Log($"🧪 테스트 완료: {testName}");
    }
    
    // ========== 디버그 및 기즈모 ==========
    
    void OnDrawGizmosSelected()
    {
        if (!showDirectionGizmos) return;
        
        // 현재 전진 방향 표시 (파란색)
        Vector3 forwardDir = GetCurrentForwardDirection();
        Gizmos.color = Color.blue;
        Gizmos.DrawRay(transform.position, forwardDir * gizmoLength);
        
        // 바퀴 위치 및 연결 표시
        if (leftWheelTransform != null && rightWheelTransform != null)
        {
        // 바퀴 위치 표시 (노란색)
        Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(leftWheelTransform.position, 0.1f);
            Gizmos.DrawWireSphere(rightWheelTransform.position, 0.1f);
            
            // 휠체어 중심에서 각 바퀴로의 연결선 (회색)
            Gizmos.color = Color.gray;
            Gizmos.DrawLine(transform.position, leftWheelTransform.position);
            Gizmos.DrawLine(transform.position, rightWheelTransform.position);
            
            // 실제 바퀴 중심점 표시 (하늘색)
            Vector3 wheelCenter = GetWheelCenterPoint();
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(wheelCenter, 0.15f);
            
            // 바퀴간 연결선 (실제 거리 표시, 흰색)
            Gizmos.color = Color.white;
            Gizmos.DrawLine(leftWheelTransform.position, rightWheelTransform.position);
            
            #if UNITY_EDITOR
            // 실제 거리 정보 표시
            float actualDistance = GetActualWheelbaseWidth();
            Vector3 midPoint = (leftWheelTransform.position + rightWheelTransform.position) * 0.5f;
            UnityEditor.Handles.Label(midPoint + Vector3.up * 0.3f, 
                $"실제 거리: {actualDistance:F2}m\n설정값: {wheelbaseWidth:F2}m");
            #endif
        }
        
        // ICC 기반 회전 시각화
        if (enableICCRotation && showICCGizmos && isRotating && Mathf.Abs(currentTurningRadius) < maxTurningRadius)
        {
            Vector3 wheelCenter = GetWheelCenterPoint();
            
            // 회전축(ICC) 위치 강조 표시 (빨간색 구와 축)
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(currentICC, 0.2f);
            
            // 회전축 Y축 표시 (빨간색 세로선)
            Gizmos.color = Color.red;
            Gizmos.DrawLine(currentICC - Vector3.up * 0.5f, currentICC + Vector3.up * 1.5f);
            
            // 회전축 위치 표식 (X자 형태)
            Vector3 xMarkSize = Vector3.one * 0.1f;
            Gizmos.DrawLine(currentICC - xMarkSize, currentICC + xMarkSize);
            Gizmos.DrawLine(currentICC - new Vector3(xMarkSize.x, 0, -xMarkSize.z), currentICC + new Vector3(xMarkSize.x, 0, -xMarkSize.z));
            
            // 바퀴 중심에서 ICC로의 연결선 (주황색 - 바퀴 회전 반지름)
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(wheelCenter, currentICC);
            
            // 휠체어 중심에서 ICC로의 연결선 (하늘색 - 휠체어 회전 반지름)
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, currentICC);
            
            // 휠체어 중심의 원형 궤도 표시 (녹색 원)
            Vector3 chairToICC = currentICC - transform.position;
            float chairRotationRadius = new Vector3(chairToICC.x, 0, chairToICC.z).magnitude;
            
            Gizmos.color = Color.green;
            int circleSegments = 36;
            Vector3 lastCirclePoint = Vector3.zero;
            
            for (int i = 0; i <= circleSegments; i++)
            {
                float angle = (360f / circleSegments) * i * Mathf.Deg2Rad;
                Vector3 circlePoint = currentICC + new Vector3(
                    Mathf.Cos(angle) * chairRotationRadius, 
                    0, 
                    Mathf.Sin(angle) * chairRotationRadius
                );
                
                if (i > 0)
                {
                    Gizmos.DrawLine(lastCirclePoint, circlePoint);
                }
                lastCirclePoint = circlePoint;
            }
            
            // 바퀴 중심의 회전 경로 표시 (자홍색 호) - 기존 유지
            Gizmos.color = Color.magenta;
            Vector3 fromICC = wheelCenter - currentICC;
            float startAngle = Mathf.Atan2(fromICC.z, fromICC.x) * Mathf.Rad2Deg;
            
            // 호를 그리기 위한 점들 (XZ 평면에서)
            int arcSegments = 30;
            float arcRange = 90f;
            Vector3 lastPoint = Vector3.zero;
            
            for (int i = 0; i <= arcSegments; i++)
            {
                float angle = (startAngle - arcRange * 0.5f + (arcRange / arcSegments) * i) * Mathf.Deg2Rad;
                Vector3 point = currentICC + new Vector3(Mathf.Cos(angle) * Mathf.Abs(currentTurningRadius), 0, Mathf.Sin(angle) * Mathf.Abs(currentTurningRadius));
                
                if (i > 0)
                {
                    Gizmos.DrawLine(lastPoint, point);
                }
                lastPoint = point;
            }
            
            // 휠체어의 접선 속도 방향 표시 (밝은 녹색 화살표)
            Vector3 radiusVector = transform.position - currentICC;
            radiusVector.y = 0;
            
            if (radiusVector.magnitude > 0.001f)
            {
                Vector3 tangentDirection = Vector3.Cross(Vector3.up, radiusVector).normalized;
                if (calculatedAngularVelocity < 0) tangentDirection = -tangentDirection;
                
                Gizmos.color = Color.green;
                Vector3 tangentStart = transform.position;
                Vector3 tangentEnd = tangentStart + tangentDirection * gizmoLength;
                Gizmos.DrawRay(tangentStart, tangentDirection * gizmoLength);
                
                // 접선 화살표 끝부분
                Vector3 arrowSide1 = tangentEnd - tangentDirection * 0.3f + Vector3.Cross(tangentDirection, Vector3.up) * 0.15f;
                Vector3 arrowSide2 = tangentEnd - tangentDirection * 0.3f - Vector3.Cross(tangentDirection, Vector3.up) * 0.15f;
                Gizmos.DrawLine(tangentEnd, arrowSide1);
                Gizmos.DrawLine(tangentEnd, arrowSide2);
            }
            
            // 바퀴 중심의 회전 방향 화살표 (하늘색) - 기존 유지
            Vector3 wheelTangentDir = Vector3.Cross(Vector3.up, fromICC.normalized);
            if (calculatedAngularVelocity < 0) wheelTangentDir = -wheelTangentDir;
            
            Gizmos.color = Color.cyan;
            Vector3 wheelArrowStart = wheelCenter;
            Vector3 wheelArrowEnd = wheelArrowStart + wheelTangentDir * gizmoLength * 0.7f;
            Gizmos.DrawRay(wheelArrowStart, wheelTangentDir * gizmoLength * 0.7f);
            
            // 바퀴 화살표 끝부분
            Vector3 wheelArrowSide1 = wheelArrowEnd - wheelTangentDir * 0.3f + Vector3.Cross(wheelTangentDir, Vector3.up) * 0.15f;
            Vector3 wheelArrowSide2 = wheelArrowEnd - wheelTangentDir * 0.3f - Vector3.Cross(wheelTangentDir, Vector3.up) * 0.15f;
            Gizmos.DrawLine(wheelArrowEnd, wheelArrowSide1);
            Gizmos.DrawLine(wheelArrowEnd, wheelArrowSide2);
            
            // 회전 중심축 라벨과 정보 표시
            #if UNITY_EDITOR
            string rotationDirection = calculatedAngularVelocity > 0 ? "좌회전 (반시계)" : "우회전 (시계)";
            string fasterWheel = leftWheelLinearVelocity > rightWheelLinearVelocity ? "왼쪽" : "오른쪽";
            float actualDistance = GetActualWheelbaseWidth();
            
            // ICC 축 정보
            UnityEditor.Handles.Label(currentICC + Vector3.up * 0.8f, 
                $"🎯 회전축 (ICC)\n{rotationDirection}\n{fasterWheel} 바퀴가 더 빠름\n바퀴 R: {currentTurningRadius:F1}m\n휠체어 R: {chairRotationRadius:F1}m\nω: {calculatedAngularVelocity * Mathf.Rad2Deg:F1}°/s\nL: {actualDistance:F2}m");
            
            // 회전 반지름 표시들
            Vector3 wheelRadiusLabelPos = wheelCenter + (currentICC - wheelCenter) * 0.5f + Vector3.up * 0.2f;
            UnityEditor.Handles.Label(wheelRadiusLabelPos, $"바퀴 R: {Mathf.Abs(currentTurningRadius):F2}m");
            
            Vector3 chairRadiusLabelPos = transform.position + (currentICC - transform.position) * 0.5f + Vector3.up * 0.4f;
            UnityEditor.Handles.Label(chairRadiusLabelPos, $"휠체어 R: {chairRotationRadius:F2}m");
            
            // 접선 속도 표시
            if (targetVelocity.magnitude > 0.01f)
            {
                UnityEditor.Handles.Label(transform.position + Vector3.up * 0.6f, 
                    $"접선속도: {targetVelocity.magnitude:F2}m/s\n방향: {targetVelocity.normalized}");
            }
            #endif
        }
        
        // 바퀴 선속도 시각화 (개선된 표시)
        if (enableICCRotation && (Mathf.Abs(leftWheelLinearVelocity) > 0.01f || Mathf.Abs(rightWheelLinearVelocity) > 0.01f))
        {
            // 왼쪽 바퀴 속도 (초록색)
            if (leftWheelTransform != null)
            {
                Gizmos.color = Color.green;
                Vector3 leftVelDir = transform.forward * Mathf.Sign(leftWheelLinearVelocity);
                float arrowLength = Mathf.Abs(leftWheelLinearVelocity) * 0.5f;
                Gizmos.DrawRay(leftWheelTransform.position, leftVelDir * arrowLength);
                
                // 속도 값 표시
                #if UNITY_EDITOR
                UnityEditor.Handles.Label(leftWheelTransform.position + Vector3.up * 0.25f, 
                    $"L: {leftWheelLinearVelocity:F2}m/s");
                #endif
            }
            
            // 오른쪽 바퀴 속도 (파란색)
        if (rightWheelTransform != null)
        {
                Gizmos.color = Color.blue;
                Vector3 rightVelDir = transform.forward * Mathf.Sign(rightWheelLinearVelocity);
                float arrowLength = Mathf.Abs(rightWheelLinearVelocity) * 0.5f;
                Gizmos.DrawRay(rightWheelTransform.position, rightVelDir * arrowLength);
                
                // 속도 값 표시
                #if UNITY_EDITOR
                UnityEditor.Handles.Label(rightWheelTransform.position + Vector3.up * 0.25f, 
                    $"R: {rightWheelLinearVelocity:F2}m/s");
                #endif
            }
        }
        
        // 지면 감지 포인트 표시 (초록색/빨간색)
        for (int i = 0; i < 4; i++)
        {
            if (groundDetectionPoints[i] == null) continue;
            
            Gizmos.color = groundDetected[i] ? Color.green : Color.red;
            Gizmos.DrawWireSphere(groundDetectionPoints[i].position, 0.05f);
            
            if (groundDetected[i])
            {
                Gizmos.DrawLine(groundDetectionPoints[i].position, groundPoints[i]);
            }
        }
        
        // 경사로 방향 표시 (주황색)
        if (slopeIntensity > 0f)
        {
            Gizmos.color = Color.Lerp(Color.white, Color.red, slopeIntensity);
            Gizmos.DrawRay(transform.position, slopeDirection * gizmoLength * slopeIntensity);
        }
        
        // 목표 속도 방향 표시 (자홍색)
        if (targetVelocity.magnitude > 0.1f)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawRay(transform.position, targetVelocity.normalized * gizmoLength * 0.5f);
        }
        
        
    }
    
    void OnDrawGizmos()
    {
        if (!showDirectionGizmos || !enableSuperconductorHover) return;
        
        // 부양 높이 표시 (하늘색)
        Gizmos.color = new Color(0.5f, 1f, 1f, 0.3f);
        Vector3 hoverPosition = transform.position - Vector3.up * hoverHeight;
        Gizmos.DrawWireCube(hoverPosition, new Vector3(1.2f, 0.02f, 1.2f));
    }
    
    /// <summary>
    /// 현재 바퀴 시스템 상태를 콘솔에 출력
    /// </summary>
    [ContextMenu("Print Wheel Status")]
    public void PrintWheelStatus()
    {
        var wheelStatus = GetWheelStatus();
        var grabStatus = GetGrabStatus();
        var moveStatus = GetMovementStatus();
        
        Debug.Log("═══════════════════════════════════════");
        Debug.Log("🚗 바퀴 시스템 상태 보고서");
        Debug.Log("═══════════════════════════════════════");
        Debug.Log($"🔄 변화량 - 왼쪽: {wheelStatus.leftDeltaZ:F2}도/프레임, 오른쪽: {wheelStatus.rightDeltaZ:F2}도/프레임");
        Debug.Log($"📐 현재 Z - 왼쪽: {wheelStatus.leftCurrentZ:F1}도, 오른쪽: {wheelStatus.rightCurrentZ:F1}도");
        Debug.Log($"🤏 잡힘 상태 - 왼쪽: {(grabStatus.leftGrabbed ? "잡힘" : "놓임")}, 오른쪽: {(grabStatus.rightGrabbed ? "잡힘" : "놓임")}");
        Debug.Log($"🏃 속도 - 수평: {new Vector3(moveStatus.velocity.x, 0, moveStatus.velocity.z).magnitude:F2}m/s, 수직: {moveStatus.velocity.y:F2}m/s");
        Debug.Log($"🌀 각속도: {moveStatus.angularVelocity * Mathf.Rad2Deg:F1}도/초");
        Debug.Log($"🏔️ 경사각: {moveStatus.slopeAngle:F1}도, 안정성: {moveStatus.stability:F2}");
        Debug.Log($"⚙️ 기본 설정 - 입력감도: {inputSensitivity}, 마찰력: {rotationFriction}, 이동배율: {movementScale}");
        Debug.Log($"🎮 속도 설정 - 전진배율: {forwardSpeedMultiplier}, 후진배율: {backwardSpeedMultiplier}, 회전배율: {rotationScale}");
        Debug.Log($"🏔️ 경사 설정 - Z회전힘: {slopeZRotationForce}도/초, 영향력: {slopeInfluence}");
        Debug.Log("═══════════════════════════════════════");
    }
    
    /// <summary>
    /// 바퀴 시스템 초기화
    /// </summary>
    [ContextMenu("Reset Wheel System")]
    public void ResetWheelSystem()
    {
        StopWheels();
        
        // 바퀴 회전값 초기화
        currentLeftWheelZ = 0f;
        currentRightWheelZ = 0f;
        
        if (leftWheelTransform != null)
        {
            leftWheelTransform.localEulerAngles = new Vector3(
                leftWheelTransform.localEulerAngles.x,
                leftWheelTransform.localEulerAngles.y,
                0f
            );
        }
        
        if (rightWheelTransform != null)
        {
            rightWheelTransform.localEulerAngles = new Vector3(
                rightWheelTransform.localEulerAngles.x,
                rightWheelTransform.localEulerAngles.y,
                0f
            );
        }
        
        // 물리 상태 초기화
        if (chairRigidbody != null)
        {
            chairRigidbody.velocity = Vector3.zero;
            chairRigidbody.angularVelocity = Vector3.zero;
        }
        
        Debug.Log("🔄 바퀴 시스템 초기화 완료");
    }
    
    // ========== 이벤트 및 콜백 ==========
    
    void OnCollisionEnter(Collision collision)
    {
        if (enableDebugLog)
        {
            Debug.Log($"💥 충돌 감지: {collision.gameObject.name}");
        }
    }
    
    void OnTriggerEnter(Collider other)
    {
        if (enableDebugLog)
        {
            Debug.Log($"🚪 트리거 진입: {other.gameObject.name}");
        }
    }
    
    // ========== 설정 검증 ==========
    
    void OnValidate()
    {
        // 설정값 범위 검증
        hoverHeight = Mathf.Max(0.1f, hoverHeight);
        minHoverHeight = Mathf.Max(0.05f, minHoverHeight);
        rotationFriction = Mathf.Clamp01(rotationFriction);
        inputSensitivity = Mathf.Max(0.1f, inputSensitivity);
        slopeInfluence = Mathf.Max(0f, slopeInfluence);
        movementScale = Mathf.Max(0.001f, movementScale);
        forwardSpeedMultiplier = Mathf.Max(0.1f, forwardSpeedMultiplier);
        backwardSpeedMultiplier = Mathf.Max(0.1f, backwardSpeedMultiplier);
        maxSpeed = Mathf.Max(0.1f, maxSpeed);
        rotationScale = Mathf.Max(0.01f, rotationScale);
        wheelRadius = Mathf.Max(0.1f, wheelRadius);
        slopeZRotationForce = Mathf.Max(0f, slopeZRotationForce);
        
        // ICC 시스템 설정값 검증
        wheelbaseWidth = Mathf.Max(0.1f, wheelbaseWidth);
        rotationTorqueMultiplier = Mathf.Max(1f, rotationTorqueMultiplier);
        minTurningRadius = Mathf.Max(0.01f, minTurningRadius);
        maxTurningRadius = Mathf.Max(minTurningRadius + 0.1f, maxTurningRadius);
        
        // 회전 안정성 설정값 검증
        iccRotationSmoothing = Mathf.Clamp(iccRotationSmoothing, 1f, 20f);
        maxRotationDeltaPerFrame = Mathf.Clamp(maxRotationDeltaPerFrame, 0.1f, 5f);
        minRotationThreshold = Mathf.Clamp(minRotationThreshold, 0.001f, 0.1f);
        
        // 회전 안정성 설정 경고
        if (maxRotationDeltaPerFrame < 0.5f)
        {
            Debug.LogWarning("⚠️ 최대 회전 속도가 너무 낮습니다. 응답성이 떨어질 수 있습니다.");
        }
        
        if (iccRotationSmoothing < 5f)
        {
            Debug.LogWarning("⚠️ ICC 회전 스무딩이 너무 낮습니다. 떨림이 발생할 수 있습니다.");
        }
        
        // 경사로 설정 검증
        slopeThreshold = Mathf.Clamp(slopeThreshold, 0f, 30f);
        maxSlideAngle = Mathf.Clamp(maxSlideAngle, slopeThreshold + 1f, 90f);
        
        // 디버그 경고
        if (leftWheelTransform == null || rightWheelTransform == null)
        {
            Debug.LogWarning("⚠️ 바퀴 Transform이 설정되지 않았습니다!");
        }
        
        if (leftWheelGrabbable == null || rightWheelGrabbable == null)
        {
            Debug.LogWarning("⚠️ 바퀴 Grabbable이 설정되지 않았습니다!");
        }
        
        // 속도 배율 권장값 경고
        if (forwardSpeedMultiplier > 2f || backwardSpeedMultiplier > 2f)
        {
            Debug.LogWarning("⚠️ 속도 배율이 너무 높습니다. 권장값: 0.5~2.0");
        }
        
        // 경사로 힘 권장값 경고
        if (slopeZRotationForce > 10f)
        {
            Debug.LogWarning("⚠️ 경사로 Z회전 힘이 너무 높습니다. 권장값: 0~5");
        }
        
        // ICC 시스템 설정값 경고
        if (enableICCRotation)
        {
            if (wheelbaseWidth < 0.2f || wheelbaseWidth > 2f)
            {
                Debug.LogWarning("⚠️ 바퀴간 거리가 비정상적입니다. 권장값: 0.2~2.0m");
            }
            
            if (rotationTorqueMultiplier > 2000f)
            {
                Debug.LogWarning("⚠️ 토크 배율이 너무 높습니다. 권장값: 100~1000");
            }
            
            if (minTurningRadius < 0.05f)
            {
                Debug.LogWarning("⚠️ 최소 회전반지름이 너무 작습니다. 권장값: 0.1m 이상");
            }
        }
        
       
    }
    
    void ProcessVirtualSlopeForces()
    {
        if (activeVirtualSlopes.Count == 0)
        {
            currentVirtualSlopeForce = 0f;
            return;
        }
        
        float totalVirtualForce = 0f;
        
        // 모든 활성 가상 경사로의 효과를 합산
        foreach (var virtualSlope in activeVirtualSlopes)
        {
            if (virtualSlope != null)
            {
                // 리플렉션을 사용하여 CalculateSlopeEffect 메서드 호출
                var method = virtualSlope.GetType().GetMethod("CalculateSlopeEffect");
                if (method != null)
                {
                    float slopeEffect = (float)method.Invoke(virtualSlope, new object[] { transform });
                    totalVirtualForce += slopeEffect;
                }
            }
        }
        
        // 가상 경사로 힘을 부드럽게 적용
        float targetForce = totalVirtualForce * virtualSlopeMultiplier;
        currentVirtualSlopeForce = Mathf.Lerp(currentVirtualSlopeForce, targetForce, virtualSlopeSmoothing * Time.fixedDeltaTime);
        
        // Z 변화량에 적용 (각 바퀴의 방향성에 맞게)
        if (Mathf.Abs(currentVirtualSlopeForce) > 0.01f)
        {
            float timeScaledForce = currentVirtualSlopeForce * Time.fixedDeltaTime;
            
            // 왼쪽 바퀴: +Z = 전진, 오른쪽 바퀴: -Z = 전진
            leftWheelDeltaZ += timeScaledForce;   // 왼쪽: 양수가 전진
            rightWheelDeltaZ += -timeScaledForce; // 오른쪽: 음수가 전진
            
            if (enableDebugLog && Time.fixedTime % 1f < Time.fixedDeltaTime)
            {
                Debug.Log($"🎮 가상 경사로 효과 적용 - 총 힘: {totalVirtualForce:F2}, 현재 힘: {currentVirtualSlopeForce:F2}");
                Debug.Log($"    활성 가상 경사로 수: {activeVirtualSlopes.Count}, 시간 배율 힘: {timeScaledForce:F3}");
                Debug.Log($"    바퀴 deltaZ 추가 - 왼쪽: +{timeScaledForce:F3}, 오른쪽: {-timeScaledForce:F3}");
            }
        }
    }
    
    /// <summary>
    /// 가상 경사로 추가 (VirtualSlope 스크립트에서 호출)
    /// </summary>
    public void AddVirtualSlope(object virtualSlope)
    {
        // VirtualSlope 타입 체크를 문자열로 우회
        if (virtualSlope != null && virtualSlope.GetType().Name == "VirtualSlope")
        {
            activeVirtualSlopes.Add(virtualSlope);
            
            if (enableDebugLog)
            {
                Debug.Log($"🎮 가상 경사로 추가: {virtualSlope.GetType().Name} (총 {activeVirtualSlopes.Count}개)");
            }
        }
    }
    
    /// <summary>
    /// 가상 경사로 제거 (VirtualSlope 스크립트에서 호출)
    /// </summary>
    public void RemoveVirtualSlope(object virtualSlope)
    {
        if (virtualSlope != null && virtualSlope.GetType().Name == "VirtualSlope")
        {
            activeVirtualSlopes.Remove(virtualSlope);
            
            if (enableDebugLog)
            {
                Debug.Log($"🎮 가상 경사로 제거: {virtualSlope.GetType().Name} (총 {activeVirtualSlopes.Count}개)");
            }
        }
    }
    
    /// <summary>
    /// 외부에서 직접 가상 경사로 힘 적용 (테스트용)
    /// </summary>
    public void ApplyVirtualSlopeForce(float force)
    {
        float timeScaledForce = force * Time.fixedDeltaTime;
        
        leftWheelDeltaZ += timeScaledForce;
        rightWheelDeltaZ += -timeScaledForce;
        
        if (enableDebugLog)
        {
            Debug.Log($"🧪 외부 가상 경사로 힘 적용: {force:F2} → 시간배율: {timeScaledForce:F3}");
        }
    }
    
    // 바퀴간 실제 거리 계산 함수
    float GetActualWheelbaseWidth()
    {
        return wheelbaseWidth; // 이제 항상 실제 거리가 저장됨
    }
    
    // 두 바퀴의 중심점 계산 (월드 좌표)
    Vector3 GetWheelCenterPoint()
    {
        if (leftWheelTransform == null || rightWheelTransform == null)
        {
            return transform.position; // 기본값으로 휠체어 중심 사용
        }
        
        return (leftWheelTransform.position + rightWheelTransform.position) * 0.5f;
    }
    
    
    
    /// <summary>
    /// ICC 원형 경로 테스트 (한쪽 바퀴만 회전)
    /// </summary>
    [ContextMenu("Test ICC Circular Path - Right Only")]
    public void TestICCCircularPathRightOnly()
    {
        if (!enableICCRotation)
        {
            Debug.LogWarning("⚠️ ICC 시스템이 비활성화되어 있습니다!");
            return;
        }
        
        StopAllCoroutines();
        // 오른쪽 바퀴만 회전 (왼쪽 정지) → 왼쪽 바퀴를 중심으로 한 원형 경로
        StartCoroutine(TestMovementCoroutine(0f, -3f, 8f, "ICC 원형 경로 (오른쪽만)"));
        Debug.Log("🎯 ICC 원형 경로 테스트 시작 - 오른쪽 바퀴만 회전 → 왼쪽 바퀴 중심 원형 이동");
    }
    
    /// <summary>
    /// ICC 원형 경로 테스트 (왼쪽 바퀴만 회전)
    /// </summary>
    [ContextMenu("Test ICC Circular Path - Left Only")]
    public void TestICCCircularPathLeftOnly()
    {
        if (!enableICCRotation)
        {
            Debug.LogWarning("⚠️ ICC 시스템이 비활성화되어 있습니다!");
            return;
        }
        
        StopAllCoroutines();
        // 왼쪽 바퀴만 회전 (오른쪽 정지) → 오른쪽 바퀴를 중심으로 한 원형 경로
        StartCoroutine(TestMovementCoroutine(3f, 0f, 8f, "ICC 원형 경로 (왼쪽만)"));
        Debug.Log("🎯 ICC 원형 경로 테스트 시작 - 왼쪽 바퀴만 회전 → 오른쪽 바퀴 중심 원형 이동");
    }
    
    /// <summary>
    /// ICC vs 레거시 원형 경로 비교 테스트
    /// </summary>
    [ContextMenu("Compare Circular Path ICC vs Legacy")]
    public void CompareCircularPathICCvsLegacy()
    {
        Debug.Log("═══════════════════════════════════════");
        Debug.Log("🔄 원형 경로 ICC vs 레거시 비교");
        Debug.Log("═══════════════════════════════════════");
        
        // 현재 상태 저장
        bool originalICCState = enableICCRotation;
        
        // 테스트 입력값 (한쪽만 회전)
        float testLeftDelta = 3f;
        float testRightDelta = 0f;
        
        // ICC 모드 테스트
        enableICCRotation = true;
        leftWheelDeltaZ = testLeftDelta;
        rightWheelDeltaZ = testRightDelta;
        CalculateMovementFromZRotation();
        
        Vector3 iccTargetVel = targetVelocity;
        float iccAngularVel = targetAngularVelocity;
        
        Debug.Log($"🎯 ICC 원형 경로 모드 결과:");
        Debug.Log($"  targetVelocity: {iccTargetVel} (크기: {iccTargetVel.magnitude:F3}m/s)");
        Debug.Log($"  각속도: {iccAngularVel * Mathf.Rad2Deg:F2}도/초");
        
        if (isRotating)
        {
            Vector3 chairToICC = currentICC - transform.position;
            float chairRotationRadius = new Vector3(chairToICC.x, 0, chairToICC.z).magnitude;
            Debug.Log($"  휠체어 회전반지름: {chairRotationRadius:F2}m");
            Debug.Log($"  ICC 위치: {currentICC}");
            Debug.Log($"  접선속도 방향: {iccTargetVel.normalized}");
        }
        
        // 레거시 모드 테스트
        enableICCRotation = false;
        leftWheelDeltaZ = testLeftDelta;
        rightWheelDeltaZ = testRightDelta;
        CalculateMovementFromZRotation();
        
        Vector3 legacyTargetVel = targetVelocity;
        float legacyAngularVel = targetAngularVelocity;
        
        Debug.Log($"🚗 레거시 모드 결과:");
        Debug.Log($"  targetVelocity: {legacyTargetVel} (크기: {legacyTargetVel.magnitude:F3}m/s)");
        Debug.Log($"  각속도: {legacyAngularVel * Mathf.Rad2Deg:F2}도/초");
        
        // 차이점 분석
        float speedDiff = iccTargetVel.magnitude - legacyTargetVel.magnitude;
        float angularDiff = (iccAngularVel - legacyAngularVel) * Mathf.Rad2Deg;
        Vector3 directionDiff = iccTargetVel.normalized - legacyTargetVel.normalized;
        
        Debug.Log($"📊 차이점 분석:");
        Debug.Log($"  속도 크기 차이: {speedDiff:F3}m/s");
        Debug.Log($"  각속도 차이: {angularDiff:F2}도/초");
        Debug.Log($"  방향 차이: {directionDiff} (크기: {directionDiff.magnitude:F3})");
        
        Debug.Log($"📝 분석 결과:");
        Debug.Log($"  ICC 모드: 원형 경로 접선 이동 (정확한 물리 기반)");
        Debug.Log($"  레거시 모드: 직진 이동 + 회전 (근사치 기반)");
        
        // 원래 상태 복원
        enableICCRotation = originalICCState;
        StopWheels();
        
        Debug.Log("═══════════════════════════════════════");
    }
    
    /// <summary>
    /// ICC 원운동 시스템 상세 디버그
    /// </summary>
    [ContextMenu("Debug ICC Circular Motion")]
    public void DebugICCCircularMotion()
    {
        Debug.Log("══════════════════════════════════════════════════");
        Debug.Log("🌀 ICC 원운동 시스템 상세 분석");
        Debug.Log("══════════════════════════════════════════════════");
        
        if (!enableICCRotation)
        {
            Debug.LogWarning("⚠️ ICC 시스템이 비활성화되어 있습니다!");
            return;
        }
        
        if (!isRotating)
        {
            Debug.Log("📐 현재 직진 이동 중 - 원운동 없음");
            return;
        }
        
        Vector3 currentVelocity = chairRigidbody.velocity;
        Vector3 radiusVector = transform.position - currentICC;
        radiusVector.y = 0;
        
        Debug.Log("─────────────────────────────────────");
        Debug.Log("🎯 ICC 기본 정보");
        Debug.Log("─────────────────────────────────────");
        Debug.Log($"ICC 위치: {currentICC}");
        Debug.Log($"휠체어 위치: {transform.position}");
        Debug.Log($"반지름 벡터: {radiusVector} (크기: {radiusVector.magnitude:F3}m)");
        Debug.Log($"계산된 각속도: {calculatedAngularVelocity * Mathf.Rad2Deg:F1}도/초");
        Debug.Log($"바퀴 회전반지름: {currentTurningRadius:F2}m");
        
        if (radiusVector.magnitude > 0.001f)
        {
            // 접선 방향 계산
            Vector3 tangentDirection = Vector3.Cross(Vector3.up, radiusVector.normalized);
            if (calculatedAngularVelocity < 0) tangentDirection = -tangentDirection;
            
            // 원운동 속도 계산
            Vector3 circularVelocity = tangentDirection * (Mathf.Abs(calculatedAngularVelocity) * radiusVector.magnitude);
            
            // 속도 배율 적용
            float speedSum = leftWheelLinearVelocity + rightWheelLinearVelocity;
            float speedMultiplier = speedSum >= 0 ? forwardSpeedMultiplier : backwardSpeedMultiplier;
            Vector3 scaledCircularVelocity = circularVelocity * speedMultiplier;
            
            Debug.Log("─────────────────────────────────────");
            Debug.Log("🌀 원운동 계산 결과");
            Debug.Log("─────────────────────────────────────");
            Debug.Log($"접선 방향: {tangentDirection}");
            Debug.Log($"기본 원운동 속도: {circularVelocity} (크기: {circularVelocity.magnitude:F2}m/s)");
            Debug.Log($"배율 적용 후: {scaledCircularVelocity} (크기: {scaledCircularVelocity.magnitude:F2}m/s)");
            Debug.Log($"속도 배율: {speedMultiplier:F2}");
            
            // 공식 검증
            float theoreticalSpeed = Mathf.Abs(calculatedAngularVelocity) * radiusVector.magnitude;
            Debug.Log($"공식 검증: v = ω × r = {calculatedAngularVelocity * Mathf.Rad2Deg:F1}° × {radiusVector.magnitude:F3}m = {theoreticalSpeed:F3}m/s");
            
            Debug.Log("─────────────────────────────────────");
            Debug.Log("⚡ 실제 적용된 속도");
            Debug.Log("─────────────────────────────────────");
            Debug.Log($"현재 Rigidbody 속도: {currentVelocity}");
            Debug.Log($"수평 속도 크기: {new Vector3(currentVelocity.x, 0, currentVelocity.z).magnitude:F2}m/s");
            Debug.Log($"실제 각속도: {chairRigidbody.angularVelocity.y * Mathf.Rad2Deg:F1}도/초");
            
            // 회전 방향 분석
            string rotationDir = calculatedAngularVelocity > 0 ? "좌회전 (반시계)" : "우회전 (시계)";
            string fasterWheel = rightWheelLinearVelocity > leftWheelLinearVelocity ? "오른쪽" : "왼쪽";
            Debug.Log($"회전 방향: {rotationDir} ({fasterWheel} 바퀴가 더 빠름)");
            
            // ICC와 바퀴 중심점 관계
            Vector3 wheelCenter = GetWheelCenterPoint();
            Vector3 wheelToICC = currentICC - wheelCenter;
            Debug.Log($"바퀴 중심점: {wheelCenter}");
            Debug.Log($"바퀴중심 → ICC: {wheelToICC} (거리: {wheelToICC.magnitude:F2}m)");
            
            // 속도 일치성 검증
            Vector3 expectedVelocity = tangentDirection * theoreticalSpeed * speedMultiplier;
            Vector3 actualHorizontalVelocity = new Vector3(currentVelocity.x, 0, currentVelocity.z);
            Vector3 velocityDiff = expectedVelocity - actualHorizontalVelocity;
            
            Debug.Log("─────────────────────────────────────");
            Debug.Log("🔍 속도 일치성 검증");
            Debug.Log("─────────────────────────────────────");
            Debug.Log($"예상 속도: {expectedVelocity}");
            Debug.Log($"실제 수평 속도: {actualHorizontalVelocity}");
            Debug.Log($"속도 차이: {velocityDiff} (크기: {velocityDiff.magnitude:F3}m/s)");
            
            if (velocityDiff.magnitude < 0.1f)
            {
                Debug.Log("✅ 속도 일치성 양호 (차이 < 0.1m/s)");
            }
            else if (velocityDiff.magnitude < 0.5f)
            {
                Debug.Log("⚠️ 속도 차이 보통 (0.1~0.5m/s)");
            }
            else
            {
                Debug.Log("❌ 속도 차이 큼 (> 0.5m/s) - 시스템 확인 필요");
            }
        }
        else
        {
            Debug.Log("⚠️ ICC와 휠체어가 너무 가까움 - 제자리 회전");
        }
        
        Debug.Log("══════════════════════════════════════════════════");
    }
    
    /// <summary>
    /// 실시간 ICC 원운동 모니터링 (한 번만 실행)
    /// </summary>
    [ContextMenu("Monitor ICC Motion Once")]
    public void MonitorICCMotionOnce()
    {
        if (!enableICCRotation || !isRotating)
        {
            Debug.Log("📐 ICC 원운동 모니터링: 회전 없음");
            return;
        }
        
        Vector3 currentVelocity = chairRigidbody.velocity;
        Vector3 radiusVector = transform.position - currentICC;
        radiusVector.y = 0;
        
        if (radiusVector.magnitude > 0.001f)
        {
            Vector3 tangentDirection = Vector3.Cross(Vector3.up, radiusVector.normalized);
            if (calculatedAngularVelocity < 0) tangentDirection = -tangentDirection;
            
            float theoreticalSpeed = Mathf.Abs(calculatedAngularVelocity) * radiusVector.magnitude;
            float speedSum = leftWheelLinearVelocity + rightWheelLinearVelocity;
            float speedMultiplier = speedSum >= 0 ? forwardSpeedMultiplier : backwardSpeedMultiplier;
            
            Debug.Log($"🌀 ICC 원운동 현재 상태:");
            Debug.Log($"   반지름: {radiusVector.magnitude:F2}m, 각속도: {calculatedAngularVelocity * Mathf.Rad2Deg:F1}°/s");
            Debug.Log($"   이론속도: {theoreticalSpeed:F2}m/s, 배율: {speedMultiplier:F2}");
            Debug.Log($"   실제속도: {new Vector3(currentVelocity.x, 0, currentVelocity.z).magnitude:F2}m/s");
            Debug.Log($"   접선방향: {tangentDirection}");
        }
    }
    
   
    
    
    /// <summary>
    /// 바퀴간 실제 거리를 Transform 위치에서 계산하여 업데이트
    /// </summary>
    void UpdateWheelbaseWidth()
    {
        if (leftWheelTransform != null && rightWheelTransform != null)
        {
            // 두 바퀴 위치의 실제 거리 계산 (XZ 평면에서)
            Vector3 leftPos = leftWheelTransform.position;
            Vector3 rightPos = rightWheelTransform.position;
            
            // Y축 차이는 무시하고 수평 거리만 계산
            float calculatedDistance = Vector3.Distance(
                new Vector3(leftPos.x, 0, leftPos.z), 
                new Vector3(rightPos.x, 0, rightPos.z)
            );
            
            // 최소 거리 보장 (너무 작으면 계산 오류 발생)
            wheelbaseWidth = Mathf.Max(calculatedDistance, 0.1f);
        }
        else
        {
            // Transform이 없으면 기본값 유지
            if (wheelbaseWidth < 0.1f)
            {
                wheelbaseWidth = 0.6f; // 기본값
            }
        }
    }
    
    /// <summary>
    /// ICC 축 기반 회전 테스트 함수들
    /// </summary>
    [ContextMenu("Test ICC Axis Rotation Left")]
    public void TestICCAxisRotationLeft()
    {
        if (!enableICCRotation)
        {
            Debug.LogWarning("⚠️ ICC 시스템이 비활성화되어 있습니다!");
            return;
        }
        
        StopAllCoroutines();
        StartCoroutine(TestMovementCoroutine(1f, -3f, 8f, "ICC 축 기반 좌회전"));
        Debug.Log("🌀 ICC 축 기반 좌회전 테스트 시작 - Transform.RotateAround 사용");
    }
    
    [ContextMenu("Test ICC Axis Rotation Right")]
    public void TestICCAxisRotationRight()
    {
        if (!enableICCRotation)
        {
            Debug.LogWarning("⚠️ ICC 시스템이 비활성화되어 있습니다!");
            return;
        }
        
        StopAllCoroutines();
        StartCoroutine(TestMovementCoroutine(3f, -1f, 8f, "ICC 축 기반 우회전"));
        Debug.Log("🌀 ICC 축 기반 우회전 테스트 시작 - Transform.RotateAround 사용");
    }
    
    [ContextMenu("Debug ICC Rotation Status")]
    public void DebugICCRotationStatus()
    {
        Debug.Log("══════════════════════════════════════════════════");
        Debug.Log("🌀 ICC 축 기반 회전 상태 진단");
        Debug.Log("══════════════════════════════════════════════════");
        Debug.Log($"🔌 ICC 시스템 활성화: {enableICCRotation}");
        Debug.Log($"🔄 현재 회전 중: {isRotating}");
        Debug.Log($"📐 회전 반지름: {currentTurningRadius:F2}m");
        Debug.Log($"⚡ 계산된 각속도: {calculatedAngularVelocity * Mathf.Rad2Deg:F1}도/초");
        Debug.Log($"🌀 Y축 회전 변화량: {iccYRotationDelta:F3}도/프레임");
        Debug.Log($"📊 누적 회전량: {accumulatedIccRotation:F1}도");
        Debug.Log($"📍 ICC 위치: {currentICC}");
        Debug.Log($"🏠 휠체어 위치: {transform.position}");
        Debug.Log($"🧭 휠체어 Y축 회전: {transform.eulerAngles.y:F1}도");
        
        if (isRotating && Mathf.Abs(currentTurningRadius) < maxTurningRadius)
        {
            Vector3 distanceToICC = currentICC - transform.position;
            Debug.Log($"📏 ICC까지 거리: {distanceToICC.magnitude:F2}m");
            Debug.Log($"✅ ICC 축 기반 회전 활성화됨");
        }
        else
        {
            Debug.Log($"❌ ICC 축 기반 회전 비활성화됨");
        }
        
        Debug.Log("══════════════════════════════════════════════════");
    }
} 
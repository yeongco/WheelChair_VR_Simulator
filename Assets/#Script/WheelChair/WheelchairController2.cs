using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Autohand;
using System.Reflection;

public class WheelchairController2 : MonoBehaviour
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
    
    [Header("🎮 이동 변환 설정")]
    public float movementScale = 0.1f; // Z 변화량을 이동거리로 변환하는 배율 (0.01에서 0.1로 증가)
    public float forwardSpeedMultiplier = 1f; // 전진 속도 배율
    public float backwardSpeedMultiplier = 0.8f; // 후진 속도 배율 (일반적으로 후진이 느림)
    public float maxSpeed = 8f; // 최대 이동 속도
    public float rotationScale = 0.1f; // 바퀴 차이를 회전속도로 변환하는 배율
    
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
    public float rotationSmoothing = 8f;
    
    [Header("🎛️ 물리 설정")]
    public Rigidbody chairRigidbody;
    public float chairMass = 80f;
    public float airResistance = 0.5f;
    public float angularDrag = 10f;
    
    [Header("🔍 디버그 표시")]
    public bool enableDebugLog = true;
    public bool showDirectionGizmos = true;
    public float gizmoLength = 1f;
    
    // 바퀴 Z 회전 변화량 (핵심 변수)
    private float leftWheelDeltaZ = 0f;  // 왼쪽 바퀴 Z 변화량 (도/프레임)
    private float rightWheelDeltaZ = 0f; // 오른쪽 바퀴 Z 변화량 (도/프레임)
    
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

    [Header("🌀 회전 임계값")]
    public float singleWheelTurnThreshold = 3f; // 한쪽 바퀴만 돌 때 임계값
    public float dualWheelTurnThreshold = 5f;   // 양쪽 합산 회전 임계값
    
    // Grab 관련 변수
    private bool _isGrabbed = false;
    
    void Start()
    {
        InitializeSuperconductorSystem();
        InitializeWheelZRotationSystem();
        
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
        
        // 7. 가상 경사로 힘 처리
        if (enableVirtualSlopes)
        {
            ProcessVirtualSlopeForces();
        }
        
        // 12. 물리 제한 적용
        ApplyPhysicsLimits();
        
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

    Vector3 GetCurrentForwardDirection()
    {
        return useLocalForwardDirection ? transform.forward : Vector3.forward;
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
    
    // ========== 디버그 및 기즈모 ==========
    
    void OnDrawGizmosSelected()
    {
        if (!showDirectionGizmos) return;
        
        // 바퀴 위치 표시 (노란색)
        Gizmos.color = Color.yellow;
        if (leftWheelTransform != null)
        {
            Gizmos.DrawWireSphere(leftWheelTransform.position, 0.1f);
            Gizmos.DrawLine(transform.position, leftWheelTransform.position);
        }
        if (rightWheelTransform != null)
        {
            Gizmos.DrawWireSphere(rightWheelTransform.position, 0.1f);
            Gizmos.DrawLine(transform.position, rightWheelTransform.position);
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
    }
    
    void ProcessVirtualSlopeForces()
    {
        if (activeVirtualSlopes.Count == 0)
        {
            currentVirtualSlopeForce = 0f;
            // 경사로가 없을 때 X 회전을 0으로 고정
            Vector3 currentRotation = transform.localEulerAngles;
            transform.localEulerAngles = new Vector3(0f, currentRotation.y, currentRotation.z);
            return;
        }
        
        float totalVirtualForce = 0f;
        
        foreach (var virtualSlope in activeVirtualSlopes)
        {
            if (virtualSlope != null)
            {
                var method = virtualSlope.GetType().GetMethod("CalculateSlopeEffect");
                if (method != null)
                {
                    float slopeEffect = (float)method.Invoke(virtualSlope, new object[] { transform });
                    totalVirtualForce += slopeEffect;
                }
            }
        }
        
        float targetForce = totalVirtualForce * virtualSlopeMultiplier;
        currentVirtualSlopeForce = Mathf.Lerp(currentVirtualSlopeForce, targetForce, virtualSlopeSmoothing * Time.fixedDeltaTime);
        
        if (Mathf.Abs(currentVirtualSlopeForce) > 0.01f)
        {
            Vector3 chairForward = GetCurrentForwardDirection();
            float slopeForwardDot = Vector3.Dot(slopeDirection, chairForward);
            
            // 경사로와 전진 방향 사이의 각도 계산 (0~180도)
            float angleBetween = Vector3.Angle(slopeDirection, chairForward);
            
            // 각도에 따른 회전력 계산 (90도일 때 0, 0도일 때 최대 양수, 180도일 때 최대 음수)
            float rotationForce = Mathf.Cos(angleBetween * Mathf.Deg2Rad) * currentVirtualSlopeForce * slopeZRotationForce;
            
            Vector3 leftWheelForce = Vector3.zero;
            Vector3 rightWheelForce = Vector3.zero;
            
            // 왼쪽 바퀴에 회전력 적용 (양수 = 전진)
            if (leftWheelTransform != null)
            {
                // 바퀴의 로컬 Z축을 기준으로 회전하는 힘 계산
                leftWheelForce = leftWheelTransform.TransformDirection(Vector3.forward) * rotationForce * slopeInfluence;
                chairRigidbody.AddForceAtPosition(leftWheelForce, leftWheelTransform.position, ForceMode.Force);
            }
            
            // 오른쪽 바퀴에 회전력 적용 (음수 = 전진)
            if (rightWheelTransform != null)
            {
                // 바퀴의 로컬 Z축을 기준으로 회전하는 힘 계산 (반대 방향)
                rightWheelForce = rightWheelTransform.TransformDirection(Vector3.forward) * rotationForce * slopeInfluence;
                chairRigidbody.AddForceAtPosition(rightWheelForce, rightWheelTransform.position, ForceMode.Force);
            }
            
            if (enableDebugLog && Time.fixedTime % 1f < Time.fixedDeltaTime)
            {
                Debug.Log($"🎮 가상 경사로 효과 적용 - 총 힘: {totalVirtualForce:F2}, 현재 힘: {currentVirtualSlopeForce:F2}");
                Debug.Log($"    경사로-전진 각도: {angleBetween:F1}도, 회전력: {rotationForce:F2}");
                Debug.Log($"    왼쪽 바퀴 힘: {leftWheelForce}, 오른쪽 바퀴 힘: {rightWheelForce}");
            }
        }
        else
        {
            // 경사로 효과가 없을 때도 X 회전을 0으로 고정
            Vector3 currentRotation = transform.localEulerAngles;
            transform.localEulerAngles = new Vector3(0f, currentRotation.y, currentRotation.z);
        }
    }
    
    public void IsGrabbed()
    {
        _isGrabbed = true;
    }
    
    public void IsReleased()
    {
        _isGrabbed = false;
    }
} 
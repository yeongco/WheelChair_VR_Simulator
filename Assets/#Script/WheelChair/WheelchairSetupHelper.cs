// using System.Collections;
// using System.Collections.Generic;
// using UnityEngine;
// using Autohand;

// [System.Serializable]
// public class WheelchairSetupHelper : MonoBehaviour
// {
//     [Header("🔋 초전도체 부양 설정")]
//     [SerializeField] private bool autoSetup = true;
//     [SerializeField] private float defaultHoverHeight = 0.3f; // 기본 부양 높이
//     [SerializeField] private float minHoverHeight = 0.1f; // 최소 부양 높이
//     [SerializeField] private bool enableSuperconductor = true; // 초전도체 시스템 활성화
    
//     [Header("🎯 지면 감지 설정")]
//     [SerializeField] private bool createGroundDetectionPoints = true;
//     [SerializeField] private float detectionPointOffset = 0.05f; // 감지 포인트 오프셋
    
//     [Header("🚗 바퀴 설정")]
//     [SerializeField] private Transform leftWheelMesh;
//     [SerializeField] private Transform rightWheelMesh;
//     [SerializeField] private float wheelRadius = 0.3f;
//     [SerializeField] private float wheelFriction = 0.95f; // 바퀴 마찰력
//     [SerializeField] private float wheelInputSensitivity = 100f; // 바퀴 입력 감도
    
//     [Header("🛡️ 안정성 설정")]
//     [SerializeField] private float stabilityForce = 15000f; // 안정화 힘
//     [SerializeField] private float maxTiltAngle = 3f; // 최대 기울기
//     [SerializeField] private bool enableGyroscopicStability = true; // 자이로스코프 안정화
    
//     [Header("🏃 이동 설정")]
//     [SerializeField] private float moveForce = 3000f; // 이동 힘
//     [SerializeField] private float rotationForce = 2000f; // 회전 힘
//     [SerializeField] private float maxSpeed = 8f; // 최대 속도
//     [SerializeField] private float maxAngularSpeed = 180f; // 최대 각속도
    
//     [Header("🎛️ 물리 설정")]
//     [SerializeField] private float chairMass = 80f; // 휠체어 질량
//     [SerializeField] private float airResistance = 0.5f; // 공기 저항
//     [SerializeField] private float angularDrag = 10f; // 각속도 저항
    
//     [Header("휠체어 크기")]
//     [SerializeField] private float chairLength = 1.2f; // 휠체어 길이
//     [SerializeField] private float chairWidth = 0.8f;  // 휠체어 너비
    
//     private WheelchairController wheelchairController;
    
//     void Start()
//     {
//         if (autoSetup)
//         {
//             SetupSuperconductorWheelchair();
//         }
//     }
    
//     [ContextMenu("🔋 Setup Superconductor Wheelchair")]
//     public void SetupSuperconductorWheelchair()
//     {
//         wheelchairController = GetComponent<WheelchairController>();
//         if (wheelchairController == null)
//         {
//             wheelchairController = gameObject.AddComponent<WheelchairController>();
//         }
        
//         // 1. Rigidbody 설정
//         SetupSuperconductorPhysics();
        
//         // 2. 지면 감지 포인트 생성
//         if (createGroundDetectionPoints)
//         {
//             CreateGroundDetectionPoints();
//         }
        
//         // 3. 바퀴 설정
//         SetupWheelSystem();
        
//         // 4. 초전도체 부양 설정 적용
//         ApplySuperconductorSettings();
        
//         Debug.Log("🔋 초전도체 휠체어 설정 완료!");
//         Debug.Log($"부양 높이: {defaultHoverHeight}m");
//         Debug.Log($"안정화 힘: {stabilityForce}");
//         Debug.Log($"최대 기울기: {maxTiltAngle}도");
//     }
    
//     void SetupSuperconductorPhysics()
//     {
//         Rigidbody rb = GetComponent<Rigidbody>();
//         if (rb == null)
//         {
//             rb = gameObject.AddComponent<Rigidbody>();
//         }
        
//         // 초전도체 부양을 위한 물리 설정
//         rb.mass = chairMass;
//         rb.useGravity = false; // 중력 비활성화 (초전도체 부양)
//         rb.drag = airResistance;
//         rb.angularDrag = angularDrag;
//         rb.centerOfMass = new Vector3(0, -0.2f, 0); // 낮은 무게중심으로 안정성 향상
//         rb.maxAngularVelocity = maxAngularSpeed * Mathf.Deg2Rad;
        
//         wheelchairController.chairRigidbody = rb;
        
//         Debug.Log("초전도체 물리 시스템 설정 완료 - 중력 비활성화");
//     }
    
//     void CreateGroundDetectionPoints()
//     {
//         // 기존 감지 포인트들 제거
//         Transform[] existingPoints = GetComponentsInChildren<Transform>();
//         foreach (Transform child in existingPoints)
//         {
//             if (child != transform && child.name.Contains("GroundDetectionPoint"))
//             {
//                 DestroyImmediate(child.gameObject);
//             }
//         }
        
//         // 4개 감지 포인트 생성 (휠체어 모서리)
//         float halfWidth = chairWidth * 0.5f;
//         float halfLength = chairLength * 0.5f;
        
//         Vector3[] positions = {
//             new Vector3(-halfWidth, detectionPointOffset, halfLength),   // 왼쪽 앞
//             new Vector3(halfWidth, detectionPointOffset, halfLength),    // 오른쪽 앞
//             new Vector3(-halfWidth, detectionPointOffset, -halfLength),  // 왼쪽 뒤
//             new Vector3(halfWidth, detectionPointOffset, -halfLength)    // 오른쪽 뒤
//         };
        
//         Transform[] detectionPoints = new Transform[4];
        
//         for (int i = 0; i < 4; i++)
//         {
//             GameObject point = new GameObject($"GroundDetectionPoint_{i}");
//             point.transform.SetParent(transform);
//             point.transform.localPosition = positions[i];
//             point.transform.localRotation = Quaternion.identity;
            
//             // 시각적 표시를 위한 기즈모 추가
//             GizmoHelper gizmo = point.AddComponent<GizmoHelper>();
//             gizmo.color = Color.cyan;
//             gizmo.size = 0.08f;
            
//             detectionPoints[i] = point.transform;
//         }
        
//         wheelchairController.groundDetectionPoints = detectionPoints;
        
//         Debug.Log("4점 지면 감지 시스템 생성 완료");
//     }
    
//     void SetupWheelSystem()
//     {
//         // 바퀴 메시 할당
//         if (leftWheelMesh != null)
//         {
//             wheelchairController.leftWheel = leftWheelMesh;
//             SetupWheelGrabbable(leftWheelMesh.gameObject, "LeftWheel");
//             wheelchairController.leftWheelGrab = leftWheelMesh.GetComponent<Grabbable>();
//         }
        
//         if (rightWheelMesh != null)
//         {
//             wheelchairController.rightWheel = rightWheelMesh;
//             SetupWheelGrabbable(rightWheelMesh.gameObject, "RightWheel");
//             wheelchairController.rightWheelGrab = rightWheelMesh.GetComponent<Grabbable>();
//         }
        
//         Debug.Log("바퀴 시스템 설정 완료");
//     }
    
//     void SetupWheelGrabbable(GameObject wheelObj, string wheelName)
//     {
//         // Grabbable 컴포넌트 추가
//         Grabbable grabbable = wheelObj.GetComponent<Grabbable>();
//         if (grabbable == null)
//         {
//             grabbable = wheelObj.AddComponent<Grabbable>();
//         }
        
//         // Collider 확인 및 추가
//         Collider col = wheelObj.GetComponent<Collider>();
//         if (col == null)
//         {
//             // 바퀴 모양에 맞는 Collider 추가
//             CapsuleCollider capsule = wheelObj.AddComponent<CapsuleCollider>();
//             capsule.direction = 0; // X축 방향
//             capsule.radius = wheelRadius;
//             capsule.height = 0.1f; // 바퀴 두께
//         }
        
//         // Grabbable 설정
//         grabbable.name = wheelName;
//         grabbable.grabType = HandGrabType.HandToGrabbable;
//         grabbable.throwPower = 0f; // 던지기 비활성화
//         grabbable.jointBreakForce = 1000f;
//         grabbable.instantGrab = false;
//         grabbable.maintainGrabOffset = true;
//         grabbable.parentOnGrab = false;
        
//         // 바퀴는 Kinematic으로 설정 (초전도체 시스템에서는 물리적 연결 불필요)
//         Rigidbody wheelRb = wheelObj.GetComponent<Rigidbody>();
//         if (wheelRb == null)
//         {
//             wheelRb = wheelObj.AddComponent<Rigidbody>();
//         }
//         wheelRb.isKinematic = true; // 바퀴는 시각적 목적만
        
//         grabbable.body = wheelRb;
        
//         Debug.Log($"{wheelName} Grabbable 설정 완료");
//     }
    
//     void ApplySuperconductorSettings()
//     {
//         if (wheelchairController == null) return;
        
//         // 초전도체 부양 설정
//         wheelchairController.enableSuperconductorHover = enableSuperconductor;
//         wheelchairController.hoverHeight = defaultHoverHeight;
//         wheelchairController.minHoverHeight = minHoverHeight;
//         wheelchairController.hoverForce = 8000f;
//         wheelchairController.hoverDamping = 1000f;
//         wheelchairController.hoverStiffness = 5000f;
        
//         // 안정성 설정
//         wheelchairController.stabilityForce = stabilityForce;
//         wheelchairController.stabilityDamping = 2000f;
//         wheelchairController.maxTiltAngle = maxTiltAngle;
//         wheelchairController.stabilityResponseSpeed = 20f;
//         wheelchairController.enableGyroscopicStabilization = enableGyroscopicStability;
        
//         // 바퀴 설정
//         wheelchairController.wheelRadius = wheelRadius;
//         wheelchairController.wheelFriction = wheelFriction;
//         wheelchairController.speedToRotationRatio = 10f;
//         wheelchairController.wheelInputSensitivity = wheelInputSensitivity;
        
//         // 이동 설정
//         wheelchairController.moveForce = moveForce;
//         wheelchairController.rotationForce = rotationForce;
//         wheelchairController.maxSpeed = maxSpeed;
//         wheelchairController.maxAngularSpeed = maxAngularSpeed;
        
//         // 물리 설정
//         wheelchairController.chairMass = chairMass;
//         wheelchairController.airResistance = airResistance;
//         wheelchairController.angularDrag = angularDrag;
        
//         // 지면 감지 설정
//         wheelchairController.groundCheckDistance = 2f;
//         wheelchairController.contactPointOffset = detectionPointOffset;
//         wheelchairController.groundLayer = 1;
        
//         Debug.Log("초전도체 설정 적용 완료");
//     }
    
//     [ContextMenu("🔧 Adjust Hover Height")]
//     public void AdjustHoverHeight()
//     {
//         if (wheelchairController != null)
//         {
//             wheelchairController.SetHoverHeight(defaultHoverHeight);
//             Debug.Log($"부양 높이 조정: {defaultHoverHeight}m");
//         }
//     }
    
//     [ContextMenu("🛡️ Test Stability")]
//     public void TestStability()
//     {
//         if (wheelchairController != null)
//         {
//             float stability = wheelchairController.GetCurrentStability();
//             bool isStable = wheelchairController.IsStable();
            
//             Debug.Log($"현재 안정성: {stability:F2} (1.0이 완전 안정)");
//             Debug.Log($"안정 상태: {(isStable ? "안정" : "불안정")}");
//             Debug.Log($"현재 기울기: {Vector3.Angle(transform.up, Vector3.up):F1}도");
//         }
//     }
    
//     [ContextMenu("🔋 Enable Superconductor Mode")]
//     public void EnableSuperconductorMode()
//     {
//         if (wheelchairController != null)
//         {
//             wheelchairController.enableSuperconductorHover = true;
            
//             // 중력 비활성화
//             Rigidbody rb = wheelchairController.chairRigidbody;
//             if (rb != null)
//             {
//                 rb.useGravity = false;
//             }
            
//             Debug.Log("🔋 초전도체 부양 모드 활성화!");
//         }
//     }
    
//     [ContextMenu("🌍 Disable Superconductor Mode")]
//     public void DisableSuperconductorMode()
//     {
//         if (wheelchairController != null)
//         {
//             wheelchairController.enableSuperconductorHover = false;
            
//             // 중력 활성화
//             Rigidbody rb = wheelchairController.chairRigidbody;
//             if (rb != null)
//             {
//                 rb.useGravity = true;
//             }
            
//             Debug.Log("🌍 일반 물리 모드로 전환!");
//         }
//     }
    
//     [ContextMenu("⚡ Increase Stability")]
//     public void IncreaseStability()
//     {
//         stabilityForce += 5000f;
//         maxTiltAngle = Mathf.Max(1f, maxTiltAngle - 0.5f);
        
//         if (wheelchairController != null)
//         {
//             wheelchairController.stabilityForce = stabilityForce;
//             wheelchairController.maxTiltAngle = maxTiltAngle;
//         }
        
//         Debug.Log($"안정성 증가 - 힘: {stabilityForce}, 최대 기울기: {maxTiltAngle}도");
//     }
    
//     [ContextMenu("🎯 Reset Ground Detection")]
//     public void ResetGroundDetection()
//     {
//         CreateGroundDetectionPoints();
//         Debug.Log("지면 감지 포인트 재설정 완료");
//     }
    
//     [ContextMenu("🚗 Test Wheel Response")]
//     public void TestWheelResponse()
//     {
//         if (wheelchairController == null) return;
        
//         Debug.Log("=== 바퀴 반응성 테스트 ===");
//         Debug.Log($"바퀴 마찰력: {wheelchairController.wheelFriction}");
//         Debug.Log($"입력 감도: {wheelchairController.wheelInputSensitivity}");
//         Debug.Log($"속도-회전 비율: {wheelchairController.speedToRotationRatio}");
        
//         // 바퀴 Grabbable 상태 확인
//         if (wheelchairController.leftWheelGrab != null)
//         {
//             Debug.Log($"왼쪽 바퀴 Grabbable: 정상");
//         }
//         else
//         {
//             Debug.LogWarning("왼쪽 바퀴 Grabbable이 설정되지 않았습니다!");
//         }
        
//         if (wheelchairController.rightWheelGrab != null)
//         {
//             Debug.Log($"오른쪽 바퀴 Grabbable: 정상");
//         }
//         else
//         {
//             Debug.LogWarning("오른쪽 바퀴 Grabbable이 설정되지 않았습니다!");
//         }
//     }
    
//     [ContextMenu("🔄 Reset Wheelchair")]
//     public void ResetWheelchair()
//     {
//         // 기존 컴포넌트들 제거
//         WheelchairController[] controllers = GetComponents<WheelchairController>();
//         foreach (var controller in controllers)
//         {
//             DestroyImmediate(controller);
//         }
        
//         // 생성된 감지 포인트들 제거
//         Transform[] children = GetComponentsInChildren<Transform>();
//         foreach (Transform child in children)
//         {
//             if (child != transform && child.name.Contains("GroundDetectionPoint"))
//             {
//                 DestroyImmediate(child.gameObject);
//             }
//         }
        
//         Debug.Log("휠체어 설정 초기화 완료");
//     }
    
//     void OnValidate()
//     {
//         // Inspector에서 값이 변경될 때 실시간 업데이트
//         if (wheelchairController != null && Application.isPlaying)
//         {
//             wheelchairController.hoverHeight = defaultHoverHeight;
//             wheelchairController.minHoverHeight = minHoverHeight;
//             wheelchairController.stabilityForce = stabilityForce;
//             wheelchairController.maxTiltAngle = maxTiltAngle;
//             wheelchairController.wheelFriction = wheelFriction;
//             wheelchairController.wheelInputSensitivity = wheelInputSensitivity;
//             wheelchairController.moveForce = moveForce;
//             wheelchairController.rotationForce = rotationForce;
//             wheelchairController.maxSpeed = maxSpeed;
//             wheelchairController.maxAngularSpeed = maxAngularSpeed;
//         }
//     }
// }

// // 기즈모 표시를 위한 헬퍼 클래스
// public class GizmoHelper : MonoBehaviour
// {
//     public Color color = Color.white;
//     public float size = 0.1f;
    
//     void OnDrawGizmos()
//     {
//         Gizmos.color = color;
//         Gizmos.DrawWireSphere(transform.position, size);
        
//         // 이름 표시 (Scene View에서)
//         #if UNITY_EDITOR
//         UnityEditor.Handles.Label(transform.position + Vector3.up * (size + 0.1f), gameObject.name);
//         #endif
//     }
// }

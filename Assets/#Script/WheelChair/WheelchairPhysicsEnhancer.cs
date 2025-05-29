using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(WheelchairController))]
public class WheelchairPhysicsEnhancer : MonoBehaviour
{
//     [Header("고급 물리 설정")]
//     [SerializeField] private bool enableAdvancedPhysics = true;
    
//     [Header("바퀴 마찰 설정")]
//     [SerializeField] private PhysicMaterial wheelPhysicMaterial;
//     [SerializeField] private float wheelFriction = 0.8f;
//     [SerializeField] private float wheelBounciness = 0.1f;
    
//     [Header("서스펜션 시뮬레이션")]
//     [SerializeField] private bool enableSuspension = true;
//     [SerializeField] private float suspensionStrength = 1000f;
//     [SerializeField] private float suspensionDamping = 50f;
//     [SerializeField] private float suspensionRange = 0.2f;
    
//     [Header("관성 시뮬레이션")]
//     [SerializeField] private bool enableInertia = true;
//     [SerializeField] private float inertiaMultiplier = 1.2f;
//     [SerializeField] private float brakingForce = 500f;
    
//     [Header("지면 접촉 감지")]
//     [SerializeField] private bool enableGroundContact = true;
//     [SerializeField] private LayerMask groundMask = 1;
//     [SerializeField] private float contactCheckRadius = 0.1f;
    
//     [Header("안정성 향상")]
//     [SerializeField] private bool enableStabilityAssist = true;
//     [SerializeField] private float stabilityThreshold = 20f; // 도
//     [SerializeField] private float stabilityRecoveryForce = 2000f;
    
//     private WheelchairController wheelchairController;
//     private Rigidbody chairRigidbody;
    
//     // 서스펜션 상태
//     private float leftWheelSuspension = 0f;
//     private float rightWheelSuspension = 0f;
    
//     // 관성 상태
//     private Vector3 lastVelocity;
//     private Vector3 lastAngularVelocity;
    
//     // 지면 접촉 상태
//     private bool leftWheelGrounded = false;
//     private bool rightWheelGrounded = false;
    
//     void Start()
//     {
//         wheelchairController = GetComponent<WheelchairController>();
//         chairRigidbody = wheelchairController.chairRigidbody;
        
//         if (enableAdvancedPhysics)
//         {
//             SetupPhysicMaterials();
//             SetupAdvancedPhysics();
//         }
        
//         lastVelocity = chairRigidbody.velocity;
//         lastAngularVelocity = chairRigidbody.angularVelocity;
//     }
    
//     void SetupPhysicMaterials()
//     {
//         // 바퀴용 물리 재질 생성
//         if (wheelPhysicMaterial == null)
//         {
//             wheelPhysicMaterial = new PhysicMaterial("WheelMaterial");
//             wheelPhysicMaterial.dynamicFriction = wheelFriction;
//             wheelPhysicMaterial.staticFriction = wheelFriction;
//             wheelPhysicMaterial.bounciness = wheelBounciness;
//             wheelPhysicMaterial.frictionCombine = PhysicMaterialCombine.Average;
//             wheelPhysicMaterial.bounceCombine = PhysicMaterialCombine.Average;
//         }
        
//         // 바퀴 콜라이더에 물리 재질 적용
//         ApplyPhysicMaterialToWheels();
//     }
    
//     void ApplyPhysicMaterialToWheels()
//     {
//         if (wheelchairController.leftWheel != null)
//         {
//             Collider leftCollider = wheelchairController.leftWheel.GetComponent<Collider>();
//             if (leftCollider != null)
//                 leftCollider.material = wheelPhysicMaterial;
//         }
        
//         if (wheelchairController.rightWheel != null)
//         {
//             Collider rightCollider = wheelchairController.rightWheel.GetComponent<Collider>();
//             if (rightCollider != null)
//                 rightCollider.material = wheelPhysicMaterial;
//         }
//     }
    
//     void SetupAdvancedPhysics()
//     {
//         // 더 정밀한 물리 설정
//         chairRigidbody.interpolation = RigidbodyInterpolation.Interpolate;
//         chairRigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        
//         // 관절 설정 최적화
//         ConfigurableJoint joint = chairRigidbody.GetComponent<ConfigurableJoint>();
//         if (joint == null)
//         {
//             joint = chairRigidbody.gameObject.AddComponent<ConfigurableJoint>();
//         }
        
//         // 위치 제한 설정
//         joint.xMotion = ConfigurableJointMotion.Free;
//         joint.yMotion = ConfigurableJointMotion.Limited;
//         joint.zMotion = ConfigurableJointMotion.Free;
        
//         // 회전 제한 설정
//         joint.angularXMotion = ConfigurableJointMotion.Limited;
//         joint.angularYMotion = ConfigurableJointMotion.Free;
//         joint.angularZMotion = ConfigurableJointMotion.Limited;
        
//         // 제한 값 설정
//         SoftJointLimit linearLimit = new SoftJointLimit();
//         linearLimit.limit = 0.5f;
//         joint.linearLimit = linearLimit;
        
//         SoftJointLimit angularLimit = new SoftJointLimit();
//         angularLimit.limit = 15f;
//         joint.lowAngularXLimit = angularLimit;
//         joint.highAngularXLimit = angularLimit;
//         joint.angularZLimit = angularLimit;
//     }
    
//     void FixedUpdate()
//     {
//         if (!enableAdvancedPhysics) return;
        
//         if (enableGroundContact)
//         {
//             CheckGroundContact();
//         }
        
//         if (enableSuspension)
//         {
//             SimulateSuspension();
//         }
        
//         if (enableInertia)
//         {
//             SimulateInertia();
//         }
        
//         if (enableStabilityAssist)
//         {
//             ApplyStabilityAssist();
//         }
        
//         // 이전 프레임 값 저장
//         lastVelocity = chairRigidbody.velocity;
//         lastAngularVelocity = chairRigidbody.angularVelocity;
//     }
    
//     void CheckGroundContact()
//     {
//         // 왼쪽 바퀴 지면 접촉 확인
//         if (wheelchairController.leftWheelCenter != null)
//         {
//             Vector3 leftWheelPos = wheelchairController.leftWheelCenter.position;
//             leftWheelGrounded = Physics.CheckSphere(leftWheelPos, contactCheckRadius, groundMask);
//         }
        
//         // 오른쪽 바퀴 지면 접촉 확인
//         if (wheelchairController.rightWheelCenter != null)
//         {
//             Vector3 rightWheelPos = wheelchairController.rightWheelCenter.position;
//             rightWheelGrounded = Physics.CheckSphere(rightWheelPos, contactCheckRadius, groundMask);
//         }
        
//         // 지면 접촉에 따른 물리 조정
//         if (!leftWheelGrounded || !rightWheelGrounded)
//         {
//             // 공중에 있을 때 안정성 감소
//             chairRigidbody.drag *= 0.5f;
//             chairRigidbody.angularDrag *= 0.5f;
//         }
//     }
    
//     void SimulateSuspension()
//     {
//         // 왼쪽 바퀴 서스펜션
//         if (wheelchairController.leftWheelCenter != null && leftWheelGrounded)
//         {
//             Vector3 leftWheelPos = wheelchairController.leftWheelCenter.position;
//             RaycastHit hit;
            
//             if (Physics.Raycast(leftWheelPos, Vector3.down, out hit, suspensionRange, groundMask))
//             {
//                 float compression = 1f - (hit.distance / suspensionRange);
//                 float suspensionForce = compression * suspensionStrength;
                
//                 // 서스펜션 힘 적용
//                 Vector3 force = Vector3.up * suspensionForce;
//                 chairRigidbody.AddForceAtPosition(force, leftWheelPos, ForceMode.Force);
                
//                 // 댐핑 적용
//                 float dampingForce = chairRigidbody.velocity.y * suspensionDamping;
//                 chairRigidbody.AddForceAtPosition(Vector3.down * dampingForce, leftWheelPos, ForceMode.Force);
                
//                 leftWheelSuspension = compression;
//             }
//         }
        
//         // 오른쪽 바퀴 서스펜션
//         if (wheelchairController.rightWheelCenter != null && rightWheelGrounded)
//         {
//             Vector3 rightWheelPos = wheelchairController.rightWheelCenter.position;
//             RaycastHit hit;
            
//             if (Physics.Raycast(rightWheelPos, Vector3.down, out hit, suspensionRange, groundMask))
//             {
//                 float compression = 1f - (hit.distance / suspensionRange);
//                 float suspensionForce = compression * suspensionStrength;
                
//                 // 서스펜션 힘 적용
//                 Vector3 force = Vector3.up * suspensionForce;
//                 chairRigidbody.AddForceAtPosition(force, rightWheelPos, ForceMode.Force);
                
//                 // 댐핑 적용
//                 float dampingForce = chairRigidbody.velocity.y * suspensionDamping;
//                 chairRigidbody.AddForceAtPosition(Vector3.down * dampingForce, rightWheelPos, ForceMode.Force);
                
//                 rightWheelSuspension = compression;
//             }
//         }
//     }
    
//     void SimulateInertia()
//     {
//         // 속도 변화량 계산
//         Vector3 velocityChange = chairRigidbody.velocity - lastVelocity;
//         Vector3 angularVelocityChange = chairRigidbody.angularVelocity - lastAngularVelocity;
        
//         // 관성 효과 적용
//         if (velocityChange.magnitude > 0.1f)
//         {
//             Vector3 inertiaForce = -velocityChange * chairRigidbody.mass * inertiaMultiplier;
//             chairRigidbody.AddForce(inertiaForce, ForceMode.Force);
//         }
        
//         // 급격한 회전 변화 제한
//         if (angularVelocityChange.magnitude > 1f)
//         {
//             Vector3 inertiaTorque = -angularVelocityChange * inertiaMultiplier;
//             chairRigidbody.AddTorque(inertiaTorque, ForceMode.Force);
//         }
        
//         // 브레이킹 효과 (바퀴를 잡고 있지 않을 때)
//         bool anyWheelGrabbed = wheelchairController.leftWheelGrab.GetHeldBy().Count > 0 || 
//                               wheelchairController.rightWheelGrab.GetHeldBy().Count > 0;
        
//         if (!anyWheelGrabbed && chairRigidbody.velocity.magnitude > 0.1f)
//         {
//             Vector3 brakingForceVector = -chairRigidbody.velocity.normalized * brakingForce * Time.fixedDeltaTime;
//             chairRigidbody.AddForce(brakingForceVector, ForceMode.Force);
//         }
//     }
    
//     void ApplyStabilityAssist()
//     {
//         // 현재 기울기 각도 계산
//         float currentTilt = Vector3.Angle(transform.up, Vector3.up);
        
//         if (currentTilt > stabilityThreshold)
//         {
//             // 안정성 보조 힘 계산
//             Vector3 correctionDirection = Vector3.Cross(transform.up, Vector3.up).normalized;
//             float correctionMagnitude = (currentTilt - stabilityThreshold) / stabilityThreshold;
            
//             Vector3 stabilityForce = correctionDirection * correctionMagnitude * stabilityRecoveryForce;
//             chairRigidbody.AddTorque(stabilityForce, ForceMode.Force);
            
//             // 과도한 회전 속도 제한
//             if (chairRigidbody.angularVelocity.magnitude > 3f)
//             {
//                 chairRigidbody.angularVelocity = chairRigidbody.angularVelocity.normalized * 3f;
//             }
//         }
//     }
    
//     // 디버그 정보 표시
//     void OnDrawGizmos()
//     {
//         if (!enableAdvancedPhysics) return;
        
//         // 지면 접촉 확인 영역 표시
//         if (enableGroundContact && wheelchairController != null)
//         {
//             if (wheelchairController.leftWheelCenter != null)
//             {
//                 Gizmos.color = leftWheelGrounded ? Color.green : Color.red;
//                 Gizmos.DrawWireSphere(wheelchairController.leftWheelCenter.position, contactCheckRadius);
//             }
            
//             if (wheelchairController.rightWheelCenter != null)
//             {
//                 Gizmos.color = rightWheelGrounded ? Color.green : Color.red;
//                 Gizmos.DrawWireSphere(wheelchairController.rightWheelCenter.position, contactCheckRadius);
//             }
//         }
        
//         // 서스펜션 범위 표시
//         if (enableSuspension && wheelchairController != null)
//         {
//             if (wheelchairController.leftWheelCenter != null)
//             {
//                 Gizmos.color = Color.yellow;
//                 Vector3 start = wheelchairController.leftWheelCenter.position;
//                 Vector3 end = start + Vector3.down * suspensionRange;
//                 Gizmos.DrawLine(start, end);
//             }
            
//             if (wheelchairController.rightWheelCenter != null)
//             {
//                 Gizmos.color = Color.yellow;
//                 Vector3 start = wheelchairController.rightWheelCenter.position;
//                 Vector3 end = start + Vector3.down * suspensionRange;
//                 Gizmos.DrawLine(start, end);
//             }
//         }
//     }
    
//     // 런타임에서 설정 변경 가능
//     public void SetWheelFriction(float friction)
//     {
//         wheelFriction = friction;
//         if (wheelPhysicMaterial != null)
//         {
//             wheelPhysicMaterial.dynamicFriction = friction;
//             wheelPhysicMaterial.staticFriction = friction;
//         }
//     }
    
//     public void SetSuspensionStrength(float strength)
//     {
//         suspensionStrength = strength;
//     }
    
//     public void SetBrakingForce(float force)
//     {
//         brakingForce = force;
//     }
    
//     // 현재 상태 정보 반환
//     public bool IsGrounded()
//     {
//         return leftWheelGrounded && rightWheelGrounded;
//     }
    
//     public float GetSuspensionCompression(bool leftWheel)
//     {
//         return leftWheel ? leftWheelSuspension : rightWheelSuspension;
//     }
    
//     public Vector3 GetCurrentInertia()
//     {
//         return chairRigidbody.velocity - lastVelocity;
//     }
 }

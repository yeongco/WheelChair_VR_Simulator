using UnityEngine;
using Autohand;
using System.Collections;

namespace Obi.Samples
{
    [RequireComponent(typeof(TangledPeg))]
    [RequireComponent(typeof(Grabbable))]
    public class TanglePegGrabbableEnhancer : MonoBehaviour
    {
        [Header("Enhanced Grabbable Settings")]
        [Tooltip("즉시 반응하도록 하는 힘의 배수")]
        public float forceMultiplier = 1.5f;
        
        [Tooltip("최대 허용 거리 (이 거리 이상 떨어지면 강제로 당김)")]
        public float maxDistanceFromHand = 0.2f;
        
        [Tooltip("부드러운 따라오기를 위한 댐핑")]
        public float damping = 2.0f;

        [Tooltip("회전 제한 활성화")]
        public bool enableRotationControl = false;
        
        [Tooltip("부모 붙이기 모드 (손 밀림 현상 방지)")]
        public bool useParentMode = false;

        private TangledPeg tanglePeg;
        private Grabbable grabbable;
        private Rigidbody rb;
        private ObiRigidbody obiRigidbody;
        
        private bool wasKinematic = false;
        private bool wasUseGravity = false;
        private float originalDrag = 0f;
        private float originalAngularDrag = 0f;

        void Awake()
        {
            tanglePeg = GetComponent<TangledPeg>();
            grabbable = GetComponent<Grabbable>();
            rb = GetComponent<Rigidbody>();
            obiRigidbody = GetComponent<ObiRigidbody>();
        }

        void Start()
        {
            // Grabbable 이벤트 연결
            grabbable.OnGrabEvent += OnGrabbed;
            grabbable.OnReleaseEvent += OnReleased;
            
            // 원래 물리 설정 저장
            SaveOriginalPhysicsSettings();
            
            // 손 밀림 방지를 위한 기본 설정 적용
            ConfigureGrabbableForStability();
        }

        void SaveOriginalPhysicsSettings()
        {
            if (rb != null)
            {
                wasKinematic = rb.isKinematic;
                wasUseGravity = rb.useGravity;
                originalDrag = rb.drag;
                originalAngularDrag = rb.angularDrag;
            }
        }
        
        void ConfigureGrabbableForStability()
        {
            if (grabbable != null)
            {
                // 완전 격리 + 안정화 방식 - 가장 안정적인 설정
                if (useParentMode)
                {
                    // 부모 붙이기 모드
                    grabbable.parentOnGrab = true;
                    grabbable.instantGrab = true;
                    grabbable.maintainGrabOffset = true;
                    grabbable.useGentleGrab = false;
                    grabbable.jointBreakForce = float.MaxValue;
                    grabbable.throwPower = 0.1f;
                }
                else
                {
                    // 물리 기반 모드 - 끊김 방지를 위한 초강력 설정
                    grabbable.parentOnGrab = false;
                    grabbable.instantGrab = false;
                    grabbable.maintainGrabOffset = true;
                    grabbable.useGentleGrab = true;
                    
                    // 조인트가 절대 끊어지지 않도록 매우 높은 값
                    grabbable.jointBreakForce = 50000f; // 끊김 방지
                    grabbable.throwPower = 0.2f;
                    
                    // 매우 안정적인 물리 설정
                    grabbable.minHeldDrag = 1f;
                    grabbable.minHeldAngleDrag = 1f;
                    grabbable.maxHeldVelocity = 20f; // 높은 속도 허용
                    grabbable.minHeldMass = 3f; // 질량 고정
                    
                    // 조인트 안정성 향상
                    grabbable.pullApartBreakOnly = false; // 모든 방향 힘 허용
                    grabbable.ignoreWeight = false;
                    grabbable.heldNoFriction = false; // 마찰 유지로 안정성 향상
                }
            }
        }

        void OnGrabbed(Hand hand, Grabbable grab)
        {
            // 🔒 완전 격리 모드: 다른 모든 물리 시스템 차단
            
            // TangledPeg의 물리 로직 완전 비활성화
            if (tanglePeg != null)
            {
                tanglePeg.enabled = false; // 전체 스크립트 비활성화로 완전 격리
            }
            
            if (useParentMode)
            {
                // 부모 붙이기 모드: 물리 완전 정지
                if (rb != null)
                {
                    rb.isKinematic = true;
                    rb.useGravity = false;
                    rb.mass = 3f; // 질량 고정
                    rb.velocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                }
                
                // ObiRigidbody 완전 kinematic
                if (obiRigidbody != null)
                {
                    obiRigidbody.kinematicForParticles = true;
                }
            }
            else
            {
                // 물리 기반 모드: AutoHand만 제어하도록 최적화
                if (rb != null)
                {
                    rb.isKinematic = false;
                    rb.useGravity = false;
                    rb.mass = 3f; // 질량 고정 - 변화 없음
                    
                    // 안정적인 물리 설정
                    rb.drag = 2f; // 낮은 드래그
                    rb.angularDrag = 2f;
                    rb.interpolation = RigidbodyInterpolation.Interpolate;
                    rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
                    
                    // 초기 속도 제거
                    rb.velocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                }

                // ObiRigidbody는 격리하지만 완전 비활성화는 아님
                if (obiRigidbody != null)
                {
                    obiRigidbody.kinematicForParticles = true; // 일단 kinematic으로
                }
            }
            
            // Transform 부모 관계 정리 (TrackingSpace는 보호)
            if (transform.parent != null && !transform.parent.name.Contains("TrackingSpace"))
            {
                transform.SetParent(null, true);
            }
            
            Debug.Log($"[TanglePeg] 완전 격리 모드 활성화: {gameObject.name}");
        }

        void OnReleased(Hand hand, Grabbable grab)
        {
            Debug.Log($"[TanglePeg] 격리 해제 및 시스템 재활성화: {gameObject.name}");
            
            // 🔓 격리 해제: 모든 시스템 단계적 재활성화
            StartCoroutine(GradualSystemReactivation());
        }
        
        private System.Collections.IEnumerator GradualSystemReactivation()
        {
            // 1단계: 기본 물리 설정 복원
            if (rb != null)
            {
                rb.isKinematic = false;
                rb.useGravity = false; // 중력은 여전히 비활성화
                rb.mass = 3f; // 질량 고정 유지
                rb.interpolation = RigidbodyInterpolation.Interpolate;
                rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
                
                // 놓을 때 적절한 물리 설정
                rb.drag = 4f; // 안정적인 드래그
                rb.angularDrag = 4f;
                
                // 잔여 속도 정리
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
            
            // 2단계: ObiRigidbody 재활성화 (1프레임 대기)
            yield return new WaitForFixedUpdate();
            
            if (obiRigidbody != null)
            {
                obiRigidbody.kinematicForParticles = false;
            }
            
            // 3단계: TangledPeg 재활성화 (추가 1프레임 대기)
            yield return new WaitForFixedUpdate();
            
            if (tanglePeg != null)
            {
                tanglePeg.enabled = true; // TangledPeg 물리 로직 재활성화
            }
            
            // 4단계: 슬롯 이동 로직 실행 (모든 시스템 안정화 후)
            yield return new WaitForFixedUpdate();
            
            // TangledPeg의 슬롯 찾기 로직 호출
            if (tanglePeg != null)
            {
                var method = typeof(TangledPeg).GetMethod("FindAndMoveToNearestSlot", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (method != null)
                {
                    StartCoroutine((System.Collections.IEnumerator)method.Invoke(tanglePeg, null));
                }
            }
            
            Debug.Log($"[TanglePeg] 모든 시스템 재활성화 완료: {gameObject.name}");
        }

        private System.Collections.IEnumerator DelayedPhysicsRestore()
        {
            yield return new WaitForSeconds(0.5f);
            
            if (rb != null)
            {
                rb.isKinematic = wasKinematic;
                rb.useGravity = wasUseGravity;
                rb.drag = originalDrag;
                rb.angularDrag = originalAngularDrag;
                rb.interpolation = RigidbodyInterpolation.None;
                rb.collisionDetectionMode = CollisionDetectionMode.Discrete;
            }
        }
        
        private System.Collections.IEnumerator FindAndMoveToNearestSlot()
        {
            yield return new WaitForFixedUpdate();
            
            // TangledPeg의 슬롯 찾기 로직 호출
            if (tanglePeg != null)
            {
                var method = typeof(TangledPeg).GetMethod("FindAndMoveToNearestSlot", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (method != null)
                {
                    StartCoroutine((System.Collections.IEnumerator)method.Invoke(tanglePeg, null));
                }
            }
        }

        void FixedUpdate()
        {
            // 🛡️ 완전 격리 모드에서는 최소한의 간섭만
            if (grabbable.IsHeld() && rb != null)
            {
                // Transform 부모 관계 보호 (TrackingSpace는 보호)
                if (transform.parent != null && !transform.parent.name.Contains("TrackingSpace"))
                {
                    transform.SetParent(null, true);
                }
                
                // 극한 상황에서만 속도 제한 (매우 여유롭게)
                if (rb.velocity.magnitude > 25f) // 매우 높은 임계값
                {
                    rb.velocity = rb.velocity.normalized * 25f;
                }
                
                if (rb.angularVelocity.magnitude > 25f) // 매우 높은 임계값
                {
                    rb.angularVelocity = rb.angularVelocity.normalized * 25f;
                }
                
                // 질량 고정 강제 유지 (다른 시스템에서 변경 방지)
                if (rb.mass != 3f)
                {
                    rb.mass = 3f;
                }
            }
        }

        void OnDestroy()
        {
            // 이벤트 해제
            if (grabbable != null)
            {
                grabbable.OnGrabEvent -= OnGrabbed;
                grabbable.OnReleaseEvent -= OnReleased;
            }
        }
    }
} 
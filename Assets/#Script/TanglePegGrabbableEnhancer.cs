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
        public float forceMultiplier = 3.0f;
        
        [Tooltip("최대 허용 거리 (이 거리 이상 떨어지면 강제로 당김)")]
        public float maxDistanceFromHand = 0.3f;
        
        [Tooltip("부드러운 따라오기를 위한 댐핑")]
        public float damping = 1.2f;

        [Tooltip("회전 제한 활성화")]
        public bool enableRotationControl = false;

        private TangledPeg tanglePeg;
        private Grabbable grabbable;
        private Rigidbody rb;
        private ObiRigidbody obiRigidbody;
        
        private bool wasKinematic = false;
        private bool wasUseGravity = false;
        private float originalDrag = 0f;
        private float originalAngularDrag = 0f;
        
        // 잡은 위치의 상대적 오프셋 저장
        private Vector3 grabOffset;
        private bool hasGrabOffset = false;

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

        void OnGrabbed(Hand hand, Grabbable grab)
        {
            // 잡은 위치의 오프셋 계산 (손과 오브젝트 간의 상대적 위치)
            grabOffset = transform.position - hand.transform.position;
            hasGrabOffset = true;
            
            // 즉시 반응하도록 물리 설정 최적화
            if (rb != null)
            {
                rb.isKinematic = false;
                rb.useGravity = false;
                rb.drag = 20f;
                rb.angularDrag = 20f;
                rb.interpolation = RigidbodyInterpolation.Interpolate;
                rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
                
                // 현재 속도 초기화 (갑작스러운 움직임 방지)
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            // ObiRigidbody 설정
            if (obiRigidbody != null)
            {
                obiRigidbody.kinematicForParticles = false;
            }

            // Grabbable 설정 최적화 (붙지 않게)
            grabbable.instantGrab = false;
            grabbable.parentOnGrab = false;
            grabbable.maintainGrabOffset = true;
            
            // Transform 부모 관계 강제 해제 (trackingspace에 귀속 방지)
            transform.SetParent(null, true);
        }

        void OnReleased(Hand hand, Grabbable grab)
        {
            hasGrabOffset = false;
            
            // 물리 설정 복원을 지연시켜 TangledPeg의 슬롯 이동 로직이 먼저 실행되도록 함
            StartCoroutine(DelayedPhysicsRestore());

            // Grabbable 설정 즉시 복원
            grabbable.instantGrab = false;
            grabbable.parentOnGrab = true;
            grabbable.maintainGrabOffset = false;
        }

        private System.Collections.IEnumerator DelayedPhysicsRestore()
        {
            // 0.5초 대기하여 TangledPeg의 슬롯 이동 로직이 완료될 시간을 줌
            yield return new WaitForSeconds(0.5f);
            
            // 원래 물리 설정 복원
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

        void FixedUpdate()
        {
            // 잡힌 상태에서만 추가 처리
            if (grabbable.IsHeld() && rb != null && !rb.isKinematic && hasGrabOffset)
            {
                // Transform 부모 관계 지속적으로 해제 (trackingspace 귀속 방지)
                if (transform.parent != null)
                {
                    transform.SetParent(null, true);
                }
                
                var heldBy = grabbable.GetHeldBy();
                if (heldBy.Count > 0)
                {
                    var hand = heldBy[0];
                    // 잡은 위치의 오프셋을 유지하여 목표 위치 계산
                    Vector3 targetPosition = hand.transform.position + grabOffset;
                    Vector3 distanceVector = targetPosition - transform.position;
                    float distance = distanceVector.magnitude;

                    // 거리 기반 힘 적용 (더 부드럽게)
                    if (distance > 0.02f) // 0.01f에서 0.02f로 증가
                    {
                        // 거리에 따라 힘 조절 (더 점진적으로)
                        float forceFactor = Mathf.Clamp01(distance / maxDistanceFromHand);
                        forceFactor = Mathf.Pow(forceFactor, 2); // 제곱으로 더 부드럽게
                        
                        Vector3 force = distanceVector * forceMultiplier * rb.mass * forceFactor * 0.5f; // 힘 절반으로 감소
                        Vector3 dampingForce = -rb.velocity * damping;
                        
                        rb.AddForce(force + dampingForce, ForceMode.Force);
                        
                        // 속도 제한 (더 낮게)
                        if (rb.velocity.magnitude > 3f) // 5f에서 3f로 감소
                        {
                            rb.velocity = rb.velocity.normalized * 3f;
                        }
                    }

                    // 회전 제어 (옵션)
                    if (enableRotationControl)
                    {
                        // 부드러운 회전 (더 약하게)
                        float dotProduct = Vector3.Dot(transform.up, hand.transform.up);
                        if (dotProduct < 0.95f)
                        {
                            Vector3 torque = Vector3.Cross(transform.up, hand.transform.up) * 5f; // 10f에서 5f로 감소
                            rb.AddTorque(torque, ForceMode.Force);
                            
                            // 각속도 제한
                            if (rb.angularVelocity.magnitude > 3f) // 5f에서 3f로 감소
                            {
                                rb.angularVelocity = rb.angularVelocity.normalized * 3f;
                            }
                        }
                    }
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
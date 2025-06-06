using System.Collections;
using UnityEngine;

namespace Obi.Samples
{
    [RequireComponent(typeof(Rigidbody))]
    public class TangledPeg : MonoBehaviour
    {
        public TangledPegSlot currentSlot;
        public Collider floorCollider;
        public ObiRope attachedRope;

        [Header("Movement")]
        public float stiffness = 200;
        public float damping = 20;
        public float maxAccel = 50;
        public float minDistance = 0.05f;

        // Grabbable 호환성을 위한 변수들
        private bool isBeingGrabbed = false;
        private bool wasGrabbed = false;

        public Rigidbody rb { get; private set; }
        public ObiRigidbody orb { get; private set; }

        // Grabbable 컴포넌트 참조 (옵셔널)
        private Autohand.Grabbable grabbable;

        void Awake()
        {
            rb = GetComponent<Rigidbody>();
            orb = GetComponent<ObiRigidbody>();
            
            // Grabbable 컴포넌트가 있다면 참조 저장
            grabbable = GetComponent<Autohand.Grabbable>();

            // Ignore collisions with the floor:
            if (floorCollider != null)
                Physics.IgnoreCollision(GetComponent<Collider>(), floorCollider);

            // Initialize the peg's current slot, if any:
            if (currentSlot != null)
            {
                currentSlot.currentPeg = this;
                transform.position = currentSlot.transform.position;
            }
        }

        void Start()
        {
            // Grabbable 이벤트 연결
            if (grabbable != null)
            {
                grabbable.OnGrabEvent += OnGrabbed;
                grabbable.OnReleaseEvent += OnReleased;
                
                // Grabbable 설정 최적화
                OptimizeGrabbableSettings();
            }
        }

        void OptimizeGrabbableSettings()
        {
            if (grabbable != null)
            {
                // TanglePegGrabbableEnhancer가 있다면 그쪽에서 설정을 처리하도록 함
                var enhancer = GetComponent<TanglePegGrabbableEnhancer>();
                if (enhancer != null)
                {
                    // Enhancer가 모든 설정을 처리하므로 여기서는 기본 설정만
                    Debug.Log("TanglePegGrabbableEnhancer detected, using its settings");
                    return;
                }
                
                // Enhancer가 없을 때만 기본 설정 적용
                grabbable.instantGrab = false;
                grabbable.useGentleGrab = true;
                grabbable.maintainGrabOffset = false;
                grabbable.parentOnGrab = false;
                grabbable.throwPower = 0.2f;
                grabbable.jointBreakForce = 3000f;
                grabbable.minHeldDrag = 6f;
                grabbable.minHeldAngleDrag = 6f;
            }
        }

        void Update()
        {
            // Grabbable이 있다면 잡힘 상태 확인
            if (grabbable != null)
            {
                isBeingGrabbed = grabbable.IsHeld();
                
                // 잡힌 상태에서 안정화
                if (isBeingGrabbed && rb != null)
                {
                    // Transform 부모 관계 지속적으로 해제 (trackingspace 귀속 방지)
                    if (transform.parent != null)
                    {
                        transform.SetParent(null, true);
                    }
                    
                    // 과도한 회전 방지 (더 여유롭게)
                    if (rb.angularVelocity.magnitude > 15f)
                    {
                        rb.angularVelocity = rb.angularVelocity.normalized * 15f;
                    }
                    
                    // 과도한 속도 방지 (더 여유롭게)
                    if (rb.velocity.magnitude > 12f)
                    {
                        rb.velocity = rb.velocity.normalized * 12f;
                    }
                }
            }
        }

        // Grabbable에 의해 잡혔을 때 호출
        private void OnGrabbed(Autohand.Hand hand, Autohand.Grabbable grab)
        {
            isBeingGrabbed = true;
            wasGrabbed = true;
            
            // 현재 슬롯에서 분리
            UndockFromCurrentSlot();
            
            // 진행 중인 슬롯 이동 코루틴 중지
            StopAllCoroutines();
            
            // 물리 설정 최적화
            if (rb != null)
            {
                rb.interpolation = RigidbodyInterpolation.Interpolate;
                rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            }
            
            // Transform 부모 관계 강제 해제 (trackingspace에 귀속 방지)
            transform.SetParent(null, true);
        }

        // Grabbable에 의해 놓였을 때 호출
        private void OnReleased(Autohand.Hand hand, Autohand.Grabbable grab)
        {
            isBeingGrabbed = false;
            
            // 슬롯 이동을 위한 물리 설정 즉시 적용
            if (rb != null)
            {
                rb.isKinematic = false; // kinematic 해제하여 MoveTowards가 작동하도록
                rb.useGravity = false; // 중력 비활성화
                rb.interpolation = RigidbodyInterpolation.Interpolate; // 부드러운 이동
                rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
                rb.drag = 5f; // 적당한 드래그
                rb.angularDrag = 5f;
            }
            
            // 가장 가까운 슬롯 찾아서 이동
            StartCoroutine(FindAndMoveToNearestSlot());
        }

        // 가장 가까운 슬롯을 찾아 이동하는 코루틴
        private IEnumerator FindAndMoveToNearestSlot()
        {
            yield return new WaitForFixedUpdate(); // 한 프레임 대기

            TangledPegSlot nearestSlot = FindNearestAvailableSlot();
            
            // 가까운 슬롯이 있으면 이동, 없으면 원래 슬롯으로 복귀
            if (nearestSlot != null && Vector3.Distance(transform.position, nearestSlot.transform.position) < 5f) // 거리 제한 증가
            {
                Debug.Log($"Moving to nearest slot: {nearestSlot.name}");
                DockInSlot(nearestSlot);
            }
            else if (currentSlot != null)
            {
                Debug.Log($"Returning to current slot: {currentSlot.name}");
                // 가까운 슬롯이 없으면 원래 슬롯으로 복귀
                DockInSlot(currentSlot);
            }
            else
            {
                Debug.Log("No slot found, finding any available slot");
                // 아예 슬롯이 없으면 아무 슬롯이나 찾아서 이동
                TangledPegSlot anySlot = FindNearestAvailableSlot();
                if (anySlot != null)
                {
                    DockInSlot(anySlot);
                }
                else
                {
                    Debug.LogWarning("No available slots found for TanglePeg!");
                    // 마지막 수단으로 중력 활성화
                    if (rb != null)
                    {
                        rb.useGravity = true;
                    }
                }
            }
        }

        // 가장 가까운 사용 가능한 슬롯 찾기
        private TangledPegSlot FindNearestAvailableSlot()
        {
            TangledPegSlot[] allSlots = FindObjectsOfType<TangledPegSlot>();
            TangledPegSlot nearest = null;
            float nearestDistance = float.MaxValue;

            Debug.Log($"Found {allSlots.Length} total slots");

            foreach (TangledPegSlot slot in allSlots)
            {
                // 비어있는 슬롯이거나 자신이 원래 있던 슬롯인 경우
                if (slot.currentPeg == null || slot.currentPeg == this)
                {
                    float distance = Vector3.Distance(transform.position, slot.transform.position);
                    Debug.Log($"Slot {slot.name}: distance = {distance}, available = {slot.currentPeg == null}");
                    
                    if (distance < nearestDistance)
                    {
                        nearest = slot;
                        nearestDistance = distance;
                    }
                }
            }

            if (nearest != null)
            {
                Debug.Log($"Nearest available slot: {nearest.name} at distance {nearestDistance}");
            }
            else
            {
                Debug.Log("No available slots found");
            }

            return nearest;
        }

        public float MoveTowards(Vector3 position)
        {
            // 잡힌 상태에서는 스프링 시스템 사용하지 않음
            if (isBeingGrabbed)
                return Vector3.Distance(transform.position, position);

            Vector3 vector = position - transform.position;
            float distance = Vector3.Magnitude(vector);

            // simple damped spring: F = -kx - vu
            Vector3 accel = stiffness * vector - damping * rb.velocity;

            // clamp spring acceleration:
            accel = Vector3.ClampMagnitude(accel, maxAccel);

            rb.AddForce(accel, ForceMode.Acceleration);

            return distance;
        }

        public void DockInSlot(TangledPegSlot slot)
        {
            // 잡힌 상태에서는 슬롯 이동하지 않음
            if (isBeingGrabbed)
                return;
                
            StopAllCoroutines();
            StartCoroutine(MoveTowardsSlot(slot));
        }

        public void UndockFromCurrentSlot()
        {
            if (currentSlot != null)
            {
                currentSlot.currentPeg = null;
                rb.isKinematic = false;
                
                // Grabbable과 호환을 위해 kinematicForParticles도 해제
                if (orb != null)
                    orb.kinematicForParticles = false;
            }
        }

        private IEnumerator MoveTowardsSlot(TangledPegSlot slot)
        {
            // 잡힌 상태에서는 슬롯 이동 중지
            if (isBeingGrabbed)
                yield break;

            float distance = float.MaxValue;
            if (orb != null)
                orb.kinematicForParticles = true;

            while (distance > minDistance && !isBeingGrabbed)
            {
                distance = MoveTowards(slot.transform.position);
                yield return new WaitForFixedUpdate();
            }

            // 이동 완료 후 슬롯에 고정 (잡힌 상태가 아닐 때만)
            if (!isBeingGrabbed)
            {
                currentSlot = slot;
                currentSlot.currentPeg = this;
                transform.position = currentSlot.transform.position;
                rb.isKinematic = true;
                if (orb != null)
                    orb.kinematicForParticles = false;
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
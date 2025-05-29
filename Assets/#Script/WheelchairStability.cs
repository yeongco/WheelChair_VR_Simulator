using UnityEngine;

public class WheelchairStability : MonoBehaviour
{
    [Header("휠체어 본체 설정")]
    [SerializeField] private float mass = 100f;
    [SerializeField] private float drag = 1.5f;
    [SerializeField] private float angularDrag = 1.5f;
    [SerializeField] private Vector3 centerOfMass = new Vector3(0, -0.8f, 1.15f); // 무게중심을 더 낮게
    
    [Header("바퀴 설정")]
    [SerializeField] private float wheelMass = 25f;
    [SerializeField] private float wheelDrag = 0.5f;
    [SerializeField] private float wheelAngularDrag = 1.5f;
    [SerializeField] private float maxWheelAngle = 30f;
    
    [Header("바퀴 들림 방지")]
    [SerializeField] private float wheelLiftPrevention = 10000f; // 바퀴 들림 방지 힘
    [SerializeField] private float maxWheelLiftHeight = 0.03f; // 최대 바퀴 들림 높이
    [SerializeField] private float groundCheckDistance = 0.15f;
    [SerializeField] private LayerMask groundLayer = 1;
    
    private Rigidbody wheelchairBody;
    private Rigidbody[] wheels;
    private HingeJoint[] wheelJoints;
    private Vector3[] originalWheelPositions;

    void Start()
    {
        // 휠체어 본체 설정
        wheelchairBody = GetComponent<Rigidbody>();
        if (wheelchairBody != null)
        {
            wheelchairBody.mass = mass;
            wheelchairBody.drag = drag;
            wheelchairBody.angularDrag = angularDrag;
            wheelchairBody.centerOfMass = centerOfMass;
            
            // 회전 제한 설정 (X축과 Z축 회전만 제한)
            wheelchairBody.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            
            // 각속도 제한으로 과도한 회전 방지
            wheelchairBody.maxAngularVelocity = 3f;
        }

        // 바퀴 설정
        wheels = GetComponentsInChildren<Rigidbody>();
        wheelJoints = GetComponentsInChildren<HingeJoint>();
        
        // 원래 바퀴 위치 저장
        originalWheelPositions = new Vector3[wheels.Length];
        
        for (int i = 0; i < wheels.Length; i++)
        {
            if (wheels[i].gameObject.name.Contains("Wheel"))
            {
                wheels[i].mass = wheelMass;
                wheels[i].drag = wheelDrag;
                wheels[i].angularDrag = wheelAngularDrag;
                
                // 바퀴의 Y축 움직임을 제한하는 조인트 설정
                SetupWheelPositionConstraints(wheels[i]);
                
                // 원래 위치 저장
                originalWheelPositions[i] = wheels[i].transform.localPosition;
            }
        }

        foreach (HingeJoint joint in wheelJoints)
        {
            if (joint.gameObject.name.Contains("Wheel"))
            {
                joint.useLimits = true;
                JointLimits limits = new JointLimits();
                limits.min = -maxWheelAngle;
                limits.max = maxWheelAngle;
                limits.bounciness = 0;
                limits.bounceMinVelocity = 0.2f;
                limits.contactDistance = 0;
                joint.limits = limits;

                // 스프링 설정으로 바퀴 움직임 안정화
                joint.useSpring = true;
                JointSpring spring = new JointSpring();
                spring.spring = 10f; // 더 강한 스프링
                spring.damper = 3f;   // 더 강한 댐퍼
                spring.targetPosition = 0;
                joint.spring = spring;
            }
        }
    }

    void SetupWheelPositionConstraints(Rigidbody wheelRb)
    {
        // 바퀴에 ConfigurableJoint 추가하여 위치 제한
        ConfigurableJoint posConstraint = wheelRb.gameObject.AddComponent<ConfigurableJoint>();
        posConstraint.connectedBody = wheelchairBody;
        
        // X, Z축 위치 고정, Y축은 제한적으로 허용
        posConstraint.xMotion = ConfigurableJointMotion.Locked;
        posConstraint.yMotion = ConfigurableJointMotion.Limited;
        posConstraint.zMotion = ConfigurableJointMotion.Locked;
        
        // 회전은 Y축만 허용 (바퀴 굴림)
        posConstraint.angularXMotion = ConfigurableJointMotion.Locked;
        posConstraint.angularYMotion = ConfigurableJointMotion.Free;
        posConstraint.angularZMotion = ConfigurableJointMotion.Locked;
        
        // Y축 위치 제한 설정
        SoftJointLimit yLimit = new SoftJointLimit();
        yLimit.limit = maxWheelLiftHeight;
        yLimit.bounciness = 0f;
        yLimit.contactDistance = 0.01f;
        posConstraint.linearLimit = yLimit;
        
        // 강한 Y축 위치 드라이브 설정
        JointDrive yDrive = new JointDrive();
        yDrive.positionSpring = wheelLiftPrevention;
        yDrive.positionDamper = wheelLiftPrevention * 0.1f;
        yDrive.maximumForce = float.MaxValue;
        posConstraint.yDrive = yDrive;
    }

    void FixedUpdate()
    {
        // 바퀴 지면 체크 및 강제 고정
        CheckAndFixWheelPositions();
        
        // 휠체어가 너무 기울어지면 자동으로 복원
        float currentTilt = Vector3.Angle(transform.up, Vector3.up);
        if (currentTilt > 30f) // 30도 이상 기울어지면
        {
            Vector3 targetRotation = Quaternion.LookRotation(transform.forward, Vector3.up).eulerAngles;
            transform.rotation = Quaternion.Slerp(transform.rotation, 
                Quaternion.Euler(targetRotation), 
                Time.deltaTime * 3f);
        }
        
        // 과도한 상하 움직임 방지
        if (wheelchairBody.velocity.y > 2f)
        {
            Vector3 vel = wheelchairBody.velocity;
            vel.y = Mathf.Min(vel.y, 2f);
            wheelchairBody.velocity = vel;
        }
    }
    
    void CheckAndFixWheelPositions()
    {
        for (int i = 0; i < wheels.Length; i++)
        {
            if (wheels[i] == null || !wheels[i].gameObject.name.Contains("Wheel")) continue;
            
            // 지면 체크
            RaycastHit hit;
            Vector3 rayStart = wheels[i].transform.position + Vector3.up * 0.1f;
            bool isGrounded = Physics.Raycast(rayStart, Vector3.down, out hit, groundCheckDistance, groundLayer);
            
            // 바퀴가 원래 위치에서 너무 멀어졌으면 강제로 당기기
            Vector3 currentLocalPos = wheels[i].transform.localPosition;
            Vector3 originalPos = originalWheelPositions[i];
            
            if (Vector3.Distance(currentLocalPos, originalPos) > maxWheelLiftHeight)
            {
                // 바퀴를 원래 위치로 당기는 힘 적용
                Vector3 correctionForce = (originalPos - currentLocalPos) * wheelLiftPrevention;
                wheels[i].AddForce(transform.TransformDirection(correctionForce), ForceMode.Force);
            }
            
            // 바퀴가 지면에서 떨어졌으면 아래로 당기기
            if (!isGrounded)
            {
                wheels[i].AddForce(Vector3.down * wheelLiftPrevention * 0.5f, ForceMode.Force);
            }
        }
    }
    
    // 바퀴가 잡혔을 때 호출할 수 있는 메서드
    public void OnWheelGrabbed(Transform wheelTransform)
    {
        Rigidbody wheelRb = wheelTransform.GetComponent<Rigidbody>();
        if (wheelRb != null)
        {
            // 바퀴가 잡혔을 때 Y축 움직임을 더 강하게 제한
            wheelRb.constraints = RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        }
    }
    
    // 바퀴가 놓였을 때 호출할 수 있는 메서드
    public void OnWheelReleased(Transform wheelTransform)
    {
        Rigidbody wheelRb = wheelTransform.GetComponent<Rigidbody>();
        if (wheelRb != null)
        {
            // 원래 제약 조건으로 복원
            wheelRb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        }
    }
} 
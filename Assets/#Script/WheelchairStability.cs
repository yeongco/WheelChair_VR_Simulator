using UnityEngine;

public class WheelchairStability : MonoBehaviour
{
    [Header("휠체어 본체 설정")]
    [SerializeField] private float mass = 100f;
    [SerializeField] private float drag = 1.5f;
    [SerializeField] private float angularDrag = 1.5f;
    [SerializeField] private Vector3 centerOfMass = new Vector3(0, 0, 1.15f);
    
    [Header("바퀴 설정")]
    [SerializeField] private float wheelMass = 25f;
    [SerializeField] private float wheelDrag = 0.5f;
    [SerializeField] private float wheelAngularDrag = 1.5f;
    [SerializeField] private float maxWheelAngle = 30f;
    
    private Rigidbody wheelchairBody;
    private Rigidbody[] wheels;
    private HingeJoint[] wheelJoints;

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
        }

        // 바퀴 설정
        wheels = GetComponentsInChildren<Rigidbody>();
        wheelJoints = GetComponentsInChildren<HingeJoint>();
        
        foreach (Rigidbody wheel in wheels)
        {
            if (wheel.gameObject.name.Contains("Wheel"))
            {
                wheel.mass = wheelMass;
                wheel.drag = wheelDrag;
                wheel.angularDrag = wheelAngularDrag;
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
                spring.spring = 3f;
                spring.damper = 1f;
                spring.targetPosition = 0;
                joint.spring = spring;
            }
        }
    }

    void Update()
    {
        // 휠체어가 너무 기울어지면 자동으로 복원
        float currentTilt = Vector3.Angle(transform.up, Vector3.up);
        if (currentTilt > 45f)
        {
            Vector3 targetRotation = Quaternion.LookRotation(transform.forward, Vector3.up).eulerAngles;
            transform.rotation = Quaternion.Slerp(transform.rotation, 
                Quaternion.Euler(targetRotation), 
                Time.deltaTime * 2f);
        }
    }
} 
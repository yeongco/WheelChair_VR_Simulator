using UnityEngine;

public class WheelchairSpinManager : MonoBehaviour
{
    [Header("Wheel References")]
    [SerializeField] private WheelchairWheel leftWheel;
    [SerializeField] private WheelchairWheel rightWheel;

    [Header("Movement Settings")]
    [SerializeField] private float forceMultiplier = 10f;
    [SerializeField] private float torqueMultiplier = 5f;

    private Rigidbody chairRigidbody;

    void Start()
    {
        chairRigidbody = GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        ApplyMovement();
        ApplyRotation();
    }

    private void ApplyMovement()
    {
        // 양 바퀴가 같은 방향으로 돌면 앞으로
        float leftSpeed = leftWheel.GetLocalZRotationDelta();
        float rightSpeed = rightWheel.GetLocalZRotationDelta();

        float forwardMovement = (leftSpeed + rightSpeed) * 0.5f;
        Vector3 force = transform.forward * forwardMovement * forceMultiplier;
        chairRigidbody.AddForce(force, ForceMode.Force);
    }

    private void ApplyRotation()
    {
        // 양 바퀴가 반대 방향으로 돌면 회전
        float leftSpeed = leftWheel.GetLocalZRotationDelta();
        float rightSpeed = rightWheel.GetLocalZRotationDelta();

        float turnMovement = (leftSpeed - rightSpeed) * 0.5f;
        Vector3 torque = Vector3.up * turnMovement * torqueMultiplier;
        chairRigidbody.AddTorque(torque, ForceMode.Force);
    }
}

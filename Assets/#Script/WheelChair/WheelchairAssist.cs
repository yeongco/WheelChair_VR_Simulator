using Unity;
using UnityEngine;

public class WheelchairDrive : MonoBehaviour
{
    public Rigidbody leftWheel;
    public Rigidbody rightWheel;
    private Rigidbody wheelchair;
    public float forwardForceMultiplier = 50f;
    public float turnTorqueMultiplier = 30f;
    private void Awake()
    {
        wheelchair = GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        // 회전 속도 측정 (축은 바퀴 모델에 따라 x/y/z 중 확인 필요)
        float leftSpin = leftWheel.angularVelocity.x;
        float rightSpin = rightWheel.angularVelocity.x;

        // 전진 힘 (양쪽 회전 평균)
        float forwardSpin = (leftSpin + rightSpin) / 2f;
        Vector3 forwardDir = wheelchair.transform.forward;
        wheelchair.AddForce(forwardDir * forwardSpin * forwardForceMultiplier);

        // 회전 힘 (양쪽 회전 차이 → y축 torque)
        float spinDiff = rightSpin - leftSpin;
        Vector3 turnTorque = Vector3.up * spinDiff * turnTorqueMultiplier;
        wheelchair.AddTorque(turnTorque);
    }
}

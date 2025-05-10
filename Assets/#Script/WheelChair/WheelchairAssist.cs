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
        // ȸ�� �ӵ� ���� (���� ���� �𵨿� ���� x/y/z �� Ȯ�� �ʿ�)
        float leftSpin = leftWheel.angularVelocity.x;
        float rightSpin = rightWheel.angularVelocity.x;

        // ���� �� (���� ȸ�� ���)
        float forwardSpin = (leftSpin + rightSpin) / 2f;
        Vector3 forwardDir = wheelchair.transform.forward;
        wheelchair.AddForce(forwardDir * forwardSpin * forwardForceMultiplier);

        // ȸ�� �� (���� ȸ�� ���� �� y�� torque)
        float spinDiff = rightSpin - leftSpin;
        Vector3 turnTorque = Vector3.up * spinDiff * turnTorqueMultiplier;
        wheelchair.AddTorque(turnTorque);
    }
}

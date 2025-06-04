using UnityEngine;

public class CasterWheelSteer : MonoBehaviour
{
    [SerializeField] private Rigidbody chairRigidbody;
    [SerializeField] private WheelCollider frontWheel;

    void Awake()
    {
        frontWheel = GetComponent<WheelCollider>();
    }
    void FixedUpdate()
    {
        Vector3 velocity = chairRigidbody.velocity;
        velocity.y = 0f; // 수평면만 본다

        if (velocity.sqrMagnitude > 0.01f)  // 움직일 때만
        {
            float targetAngle = Mathf.Atan2(velocity.x, velocity.z) * Mathf.Rad2Deg;
            float currentAngle = frontWheel.steerAngle;
            float newAngle = Mathf.MoveTowards(currentAngle, targetAngle, 720f * Time.fixedDeltaTime);
            frontWheel.steerAngle = newAngle;
        }
    }
}

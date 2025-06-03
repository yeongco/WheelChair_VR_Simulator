using UnityEngine;

public class WheelchairSpinManager : MonoBehaviour
{
    [Header("Wheel References")]
    [SerializeField] private WheelchairWheel leftWheel;
    [SerializeField] private WheelchairWheel rightWheel;

    [Header("Spin Settings")]
    [SerializeField] private float minSpinThreshold = 5f;
    [SerializeField] private float maxSpinForce = 10f;
    [SerializeField] private float maxForwardForce = 10f;
    [SerializeField] private float spinSmoothing = 0.1f;

    private Rigidbody chairRigidbody;
    private float currentSpinForce = 0f;
    private float currentForward = 0f;

    void Start()
    {
        chairRigidbody = GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        TurnleftRight();
        GoStraight();
    }

    public float GetCurrentSpinForce()
    {
        return currentSpinForce;
    }

    public void TurnleftRight()
    {
        // 양쪽 바퀴의 회전력 합산
        float totalRotationForce = leftWheel.GetCurrentRotationForce() + rightWheel.GetCurrentRotationForce();

        // 회전 임계값 체크
        if (Mathf.Abs(totalRotationForce) >= minSpinThreshold)
        {
            // 회전력 제한
            totalRotationForce = Mathf.Clamp(totalRotationForce, -maxSpinForce, maxSpinForce);
            
            // 부드러운 회전을 위한 보간
            currentSpinForce = Mathf.Lerp(currentSpinForce, totalRotationForce, spinSmoothing);
            
            // Y축 회전 적용
            transform.Rotate(Vector3.up * currentSpinForce * Time.fixedDeltaTime);
        }
        else
        {
            // 임계값 미만일 경우 회전력 감소
            currentSpinForce = Mathf.Lerp(currentSpinForce, 0f, spinSmoothing);
        }
    }

    public void GoStraight()
    {
        // 양쪽 바퀴의 회전력 합산
        float totalForwardForce = (leftWheel.GetCurrentRotationForce() - rightWheel.GetCurrentRotationForce());
        Debug.Log(totalForwardForce);

        // 전진 임계값 체크
        if (Mathf.Abs(totalForwardForce) >= minSpinThreshold)
        {
            // 전진력 제한
            totalForwardForce = Mathf.Clamp(totalForwardForce, -maxForwardForce, maxForwardForce);
            
            // 부드러운 전진을 위한 보간
            currentForward = Mathf.Lerp(currentForward, totalForwardForce, spinSmoothing);
            
            // 의자에 전진력 적용 (질량을 고려한 힘)
            chairRigidbody.AddForce(transform.forward * currentForward * chairRigidbody.mass, ForceMode.Force);
            // 또는
            // chairRigidbody.AddForce(transform.forward * currentForward, ForceMode.Acceleration);
        }
        else
        {
            // 임계값 미만일 경우 전진력 감소
            currentForward = Mathf.Lerp(currentForward, 0f, spinSmoothing);
        }
    }
} 
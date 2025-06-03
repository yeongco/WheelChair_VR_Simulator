using UnityEngine;

public class WheelchairFrontSteer : MonoBehaviour
{
    [Header("앞바퀴 WheelCollider")]
    public WheelCollider leftFrontWheel;
    public WheelCollider rightFrontWheel;

    [Header("조향 관련 설정")]
    public float maxSteerAngle = 45f; // 최대 조향각

    [Header("SpinManager 참조")]
    public WheelchairSpinManager spinManager;

    void FixedUpdate()
    {
        // SpinManager에서 현재 회전력(좌/우회전 시도값) 받아오기
        float spinForce = spinManager.GetCurrentSpinForce();
        Debug.Log(spinForce);

        // 회전력에 따라 조향각 결정 (좌/우회전 시 자연스럽게)
        float steer = Mathf.Clamp(spinForce, -1f, 1f) * maxSteerAngle;

        leftFrontWheel.steerAngle = steer;
        rightFrontWheel.steerAngle = steer;
    }
}
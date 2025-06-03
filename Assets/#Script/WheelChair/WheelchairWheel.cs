using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WheelchairWheel : MonoBehaviour
{
    [Header("Wheel Settings")]
    [SerializeField] private bool isLeftWheel = false;
    [SerializeField] private float rotationForce = 1f;
    
    private float currentRotationForce = 0f;
    private float lastLocalZRotation = 0f;
    private float deltaZRotation = 0f;
    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        lastLocalZRotation = transform.localEulerAngles.z;
    }

    void FixedUpdate()
    {
        // 현재 로컬 Z 회전값
        float currentLocalZRotation = transform.localEulerAngles.z;
        
        // Z 회전 변화량 계산 (각도 차이)
        deltaZRotation = Mathf.DeltaAngle(lastLocalZRotation, currentLocalZRotation);
        
        // 회전력 계산 (변화량에 비례)
        currentRotationForce = deltaZRotation * rotationForce;
        
        // 왼쪽 바퀴는 반대 방향으로 회전
        if(isLeftWheel) currentRotationForce = -currentRotationForce;
        
        // 현재 회전값 저장
        lastLocalZRotation = currentLocalZRotation;
    }

    public float GetCurrentRotationForce()
    {
        return currentRotationForce;
    }

    public float GetDeltaZRotation()
    {
        return deltaZRotation;
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RotationLimit : MonoBehaviour
{
    [SerializeField, Range(0f, 90f)]
    private float maxRotationAngle = 5f;  // 기본값 5도로 설정

    void FixedUpdate()
    {
        Vector3 rot = transform.eulerAngles;
        // x축 회전 각도가 maxRotationAngle 이상(뒤로 넘어가는 각도)이면 maxRotationAngle로 고정
        // Unity의 EulerAngles는 0~360도로 표현되므로, (360-maxRotationAngle)~360도(즉, -maxRotationAngle도)도 체크
        if (rot.x > maxRotationAngle && rot.x < 180f)
        {
            rot.x = maxRotationAngle;
            transform.eulerAngles = rot;
        }
        else if (rot.x > 180f && rot.x < (360f - maxRotationAngle))
        {
            rot.x = 360f - maxRotationAngle;
            transform.eulerAngles = rot;
        }
    }
}

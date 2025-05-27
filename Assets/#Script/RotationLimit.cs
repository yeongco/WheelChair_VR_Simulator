using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RotationLimit : MonoBehaviour
{
    void FixedUpdate()
    {
        Vector3 rot = transform.eulerAngles;
        // x축 회전 각도가 30도 이상(뒤로 넘어가는 각도)이면 30도로 고정
        // Unity의 EulerAngles는 0~360도로 표현되므로, 330~360도(즉, -30도)도 체크
        if (rot.x > 30f && rot.x < 180f)
        {
            rot.x = 30f;
            transform.eulerAngles = rot;
        }
        else if (rot.x > 180f && rot.x < 330f)
        {
            rot.x = 330f;
            transform.eulerAngles = rot;
        }
    }
}

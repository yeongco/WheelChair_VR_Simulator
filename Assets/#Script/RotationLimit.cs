using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RotationLimit : MonoBehaviour
{
    void FixedUpdate()
    {
        Vector3 rot = transform.eulerAngles;
        // x축 회전 각도가 10도 이상(뒤로 넘어가는 각도)이면 10도로 고정
        // Unity의 EulerAngles는 0~360도로 표현되므로, 350~360도(즉, -10도)도 체크
        if (rot.x > 10f && rot.x < 180f)
        {
            rot.x = 10f;
            transform.eulerAngles = rot;
        }
        else if (rot.x > 180f && rot.x < 350f)
        {
            rot.x = 350f;
            transform.eulerAngles = rot;
        }
    }
}

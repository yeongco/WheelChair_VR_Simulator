using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RotationLimit : MonoBehaviour
{
    void FixedUpdate()
    {
        Vector3 rot = transform.eulerAngles;
        // x축 회전 각도가 15도 이상(뒤로 넘어가는 각도)이면 15도로 고정
        // Unity의 EulerAngles는 0~360도로 표현되므로, 345~360도(즉, -15도)도 체크
        if (rot.x > 15f && rot.x < 180f)
        {
            rot.x = 15f;
            transform.eulerAngles = rot;
        }
        else if (rot.x > 180f && rot.x < 345f)
        {
            rot.x = 345f;
            transform.eulerAngles = rot;
        }
        if(rot.z > 2f && rot.z < 180f)
        {
            rot.z = 2f;
            transform.eulerAngles = rot;
        }
        else if(rot.z > 180f && rot.z < 358f)
        {
            rot.z = 358f;
            transform.eulerAngles = rot;
        }
    }
}
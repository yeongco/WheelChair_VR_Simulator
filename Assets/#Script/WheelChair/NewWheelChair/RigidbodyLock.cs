using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RigidbodyLock : MonoBehaviour
{
    private Rigidbody rb;

    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogError("Rigidbody 컴포넌트가 없습니다!");
        }
    }

    // 모든 회전 잠금
    public void LockAllRotation()
    {
        if (rb != null)
        {
            rb.constraints = RigidbodyConstraints.FreezeRotation;
        }
    }

    // 모든 회전 잠금 해제
    public void UnlockAllRotation()
    {
        if (rb != null)
        {
            rb.constraints = RigidbodyConstraints.None;
        }
    }
}

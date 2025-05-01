using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EndlessCorridorController : MonoBehaviour
{
    [SerializeField] private EndlessCorridor corridor;
    [SerializeField] private bool useDOTween = false;

    private void Start()
    {
        if (corridor == null)
        {
            corridor = GetComponent<EndlessCorridor>();
        }

        if (corridor == null)
        {
            Debug.LogError("EndlessCorridor 컴포넌트를 찾을 수 없습니다!");
            return;
        }

        if (useDOTween)
        {
            corridor.StartCorridorMovementWithDOTween();
        }
    }
} 
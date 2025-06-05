using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IgnoreHandWheel : MonoBehaviour
{
    [SerializeField] private GameObject[] ignoreObjects;

    private void Start()
    {
        // 현재 오브젝트의 모든 콜라이더 가져오기
        Collider[] currentColliders = GetComponents<Collider>();
        
        foreach (GameObject ignoreObj in ignoreObjects)
        {
            // 대상 오브젝트의 모든 콜라이더 가져오기 (자식 포함)
            Collider[] targetColliders = ignoreObj.GetComponentsInChildren<Collider>();
            
            // 각 콜라이더 쌍에 대해 충돌 무시 설정
            foreach (Collider currentCollider in currentColliders)
            {
                foreach (Collider targetCollider in targetColliders)
                {
                    // 물리적 충돌만 무시하고 트리거는 유지
                    if (!currentCollider.isTrigger && !targetCollider.isTrigger)
                    {
                        Physics.IgnoreCollision(currentCollider, targetCollider, true);
                    }
                }
            }
        }
    }
}

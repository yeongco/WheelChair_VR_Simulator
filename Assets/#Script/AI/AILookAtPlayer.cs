using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations.Rigging;

public class AILookAtPlayer : MonoBehaviour
{
    public Transform player; // 플레이어 Transform
    public MultiAimConstraint spineAimConstraint; // 상체용
    public MultiAimConstraint headAimConstraint; // 머리용
    float lookDistance = 3f; // 몇 m 이내로 오면 쳐다볼지
    public float weightSpeed = 2f; // Weight 변화 속도
    
    private float targetWeight = 0f;

    private void Awake()
    {
        //aimConstraint = GetComponent<MultiAimConstraint>();
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    void Update()
    {
        float distance = Vector3.Distance(player.position, transform.position);

        float targetWeight = distance < lookDistance ? 1f : 0f;

        // 둘 다 부드럽게 변화
        spineAimConstraint.weight = Mathf.Lerp(spineAimConstraint.weight, targetWeight * 0.45f, Time.deltaTime * weightSpeed); // 상체는 40%만
        headAimConstraint.weight = Mathf.Lerp(headAimConstraint.weight, targetWeight, Time.deltaTime * weightSpeed); // 머리는 100%
        }
}

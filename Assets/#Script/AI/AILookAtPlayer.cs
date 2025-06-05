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
    
    //[Header("Vertical Look Settings")]
    private float headHeight = 1.4f; // NPC 머리 높이
    private float playerHeadHeight = 1.5f; // 플레이어 머리 높이 (또는 카메라 높이)
    
    private float targetWeight = 0f;
    
    [Header("Sound Settings")]
    public AudioSource audioSource;
    public AudioClip soundClip;
    float soundTriggerDistance = 2.5f;
    
    private bool hasPlayedSound = false;
    
    // 가상의 룩앳 타겟 (높이 조정용)
    private GameObject lookAtTarget;
    
    // Animation 충돌 방지를 위한 변수들
    private Animator animator;
    private RigBuilder rigBuilder;

    private void Awake()
    {
        //aimConstraint = GetComponent<MultiAimConstraint>();
        player = EventsManager.instance.player.GetComponent<Transform>();
        audioSource = GetComponent<AudioSource>();
        animator = GetComponent<Animator>();
        rigBuilder = GetComponent<RigBuilder>();

        // 가상의 룩앳 타겟 생성 (플레이어 머리 높이에 맞춤)
        CreateLookAtTarget();

        // spine에 플레이어 설정 (40%)
        var spineSources = new WeightedTransformArray();
        spineSources.Add(new WeightedTransform(player, 1f));
        spineAimConstraint.data.sourceObjects = spineSources;

        // head에도 플레이어 설정 (100%)
        var headSources = new WeightedTransformArray();
        headSources.Add(new WeightedTransform(player, 1f));
        headAimConstraint.data.sourceObjects = headSources;
    }
    
    void CreateLookAtTarget()
    {
        // 빈 게임오브젝트 생성
        lookAtTarget = new GameObject("LookAtTarget_" + gameObject.name);
        
        // 플레이어를 따라다니도록 설정하지만 높이는 조정
        lookAtTarget.transform.SetParent(player);
        lookAtTarget.transform.localPosition = new Vector3(0, playerHeadHeight, 0);
    }

    // Start is called before the first frame update
    void Start()
    {
        // Rig가 Animator보다 우선순위를 갖도록 설정
        var rigBuilder = GetComponent<RigBuilder>();
        if (rigBuilder != null)
        {
            rigBuilder.Build();
        }
        
        // 애니메이션 레이어 가중치 조정 (만약 Rig Layer가 있다면)
        if (animator != null && animator.layerCount > 1)
        {
            // 두 번째 레이어(Rig Layer)의 가중치를 높게 설정
            animator.SetLayerWeight(1, 1.0f);
        }
    }

    void Update()
    {
        float distance = Vector3.Distance(player.position, transform.position);

        // 고개 돌리기
        targetWeight = distance < lookDistance ? 1f : 0f;
        
        // 룩앳 타겟 위치 업데이트 (플레이어 위치 + 머리 높이)
        if (lookAtTarget != null)
        {
            Vector3 targetPos = player.position;
            targetPos.y += playerHeadHeight; // 플레이어 머리 높이로 조정
            lookAtTarget.transform.position = targetPos;
        }
        
        // 애니메이션 충돌 방지: Rig가 활성화될 때 관련 애니메이션 파라미터 비활성화
        if (targetWeight > 0.1f)
        {
            DisableConflictingAnimations();
        }


        // 둘 다 부드럽게 변화
        spineAimConstraint.weight = Mathf.Lerp(spineAimConstraint.weight, targetWeight * 0.45f, Time.deltaTime * weightSpeed); // 상체는 40%만
        headAimConstraint.weight = Mathf.Lerp(headAimConstraint.weight, targetWeight, Time.deltaTime * weightSpeed); // 머리는 100%
        
        // 효과음 재생 조건 (한 번만)
        if (distance < soundTriggerDistance && !hasPlayedSound)
        {
            if (audioSource != null && soundClip != null)
            {
                hasPlayedSound = true;
                audioSource.PlayOneShot(soundClip);
            }
        }
    }
    
    // 충돌하는 애니메이션 파라미터들을 비활성화
    void DisableConflictingAnimations()
    {
        if (animator == null) return;
        
        // 일반적인 목/머리 회전 관련 파라미터들 비활성화
        string[] neckParameters = { "LookAt", "HeadTurn", "NeckRotation", "HeadWeight", "LookAtWeight" };
        
        foreach (string param in neckParameters)
        {
            int paramIndex = GetParameterIndex(param);
            if (paramIndex >= 0)
            {
                if (animator.GetParameter(paramIndex).type == AnimatorControllerParameterType.Bool)
                    animator.SetBool(param, false);
                else if (animator.GetParameter(paramIndex).type == AnimatorControllerParameterType.Float)
                    animator.SetFloat(param, 0f);
            }
        }
    }
    
    bool HasParameter(string paramName)
    {
        if (animator == null) return false;
        
        foreach (AnimatorControllerParameter param in animator.parameters)
        {
            if (param.name == paramName)
                return true;
        }
        return false;
    }
    
    // 파라미터 인덱스를 반환하는 헬퍼 함수
    int GetParameterIndex(string paramName)
    {
        if (animator == null) return -1;
        
        for (int i = 0; i < animator.parameters.Length; i++)
        {
            if (animator.parameters[i].name == paramName)
                return i;
        }
        return -1;
    }
    
    private void OnDestroy()
    {
        // 생성한 가상 타겟 정리
        if (lookAtTarget != null)
        {
            DestroyImmediate(lookAtTarget);
        }
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;
public enum AIType
{
    Walking, sitting, standing
}
public class AIContoller : MonoBehaviour
{
    // 나중에 걷는 것 말고 다른 상태를 추가할 것을 고려해서 FSM 구조로 짰습니다.
    #region AI State
    private enum AIState { Idle, Walking }
    private AIState currentState = AIState.Idle;
    #endregion

    [Header("AI Settings")]
    public AIType aiType = AIType.Walking;
    
    [Space(10)]
    [Header("Standing option")]
    public bool withPhone;
    public bool withMusic;
    public bool talk1;
    public bool talk2;
    public bool wall;
    
    [Space(10)]
    [Header("Sitting option")]
    public bool talking1;
    public bool talking2;
    public bool shakingLeg;
    public bool rubbing;
    public bool girl;
    public bool simple;
    
    [Space(10)]
    [Header("Moving option")]
    public GameObject target;    // AI가 향할 목표 오브젝트 (일단 inpector 상에서 설정할 수 있게 임시 지정)

    private NavMeshAgent agent;
    private Animator animator;
    
    // Rig 제어를 위한 참조
    private AILookAtPlayer lookAtPlayer;
    
    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        lookAtPlayer = GetComponent<AILookAtPlayer>();
    }
    private void Start()
    {
        if (aiType == AIType.Walking)
            SetTarget(target.transform.position);
        if (aiType == AIType.sitting)
            SetSittingAnimation();
        if (aiType == AIType.standing)
            SetStandingAnimation();
    }


    private void Update()
    {
        switch (currentState)
        {
            case AIState.Idle:
                if(aiType == AIType.Walking)
                  StopMovement();
                break;
            case AIState.Walking:
                CheckGoal();
                break;
        }
    }

    void CheckGoal()
    {
        // 목적지 도달 여부 확인
        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.05f)
        {
            // 목적지 도달 시 idle 상태로 전환
            StopMovement();
            Debug.Log("목적지 도달");
            return;
        }
    }

    public void SetTarget(Vector3 position)
    {
        agent.isStopped = false;
        agent.SetDestination(position);
        if (withPhone)
        {
            animator.SetBool("phone", false);
        }
        animator.SetBool("walking", true);
        currentState = AIState.Walking;
    }

    void StopMovement()
    {
        if (aiType == AIType.Walking)
        {
            agent.isStopped = true;
            animator.SetBool("walking", false);
            if (withPhone)
            {
                animator.SetBool("phone", true);
            }
        }
        currentState = AIState.Idle;
    }

    void SetSittingAnimation()
    {
        if (shakingLeg)
        {
            animator.SetBool("shaking", true);
        } else if (talking1)
        {
            animator.SetBool("talk1", true);
        } else if (talking2)
        {
            animator.SetBool("talk2", true);
        } else if (rubbing)
        {
            animator.SetBool("rubbing", true);
        } else if (girl)
        {
            animator.SetBool("girl", true);
        }
        else
        {
            animator.SetBool("simple", true);
        }
    }
    
    void SetStandingAnimation()
    {
        if (withMusic)
        {
            animator.SetBool("music", true);
        } else if (withPhone)
        {
            animator.SetBool("phone", true);
        } else if (talk1)
        {
            animator.SetBool("talk1", true);
        } else if (talk2)
        {
            animator.SetBool("talk2", true);
        } else if (wall)
        {
            animator.SetBool("wall", true);
        }
    }
    
    // 목 회전에 영향을 주는 애니메이션 파라미터들을 비활성화
    void DisableNeckAnimations()
    {
        // 만약 애니메이션에 LookAt이나 HeadTurn 같은 파라미터가 있다면 비활성화
        if (HasParameter("LookAt"))
            animator.SetBool("LookAt", false);
        if (HasParameter("HeadTurn"))
            animator.SetFloat("HeadTurn", 0f);
        if (HasParameter("NeckRotation"))
            animator.SetFloat("NeckRotation", 0f);
            
        // 필요하다면 여기에 다른 목/머리 관련 파라미터들 추가
    }
    
    // 애니메이터에 특정 파라미터가 있는지 확인하는 헬퍼 함수
    bool HasParameter(string paramName)
    {
        foreach (AnimatorControllerParameter param in animator.parameters)
        {
            if (param.name == paramName)
                return true;
        }
        return false;
    }
}

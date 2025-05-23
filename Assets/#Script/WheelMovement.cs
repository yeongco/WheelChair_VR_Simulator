using UnityEngine;
using Autohand;
using Autohand.Demo;

public class WheelMovement : MonoBehaviour
{
    [Header("바퀴 설정")]
    public WheelRotator wheelRotator;    // 바퀴 회전 컴포넌트
    public float wheelRadius = 0.5f;     // 바퀴 반지름 (미터)
    
    [Header("이동 설정")]
    public Transform parentObject;        // 이동시킬 부모 오브젝트
    public float moveSpeed = 1f;         // 이동 속도 계수
    public bool useLocalMovement = true; // 로컬 좌표계 사용 여부

    private float lastValue = 0f;        // 이전 프레임의 회전량
    private float totalValue = 0f;       // 누적 회전량

    void Start()
    {
        // 부모 오브젝트가 지정되지 않았다면 현재 오브젝트의 부모를 사용
        if (parentObject == null)
        {
            parentObject = transform.parent;
        }

        // 초기 회전량 저장
        if (wheelRotator != null)
        {
            lastValue = wheelRotator.GetRotationValue();
        }
    }

    void Update()
    {
        if (wheelRotator == null || parentObject == null)
            return;

        // 현재 회전량 가져오기
        float currentValue = wheelRotator.GetRotationValue();
        
        // 회전량 변화 계산
        float valueDelta = currentValue - lastValue;
        
        // 회전량에 따른 이동 거리 계산
        float moveDistance = CalculateMoveDistance(valueDelta);
        
        // 부모 오브젝트 이동
        MoveParentObject(moveDistance);
        
        // 현재 회전량 저장
        lastValue = currentValue;
        totalValue += valueDelta;
    }

    private float CalculateMoveDistance(float valueDelta)
    {
        // 회전량에 따른 이동 거리 계산
        // valueDelta가 1이면 바퀴가 한 바퀴 회전한 것
        float circumference = 2f * Mathf.PI * wheelRadius;
        return valueDelta * circumference * moveSpeed;
    }

    private void MoveParentObject(float distance)
    {
        // 이동 방향 결정 (바퀴의 회전 축에 따라)
        Vector3 moveDirection = GetMoveDirection();
        
        // 이동 적용
        Vector3 movement = moveDirection * distance;
        if (useLocalMovement)
        {
            parentObject.localPosition += movement;
        }
        else
        {
            parentObject.position += movement;
        }
    }

    private Vector3 GetMoveDirection()
    {
        // 바퀴의 회전 축에 따라 이동 방향 결정
        if (Mathf.Abs(wheelRotator.angle.x) > 0)
            return Vector3.right;
        else if (Mathf.Abs(wheelRotator.angle.y) > 0)
            return Vector3.up;
        else
            return Vector3.forward;
    }

    // 현재까지의 총 회전량 반환
    public float GetTotalValue()
    {
        return totalValue;
    }

    // 이동 거리 초기화
    public void ResetMovement()
    {
        totalValue = 0f;
        lastValue = wheelRotator.GetRotationValue();
    }
} 
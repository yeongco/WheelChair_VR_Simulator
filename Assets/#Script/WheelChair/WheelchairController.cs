using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Autohand;

public class WheelchairController : MonoBehaviour
{
    [Header("휠체어 본체 설정")]
    public Rigidbody chairRigidbody;
    public Transform chairCenter; // 휠체어 중앙 위치
    
    [Header("바퀴 설정")]
    public Transform leftWheel;
    public Transform rightWheel;
    public Transform leftWheelCenter; // 왼쪽 바퀴 중심 위치 (빈 오브젝트)
    public Transform rightWheelCenter; // 오른쪽 바퀴 중심 위치 (빈 오브젝트)
    public Grabbable leftWheelGrab;
    public Grabbable rightWheelGrab;
    
    [Header("높이 감지 설정")]
    public Transform frontHeightPoint; // 앞쪽 높이 감지 포인트 (빈 오브젝트)
    public Transform rearHeightPoint; // 뒤쪽 높이 감지 포인트 (빈 오브젝트)
    public float groundCheckDistance = 2f; // 바닥 감지 거리
    public float hoverHeight = 0.1f; // 바닥에서 띄울 높이
    public LayerMask groundLayer = 1; // 바닥 레이어
    
    [Header("이동 설정")]
    public float wheelForce = 500f; // 바퀴 힘
    public float moveSpeedMultiplier = 1f; // 이동 속도 가중치
    public float rotationSpeedMultiplier = 1f; // 회전 속도 가중치
    public float speedDecayRate = 0.95f; // 회전 속도 감소율
    
    [Header("경사로 설정")]
    public float slopeSlipFactor = 0.5f; // 경사로 미끄러짐 정도 (0-1)
    public float maxSlopeAngle = 30f; // 최대 경사각
    
    [Header("안정성 설정")]
    public float stabilityForce = 1000f; // 안정화 힘
    public float maxTiltAngle = 15f; // 최대 기울기 각도
    
    // 내부 변수들
    private float leftWheelSpeed = 0f;
    private float rightWheelSpeed = 0f;
    private float leftWheelRotation = 0f;
    private float rightWheelRotation = 0f;
    
    private Vector3 lastLeftHandPos;
    private Vector3 lastRightHandPos;
    private bool lastLeftGrabbed = false;
    private bool lastRightGrabbed = false;
    
    private float frontGroundHeight = 0f;
    private float rearGroundHeight = 0f;
    private float currentSlopeAngle = 0f;
    
    void Start()
    {
        // 휠체어 물리 설정
        if (chairRigidbody == null)
            chairRigidbody = GetComponent<Rigidbody>();
            
        // 무게중심을 낮게 설정하여 안정성 향상
        chairRigidbody.centerOfMass = new Vector3(0, -0.5f, 0);
        chairRigidbody.maxAngularVelocity = 5f;
    }
    
    void FixedUpdate()
    {
        // 바닥 높이 감지 및 휠체어 높이 조절
        CheckGroundHeight();
        AdjustChairHeight();
        
        // 경사로 각도 계산
        CalculateSlopeAngle();
        
        // 바퀴 입력 처리
        ProcessWheelInput();
        
        // 휠체어 이동 및 회전
        MoveWheelchair();
        
        // 경사로에서 미끄러짐 처리
        HandleSlopeSlipping();
        
        // 안정성 유지
        StabilizeChair();
        
        // 바퀴 회전 시각화
        UpdateWheelVisuals();
    }
    
    void CheckGroundHeight()
    {
        // 앞쪽 지점에서 바닥까지의 거리 측정
        RaycastHit frontHit;
        if (Physics.Raycast(frontHeightPoint.position, Vector3.down, out frontHit, groundCheckDistance, groundLayer))
        {
            frontGroundHeight = frontHit.point.y;
        }
        else
        {
            frontGroundHeight = frontHeightPoint.position.y - groundCheckDistance;
        }
        
        // 뒤쪽 지점에서 바닥까지의 거리 측정
        RaycastHit rearHit;
        if (Physics.Raycast(rearHeightPoint.position, Vector3.down, out rearHit, groundCheckDistance, groundLayer))
        {
            rearGroundHeight = rearHit.point.y;
        }
        else
        {
            rearGroundHeight = rearHeightPoint.position.y - groundCheckDistance;
        }
        
        // 디버그용 레이 그리기
        Debug.DrawRay(frontHeightPoint.position, Vector3.down * groundCheckDistance, Color.red);
        Debug.DrawRay(rearHeightPoint.position, Vector3.down * groundCheckDistance, Color.blue);
    }
    
    void AdjustChairHeight()
    {
        // 두 지점의 평균 높이 계산
        float averageGroundHeight = (frontGroundHeight + rearGroundHeight) * 0.5f;
        float targetHeight = averageGroundHeight + hoverHeight;
        
        // 현재 높이와 목표 높이의 차이 계산
        float heightDifference = targetHeight - transform.position.y;
        
        // 높이 조절을 위한 힘 적용
        if (Mathf.Abs(heightDifference) > 0.01f)
        {
            Vector3 heightForce = Vector3.up * heightDifference * stabilityForce;
            chairRigidbody.AddForce(heightForce, ForceMode.Force);
        }
        
        // 경사에 따른 휠체어 기울기 조절
        float heightDiff = frontGroundHeight - rearGroundHeight;
        float distance = Vector3.Distance(frontHeightPoint.position, rearHeightPoint.position);
        float targetAngle = Mathf.Atan2(heightDiff, distance) * Mathf.Rad2Deg;
        
        // 목표 회전 계산
        Vector3 currentEuler = transform.eulerAngles;
        Vector3 targetEuler = new Vector3(targetAngle, currentEuler.y, currentEuler.z);
        
        // 부드러운 회전 적용
        Quaternion targetRotation = Quaternion.Euler(targetEuler);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.fixedDeltaTime * 2f);
    }
    
    void CalculateSlopeAngle()
    {
        // 앞뒤 높이 차이로 경사각 계산
        float heightDiff = frontGroundHeight - rearGroundHeight;
        float distance = Vector3.Distance(frontHeightPoint.position, rearHeightPoint.position);
        currentSlopeAngle = Mathf.Atan2(Mathf.Abs(heightDiff), distance) * Mathf.Rad2Deg;
    }
    
    void ProcessWheelInput()
    {
        // 왼쪽 바퀴 입력 처리
        bool leftGrabbed = leftWheelGrab.GetHeldBy().Count > 0;
        float leftInput = GetWheelInput(leftWheelGrab, ref lastLeftHandPos, ref lastLeftGrabbed);
        
        // 오른쪽 바퀴 입력 처리
        bool rightGrabbed = rightWheelGrab.GetHeldBy().Count > 0;
        float rightInput = GetWheelInput(rightWheelGrab, ref lastRightHandPos, ref lastRightGrabbed);
        
        // 바퀴 속도 업데이트
        if (leftGrabbed)
        {
            leftWheelSpeed = leftInput * moveSpeedMultiplier;
        }
        else
        {
            leftWheelSpeed *= speedDecayRate; // 속도 감소
        }
        
        if (rightGrabbed)
        {
            rightWheelSpeed = rightInput * moveSpeedMultiplier;
        }
        else
        {
            rightWheelSpeed *= speedDecayRate; // 속도 감소
        }
        
        // 바퀴 회전 누적
        leftWheelRotation += leftWheelSpeed * Time.fixedDeltaTime;
        rightWheelRotation += rightWheelSpeed * Time.fixedDeltaTime;
    }
    
    float GetWheelInput(Grabbable grab, ref Vector3 lastHandPos, ref bool lastGrabbed)
    {
        if (grab.GetHeldBy().Count == 0)
        {
            lastGrabbed = false;
            return 0f;
        }
        
        Hand hand = grab.GetHeldBy()[0] as Hand;
        if (hand == null)
            return 0f;
            
        Vector3 handPos = hand.transform.position;
        
        if (!lastGrabbed)
        {
            lastHandPos = handPos;
            lastGrabbed = true;
            return 0f;
        }
        
        // 손의 이동 벡터 계산
        Vector3 delta = handPos - lastHandPos;
        
        // 휠체어의 전진 방향으로 투영
        Vector3 forwardDirection = transform.forward;
        forwardDirection.y = 0;
        forwardDirection.Normalize();
        
        float input = Vector3.Dot(delta, forwardDirection);
        
        lastHandPos = handPos;
        return input * 100f; // 입력 감도 조절
    }
    
    void MoveWheelchair()
    {
        // 두 바퀴가 모두 잡혀있는지 확인
        bool leftGrabbed = leftWheelGrab.GetHeldBy().Count > 0;
        bool rightGrabbed = rightWheelGrab.GetHeldBy().Count > 0;
        
        if (leftGrabbed && rightGrabbed)
        {
            // 두 바퀴 모두 잡혀있을 때
            if (Mathf.Sign(leftWheelSpeed) != Mathf.Sign(rightWheelSpeed) && 
                Mathf.Abs(leftWheelSpeed) > 0.1f && Mathf.Abs(rightWheelSpeed) > 0.1f)
            {
                // 서로 다른 방향으로 돌릴 때 - 중앙에서 회전
                float rotationSpeed = (rightWheelSpeed - leftWheelSpeed) * rotationSpeedMultiplier;
                chairRigidbody.AddTorque(Vector3.up * rotationSpeed, ForceMode.Force);
            }
            else
            {
                // 같은 방향으로 돌릴 때 - 직진
                float averageSpeed = (leftWheelSpeed + rightWheelSpeed) * 0.5f;
                Vector3 moveForce = transform.forward * averageSpeed * wheelForce;
                chairRigidbody.AddForce(moveForce, ForceMode.Force);
            }
        }
        else if (leftGrabbed && !rightGrabbed)
        {
            // 왼쪽 바퀴만 잡혀있을 때 - 오른쪽 바퀴 중심으로 회전
            Vector3 pivotPoint = rightWheelCenter.position;
            RotateAroundPoint(pivotPoint, leftWheelSpeed);
        }
        else if (!leftGrabbed && rightGrabbed)
        {
            // 오른쪽 바퀴만 잡혀있을 때 - 왼쪽 바퀴 중심으로 회전
            Vector3 pivotPoint = leftWheelCenter.position;
            RotateAroundPoint(pivotPoint, -rightWheelSpeed);
        }
    }
    
    void RotateAroundPoint(Vector3 pivotPoint, float speed)
    {
        // 피벗 포인트를 중심으로 회전
        Vector3 direction = (transform.position - pivotPoint).normalized;
        Vector3 tangent = Vector3.Cross(Vector3.up, direction);
        
        Vector3 moveForce = tangent * speed * wheelForce * rotationSpeedMultiplier;
        chairRigidbody.AddForce(moveForce, ForceMode.Force);
        
        // 회전 토크 추가
        float rotationTorque = speed * rotationSpeedMultiplier;
        chairRigidbody.AddTorque(Vector3.up * rotationTorque, ForceMode.Force);
    }
    
    void HandleSlopeSlipping()
    {
        // 경사로에서 미끄러짐 처리
        if (currentSlopeAngle > 5f) // 5도 이상의 경사에서만 적용
        {
            // 바퀴가 잡혀있지 않을 때만 미끄러짐 적용
            bool anyWheelGrabbed = leftWheelGrab.GetHeldBy().Count > 0 || rightWheelGrab.GetHeldBy().Count > 0;
            
            if (!anyWheelGrabbed)
            {
                // 경사 방향 계산
                Vector3 slopeDirection = Vector3.zero;
                if (frontGroundHeight < rearGroundHeight)
                {
                    slopeDirection = transform.forward; // 앞으로 미끄러짐
                }
                else
                {
                    slopeDirection = -transform.forward; // 뒤로 미끄러짐
                }
                
                // 미끄러짐 힘 계산
                float slopeForce = Mathf.Sin(currentSlopeAngle * Mathf.Deg2Rad) * slopeSlipFactor * 100f;
                Vector3 slipForce = slopeDirection * slopeForce;
                
                chairRigidbody.AddForce(slipForce, ForceMode.Force);
            }
        }
    }
    
    void StabilizeChair()
    {
        // 과도한 기울기 방지
        Vector3 currentUp = transform.up;
        float tiltAngle = Vector3.Angle(currentUp, Vector3.up);
        
        if (tiltAngle > maxTiltAngle)
        {
            Vector3 correctionAxis = Vector3.Cross(currentUp, Vector3.up);
            float correctionForce = (tiltAngle - maxTiltAngle) * stabilityForce;
            chairRigidbody.AddTorque(correctionAxis * correctionForce, ForceMode.Force);
        }
        
        // 과도한 속도 제한
        if (chairRigidbody.velocity.magnitude > 10f)
        {
            chairRigidbody.velocity = chairRigidbody.velocity.normalized * 10f;
        }
        
        if (chairRigidbody.angularVelocity.magnitude > 5f)
        {
            chairRigidbody.angularVelocity = chairRigidbody.angularVelocity.normalized * 5f;
        }
    }
    
    void UpdateWheelVisuals()
    {
        // 바퀴 회전 시각화
        if (leftWheel != null)
        {
            leftWheel.Rotate(Vector3.right * leftWheelSpeed * Time.fixedDeltaTime * 50f, Space.Self);
        }
        
        if (rightWheel != null)
        {
            rightWheel.Rotate(Vector3.right * rightWheelSpeed * Time.fixedDeltaTime * 50f, Space.Self);
        }
    }
    
    // 디버그 정보 표시
    void OnDrawGizmos()
    {
        if (frontHeightPoint != null && rearHeightPoint != null)
        {
            // 높이 감지 포인트 표시
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(frontHeightPoint.position, 0.1f);
            
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(rearHeightPoint.position, 0.1f);
            
            // 바닥 감지 레이 표시
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(frontHeightPoint.position, frontHeightPoint.position + Vector3.down * groundCheckDistance);
            Gizmos.DrawLine(rearHeightPoint.position, rearHeightPoint.position + Vector3.down * groundCheckDistance);
        }
        
        if (leftWheelCenter != null && rightWheelCenter != null)
        {
            // 바퀴 중심 포인트 표시
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(leftWheelCenter.position, 0.15f);
            Gizmos.DrawWireSphere(rightWheelCenter.position, 0.15f);
        }
        
        if (chairCenter != null)
        {
            // 휠체어 중앙 표시
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(chairCenter.position, 0.2f);
        }
    }
}

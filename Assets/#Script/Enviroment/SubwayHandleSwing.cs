using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class SubwayHandleSwing : MonoBehaviour
{
    [Header("진자 설정")]
    [SerializeField] private float swingAmount = 10f; // 흔들림 각도 (최대)
    [SerializeField] private float swingDuration = 1.2f; // 한번 흔들리는데 걸리는 시간
    [SerializeField] private float returnDelay = 0.2f; // 복귀 지연 시간
    [SerializeField] private Ease swingEase = Ease.OutQuad; // 흔들림 애니메이션 이징
    [SerializeField] private Ease returnEase = Ease.InOutSine; // 복귀 애니메이션 이징
    
    [Header("진자 축")]
    [SerializeField] private Vector3 pivotAxis = Vector3.forward; // 손잡이 회전 축 (Z축 기준)
    [SerializeField] private bool useLocalRotation = true; // 로컬 회전 사용 여부
    [SerializeField] private bool invertDirection = false; // 방향 반전 여부
    [SerializeField] private bool preserveInitialRotation = true; // 초기 회전 유지 여부

    [Header("외부 영향")]
    [SerializeField] private SubwayVibration subwayVibration; // 지하철 진동 참조
    [SerializeField] private float vibrationInfluence = 1f; // 진동 영향력
    [SerializeField] private float randomMotionInterval = 1.5f; // 무작위 움직임 간격
    [SerializeField] [Range(0f, 1f)] private float randomChance = 0.5f; // 무작위 움직임 발생 확률
    
    [Header("동기화 설정")]
    [SerializeField] private bool useSynchronization = false; // 동기화 사용 여부
    [SerializeField] private SyncMode syncMode = SyncMode.Independent; // 동기화 모드
    [SerializeField] private SubwayHandleSwing masterHandle; // 마스터 손잡이 (팔로워 모드에서 사용)
    [SerializeField] private List<SubwayHandleSwing> syncedHandles = new List<SubwayHandleSwing>(); // 동기화할 손잡이들 (마스터 모드에서 사용)
    [SerializeField] private string syncGroupID = ""; // 동기화 그룹 ID (그룹 모드에서 사용)
    
    // 동기화 모드 열거형
    public enum SyncMode
    {
        Independent, // 독립 모드 (동기화 없음)
        Master,      // 마스터 모드 (다른 손잡이 제어)
        Follower,    // 팔로워 모드 (마스터 손잡이 따라감)
        Group        // 그룹 모드 (같은 그룹 ID 공유)
    }
    
    // 내부 변수
    private Vector3 initialRotation; // 초기 회전값
    private Quaternion initialLocalRotation; // 초기 로컬 회전
    private Sequence swingSequence; // DOTween 시퀀스
    private bool isSwinging = false; // 현재 흔들림 진행 여부
    private float lastSwingTime = 0f; // 마지막 흔들림 시간
    private float currentSwingAmount = 0f; // 현재 흔들림 크기
    private static Dictionary<string, List<SubwayHandleSwing>> syncGroups = new Dictionary<string, List<SubwayHandleSwing>>(); // 동기화 그룹
    private bool isReceivingSync = false; // 동기화 데이터 수신 중 플래그 (무한 루프 방지)
    
    private void Awake()
    {
        // 진자 축 정규화
        pivotAxis = pivotAxis.normalized;
        
        // 동기화 그룹에 등록
        if (useSynchronization && syncMode == SyncMode.Group && !string.IsNullOrEmpty(syncGroupID))
        {
            RegisterToSyncGroup();
        }
    }
    
    private void Start()
    {
        // 초기 회전값 저장
        initialRotation = transform.eulerAngles;
        initialLocalRotation = transform.localRotation;
        
        // 지하철 진동 컴포넌트 자동 찾기
        if (subwayVibration == null)
        {
            subwayVibration = GetComponentInParent<SubwayVibration>();
        }
        
        // 독립 모드나 마스터 모드에서만 자동 움직임 시작
        if (CanInitiateMovement())
        {
            StartCoroutine(RandomSwingRoutine());
            
            // 시작 시 약간의 흔들림 적용
            ApplyImpulse(Random.Range(0.3f, 0.7f), pivotAxis);
        }
        
        // 동기화 대상 검증
        ValidateSyncSettings();
    }

    private void OnEnable()
    {
        // 활성화될 때 애니메이션 초기화
        if (swingSequence != null)
        {
            swingSequence.Kill();
            swingSequence = null;
        }
        
        // 진행 중인 애니메이션 중지 및 초기 회전 복원 (필요한 경우)
        if (!isSwinging)
        {
            ResetToInitialRotation();
        }
    }
    
    private void OnDisable()
    {
        // 비활성화될 때 애니메이션 정리
        if (swingSequence != null)
        {
            swingSequence.Kill();
            swingSequence = null;
        }
        
        // 진행 중인 애니메이션 중지 및 초기 회전 복원
        ResetToInitialRotation();
    }
    
    // 초기 회전으로 복원
    private void ResetToInitialRotation()
    {
        if (preserveInitialRotation)
        {
            transform.localRotation = initialLocalRotation;
        }
    }
    
    // 동기화 설정 검증
    private void ValidateSyncSettings()
    {
        if (!useSynchronization) return;
        
        switch (syncMode)
        {
            case SyncMode.Master:
                // 동기화 대상 목록에서 null 제거
                syncedHandles.RemoveAll(handle => handle == null);
                // 자기 자신이 목록에 있으면 제거
                syncedHandles.RemoveAll(handle => handle == this);
                break;
                
            case SyncMode.Follower:
                // 마스터가 없거나 자기 자신이면 독립 모드로 전환
                if (masterHandle == null || masterHandle == this)
                {
                    Debug.LogWarning(name + ": 마스터 손잡이가 올바르게 설정되지 않았습니다. 독립 모드로 전환합니다.");
                    syncMode = SyncMode.Independent;
                }
                break;
        }
    }
    
    // 동기화 그룹에 등록
    private void RegisterToSyncGroup()
    {
        if (!syncGroups.ContainsKey(syncGroupID))
        {
            syncGroups[syncGroupID] = new List<SubwayHandleSwing>();
        }
        
        if (!syncGroups[syncGroupID].Contains(this))
        {
            syncGroups[syncGroupID].Add(this);
        }
    }
    
    // 동기화 그룹에서 제거
    private void UnregisterFromSyncGroup()
    {
        if (syncGroups.ContainsKey(syncGroupID) && syncGroups[syncGroupID].Contains(this))
        {
            syncGroups[syncGroupID].Remove(this);
            
            // 그룹이 비었으면 제거
            if (syncGroups[syncGroupID].Count == 0)
            {
                syncGroups.Remove(syncGroupID);
            }
        }
    }
    
    // 동작을 시작할 수 있는지 확인
    private bool CanInitiateMovement()
    {
        return !useSynchronization || 
               syncMode == SyncMode.Independent || 
               syncMode == SyncMode.Master || 
               (syncMode == SyncMode.Group && IsGroupLeader());
    }
    
    // 그룹의 리더인지 확인 (리스트의 첫 번째 요소)
    private bool IsGroupLeader()
    {
        return syncMode == SyncMode.Group && 
               syncGroups.ContainsKey(syncGroupID) && 
               syncGroups[syncGroupID].Count > 0 && 
               syncGroups[syncGroupID][0] == this;
    }
    
    // 축 방향에 맞게 벡터를 변환
    private Vector3 GetAxisAlignedVector(float amount)
    {
        Vector3 rotationVector = Vector3.zero;
        
        // 피벗 축에 따라 회전 값 할당
        if (Mathf.Abs(pivotAxis.x) > Mathf.Abs(pivotAxis.y) && Mathf.Abs(pivotAxis.x) > Mathf.Abs(pivotAxis.z))
            rotationVector.x = amount * Mathf.Sign(pivotAxis.x) * (invertDirection ? -1 : 1);
        else if (Mathf.Abs(pivotAxis.y) > Mathf.Abs(pivotAxis.x) && Mathf.Abs(pivotAxis.y) > Mathf.Abs(pivotAxis.z))
            rotationVector.y = amount * Mathf.Sign(pivotAxis.y) * (invertDirection ? -1 : 1);
        else
            rotationVector.z = amount * Mathf.Sign(pivotAxis.z) * (invertDirection ? -1 : 1);
            
        return rotationVector;
    }
    
    // DOTween을 사용하여 스윙 효과 적용
    private void ApplySwingWithDOTween(float angle, float duration, bool rebound = true, bool propagateSync = true)
    {
        // 이미 진행 중인 시퀀스가 있으면 정지
        if (swingSequence != null)
        {
            swingSequence.Kill();
            swingSequence = null;
        }
        
        // 현재 진자 상태 업데이트
        isSwinging = true;
        lastSwingTime = Time.time;
        currentSwingAmount = angle;
        
        // 새 시퀀스 생성
        swingSequence = DOTween.Sequence();
        
        // 기본 축으로 변환된 회전값 계산
        Vector3 rotationVector = GetAxisAlignedVector(angle);
        
        // 현재 회전 상태 가져오기
        Quaternion currentRotation = transform.localRotation;
        
        // 진자 운동 (흔들림) 구현
        if (useLocalRotation)
        {
            // 첫 번째 흔들림 (초기 위치에서 최대 각도로)
            Quaternion targetRotation = preserveInitialRotation ? 
                initialLocalRotation * Quaternion.Euler(rotationVector) : 
                Quaternion.Euler(rotationVector);
                
            swingSequence.Append(transform.DOLocalRotateQuaternion(targetRotation, duration * 0.5f)
                             .SetEase(swingEase));
            
            if (rebound)
            {
                // 반동 (최대 각도에서 반대 방향으로 약간 더 작은 각도)
                Vector3 reboundVector = new Vector3(-rotationVector.x * 0.7f, -rotationVector.y * 0.7f, -rotationVector.z * 0.7f);
                Quaternion reboundRotation = preserveInitialRotation ? 
                    initialLocalRotation * Quaternion.Euler(reboundVector) : 
                    Quaternion.Euler(reboundVector);
                    
                swingSequence.Append(transform.DOLocalRotateQuaternion(reboundRotation, duration * 0.4f)
                                 .SetEase(swingEase));
                
                // 진자 운동이 자연스럽게 줄어들도록 몇 번 더 작은 흔들림 추가
                Vector3 dampVector1 = new Vector3(rotationVector.x * 0.4f, rotationVector.y * 0.4f, rotationVector.z * 0.4f);
                Quaternion dampRotation1 = preserveInitialRotation ? 
                    initialLocalRotation * Quaternion.Euler(dampVector1) : 
                    Quaternion.Euler(dampVector1);
                    
                swingSequence.Append(transform.DOLocalRotateQuaternion(dampRotation1, duration * 0.35f)
                                 .SetEase(swingEase));
                
                Vector3 dampVector2 = new Vector3(-rotationVector.x * 0.2f, -rotationVector.y * 0.2f, -rotationVector.z * 0.2f);
                Quaternion dampRotation2 = preserveInitialRotation ? 
                    initialLocalRotation * Quaternion.Euler(dampVector2) : 
                    Quaternion.Euler(dampVector2);
                    
                swingSequence.Append(transform.DOLocalRotateQuaternion(dampRotation2, duration * 0.3f)
                                 .SetEase(swingEase));
            }
            
            // 중앙으로 돌아오기 (초기 로컬 회전으로)
            swingSequence.Append(transform.DOLocalRotateQuaternion(initialLocalRotation, duration * 0.25f)
                             .SetEase(returnEase));
        }
        else
        {
            // 월드 회전을 사용하는 경우
            Quaternion startRot = transform.rotation;
            Quaternion targetRot = startRot * Quaternion.AngleAxis(angle, pivotAxis);
            Quaternion reboundRot = startRot * Quaternion.AngleAxis(-angle * 0.7f, pivotAxis);
            
            // 첫 번째 흔들림
            swingSequence.Append(transform.DORotateQuaternion(targetRot, duration * 0.5f)
                             .SetEase(swingEase));
            
            if (rebound)
            {
                // 반동
                swingSequence.Append(transform.DORotateQuaternion(reboundRot, duration * 0.4f)
                                 .SetEase(swingEase));
                
                // 몇 번 더 작은 흔들림
                swingSequence.Append(transform.DORotateQuaternion(
                    startRot * Quaternion.AngleAxis(angle * 0.4f, pivotAxis), duration * 0.35f)
                                 .SetEase(swingEase));
                
                swingSequence.Append(transform.DORotateQuaternion(
                    startRot * Quaternion.AngleAxis(-angle * 0.2f, pivotAxis), duration * 0.3f)
                                 .SetEase(swingEase));
            }
            
            // 원래 회전으로 돌아오기
            Quaternion initialWorldRotation = preserveInitialRotation ? 
                Quaternion.Euler(initialRotation) : 
                startRot;
                
            swingSequence.Append(transform.DORotateQuaternion(initialWorldRotation, duration * 0.25f)
                             .SetEase(returnEase));
        }
        
        // 애니메이션 완료 후 상태 초기화
        swingSequence.OnComplete(() => {
            isSwinging = false;
            swingSequence = null;
        });
        
        // 시퀀스 실행
        swingSequence.Play();
        
        // 동기화 전파 (필요한 경우)
        if (useSynchronization && propagateSync && !isReceivingSync)
        {
            SynchronizeMovement(angle, duration, rebound);
        }
    }
    
    // 움직임 동기화
    private void SynchronizeMovement(float angle, float duration, bool rebound)
    {
        switch (syncMode)
        {
            case SyncMode.Master:
                // 마스터 모드에서는 등록된 모든 팔로워에게 동기화
                foreach (var handle in syncedHandles)
                {
                    if (handle != null && handle.gameObject.activeInHierarchy)
                    {
                        handle.ReceiveSyncedMovement(angle, duration, rebound);
                    }
                }
                break;
                
            case SyncMode.Group:
                // 그룹 모드에서는 같은 그룹의 다른 손잡이에게 동기화
                if (syncGroups.ContainsKey(syncGroupID))
                {
                    foreach (var handle in syncGroups[syncGroupID])
                    {
                        if (handle != null && handle != this && handle.gameObject.activeInHierarchy)
                        {
                            handle.ReceiveSyncedMovement(angle, duration, rebound);
                        }
                    }
                }
                break;
        }
    }
    
    // 동기화된 움직임 수신
    public void ReceiveSyncedMovement(float angle, float duration, bool rebound)
    {
        if (!gameObject.activeInHierarchy || !useSynchronization) return;
        
        isReceivingSync = true;
        
        // 방향 반전 적용
        if (invertDirection)
        {
            angle *= -1;
        }
        
        // 움직임 적용
        ApplySwingWithDOTween(angle, duration, rebound, false);
        
        isReceivingSync = false;
    }
    
    // 주기적으로 무작위 흔들림 적용
    private IEnumerator RandomSwingRoutine()
    {
        while (true)
        {
            // 다음 움직임까지 대기
            float waitTime = Random.Range(randomMotionInterval * 0.8f, randomMotionInterval * 1.2f);
            yield return new WaitForSeconds(waitTime);
            
            // 설정된 확률로 무작위 흔들림 발생
            if (Random.value < randomChance && !isSwinging && CanInitiateMovement())
            {
                float randomAngle = Random.Range(swingAmount * 0.3f, swingAmount * 0.7f);
                float randomDirection = Random.value > 0.5f ? 1f : -1f;
                
                ApplySwingWithDOTween(randomAngle * randomDirection, swingDuration * Random.Range(0.8f, 1.2f));
            }
        }
    }
    
    // 지하철 충격/진동에 의한 힘 적용
    public void ApplyImpulse(float force, Vector3 direction)
    {
        // 팔로워 모드에서는 임펄스를 직접 받지 않음
        if (useSynchronization && syncMode == SyncMode.Follower)
        {
            return;
        }
        
        // 이미 강하게 흔들리고 있으면 새 충격 건너뛰기
        if (isSwinging && Mathf.Abs(currentSwingAmount) > Mathf.Abs(force * swingAmount * vibrationInfluence))
        {
            return;
        }
        
        // 방향 벡터와 회전 축의 내적을 계산하여 효과적인 힘 계산
        float effectiveForce = Vector3.Dot(direction.normalized, pivotAxis) * force;
        
        // DOTween으로 흔들림 애니메이션 적용
        float swingAngle = effectiveForce * swingAmount * vibrationInfluence;
        float impulseDuration = swingDuration * Mathf.Lerp(0.7f, 1.3f, Mathf.Abs(effectiveForce));
        
        ApplySwingWithDOTween(swingAngle, impulseDuration);
    }
    
    // 지하철의 급정거, 급출발 시 강한 충격 적용
    public void ApplyStrongImpulse(float direction = 1f)
    {
        // 팔로워 모드에서는 임펄스를 직접 받지 않음
        if (useSynchronization && syncMode == SyncMode.Follower)
        {
            return;
        }
        
        // 진행 방향으로 강한 충격 적용
        float force = Random.Range(1.5f, 3f) * direction;
        float swingAngle = force * swingAmount * vibrationInfluence;
        
        // 더 오래 흔들리게 설정
        ApplySwingWithDOTween(swingAngle, swingDuration * 1.5f);
    }
    
    // 외부에서 이 메서드를 호출하여 여러 손잡이에 동시에 충격 적용 가능
    public static void ApplyImpulseToAllHandles(float force, Vector3 direction)
    {
        SubwayHandleSwing[] allHandles = FindObjectsOfType<SubwayHandleSwing>();
        foreach (SubwayHandleSwing handle in allHandles)
        {
            // 마스터와 독립 모드 손잡이만 처리
            if (handle.CanInitiateMovement())
            {
                // 약간의 지연을 두고 모든 손잡이가 동시에 움직이지 않도록 함
                float delay = Random.Range(0f, 0.15f);
                handle.StartCoroutine(handle.DelayedImpulse(force, direction, delay));
            }
        }
    }
    
    // 지연된 충격 적용
    private IEnumerator DelayedImpulse(float force, Vector3 direction, float delay)
    {
        yield return new WaitForSeconds(delay);
        ApplyImpulse(force, direction);
    }
    
    // 편집기에서 축 위치 시각화
    private void OnDrawGizmosSelected()
    {
        // 축 방향 표시 (방향 반전 시 색상 변경)
        Vector3 axisDirection = pivotAxis.normalized * 0.1f;
        if (invertDirection)
        {
            Gizmos.color = Color.red;
        }
        else
        {
            Gizmos.color = Color.yellow;
        }
        
        Gizmos.DrawLine(transform.position, transform.position + axisDirection);
        
        // 축 방향 표시 (화살표)
        Vector3 arrowPos = transform.position + axisDirection * 0.8f;
        Gizmos.DrawSphere(arrowPos, 0.01f);
        
        // 동기화 관계 시각화
        if (useSynchronization)
        {
            switch (syncMode)
            {
                case SyncMode.Master:
                    // 마스터 -> 팔로워 연결 표시
                    Gizmos.color = Color.green;
                    foreach (var follower in syncedHandles)
                    {
                        if (follower != null)
                        {
                            Gizmos.DrawLine(transform.position, follower.transform.position);
                            
                            // 화살표 머리 그리기
                            Vector3 direction = (follower.transform.position - transform.position).normalized;
                            Vector3 arrowPos2 = follower.transform.position - direction * 0.1f;
                            Gizmos.DrawSphere(arrowPos2, 0.01f);
                        }
                    }
                    break;
                    
                case SyncMode.Follower:
                    // 팔로워 -> 마스터 연결 표시
                    if (masterHandle != null)
                    {
                        Gizmos.color = Color.blue;
                        Gizmos.DrawLine(transform.position, masterHandle.transform.position);
                        
                        // 화살표 머리 그리기
                        Vector3 direction = (masterHandle.transform.position - transform.position).normalized;
                        Vector3 arrowPos2 = masterHandle.transform.position - direction * 0.1f;
                        Gizmos.DrawSphere(arrowPos2, 0.01f);
                    }
                    break;
                    
                case SyncMode.Group:
                    // 그룹 연결 표시
                    if (!string.IsNullOrEmpty(syncGroupID) && syncGroups.ContainsKey(syncGroupID))
                    {
                        Gizmos.color = Color.magenta;
                        foreach (var member in syncGroups[syncGroupID])
                        {
                            if (member != null && member != this)
                            {
                                Gizmos.DrawLine(transform.position, member.transform.position);
                            }
                        }
                    }
                    break;
            }
        }
    }
    
    // 애플리케이션 종료 시 정리
    private void OnDestroy()
    {
        // 동기화 그룹에서 제거
        if (useSynchronization && syncMode == SyncMode.Group)
        {
            UnregisterFromSyncGroup();
        }
        
        // DOTween 애니메이션 정리
        if (swingSequence != null)
        {
            swingSequence.Kill();
            swingSequence = null;
        }
    }
} 
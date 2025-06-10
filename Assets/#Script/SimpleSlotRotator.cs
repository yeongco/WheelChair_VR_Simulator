using UnityEngine;
using Obi.Samples;

/// <summary>
/// 간단한 버전: 특정 슬롯에 붙으면 Z축을 180도 회전시키는 컴포넌트
/// </summary>
[RequireComponent(typeof(TangledPeg))]
public class SimpleSlotRotator : MonoBehaviour
{
    [Header("회전 설정")]
    public string targetSlotName = "TangledPegSlot (4)";   // 대상 슬롯 이름
    public int targetSlotNumber = 4;                       // 대상 슬롯 번호
    public Vector3 rotationWhenDocked = new Vector3(0, 0, 180);  // 도킹 시 회전값
    public bool smoothRotation = true;                     // 부드러운 회전
    public float rotationSpeed = 3f;                       // 회전 속도
    
    private TangledPeg tangledPeg;
    private TangledPegSlot lastSlot;
    private Vector3 originalRotation;
    private bool isRotating = false;
    
    void Start()
    {
        tangledPeg = GetComponent<TangledPeg>();
        originalRotation = transform.eulerAngles;
        
        Debug.Log($"[SimpleSlotRotator] {gameObject.name}: 초기화 완료 - 대상 슬롯: {targetSlotName}");
    }
    
    void Update()
    {
        CheckSlotChange();
    }
    
    void CheckSlotChange()
    {
        if (tangledPeg == null) return;
        
        TangledPegSlot currentSlot = tangledPeg.currentSlot;
        
        // 슬롯이 변경되었을 때만 처리
        if (currentSlot != lastSlot)
        {
            if (currentSlot != null && IsTargetSlot(currentSlot))
            {
                // 목표 슬롯에 도킹됨
                RotateToTarget();
            }
            else if (lastSlot != null && IsTargetSlot(lastSlot))
            {
                // 목표 슬롯에서 떠남
                RotateToOriginal();
            }
            
            lastSlot = currentSlot;
        }
    }
    
    bool IsTargetSlot(TangledPegSlot slot)
    {
        // 이름으로 확인
        if (slot.name.Contains(targetSlotName))
            return true;
        
        // 번호로 확인
        string slotNumberStr = slot.name.Replace("TangledPegSlot", "").Replace("(", "").Replace(")", "").Trim();
        if (int.TryParse(slotNumberStr, out int slotNumber))
        {
            return slotNumber == targetSlotNumber;
        }
        
        return false;
    }
    
    void RotateToTarget()
    {
        if (isRotating) return;
        
        Debug.Log($"[SimpleSlotRotator] {gameObject.name}: 목표 슬롯에 도킹 - Z축 180도 회전 시작");
        
        if (smoothRotation)
        {
            StartCoroutine(SmoothRotate(rotationWhenDocked));
        }
        else
        {
            transform.eulerAngles = rotationWhenDocked;
        }
    }
    
    void RotateToOriginal()
    {
        if (isRotating) return;
        
        Debug.Log($"[SimpleSlotRotator] {gameObject.name}: 목표 슬롯에서 언도킹 - 원래 회전으로 복원");
        
        if (smoothRotation)
        {
            StartCoroutine(SmoothRotate(originalRotation));
        }
        else
        {
            transform.eulerAngles = originalRotation;
        }
    }
    
    System.Collections.IEnumerator SmoothRotate(Vector3 targetRotation)
    {
        isRotating = true;
        
        Vector3 startRotation = transform.eulerAngles;
        float elapsedTime = 0f;
        float duration = 1f / rotationSpeed;
        
        while (elapsedTime < duration)
        {
            float t = elapsedTime / duration;
            
            // 각 축별로 최단 회전 경로 계산
            Vector3 currentRotation = new Vector3(
                Mathf.LerpAngle(startRotation.x, targetRotation.x, t),
                Mathf.LerpAngle(startRotation.y, targetRotation.y, t),
                Mathf.LerpAngle(startRotation.z, targetRotation.z, t)
            );
            
            transform.eulerAngles = currentRotation;
            
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        
        // 정확한 최종 회전값 설정
        transform.eulerAngles = targetRotation;
        isRotating = false;
        
        Debug.Log($"[SimpleSlotRotator] {gameObject.name}: 회전 완료 - {targetRotation}");
    }
    
    // 수동 회전 (테스트용)
    public void ManualRotateToTarget()
    {
        RotateToTarget();
    }
    
    public void ManualRotateToOriginal()
    {
        RotateToOriginal();
    }
    
    // 설정 변경
    public void SetTargetSlot(string slotName)
    {
        targetSlotName = slotName;
        Debug.Log($"[SimpleSlotRotator] {gameObject.name}: 대상 슬롯 변경 - {slotName}");
    }
    
    public void SetTargetSlot(int slotNumber)
    {
        targetSlotNumber = slotNumber;
        targetSlotName = $"TangledPegSlot ({slotNumber})";
        Debug.Log($"[SimpleSlotRotator] {gameObject.name}: 대상 슬롯 변경 - 슬롯 {slotNumber}");
    }
    
    public void SetRotationValues(Vector3 dockRotation)
    {
        rotationWhenDocked = dockRotation;
        Debug.Log($"[SimpleSlotRotator] {gameObject.name}: 회전값 변경 - {dockRotation}");
    }
} 
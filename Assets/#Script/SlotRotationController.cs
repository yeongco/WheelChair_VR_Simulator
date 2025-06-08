using System.Collections;
using UnityEngine;
using Obi.Samples;

/// <summary>
/// 특정 슬롯에 도킹될 때 오브젝트의 rotation을 변경하는 컴포넌트
/// TangledPeg와 함께 사용하여 특정 슬롯에 붙을 때 회전값을 조정합니다.
/// </summary>
[RequireComponent(typeof(TangledPeg))]
public class SlotRotationController : MonoBehaviour
{
    [Header("회전 설정")]
    [SerializeField] private SlotRotationRule[] rotationRules;
    
    [Header("애니메이션 설정")]
    [SerializeField] private float rotationSpeed = 2f;
    [SerializeField] private bool smoothRotation = true;
    [SerializeField] private AnimationCurve rotationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    
    [Header("디버그")]
    [SerializeField] private bool showDebugInfo = true;
    
    private TangledPeg tangledPeg;
    private TangledPegSlot lastKnownSlot;
    private Quaternion originalRotation;
    private Coroutine rotationCoroutine;
    
    [System.Serializable]
    public class SlotRotationRule
    {
        [Header("슬롯 식별")]
        public string slotName = "TangledPegSlot (4)";       // 슬롯 이름
        public int slotIndex = 4;                            // 슬롯 번호 (대안)
        
        [Header("회전값")]
        public Vector3 targetRotation = new Vector3(0, 0, 180);  // 목표 회전값
        public bool useLocalRotation = true;                     // 로컬/월드 회전
        
        [Header("조건")]
        public bool onlyWhenDocking = true;                      // 도킹할 때만 적용
        public bool resetWhenUndocking = true;                   // 언도킹할 때 원래대로
        
        [Header("특수 설정")]
        public bool addToCurrentRotation = false;               // 현재 회전에 추가
        public float delayBeforeRotation = 0f;                  // 회전 전 지연시간
    }

    void Start()
    {
        tangledPeg = GetComponent<TangledPeg>();
        originalRotation = transform.rotation;
        
        // 기본 규칙이 없으면 생성
        if (rotationRules == null || rotationRules.Length == 0)
        {
            CreateDefaultRule();
        }
        
        if (showDebugInfo)
            Debug.Log($"[SlotRotationController] {gameObject.name}: 초기화 완료, 규칙 {rotationRules.Length}개");
    }
    
    void CreateDefaultRule()
    {
        rotationRules = new SlotRotationRule[]
        {
            new SlotRotationRule
            {
                slotName = "TangledPegSlot (4)",
                slotIndex = 4,
                targetRotation = new Vector3(0, 0, 180),
                useLocalRotation = true,
                onlyWhenDocking = true,
                resetWhenUndocking = true
            }
        };
    }

    void Update()
    {
        CheckSlotChange();
    }
    
    void CheckSlotChange()
    {
        if (tangledPeg == null) return;
        
        TangledPegSlot currentSlot = tangledPeg.currentSlot;
        
        // 슬롯이 변경되었는지 확인
        if (currentSlot != lastKnownSlot)
        {
            if (lastKnownSlot != null)
            {
                OnSlotUndocked(lastKnownSlot);
            }
            
            if (currentSlot != null)
            {
                OnSlotDocked(currentSlot);
            }
            
            lastKnownSlot = currentSlot;
        }
    }
    
    void OnSlotDocked(TangledPegSlot slot)
    {
        if (showDebugInfo)
            Debug.Log($"[SlotRotationController] {gameObject.name}: 슬롯에 도킹됨 - {slot.name}");
        
        // 매칭되는 규칙 찾기
        SlotRotationRule matchingRule = FindMatchingRule(slot);
        
        if (matchingRule != null)
        {
            ApplyRotationRule(matchingRule);
        }
    }
    
    void OnSlotUndocked(TangledPegSlot slot)
    {
        if (showDebugInfo)
            Debug.Log($"[SlotRotationController] {gameObject.name}: 슬롯에서 언도킹됨 - {slot.name}");
        
        // 매칭되는 규칙 찾기
        SlotRotationRule matchingRule = FindMatchingRule(slot);
        
        if (matchingRule != null && matchingRule.resetWhenUndocking)
        {
            ResetToOriginalRotation();
        }
    }
    
    SlotRotationRule FindMatchingRule(TangledPegSlot slot)
    {
        foreach (SlotRotationRule rule in rotationRules)
        {
            // 이름으로 매칭
            if (!string.IsNullOrEmpty(rule.slotName) && slot.name.Contains(rule.slotName))
            {
                return rule;
            }
            
            // 슬롯 번호로 매칭 (이름에서 숫자 추출)
            if (ExtractSlotNumber(slot.name) == rule.slotIndex)
            {
                return rule;
            }
        }
        
        return null;
    }
    
    int ExtractSlotNumber(string slotName)
    {
        // "TangledPegSlot (4)" 같은 형식에서 숫자 추출
        string numberPart = slotName.Replace("TangledPegSlot", "").Replace("(", "").Replace(")", "").Trim();
        
        if (int.TryParse(numberPart, out int number))
        {
            return number;
        }
        
        return -1;
    }
    
    void ApplyRotationRule(SlotRotationRule rule)
    {
        if (showDebugInfo)
            Debug.Log($"[SlotRotationController] {gameObject.name}: 회전 규칙 적용 - {rule.targetRotation}");
        
        // 이전 회전 코루틴 중지
        if (rotationCoroutine != null)
        {
            StopCoroutine(rotationCoroutine);
        }
        
        if (rule.delayBeforeRotation > 0)
        {
            rotationCoroutine = StartCoroutine(DelayedRotation(rule));
        }
        else
        {
            rotationCoroutine = StartCoroutine(RotateToTarget(rule));
        }
    }
    
    IEnumerator DelayedRotation(SlotRotationRule rule)
    {
        yield return new WaitForSeconds(rule.delayBeforeRotation);
        yield return StartCoroutine(RotateToTarget(rule));
    }
    
    IEnumerator RotateToTarget(SlotRotationRule rule)
    {
        Quaternion startRotation = transform.rotation;
        Quaternion targetRotation;
        
        if (rule.addToCurrentRotation)
        {
            // 현재 회전에 추가
            targetRotation = startRotation * Quaternion.Euler(rule.targetRotation);
        }
        else
        {
            // 절대 회전값으로 설정
            if (rule.useLocalRotation)
            {
                targetRotation = Quaternion.Euler(rule.targetRotation);
            }
            else
            {
                targetRotation = transform.parent != null ? 
                    transform.parent.rotation * Quaternion.Euler(rule.targetRotation) : 
                    Quaternion.Euler(rule.targetRotation);
            }
        }
        
        if (smoothRotation)
        {
            float elapsedTime = 0f;
            float duration = 1f / rotationSpeed;
            
            while (elapsedTime < duration)
            {
                float t = elapsedTime / duration;
                float curveValue = rotationCurve.Evaluate(t);
                
                transform.rotation = Quaternion.Lerp(startRotation, targetRotation, curveValue);
                
                elapsedTime += Time.deltaTime;
                yield return null;
            }
        }
        
        // 최종 회전값 정확히 설정
        transform.rotation = targetRotation;
        
        if (showDebugInfo)
            Debug.Log($"[SlotRotationController] {gameObject.name}: 회전 완료 - {transform.rotation.eulerAngles}");
    }
    
    void ResetToOriginalRotation()
    {
        if (showDebugInfo)
            Debug.Log($"[SlotRotationController] {gameObject.name}: 원래 회전으로 복원");
        
        // 이전 회전 코루틴 중지
        if (rotationCoroutine != null)
        {
            StopCoroutine(rotationCoroutine);
        }
        
        if (smoothRotation)
        {
            rotationCoroutine = StartCoroutine(RotateToOriginal());
        }
        else
        {
            transform.rotation = originalRotation;
        }
    }
    
    IEnumerator RotateToOriginal()
    {
        Quaternion startRotation = transform.rotation;
        float elapsedTime = 0f;
        float duration = 1f / rotationSpeed;
        
        while (elapsedTime < duration)
        {
            float t = elapsedTime / duration;
            float curveValue = rotationCurve.Evaluate(t);
            
            transform.rotation = Quaternion.Lerp(startRotation, originalRotation, curveValue);
            
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        
        transform.rotation = originalRotation;
    }
    
    /// <summary>
    /// 특정 슬롯에 대한 회전 규칙을 추가합니다
    /// </summary>
    public void AddRotationRule(string slotName, Vector3 rotation)
    {
        var newRule = new SlotRotationRule
        {
            slotName = slotName,
            targetRotation = rotation,
            useLocalRotation = true,
            onlyWhenDocking = true,
            resetWhenUndocking = true
        };
        
        System.Array.Resize(ref rotationRules, rotationRules.Length + 1);
        rotationRules[rotationRules.Length - 1] = newRule;
        
        if (showDebugInfo)
            Debug.Log($"[SlotRotationController] {gameObject.name}: 새 규칙 추가 - {slotName}: {rotation}");
    }
    
    /// <summary>
    /// 특정 슬롯 번호에 대한 회전 규칙을 추가합니다
    /// </summary>
    public void AddRotationRule(int slotIndex, Vector3 rotation)
    {
        var newRule = new SlotRotationRule
        {
            slotName = $"TangledPegSlot ({slotIndex})",
            slotIndex = slotIndex,
            targetRotation = rotation,
            useLocalRotation = true,
            onlyWhenDocking = true,
            resetWhenUndocking = true
        };
        
        System.Array.Resize(ref rotationRules, rotationRules.Length + 1);
        rotationRules[rotationRules.Length - 1] = newRule;
        
        if (showDebugInfo)
            Debug.Log($"[SlotRotationController] {gameObject.name}: 새 규칙 추가 - 슬롯 {slotIndex}: {rotation}");
    }
    
    /// <summary>
    /// 수동으로 회전을 적용합니다
    /// </summary>
    public void ManualRotate(Vector3 rotation)
    {
        if (rotationCoroutine != null)
        {
            StopCoroutine(rotationCoroutine);
        }
        
        var manualRule = new SlotRotationRule
        {
            targetRotation = rotation,
            useLocalRotation = true,
            addToCurrentRotation = false
        };
        
        rotationCoroutine = StartCoroutine(RotateToTarget(manualRule));
    }
    
    void OnDrawGizmosSelected()
    {
        if (!showDebugInfo) return;
        
        // 현재 슬롯 표시
        if (tangledPeg != null && tangledPeg.currentSlot != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, tangledPeg.currentSlot.transform.position);
            
            // 회전 방향 표시
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(transform.position, transform.forward * 0.5f);
            Gizmos.color = Color.red;
            Gizmos.DrawRay(transform.position, transform.right * 0.5f);
            Gizmos.color = Color.green;
            Gizmos.DrawRay(transform.position, transform.up * 0.5f);
        }
    }
} 
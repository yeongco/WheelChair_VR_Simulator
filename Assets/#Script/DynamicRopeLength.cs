using UnityEngine;
using Obi;

/// <summary>
/// 줄을 처음에는 짧게 시작하여 점진적으로 늘리는 컴포넌트
/// TanglePeg와 연결된 Rope의 길이를 게임 진행에 따라 조절합니다.
/// </summary>
[RequireComponent(typeof(ObiRope))]
[RequireComponent(typeof(ObiRopeCursor))]
public class DynamicRopeLength : MonoBehaviour
{
    [Header("줄 길이 설정")]
    [SerializeField] private float initialLength = 0.5f;          // 초기 줄 길이
    [SerializeField] private float maxLength = 3.0f;              // 최대 줄 길이
    [SerializeField] private float extensionSpeed = 0.5f;         // 줄이 늘어나는 속도 (초당)
    
    [Header("자동 확장 조건")]
    [SerializeField] private bool autoExtendEnabled = true;       // 자동 확장 활성화
    [SerializeField] private float extensionTriggerTime = 5f;     // 몇 초 후 확장 시작
    [SerializeField] private float extensionInterval = 3f;       // 확장 간격 (초)
    
    [Header("수동 제어")]
    [SerializeField] private KeyCode extendKey = KeyCode.E;       // 수동 확장 키
    [SerializeField] private KeyCode retractKey = KeyCode.Q;      // 수동 수축 키
    [SerializeField] private float manualSpeed = 1f;             // 수동 조절 속도
    
    [Header("디버그 정보")]
    [SerializeField] private bool showDebugInfo = true;
    
    private ObiRope rope;
    private ObiRopeCursor cursor;
    private float targetLength;
    private float lastExtensionTime;
    private float startTime;
    
    // 현재 줄 길이 (읽기 전용)
    public float CurrentLength => rope != null ? rope.restLength : 0f;
    
    // 목표 줄 길이 (읽기 전용)
    public float TargetLength => targetLength;
    
    // 최대 길이에 도달했는지 여부
    public bool IsMaxLength => Mathf.Approximately(targetLength, maxLength);

    void Start()
    {
        // 컴포넌트 초기화
        rope = GetComponent<ObiRope>();
        cursor = GetComponent<ObiRopeCursor>();
        
        if (rope == null || cursor == null)
        {
            Debug.LogError($"[DynamicRopeLength] {gameObject.name}: ObiRope 또는 ObiRopeCursor 컴포넌트를 찾을 수 없습니다!");
            enabled = false;
            return;
        }
        
        // 초기 설정
        targetLength = initialLength;
        startTime = Time.time;
        lastExtensionTime = startTime;
        
        // 초기 줄 길이 설정
        SetRopeLength(initialLength);
        
        Debug.Log($"[DynamicRopeLength] {gameObject.name}: 초기 줄 길이 {initialLength}m로 설정됨");
    }

    void Update()
    {
        if (rope == null || cursor == null) return;
        
        // 자동 확장 처리
        HandleAutoExtension();
        
        // 수동 제어 처리
        HandleManualControl();
        
        // 줄 길이 부드럽게 조절
        UpdateRopeLength();
        
        // 디버그 정보 표시
        if (showDebugInfo)
            ShowDebugInfo();
    }
    
    /// <summary>
    /// 자동 확장 처리
    /// </summary>
    private void HandleAutoExtension()
    {
        if (!autoExtendEnabled || IsMaxLength) return;
        
        float timeSinceStart = Time.time - startTime;
        float timeSinceLastExtension = Time.time - lastExtensionTime;
        
        // 처음 확장 조건 체크
        if (timeSinceStart >= extensionTriggerTime && 
            timeSinceLastExtension >= extensionInterval)
        {
            ExtendRope(extensionSpeed * extensionInterval);
            lastExtensionTime = Time.time;
        }
    }
    
    /// <summary>
    /// 수동 제어 처리
    /// </summary>
    private void HandleManualControl()
    {
        bool extending = Input.GetKey(extendKey);
        bool retracting = Input.GetKey(retractKey);
        
        if (extending && !retracting)
        {
            ExtendRope(manualSpeed * Time.deltaTime);
        }
        else if (retracting && !extending)
        {
            RetractRope(manualSpeed * Time.deltaTime);
        }
    }
    
    /// <summary>
    /// 줄 길이를 부드럽게 업데이트
    /// </summary>
    private void UpdateRopeLength()
    {
        float currentLength = rope.restLength;
        float lengthDifference = targetLength - currentLength;
        
        // 목표 길이와 현재 길이가 거의 같으면 업데이트 생략
        if (Mathf.Abs(lengthDifference) < 0.01f) return;
        
        // 부드럽게 길이 변경
        float changeAmount = Mathf.Sign(lengthDifference) * extensionSpeed * Time.deltaTime;
        
        // 목표를 초과하지 않도록 제한
        if (Mathf.Abs(changeAmount) > Mathf.Abs(lengthDifference))
            changeAmount = lengthDifference;
            
        cursor.ChangeLength(changeAmount);
    }
    
    /// <summary>
    /// 줄을 확장합니다
    /// </summary>
    /// <param name="amount">확장할 길이</param>
    public void ExtendRope(float amount)
    {
        if (amount <= 0) return;
        
        targetLength = Mathf.Min(targetLength + amount, maxLength);
        
        if (showDebugInfo)
            Debug.Log($"[DynamicRopeLength] {gameObject.name}: 줄 확장 - 목표 길이: {targetLength:F2}m");
    }
    
    /// <summary>
    /// 줄을 수축합니다
    /// </summary>
    /// <param name="amount">수축할 길이</param>
    public void RetractRope(float amount)
    {
        if (amount <= 0) return;
        
        targetLength = Mathf.Max(targetLength - amount, 0.1f); // 최소 길이 0.1m
        
        if (showDebugInfo)
            Debug.Log($"[DynamicRopeLength] {gameObject.name}: 줄 수축 - 목표 길이: {targetLength:F2}m");
    }
    
    /// <summary>
    /// 줄 길이를 즉시 설정합니다
    /// </summary>
    /// <param name="length">설정할 길이</param>
    public void SetRopeLength(float length)
    {
        targetLength = Mathf.Clamp(length, 0.1f, maxLength);
        
        // 즉시 변경
        float currentLength = rope.restLength;
        float changeAmount = targetLength - currentLength;
        cursor.ChangeLength(changeAmount);
        
        if (showDebugInfo)
            Debug.Log($"[DynamicRopeLength] {gameObject.name}: 줄 길이 즉시 설정: {targetLength:F2}m");
    }
    
    /// <summary>
    /// 자동 확장을 활성화/비활성화합니다
    /// </summary>
    /// <param name="enabled">활성화 여부</param>
    public void SetAutoExtensionEnabled(bool enabled)
    {
        autoExtendEnabled = enabled;
        if (enabled)
        {
            lastExtensionTime = Time.time;
        }
        
        Debug.Log($"[DynamicRopeLength] {gameObject.name}: 자동 확장 {(enabled ? "활성화" : "비활성화")}");
    }
    
    /// <summary>
    /// 확장 속도를 설정합니다
    /// </summary>
    /// <param name="speed">확장 속도 (초당 미터)</param>
    public void SetExtensionSpeed(float speed)
    {
        extensionSpeed = Mathf.Max(0.1f, speed);
        Debug.Log($"[DynamicRopeLength] {gameObject.name}: 확장 속도 설정: {extensionSpeed:F2}m/s");
    }
    
    /// <summary>
    /// 디버그 정보 표시
    /// </summary>
    private void ShowDebugInfo()
    {
        if (rope == null) return;
        
        // 화면에 정보 표시 (씬 뷰에서만 보임)
        Debug.DrawRay(transform.position, Vector3.up * CurrentLength, Color.green);
        Debug.DrawRay(transform.position + Vector3.right * 0.1f, Vector3.up * TargetLength, Color.red);
    }
    
    /// <summary>
    /// 게임 상황에 따른 확장 트리거
    /// </summary>
    public void TriggerProgressiveExtension()
    {
        if (IsMaxLength) return;
        
        ExtendRope(0.5f); // 0.5m씩 확장
        Debug.Log($"[DynamicRopeLength] {gameObject.name}: 진행 상황에 따른 줄 확장");
    }
    
    /// <summary>
    /// 줄을 초기 상태로 리셋
    /// </summary>
    public void ResetToInitialLength()
    {
        SetRopeLength(initialLength);
        startTime = Time.time;
        lastExtensionTime = startTime;
        
        Debug.Log($"[DynamicRopeLength] {gameObject.name}: 줄 길이 초기화");
    }
    
    void OnDrawGizmosSelected()
    {
        if (!showDebugInfo) return;
        
        // 현재 길이 표시 (녹색)
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position + Vector3.down * CurrentLength, 0.1f);
        
        // 목표 길이 표시 (빨간색)
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position + Vector3.down * TargetLength, 0.1f);
        
        // 최대 길이 표시 (파란색)
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position + Vector3.down * maxLength, 0.1f);
    }
} 
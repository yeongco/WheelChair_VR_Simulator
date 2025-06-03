using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 가상 경사로 시스템
/// 물리적 경사로 없이도 휠체어에 방향성 있는 힘을 적용할 수 있는 시스템
/// </summary>
public class VirtualSlope : MonoBehaviour
{
    [Header("🏔️ 가상 경사로 설정")]
    [SerializeField] private Vector3 slopeDirection = Vector3.forward;
    [SerializeField] private float slopeForce = 2f;
    [SerializeField] private bool normalizeDirection = true;
    [SerializeField] private bool ignoreYAxis = true;
    
    [Header("🎯 적용 범위")]
    [SerializeField] private LayerMask wheelchairLayer = -1;
    [SerializeField] private bool requireWheelchairController = true;
    
    [Header("🔍 디버그 설정")]
    [SerializeField] private bool enableDebugLog = true;
    [SerializeField] private bool showDirectionGizmos = true;
    [SerializeField] private float gizmoLength = 2f;
    [SerializeField] private Color gizmoColor = Color.red;
    
    // 현재 영향받는 휠체어들
    private HashSet<WheelchairController> affectedWheelchairs = new HashSet<WheelchairController>();
    
    // 내부 계산용 변수들
    private Vector3 normalizedSlopeDirection;
    private Collider triggerCollider;
    
    void Start()
    {
        InitializeVirtualSlope();
    }
    
    void InitializeVirtualSlope()
    {
        // 트리거 콜라이더 확인
        triggerCollider = GetComponent<Collider>();
        if (triggerCollider == null)
        {
            Debug.LogError($"⚠️ {gameObject.name}: VirtualSlope에 Collider가 없습니다! Trigger Collider를 추가해주세요.");
            return;
        }
        
        if (!triggerCollider.isTrigger)
        {
            triggerCollider.isTrigger = true;
            Debug.LogWarning($"⚠️ {gameObject.name}: Collider가 Trigger로 설정되지 않아 자동으로 설정했습니다.");
        }
        
        // 방향 정규화
        UpdateSlopeDirection();
        
        if (enableDebugLog)
        {
            Debug.Log($"🏔️ 가상 경사로 '{gameObject.name}' 초기화 완료");
            Debug.Log($"    방향: {normalizedSlopeDirection}, 힘: {slopeForce}");
        }
    }
    
    void UpdateSlopeDirection()
    {
        Vector3 direction = slopeDirection;
        
        // Y축 무시 옵션
        if (ignoreYAxis)
        {
            direction.y = 0f;
        }
        
        // 방향 정규화
        if (normalizeDirection && direction.magnitude > 0.001f)
        {
            normalizedSlopeDirection = direction.normalized;
        }
        else
        {
            normalizedSlopeDirection = direction;
        }
    }
    
    void OnTriggerEnter(Collider other)
    {
        // 레이어 체크
        if (!IsInLayerMask(other.gameObject.layer, wheelchairLayer))
            return;
            
        // 휠체어 컨트롤러 확인
        WheelchairController wheelchair = other.GetComponent<WheelchairController>();
        if (wheelchair == null)
        {
            if (requireWheelchairController)
                return;
                
            // 부모에서 찾아보기
            wheelchair = other.GetComponentInParent<WheelchairController>();
            if (wheelchair == null)
                return;
        }
        
        // 휠체어를 영향 목록에 추가
        if (affectedWheelchairs.Add(wheelchair))
        {
            wheelchair.AddVirtualSlope(this);
            
            if (enableDebugLog)
            {
                Debug.Log($"🏔️ 휠체어 '{wheelchair.gameObject.name}'이 가상 경사로 '{gameObject.name}'에 진입");
            }
        }
    }
    
    void OnTriggerExit(Collider other)
    {
        // 레이어 체크
        if (!IsInLayerMask(other.gameObject.layer, wheelchairLayer))
            return;
            
        // 휠체어 컨트롤러 확인
        WheelchairController wheelchair = other.GetComponent<WheelchairController>();
        if (wheelchair == null)
        {
            wheelchair = other.GetComponentInParent<WheelchairController>();
            if (wheelchair == null)
                return;
        }
        
        // 휠체어를 영향 목록에서 제거
        if (affectedWheelchairs.Remove(wheelchair))
        {
            wheelchair.RemoveVirtualSlope(this);
            
            if (enableDebugLog)
            {
                Debug.Log($"🏔️ 휠체어 '{wheelchair.gameObject.name}'이 가상 경사로 '{gameObject.name}'에서 퇴장");
            }
        }
    }
    
    /// <summary>
    /// 특정 휠체어에 대한 경사로 효과 계산
    /// </summary>
    /// <param name="wheelchairTransform">휠체어 Transform</param>
    /// <returns>계산된 경사로 효과 (Z 변화량)</returns>
    public float CalculateSlopeEffect(Transform wheelchairTransform)
    {
        if (wheelchairTransform == null || normalizedSlopeDirection.magnitude < 0.001f)
            return 0f;
        
        // 휠체어의 전진 방향 (글로벌)
        Vector3 wheelchairForward = wheelchairTransform.forward;
        
        // Y축 무시
        if (ignoreYAxis)
        {
            wheelchairForward.y = 0f;
            wheelchairForward = wheelchairForward.normalized;
        }
        
        // 내적 계산 (방향 일치도: -1 ~ 1)
        float directionDot = Vector3.Dot(wheelchairForward, normalizedSlopeDirection);
        
        // 경사로 효과 계산 (방향이 일치할 때 최대, 수직일 때 0)
        float slopeEffect = directionDot * slopeForce;
        
        return slopeEffect;
    }
    
    /// <summary>
    /// 레이어 마스크 체크
    /// </summary>
    private bool IsInLayerMask(int layer, LayerMask layerMask)
    {
        return (layerMask.value & (1 << layer)) != 0;
    }
    
    // ========== 공개 API ==========
    
    /// <summary>
    /// 경사로 방향 설정
    /// </summary>
    public void SetSlopeDirection(Vector3 direction)
    {
        slopeDirection = direction;
        UpdateSlopeDirection();
        
        if (enableDebugLog)
        {
            Debug.Log($"🏔️ '{gameObject.name}' 경사로 방향 설정: {normalizedSlopeDirection}");
        }
    }
    
    /// <summary>
    /// 경사로 힘 설정
    /// </summary>
    public void SetSlopeForce(float force)
    {
        slopeForce = force;
        
        if (enableDebugLog)
        {
            Debug.Log($"🏔️ '{gameObject.name}' 경사로 힘 설정: {slopeForce}");
        }
    }
    
    /// <summary>
    /// 현재 영향받는 휠체어 수
    /// </summary>
    public int GetAffectedWheelchairCount()
    {
        return affectedWheelchairs.Count;
    }
    
    /// <summary>
    /// 현재 설정 정보 반환
    /// </summary>
    public (Vector3 direction, float force, int affectedCount) GetSlopeInfo()
    {
        return (normalizedSlopeDirection, slopeForce, affectedWheelchairs.Count);
    }
    
    /// <summary>
    /// 경사로 상태 디버그 출력
    /// </summary>
    [ContextMenu("Debug Slope Info")]
    public void DebugSlopeInfo()
    {
        Debug.Log("═══════════════════════════════════════");
        Debug.Log($"🏔️ 가상 경사로 '{gameObject.name}' 정보");
        Debug.Log("═══════════════════════════════════════");
        Debug.Log($"📐 원본 방향: {slopeDirection}");
        Debug.Log($"🎯 정규화된 방향: {normalizedSlopeDirection}");
        Debug.Log($"⚡ 경사로 힘: {slopeForce}");
        Debug.Log($"🚗 영향받는 휠체어 수: {affectedWheelchairs.Count}");
        Debug.Log($"🔧 Y축 무시: {ignoreYAxis}, 방향 정규화: {normalizeDirection}");
        
        if (affectedWheelchairs.Count > 0)
        {
            Debug.Log("📋 영향받는 휠체어들:");
            foreach (var wheelchair in affectedWheelchairs)
            {
                if (wheelchair != null)
                {
                    float effect = CalculateSlopeEffect(wheelchair.transform);
                    Vector3 wheelchairForward = wheelchair.transform.forward;
                    if (ignoreYAxis) wheelchairForward.y = 0f;
                    float dot = Vector3.Dot(wheelchairForward.normalized, normalizedSlopeDirection);
                    
                    Debug.Log($"  • {wheelchair.gameObject.name}: 효과 {effect:F2}, 방향 일치도 {dot:F2}");
                }
            }
        }
        Debug.Log("═══════════════════════════════════════");
    }
    
    /// <summary>
    /// 모든 휠체어에 대한 효과 즉시 테스트
    /// </summary>
    [ContextMenu("Test Slope Effects")]
    public void TestSlopeEffects()
    {
        if (affectedWheelchairs.Count == 0)
        {
            Debug.Log($"⚠️ '{gameObject.name}': 영향받는 휠체어가 없습니다.");
            return;
        }
        
        Debug.Log($"🧪 '{gameObject.name}' 경사로 효과 테스트:");
        foreach (var wheelchair in affectedWheelchairs)
        {
            if (wheelchair != null)
            {
                float effect = CalculateSlopeEffect(wheelchair.transform);
                wheelchair.ApplyVirtualSlopeForce(effect);
                Debug.Log($"  📤 {wheelchair.gameObject.name}에 효과 {effect:F2} 적용");
            }
        }
    }
    
    // ========== 디버그 및 기즈모 ==========
    
    void OnDrawGizmos()
    {
        if (!showDirectionGizmos) return;
        
        // 방향 업데이트 (에디터에서 실시간 반영)
        UpdateSlopeDirection();
        
        // 경사로 방향 화살표 (빨간색)
        Gizmos.color = gizmoColor;
        Vector3 startPos = transform.position;
        Vector3 endPos = startPos + normalizedSlopeDirection * gizmoLength;
        
        // 메인 화살표
        Gizmos.DrawLine(startPos, endPos);
        
        // 화살표 머리
        if (normalizedSlopeDirection.magnitude > 0.001f)
        {
            Vector3 arrowHead1 = endPos - (normalizedSlopeDirection + Vector3.right * 0.3f).normalized * (gizmoLength * 0.2f);
            Vector3 arrowHead2 = endPos - (normalizedSlopeDirection + Vector3.left * 0.3f).normalized * (gizmoLength * 0.2f);
            Vector3 arrowHead3 = endPos - (normalizedSlopeDirection + Vector3.forward * 0.3f).normalized * (gizmoLength * 0.2f);
            Vector3 arrowHead4 = endPos - (normalizedSlopeDirection + Vector3.back * 0.3f).normalized * (gizmoLength * 0.2f);
            
            Gizmos.DrawLine(endPos, arrowHead1);
            Gizmos.DrawLine(endPos, arrowHead2);
            Gizmos.DrawLine(endPos, arrowHead3);
            Gizmos.DrawLine(endPos, arrowHead4);
        }
        
        // 힘 크기 표시 (구 크기로)
        Gizmos.color = new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, 0.3f);
        float sphereSize = Mathf.Clamp(slopeForce * 0.1f, 0.1f, 1f);
        Gizmos.DrawSphere(startPos, sphereSize);
        
        // 트리거 영역 표시 (와이어프레임)
        if (triggerCollider != null)
        {
            Gizmos.color = new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, 0.5f);
            
            if (triggerCollider is BoxCollider box)
            {
                Gizmos.matrix = transform.localToWorldMatrix;
                Gizmos.DrawWireCube(box.center, box.size);
                Gizmos.matrix = Matrix4x4.identity;
            }
            else if (triggerCollider is SphereCollider sphere)
            {
                Gizmos.matrix = transform.localToWorldMatrix;
                Gizmos.DrawWireSphere(sphere.center, sphere.radius);
                Gizmos.matrix = Matrix4x4.identity;
            }
        }
    }
    
    void OnDrawGizmosSelected()
    {
        if (!showDirectionGizmos) return;
        
        // 선택되었을 때 더 자세한 정보 표시
        Gizmos.color = Color.yellow;
        
        // 영향받는 휠체어들과의 연결선
        foreach (var wheelchair in affectedWheelchairs)
        {
            if (wheelchair != null)
            {
                Gizmos.DrawLine(transform.position, wheelchair.transform.position);
                
                // 휠체어의 방향도 표시
                Vector3 wheelchairForward = wheelchair.transform.forward;
                if (ignoreYAxis) wheelchairForward.y = 0f;
                
                Gizmos.color = Color.cyan;
                Gizmos.DrawRay(wheelchair.transform.position, wheelchairForward.normalized * (gizmoLength * 0.5f));
                Gizmos.color = Color.yellow;
            }
        }
    }
    
    // ========== 에디터 검증 ==========
    
    void OnValidate()
    {
        // 설정값 검증
        slopeForce = Mathf.Max(0f, slopeForce);
        gizmoLength = Mathf.Max(0.1f, gizmoLength);
        
        // 실행 중일 때만 방향 업데이트
        if (Application.isPlaying)
        {
            UpdateSlopeDirection();
        }
        
        // 경고 메시지
        if (slopeForce > 10f)
        {
            Debug.LogWarning($"⚠️ '{gameObject.name}': 경사로 힘이 너무 높습니다 ({slopeForce}). 권장값: 0~5");
        }
        
        if (slopeDirection.magnitude < 0.001f)
        {
            Debug.LogWarning($"⚠️ '{gameObject.name}': 경사로 방향이 설정되지 않았습니다.");
        }
    }
} 
using UnityEngine;
using Obi;

/// <summary>
/// VR 휠체어 시뮬레이터를 위한 안전벨트 시스템
/// ObiRopeReel을 활용하여 실제 안전벨트처럼 동작
/// </summary>
[RequireComponent(typeof(ObiRope))]
[RequireComponent(typeof(ObiRopeCursor))]
[RequireComponent(typeof(ObiRopeReel))]
public class VRSeatbeltSystem : MonoBehaviour
{
    [Header("안전벨트 설정")]
    [SerializeField] private float restingTension = 0.1f;      // 평상시 장력
    [SerializeField] private float maxExtension = 0.8f;        // 최대 확장 거리
    [SerializeField] private float retractSpeed = 1.5f;       // 당김 속도
    [SerializeField] private float extendSpeed = 2.0f;        // 풀림 속도
    
    [Header("안전 제동 설정")]
    [SerializeField] private bool emergencyBrakeEnabled = true; // 비상 제동 활성화
    [SerializeField] private float brakeThreshold = 3.0f;      // 비상 제동 발동 속도
    [SerializeField] private float brakeForce = 10.0f;        // 제동력
    
    [Header("사용자 추적")]
    [SerializeField] private Transform userChest;              // 사용자 가슴 위치
    [SerializeField] private Transform seatbeltAnchor;         // 안전벨트 고정점
    
    [Header("피드백")]
    [SerializeField] private bool hapticFeedback = true;       // 햅틱 피드백
    [SerializeField] private AudioClip lockSound;             // 잠금 소리
    [SerializeField] private AudioClip unlockSound;           // 해제 소리
    
    private ObiRope rope;
    private ObiRopeCursor cursor;
    private ObiRopeReel reel;
    private AudioSource audioSource;
    
    private bool isLocked = false;
    private Vector3 lastUserPosition;
    private float userVelocity;
    
    // 안전벨트 상태
    public bool IsLocked => isLocked;
    public float CurrentTension => rope != null ? rope.CalculateLength() - rope.restLength : 0f;
    public float ExtensionRatio => CurrentTension / maxExtension;

    void Start()
    {
        InitializeComponents();
        SetupSeatbelt();
        
        if (userChest == null)
        {
            Debug.LogWarning("[VRSeatbeltSystem] 사용자 가슴 위치(userChest)가 설정되지 않았습니다. VR 헤드셋 또는 컨트롤러를 할당해주세요.");
        }
    }
    
    void InitializeComponents()
    {
        rope = GetComponent<ObiRope>();
        cursor = GetComponent<ObiRopeCursor>();
        reel = GetComponent<ObiRopeReel>();
        audioSource = GetComponent<AudioSource>();
        
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
    }
    
    void SetupSeatbelt()
    {
        // ObiRopeReel 설정 - 실제 안전벨트처럼 작동
        reel.outThreshold = restingTension;        // 이 정도 당겨지면 풀어줌
        reel.inThreshold = restingTension * 0.5f;  // 이 정도 느슨해지면 당김
        reel.outSpeed = extendSpeed;               // 풀리는 속도
        reel.inSpeed = retractSpeed;               // 당겨지는 속도
        reel.maxLength = maxExtension;             // 최대 확장 거리
        
        Debug.Log("[VRSeatbeltSystem] 안전벨트 시스템 초기화 완료");
    }

    void Update()
    {
        if (userChest != null)
        {
            TrackUserMovement();
            CheckEmergencyBrake();
        }
        
        HandleSeatbeltControls();
        UpdateTensionFeedback();
    }
    
    /// <summary>
    /// 사용자 움직임 추적
    /// </summary>
    void TrackUserMovement()
    {
        Vector3 currentPosition = userChest.position;
        userVelocity = Vector3.Distance(currentPosition, lastUserPosition) / Time.deltaTime;
        lastUserPosition = currentPosition;
        
        // 사용자와 안전벨트 고정점 사이의 거리 계산
        if (seatbeltAnchor != null)
        {
            float distanceToAnchor = Vector3.Distance(userChest.position, seatbeltAnchor.position);
            
            // 거리가 늘어나면 벨트가 풀려야 하고, 줄어들면 당겨져야 함
            // ObiRopeReel이 자동으로 처리하므로 여기서는 모니터링만
        }
    }
    
    /// <summary>
    /// 비상 제동 시스템
    /// </summary>
    void CheckEmergencyBrake()
    {
        if (!emergencyBrakeEnabled || !isLocked) return;
        
        // 갑작스런 움직임 감지
        if (userVelocity > brakeThreshold)
        {
            ActivateEmergencyBrake();
        }
    }
    
    /// <summary>
    /// 비상 제동 발동
    /// </summary>
    void ActivateEmergencyBrake()
    {
        // ObiRopeReel의 속도를 급격히 줄여서 제동 효과
        float originalOutSpeed = reel.outSpeed;
        reel.outSpeed = 0.1f; // 거의 풀리지 않게
        
        Debug.Log("[VRSeatbeltSystem] 비상 제동 발동! 사용자 속도: " + userVelocity.ToString("F2"));
        
        // 햅틱 피드백
        if (hapticFeedback)
            TriggerHapticFeedback();
        
        // 일정 시간 후 원래 속도로 복원
        Invoke(nameof(RestoreNormalSpeed), 0.5f);
    }
    
    void RestoreNormalSpeed()
    {
        reel.outSpeed = extendSpeed;
        Debug.Log("[VRSeatbeltSystem] 정상 속도 복원");
    }
    
    /// <summary>
    /// 안전벨트 수동 제어
    /// </summary>
    void HandleSeatbeltControls()
    {
        // 스페이스바로 안전벨트 잠금/해제
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (isLocked)
                UnlockSeatbelt();
            else
                LockSeatbelt();
        }
        
        // L키로 잠금, U키로 해제
        if (Input.GetKeyDown(KeyCode.L))
            LockSeatbelt();
        if (Input.GetKeyDown(KeyCode.U))
            UnlockSeatbelt();
    }
    
    /// <summary>
    /// 안전벨트 잠금
    /// </summary>
    public void LockSeatbelt()
    {
        if (isLocked) return;
        
        isLocked = true;
        reel.enabled = true; // 자동 조절 활성화
        
        PlaySound(lockSound);
        Debug.Log("[VRSeatbeltSystem] 안전벨트 잠금");
        
        if (hapticFeedback)
            TriggerHapticFeedback();
    }
    
    /// <summary>
    /// 안전벨트 해제
    /// </summary>
    public void UnlockSeatbelt()
    {
        if (!isLocked) return;
        
        isLocked = false;
        reel.enabled = false; // 자동 조절 비활성화
        
        PlaySound(unlockSound);
        Debug.Log("[VRSeatbeltSystem] 안전벨트 해제");
        
        if (hapticFeedback)
            TriggerHapticFeedback();
    }
    
    /// <summary>
    /// 장력에 따른 피드백 제공
    /// </summary>
    void UpdateTensionFeedback()
    {
        if (!isLocked) return;
        
        float tension = CurrentTension;
        
        // 장력이 높을 때 시각적/청각적 피드백
        if (tension > maxExtension * 0.8f)
        {
            // 경고: 안전벨트가 너무 늘어남
            Debug.LogWarning("[VRSeatbeltSystem] 안전벨트 장력 경고: " + tension.ToString("F2"));
        }
    }
    
    /// <summary>
    /// 햅틱 피드백 트리거
    /// </summary>
    void TriggerHapticFeedback()
    {
        // VR 컨트롤러 진동 구현
        // 실제 구현에서는 VR SDK의 햅틱 API 사용
        Debug.Log("[VRSeatbeltSystem] 햅틱 피드백 트리거");
    }
    
    /// <summary>
    /// 사운드 재생
    /// </summary>
    void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
            audioSource.PlayOneShot(clip);
    }
    
    /// <summary>
    /// 안전벨트 길이를 수동으로 조절 (비상시에만)
    /// </summary>
    /// <param name="lengthChange">변경할 길이</param>
    public void ManualAdjustLength(float lengthChange)
    {
        if (isLocked && cursor != null)
        {
            cursor.ChangeLength(lengthChange);
            Debug.Log($"[VRSeatbeltSystem] 수동 길이 조절: {lengthChange:F2}m");
        }
    }
    
    void OnDrawGizmosSelected()
    {
        if (userChest != null && seatbeltAnchor != null)
        {
            // 사용자와 고정점 사이의 거리 시각화
            Gizmos.color = isLocked ? Color.red : Color.gray;
            Gizmos.DrawLine(userChest.position, seatbeltAnchor.position);
            
            // 최대 확장 범위 표시
            Gizmos.color = Color.yellow;
            if (seatbeltAnchor != null)
                Gizmos.DrawWireSphere(seatbeltAnchor.position, maxExtension);
        }
        
        // 현재 장력 표시
        if (Application.isPlaying && rope != null)
        {
            Gizmos.color = Color.green;
            Vector3 tensionPos = transform.position + Vector3.up * CurrentTension;
            Gizmos.DrawWireCube(tensionPos, Vector3.one * 0.1f);
        }
    }
} 
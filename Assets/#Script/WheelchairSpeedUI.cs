using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class WheelchairSpeedUI : MonoBehaviour
{
    [Header("UI 설정")]
    [SerializeField] private TextMeshProUGUI speedText;        // 속도 표시 텍스트
    [SerializeField] private Transform speedMeter;            // 속도계 이미지 (선택사항)
    [SerializeField] private float updateInterval = 0.1f;     // UI 업데이트 간격
    
    [Header("속도계 설정")]
    [SerializeField] private float maxSpeed = 5f;             // 최대 속도 (속도계 최대값)
    [SerializeField] private bool showMPH = false;            // MPH 단위로 표시 (false면 m/s)
    [SerializeField] private bool showDecimal = true;         // 소수점 표시 여부
    
    private Rigidbody wheelchairBody;
    private float currentSpeed = 0f;
    private float updateTimer = 0f;
    
    void Start()
    {
        // 휠체어 본체의 Rigidbody 가져오기
        wheelchairBody = GetComponent<Rigidbody>();
        
        // TextMeshPro 컴포넌트가 없으면 자동 생성
        if (speedText == null)
        {
            GameObject textObj = new GameObject("SpeedText");
            textObj.transform.SetParent(transform);
            textObj.transform.localPosition = Vector3.zero;
            
            speedText = textObj.AddComponent<TextMeshProUGUI>();
            speedText.alignment = TextAlignmentOptions.Center;
            speedText.fontSize = 36;
            speedText.color = Color.white;
            
            // Canvas 설정
            Canvas canvas = textObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = Camera.main;
            
            // Canvas Scaler 설정
            CanvasScaler scaler = textObj.AddComponent<CanvasScaler>();
            scaler.dynamicPixelsPerUnit = 100f;
            
            // RectTransform 설정
            RectTransform rect = textObj.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(200, 100);
            rect.localPosition = new Vector3(0, 0, 2f); // 카메라 앞에 배치
        }
    }
    
    void Update()
    {
        if (wheelchairBody == null || speedText == null) return;
        
        // 업데이트 타이머
        updateTimer += Time.deltaTime;
        if (updateTimer < updateInterval) return;
        updateTimer = 0f;
        
        // 현재 속도 계산 (Y축 속도 제외)
        Vector3 horizontalVelocity = new Vector3(wheelchairBody.velocity.x, 0, wheelchairBody.velocity.z);
        currentSpeed = horizontalVelocity.magnitude;
        
        // 속도 표시 업데이트
        UpdateSpeedDisplay();
        
        // 속도계 회전 (선택사항)
        if (speedMeter != null)
        {
            float speedRatio = Mathf.Clamp01(currentSpeed / maxSpeed);
            speedMeter.localRotation = Quaternion.Euler(0, 0, -180f * speedRatio);
        }
    }
    
    void UpdateSpeedDisplay()
    {
        // 속도 단위 변환 (m/s to mph)
        float displaySpeed = showMPH ? currentSpeed * 2.237f : currentSpeed;
        
        // 소수점 표시 여부에 따라 포맷 설정
        string speedFormat = showDecimal ? "F1" : "F0";
        string unit = showMPH ? " MPH" : " m/s";
        
        // 텍스트 업데이트
        speedText.text = displaySpeed.ToString(speedFormat) + unit;
        
        // 속도에 따른 색상 변경 (선택사항)
        float speedRatio = Mathf.Clamp01(currentSpeed / maxSpeed);
        speedText.color = Color.Lerp(Color.green, Color.red, speedRatio);
    }
    
    // 에디터에서 시각화 (선택 사항)
    void OnDrawGizmosSelected()
    {
        if (wheelchairBody != null)
        {
            // 현재 속도 표시
            Vector3 horizontalVelocity = new Vector3(wheelchairBody.velocity.x, 0, wheelchairBody.velocity.z);
            float currentSpeed = horizontalVelocity.magnitude;
            
            // 속도에 따른 색상 계산
            float speedRatio = Mathf.Clamp01(currentSpeed / maxSpeed);
            Color speedColor = Color.Lerp(Color.green, Color.red, speedRatio);
            
            // 속도 벡터 시각화
            Gizmos.color = speedColor;
            Gizmos.DrawRay(transform.position, horizontalVelocity.normalized * currentSpeed);
        }
    }
} 
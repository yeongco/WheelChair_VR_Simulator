using UnityEngine;
using Obi;

/// <summary>
/// ObiRopeCursor의 cursorMu, sourceMu, direction 매개변수 활용 예시
/// </summary>
[RequireComponent(typeof(ObiRope))]
[RequireComponent(typeof(ObiRopeCursor))]
public class RopeCursorExample : MonoBehaviour
{
    [Header("커서 설정 테스트")]
    [Range(0f, 1f)]
    [SerializeField] private float testCursorMu = 1.0f;
    
    [Range(0f, 1f)]
    [SerializeField] private float testSourceMu = 0.0f;
    
    [SerializeField] private bool testDirection = true;
    
    [Header("안전벨트 시나리오")]
    [SerializeField] private CursorConfiguration seatbeltConfig;
    
    [Header("갈고리/윈치 시나리오")]
    [SerializeField] private CursorConfiguration winchConfig;
    
    [Header("로프 클라이밍 시나리오")]
    [SerializeField] private CursorConfiguration climbingConfig;
    
    private ObiRope rope;
    private ObiRopeCursor cursor;

    [System.Serializable]
    public class CursorConfiguration
    {
        [Range(0f, 1f)] public float cursorMu = 1.0f;
        [Range(0f, 1f)] public float sourceMu = 0.0f;
        public bool direction = true;
        public string description;
    }

    void Start()
    {
        rope = GetComponent<ObiRope>();
        cursor = GetComponent<ObiRopeCursor>();
        
        InitializeConfigurations();
        ApplyConfiguration(seatbeltConfig);
    }
    
    void InitializeConfigurations()
    {
        // 안전벨트 설정: 끝에서 사용자 쪽으로 확장
        seatbeltConfig = new CursorConfiguration
        {
            cursorMu = 1.0f,     // 끝점에서 변경
            sourceMu = 0.0f,     // 시작점 속성 복사
            direction = true,    // 사용자 방향으로 확장
            description = "안전벨트: 고정점에서 사용자 방향으로 늘어남"
        };
        
        // 윈치/갈고리 설정: 중간에서 양방향 확장
        winchConfig = new CursorConfiguration
        {
            cursorMu = 0.5f,     // 중간점에서 변경
            sourceMu = 0.5f,     // 중간 속성 복사
            direction = true,    // 갈고리 방향으로 확장
            description = "윈치: 중간에서 갈고리 방향으로 늘어남"
        };
        
        // 로프 클라이밍 설정: 시작점에서 위로 확장
        climbingConfig = new CursorConfiguration
        {
            cursorMu = 0.0f,     // 시작점에서 변경
            sourceMu = 1.0f,     // 끝점 속성 복사
            direction = false,   // 위쪽으로 확장
            description = "클라이밍: 상단에서 위로 늘어남"
        };
    }

    void Update()
    {
        // 실시간 테스트
        cursor.cursorMu = testCursorMu;
        cursor.sourceMu = testSourceMu;
        cursor.direction = testDirection;
        
        HandleInput();
        ShowDebugInfo();
    }
    
    void HandleInput()
    {
        // 숫자 키로 다른 설정 적용
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            ApplyConfiguration(seatbeltConfig);
            Debug.Log("안전벨트 설정 적용: " + seatbeltConfig.description);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            ApplyConfiguration(winchConfig);
            Debug.Log("윈치 설정 적용: " + winchConfig.description);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            ApplyConfiguration(climbingConfig);
            Debug.Log("클라이밍 설정 적용: " + climbingConfig.description);
        }
        
        // E/Q 키로 길이 조절
        if (Input.GetKey(KeyCode.E))
        {
            cursor.ChangeLength(1.0f * Time.deltaTime);
        }
        else if (Input.GetKey(KeyCode.Q))
        {
            cursor.ChangeLength(-1.0f * Time.deltaTime);
        }
        
        // R 키로 리셋
        if (Input.GetKeyDown(KeyCode.R))
        {
            ResetRope();
        }
    }
    
    void ApplyConfiguration(CursorConfiguration config)
    {
        cursor.cursorMu = config.cursorMu;
        cursor.sourceMu = config.sourceMu;
        cursor.direction = config.direction;
        
        // 인스펙터 값도 동기화
        testCursorMu = config.cursorMu;
        testSourceMu = config.sourceMu;
        testDirection = config.direction;
    }
    
    void ResetRope()
    {
        // 줄을 원래 길이로 리셋
        float currentLength = rope.restLength;
        float originalLength = 2.0f; // 기본 길이
        float difference = originalLength - currentLength;
        cursor.ChangeLength(difference);
        
        Debug.Log("줄 길이 리셋: " + originalLength + "m");
    }
    
    void ShowDebugInfo()
    {
        if (rope == null) return;
        
        // 커서 위치 시각화
        Vector3 cursorPosition = GetCursorWorldPosition();
        Debug.DrawRay(cursorPosition, Vector3.up * 0.5f, Color.red);
        
        // 소스 위치 시각화
        Vector3 sourcePosition = GetSourceWorldPosition();
        Debug.DrawRay(sourcePosition, Vector3.up * 0.3f, Color.blue);
        
        // 방향 시각화
        Vector3 directionVector = testDirection ? Vector3.right : Vector3.left;
        Debug.DrawRay(cursorPosition, directionVector * 0.5f, Color.green);
    }
    
    Vector3 GetCursorWorldPosition()
    {
        if (rope.elements == null || rope.elements.Count == 0) return transform.position;
        
        int elementIndex = Mathf.FloorToInt(testCursorMu * (rope.elements.Count - 1));
        elementIndex = Mathf.Clamp(elementIndex, 0, rope.elements.Count - 1);
        
        var element = rope.elements[elementIndex];
        Vector3 pos1 = rope.solver.positions[element.particle1];
        Vector3 pos2 = rope.solver.positions[element.particle2];
        
        return Vector3.Lerp(pos1, pos2, testCursorMu);
    }
    
    Vector3 GetSourceWorldPosition()
    {
        if (rope.elements == null || rope.elements.Count == 0) return transform.position;
        
        int elementIndex = Mathf.FloorToInt(testSourceMu * (rope.elements.Count - 1));
        elementIndex = Mathf.Clamp(elementIndex, 0, rope.elements.Count - 1);
        
        var element = rope.elements[elementIndex];
        Vector3 pos1 = rope.solver.positions[element.particle1];
        Vector3 pos2 = rope.solver.positions[element.particle2];
        
        return Vector3.Lerp(pos1, pos2, testSourceMu);
    }
    
#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying) return;
        
        // 커서 위치 (빨간색 구)
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(GetCursorWorldPosition(), 0.1f);
        
        // 소스 위치 (파란색 구)
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(GetSourceWorldPosition(), 0.08f);
        
        // 방향 표시 (녹색 화살표)
        Gizmos.color = Color.green;
        Vector3 cursorPos = GetCursorWorldPosition();
        Vector3 directionVec = testDirection ? Vector3.right : Vector3.left;
        Gizmos.DrawRay(cursorPos, directionVec * 0.3f);
        
        // 정보 텍스트 (Scene 뷰에서 보임)
        UnityEditor.Handles.Label(cursorPos + Vector3.up * 0.3f, 
            $"Cursor: {testCursorMu:F2}\nSource: {testSourceMu:F2}\nDir: {(testDirection ? "→" : "←")}");
    }
}

// 커스텀 에디터로 더 나은 시각화화
[UnityEditor.CustomEditor(typeof(RopeCursorExample))]
public class RopeCursorExampleEditor : UnityEditor.Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        
        UnityEditor.EditorGUILayout.Space();
        UnityEditor.EditorGUILayout.LabelField("컨트롤", UnityEditor.EditorStyles.boldLabel);
        UnityEditor.EditorGUILayout.LabelField("1: 안전벨트 설정");
        UnityEditor.EditorGUILayout.LabelField("2: 윈치 설정");
        UnityEditor.EditorGUILayout.LabelField("3: 클라이밍 설정");
        UnityEditor.EditorGUILayout.LabelField("E: 확장, Q: 수축, R: 리셋");
        
        UnityEditor.EditorGUILayout.Space();
        var example = (RopeCursorExample)target;
        
        if (UnityEditor.EditorApplication.isPlaying)
        {
            UnityEditor.EditorGUILayout.LabelField("현재 줄 길이", example.GetComponent<ObiRope>().restLength.ToString("F2") + "m");
        }
    }
    #endif 
}
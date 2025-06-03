using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 휠체어 동작 테스트를 위한 스크립트
/// 인스펙터에서 바퀴 Z변화량을 직접 설정하여 휠체어 이동을 테스트할 수 있습니다.
/// </summary>
public class WheelchairTest : MonoBehaviour
{
    [Header("🎯 휠체어 참조")]
    public WheelchairController wheelchair;
    public bool autoFindWheelchair = true; // 자동으로 휠체어 찾기
    
    [Header("🎮 바퀴 Z변화량 설정")]
    [Range(-10f, 10f)]
    public float leftWheelDeltaZ = 0f; // 왼쪽 바퀴 Z변화량 (도/프레임)
    [Range(-10f, 10f)]
    public float rightWheelDeltaZ = 0f; // 오른쪽 바퀴 Z변화량 (도/프레임)
    
    [Header("🔄 실시간 제어")]
    public bool enableRealTimeControl = true; // 실시간 제어 활성화
    public bool continuousApplication = false; // 지속적 적용 (체크하면 매 프레임 적용)
    
    [Header("🧪 테스트 설정")]
    public float testDuration = 3f; // 각 테스트 지속 시간
    public float testIntensity = 2f; // 테스트 강도
    public bool showTestResults = true; // 테스트 결과 표시
    public bool enableAutoTest = false; // 자동 테스트 활성화
    
    [Header("📊 상태 모니터링")]
    public bool showCurrentStatus = true; // 현재 상태 표시
    public float statusUpdateInterval = 0.5f; // 상태 업데이트 간격
    
    [Header("⚙️ 고급 설정")]
    public bool resetOnStart = true; // 시작 시 휠체어 초기화
    public bool stopOnDisable = true; // 비활성화 시 정지
    
    // 테스트 상태 변수들
    private bool isTestRunning = false;
    private string currentTestName = "";
    private float testStartTime = 0f;
    private Vector3 testStartPosition = Vector3.zero;
    private float testStartRotation = 0f;
    
    // 이전 값들 (변화 감지용)
    private float prevLeftDeltaZ = 0f;
    private float prevRightDeltaZ = 0f;
    
    // 자동 테스트 관련
    private Coroutine autoTestCoroutine = null;
    private int currentAutoTestIndex = 0;
    
    // 상태 모니터링
    private float lastStatusUpdate = 0f;
    
    void Start()
    {
        InitializeTest();
    }
    
    void InitializeTest()
    {
        // 휠체어 자동 찾기
        if (autoFindWheelchair && wheelchair == null)
        {
            wheelchair = FindObjectOfType<WheelchairController>();
            if (wheelchair != null)
            {
                Debug.Log($"🎯 휠체어 자동 발견: {wheelchair.gameObject.name}");
            }
            else
            {
                Debug.LogWarning("⚠️ 휠체어를 찾을 수 없습니다!");
                return;
            }
        }
        
        if (wheelchair == null)
        {
            Debug.LogError("❌ 휠체어 참조가 설정되지 않았습니다!");
            return;
        }
        
        // 초기화
        if (resetOnStart)
        {
            wheelchair.StopWheels();
            ResetTestState();
        }
        
        // 초기값 저장
        prevLeftDeltaZ = leftWheelDeltaZ;
        prevRightDeltaZ = rightWheelDeltaZ;
        
        Debug.Log("🧪 휠체어 테스트 시스템 초기화 완료");
        
        // 자동 테스트 시작
        if (enableAutoTest)
        {
            StartAutoTest();
        }
    }
    
    void Update()
    {
        if (wheelchair == null) return;
        
        // 실시간 제어
        if (enableRealTimeControl)
        {
            ProcessRealTimeControl();
        }
        
        // 상태 모니터링
        if (showCurrentStatus && Time.time - lastStatusUpdate > statusUpdateInterval)
        {
            UpdateStatusDisplay();
            lastStatusUpdate = Time.time;
        }
        
        // 테스트 진행 상황 확인
        if (isTestRunning)
        {
            UpdateTestProgress();
        }
    }
    
    void ProcessRealTimeControl()
    {
        // 값 변화 감지
        bool leftChanged = Mathf.Abs(leftWheelDeltaZ - prevLeftDeltaZ) > 0.001f;
        bool rightChanged = Mathf.Abs(rightWheelDeltaZ - prevRightDeltaZ) > 0.001f;
        
        // 지속적 적용 또는 값 변화 시 적용
        if (continuousApplication || leftChanged || rightChanged)
        {
            wheelchair.SetLeftWheelDeltaZ(leftWheelDeltaZ);
            wheelchair.SetRightWheelDeltaZ(rightWheelDeltaZ);
            
            if (leftChanged || rightChanged)
            {
                LogMovementChange();
            }
        }
        
        // 이전 값 업데이트
        prevLeftDeltaZ = leftWheelDeltaZ;
        prevRightDeltaZ = rightWheelDeltaZ;
    }
    
    void LogMovementChange()
    {
        string movementType = DetermineMovementType(leftWheelDeltaZ, rightWheelDeltaZ);
        Debug.Log($"🎮 실시간 제어 - {movementType}: L{leftWheelDeltaZ:F1} R{rightWheelDeltaZ:F1}");
    }
    
    string DetermineMovementType(float left, float right)
    {
        float threshold = 0.1f;
        
        if (Mathf.Abs(left) < threshold && Mathf.Abs(right) < threshold)
            return "정지";
        
        if (Mathf.Abs(left - (-right)) < threshold) // 전진: 왼쪽+, 오른쪽-
        {
            if (left > 0) return "전진";
            else return "후진";
        }
        
        if (left > right) return "우회전";
        if (right > left) return "좌회전";
        
        return "복합이동";
    }
    
    void UpdateStatusDisplay()
    {
        var wheelStatus = wheelchair.GetWheelStatus();
        var movementStatus = wheelchair.GetMovementStatus();
        var grabStatus = wheelchair.GetGrabStatus();
        
        Debug.Log($"📊 휠체어 상태 - 속도: {movementStatus.velocity.magnitude:F2}m/s, 각속도: {movementStatus.angularVelocity * Mathf.Rad2Deg:F1}°/s");
        Debug.Log($"    바퀴 deltaZ: L{wheelStatus.leftDeltaZ:F2} R{wheelStatus.rightDeltaZ:F2}, 잡힌상태: L{grabStatus.leftGrabbed} R{grabStatus.rightGrabbed}");
    }
    
    void UpdateTestProgress()
    {
        float elapsedTime = Time.time - testStartTime;
        if (elapsedTime >= testDuration)
        {
            FinishCurrentTest();
        }
    }
    
    void ResetTestState()
    {
        leftWheelDeltaZ = 0f;
        rightWheelDeltaZ = 0f;
        isTestRunning = false;
        currentTestName = "";
    }
    
    // ========== 공개 테스트 메서드들 ==========
    
    /// <summary>
    /// 현재 인스펙터 값으로 즉시 테스트
    /// </summary>
    [ContextMenu("Apply Current Values")]
    public void ApplyCurrentValues()
    {
        if (wheelchair == null)
        {
            Debug.LogError("❌ 휠체어 참조가 없습니다!");
            return;
        }
        
        wheelchair.SetLeftWheelDeltaZ(leftWheelDeltaZ);
        wheelchair.SetRightWheelDeltaZ(rightWheelDeltaZ);
        
        string movement = DetermineMovementType(leftWheelDeltaZ, rightWheelDeltaZ);
        Debug.Log($"✅ 현재 값 적용 - {movement}: 왼쪽 {leftWheelDeltaZ:F1}, 오른쪽 {rightWheelDeltaZ:F1}");
    }
    
    /// <summary>
    /// 휠체어 정지
    /// </summary>
    [ContextMenu("Stop Wheelchair")]
    public void StopWheelchair()
    {
        if (wheelchair != null)
        {
            wheelchair.StopWheels();
            leftWheelDeltaZ = 0f;
            rightWheelDeltaZ = 0f;
            isTestRunning = false;
            Debug.Log("🛑 휠체어 정지");
        }
    }
    
    /// <summary>
    /// 전진 테스트
    /// </summary>
    [ContextMenu("Test Forward")]
    public void TestForward()
    {
        StartSingleTest("전진", testIntensity, -testIntensity);
    }
    
    /// <summary>
    /// 후진 테스트
    /// </summary>
    [ContextMenu("Test Backward")]
    public void TestBackward()
    {
        StartSingleTest("후진", -testIntensity, testIntensity);
    }
    
    /// <summary>
    /// 좌회전 테스트
    /// </summary>
    [ContextMenu("Test Turn Left")]
    public void TestTurnLeft()
    {
        StartSingleTest("좌회전", testIntensity * 0.5f, -testIntensity * 1.5f);
    }
    
    /// <summary>
    /// 우회전 테스트
    /// </summary>
    [ContextMenu("Test Turn Right")]
    public void TestTurnRight()
    {
        StartSingleTest("우회전", testIntensity * 1.5f, -testIntensity * 0.5f);
    }
    
    /// <summary>
    /// 제자리 좌회전 테스트
    /// </summary>
    [ContextMenu("Test Spin Left")]
    public void TestSpinLeft()
    {
        StartSingleTest("제자리 좌회전", testIntensity, testIntensity);
    }
    
    /// <summary>
    /// 제자리 우회전 테스트
    /// </summary>
    [ContextMenu("Test Spin Right")]
    public void TestSpinRight()
    {
        StartSingleTest("제자리 우회전", -testIntensity, -testIntensity);
    }
    
    void StartSingleTest(string testName, float leftDelta, float rightDelta)
    {
        if (wheelchair == null)
        {
            Debug.LogError("❌ 휠체어 참조가 없습니다!");
            return;
        }
        
        // 기존 테스트 정지
        StopCurrentTest();
        
        // 새 테스트 시작
        currentTestName = testName;
        isTestRunning = true;
        testStartTime = Time.time;
        testStartPosition = wheelchair.transform.position;
        testStartRotation = wheelchair.transform.eulerAngles.y;
        
        // 값 설정
        leftWheelDeltaZ = leftDelta;
        rightWheelDeltaZ = rightDelta;
        
        wheelchair.SetLeftWheelDeltaZ(leftDelta);
        wheelchair.SetRightWheelDeltaZ(rightDelta);
        
        Debug.Log($"🧪 테스트 시작: {testName} ({testDuration}초) - L{leftDelta:F1} R{rightDelta:F1}");
    }
    
    void FinishCurrentTest()
    {
        if (!isTestRunning) return;
        
        // 결과 계산
        Vector3 currentPosition = wheelchair.transform.position;
        float currentRotation = wheelchair.transform.eulerAngles.y;
        
        float distanceMoved = Vector3.Distance(testStartPosition, currentPosition);
        float rotationChange = Mathf.DeltaAngle(testStartRotation, currentRotation);
        
        // 휠체어 정지
        wheelchair.StopWheels();
        leftWheelDeltaZ = 0f;
        rightWheelDeltaZ = 0f;
        
        // 결과 표시
        if (showTestResults)
        {
            Debug.Log($"✅ 테스트 완료: {currentTestName}");
            Debug.Log($"    이동거리: {distanceMoved:F2}m");
            Debug.Log($"    회전각도: {rotationChange:F1}도");
            Debug.Log($"    지속시간: {testDuration:F1}초");
        }
        
        isTestRunning = false;
        currentTestName = "";
    }
    
    void StopCurrentTest()
    {
        if (isTestRunning)
        {
            FinishCurrentTest();
        }
        
        if (autoTestCoroutine != null)
        {
            StopCoroutine(autoTestCoroutine);
            autoTestCoroutine = null;
        }
    }
    
    /// <summary>
    /// 자동 테스트 시퀀스 시작
    /// </summary>
    [ContextMenu("Start Auto Test")]
    public void StartAutoTest()
    {
        if (wheelchair == null)
        {
            Debug.LogError("❌ 휠체어 참조가 없습니다!");
            return;
        }
        
        StopCurrentTest();
        autoTestCoroutine = StartCoroutine(AutoTestSequence());
    }
    
    /// <summary>
    /// 자동 테스트 중지
    /// </summary>
    [ContextMenu("Stop Auto Test")]
    public void StopAutoTest()
    {
        enableAutoTest = false;
        StopCurrentTest();
        Debug.Log("🛑 자동 테스트 중지");
    }
    
    IEnumerator AutoTestSequence()
    {
        Debug.Log("🔄 자동 테스트 시퀀스 시작");
        
        var testSequence = new (string name, float left, float right)[]
        {
            ("전진", testIntensity, -testIntensity),
            ("정지", 0f, 0f),
            ("후진", -testIntensity, testIntensity),
            ("정지", 0f, 0f),
            ("좌회전", testIntensity * 0.5f, -testIntensity * 1.5f),
            ("정지", 0f, 0f),
            ("우회전", testIntensity * 1.5f, -testIntensity * 0.5f),
            ("정지", 0f, 0f),
            ("제자리 좌회전", testIntensity, testIntensity),
            ("정지", 0f, 0f),
            ("제자리 우회전", -testIntensity, -testIntensity),
            ("최종 정지", 0f, 0f)
        };
        
        currentAutoTestIndex = 0;
        
        while (enableAutoTest && currentAutoTestIndex < testSequence.Length)
        {
            var test = testSequence[currentAutoTestIndex];
            
            Debug.Log($"🔄 자동 테스트 [{currentAutoTestIndex + 1}/{testSequence.Length}]: {test.name}");
            
            StartSingleTest(test.name, test.left, test.right);
            
            // 테스트 완료까지 대기
            while (isTestRunning && enableAutoTest)
            {
                yield return new WaitForSeconds(0.1f);
            }
            
            // 다음 테스트 전 잠시 대기
            yield return new WaitForSeconds(0.5f);
            
            currentAutoTestIndex++;
        }
        
        Debug.Log("✅ 자동 테스트 시퀀스 완료");
        autoTestCoroutine = null;
    }
    
    /// <summary>
    /// 현재 휠체어 상태 즉시 출력
    /// </summary>
    [ContextMenu("Debug Current State")]
    public void DebugCurrentState()
    {
        if (wheelchair == null)
        {
            Debug.LogError("❌ 휠체어 참조가 없습니다!");
            return;
        }
        
        Debug.Log("═══════════════════════════════════════");
        Debug.Log("🔍 휠체어 테스트 - 현재 상태");
        Debug.Log("═══════════════════════════════════════");
        
        var wheelStatus = wheelchair.GetWheelStatus();
        var movementStatus = wheelchair.GetMovementStatus();
        var grabStatus = wheelchair.GetGrabStatus();
        
        Debug.Log($"🎮 테스트 설정값:");
        Debug.Log($"  왼쪽 바퀴 deltaZ: {leftWheelDeltaZ:F2}");
        Debug.Log($"  오른쪽 바퀴 deltaZ: {rightWheelDeltaZ:F2}");
        Debug.Log($"  예상 동작: {DetermineMovementType(leftWheelDeltaZ, rightWheelDeltaZ)}");
        
        Debug.Log($"🚗 실제 휠체어 상태:");
        Debug.Log($"  실제 왼쪽 deltaZ: {wheelStatus.leftDeltaZ:F2}");
        Debug.Log($"  실제 오른쪽 deltaZ: {wheelStatus.rightDeltaZ:F2}");
        Debug.Log($"  현재 속도: {movementStatus.velocity.magnitude:F2}m/s");
        Debug.Log($"  현재 각속도: {movementStatus.angularVelocity * Mathf.Rad2Deg:F1}도/초");
        
        Debug.Log($"🤏 바퀴 잡힘 상태:");
        Debug.Log($"  왼쪽: {(grabStatus.leftGrabbed ? "잡힘" : "놓임")}");
        Debug.Log($"  오른쪽: {(grabStatus.rightGrabbed ? "잡힘" : "놓임")}");
        
        if (isTestRunning)
        {
            float elapsed = Time.time - testStartTime;
            Debug.Log($"🧪 진행 중인 테스트: {currentTestName} ({elapsed:F1}/{testDuration:F1}초)");
        }
        
        Debug.Log("═══════════════════════════════════════");
    }
    
    /// <summary>
    /// 휠체어 시스템 초기화
    /// </summary>
    [ContextMenu("Reset Wheelchair")]
    public void ResetWheelchair()
    {
        if (wheelchair != null)
        {
            wheelchair.ResetWheelSystem();
            ResetTestState();
            Debug.Log("🔄 휠체어 시스템 초기화 완료");
        }
    }
    
    void OnDisable()
    {
        if (stopOnDisable)
        {
            StopCurrentTest();
            if (wheelchair != null)
            {
                wheelchair.StopWheels();
            }
        }
    }
    
    void OnDestroy()
    {
        StopCurrentTest();
    }
    
    // ========== 유니티 에디터 전용 메서드들 ==========
    
    #if UNITY_EDITOR
    /// <summary>
    /// 에디터에서 값 변경 시 자동 적용 (Inspector에서 값 변경 시)
    /// </summary>
    void OnValidate()
    {
        // 플레이 모드에서만 실행
        if (Application.isPlaying && enableRealTimeControl && wheelchair != null)
        {
            wheelchair.SetLeftWheelDeltaZ(leftWheelDeltaZ);
            wheelchair.SetRightWheelDeltaZ(rightWheelDeltaZ);
        }
        
        // 설정값 검증
        testDuration = Mathf.Max(0.1f, testDuration);
        testIntensity = Mathf.Max(0.1f, testIntensity);
        statusUpdateInterval = Mathf.Max(0.1f, statusUpdateInterval);
    }
    #endif
}

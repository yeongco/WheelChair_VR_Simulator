using UnityEngine;

public class WheelchairHover : MonoBehaviour
{
    [Header("🔋 초전도체 부양 시스템")]
    [Tooltip("부양 높이를 지면으로부터 이만큼 유지합니다.")]
    public float hoverHeight = 0.1f;

    [Header("🎯 4점 지면 감지 시스템")]
    [Tooltip("부양을 위해 레이캐스트할 총 4개 지점")]
    public Transform[] groundDetectionPoints = new Transform[4];
    [Tooltip("지면 감지용 레이의 최대 길이")]
    public float groundCheckDistance = 2f;
    [Tooltip("지면으로 인식할 레이어")]
    public LayerMask groundLayer = 1;
    [Tooltip("감지 지점 만들 때 로컬 Y 오프셋 (기본값 0.05)")]
    public float contactPointOffset = 0.05f;

    [Header("🎛️ 물리 설정")]
    [Tooltip("부양할 Rigidbody")]
    public Rigidbody chairRigidbody;
    [Tooltip("휠체어 질량 (Inspector에서 직접 지정)")]
    public float chairMass = 80f;

    // 내부 계산용: “각 지점” 스프링 계수와 댐핑 계수
    private float k_perPoint;    // 스프링 계수
    private float c_perPoint;    // 댐핑 계수

    // 내부용: 지면 감지 데이터
    private float[] groundDistances = new float[4];
    private Vector3[] groundPoints = new Vector3[4];
    private Vector3[] groundNormals = new Vector3[4];
    private bool[] groundDetected = new bool[4];

    void Start()
    {
        // Rigidbody가 없으면 붙이고, 물리 속성 세팅
        if (chairRigidbody == null)
            chairRigidbody = GetComponent<Rigidbody>();

        chairRigidbody.mass = chairMass;
        chairRigidbody.useGravity = true;      // 중력을 켜둡니다
        chairRigidbody.drag = 0.5f;            // 공기 저항 예시
        chairRigidbody.angularDrag = 10f;      // 회전 저항 예시
        chairRigidbody.centerOfMass = new Vector3(0, -0.2f, 0);
        chairRigidbody.maxAngularVelocity = 50f;

        // “각 지점” 스프링/댐핑 계수 계산
        float g = Physics.gravity.magnitude; // 보통 9.81

        // k_perPoint = (m * g) / (4 * hoverHeight)
        k_perPoint = (chairMass * g) / (4f * hoverHeight);

        // 임계 댐핑 계수: m_eff = m/4
        float m_eff = chairMass / 4f;
        c_perPoint = 2f * Mathf.Sqrt(k_perPoint * m_eff);

        // 지면 감지 포인트가 할당되어 있지 않으면 자동 생성
        if (groundDetectionPoints[0] == null)
            CreateGroundDetectionPoints();

        // ➡️ 시작 시, “딱 한 번” 바닥의 높이를 찾아서 Transform.y를 재조정합니다.
        //    이렇게 해서 네 개의 감지 지점(GroundDetectionPoint)이 모두 
        //    groundY + hoverHeight에 위치하도록 만듭니다.
        // 1) 한 번 PerformGroundDetection을 실행해서 groundPoints[]에 채워넣고
        PerformGroundDetection();

        // 2) 실제로 감지된 지점들이 하나라도 있으면, 평균 groundY를 구해서 
        //    transform.position.y를 조정합니다.
        float sumY = 0f;
        int validCount = 0;
        for (int i = 0; i < 4; i++)
        {
            if (groundDetected[i])
            {
                sumY += groundPoints[i].y;
                validCount++;
            }
        }

        if (validCount > 0)
        {
            float avgGroundY = sumY / validCount;
            // detectionPoint 한 개의 local Y 오프셋 = contactPointOffset
            // → detectionPoint.worldY = transform.position.y + contactPointOffset
            // 우리가 원하는 건 “detectionPoint.worldY == avgGroundY + hoverHeight”
            // 그러므로 transform.position.y를 다음과 같이 세팅:
            float newTransformY = avgGroundY + hoverHeight - contactPointOffset;
            transform.position = new Vector3(transform.position.x,
                                             newTransformY,
                                             transform.position.z);

            // transform을 옮겼으니, groundDetectionPoints도 함께 이동합니다.
            // (Child이므로 자동 반영됩니다.)
        }
    }

    void FixedUpdate()
    {
        PerformGroundDetection();
        ApplySuperconductorHover();
    }

    /// <summary>
    /// 4개 지점에서 레이캐스트를 쏘아 지면을 감지합니다.
    /// </summary>
    void PerformGroundDetection()
    {
        for (int i = 0; i < 4; i++)
        {
            groundDetected[i] = false;
            groundDistances[i] = groundCheckDistance;

            if (groundDetectionPoints[i] == null) 
                continue;

            Vector3 rayStart = groundDetectionPoints[i].position;
            RaycastHit hit;
            if (Physics.Raycast(rayStart, Vector3.down, out hit, groundCheckDistance, groundLayer))
            {
                groundDistances[i] = hit.distance;
                groundPoints[i] = hit.point;
                groundNormals[i] = hit.normal;
                groundDetected[i] = true;
            }
        }
    }

    /// <summary>
    /// 지면으로부터 hoverHeight만큼 떠 있도록 네 지점에 스프링+댐핑 힘을 계산해 가합니다.
    /// Rigidbody.useGravity = true 상태이므로, 중력은 Unity가 자체적으로 적용합니다.
    /// </summary>
    void ApplySuperconductorHover()
    {
        bool anyGround = false;
        float sumDistances = 0f;
        int count = 0;

        // 유효 지면 지점 수집
        for (int i = 0; i < 4; i++)
        {
            if (!groundDetected[i]) 
                continue;
            anyGround = true;
            sumDistances += groundDistances[i];
            count++;
        }

        // 지면 감지가 하나도 안 된 경우: 부양력 없음 (중력만)
        if (!anyGround)
            return;

        // 평균 거리 (부양 세기를 조정하기 위해 사용)
        float avgDistance = (count > 0) ? (sumDistances / count) : groundCheckDistance;

        // “멀어졌을 때만 서서히 힘을 줄이기” 위한 보정 계수
        // → hoverHeight까진 full (=1),
        //   hoverHeight~(hoverHeight+0.5) 구간에서는 Cosine으로 1→0 천천히 감소,
        //   hoverHeight+0.5 이상부터는 0
        float hoverInfluence = CalculateHoverInfluence(avgDistance);

        for (int i = 0; i < 4; i++)
        {
            if (!groundDetected[i]) 
                continue;

            Vector3 pointPos = groundDetectionPoints[i].position;
            float actualGroundY = groundPoints[i].y;

            // 지면으로부터 이 지점의 높이 = pointPos.y - actualGroundY
            float currentHeight = pointPos.y - actualGroundY;
            // 목표 높이 오차(error) = (actualGroundY + hoverHeight) - pointPos.y
            float heightError = (actualGroundY + hoverHeight) - pointPos.y;

            // ● 스프링 힘: k_perPoint * heightError
            float springForce = k_perPoint * heightError;

            // ● 댐퍼 힘: - c_perPoint * v_y
            float verticalVel = Vector3.Dot(chairRigidbody.velocity, Vector3.up);
            float damperForce = -c_perPoint * verticalVel;

            // 멀어졌을 때만 힘을 줄이도록 hoverInfluence 곱함
            Vector3 totalForce = Vector3.up * (springForce + damperForce) * hoverInfluence;

            chairRigidbody.AddForceAtPosition(totalForce, pointPos, ForceMode.Force);
        }

        // **별도 중력 보강은 하지 않습니다**.
        // Rigidbody.useGravity=true 이므로 부족한 힘은 Unity가 자동으로 중력으로 보강합니다.
    }

    /// <summary>
    /// 평균 지면과의 거리에 따라 부양 영향력(0~1)을 보정합니다.
    ///
    /// distanceToGround <= hoverHeight → 항상 1.0 (풀 파워 스프링)
    /// hoverHeight < distanceToGround < hoverHeight+0.5 → Cosine 곡선으로 1→0 감소
    /// distanceToGround >= hoverHeight+0.5 → 0 (부양력 없음)
    /// </summary>
    float CalculateHoverInfluence(float distanceToGround)
    {
        if (distanceToGround <= hoverHeight)
            return 1f;

        float transitionRange = 0.5f;
        float excess = distanceToGround - hoverHeight;
        if (excess >= transitionRange)
            return 0f;

        float t = excess / transitionRange; // 0→1
        return Mathf.Cos(t * Mathf.PI * 0.5f);
    }

    /// <summary>
    /// 주변에 4개의 빈 오브젝트(Transform)를 생성하여 지면 감지 포인트로 사용합니다.
    /// </summary>
    void CreateGroundDetectionPoints()
    {
        float halfWidth = 0.4f;
        float halfLength = 0.6f;
        Vector3[] localPositions = new Vector3[4]
        {
            new Vector3(-halfWidth, contactPointOffset,  halfLength),
            new Vector3( halfWidth, contactPointOffset,  halfLength),
            new Vector3(-halfWidth, contactPointOffset, -halfLength),
            new Vector3( halfWidth, contactPointOffset, -halfLength)
        };

        for (int i = 0; i < 4; i++)
        {
            GameObject go = new GameObject($"GroundPoint_{i}");
            go.transform.SetParent(transform);
            go.transform.localPosition = localPositions[i];
            groundDetectionPoints[i] = go.transform;
        }
    }

    /// <summary>
    /// 기울어진 지면 위에 있는지 확인하는 유틸 함수 (추후 slope 로직 작성용).
    /// </summary>
    /// <param name="slopeThreshold">기울기 판정 임계값(도)</param>
    /// <param name="averageNormal">검출된 지면 법선의 평균</param>
    /// <returns>임계값 이상 기울어졌으면 true</returns>
    public bool IsOnSlope(float slopeThreshold, out Vector3 averageNormal)
    {
        averageNormal = Vector3.zero;
        int valid = 0;

        for (int i = 0; i < 4; i++)
        {
            if (groundDetected[i])
            {
                averageNormal += groundNormals[i];
                valid++;
            }
        }

        if (valid == 0)
            return false;

        averageNormal = (averageNormal / valid).normalized;
        float angle = Vector3.Angle(averageNormal, Vector3.up);
        return angle > slopeThreshold;
    }
}

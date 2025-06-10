using UnityEngine;

public class WheelchairController : MonoBehaviour
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
        // Rigidbody 초기화
        if (chairRigidbody == null)
            chairRigidbody = GetComponent<Rigidbody>();

        // mass가 바뀌어도 항상 반영되도록 Rigidbody에 설정
        chairRigidbody.mass = chairMass;
        chairRigidbody.useGravity = false;
        chairRigidbody.drag = 0.5f;        // 예시로 약간의 공기 저항
        chairRigidbody.angularDrag = 10f;  // 약간의 회전 저항
        chairRigidbody.centerOfMass = new Vector3(0, -0.2f, 0);
        chairRigidbody.maxAngularVelocity = 50f;

        // “각 지점” 스프링/댐핑 계수 계산
        // g = Physics.gravity.magnitude (예: 9.81)
        float g = Physics.gravity.magnitude;

        // 1) 스프링 계수:
        //    k_total = (m * g) / hoverHeight   → 네 지점으로 분산 → k_perPoint = k_total / 4
        //    즉 k_perPoint = (m * g) / (4 * hoverHeight)
        k_perPoint = (chairMass * g) / (4f * hoverHeight);

        // 2) 임계 댐핑 계수(critical damping) for each point:
        //    m_eff = m / 4  (각 스프링이 담당하는 유효 질량)
        //    c_perPoint = 2 * sqrt(k_perPoint * m_eff)
        float m_eff = chairMass / 4f;
        c_perPoint = 2f * Mathf.Sqrt(k_perPoint * m_eff);

        // 지면 감지 포인트가 할당되어 있지 않으면 자동 생성
        if (groundDetectionPoints[0] == null)
            CreateGroundDetectionPoints();
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
            if (groundDetectionPoints[i] == null) continue;

            Vector3 rayStart = groundDetectionPoints[i].position;
            RaycastHit hit;
            if (Physics.Raycast(rayStart, Vector3.down, out hit, groundCheckDistance, groundLayer))
            {
                groundDistances[i] = hit.distance;
                groundPoints[i] = hit.point;
                groundNormals[i] = hit.normal;
                groundDetected[i] = true;
            }
            else
            {
                groundDetected[i] = false;
                groundDistances[i] = groundCheckDistance;
            }
        }
    }

    /// <summary>
    /// 지면으로부터 hoverHeight만큼 떠 있도록 네 지점에 힘을 계산해서 가합니다.
    /// 각 지점 스프링 계수와 댐핑 계수는 mass 기반 공식으로 미리 계산해 두었습니다.
    /// </summary>
    void ApplySuperconductorHover()
    {
        bool anyGround = false;
        float sumDistances = 0f;
        int count = 0;

        // 유효 지면 지점 수집
        for (int i = 0; i < 4; i++)
        {
            if (!groundDetected[i]) continue;
            anyGround = true;
            sumDistances += groundDistances[i];
            count++;
        }

        // 지면 감지가 하나도 안 된 경우: 중력만 적용
        if (!anyGround)
        {
            Vector3 gravity = Vector3.down * chairMass * Physics.gravity.magnitude;
            chairRigidbody.AddForce(gravity, ForceMode.Force);
            return;
        }

        // 평균 거리 (부양 영향력 산정을 위해 사용 가능)
        float avgDistance = (count > 0) ? (sumDistances / count) : groundCheckDistance;

        // iteration마다 네 지점에 스프링+댐핑 힘 적용
        // hoverInfluence를 곱해서, 너무 멀리 떨어지면 점점 전혀 힘을 주지 않도록
        float hoverInfluence = CalculateHoverInfluence(avgDistance);

        for (int i = 0; i < 4; i++)
        {
            if (!groundDetected[i]) continue;

            Vector3 pointPos = groundDetectionPoints[i].position;
            float actualGroundY = groundPoints[i].y;

            // “지면으로부터 이 지점까지의 현재 높이”
            float currentHeight = pointPos.y - actualGroundY;
            // 목표 높이는 hoverHeight
            float heightError = (actualGroundY + hoverHeight) - pointPos.y;

            // 스프링 힘 = k_perPoint * heightError
            float springForce = k_perPoint * heightError;

            // 댐핑 힘 = - c_perPoint * v_y
            float verticalVel = Vector3.Dot(chairRigidbody.velocity, Vector3.up);
            float damperForce = -c_perPoint * verticalVel;

            // 네 지점을 합친 총 힘이 m·g를 상쇄하려면,  
            // 각 지점에 (springForce + damperForce) / 4 대신
            // 이미 k_perPoint, c_perPoint 가 4분할을 반영해 두었으므로 곱해서 사용
            Vector3 totalForce = Vector3.up * (springForce + damperForce) * hoverInfluence * 1f;

            chairRigidbody.AddForceAtPosition(totalForce, pointPos, ForceMode.Force);
        }

        // hoverInfluence가 1보다 작으면 일부 중력 적용
        float gravityPortion = 1f - hoverInfluence;
        if (gravityPortion > 0f)
        {
            Vector3 partialGravity = Vector3.down * chairMass * Physics.gravity.magnitude * gravityPortion;
            chairRigidbody.AddForce(partialGravity, ForceMode.Force);
        }
    }

    /// <summary>
    /// 평균 지면과의 거리에 따라 부양 영향력(0~1)을 보정합니다.
    /// (distanceToGround <= hoverHeight 이면 1.0,  
    ///  hoverHeight~hoverHeight+1 범위에서는 서서히 0으로 감소,  
    ///  hoverHeight+1 이상이면 0)
    /// </summary>
    float CalculateHoverInfluence(float distanceToGround)
    {
        if (distanceToGround <= hoverHeight)
            return 1f;

        float transitionRange = 1f; 
        float excess = distanceToGround - hoverHeight;
        if (excess >= transitionRange)
            return 0f;

        float t = excess / transitionRange;
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

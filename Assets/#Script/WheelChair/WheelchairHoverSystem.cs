using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(WheelchairController))]
public class WheelchairHoverSystem : MonoBehaviour
{
//     [Header("고급 부양 설정")]
//     [SerializeField] private bool enableAdvancedHover = true;
    
//     [Header("다중 포인트 높이 제어")]
//     [SerializeField] private bool useMultiPointControl = true;
//     [SerializeField] private Transform[] additionalHeightPoints; // 추가 높이 제어 포인트들
//     [SerializeField] private float heightControlStrength = 1500f;
//     [SerializeField] private float heightControlDamping = 100f;
    
//     [Header("자이로스코프 안정성")]
//     [SerializeField] private bool enableGyroscopicStability = true;
//     [SerializeField] private float gyroscopicForce = 2000f;
//     [SerializeField] private float gyroscopicDamping = 150f;
//     [SerializeField] private Vector3 targetUpDirection = Vector3.up;
    
//     [Header("적응형 높이 조절")]
//     [SerializeField] private bool useAdaptiveHeight = true;
//     [SerializeField] private float minHoverHeight = 0.3f;
//     [SerializeField] private float maxHoverHeight = 1.0f;
//     [SerializeField] private float heightAdjustmentSpeed = 2f;
//     [SerializeField] private float terrainSmoothingFactor = 0.8f;
    
//     [Header("안전 시스템")]
//     [SerializeField] private bool enableSafetyLimits = true;
//     [SerializeField] private float maxAllowedHeight = 2f;
//     [SerializeField] private float emergencyDescentForce = 5000f;
//     [SerializeField] private float safetyCheckRadius = 1f;
    
//     [Header("지형 적응")]
//     [SerializeField] private bool enableTerrainAdaptation = true;
//     [SerializeField] private float terrainScanRadius = 1.5f;
//     [SerializeField] private int terrainScanPointCount = 8;
//     [SerializeField] private LayerMask terrainLayer = 1;
    
//     private WheelchairController wheelchairController;
//     private Rigidbody chairRigidbody;
    
//     // 높이 제어 상태
//     private float[] heightPointDistances;
//     private Vector3[] heightPointGroundPositions;
//     private float smoothedGroundHeight;
//     private float targetHoverHeight;
    
//     // 안정성 상태
//     private Vector3 lastUpDirection;
//     private float stabilityTimer = 0f;
    
//     // 지형 분석 데이터
//     private Vector3[] terrainScanPositions;
//     private float[] terrainHeights;
//     private Vector3 averageTerrainNormal;
    
//     void Start()
//     {
//         wheelchairController = GetComponent<WheelchairController>();
//         chairRigidbody = wheelchairController.chairRigidbody;
        
//         // 배열 초기화
//         int totalPoints = 2 + (additionalHeightPoints != null ? additionalHeightPoints.Length : 0);
//         heightPointDistances = new float[totalPoints];
//         heightPointGroundPositions = new Vector3[totalPoints];
        
//         // 지형 스캔 포인트 초기화
//         terrainScanPositions = new Vector3[terrainScanPointCount];
//         terrainHeights = new float[terrainScanPointCount];
        
//         lastUpDirection = transform.up;
//         targetHoverHeight = wheelchairController.hoverHeight;
//         smoothedGroundHeight = transform.position.y;
//     }
    
//     void FixedUpdate()
//     {
//         if (!enableAdvancedHover) return;
        
//         if (useMultiPointControl)
//         {
//             UpdateMultiPointHeightControl();
//         }
        
//         if (enableGyroscopicStability)
//         {
//             ApplyGyroscopicStability();
//         }
        
//         if (useAdaptiveHeight)
//         {
//             UpdateAdaptiveHeight();
//         }
        
//         if (enableTerrainAdaptation)
//         {
//             AnalyzeTerrain();
//             AdaptToTerrain();
//         }
        
//         if (enableSafetyLimits)
//         {
//             ApplySafetyLimits();
//         }
//     }
    
//     void UpdateMultiPointHeightControl()
//     {
//         // 기본 높이 포인트들 처리
//         ProcessHeightPoint(wheelchairController.frontHeightPoint, 0);
//         ProcessHeightPoint(wheelchairController.rearHeightPoint, 1);
        
//         // 추가 높이 포인트들 처리
//         if (additionalHeightPoints != null)
//         {
//             for (int i = 0; i < additionalHeightPoints.Length; i++)
//             {
//                 ProcessHeightPoint(additionalHeightPoints[i], i + 2);
//             }
//         }
//     }
    
//     void ProcessHeightPoint(Transform heightPoint, int index)
//     {
//         if (heightPoint == null) return;
        
//         RaycastHit hit;
//         Vector3 rayStart = heightPoint.position;
        
//         if (Physics.Raycast(rayStart, Vector3.down, out hit, wheelchairController.groundCheckDistance, wheelchairController.groundLayer))
//         {
//             heightPointDistances[index] = hit.distance;
//             heightPointGroundPositions[index] = hit.point;
            
//             // 목표 높이 계산
//             float targetHeight = hit.point.y + targetHoverHeight;
//             float currentHeight = heightPoint.position.y;
//             float heightError = targetHeight - currentHeight;
            
//             // 높이 제어 힘 적용
//             if (Mathf.Abs(heightError) > 0.01f)
//             {
//                 Vector3 controlForce = Vector3.up * heightError * heightControlStrength;
//                 chairRigidbody.AddForceAtPosition(controlForce, heightPoint.position, ForceMode.Force);
                
//                 // 댐핑 적용
//                 float verticalVelocity = Vector3.Dot(chairRigidbody.velocity, Vector3.up);
//                 Vector3 dampingForce = Vector3.down * verticalVelocity * heightControlDamping;
//                 chairRigidbody.AddForceAtPosition(dampingForce, heightPoint.position, ForceMode.Force);
//             }
//         }
//         else
//         {
//             heightPointDistances[index] = wheelchairController.groundCheckDistance;
//             heightPointGroundPositions[index] = rayStart + Vector3.down * wheelchairController.groundCheckDistance;
//         }
//     }
    
//     void ApplyGyroscopicStability()
//     {
//         // 현재 상향 벡터와 목표 상향 벡터 비교
//         Vector3 currentUp = transform.up;
//         Vector3 targetUp = targetUpDirection;
        
//         // 회전 오차 계산
//         Vector3 rotationError = Vector3.Cross(currentUp, targetUp);
//         float errorMagnitude = rotationError.magnitude;
        
//         if (errorMagnitude > 0.01f)
//         {
//             // 자이로스코프 토크 적용
//             Vector3 gyroTorque = rotationError.normalized * errorMagnitude * gyroscopicForce;
//             chairRigidbody.AddTorque(gyroTorque, ForceMode.Force);
            
//             // 각속도 댐핑
//             Vector3 angularVelocity = chairRigidbody.angularVelocity;
//             Vector3 dampingTorque = -angularVelocity * gyroscopicDamping;
//             chairRigidbody.AddTorque(dampingTorque, ForceMode.Force);
//         }
        
//         // 안정성 타이머 업데이트
//         if (errorMagnitude < 0.1f)
//         {
//             stabilityTimer += Time.fixedDeltaTime;
//         }
//         else
//         {
//             stabilityTimer = 0f;
//         }
        
//         lastUpDirection = currentUp;
//     }
    
//     void UpdateAdaptiveHeight()
//     {
//         // 현재 지형 높이 분석
//         float averageGroundHeight = CalculateAverageGroundHeight();
        
//         // 부드러운 지면 높이 업데이트
//         smoothedGroundHeight = Mathf.Lerp(smoothedGroundHeight, averageGroundHeight, 
//             Time.fixedDeltaTime * terrainSmoothingFactor);
        
//         // 지형 경사도에 따른 적응형 높이 계산
//         float terrainSteepness = CalculateTerrainSteepness();
//         float adaptiveHeight = Mathf.Lerp(minHoverHeight, maxHoverHeight, terrainSteepness);
        
//         // 목표 높이 부드럽게 조정
//         targetHoverHeight = Mathf.Lerp(targetHoverHeight, adaptiveHeight, 
//             Time.fixedDeltaTime * heightAdjustmentSpeed);
        
//         // WheelchairController의 hoverHeight 업데이트
//         wheelchairController.hoverHeight = targetHoverHeight;
//     }
    
//     float CalculateAverageGroundHeight()
//     {
//         float totalHeight = 0f;
//         int validPoints = 0;
        
//         for (int i = 0; i < heightPointGroundPositions.Length; i++)
//         {
//             if (heightPointDistances[i] < wheelchairController.groundCheckDistance)
//             {
//                 totalHeight += heightPointGroundPositions[i].y;
//                 validPoints++;
//             }
//         }
        
//         return validPoints > 0 ? totalHeight / validPoints : transform.position.y;
//     }
    
//     float CalculateTerrainSteepness()
//     {
//         if (heightPointGroundPositions.Length < 2) return 0f;
        
//         float maxHeightDiff = 0f;
//         for (int i = 0; i < heightPointGroundPositions.Length - 1; i++)
//         {
//             for (int j = i + 1; j < heightPointGroundPositions.Length; j++)
//             {
//                 float heightDiff = Mathf.Abs(heightPointGroundPositions[i].y - heightPointGroundPositions[j].y);
//                 maxHeightDiff = Mathf.Max(maxHeightDiff, heightDiff);
//             }
//         }
        
//         // 경사도를 0-1 범위로 정규화
//         return Mathf.Clamp01(maxHeightDiff / 2f);
//     }
    
//     void AnalyzeTerrain()
//     {
//         Vector3 centerPosition = transform.position;
//         averageTerrainNormal = Vector3.zero;
        
//         // 원형으로 지형 스캔 포인트 배치
//         for (int i = 0; i < terrainScanPositions.Length; i++)
//         {
//             float angle = (float)i / terrainScanPositions.Length * 2f * Mathf.PI;
//             Vector3 scanDirection = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle));
//             Vector3 scanPosition = centerPosition + scanDirection * terrainScanRadius;
            
//             terrainScanPositions[i] = scanPosition;
            
//             // 지형 높이 측정
//             RaycastHit hit;
//             if (Physics.Raycast(scanPosition + Vector3.up * 5f, Vector3.down, out hit, 10f, terrainLayer))
//             {
//                 terrainHeights[i] = hit.point.y;
//                 averageTerrainNormal += hit.normal;
//             }
//             else
//             {
//                 terrainHeights[i] = centerPosition.y;
//                 averageTerrainNormal += Vector3.up;
//             }
//         }
        
//         // 평균 지형 법선 계산
//         averageTerrainNormal = (averageTerrainNormal / terrainScanPositions.Length).normalized;
//     }
    
//     void AdaptToTerrain()
//     {
//         // 지형 법선에 따른 휠체어 방향 조정
//         Vector3 targetUp = Vector3.Slerp(Vector3.up, averageTerrainNormal, 0.3f);
//         Vector3 currentUp = transform.up;
        
//         Vector3 rotationCorrection = Vector3.Cross(currentUp, targetUp);
//         if (rotationCorrection.magnitude > 0.01f)
//         {
//             Vector3 correctionTorque = rotationCorrection * gyroscopicForce * 0.5f;
//             chairRigidbody.AddTorque(correctionTorque, ForceMode.Force);
//         }
//     }
    
//     void ApplySafetyLimits()
//     {
//         // 최대 높이 제한
//         float currentHeight = transform.position.y;
//         float groundHeight = CalculateAverageGroundHeight();
        
//         if (currentHeight - groundHeight > maxAllowedHeight)
//         {
//             // 긴급 하강 힘 적용
//             Vector3 emergencyForce = Vector3.down * emergencyDescentForce;
//             chairRigidbody.AddForce(emergencyForce, ForceMode.Force);
//         }
        
//         // 안전 반경 내 장애물 검사
//         Collider[] obstacles = Physics.OverlapSphere(transform.position, safetyCheckRadius);
//         foreach (Collider obstacle in obstacles)
//         {
//             if (obstacle.gameObject != gameObject && !obstacle.isTrigger)
//             {
//                 // 장애물과의 충돌 회피
//                 Vector3 avoidanceDirection = (transform.position - obstacle.transform.position).normalized;
//                 Vector3 avoidanceForce = avoidanceDirection * 500f;
//                 chairRigidbody.AddForce(avoidanceForce, ForceMode.Force);
//             }
//         }
//     }
    
//     // 공개 메서드들
//     public void SetTargetHoverHeight(float height)
//     {
//         targetHoverHeight = Mathf.Clamp(height, minHoverHeight, maxHoverHeight);
//     }
    
//     public float GetCurrentStability()
//     {
//         Vector3 currentUp = transform.up;
//         return Vector3.Dot(currentUp, Vector3.up);
//     }
    
//     public bool IsStable()
//     {
//         return stabilityTimer > 1f && GetCurrentStability() > 0.9f;
//     }
    
//     public Vector3 GetAverageTerrainNormal()
//     {
//         return averageTerrainNormal;
//     }
    
//     // 디버그 시각화
//     void OnDrawGizmos()
//     {
//         if (!enableAdvancedHover) return;
        
//         // 높이 제어 포인트들 표시
//         if (useMultiPointControl && heightPointGroundPositions != null)
//         {
//             for (int i = 0; i < heightPointGroundPositions.Length; i++)
//             {
//                 if (heightPointDistances != null && i < heightPointDistances.Length)
//                 {
//                     Gizmos.color = heightPointDistances[i] < wheelchairController.groundCheckDistance ? Color.green : Color.red;
//                     Gizmos.DrawWireSphere(heightPointGroundPositions[i], 0.1f);
//                 }
//             }
//         }
        
//         // 지형 스캔 포인트들 표시
//         if (enableTerrainAdaptation && terrainScanPositions != null)
//         {
//             Gizmos.color = Color.blue;
//             for (int i = 0; i < terrainScanPositions.Length; i++)
//             {
//                 if (terrainHeights != null && i < terrainHeights.Length)
//                 {
//                     Vector3 terrainPoint = new Vector3(terrainScanPositions[i].x, terrainHeights[i], terrainScanPositions[i].z);
//                     Gizmos.DrawWireSphere(terrainPoint, 0.05f);
//                 }
//             }
            
//             // 지형 스캔 반경 표시
//             Gizmos.color = Color.cyan;
//             Gizmos.DrawWireSphere(transform.position, terrainScanRadius);
//         }
        
//         // 안전 반경 표시
//         if (enableSafetyLimits)
//         {
//             Gizmos.color = Color.yellow;
//             Gizmos.DrawWireSphere(transform.position, safetyCheckRadius);
//         }
        
//         // 자이로스코프 안정성 표시
//         if (enableGyroscopicStability)
//         {
//             Gizmos.color = IsStable() ? Color.green : new Color(1f, 0.5f, 0f); // 주황색
//             Gizmos.DrawLine(transform.position, transform.position + transform.up * 2f);
            
//             Gizmos.color = Color.white;
//             Gizmos.DrawLine(transform.position, transform.position + targetUpDirection * 2f);
//         }
        
//         // 평균 지형 법선 표시
//         if (enableTerrainAdaptation && averageTerrainNormal != Vector3.zero)
//         {
//             Gizmos.color = Color.magenta;
//             Gizmos.DrawLine(transform.position, transform.position + averageTerrainNormal * 1.5f);
//         }
//     }
 } 
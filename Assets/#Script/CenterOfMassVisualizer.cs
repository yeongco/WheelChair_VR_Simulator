using UnityEngine;

public class CenterOfMassVisualizer : MonoBehaviour
{
    [Header("무게중심 시각화")]
    [SerializeField] private Color gizmoColor = Color.red;
    [SerializeField] private float sphereRadius = 0.1f;
    [SerializeField] private float lineLength = 0.5f;
    
    private Rigidbody targetRigidbody;
    
    void OnDrawGizmos()
    {
        // Rigidbody가 없으면 가져오기
        if (targetRigidbody == null)
            targetRigidbody = GetComponent<Rigidbody>();
            
        // Rigidbody가 있으면 무게중심 시각화
        if (targetRigidbody != null)
        {
            // 무게중심 월드 좌표 계산
            Vector3 comWorld = transform.TransformPoint(targetRigidbody.centerOfMass);
            
            // 무게중심 위치에 구체 그리기
            Gizmos.color = gizmoColor;
            Gizmos.DrawSphere(comWorld, sphereRadius);
            
            // 무게중심에서 아래로 선 그리기
            Gizmos.DrawLine(comWorld, comWorld + Vector3.down * lineLength);
        }
    }
} 
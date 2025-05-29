using UnityEngine;

public class CenterOfMassAdjuster : MonoBehaviour
{
    [Header("무게중심 설정")]
    [SerializeField] private Vector3 centerOfMassOffset = new Vector3(0, -0.8f, 1.15f);
    [SerializeField] private bool showCenterOfMass = true;
    
    private Rigidbody wheelchairBody;
    
    void Start()
    {
        // 휠체어 본체의 Rigidbody 가져오기
        wheelchairBody = GetComponent<Rigidbody>();
        
        if (wheelchairBody != null)
        {
            // 무게중심 설정
            wheelchairBody.centerOfMass = centerOfMassOffset;
            
            // 안정성을 위한 추가 설정
            wheelchairBody.maxAngularVelocity = 3f; // 과도한 회전 방지
            wheelchairBody.angularDrag = 1.5f;      // 회전 저항 증가
        }
        else
        {
            Debug.LogWarning("CenterOfMassAdjuster: Rigidbody를 찾을 수 없습니다!");
        }
    }
    
    void OnDrawGizmos()
    {
        // 무게중심 시각화 (에디터에서만 표시)
        if (showCenterOfMass && wheelchairBody != null)
        {
            Gizmos.color = Color.red;
            Vector3 comWorld = transform.TransformPoint(wheelchairBody.centerOfMass);
            Gizmos.DrawSphere(comWorld, 0.1f);
            
            // 무게중심에서 아래로 선 그리기
            Gizmos.DrawLine(comWorld, comWorld + Vector3.down * 0.5f);
        }
    }
} 
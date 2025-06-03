using UnityEngine;
using Autohand;

[System.Serializable]
public class WheelGrabbableSetup : MonoBehaviour
{
    [Header("🎡 바퀴 Grabbable 설정")]
    public Transform wheelParent; // 바퀴 부모 오브젝트 (Left_Wheel, Right_Wheel)
    public Transform wheelVisual; // 실제 바퀴 모델링
    public Transform wheelCollider; // 실린더 콜라이더 오브젝트
    public Grabbable grabbableComponent; // Grabbable 컴포넌트
    
    [Header("🔧 자동 설정")]
    public bool autoSetupOnStart = true;
    public bool createGrabbableIfMissing = true;
    public bool setupColliderIfMissing = true;
    
    [Header("📏 바퀴 설정")]
    public float wheelRadius = 0.3f;
    public float wheelWidth = 0.1f;
    public LayerMask grabbableLayer = 11; // Grabbable 레이어
    
    void Start()
    {
        if (autoSetupOnStart)
        {
            SetupWheelGrabbable();
        }
    }
    
    [ContextMenu("바퀴 Grabbable 설정")]
    public void SetupWheelGrabbable()
    {
        if (wheelParent == null)
        {
            wheelParent = transform;
        }
        
        // 1. Grabbable 컴포넌트 설정
        SetupGrabbableComponent();
        
        // 2. 콜라이더 설정
        SetupWheelCollider();
        
        // 3. 바퀴 시각적 요소 찾기
        FindWheelVisual();
        
        // 4. 레이어 설정
        SetupLayers();
        
        Debug.Log($"🎡 {wheelParent.name} 바퀴 Grabbable 설정 완료");
    }
    
    void SetupGrabbableComponent()
    {
        // Grabbable 컴포넌트가 없으면 추가
        if (grabbableComponent == null)
        {
            grabbableComponent = wheelParent.GetComponent<Grabbable>();
        }
        
        if (grabbableComponent == null && createGrabbableIfMissing)
        {
            grabbableComponent = wheelParent.gameObject.AddComponent<Grabbable>();
            Debug.Log($"🎡 {wheelParent.name}에 Grabbable 컴포넌트 추가됨");
        }
        
        if (grabbableComponent != null)
        {
            // Grabbable 설정 최적화 (올바른 속성명으로 수정)
            // grabbableComponent.handFollowStrength = 30f; // 손 따라가기 강도
            // grabbableComponent.handRotationStrength = 20f; // 손 회전 강도
            // grabbableComponent.throwPower = 0f; // 던지기 비활성화
            // grabbableComponent.ignoreWeight = true; // 무게 무시
            // grabbableComponent.parentOnGrab = false; // 잡을 때 부모 설정 안함
            // grabbableComponent.maintainGrabOffset = true; // 잡기 오프셋 유지
            
            // 바퀴는 특정 축으로만 회전하도록 제한
            Rigidbody wheelRb = grabbableComponent.body;
            if (wheelRb == null)
            {
                wheelRb = wheelParent.gameObject.AddComponent<Rigidbody>();
                grabbableComponent.body = wheelRb;
            }
            
            // 바퀴 물리 설정
            wheelRb.mass = 5f; // 적당한 무게
            wheelRb.drag = 2f; // 공기 저항
            wheelRb.angularDrag = 5f; // 각속도 저항
            wheelRb.useGravity = false; // 중력 비활성화 (휠체어에 고정)
            
            // 바퀴는 Y축 이동과 X, Z축 회전을 제한
            wheelRb.constraints = RigidbodyConstraints.FreezePositionY | 
                                 RigidbodyConstraints.FreezeRotationX | 
                                 RigidbodyConstraints.FreezeRotationZ;
        }
    }
    
    void SetupWheelCollider()
    {
        // 기존 콜라이더 찾기
        if (wheelCollider == null)
        {
            // "Collider" 이름의 자식 오브젝트 찾기
            Transform colliderChild = wheelParent.Find("Collider");
            if (colliderChild != null)
            {
                wheelCollider = colliderChild;
            }
        }
        
        // 콜라이더가 없으면 생성
        if (wheelCollider == null && setupColliderIfMissing)
        {
            GameObject colliderObj = new GameObject("WheelCollider");
            colliderObj.transform.SetParent(wheelParent);
            colliderObj.transform.localPosition = Vector3.zero;
            colliderObj.transform.localRotation = Quaternion.identity;
            wheelCollider = colliderObj.transform;
            
            Debug.Log($"🎡 {wheelParent.name}에 새 콜라이더 오브젝트 생성됨");
        }
        
        if (wheelCollider != null)
        {
            // 실린더 콜라이더 설정
            CapsuleCollider capsuleCol = wheelCollider.GetComponent<CapsuleCollider>();
            if (capsuleCol == null)
            {
                capsuleCol = wheelCollider.gameObject.AddComponent<CapsuleCollider>();
            }
            
            // 바퀴 모양으로 콜라이더 설정
            capsuleCol.radius = wheelRadius;
            capsuleCol.height = wheelWidth;
            capsuleCol.direction = 0; // X축 방향 (바퀴 회전축)
            capsuleCol.isTrigger = false;
            
            // 콜라이더 회전 (바퀴가 세로로 서도록)
            wheelCollider.localRotation = Quaternion.Euler(0, 0, 90);
            
            // 물리 재질 설정 (선택사항)
            PhysicMaterial wheelPhysics = new PhysicMaterial("WheelPhysics");
            wheelPhysics.dynamicFriction = 0.6f;
            wheelPhysics.staticFriction = 0.8f;
            wheelPhysics.bounciness = 0.1f;
            capsuleCol.material = wheelPhysics;
            
            Debug.Log($"🎡 {wheelParent.name} 콜라이더 설정 완료 - 반지름: {wheelRadius}, 폭: {wheelWidth}");
        }
    }
    
    void FindWheelVisual()
    {
        if (wheelVisual == null)
        {
            // 바퀴 시각적 요소 자동 찾기
            MeshRenderer[] renderers = wheelParent.GetComponentsInChildren<MeshRenderer>();
            foreach (MeshRenderer renderer in renderers)
            {
                // "Collider"가 아닌 렌더러를 찾기
                if (!renderer.name.Contains("Collider") && renderer.enabled)
                {
                    wheelVisual = renderer.transform;
                    break;
                }
            }
            
            if (wheelVisual != null)
            {
                Debug.Log($"🎡 {wheelParent.name} 시각적 바퀴 발견: {wheelVisual.name}");
            }
        }
    }
    
    void SetupLayers()
    {
        // 바퀴 부모와 모든 자식을 Grabbable 레이어로 설정
        SetLayerRecursively(wheelParent.gameObject, grabbableLayer);
    }
    
    void SetLayerRecursively(GameObject obj, LayerMask layer)
    {
        obj.layer = (int)Mathf.Log(layer.value, 2);
        
        foreach (Transform child in obj.transform)
        {
            SetLayerRecursively(child.gameObject, layer);
        }
    }
    
    // 공개 메서드들
    public Grabbable GetGrabbableComponent()
    {
        return grabbableComponent;
    }
    
    public Transform GetWheelVisual()
    {
        return wheelVisual;
    }
    
    public Transform GetWheelCollider()
    {
        return wheelCollider;
    }
    
    public bool IsGrabbableSetup()
    {
        return grabbableComponent != null && wheelCollider != null;
    }
    
    [ContextMenu("바퀴 정보 출력")]
    public void LogWheelInfo()
    {
        Debug.Log($"🎡 {wheelParent.name} 바퀴 정보:");
        Debug.Log($"  - Grabbable: {(grabbableComponent != null ? "설정됨" : "없음")}");
        Debug.Log($"  - 콜라이더: {(wheelCollider != null ? "설정됨" : "없음")}");
        Debug.Log($"  - 시각적 요소: {(wheelVisual != null ? wheelVisual.name : "없음")}");
        Debug.Log($"  - 레이어: {wheelParent.gameObject.layer}");
        
        if (grabbableComponent != null && grabbableComponent.body != null)
        {
            Rigidbody rb = grabbableComponent.body;
            Debug.Log($"  - Rigidbody 제약: {rb.constraints}");
            Debug.Log($"  - 질량: {rb.mass}");
        }
    }
    
    // 바퀴 회전 테스트
    [ContextMenu("바퀴 회전 테스트")]
    public void TestWheelRotation()
    {
        if (wheelVisual != null)
        {
            // 90도 회전 테스트
            wheelVisual.Rotate(90f, 0, 0);
            Debug.Log($"🎡 {wheelParent.name} 바퀴 90도 회전 테스트 실행");
        }
        else
        {
            Debug.LogWarning($"🎡 {wheelParent.name} 시각적 바퀴를 찾을 수 없음");
        }
    }
    
    // 바퀴 잡기 테스트
    public void TestGrabbable()
    {
        if (grabbableComponent != null)
        {
            Debug.Log($"🎡 {wheelParent.name} Grabbable 테스트:");
            // Debug.Log($"  - 잡을 수 있음: {grabbableComponent.CanGrab()}");
            Debug.Log($"  - 현재 잡는 손: {grabbableComponent.GetHeldBy().Count}개");
        }
        else
        {
            Debug.LogWarning($"🎡 {wheelParent.name} Grabbable 컴포넌트가 없음");
        }
    }
} 
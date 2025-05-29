using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Autohand;

[System.Serializable]
public class WheelchairSetupHelper : MonoBehaviour
{
    [Header("자동 설정")]
    [SerializeField] private bool autoSetup = true;
    [SerializeField] private GameObject wheelchairPrefab;
    
    [Header("생성할 빈 오브젝트들")]
    [SerializeField] private bool createEmptyObjects = true;
    
    [Header("바퀴 설정")]
    [SerializeField] private Transform leftWheelMesh;
    [SerializeField] private Transform rightWheelMesh;
    [SerializeField] private float wheelRadius = 0.3f;
    
    [Header("휠체어 크기")]
    [SerializeField] private float chairLength = 1.2f; // 휠체어 길이
    [SerializeField] private float chairWidth = 0.6f;  // 휠체어 너비
    
    private WheelchairController wheelchairController;
    
    void Start()
    {
        if (autoSetup)
        {
            SetupWheelchair();
        }
    }
    
    [ContextMenu("Setup Wheelchair")]
    public void SetupWheelchair()
    {
        wheelchairController = GetComponent<WheelchairController>();
        if (wheelchairController == null)
        {
            wheelchairController = gameObject.AddComponent<WheelchairController>();
        }
        
        // Rigidbody 설정
        SetupRigidbody();
        
        if (createEmptyObjects)
        {
            // 필요한 빈 오브젝트들 생성
            CreateEmptyObjects();
        }
        
        // Grabbable 컴포넌트 설정
        SetupGrabbables();
        
        Debug.Log("휠체어 설정이 완료되었습니다!");
    }
    
    void SetupRigidbody()
    {
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }
        
        // 휠체어에 적합한 물리 설정
        rb.mass = 50f;
        rb.drag = 2f;
        rb.angularDrag = 5f;
        rb.centerOfMass = new Vector3(0, -0.5f, 0);
        rb.maxAngularVelocity = 5f;
        
        wheelchairController.chairRigidbody = rb;
    }
    
    void CreateEmptyObjects()
    {
        // 휠체어 중앙 포인트 생성
        GameObject chairCenter = CreateEmptyChild("ChairCenter", Vector3.zero);
        wheelchairController.chairCenter = chairCenter.transform;
        
        // 바퀴 중심 포인트들 생성
        Vector3 leftWheelPos = new Vector3(-chairWidth * 0.5f, 0, 0);
        Vector3 rightWheelPos = new Vector3(chairWidth * 0.5f, 0, 0);
        
        GameObject leftWheelCenter = CreateEmptyChild("LeftWheelCenter", leftWheelPos);
        GameObject rightWheelCenter = CreateEmptyChild("RightWheelCenter", rightWheelPos);
        
        wheelchairController.leftWheelCenter = leftWheelCenter.transform;
        wheelchairController.rightWheelCenter = rightWheelCenter.transform;
        
        // 바퀴 메시 설정
        if (leftWheelMesh != null)
        {
            wheelchairController.leftWheel = leftWheelMesh;
        }
        if (rightWheelMesh != null)
        {
            wheelchairController.rightWheel = rightWheelMesh;
        }
        
        // 높이 감지 포인트들 생성
        Vector3 frontPos = new Vector3(0, 0, chairLength * 0.4f);
        Vector3 rearPos = new Vector3(0, 0, -chairLength * 0.4f);
        
        GameObject frontHeightPoint = CreateEmptyChild("FrontHeightPoint", frontPos);
        GameObject rearHeightPoint = CreateEmptyChild("RearHeightPoint", rearPos);
        
        wheelchairController.frontHeightPoint = frontHeightPoint.transform;
        wheelchairController.rearHeightPoint = rearHeightPoint.transform;
        
        // 시각적 표시를 위한 기즈모 추가
        AddGizmoComponent(chairCenter, Color.cyan, 0.2f);
        AddGizmoComponent(leftWheelCenter, Color.green, 0.15f);
        AddGizmoComponent(rightWheelCenter, Color.green, 0.15f);
        AddGizmoComponent(frontHeightPoint, Color.red, 0.1f);
        AddGizmoComponent(rearHeightPoint, Color.blue, 0.1f);
    }
    
    GameObject CreateEmptyChild(string name, Vector3 localPosition)
    {
        GameObject child = new GameObject(name);
        child.transform.SetParent(transform);
        child.transform.localPosition = localPosition;
        child.transform.localRotation = Quaternion.identity;
        return child;
    }
    
    void AddGizmoComponent(GameObject obj, Color color, float size)
    {
        GizmoHelper gizmo = obj.AddComponent<GizmoHelper>();
        gizmo.color = color;
        gizmo.size = size;
    }
    
    void SetupGrabbables()
    {
        // 왼쪽 바퀴 Grabbable 설정
        if (leftWheelMesh != null)
        {
            SetupWheelGrabbable(leftWheelMesh.gameObject, "LeftWheel");
            wheelchairController.leftWheelGrab = leftWheelMesh.GetComponent<Grabbable>();
        }
        
        // 오른쪽 바퀴 Grabbable 설정
        if (rightWheelMesh != null)
        {
            SetupWheelGrabbable(rightWheelMesh.gameObject, "RightWheel");
            wheelchairController.rightWheelGrab = rightWheelMesh.GetComponent<Grabbable>();
        }
    }
    
    void SetupWheelGrabbable(GameObject wheelObj, string wheelName)
    {
        // Grabbable 컴포넌트 추가
        Grabbable grabbable = wheelObj.GetComponent<Grabbable>();
        if (grabbable == null)
        {
            grabbable = wheelObj.AddComponent<Grabbable>();
        }
        
        // Collider 확인 및 추가
        Collider col = wheelObj.GetComponent<Collider>();
        if (col == null)
        {
            // 바퀴 모양에 맞는 Collider 추가
            CapsuleCollider capsule = wheelObj.AddComponent<CapsuleCollider>();
            capsule.direction = 0; // X축 방향
            capsule.radius = wheelRadius;
            capsule.height = 0.1f; // 바퀴 두께
        }
        
        // Grabbable 설정 (올바른 AutoHand API 사용)
        grabbable.name = wheelName;
        grabbable.grabType = HandGrabType.HandToGrabbable; // 올바른 enum 사용
        grabbable.throwPower = 0f; // 던지기 비활성화
        grabbable.jointBreakForce = 1000f; // 관절 파괴 힘 설정
        grabbable.instantGrab = false; // 즉시 잡기 비활성화
        grabbable.maintainGrabOffset = true; // 잡기 오프셋 유지
        grabbable.parentOnGrab = false; // 잡을 때 부모 설정 안함
        
        // 물리 설정
        Rigidbody wheelRb = wheelObj.GetComponent<Rigidbody>();
        if (wheelRb == null)
        {
            wheelRb = wheelObj.AddComponent<Rigidbody>();
        }
        
        wheelRb.mass = 5f;
        wheelRb.drag = 1f;
        wheelRb.angularDrag = 1f;
        wheelRb.isKinematic = true; // 바퀴는 kinematic으로 설정
        
        // Grabbable의 body 참조 설정
        grabbable.body = wheelRb;
    }
    
    [ContextMenu("Reset Wheelchair")]
    public void ResetWheelchair()
    {
        // 기존 설정 초기화
        if (wheelchairController != null)
        {
            DestroyImmediate(wheelchairController);
        }
        
        // 생성된 빈 오브젝트들 제거
        Transform[] children = GetComponentsInChildren<Transform>();
        foreach (Transform child in children)
        {
            if (child != transform && (child.name.Contains("Center") || 
                child.name.Contains("Point") || child.name.Contains("Helper")))
            {
                DestroyImmediate(child.gameObject);
            }
        }
        
        Debug.Log("휠체어 설정이 초기화되었습니다.");
    }
    
    void OnValidate()
    {
        // Inspector에서 값이 변경될 때 자동으로 업데이트
        if (wheelchairController != null && createEmptyObjects)
        {
            UpdatePositions();
        }
    }
    
    void UpdatePositions()
    {
        // 바퀴 중심 위치 업데이트
        if (wheelchairController.leftWheelCenter != null)
        {
            wheelchairController.leftWheelCenter.localPosition = new Vector3(-chairWidth * 0.5f, 0, 0);
        }
        if (wheelchairController.rightWheelCenter != null)
        {
            wheelchairController.rightWheelCenter.localPosition = new Vector3(chairWidth * 0.5f, 0, 0);
        }
        
        // 높이 감지 포인트 위치 업데이트
        if (wheelchairController.frontHeightPoint != null)
        {
            wheelchairController.frontHeightPoint.localPosition = new Vector3(0, 0, chairLength * 0.4f);
        }
        if (wheelchairController.rearHeightPoint != null)
        {
            wheelchairController.rearHeightPoint.localPosition = new Vector3(0, 0, -chairLength * 0.4f);
        }
    }
}

// 기즈모 표시를 위한 헬퍼 클래스
public class GizmoHelper : MonoBehaviour
{
    public Color color = Color.white;
    public float size = 0.1f;
    
    void OnDrawGizmos()
    {
        Gizmos.color = color;
        Gizmos.DrawWireSphere(transform.position, size);
    }
}

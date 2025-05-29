using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WheelLiftPrevention : MonoBehaviour
{
    [Header("휠체어 구성 요소")]
    [SerializeField] private Rigidbody wheelchairBody;
    [SerializeField] private Transform[] wheels;
    [SerializeField] private Rigidbody[] wheelRigidbodies;
    
    [Header("바퀴 들림 방지 설정")]
    [SerializeField] private float wheelAnchorSpring = 50000f;
    [SerializeField] private float wheelAnchorDamper = 1000f;
    [SerializeField] private float maxWheelLift = 0.05f; // 바퀴가 들릴 수 있는 최대 높이
    [SerializeField] private float groundCheckDistance = 0.2f;
    [SerializeField] private LayerMask groundLayerMask = 1;
    
    [Header("무게중심 및 안정성")]
    [SerializeField] private Vector3 centerOfMassOffset = new Vector3(0, -0.5f, 0);
    [SerializeField] private float stabilityForce = 2000f;
    [SerializeField] private float maxAngularVelocity = 2f;
    
    private ConfigurableJoint[] wheelJoints;
    private Vector3[] originalWheelPositions;
    private bool[] wheelGrounded;
    
    void Start()
    {
        InitializeWheelchairStability();
        SetupWheelAnchors();
    }
    
    void InitializeWheelchairStability()
    {
        if (wheelchairBody == null)
            wheelchairBody = GetComponent<Rigidbody>();
            
        // 무게중심을 낮게 설정하여 안정성 향상
        wheelchairBody.centerOfMass = centerOfMassOffset;
        
        // 각속도 제한으로 과도한 회전 방지
        wheelchairBody.maxAngularVelocity = maxAngularVelocity;
        
        // 바퀴 개수만큼 배열 초기화
        if (wheels != null && wheels.Length > 0)
        {
            originalWheelPositions = new Vector3[wheels.Length];
            wheelGrounded = new bool[wheels.Length];
            
            for (int i = 0; i < wheels.Length; i++)
            {
                originalWheelPositions[i] = wheels[i].localPosition;
                wheelGrounded[i] = true;
            }
        }
    }
    
    void SetupWheelAnchors()
    {
        if (wheels == null || wheelRigidbodies == null) return;
        
        wheelJoints = new ConfigurableJoint[wheels.Length];
        
        for (int i = 0; i < wheels.Length; i++)
        {
            if (wheels[i] == null || wheelRigidbodies[i] == null) continue;
            
            // 바퀴에 ConfigurableJoint 추가
            ConfigurableJoint joint = wheelRigidbodies[i].gameObject.AddComponent<ConfigurableJoint>();
            joint.connectedBody = wheelchairBody;
            
            // 위치 제한 설정 - Y축(상하) 움직임 제한
            joint.xMotion = ConfigurableJointMotion.Locked;
            joint.yMotion = ConfigurableJointMotion.Limited;
            joint.zMotion = ConfigurableJointMotion.Locked;
            
            // 회전 설정 - Y축 회전만 허용 (바퀴 굴리기)
            joint.angularXMotion = ConfigurableJointMotion.Locked;
            joint.angularYMotion = ConfigurableJointMotion.Free;
            joint.angularZMotion = ConfigurableJointMotion.Locked;
            
            // Y축 위치 제한 설정
            SoftJointLimit yLimit = new SoftJointLimit();
            yLimit.limit = maxWheelLift;
            yLimit.bounciness = 0f;
            yLimit.contactDistance = 0.01f;
            joint.linearLimit = yLimit;
            
            // 스프링 드라이브로 바퀴를 원래 위치에 고정
            JointDrive yDrive = new JointDrive();
            yDrive.positionSpring = wheelAnchorSpring;
            yDrive.positionDamper = wheelAnchorDamper;
            yDrive.maximumForce = float.MaxValue;
            joint.yDrive = yDrive;
            
            wheelJoints[i] = joint;
        }
    }
    
    void FixedUpdate()
    {
        CheckWheelGrounding();
        ApplyStabilityForces();
        PreventExcessiveTilting();
    }
    
    void CheckWheelGrounding()
    {
        if (wheels == null) return;
        
        for (int i = 0; i < wheels.Length; i++)
        {
            if (wheels[i] == null) continue;
            
            // 바퀴 아래로 레이캐스트하여 지면 체크
            RaycastHit hit;
            Vector3 rayStart = wheels[i].position + Vector3.up * 0.1f;
            bool isGrounded = Physics.Raycast(rayStart, Vector3.down, out hit, groundCheckDistance, groundLayerMask);
            
            wheelGrounded[i] = isGrounded;
            
            // 바퀴가 지면에서 너무 멀어지면 강제로 끌어내리기
            if (!isGrounded && wheelJoints[i] != null)
            {
                Vector3 targetPos = wheelJoints[i].targetPosition;
                targetPos.y = -maxWheelLift;
                wheelJoints[i].targetPosition = targetPos;
            }
        }
    }
    
    void ApplyStabilityForces()
    {
        if (wheelchairBody == null) return;
        
        // 휠체어가 기울어져 있으면 안정화 힘 적용
        Vector3 upDirection = transform.up;
        float tiltAngle = Vector3.Angle(upDirection, Vector3.up);
        
        if (tiltAngle > 1f) // 1도 이상 기울어졌을 때
        {
            Vector3 correctionDirection = Vector3.Cross(Vector3.Cross(upDirection, Vector3.up), upDirection);
            wheelchairBody.AddForce(correctionDirection.normalized * stabilityForce * tiltAngle * Time.fixedDeltaTime, ForceMode.Force);
        }
    }
    
    void PreventExcessiveTilting()
    {
        if (wheelchairBody == null) return;
        
        // 과도한 각속도 제한
        Vector3 angularVel = wheelchairBody.angularVelocity;
        if (angularVel.magnitude > maxAngularVelocity)
        {
            wheelchairBody.angularVelocity = angularVel.normalized * maxAngularVelocity;
        }
        
        // X축과 Z축 회전이 과도하면 보정
        Vector3 euler = transform.eulerAngles;
        bool needsCorrection = false;
        
        if (euler.x > 15f && euler.x < 180f)
        {
            euler.x = 15f;
            needsCorrection = true;
        }
        else if (euler.x > 180f && euler.x < 345f)
        {
            euler.x = 345f;
            needsCorrection = true;
        }
        
        if (euler.z > 15f && euler.z < 180f)
        {
            euler.z = 15f;
            needsCorrection = true;
        }
        else if (euler.z > 180f && euler.z < 345f)
        {
            euler.z = 345f;
            needsCorrection = true;
        }
        
        if (needsCorrection)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(euler), Time.fixedDeltaTime * 5f);
        }
    }
    
    // 바퀴가 잡혔을 때 추가 제약 적용
    public void OnWheelGrabbed(int wheelIndex)
    {
        if (wheelJoints != null && wheelIndex < wheelJoints.Length && wheelJoints[wheelIndex] != null)
        {
            // 바퀴가 잡혔을 때 Y축 움직임을 더 강하게 제한
            JointDrive yDrive = wheelJoints[wheelIndex].yDrive;
            yDrive.positionSpring = wheelAnchorSpring * 2f;
            wheelJoints[wheelIndex].yDrive = yDrive;
        }
    }
    
    // 바퀴가 놓였을 때 원래 설정으로 복원
    public void OnWheelReleased(int wheelIndex)
    {
        if (wheelJoints != null && wheelIndex < wheelJoints.Length && wheelJoints[wheelIndex] != null)
        {
            JointDrive yDrive = wheelJoints[wheelIndex].yDrive;
            yDrive.positionSpring = wheelAnchorSpring;
            wheelJoints[wheelIndex].yDrive = yDrive;
        }
    }
    
    void OnDrawGizmos()
    {
        // 바퀴 지면 체크 시각화
        if (wheels != null)
        {
            Gizmos.color = Color.yellow;
            for (int i = 0; i < wheels.Length; i++)
            {
                if (wheels[i] != null)
                {
                    Vector3 start = wheels[i].position + Vector3.up * 0.1f;
                    Gizmos.DrawRay(start, Vector3.down * groundCheckDistance);
                }
            }
        }
        
        // 무게중심 시각화
        if (wheelchairBody != null)
        {
            Gizmos.color = Color.red;
            Vector3 comWorld = transform.TransformPoint(wheelchairBody.centerOfMass);
            Gizmos.DrawSphere(comWorld, 0.1f);
        }
    }
} 
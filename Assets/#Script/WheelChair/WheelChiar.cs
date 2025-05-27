// using System.Collections;
// using System.Collections.Generic;
// using UnityEngine;
// using Autohand;

// public class WheelChiar : MonoBehaviour
// {
//     public Rigidbody chairRigidbody;
//     public Transform leftWheel, rightWheel;
//     public Rigidbody leftWheelRb, rightWheelRb;
//     public Grabbable leftWheelGrab, rightWheelGrab;

//     public float wheelForce = 500f;
//     public float brakeForce = 1000f;
//     public float inertia = 0.98f;
//     public float turnMultiplier = 2f;
//     public float maxTiltAngle = 15f; // 최대 기울기 각도

//     private float leftInput, rightInput;
//     private bool leftGrabbed, rightGrabbed;
//     private Vector3 lastLeftHandPos, lastRightHandPos;
//     private bool lastLeftGrabbed, lastRightGrabbed;
//     private Vector3 initialPosition;
//     private Quaternion initialRotation;
//     private ConfigurableJoint chairJoint;
//     private ConfigurableJoint leftWheelJoint, rightWheelJoint;

//     // Start is called before the first frame update
//     void Start()
//     {
//         initialPosition = transform.position;
//         initialRotation = transform.rotation;
//         SetupChairJoint();
//         SetupWheelJoints();
        
//         // 의자의 무게중심을 낮게 설정
//         chairRigidbody.centerOfMass = Vector3.down * 0.3f;
        
//         // 회전 안정성을 위한 설정
//         chairRigidbody.maxAngularVelocity = 3.0f;
//         chairRigidbody.angularDrag = 5.0f;
//     }

//     void SetupChairJoint()
//     {
//         // 의자 본체에 조인트 추가
//         GameObject anchor = new GameObject("ChairAnchor");
//         anchor.transform.position = transform.position;
//         anchor.transform.rotation = transform.rotation;
        
//         chairJoint = gameObject.AddComponent<ConfigurableJoint>();
//         chairJoint.connectedBody = null; // 월드에 고정
//         chairJoint.configuredInWorldSpace = true;
        
//         // 위치 이동은 자유롭게
//         chairJoint.xMotion = ConfigurableJointMotion.Free;
//         chairJoint.zMotion = ConfigurableJointMotion.Free;
//         chairJoint.yMotion = ConfigurableJointMotion.Limited;
        
//         // X축 회전 제한 설정
//         var limit = new SoftJointLimit();
//         limit.limit = maxTiltAngle;
//         chairJoint.lowAngularXLimit = limit;  // 아래쪽 제한
        
//         var highLimit = new SoftJointLimit();
//         highLimit.limit = maxTiltAngle;
//         chairJoint.highAngularXLimit = highLimit;  // 위쪽 제한

//         // Z축 회전 제한 설정
//         var zLimit = new SoftJointLimit();
//         zLimit.limit = maxTiltAngle;
//         chairJoint.angularZLimit = zLimit;
        
//         // 회전 복원력 설정
//         var drive = new JointDrive();
//         drive.positionSpring = 1000f;
//         drive.positionDamper = 50f;
//         drive.maximumForce = 1000f;
        
//         chairJoint.angularDrive = drive;
        
//         // Y축 위치 제한
//         var yLimit = new SoftJointLimit();
//         yLimit.limit = 0.1f; // 약간의 상하 움직임만 허용
//         chairJoint.linearLimit = yLimit;
//     }

//     void SetupWheelJoints()
//     {
//         // 왼쪽 바퀴 조인트 설정
//         leftWheelJoint = leftWheel.gameObject.AddComponent<ConfigurableJoint>();
//         ConfigureWheelJoint(leftWheelJoint, leftWheelRb);

//         // 오른쪽 바퀴 조인트 설정
//         rightWheelJoint = rightWheel.gameObject.AddComponent<ConfigurableJoint>();
//         ConfigureWheelJoint(rightWheelJoint, rightWheelRb);
//     }

//     void ConfigureWheelJoint(ConfigurableJoint joint, Rigidbody rb)
//     {
//         joint.connectedBody = chairRigidbody;
        
//         // XYZ 위치 잠금
//         joint.xMotion = ConfigurableJointMotion.Locked;
//         joint.yMotion = ConfigurableJointMotion.Locked;
//         joint.zMotion = ConfigurableJointMotion.Locked;

//         // X, Z축 회전 잠금 (Y축만 자유롭게)
//         joint.angularXMotion = ConfigurableJointMotion.Locked;
//         joint.angularYMotion = ConfigurableJointMotion.Free;
//         joint.angularZMotion = ConfigurableJointMotion.Locked;

//         // 회전 드라이브 설정
//         var drive = new JointDrive();
//         drive.positionSpring = 10000;
//         drive.positionDamper = 100;
//         drive.maximumForce = 10000;

//         joint.angularDrive = drive;
//     }

//     void FixedUpdate()
//     {
//         leftGrabbed = leftWheelGrab.GetHeldBy().Count > 0;
//         rightGrabbed = rightWheelGrab.GetHeldBy().Count > 0;

//         // 바퀴 입력 계산
//         leftInput = GetWheelInput(leftWheel, leftWheelGrab, ref lastLeftHandPos, ref lastLeftGrabbed);
//         rightInput = GetWheelInput(rightWheel, rightWheelGrab, ref lastRightHandPos, ref lastRightGrabbed);

//         // 이동 및 회전 계산
//         float forwardInput = (leftInput + rightInput) * 0.5f;
//         float turnInput = (rightInput - leftInput) * turnMultiplier;

//         // 이동 벡터 계산 (Y축 이동 제한)
//         Vector3 moveDirection = transform.forward * forwardInput;
//         moveDirection.y = 0;
//         Vector3 move = moveDirection * wheelForce * Time.fixedDeltaTime;

//         // 경사로 처리
//         if (!leftGrabbed && !rightGrabbed)
//         {
//             Vector3 velocity = chairRigidbody.velocity;
//             velocity.y = Mathf.Max(velocity.y, 0); // 아래로 떨어지는 속도 제한
//             chairRigidbody.velocity = velocity * inertia;
//         }
//         else
//         {
//             if (Mathf.Abs(forwardInput) < 0.01f && Mathf.Abs(turnInput) < 0.01f)
//             {
//                 Vector3 velocity = chairRigidbody.velocity;
//                 velocity.y = Mathf.Max(velocity.y, 0);
//                 chairRigidbody.velocity = velocity * 0.8f;
                
//                 if (chairRigidbody.velocity.magnitude < 0.01f)
//                 {
//                     chairRigidbody.velocity = Vector3.zero;
//                 }
//             }
//         }

//         // 이동 및 회전 적용
//         chairRigidbody.AddForce(move, ForceMode.Force);
//         chairRigidbody.AddTorque(Vector3.up * turnInput * wheelForce, ForceMode.Force);

//         // 바퀴 회전 시각화
//         UpdateWheelRotation(leftWheel, leftInput);
//         UpdateWheelRotation(rightWheel, rightInput);

//         // 안정성 유지를 위한 추가 처리
//         StabilizeChair();
//     }

//     void StabilizeChair()
//     {
//         // 현재 기울기 각도 확인
//         float currentTiltX = Vector3.Angle(transform.up, Vector3.up);
        
//         // 심하게 기울어졌을 때 보정력 적용
//         if (currentTiltX > maxTiltAngle)
//         {
//             Vector3 correctionTorque = Vector3.Cross(transform.up, Vector3.up);
//             chairRigidbody.AddTorque(correctionTorque * 1000f * Time.fixedDeltaTime, ForceMode.Force);
//         }
//     }

//     float GetWheelInput(Transform wheel, Grabbable grab, ref Vector3 lastHandPos, ref bool lastGrabbed)
//     {
//         if (grab.GetHeldBy().Count == 0)
//         {
//             lastGrabbed = false;
//             return 0f;
//         }

//         Hand hand = grab.GetHeldBy()[0] as Hand;
//         if (hand == null)
//             return 0f;

//         Vector3 handPos = hand.transform.position;

//         if (!lastGrabbed)
//         {
//             lastHandPos = handPos;
//             lastGrabbed = true;
//             return 0f;
//         }

//         Vector3 delta = handPos - lastHandPos;
//         delta.y = 0;
        
//         Vector3 wheelForward = wheel.forward;
//         wheelForward.y = 0;
//         wheelForward.Normalize();
        
//         float input = Vector3.Dot(delta, wheelForward);

//         lastHandPos = handPos;
//         return input;
//     }

//     void UpdateWheelRotation(Transform wheel, float input)
//     {
//         wheel.Rotate(Vector3.right * input * 100f, Space.Self);
//     }

//     // Update is called once per frame
//     void Update()
//     {
        
//     }
// }

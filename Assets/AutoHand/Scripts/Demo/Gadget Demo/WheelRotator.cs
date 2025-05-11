using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Autohand;

namespace Autohand.Demo{
    public class WheelRotator : PhysicsGadgetHingeAngleReader{
        public Transform move;
        public Vector3 angle;
        public bool useLocal = false;
        
        [Header("바퀴 설정")]
        public float wheelRadius = 0.5f;         // 바퀴 반지름
        public float rotationMultiplier = 1f;    // 회전량 배수
        public bool useWheelPhysics = true;      // 물리 기반 바퀴 회전 사용
        
        private float totalRotation = 0f;        // 누적 회전량
        private float lastAngle = 0f;            // 이전 각도
        private Vector3 lastPosition;            // 이전 위치
        private float wheelCircumference;        // 바퀴 둘레

        void Start()
        {
            if(move != null)
            {
                lastAngle = GetCurrentAngle();
                lastPosition = useLocal ? move.localPosition : move.position;
                wheelCircumference = 2f * Mathf.PI * wheelRadius;
            }
        }

        void Update(){
            if(move == null) return;

            float currentAngle = GetCurrentAngle();
            float angleDelta = currentAngle - lastAngle;
            
            // 회전량 누적
            totalRotation += angleDelta * rotationMultiplier;

            if (useWheelPhysics)
            {
                // 물리 기반 바퀴 회전
                float rotationAmount = GetValue() * Time.deltaTime;
                Vector3 rotationAxis = GetRotationAxis();
                
                // 바퀴 회전
                if(useLocal)
                {
                    move.localRotation *= Quaternion.Euler(rotationAxis * rotationAmount);
                    // 바퀴 위치 이동 (굴러가는 효과)
                    Vector3 moveDirection = GetMoveDirection();
                    float moveDistance = (rotationAmount / 360f) * wheelCircumference;
                    move.localPosition += moveDirection * moveDistance;
                }
                else
                {
                    move.rotation *= Quaternion.Euler(rotationAxis * rotationAmount);
                    // 바퀴 위치 이동 (굴러가는 효과)
                    Vector3 moveDirection = GetMoveDirection();
                    float moveDistance = (rotationAmount / 360f) * wheelCircumference;
                    move.position += moveDirection * moveDistance;
                }
            }
            else
            {
                // 기존 회전 방식
                if(useLocal)
                    move.localRotation *= Quaternion.Euler(angle*Time.deltaTime*GetValue());
                else
                    move.rotation *= Quaternion.Euler(angle*Time.deltaTime*GetValue());
            }
                
            lastAngle = currentAngle;
            lastPosition = useLocal ? move.localPosition : move.position;
        }

        private Vector3 GetRotationAxis()
        {
            // 회전 축 결정
            if (Mathf.Abs(angle.x) > 0)
                return Vector3.right;
            else if (Mathf.Abs(angle.y) > 0)
                return Vector3.up;
            else
                return Vector3.forward;
        }

        private Vector3 GetMoveDirection()
        {
            // 이동 방향 결정 (회전 축에 수직인 방향)
            Vector3 rotationAxis = GetRotationAxis();
            if (rotationAxis == Vector3.right)
                return Vector3.forward;
            else if (rotationAxis == Vector3.up)
                return Vector3.right;
            else
                return Vector3.right;
        }

        // 현재 회전 각도 가져오기
        private float GetCurrentAngle()
        {
            if(move == null) return 0f;
            
            Vector3 currentEuler = useLocal ? move.localEulerAngles : move.eulerAngles;
            
            if (Mathf.Abs(angle.x) > 0)
                return currentEuler.x;
            else if (Mathf.Abs(angle.y) > 0)
                return currentEuler.y;
            else
                return currentEuler.z;
        }

        // 현재까지의 총 회전량 반환 (바퀴 회전 횟수)
        public float GetTotalRotation()
        {
            return totalRotation / 360f;
        }

        // 회전량 초기화
        public void ResetRotation()
        {
            totalRotation = 0f;
            lastAngle = GetCurrentAngle();
            lastPosition = useLocal ? move.localPosition : move.position;
        }

        // 현재 회전량 가져오기
        public float GetRotationValue()
        {
            return GetTotalRotation();
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class SubwayMove : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float movementSpeed = 15f;
    [SerializeField] private float arrivalDistance = 30f;
    [SerializeField] private float departureDistance = 30f;
    
    [Header("Arrival Settings")]
    [SerializeField] private float initialSpeed = 15f; // 초기 속도
    [SerializeField] private Ease decelerationEase = Ease.OutQuad;
    
    [Header("Departure Settings")]
    [SerializeField] private float accelerationDuration = 5f;
    [SerializeField] private Ease accelerationEase = Ease.InQuad;
    
    private Vector3 stationPosition; // 역 정차 위치
    private Vector3 initialPosition; // 지하철 초기 위치
    private Vector3 departurePosition;
    private Tween currentMovementTween;
    
    private void Awake()
    {
        // 현재 위치를 정차 위치로 설정
        stationPosition = transform.position;
        
        // 초기 위치를 정차 위치에서 arrivalDistance만큼 뒤로 설정
        initialPosition = stationPosition - transform.forward * arrivalDistance;

        MoveToInitialPosition();
    }
    
    // 지하철을 초기 위치로 이동시키는 메서드
    public void MoveToInitialPosition()
    {
        KillCurrentTween();
        transform.position = initialPosition;
    }
    
    // Called to trigger subway arrival at the station
    public void TriggerArrival()
    {
        // Kill any existing movement tween
        KillCurrentTween();
        
        // 현재 위치를 감속 시작 위치로 저장
        Vector3 arrivalStartPosition = transform.position;
        
        // 정차 위치까지의 거리 계산
        float distanceToStation = Vector3.Distance(arrivalStartPosition, stationPosition);
        
        // 초기 속도와 거리를 기반으로 감속 시간 계산
        // 감속은 0까지 줄어드는 것이므로 평균 속도는 initialSpeed/2
        float decelerationTime = distanceToStation / (initialSpeed / 2);
        
        // 감속 트윈 생성
        currentMovementTween = transform.DOMove(stationPosition, decelerationTime)
            .SetEase(decelerationEase)
            .OnComplete(() => {
                Debug.Log("Subway arrived at station");
            });
    }
    
    // Called to trigger subway departure from the station
    public void TriggerDeparture()
    {
        // Kill any existing movement tween
        KillCurrentTween();
        
        // Calculate departure position (current position + departure distance)
        departurePosition = stationPosition + transform.forward * departureDistance;
        
        // Create the acceleration tween
        currentMovementTween = transform.DOMove(departurePosition, accelerationDuration)
            .SetEase(accelerationEase)
            .OnComplete(() => {
                Debug.Log("Subway departed from station");
            });
    }
    
    // Utility method to kill current tween safely
    private void KillCurrentTween()
    {
        if (currentMovementTween != null && currentMovementTween.IsActive())
        {
            currentMovementTween.Kill();
        }
    }
    
    // Use this to reset subway position for testing
    public void ResetPosition()
    {
        KillCurrentTween();
        transform.position = stationPosition;
    }
    
    // 디버깅용 - 초기 위치 및 정차 위치 기즈모로 표시
    private void OnDrawGizmosSelected()
    {
        if (Application.isPlaying)
        {
            // 정차 위치 (빨간색)
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(stationPosition, 0.5f);
            
            // 초기 위치 (파란색)
            Gizmos.color = Color.blue;
            Gizmos.DrawSphere(initialPosition, 0.5f);
            
            // 출발 후 위치 (녹색)
            if (departurePosition != Vector3.zero)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawSphere(departurePosition, 0.5f);
            }
        }
    }

    private void Start()
    {
        TriggerArrival();
    }
}

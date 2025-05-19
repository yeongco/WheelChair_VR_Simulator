using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class SubwayVibration : MonoBehaviour
{
    [Header("진동 설정")]
    [SerializeField] private bool useVibration = true; // 진동 효과 사용 여부
    [SerializeField] private float vibrationStrength = 0.05f; // 진동 강도
    [SerializeField] private float vibrationDuration = 0.5f; // 진동 지속 시간
    [SerializeField] private float vibrationInterval = 1f; // 진동 간격
    [SerializeField] private Vector3 vibrationDirection = new Vector3(0f, 1f, 0f); // 진동 방향 (기본값: 수직 방향)
    
    [Header("진동 효과음")]
    [SerializeField] private bool useSound = false; // 효과음 사용 여부
    [SerializeField] private AudioSource audioSource; // 오디오 소스
    [SerializeField] private AudioClip[] vibrationSounds; // 진동 효과음 클립
    [SerializeField] [Range(0f, 1f)] private float soundVolume = 0.5f; // 효과음 볼륨
    [SerializeField] [Range(0.8f, 1.2f)] private float pitchVariation = 1.1f; // 음높이 변화

    [Header("속도 설정")]
    [SerializeField] private float movementSpeed = 5f; // 지하철 이동 속도
    [SerializeField] private bool applySpeedToVibration = true; // 속도에 따른 진동 강도 변화 적용
    [SerializeField] [Range(0f, 1f)] private float speedInfluence = 0.5f; // 속도가 진동에 미치는 영향

    // 진동 관련 변수
    private Sequence vibrationSequence;
    private Vector3 originalPosition;
    private bool isShaking = false;

    private void Start()
    {
        // 원래 위치 저장
        originalPosition = transform.localPosition;

        // 오디오 소스가 지정되지 않았다면 자동으로 찾기
        if (useSound && audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.spatialBlend = 1f; // 3D 사운드
                audioSource.loop = false;
                audioSource.playOnAwake = false;
            }
        }

        // 진동 효과 시작
        if (useVibration)
        {
            StartVibrationEffect();
        }
    }

    private void OnEnable()
    {
        // 활성화될 때 진동 효과 시작
        if (useVibration && vibrationSequence == null && gameObject.activeInHierarchy)
        {
            StartVibrationEffect();
        }
    }

    private void OnDisable()
    {
        // 비활성화될 때 진동 효과 중지
        StopVibrationEffect();
    }

    // 지하철 진동 효과 시작
    public void StartVibrationEffect()
    {
        // 기존 진동 시퀀스가 있다면 정지
        if (vibrationSequence != null)
        {
            vibrationSequence.Kill();
        }

        // 진동 시퀀스 생성
        vibrationSequence = DOTween.Sequence();
        
        // 주기적으로 진동 효과 추가
        vibrationSequence.AppendCallback(() => {
            // 현재 속도에 따른 진동 강도 조절
            float currentStrength = vibrationStrength;
            if (applySpeedToVibration)
            {
                currentStrength *= Mathf.Lerp(0.5f, 1.5f, speedInfluence * movementSpeed / 10f);
            }
            
            // 랜덤한 강도로 진동 생성 (변동폭 추가)
            float randomStrength = currentStrength * Random.Range(0.8f, 1.2f);
            
            // 랜덤한 방향으로 약간 변형된 진동 방향 계산
            Vector3 randomDirection = vibrationDirection + new Vector3(
                Random.Range(-0.2f, 0.2f),
                Random.Range(-0.2f, 0.2f),
                Random.Range(-0.2f, 0.2f)
            );
            
            // 이미 진동 중이 아닐 때만 새 진동 시작
            if (!isShaking)
            {
                isShaking = true;
                
                // 진동 효과 생성
                transform.DOShakePosition(vibrationDuration, randomDirection * randomStrength, 10, 90, false, false)
                    .OnComplete(() => {
                        // 진동 후 원래 위치로 부드럽게 복귀
                        transform.DOLocalMove(originalPosition, 0.2f).OnComplete(() => {
                            isShaking = false;
                        });
                        
                        // 효과음 재생
                        PlayVibrationSound();
                    });
            }
        });
        
        // 대기 시간 추가 (랜덤 간격)
        vibrationSequence.AppendInterval(vibrationInterval * Random.Range(0.8f, 1.2f));
        
        // 무한 반복
        vibrationSequence.SetLoops(-1);
        vibrationSequence.Play();
    }

    // 효과음 재생
    private void PlayVibrationSound()
    {
        if (useSound && audioSource != null && vibrationSounds != null && vibrationSounds.Length > 0)
        {
            // 랜덤 효과음 선택
            AudioClip randomClip = vibrationSounds[Random.Range(0, vibrationSounds.Length)];
            if (randomClip != null)
            {
                // 랜덤 음높이 설정
                audioSource.pitch = Random.Range(1f / pitchVariation, pitchVariation);
                audioSource.volume = soundVolume * Random.Range(0.9f, 1.1f);
                audioSource.PlayOneShot(randomClip);
            }
        }
    }

    // 진동 효과 중지
    public void StopVibrationEffect()
    {
        if (vibrationSequence != null)
        {
            vibrationSequence.Kill();
            vibrationSequence = null;
        }
        
        // 진동 중지 후 원래 위치로 복귀
        transform.DOKill();
        transform.DOLocalMove(originalPosition, 0.5f);
        isShaking = false;
    }

    // 진동 강도 설정
    public void SetVibrationStrength(float strength)
    {
        vibrationStrength = strength;
        if (useVibration && vibrationSequence != null)
        {
            // 진동 설정 변경 시 재시작
            StopVibrationEffect();
            StartVibrationEffect();
        }
    }

    // 이동 속도 설정
    public void SetMovementSpeed(float speed)
    {
        movementSpeed = speed;
        // 속도가 변하면 진동에도 영향을 줌
        if (applySpeedToVibration && useVibration && vibrationSequence != null)
        {
            // 진동 간격을 속도에 반비례하게 조정 (더 빠를수록 더 자주 진동)
            float newInterval = Mathf.Lerp(vibrationInterval * 1.5f, vibrationInterval * 0.5f, 
                                          speedInfluence * movementSpeed / 10f);
            
            // 진동 설정 변경 시 재시작
            StopVibrationEffect();
            vibrationInterval = newInterval;
            StartVibrationEffect();
        }
    }

    // 특별한 순간에 강한 진동 효과 생성 (터널 통과, 급정거 등)
    public void CreateSpecialVibration(float strengthMultiplier = 2.5f, float durationMultiplier = 1.5f)
    {
        // 현재 진동 일시 중지
        StopVibrationEffect();
        
        // 특별 진동 생성
        transform.DOShakePosition(vibrationDuration * durationMultiplier, 
                                 vibrationDirection * vibrationStrength * strengthMultiplier, 
                                 15, 90, false, false)
            .OnComplete(() => {
                // 진동 후 원래 위치로 부드럽게 복귀
                transform.DOLocalMove(originalPosition, 0.3f).OnComplete(() => {
                    // 일반 진동 다시 시작
                    if (useVibration)
                    {
                        StartVibrationEffect();
                    }
                });
                
                // 효과음 재생 (더 큰 볼륨으로)
                if (useSound && audioSource != null && vibrationSounds != null && vibrationSounds.Length > 0)
                {
                    AudioClip randomClip = vibrationSounds[Random.Range(0, vibrationSounds.Length)];
                    if (randomClip != null)
                    {
                        audioSource.pitch = Random.Range(0.8f, 0.9f); // 더 낮은 음높이
                        audioSource.volume = soundVolume * 1.5f;
                        audioSource.PlayOneShot(randomClip);
                    }
                }
            });
    }

    private void OnDestroy()
    {
        // 모든 DOTween 애니메이션 정리
        DOTween.Kill(transform);
        if (vibrationSequence != null)
        {
            vibrationSequence.Kill();
        }
    }
} 
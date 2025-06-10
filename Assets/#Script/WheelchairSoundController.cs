using UnityEngine;

public class WheelchairSoundController : MonoBehaviour
{
    [Header("사운드 설정")]
    [SerializeField] private AudioSource wheelchairSound;
    [SerializeField] private AudioClip wheelRollingSound;
    
    [Header("속도 설정")]
    [SerializeField] private float minSpeed = 0.1f;        // 최소 속도 (이 속도 이하면 소리 재생 안 함)
    [SerializeField] private float maxSpeed = 5f;         // 최대 속도 (이 속도 이상이면 최대 피치)
    [SerializeField] private float minPitch = 0.5f;       // 최소 피치
    [SerializeField] private float maxPitch = 1.5f;       // 최대 피치
    [SerializeField] private float volumeFadeSpeed = 2f;  // 볼륨 페이드 속도
    
    private Vector3 previousPosition;
    private float currentVolume = 0f;
    private bool isMoving = false;
    
    void Start()
    {
        // 초기 위치 저장
        previousPosition = transform.position;
        
        // AudioSource 설정
        if (wheelchairSound == null)
        {
            wheelchairSound = gameObject.AddComponent<AudioSource>();
        }
        
        // 오디오 소스 초기 설정
        wheelchairSound.clip = wheelRollingSound;
        wheelchairSound.loop = true;
        wheelchairSound.playOnAwake = false;
        wheelchairSound.volume = 0f;
        wheelchairSound.pitch = minPitch;
    }
    
    void Update()
    {
        // 현재 위치에서 Y축 제외
        Vector3 currentPosition = transform.position;
        Vector3 previousHorizontalPos = new Vector3(previousPosition.x, 0, previousPosition.z);
        Vector3 currentHorizontalPos = new Vector3(currentPosition.x, 0, currentPosition.z);
        
        // 위치 변화로 속도 계산
        Vector3 positionDelta = currentHorizontalPos - previousHorizontalPos;
        float currentSpeed = positionDelta.magnitude / Time.deltaTime;
        
        // 속도에 따른 피치 계산
        float speedRatio = Mathf.Clamp01((currentSpeed - minSpeed) / (maxSpeed - minSpeed));
        float targetPitch = Mathf.Lerp(minPitch, maxPitch, speedRatio);
        
        // 속도에 따른 볼륨 계산
        float targetVolume = currentSpeed > minSpeed ? 1f : 0f;
        
        // 볼륨 부드럽게 변경
        currentVolume = Mathf.Lerp(currentVolume, targetVolume, Time.deltaTime * volumeFadeSpeed);
        
        // 오디오 소스 업데이트
        UpdateAudioSource(currentSpeed, targetPitch, currentVolume);
        
        // 현재 위치를 이전 위치로 저장
        previousPosition = currentPosition;
    }
    
    void UpdateAudioSource(float currentSpeed, float targetPitch, float targetVolume)
    {
        // 최소 속도 이상일 때만 소리 재생
        if (currentSpeed > minSpeed)
        {
            if (!wheelchairSound.isPlaying)
            {
                wheelchairSound.Play();
            }
            
            // 피치와 볼륨 업데이트
            wheelchairSound.pitch = targetPitch;
            wheelchairSound.volume = targetVolume;
        }
        else
        {
            // 볼륨이 거의 0이 되면 재생 중지
            if (currentVolume < 0.01f && wheelchairSound.isPlaying)
            {
                wheelchairSound.Stop();
            }
        }
    }
    
    // 에디터에서 시각화 (선택 사항)
    void OnDrawGizmosSelected()
    {
        // 현재 속도 표시
        Vector3 currentPosition = transform.position;
        Vector3 previousHorizontalPos = new Vector3(previousPosition.x, 0, previousPosition.z);
        Vector3 currentHorizontalPos = new Vector3(currentPosition.x, 0, currentPosition.z);
        
        Vector3 positionDelta = currentHorizontalPos - previousHorizontalPos;
        float currentSpeed = positionDelta.magnitude / Time.deltaTime;
        
        // 속도에 따른 색상 계산
        float speedRatio = Mathf.Clamp01((currentSpeed - minSpeed) / (maxSpeed - minSpeed));
        Color speedColor = Color.Lerp(Color.green, Color.red, speedRatio);
        
        // 속도 벡터 시각화
        Gizmos.color = speedColor;
        Gizmos.DrawRay(transform.position, positionDelta.normalized * currentSpeed);
    }
} 
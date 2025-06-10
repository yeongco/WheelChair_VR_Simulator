using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class FirstPersonController : MonoBehaviour
{
    [Header("이동 설정")]
    [SerializeField] float moveSpeed = 5f;              // 기본 이동 속도
    [SerializeField] float gravity = -9.81f;            // 중력 가속도

    [Header("마우스 설정")]
    [SerializeField] float lookSensitivity = 2f;        // 마우스 감도
    [SerializeField] float maxLookAngle = 90f;          // 상하 제한 각도

    CharacterController cc;
    Transform cam;                                       // 카메라 트랜스폼
    float verticalVelocity;
    float pitch = 0f;                                    // 상하 카메라 회전 값

    void Awake()
    {
        cc = GetComponent<CharacterController>();
        cam = GetComponentInChildren<Camera>().transform;
        // 마우스 커서 고정/숨김
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        HandleMouseLook();
        HandleMovement();
    }

    // 마우스 움직임에 따른 시선 처리
    void HandleMouseLook()
    {
        float mouseX = Input.GetAxis("Mouse X") * lookSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * lookSensitivity;

        // 좌우 회전 (플레이어 전체 Y축)
        transform.Rotate(Vector3.up * mouseX);

        // 상하 회전 (카메라 피치)
        pitch -= mouseY;
        pitch = Mathf.Clamp(pitch, -maxLookAngle, maxLookAngle);
        cam.localEulerAngles = Vector3.right * pitch;
    }

    // 키보드 입력에 따른 이동 및 중력 처리
    void HandleMovement()
    {
        // 수평 이동 입력
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        Vector3 move = transform.right * x + transform.forward * z;
        move *= moveSpeed;

        // 중력 적용
        if (cc.isGrounded)
        {
            verticalVelocity = -1f;  // 바닥에 붙어 있게 약간의 음수
        }
        else
        {
            verticalVelocity += gravity * Time.deltaTime;
        }
        move.y = verticalVelocity;

        // 실제 이동
        cc.Move(move * Time.deltaTime);
    }
}

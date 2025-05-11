using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class wheel : MonoBehaviour
{
    [Header("바퀴 설정")]
    public float wheelSpeed = 100f;        // 바퀴 회전 속도
    public float maxSpeed = 10f;          // 최대 이동 속도
    public float acceleration = 5f;       // 가속도
    public float deceleration = 2f;       // 감속도

    private Rigidbody rb;
    private float currentSpeed = 0f;
    private bool isMoving = false;

    // Start is called before the first frame update
    void Start()
    {
        // Rigidbody 컴포넌트 가져오기
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }
        
        // Rigidbody 설정
        rb.mass = 1f;
        rb.drag = 0.5f;
        rb.angularDrag = 0.5f;
        rb.constraints = RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
    }

    // Update is called once per frame
    void Update()
    {
        // 입력 처리
        float input = Input.GetAxis("Horizontal"); // 좌우 입력 받기
        
        if (input != 0)
        {
            isMoving = true;
            // 가속
            currentSpeed = Mathf.MoveTowards(currentSpeed, maxSpeed * Mathf.Abs(input), acceleration * Time.deltaTime);
        }
        else
        {
            isMoving = false;
            // 감속
            currentSpeed = Mathf.MoveTowards(currentSpeed, 0f, deceleration * Time.deltaTime);
        }

        // 바퀴 회전
        if (isMoving)
        {
            transform.Rotate(Vector3.right * wheelSpeed * Time.deltaTime * Mathf.Sign(input));
        }
    }

    void FixedUpdate()
    {
        // 물리 기반 이동
        if (isMoving)
        {
            Vector3 movement = transform.forward * currentSpeed;
            rb.velocity = new Vector3(movement.x, rb.velocity.y, movement.z);
        }
    }
}

using System.Collections;
using UnityEngine;
using static OVRInput;

public class WheelInteractable : MonoBehaviour
{
    Rigidbody m_Rigidbody;
    float wheelRadius;
    bool onSlope = false;
    [SerializeField] bool hapticsEnabled = true;
    [Range(0, 0.5f)]
    [SerializeField] float deselectionThreshold = 0.25f;
    GameObject grabPoint;

    private Vector3 lastInteractorPosition;
    [SerializeField]
    private bool isGrabbing = false;
    private Transform currentHand;
    private bool isLeftHand;

    private void Start()
    {
        m_Rigidbody = GetComponent<Rigidbody>();
        wheelRadius = GetComponent<SphereCollider>().radius;
        StartCoroutine(CheckForSlope());
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!isGrabbing && other.gameObject.layer == LayerMask.NameToLayer("Hand"))
        {
            Debug.Log("Enter");
            isLeftHand = other.name.Contains("Left");
            currentHand = other.transform;
            CheckGrabInput();
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Hand") && currentHand == other.transform)
        {
            CheckGrabInput();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (isGrabbing && other.transform == currentHand)
        {
            EndGrab();
        }
    }

    private void CheckGrabInput()
    {
        if (!isGrabbing)
        {
            // 그립 버튼이 눌렸는지 확인
            if (OVRInput.GetDown(Button.PrimaryHandTrigger, isLeftHand ? Controller.LTouch : Controller.RTouch))
            {
                Debug.Log("grab");
                StartGrab();
            }
        }
        else
        {
            // 그립 버튼이 떼어졌는지 확인
            if (OVRInput.GetUp(Button.PrimaryHandTrigger, isLeftHand ? Controller.LTouch : Controller.RTouch))
            {
                EndGrab();
            }
        }
    }

    private void StartGrab()
    {
        isGrabbing = true;
        lastInteractorPosition = currentHand.position;
        
        SpawnGrabPoint();
        StartCoroutine(BrakeAssist());
        StartCoroutine(MonitorDetachDistance());
        if (hapticsEnabled)
        {
            StartCoroutine(SendHapticFeedback());
        }
    }

    private void EndGrab()
    {
        isGrabbing = false;
        if (grabPoint)
        {
            Destroy(grabPoint);
            grabPoint = null;
        }
        currentHand = null;
    }

    void SpawnGrabPoint()
    {
        if (grabPoint)
        {
            Destroy(grabPoint);
        }

        grabPoint = new GameObject($"{transform.name}'s grabPoint", typeof(Rigidbody), typeof(FixedJoint));
        grabPoint.transform.position = currentHand.position;
        grabPoint.GetComponent<FixedJoint>().connectedBody = GetComponent<Rigidbody>();
    }

    IEnumerator BrakeAssist()
    {
        while (grabPoint && isGrabbing)
        {
            Vector3 currentPosition = currentHand.position;
            Vector3 interactorVelocity = (currentPosition - lastInteractorPosition) / Time.deltaTime;
            lastInteractorPosition = currentPosition;

            if (interactorVelocity.z < 0.005f && interactorVelocity.z > -0.005f)
            {
                m_Rigidbody.AddTorque(-m_Rigidbody.angularVelocity.normalized * 25f);
                SpawnGrabPoint();
            }
            yield return new WaitForFixedUpdate();
        }
    }

    IEnumerator MonitorDetachDistance()
    {
        while (grabPoint && isGrabbing)
        {
            if (Vector3.Distance(transform.position, currentHand.position) >= wheelRadius + deselectionThreshold)
            {
                EndGrab();
            }
            yield return null;
        }
    }

    IEnumerator SendHapticFeedback()
    {
        float runInterval = 0.1f;
        Vector3 lastAngularVelocity = new Vector3(transform.InverseTransformDirection(m_Rigidbody.angularVelocity).x, 0f, 0f);

        while (grabPoint && isGrabbing)
        {
            Vector3 currentAngularVelocity = new Vector3(transform.InverseTransformDirection(m_Rigidbody.angularVelocity).x, 0f, 0f);
            Vector3 angularAcceleration = (currentAngularVelocity - lastAngularVelocity) / runInterval;

            if (Vector3.Dot(currentAngularVelocity.normalized, angularAcceleration.normalized) < 0f)
            {
                float impulseAmplitude = Mathf.Abs(angularAcceleration.x);
                if (impulseAmplitude > 1.5f)
                {
                    float remappedImpulseAmplitude = Remap(impulseAmplitude, 1.5f, 40f, 0f, 1f);
                    OVRInput.SetControllerVibration(1, remappedImpulseAmplitude, isLeftHand ? Controller.LTouch : Controller.RTouch);
                    yield return new WaitForSeconds(runInterval * 2f);
                    OVRInput.SetControllerVibration(0, 0, isLeftHand ? Controller.LTouch : Controller.RTouch);
                }
            }

            lastAngularVelocity = currentAngularVelocity;
            yield return new WaitForSeconds(runInterval);
        }
    }

    float Remap(float value, float from1, float to1, float from2, float to2)
    {
        return (value - from1) / (to1 - from1) * (to2 - from2) + from2;
    }

    IEnumerator CheckForSlope()
    {
        while (true)
        {
            if (Physics.Raycast(transform.position, -Vector3.up, out RaycastHit hit))
            {
                onSlope = hit.normal != Vector3.up;
            }
            yield return new WaitForSeconds(0.1f);
        }
    }
}
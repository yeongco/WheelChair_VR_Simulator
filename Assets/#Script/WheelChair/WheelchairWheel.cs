using UnityEngine;

public class WheelchairWheel : MonoBehaviour
{
    private float previousRotation;
    public float rotateMultipliyer = 1;

    void Start()
    {
        previousRotation = transform.localEulerAngles.z;
    }

    public float GetLocalZRotationDelta()
    {
        float currentRotation = transform.localEulerAngles.z;
        float delta = Mathf.DeltaAngle(previousRotation, currentRotation)*rotateMultipliyer;
        previousRotation = currentRotation;
        return delta;
    }
}

using UnityEngine;

/// <summary>
/// Animates wheelchair meshes to express the physical movement of the rig.
/// </summary>
public class VRWC_MeshAnimator : MonoBehaviour
{
    public Rigidbody frame;

    public Transform wheelLeft;
    public Transform wheelRight;

    public Transform wheelLeftMesh;
    public Transform wheelRightMesh;


    void Update()
    {
        if (frame.velocity.magnitude > 0.05f)
        {
            RotateWheels();
        }
    }

    void RotateWheels()
    {
        wheelLeftMesh.rotation = wheelLeft.rotation;
        wheelRightMesh.rotation = wheelRight.rotation;
    }
}

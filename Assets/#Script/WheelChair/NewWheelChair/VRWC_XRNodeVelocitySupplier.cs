using UnityEngine;
using UnityEngine.XR;
using UnityEngine.InputSystem.XR;

/// <summary>
/// Supplies the current velocity of a specific XRNode. This class is meant to supplement the ActionBasedController by providing a XR Controller with velocity input.
/// </summary>
public class VRWC_XRNodeVelocitySupplier : MonoBehaviour
{
    private TrackedPoseDriver _poseDriver;
    private Vector3 _lastPosition;
    private Vector3 _velocity = Vector3.zero;
    private bool _isInitialized = false;

    /// <summary>
    /// Most recently tracked velocity of attached transform. Read only.;
    /// </summary>
    public Vector3 velocity { get => _velocity; }

    private void Start()
    {
        _poseDriver = GetComponent<TrackedPoseDriver>();
        if (_poseDriver == null)
        {
            Debug.LogError("TrackedPoseDriver not found on this object!");
            return;
        }
        _lastPosition = transform.position;
        _isInitialized = true;
    }

    private void Update()
    {
        if (!_isInitialized) return;

        Vector3 currentPosition = transform.position;
        _velocity = (currentPosition - _lastPosition) / Time.deltaTime;
        _lastPosition = currentPosition;
    }

    private void OnDisable()
    {
        _isInitialized = false;
        _velocity = Vector3.zero;
    }
}

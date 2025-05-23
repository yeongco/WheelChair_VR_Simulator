using UnityEngine;
using System.Collections;

public enum DoorDirection
{
    Up, Down, Left, Right, Forward, Back
}

public class SlidingDoor : MonoBehaviour
{
    [Header("Door Settings")]
    public DoorDirection openDirection = DoorDirection.Right;
    public float openDistance = 1f;
    public float openDuration = 1f;

    [Header("Animation Curve")]
    public AnimationCurve openCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Linked Doors (Optional)")]
    public SlidingDoor[] linkedDoors;

    private Vector3 closedPosition;
    private Vector3 openPosition;
    private Coroutine currentCoroutine;
    private bool isOpen = false;

    void Start()
    {
        closedPosition = transform.localPosition;
        openPosition = closedPosition + GetDirectionVector(openDirection) * openDistance;
    }

    public void OpenDoor()
    {
        if (isOpen || currentCoroutine != null) return;

        StartLinkedDoors(true);
        currentCoroutine = StartCoroutine(MoveDoor(openPosition, true));
    }

    public void CloseDoor()
    {
        if (!isOpen || currentCoroutine != null) return;

        StartLinkedDoors(false);
        currentCoroutine = StartCoroutine(MoveDoor(closedPosition, false));
    }

    private IEnumerator MoveDoor(Vector3 target, bool opening)
    {
        Vector3 start = transform.localPosition;
        float elapsed = 0f;

        while (elapsed < openDuration)
        {
            float t = elapsed / openDuration;
            float curveT = openCurve.Evaluate(t);
            transform.localPosition = Vector3.Lerp(start, target, curveT);
            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.localPosition = target;
        isOpen = opening;
        currentCoroutine = null;
    }

    private void StartLinkedDoors(bool opening)
    {
        foreach (var door in linkedDoors)
        {
            if (door != null)
            {
                if (opening) door.OpenDoor();
                else door.CloseDoor();
            }
        }
    }

    private Vector3 GetDirectionVector(DoorDirection dir)
    {
        return dir switch
        {
            DoorDirection.Up => transform.up,
            DoorDirection.Down => -transform.up,
            DoorDirection.Left => -transform.right,
            DoorDirection.Right => transform.right,
            DoorDirection.Forward => transform.forward,
            DoorDirection.Back => -transform.forward,
            _ => transform.right,
        };
    }
}

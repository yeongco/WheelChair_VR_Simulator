using UnityEngine;
using System.Collections;

public class DoorTrigger : MonoBehaviour
{
    public SlidingDoor targetDoor;
    public float delayBeforeClose = 2f;

    private Coroutine closeCoroutine = null;
    private int triggerCount = 0; // 여러 콜라이더가 들어올 수 있어서 감지 수 카운트

    private void OnTriggerEnter(Collider other)
    {
        if (!IsValidTarget(other)) return;

        triggerCount++;
        if (triggerCount == 1)
        {
            targetDoor.OpenDoor();
        }

        // 나갔다가 다시 들어오면 닫기 예약 취소
        if (closeCoroutine != null)
        {
            StopCoroutine(closeCoroutine);
            closeCoroutine = null;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!IsValidTarget(other)) return;

        triggerCount = Mathf.Max(triggerCount - 1, 0);

        if (triggerCount == 0)
        {
            // 아무도 없을 때만 닫기 예약 시작
            closeCoroutine = StartCoroutine(DelayedClose());
        }
    }

    private IEnumerator DelayedClose()
    {
        yield return new WaitForSeconds(delayBeforeClose);
        targetDoor.CloseDoor();
        closeCoroutine = null;
    }

    private bool IsValidTarget(Collider other)
    {
        return other.CompareTag("Player"); // 필요 시 태그 조건 변경 가능
    }
}
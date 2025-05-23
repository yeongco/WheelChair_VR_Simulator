using System.Collections;
using UnityEngine;

public class TutoMoveStep : QuestStep
{
    [SerializeField]
    private float targetMoveValue = 10f; // 통과 기준 거리

    [SerializeField]
    private float nowMoveValue = 0f; // 누적 이동 거리

    private Vector3 lastPosition;

    public GameObject player;

    private void Start()
    {
        if (player == null)
            player = EventsManager.instance.player;

        lastPosition = player.transform.position;
    }

    private void Update()
    {
        Vector3 currentPosition = player.transform.position;
        float deltaDistance = Vector3.Distance(lastPosition, currentPosition);

        if (deltaDistance > 0.001f) // 아주 미세한 움직임은 무시
        {
            nowMoveValue += deltaDistance;
            lastPosition = currentPosition;

            Debug.Log($"누적 거리: {nowMoveValue}");

            if (nowMoveValue >= targetMoveValue)
            {
                FinishQuestStep();
            }
        }
    }
}

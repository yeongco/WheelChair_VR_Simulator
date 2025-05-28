using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveStep : QuestStep
{
    [Header("이동 설정")]
    [Tooltip("목표 위치에 도달했는지 확인할 자식 오브젝트")]
    public GameObject player;

    private void OnEnable()
    {
        if (player == null)
            player = EventsManager.instance.player;
    }

    private void OnTriggerEnter(Collider other)
    {
        // 트리거에 들어온 오브젝트가 플레이어인지 확인
        if (other.CompareTag("Player"))
        {
            Debug.Log("플레이어 진입");
            FinishQuestStep();
        }
    }
}

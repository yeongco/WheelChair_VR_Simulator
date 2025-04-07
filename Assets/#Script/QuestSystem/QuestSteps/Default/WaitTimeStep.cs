using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaitTimeStep : QuestStep
{
    [SerializeField]
    private float time = 5f; //���ð�
    [SerializeField]
    private float alphaTime = 0f; //�߰����ð�

    Coroutine timerCoroutine;

    private void OnEnable()
    {
        if(OnStartNarration != null)
        {
            time = OnStartNarration.length + alphaTime ;

        }
        timerCoroutine = StartCoroutine(StartTimer());
    }

    private void OnDisable()
    {
        StopCoroutine(timerCoroutine);
    }

    IEnumerator StartTimer()
    {
        yield return new WaitForSeconds(time);
        FinishQuestStep();
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class WaitEnterA : QuestStep
{
    public UnityEvent OnStepNarrationDone;
    [SerializeField]
    private float time = 3f;
    [SerializeField]
    private float alphaTime = 0f;
    public bool waitTimer = true;
    Coroutine timerCoroutine;

    private void OnEnable()
    {
        // 버튼 입력 이벤트 구독
        EventsManager.instance.inputEvents.onButtonPressed += OnButtonPressed;

        if(OnStartNarration != null)
        {
            time = OnStartNarration.length + alphaTime;
            waitTimer = true;
        }
        timerCoroutine = StartCoroutine(StartTimer());
    }

    private void OnDisable()
    {
        // 이벤트 구독 해제
        EventsManager.instance.inputEvents.onButtonPressed -= OnButtonPressed;
        
        if(timerCoroutine != null)
        {
            StopCoroutine(timerCoroutine);
        }
    }

    private IEnumerator StartTimer()
    {
        yield return new WaitForSeconds(time);
        waitTimer = false;
        OnStepNarrationDone.Invoke();
    }

    private void OnButtonPressed(ControllerButton button)
    {
        // 내레이션 재생 중이면 버튼 입력 무시
        if(waitTimer) return;

        // Primary 버튼이 눌렸을 때 퀘스트 스텝 완료
        if(button == ControllerButton.A)
        {
            Debug.Log("Primary 버튼 눌림");
            FinishQuestStep();
        }
    }
}

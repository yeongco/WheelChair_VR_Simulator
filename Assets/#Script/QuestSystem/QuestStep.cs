using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

//QuestStep 스크립트를 작성할 때 이 클래스를 상속 받아서 작성
public abstract class QuestStep : MonoBehaviour
{
    private bool isFinished = false; //스텝 완료 여부
    public Quest quest; //속한 퀘스트
    public string text = "";
    //public int stepIndex; //스텝의 인덱스
    [SerializeField] public AudioClip OnStartNarration;

    public UnityEvent OnStepStart;
    public UnityEvent OnStepFinished;

    private void OnEnable()
    {
        //Debug.Log(text);
        //OnStepStart.Invoke();
    }
    
    //초기화
    public void InitializeQuestStep(Quest quest)
    {
        isFinished = false;
        this.quest = quest;
        //this.stepIndex = stepIndex;
    }

    //스텝 완료
    protected void FinishQuestStep()
    {
        if (!isFinished)
        {
            isFinished = true;
            EventsManager.instance.questEvents.AdvanceQuest(quest);
            //OnStepFinished.Invoke();
            //Destroy(this.gameObject);
            gameObject.SetActive(false);
        }
    }

}

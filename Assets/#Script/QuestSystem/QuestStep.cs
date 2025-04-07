using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

//QuestStep ��ũ��Ʈ�� �ۼ��� �� �� Ŭ������ ��� �޾Ƽ� �ۼ�
public abstract class QuestStep : MonoBehaviour
{
    private bool isFinished = false; //���� �Ϸ� ����
    public Quest quest; //���� ����Ʈ
    public string text = "";
    //public int stepIndex; //������ �ε���
    [SerializeField] public AudioClip OnStartNarration;

    public UnityEvent OnStepStart;
    public UnityEvent OnStepFinished;

    private void OnEnable()
    {
        //Debug.Log(text);
        //OnStepStart.Invoke();
    }
    
    //�ʱ�ȭ
    public void InitializeQuestStep(Quest quest)
    {
        isFinished = false;
        this.quest = quest;
        //this.stepIndex = stepIndex;
    }

    //���� �Ϸ�
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

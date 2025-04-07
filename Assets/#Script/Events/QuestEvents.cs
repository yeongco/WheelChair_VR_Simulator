using System;

public class QuestEvents
{
    public event Action<int> onStartQuest; //퀘스트 시작 이벤트
    public void StartQuest(int index)
    {
        if (onStartQuest != null)
        {
            onStartQuest(index);
        }
    }

    public event Action<Quest> onAdvanceQuest; //퀘스트 Step 갱신 이벤트
    public void AdvanceQuest(Quest quest)
    {
        if (onAdvanceQuest != null)
        {
            onAdvanceQuest(quest);
        }
    }

    public event Action<Quest> onFinishQuest; //퀘스트 종료 이벤트
    public void FinishQuest(Quest quest)
    {
        if (onFinishQuest != null)
        {
            onFinishQuest(quest);
        }
    }

    public event Action<Quest> onQuestStateChange; //퀘스트 상태 갱신 이벤트
    public void QuestStateChange(Quest quest)
    {
        if (onQuestStateChange != null)
        {
            onQuestStateChange(quest);
        }
    }
}

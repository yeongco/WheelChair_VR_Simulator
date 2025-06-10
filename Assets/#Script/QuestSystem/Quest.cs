using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;

public class Quest : MonoBehaviour
{
    [field: SerializeField] public string id { get; private set; }

    [Header("General")]
    public QuestManager manager; //����Ʈ �Ŵ���
    public string displayName; //����Ʈ �̸�

    [Header("Steps")]
    public GameObject[] questSteps; //����Ʈ ���� ������

    [Header("Etc")]
    public QuestState state; //����Ʈ ����
    public int currentQuestStepIndex; //���� ����

    public AudioSource _audio;

    private void Awake()
    {
        List<GameObject> steps = new List<GameObject>();

        // transform의 직계 자식만 순회
        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            steps.Add(child.gameObject);
        }

        questSteps = steps.ToArray();
        manager = transform.parent.GetComponent<QuestManager>();
        _audio = GetComponent<AudioSource>();
    }

    private void OnEnable() //Quest������Ʈ�� Active�Ǹ� ����Ʈ ����
    {
        List<GameObject> steps = new List<GameObject>();

        // transform의 직계 자식만 순회
        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            steps.Add(child.gameObject);
        }

        questSteps = steps.ToArray();
        manager = transform.parent.GetComponent<QuestManager>();
        _audio = GetComponent<AudioSource>();
        
        for (int i = 0; i < questSteps.Length; i++)
        {
            questSteps[i].GetComponent<QuestStep>().InitializeQuestStep(this);
            questSteps[i].gameObject.SetActive(false);
        }

        for (int i = 0; i < manager.questList.Length; i++)
        {
            if (this == manager.questList[i])
            {
                EventsManager.instance.questEvents.StartQuest(i);
                break;
            }
        }


        currentQuestStepIndex = 0;
        Debug.Log("현재퀘스트번호 : " + currentQuestStepIndex);
        questSteps[currentQuestStepIndex].gameObject.SetActive(true); //0�� ���ܺ��� Ȱ��ȭ
        if (_audio != null)
        {
            AudioClip clip = questSteps[currentQuestStepIndex].GetComponent<QuestStep>().OnStartNarration;
            if (clip != null)
            {
                _audio.clip = clip;
                _audio.Play(); //Step�� ������ �� �����̼� ���
            }
        }
        questSteps[currentQuestStepIndex].GetComponent<QuestStep>().OnStepStart.Invoke();
        //manager.ui.setTextColor(currentQuestStepIndex, Color.yellow);
    }

    public void MoveToNextStep() //���� �������� �̵�
    {
        questSteps[currentQuestStepIndex].GetComponent<QuestStep>().OnStepFinished.Invoke();
        questSteps[currentQuestStepIndex].SetActive(false); //���� ���� ��Ȱ��ȭ
        //manager.ui.setTextColor(currentQuestStepIndex, Color.gray);
        currentQuestStepIndex++; //�ε��� ����
        if (CurrentStepExists()) //���� ������ �ִ� ���
        {
            Debug.Log(questSteps.Length);
            Debug.Log("현재퀘스트번호 : " + currentQuestStepIndex);
            questSteps[currentQuestStepIndex].SetActive(true); //Ȱ��ȭ
            if (_audio != null)
            {
                AudioClip clip = questSteps[currentQuestStepIndex].GetComponent<QuestStep>().OnStartNarration;
                if (clip != null)
                {
                    _audio.clip = clip;
                    _audio.Play(); //Step�� ������ �� �����̼� ���
                }
            }
            questSteps[currentQuestStepIndex].GetComponent<QuestStep>().OnStepStart.Invoke();
            //manager.ui.setTextColor(currentQuestStepIndex, Color.yellow);
        }
        else //���� ������ ���� ���
        {
            //Debug.Log("�������̸� ���� ����Ʈ ��ȣ : " + currentQuestStepIndex);
            EventsManager.instance.questEvents.FinishQuest(this); //����Ʈ �Ϸ� �̺�Ʈ ȣ��
        }
    }

    public bool CurrentStepExists() //���� ������ �����ϴ��� �Ǵ�
    {
        return (currentQuestStepIndex < questSteps.Length);
    }

    // ����Ƽ ������Ʈ�� �̸��� id�� �׻� ��ġ�ϵ��� ����
    private void OnValidate()
    {
#if UNITY_EDITOR
        id = this.name;
        UnityEditor.EditorUtility.SetDirty(this);
#endif
    }
}

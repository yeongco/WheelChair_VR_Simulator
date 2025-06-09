using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class EventsManager : MonoBehaviour
{
    public static EventsManager instance { get; private set; }
    public GameObject player { get; private set; }
    public QuestEvents questEvents; //����Ʈ ���� �̺�Ʈ
    public PlayerEvents playerEvents; //�÷��̾� �̵� ���� �̺�Ʈ
    public GameEvents gameEvents; //�ڵ� ȸ��, ���� �̵� �� ���� �̺�Ʈ
    public InputEvents inputEvents; //��Ʈ�ѷ� ��ư �Է� �̺�Ʈ

    public static int SceneNumber = 0;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else
        {
            Destroy(gameObject);
        }
        //SceneManager.sceneLoaded += OnSceneLoaded;
        player = GameObject.FindWithTag("Player");
        // initialize all events
        questEvents = new QuestEvents();
        playerEvents = new PlayerEvents();
        gameEvents = new GameEvents();
        inputEvents = new InputEvents();
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        player = GameObject.FindWithTag("Player");
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    /*private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // �� �ε� �Ϸ� �� ������ ���
        Vector3 playerPos = new Vector3(0f, 0f, 0f);

        switch (SceneNumber)
        {
            case 2: playerPos = new Vector3(4f, -1.8f, 0f); break; //outside
            case 3: playerPos = new Vector3(-7.5f, 1.8f, 0f); break; //nav
        }
        
        //player.transform.position = playerPos;

        SceneManager.sceneLoaded -= OnSceneLoaded;
    }*/
}

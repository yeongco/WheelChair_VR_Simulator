using System.Collections;
using System.Collections.Generic;
using Meta.XR.MRUtilityKit.SceneDecorator;
using Unity.Mathematics;
using UnityEngine;

public class TutoTurnStep : QuestStep
{
    //처음 회전 값에서
    //어느정도 회전을 하면 퀘스트가 클리어되게 만들고 싶다

    [SerializeField]
    private float yRotate;  //업데이트될 y회전값

    [SerializeField]
    private float nowRotateValue = 0; //현재 회전량

    [SerializeField]
    private float targetRotateValue = 300; //목표 회전량

    public GameObject player;  //휠체어 오브젝트 (플레이어취급)

    private void Start()
    {
        if(player == null)
            player = EventsManager.instance.player;
        yRotate = player.transform.localEulerAngles.y;
    }
    private void Update()
    {
        if(yRotate!=player.transform.localEulerAngles.y)
        {
            Debug.Log("회전 인식됨");
            UpdateRotate();
        }
    }

    private void UpdateRotate()
    {
        float currentY = player.transform.localEulerAngles.y;
        float deltaY = Mathf.DeltaAngle(yRotate, currentY);
        yRotate = currentY;
        nowRotateValue += Mathf.Abs(deltaY);

        if(nowRotateValue >= targetRotateValue)
            FinishQuestStep();
    }


}
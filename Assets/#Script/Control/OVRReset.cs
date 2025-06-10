using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OVRReset : MonoBehaviour
{
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private Transform rigRoot;

        void Awake()
    {
        // 실행 시 반드시 FloorLevel(바닥 기준) 트래킹으로 설정
        OVRManager.instance.trackingOriginType = OVRManager.TrackingOrigin.FloorLevel;
    }

    void OnEnable()
    {
        EventsManager.instance.inputEvents.onButtonPressed += Reset;
    }

    void Start()
    {
        // 앱 시작 시 위치·회전 동기화
        OVRManager.display.RecenterPose();
        rigRoot.position = spawnPoint.position;
        rigRoot.rotation = spawnPoint.rotation;
        
    }

    void Reset(ControllerButton button)
    {
        if(button == ControllerButton.A)
            OVRManager.display.RecenterPose();   
    }
}
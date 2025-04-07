using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class VRSelector : MonoBehaviour
{
    public Transform rayOrigin;

    private RaycastHit hit;
    public float selectDistance = 0.8f;
    public LayerMask selectableLayerMask; // 레이어 마스크 추가

    private GameObject selectedObject;

    bool isPressed;

    private void OnEnable()
    {
        isPressed = false;
        EventsManager.instance.inputEvents.onButtonPressed += OnButtonPressed;
    }

    private void OnDisable()
    {
        EventsManager.instance.inputEvents.onButtonPressed -= OnButtonPressed;
    }

    // Update is called once per frame
    void Update()
    {
        // 레이캐스트
        Ray ray = new Ray(rayOrigin.position, rayOrigin.forward);
        if (Physics.Raycast(ray, out hit, selectDistance, selectableLayerMask)) // 충돌한 물체가 있을 경우 Focus In
        {
            Debug.DrawRay(ray.origin, ray.direction * hit.distance, Color.red);

            // 새로운 물체가 선택되었을 경우에만 Focus 상태 변경, 두 개 이상의 물체 중복 처리 방지
            if (hit.transform.gameObject != selectedObject)
            {
                // 이전 선택된 물체가 있을 경우 Focus Out 처리
                if (selectedObject != null)
                {
                    selectedObject.GetComponent<SelectableObject>().OnFocusedOut();
                }

                // 새로운 물체 선택
                selectedObject = hit.transform.gameObject;
                selectedObject.GetComponent<SelectableObject>().OnFocusedIn();
            }
        }
        else //충돌한 물체가 없는 경우 Focus Out
        {
            if (selectedObject != null)
            {
                selectedObject.GetComponent<SelectableObject>().OnFocusedOut();
                selectedObject = null;
            }
        }

        if (isPressed) //컨트롤러 A버튼 클릭
        {
            if (selectedObject != null) {
                selectedObject.GetComponent<SelectableObject>().OnSelected();
                this.enabled = false;
            }            
        }

    }


    void OnButtonPressed(ControllerButton button)
    {
        if(button == ControllerButton.PRIMARY)
        {
            isPressed = true;
            Update();
            isPressed = false;
        }
    }

}

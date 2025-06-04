using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class VRSelector : MonoBehaviour
{
    public Transform rayOrigin;

    private RaycastHit hit;
    public float selectDistance = 0.8f;
    public LayerMask selectableLayerMask; // ���̾� ����ũ �߰�

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
        // ����ĳ��Ʈ
        Ray ray = new Ray(rayOrigin.position, rayOrigin.forward);
        if (Physics.Raycast(ray, out hit, selectDistance, selectableLayerMask)) // �浹�� ��ü�� ���� ��� Focus In
        {
            Debug.DrawRay(ray.origin, ray.direction * hit.distance, Color.red);

            // ���ο� ��ü�� ���õǾ��� ��쿡�� Focus ���� ����, �� �� �̻��� ��ü �ߺ� ó�� ����
            if (hit.transform.gameObject != selectedObject)
            {
                // ���� ���õ� ��ü�� ���� ��� Focus Out ó��
                if (selectedObject != null)
                {
                    selectedObject.GetComponent<SelectableObject>().OnFocusedOut();
                }

                // ���ο� ��ü ����
                selectedObject = hit.transform.gameObject;
                selectedObject.GetComponent<SelectableObject>().OnFocusedIn();
            }
        }
        else //�浹�� ��ü�� ���� ��� Focus Out
        {
            if (selectedObject != null)
            {
                selectedObject.GetComponent<SelectableObject>().OnFocusedOut();
                selectedObject = null;
            }
        }

        if (isPressed) //��Ʈ�ѷ� A��ư Ŭ��
        {
            if (selectedObject != null) {
                selectedObject.GetComponent<SelectableObject>().OnSelected();
                this.enabled = false;
            }            
        }

    }


    void OnButtonPressed(ControllerButton button)
    {
        if(button == ControllerButton.A)
        {
            isPressed = true;
            Update();
            isPressed = false;
        }
    }

}

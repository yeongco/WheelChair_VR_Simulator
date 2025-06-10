using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static OVRInput;

public class OVRInputManager : MonoBehaviour
{
    void Update()
    {
        //내림
        // 왼손 X 버튼 눌림 (물리 버튼 X)
        if (OVRInput.GetDown(Button.One, Controller.LTouch))
        {
            EventsManager.instance.inputEvents.ButtonPressed(ControllerButton.X);
        }
        // 왼손 Y 버튼 눌림 (물리 버튼 Y)
        if (OVRInput.GetDown(Button.Two, Controller.LTouch))
        {
            EventsManager.instance.inputEvents.ButtonPressed(ControllerButton.Y);
        }
        // 오른손 A 버튼 눌림 (물리 버튼 A)
        if (OVRInput.GetDown(Button.One, Controller.RTouch))
        {
            EventsManager.instance.inputEvents.ButtonPressed(ControllerButton.A);
        }
        // 오른손 B 버튼 눌림 (물리 버튼 B)
        if (OVRInput.GetDown(Button.Two, Controller.RTouch))
        {
            EventsManager.instance.inputEvents.ButtonPressed(ControllerButton.B);
        }

        //올림

        // 왼손 X 버튼 눌림 (물리 버튼 X)
        if (OVRInput.GetUp(Button.One, Controller.LTouch))
        {
            EventsManager.instance.inputEvents.ButtonReleased(ControllerButton.A);
        }
        // 왼손 Y 버튼 눌림 (물리 버튼 Y)
        if (OVRInput.GetDown(Button.Two, Controller.LTouch))
        {
            EventsManager.instance.inputEvents.ButtonReleased(ControllerButton.A);
        }
        // 오른손 A 버튼 눌림 (물리 버튼 A)
        if (OVRInput.GetDown(Button.One, Controller.RTouch))
        {
            EventsManager.instance.inputEvents.ButtonReleased(ControllerButton.A);
        }
        // 오른손 B 버튼 눌림 (물리 버튼 B)
        if (OVRInput.GetDown(Button.Two, Controller.RTouch))
        {
            EventsManager.instance.inputEvents.ButtonReleased(ControllerButton.A);
        }
    }
}

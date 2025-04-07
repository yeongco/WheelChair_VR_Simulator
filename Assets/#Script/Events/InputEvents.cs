using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InputEvents
{
    public event Action<ControllerButton> onButtonPressed; //��Ʈ�ѷ� ��ư ������ �̺�Ʈ
    public void ButtonPressed(ControllerButton button)
    {
        if (onButtonPressed != null)
        {
            onButtonPressed(button);
        }
    }

    public event Action<ControllerButton> onButtonReleased; //��Ʈ�ѷ� ��ư �� �� �̺�Ʈ
    public void ButtonReleased(ControllerButton button)
    {
        if (onButtonReleased != null)
        {
            onButtonReleased(button);
        }
    }

}


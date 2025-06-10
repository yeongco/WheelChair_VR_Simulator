using Autohand.Demo;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

public class GameEvents
{
    public event Action<bool> onBittonClicked;
    public void ButtonClicked(bool clicked)
    {
        if (onBittonClicked != null)
        {
            onBittonClicked(clicked);
        }
    }

    public event Action<bool> onOpenDoor;
    public void OpenDoor(bool clicked)
    {
        if (onOpenDoor != null)
        {
            onOpenDoor(clicked);
        }
    }
}

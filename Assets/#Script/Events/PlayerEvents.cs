using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerEvents
{
    public event Action<float> onPlayerRotated; //플레이어 회전 이벤트
    public void PlayerRotated(float amout)
    {
        if (onPlayerRotated != null)
        {
            onPlayerRotated(amout);
        }
    }
    public event Action<float> onPlayerMoved; //플레이어 이동 이벤트
    public void PlayerMoved(float amout)
    {
        if (onPlayerMoved != null)
        {
            onPlayerMoved(amout);
        }
    }
}

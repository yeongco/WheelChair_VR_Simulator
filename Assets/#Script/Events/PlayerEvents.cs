using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerEvents
{
    public event Action<float> onPlayerRotated; //�÷��̾� ȸ�� �̺�Ʈ
    public void PlayerRotated(float amout)
    {
        if (onPlayerRotated != null)
        {
            onPlayerRotated(amout);
        }
    }
    public event Action<float> onPlayerMoved; //�÷��̾� �̵� �̺�Ʈ
    public void PlayerMoved(float amout)
    {
        if (onPlayerMoved != null)
        {
            onPlayerMoved(amout);
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HandInteractionStep : QuestStep
{
    public GameObject target;

    public void OnEnable()
    {
        EventsManager.instance.gameEvents.onBittonClicked += ButtonClicked;
    }

    public void OnDisable()
    {
        EventsManager.instance.gameEvents.onBittonClicked -= ButtonClicked;
    }

    public void ButtonClicked(bool isClicked)
    {
       FinishQuestStep();
    }
}

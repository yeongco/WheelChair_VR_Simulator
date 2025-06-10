using System.Collections;
using System.Collections.Generic;
using Obi.Samples;
using UnityEngine;

public class BeltStep : QuestStep
{
    [SerializeField]
    TangledPegSlot slot;

    void Update()
    {
        if(slot.HasPegAttached)
            FinishQuestStep();
    }
}

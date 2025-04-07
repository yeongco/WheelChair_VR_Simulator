using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExitStep : QuestStep
{
    private void OnEnable()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}

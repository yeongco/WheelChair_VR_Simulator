using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface SelectableObject
{
    public void OnFocusedIn(); 
    public void OnFocusedOut();
    public void OnSelected();
}

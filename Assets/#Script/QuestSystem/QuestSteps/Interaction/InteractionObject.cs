using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class InteractionObject : MonoBehaviour
{
    public AudioClip interactSound;
    private AudioSource AudioSource;
    bool _isInteract = false;


    private void Awake()
    {
        AudioSource = GetComponent<AudioSource>();
    }
    public void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Hand") && !_isInteract)
        {
            EventsManager.instance.gameEvents.ButtonClicked(true);
            AudioSource.PlayOneShot(interactSound);
            _isInteract = true;
            //Debug.Log("clicked");
        }
        
    }

    public void OnTriggerExit(Collider other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Hand"))
        {
            EventsManager.instance.gameEvents.ButtonClicked(false);
        }
    }
}

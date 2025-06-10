using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class DisableEnable : MonoBehaviour
{
    [SerializeField]
    private GameObject portal;

    private void OnTriggerEnter(Collider other)
    {
        if (other == null || EventsManager.instance == null || portal == null)
        {
            Debug.LogWarning("필요한 컴포넌트가 없습니다.");
            return;
        }

        if (other.gameObject == EventsManager.instance.player)
        {
            portal.SetActive(true);
            gameObject.SetActive(false);
        }
    }
}

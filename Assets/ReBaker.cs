using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.AI.Navigation;

public class ReBaker : MonoBehaviour
{
    private NavMeshSurface cubeAreaSurface;

    private void Awake()
    {
        cubeAreaSurface = GetComponent<NavMeshSurface>();
    }
    public void BakeElevatorPassage()
    {
        cubeAreaSurface.BuildNavMesh();
    }
}

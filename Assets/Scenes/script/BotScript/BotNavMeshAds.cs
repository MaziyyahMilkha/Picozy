using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class BotNavMeshAds : MonoBehaviour
{
    [SerializeField] private Transform endPosition;

    public Transform EndPosition { get => endPosition; set => endPosition = value; }

        private void UpdateBotDestionation(int id)
    {
        if (id == 2)
        {
            NavMeshAgent agent = GetComponent<NavMeshAgent>();
            Debug.Log("�������������� ����");
            agent.isStopped = false;
            agent.ResetPath();
            agent.SetDestination(endPosition.position);
        }
    }

}

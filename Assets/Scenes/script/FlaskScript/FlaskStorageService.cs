using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

public class FlaskStorageService : MonoBehaviour
{
    private Bot[] _bots;

    private void OnEnable()
    {
        GlobalEvents.OnBotsInitialized.AddListener(SaveInitializedFlasks);
    }
    private void OnDisable()
    {
        GlobalEvents.OnBotsInitialized.RemoveListener(SaveInitializedFlasks);
    }

    private void SaveInitializedFlasks(Bot[] bots, bool restart)
    {
        _bots = bots;
    }

    public void RestartFlask()
    {
        foreach (var bot in _bots)
        {
            bool isNeedToMoveBot = !bot.SpawnedBot.GetComponentsInParent<Transform>()[1].Equals(bot.ParentPosition);
            bot.SpawnedBot.transform.SetParent(bot.ParentPosition);

            var sunny = bot.SpawnedBot.GetComponent<Sunny>();
            var animator = bot.SpawnedBot.GetComponent<Animator>();
            
            if (sunny != null)
            {
               sunny.MoveTo(bot.ParentPosition.position);
            }
            else
            {
               // Fallback
               bot.SpawnedBot.transform.position = bot.ParentPosition.position;
            }

            if (!animator.GetBool("IsRunning") && isNeedToMoveBot)
                animator.SetBool("IsRunning", true);
        }

        var flasks = GameObject.FindGameObjectsWithTag("Flask");
        foreach (var flask in flasks)
        {
            flask.GetComponent<FlaskController>().InitializeComponent(null,true);
        }

        GetComponent<FinishGameHandler>().CurrentFilledFlaskCount = 0;

        GetComponent<ReverseActionSystem>().ResetReverseActionCount();
    }
}

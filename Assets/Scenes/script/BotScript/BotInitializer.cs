using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BotInitializer : MonoBehaviour
{
    [SerializeField] private GameObject botPrefab;

    private SunnyType[] botGeneratedColors;
    private Bot[] bots;

    [SerializeField] private Material[] colors; // Direct assignment instead of GetComponent<ColorConstants>

    private int emptyFlask1;
    private int emptyFlask2;
    private int? emptyFlask3;

    public void Initialize(GameObject[] flasks)
    {
        // Removed dependency on missing ColorConstants script
        // var colorsConstant = GetComponent<ColorConstants>();
        // colors = colorsConstant.BotsMaterial;
        
        // Ensure we have enough materials for the types
        if (colors == null || colors.Length == 0)
        {
            Debug.LogError("No materials assigned to BotInitializer!");
            return;
        }

        bots = new Bot[(flasks.Length - FlaskInitializer.emptyFlaskCount) * 4];

        for (int i = 0; i < bots.Length; i++)
        {
            bots[i] = new Bot();
        }

        // Logic for empty flasks
        if (FlaskInitializer.emptyFlaskCount == 2)
        {
            System.Random rnd = new System.Random();
            emptyFlask1 = rnd.Next(0, flasks.Length);
            do
            {
                emptyFlask2 = rnd.Next(0, flasks.Length);
            }
            while (emptyFlask1 == emptyFlask2);
        }
        else
        {
            System.Random rnd = new System.Random();
            emptyFlask1 = rnd.Next(0, flasks.Length);
            do
            {
                emptyFlask2 = rnd.Next(0, flasks.Length);
                emptyFlask3 = rnd.Next(0, flasks.Length);
            }
            while ((emptyFlask1 == emptyFlask2) || (emptyFlask1 == emptyFlask3) || (emptyFlask2 == emptyFlask3));
        }

        GenerateRandomColors(flasks.Length - FlaskInitializer.emptyFlaskCount);
        int flaskCount = 0;
        for (int i = 0; i < flasks.Length; i++)
        {
            if (i == emptyFlask1 || i == emptyFlask2 || i == emptyFlask3)
                continue;
            IntstantiateBots(flasks[i], flaskCount);
            flaskCount++;
        }
        SetBotColor();
        // GlobalEvents removed/mocked
        // GlobalEvents.SendBotsInitialized(bots); 
    }

    private void IntstantiateBots(GameObject flask, int flaskCount)
    {
        var spawnPositions = flask.GetComponentsInChildren<Transform>();

        // Start from 1 because 0 is parent
        for (int i = 1; i < spawnPositions.Length && i <= 4; i++)
        {
            int botIndex = i - 1 + (flaskCount * 4);
            if (botIndex < bots.Length)
            {
                bots[botIndex].SpawnedBot = Instantiate(botPrefab, spawnPositions[i].position, Quaternion.Euler(0f, 180f, 0f), spawnPositions[i]);
                bots[botIndex].ParentPosition = spawnPositions[i];
                
                // Ensure Sunny component exists
                if (bots[botIndex].SpawnedBot.GetComponent<Sunny>() == null)
                {
                    bots[botIndex].SpawnedBot.AddComponent<Sunny>();
                }
                
                // Link Sunny to Flask if needed, though they start in a flask? 
                // Currently FlaskController handles assignment when added, but initial bots are just visual until moved?
                // Actually, we might need to initialize the Sunny's type here if it's not set.
            }
        }
    }

    private void GenerateRandomColors(int flaskCount)
    {
        RandomBotColorGenerator colorGenerator = new RandomBotColorGenerator(flaskCount);
        botGeneratedColors = colorGenerator.GenerateRandomColor();

        for (int i = 0; i < botGeneratedColors.Length; i++)
        {
            bots[i].BotColor = botGeneratedColors[i];
        }
    }

    private void SetBotColor()
    {
        foreach (var bot in bots)
        {
            if (bot.SpawnedBot != null)
            {
                var meshRenderer = bot.SpawnedBot.GetComponentInChildren<SkinnedMeshRenderer>();
                if (meshRenderer != null)
                {
                    meshRenderer.material = null;
                    meshRenderer.sharedMaterial = SelectColorConstant(bot.BotColor);
                }
            }
        }
    }

    private Material SelectColorConstant(SunnyType color)
    {
        // Simple mapping based on SunnyType enum
        int index = (int)color;
        if (index >= 0 && index < colors.Length)
        {
            return colors[index];
        }
        return colors[0]; // Fallback
    }
}

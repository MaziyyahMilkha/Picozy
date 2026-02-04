using UnityEngine;

public class Bot
{
    private GameObject _spawnedBot;
    private SunnyType _botColor;
    private Transform parentTransform;

    public Bot()
    {
    }
    public Bot(GameObject spawnedBot, SunnyType botColor)
    {
        _spawnedBot = spawnedBot;
        _botColor = botColor;
    }

    public SunnyType BotColor { get => _botColor; set => _botColor = value; }
    public GameObject SpawnedBot { get => _spawnedBot; set => _spawnedBot = value; }
    public Transform ParentPosition { get => parentTransform; set => parentTransform = value; }
}

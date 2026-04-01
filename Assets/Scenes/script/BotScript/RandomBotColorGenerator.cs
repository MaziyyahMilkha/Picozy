using System;
using System.Collections.Generic;
using UnityEngine;

public class RandomBotColorGenerator
{
    private int _botsCount;

    private SunnyType[] _botColors;

    public RandomBotColorGenerator(int flaskCount)
    {
        _botsCount = flaskCount * 4;
        _botColors = new SunnyType[_botsCount];
    }

    public SunnyType[] GenerateRandomColor()
    {

        for (int i = 0; i < _botsCount; i++)
        {
            _botColors[i] = SelectCurrentColor(i);
        }
        ShuffleArray();
        return _botColors;
    }

    private SunnyType SelectCurrentColor(int indexElement)
    {
        int colorIndex = indexElement / 4;
        // Check Enum bounds
        if (System.Enum.IsDefined(typeof(SunnyType), colorIndex))
        {
            return (SunnyType)colorIndex;
        }
        return SunnyType.Red; // Default
    }

    private void ShuffleArray()
    {
        System.Random rnd = new System.Random();
        for (int i = 0; i < _botColors.Length - 2; i++)
        {
            int newIndex = i + rnd.Next(_botColors.Length - i);
            SwapElements(i, newIndex);
        }
    }

    private void SwapElements(int oldIndex, int newIndex)
    {
        var temp = _botColors[oldIndex];
        _botColors[oldIndex] = _botColors[newIndex];
        _botColors[newIndex] = temp;
    }

}

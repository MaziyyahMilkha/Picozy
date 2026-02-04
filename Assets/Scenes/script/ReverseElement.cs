using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ReverseElement
{
    public List<GameObject> Bots;
    public FlaskController PreviousFlask;

    public ReverseElement(List<GameObject> bots, FlaskController previousFlask)
    {
        Bots = bots;
        PreviousFlask = previousFlask;
    }
}

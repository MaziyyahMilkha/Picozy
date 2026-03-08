using System;
using System.Collections.Generic;

public static class SortEventManager
{
    private static readonly Dictionary<Type, List<Delegate>> _subscribers = new Dictionary<Type, List<Delegate>>();

    public static void Subscribe<T>(Action<T> handler)
    {
        if (handler == null) return;
        var t = typeof(T);
        if (!_subscribers.TryGetValue(t, out var list))
        {
            list = new List<Delegate>();
            _subscribers[t] = list;
        }
        if (!list.Contains(handler))
            list.Add(handler);
    }

    public static void Unsubscribe<T>(Action<T> handler)
    {
        if (handler == null) return;
        var t = typeof(T);
        if (_subscribers.TryGetValue(t, out var list))
            list.Remove(handler);
    }

    public static void Publish<T>(T data)
    {
        var t = typeof(T);
        if (!_subscribers.TryGetValue(t, out var list)) return;
        var handlers = (List<Delegate>)list;
        for (int i = handlers.Count - 1; i >= 0; i--)
        {
            try { ((Action<T>)handlers[i]).Invoke(data); } catch (Exception ex) { UnityEngine.Debug.LogException(ex); }
        }
    }
}

public struct WinEvent { }
public struct LoseEvent { }

public struct PlayAudioEvent
{
    public string id;
    public SortAudioPlayMode mode;
}

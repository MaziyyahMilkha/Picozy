using System;
using System.Collections.Generic;

public struct UIActionEvent
{
    public string ActionId;
    public string Data;
    public UIActionEvent(string actionId, string data = null)
    {
        ActionId = actionId;
        Data = data;
    }
}

public static class SortEventManager
{
    private static readonly Dictionary<string, List<Action>> _handlers = new Dictionary<string, List<Action>>(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, List<Action<string>>> _handlersWithData = new Dictionary<string, List<Action<string>>>(StringComparer.OrdinalIgnoreCase);
    private static readonly object _lock = new object();

    public static void SubscribeAction(string actionId, Action handler)
    {
        if (string.IsNullOrEmpty(actionId) || handler == null) return;
        lock (_lock)
        {
            if (!_handlers.TryGetValue(actionId, out var list))
            {
                list = new List<Action>();
                _handlers[actionId] = list;
            }
            if (!list.Contains(handler))
                list.Add(handler);
        }
    }

    public static void UnsubscribeAction(string actionId, Action handler)
    {
        if (string.IsNullOrEmpty(actionId) || handler == null) return;
        lock (_lock)
        {
            if (_handlers.TryGetValue(actionId, out var list))
                list.Remove(handler);
        }
    }

    public static void SubscribeAction(string actionId, Action<string> handler)
    {
        if (string.IsNullOrEmpty(actionId) || handler == null) return;
        lock (_lock)
        {
            if (!_handlersWithData.TryGetValue(actionId, out var list))
            {
                list = new List<Action<string>>();
                _handlersWithData[actionId] = list;
            }
            if (!list.Contains(handler))
                list.Add(handler);
        }
    }

    public static void UnsubscribeAction(string actionId, Action<string> handler)
    {
        if (string.IsNullOrEmpty(actionId) || handler == null) return;
        lock (_lock)
        {
            if (_handlersWithData.TryGetValue(actionId, out var list))
                list.Remove(handler);
        }
    }

    public static void Publish(UIActionEvent e)
    {
        if (string.IsNullOrEmpty(e.ActionId)) return;
        List<Action> copy;
        List<Action<string>> copyWithData;
        lock (_lock)
        {
            _handlers.TryGetValue(e.ActionId, out var list);
            copy = list != null && list.Count > 0 ? new List<Action>(list) : null;
            _handlersWithData.TryGetValue(e.ActionId, out var listData);
            copyWithData = listData != null && listData.Count > 0 ? new List<Action<string>>(listData) : null;
        }
        if (copy != null)
        {
            foreach (var a in copy)
            {
                try { a?.Invoke(); }
                catch (Exception ex) { UnityEngine.Debug.LogException(ex); }
            }
        }
        if (copyWithData != null && !string.IsNullOrEmpty(e.Data))
        {
            foreach (var a in copyWithData)
            {
                try { a?.Invoke(e.Data); }
                catch (Exception ex) { UnityEngine.Debug.LogException(ex); }
            }
        }
    }

    public static void ClearAll()
    {
        lock (_lock)
        {
            _handlers.Clear();
            _handlersWithData.Clear();
        }
    }
}

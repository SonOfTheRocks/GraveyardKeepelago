using System;
using System.Collections.Generic;

namespace GraveyardKeepelago.Items.Notifications;

// Game-thread feed. Time advances only while the player can actually see the notifications.
public sealed class ItemNotificationQueue
{
    public const int Capacity = 4;
    public const float Interval = 1.2f;
    public const float ReadableDuration = 10f;
    public const float FadeInDuration = 0.2f;
    public const float Lifetime = FadeInDuration + ReadableDuration + FadeDuration;
    public const float FadeDuration = 0.8f;
    private readonly Queue<ItemNotification> _pending = new();
    private readonly List<VisibleReceipt> _visible = new();
    private float _untilNext;
    public int PendingCount => _pending.Count;
    public IReadOnlyList<VisibleReceipt> Visible => _visible;

    public sealed class VisibleReceipt
    {
        public ItemNotification Notification { get; }
        public float Age { get; internal set; }
        public float Opacity => Math.Min(Math.Min(1f, Age / FadeInDuration), Math.Max(0f, (Lifetime - Age) / FadeDuration));
        internal VisibleReceipt(ItemNotification notification) => Notification = notification;
    }

    public void Enqueue(ItemNotification notification) => _pending.Enqueue(notification);

    public void Update(float elapsed, bool canShow)
    {
        if (!canShow) return;
        // A stalled frame must not consume a whole batch or expire messages nobody saw.
        elapsed = Math.Max(0, Math.Min(elapsed, 0.1f));
        foreach (var receipt in _visible) receipt.Age += elapsed;
        _visible.RemoveAll(receipt => receipt.Age >= Lifetime);
        _untilNext = Math.Max(0, _untilNext - elapsed);
        if (_untilNext > 0 || _pending.Count == 0 || _visible.Count >= Capacity) return;
        _visible.Add(new VisibleReceipt(_pending.Dequeue()));
        _untilNext = Interval;
    }

    public void Clear()
    {
        _pending.Clear();
        _visible.Clear();
        _untilNext = 0;
    }
}



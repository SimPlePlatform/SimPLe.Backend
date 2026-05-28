using SimPle.Domain.Common;

namespace SimPle.Domain.Notifications;

public class Notification : Entity
{
    public Guid RecipientId { get; private set; }
    public Guid? SenderId { get; private set; }
    public NotificationKind Kind { get; private set; }
    public string Title { get; private set; } = default!;
    public string? Body { get; private set; }
    public string? ActionUrl { get; private set; }
    public bool IsRead { get; private set; }

    private Notification() { }

    public static Notification Create(Guid recipientId, NotificationKind kind, string title, string? body = null, Guid? senderId = null) => new()
    {
        RecipientId = recipientId,
        SenderId = senderId,
        Kind = kind,
        Title = title,
        Body = body,
    };

    public void MarkRead() { IsRead = true; Touch(); }
}

public enum NotificationKind { FriendRequest, LobbyInvite, MatchResult, Achievement, SystemMessage, SeasonUpdate }

using SimPle.Domain.Common;

namespace SimPle.Domain.Chat;

public class ChatMessage : Entity
{
    public Guid SenderId { get; private set; }
    public ChatContext Context { get; private set; }
    public Guid ContextId { get; private set; }  // LobbyId or SessionId
    public string Content { get; private set; } = default!;
    public bool IsDeleted { get; private set; }
    public bool IsFlagged { get; private set; }

    private ChatMessage() { }

    public static ChatMessage Create(Guid senderId, ChatContext context, Guid contextId, string content) => new()
    {
        SenderId = senderId,
        Context = context,
        ContextId = contextId,
        Content = content,
    };

    public void Delete() { IsDeleted = true; Touch(); }
    public void Flag() { IsFlagged = true; Touch(); }
}

public enum ChatContext { Lobby, Match, DirectMessage }

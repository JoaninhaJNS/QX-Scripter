namespace Qx;

/// <summary>Specifies the direction of a message.</summary>
[Flags]
public enum Direction
{
    /// <summary>No direction.</summary>
    None = 0,
    /// <summary>Incoming, from the server to the client.</summary>
    In = 1 << 0,
    /// <summary>Outgoing, from the client to the server.</summary>
    Out = 1 << 1,
    /// <summary>Both incoming and outgoing.</summary>
    Both = In | Out
}

namespace Qx.Messages;

/// <summary>Defines the session context that parsers and composers use to adapt to the connected client build.</summary>
public interface IParserContext
{
    /// <summary>Gets the message manager of the session.</summary>
    IMessageManager Messages { get; }
    /// <summary>Gets the wire profile of the connected client build.</summary>
    MessageWireProfile WireProfile { get; }
}

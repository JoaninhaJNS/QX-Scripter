namespace Qx.Messages;

/// <summary>Defines a type that can be read from and written to a packet.</summary>
/// <typeparam name="T">The type that is read and written.</typeparam>
public interface IParserComposer<T> : IComposer, IParser<T> where T : IParser<T>;

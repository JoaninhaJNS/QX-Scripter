namespace Qx.Protocol;

/// <summary>Specifies the wire type of an outgoing message parameter, as declared by the client.</summary>
public enum OutgoingWireType
{
    /// <summary>The type is not known.</summary>
    Unknown,
    /// <summary>A boolean written as one byte, 0 or 1.</summary>
    Boolean,
    /// <summary>A signed 8-bit integer written as one byte.</summary>
    Int8,
    /// <summary>An unsigned 8-bit integer written as one byte.</summary>
    UInt8,
    /// <summary>A signed 16-bit big-endian integer.</summary>
    Int16,
    /// <summary>An unsigned 16-bit big-endian integer.</summary>
    UInt16,
    /// <summary>A signed 32-bit big-endian integer.</summary>
    Int32,
    /// <summary>An unsigned 32-bit big-endian integer.</summary>
    UInt32,
    /// <summary>A signed 64-bit big-endian integer.</summary>
    Int64,
    /// <summary>An unsigned 64-bit big-endian integer.</summary>
    UInt64,
    /// <summary>A 32-bit float, which Flash writes as a string.</summary>
    Float32,
    /// <summary>A 64-bit big-endian IEEE 754 double.</summary>
    Float64,
    /// <summary>A decimal number, which schema matching does not support.</summary>
    Decimal,
    /// <summary>A single character written as a length-prefixed string.</summary>
    Character,
    /// <summary>A UTF-8 string prefixed by its 16-bit byte length.</summary>
    String
}

/// <summary>Specifies whether an outgoing message parameter is a single value or a collection.</summary>
public enum OutgoingCollectionKind
{
    /// <summary>A single value.</summary>
    None,
    /// <summary>A list, written as a length followed by its elements.</summary>
    List,
    /// <summary>An array, written as a length followed by its elements.</summary>
    Array
}

/// <summary>Represents one parameter of an outgoing message schema.</summary>
/// <param name="Position">The zero-based position of the parameter.</param>
/// <param name="SourceType">The parameter type as declared in the client source.</param>
/// <param name="Name">The parameter name in the client source.</param>
/// <param name="DefaultValue">The default value in the client source, or <see langword="null"/> when there is none.</param>
/// <param name="WireType">The wire type of the parameter, or of its elements for a collection.</param>
/// <param name="Collection">Whether the parameter is a single value or a collection.</param>
/// <param name="ElementWireTypes">The wire types of the fields of each collection element, or <see langword="null"/> when each element is one value of <paramref name="WireType"/>.</param>
public sealed record OutgoingParameterSchema(
    int Position,
    string SourceType,
    string Name,
    string? DefaultValue,
    OutgoingWireType WireType,
    OutgoingCollectionKind Collection,
    IReadOnlyList<OutgoingWireType>? ElementWireTypes = null);

/// <summary>Represents the parameter layout of an outgoing message, as declared by the client.</summary>
/// <param name="SourceName">The name of the message composer in the client source.</param>
/// <param name="Parameters">The parameters in wire order.</param>
public sealed record OutgoingMessageSchema(
    string SourceName,
    IReadOnlyList<OutgoingParameterSchema> Parameters);

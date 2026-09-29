using Qx.Model.Messages.Incoming;

namespace Qx.Game.Application;

/// <param name="UserIndex">Only the values of the avatar with this room index.</param>
/// <param name="FurniId">Only the values of the furni with this item id.</param>
/// <param name="Variable">Only the values of the variable with this id or drawn with this icon.</param>
public sealed record RoomVariableFxStateRequest(int? UserIndex = null, Id? FurniId = null, string? Variable = null);

public sealed record RoomVariableFxState(
    Id? RoomId,
    long RoomGeneration,
    IReadOnlyList<VariableFxConfigEntry> Configs,
    IReadOnlyList<VariableFxValue> Values);

/// <param name="Value">The value as it is now, or as it was when it was removed.</param>
/// <param name="Previous">The value before this change, or <see langword="null"/> when it is new or removed.</param>
/// <param name="Removed">Whether the value was removed.</param>
public sealed record RoomVariableFxChange(VariableFxValue Value, VariableFxValue? Previous, bool Removed);

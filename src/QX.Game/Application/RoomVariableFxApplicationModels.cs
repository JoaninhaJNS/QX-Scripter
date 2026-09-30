using Qx.Model.Messages.Incoming;

namespace Qx.Game.Application;

/// <summary>
/// Represents a request to read the Fx bar of the current room.
/// </summary>
/// <remarks>
/// Used by <see cref="ApplicationMemberIds.RoomVariableFxState"/>. <paramref name="UserIndex"/> and
/// <paramref name="FurniId"/> cannot both be set.
/// </remarks>
/// <param name="UserIndex">
/// Only the values of the avatar with this room index, or <see langword="null"/> to not filter by avatar.
/// </param>
/// <param name="FurniId">Only the values of the furni with this item id, or <see langword="null"/> to not filter by furni.</param>
/// <param name="Variable">
/// Only the values of the variable with this id or drawn with this icon, compared case sensitively, or
/// <see langword="null"/> or empty to not filter by variable.
/// </param>
public sealed record RoomVariableFxStateRequest(int? UserIndex = null, Id? FurniId = null, string? Variable = null);

/// <summary>
/// Represents the Fx bar of the current room.
/// </summary>
/// <remarks>
/// Returned by <see cref="ApplicationMemberIds.RoomVariableFxState"/>.
/// </remarks>
/// <param name="RoomId">The id of the current room, or <see langword="null"/> when no room is loaded.</param>
/// <param name="RoomGeneration">The room state generation the Fx bar was read in.</param>
/// <param name="Configs">Every Fx bar configuration of the room, for avatars and for furni.</param>
/// <param name="Values">The Fx bar values that match the request filters, bound to their current configuration.</param>
public sealed record RoomVariableFxState(
    Id? RoomId,
    long RoomGeneration,
    IReadOnlyList<VariableFxConfigEntry> Configs,
    IReadOnlyList<VariableFxValue> Values);

/// <summary>
/// Represents a change of one Fx bar value in the current room.
/// </summary>
/// <remarks>
/// Published by <see cref="ApplicationMemberIds.RoomVariableFxChanged"/> when a value arrives, when the
/// configuration it is drawn with changes and when it is removed, including when its avatar or furni leaves the room.
/// </remarks>
/// <param name="Value">The value as it is now, or as it was when it was removed.</param>
/// <param name="Previous">The value before this change, or <see langword="null"/> when it is new or removed.</param>
/// <param name="Removed">Whether the value was removed.</param>
public sealed record RoomVariableFxChange(VariableFxValue Value, VariableFxValue? Previous, bool Removed);

using Qx.Game;
using Qx.Model;

namespace Qx.Scripting;

public partial class ScriptGlobals
{
    /// <summary>
    /// Registers a handler that runs on every movement of every avatar, together with what caused it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Movements are each status update while walking or standing, each roller slide and each
    /// wired move.
    /// </para>
    /// <para>
    /// Unlike <see cref="OnAvatarMoved"/> this also fires when a status update leaves the avatar
    /// on its tile, which is how a walk that starts or stops is seen. Read
    /// <see cref="RoomManager.Revision"/> before sending a command and compare it with
    /// <see cref="AvatarMovement.Revision"/> to keep only the movements that came after it.
    /// </para>
    /// </remarks>
    /// <param name="handler">The handler to call with the avatar in its updated state and the movement.</param>
    /// <returns>A handle that removes the handler when disposed; it is also disposed when the script stops.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="handler"/> is <see langword="null"/>.</exception>
    public IDisposable OnAvatarMovement(Action<Avatar, AvatarMovement> handler) =>
        Subscribe(
            handler,
            listener => Room.AvatarMovementReceived += listener,
            listener => Room.AvatarMovementReceived -= listener);

    /// <summary>
    /// Registers a handler that runs on every movement of the local user's own avatar.
    /// </summary>
    /// <remarks>
    /// It receives the same events as <see cref="OnAvatarMovement"/>, filtered by
    /// <see cref="AvatarMovement.IsSelf"/>, and stays correct after the avatar enters a room
    /// again with a new index.
    /// </remarks>
    /// <param name="handler">The handler to call with the movement.</param>
    /// <returns>A handle that removes the handler when disposed; it is also disposed when the script stops.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="handler"/> is <see langword="null"/>.</exception>
    public IDisposable OnSelfMovement(Action<AvatarMovement> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);
        return OnAvatarMovement((_, movement) =>
        {
            if (movement.IsSelf)
                handler(movement);
        });
    }

    /// <summary>
    /// Registers a handler that runs on every movement of every floor item.
    /// </summary>
    /// <remarks>
    /// Movements are roller slides, wired moves and rotations, and moves by someone with rights.
    /// Wired and roller moves are reported even when they leave the item where it was, which is
    /// what a wired cycle can be timed by.
    /// </remarks>
    /// <param name="handler">The handler to call with the item in its updated state and the movement.</param>
    /// <returns>A handle that removes the handler when disposed; it is also disposed when the script stops.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="handler"/> is <see langword="null"/>.</exception>
    public IDisposable OnFloorItemMovement(Action<FloorItem, FloorItemMovement> handler) =>
        Subscribe(
            handler,
            listener => Room.FloorItemMovementReceived += listener,
            listener => Room.FloorItemMovementReceived -= listener);
}

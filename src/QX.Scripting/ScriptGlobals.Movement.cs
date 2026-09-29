using Qx.Game;
using Qx.Model;

namespace Qx.Scripting;

public partial class ScriptGlobals
{
    /// <summary>
    /// Subscribes to every movement of every avatar: each status update while walking or
    /// standing, each roller slide and each wired move, together with what caused it.
    /// </summary>
    /// <remarks>
    /// Unlike <see cref="OnAvatarMoved"/> this also fires when a status update leaves the avatar
    /// on its tile, which is how a walk that starts or stops is seen. Read
    /// <see cref="RoomManager.Revision"/> before sending a command and compare it with
    /// <see cref="AvatarMovement.Revision"/> to keep only the movements that came after it.
    /// </remarks>
    /// <param name="handler">Receives the avatar in its updated state and the movement.</param>
    /// <returns>A handle that unsubscribes when disposed; also disposed when the script stops.</returns>
    public IDisposable OnAvatarMovement(Action<Avatar, AvatarMovement> handler) =>
        Subscribe(
            handler,
            listener => Room.AvatarMovementReceived += listener,
            listener => Room.AvatarMovementReceived -= listener);

    /// <summary>
    /// Subscribes to the movements of the local user's own avatar only. Same events as
    /// <see cref="OnAvatarMovement"/>, already filtered, and still correct after the avatar
    /// re-enters a room with a new index.
    /// </summary>
    /// <returns>A handle that unsubscribes when disposed; also disposed when the script stops.</returns>
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
    /// Subscribes to every movement of every floor item: roller slides, wired moves and
    /// rotations, and moves by someone with rights. Wired and roller moves are reported even when
    /// they leave the item where it was, which is what a wired cycle can be timed by.
    /// </summary>
    /// <param name="handler">Receives the item in its updated state and the movement.</param>
    /// <returns>A handle that unsubscribes when disposed; also disposed when the script stops.</returns>
    public IDisposable OnFloorItemMovement(Action<FloorItem, FloorItemMovement> handler) =>
        Subscribe(
            handler,
            listener => Room.FloorItemMovementReceived += listener,
            listener => Room.FloorItemMovementReceived -= listener);
}

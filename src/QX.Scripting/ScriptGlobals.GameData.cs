using Qx.Game;
using Qx.Model;

namespace Qx.Scripting;

public partial class ScriptGlobals
{
    /// <summary>
    /// Gets the downloaded game data, such as furni definitions, catalog products and hotel texts.
    /// </summary>
    /// <remarks>
    /// Check <see cref="Qx.Game.GameData.IsLoaded"/> before relying on it; each data property is
    /// <see langword="null"/> until it has been loaded for the current hotel.
    /// </remarks>
    public GameData GameData => Game.GameData;

    /// <summary>
    /// Gets the furni definition behind a room item.
    /// </summary>
    /// <remarks>
    /// The definition holds the class identifier, display name, category, stacking and sit and
    /// walk flags. Matching prefers the item's own class identifier and falls back to its type
    /// and kind. Inventory and trade items are not <see cref="Furni"/>; look those up with
    /// <see cref="FurniOf(ItemType, int)"/>.
    /// </remarks>
    /// <param name="item">Any floor or wall item in a room.</param>
    /// <returns>
    /// The definition, or <see langword="null"/> when the furni data has not downloaded or the
    /// item's kind is not in it.
    /// </returns>
    public FurniInfo? FurniOf(Furni item) => Game.GameData.Furni?.GetInfo(item);

    /// <summary>
    /// Gets the furni definition for a type and kind.
    /// </summary>
    /// <remarks>
    /// Use it for items that are not room furni, such as inventory items, trade offers,
    /// marketplace offers and catalog entries.
    /// </remarks>
    /// <param name="type">Whether the kind is a floor or a wall item.</param>
    /// <param name="kind">The numeric kind, which differs between hotels.</param>
    /// <returns>
    /// The definition, or <see langword="null"/> when the furni data has not downloaded or the
    /// kind is not in it.
    /// </returns>
    public FurniInfo? FurniOf(ItemType type, int kind) => Game.GameData.Furni?.GetInfo(type, kind);

    /// <summary>
    /// Gets the display name of a room item, as shown in the client.
    /// </summary>
    /// <remarks>
    /// For inventory and trade items use <see cref="FurniName(ItemType, int)"/>.
    /// </remarks>
    /// <param name="item">Any floor or wall item in a room.</param>
    /// <returns>
    /// The localized name, or <c>"#"</c> followed by the numeric kind when the furni data has not
    /// downloaded, the kind is unknown or the name is empty, so the result is never empty.
    /// </returns>
    public string FurniName(Furni item) =>
        Game.GameData.Furni?.GetInfo(item)?.Name is { Length: > 0 } name ? name : "#" + item.Kind;

    /// <summary>
    /// Gets the display name for a type and kind, as shown in the client.
    /// </summary>
    /// <param name="type">Whether the kind is a floor or a wall item.</param>
    /// <param name="kind">The numeric kind, which differs between hotels.</param>
    /// <returns>
    /// The localized name, or <c>"#"</c> followed by the kind when the furni data has not
    /// downloaded, the kind is unknown or the name is empty, so the result is never empty.
    /// </returns>
    public string FurniName(ItemType type, int kind) =>
        Game.GameData.Furni?.GetInfo(type, kind)?.Name is { Length: > 0 } name ? name : "#" + kind;

    /// <summary>
    /// Gets whether a furni is of the given class, comparing class identifiers and ignoring case.
    /// </summary>
    /// <remarks>
    /// Comparing identifiers recognizes a furni across hotels, since kind numbers differ between
    /// hotels. The comparison uses the full identifier from the furni data, including any
    /// <c>*</c> color suffix.
    /// </remarks>
    /// <param name="item">The furni to test.</param>
    /// <param name="identifier">The class identifier, for example <c>"rare_dragonlamp"</c>.</param>
    /// <returns>
    /// <see langword="true"/> when the identifiers match; otherwise, <see langword="false"/>,
    /// which is also the result when the furni data has not downloaded yet.
    /// </returns>
    public bool IsIdentifier(Furni item, string identifier) =>
        string.Equals(FurniOf(item)?.Identifier, identifier, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets the catalog product definition for a product code.
    /// </summary>
    /// <param name="code">The product code as used by the catalog, matched case-sensitively.</param>
    /// <returns>
    /// The definition, or <see langword="null"/> when the product data has not downloaded or the
    /// code is unknown.
    /// </returns>
    public ProductInfo? ProductOf(string code) => Game.GameData.Products?.GetInfo(code);

    /// <summary>
    /// Gets the display name of a catalog product.
    /// </summary>
    /// <param name="code">The product code as used by the catalog.</param>
    /// <returns>The localized name, or the code itself when it cannot be resolved.</returns>
    public string ProductName(string code) =>
        ProductOf(code)?.Name is { Length: > 0 } name ? name : code;

    /// <summary>
    /// Gets the description text of a catalog product.
    /// </summary>
    /// <param name="code">The product code as used by the catalog.</param>
    /// <returns>The description, or an empty string when it cannot be resolved.</returns>
    public string ProductDescription(string code) =>
        ProductOf(code)?.Description ?? "";

    /// <summary>
    /// Gets the display name of a badge.
    /// </summary>
    /// <param name="code">The badge code, for example <c>"ACH_BasicClub1"</c>.</param>
    /// <returns>The localized name, or the code itself when the texts have not downloaded or have no entry.</returns>
    public string BadgeName(string code) => Game.GameData.Texts?.BadgeName(code) ?? code;

    /// <summary>
    /// Gets the display name of an avatar effect.
    /// </summary>
    /// <param name="id">The effect id, as reported by <see cref="OnAvatarEffectChanged"/>.</param>
    /// <returns>The localized name, or an empty string when it cannot be resolved.</returns>
    public string EffectName(int id) => Game.GameData.Texts?.EffectName(id) ?? "";

    /// <summary>
    /// Gets the display name of a hand item, which is the drink or object an avatar holds.
    /// </summary>
    /// <param name="id">The hand item id, as reported by <see cref="OnAvatarHandItemChanged"/>.</param>
    /// <returns>The localized name, or an empty string when it cannot be resolved.</returns>
    public string HandItemName(int id) => Game.GameData.Texts?.HandItemName(id) ?? "";
}

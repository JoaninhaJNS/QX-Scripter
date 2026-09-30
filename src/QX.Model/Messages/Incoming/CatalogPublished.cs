using Qx.Messages;

namespace Qx.Model.Messages.Incoming;

/// <summary>
/// Represents the <c>CatalogPublished</c> message, received when the hotel publishes a new version of the catalog.
/// </summary>
/// <param name="InstantlyRefreshCatalogue">Whether the client should refresh the catalog right away.</param>
/// <param name="NewFurniDataHash">
/// The hash of the new furni data, or <see langword="null"/> when the message does not carry one.
/// </param>
public sealed record CatalogPublished(bool InstantlyRefreshCatalogue, string? NewFurniDataHash)
    : IParserComposer<CatalogPublished>
{
    /// <summary>Parses the message from a packet.</summary>
    /// <param name="p">The packet reader.</param>
    public static CatalogPublished Parse(in PacketReader p) =>
        FlashWire.Parse(in p, ParseFlash);

    private static CatalogPublished ParseFlash(in PacketReader p)
    {
        bool instantly_refresh_catalogue = p.ReadBool();
        string? new_furni_data_hash = null;
        if (p.Available > 0)
        {
            var strings = new CatalogStringBudget(1, ushort.MaxValue);
            new_furni_data_hash = strings.Read(in p, nameof(NewFurniDataHash));
        }
        CatalogWire.RequireEmpty(in p, nameof(CatalogPublished));
        return new CatalogPublished(instantly_refresh_catalogue, new_furni_data_hash);
    }

    /// <summary>Composes the message into a packet.</summary>
    /// <param name="p">The packet writer.</param>
    public void Compose(in PacketWriter p) =>
        FlashWire.Compose(this, in p, ComposeFlash);

    private static void ComposeFlash(CatalogPublished value, in PacketWriter p)
    {
        if (value.NewFurniDataHash is not null)
            CatalogWire.RequireString(value.NewFurniDataHash, nameof(NewFurniDataHash), in p);
        p.WriteBool(value.InstantlyRefreshCatalogue);
        if (value.NewFurniDataHash is not null)
            p.WriteString(value.NewFurniDataHash);
    }
}

using Qx.Messages;

namespace Qx.Model.Messages.Incoming;

/// <summary>Represents a product included in a catalog offer.</summary>
/// <param name="ProductType">
/// The product type code, for example <see cref="CatalogProduct.TypeStuff"/> or <see cref="CatalogProduct.TypeBadge"/>.
/// </param>
/// <param name="FurniClassId">The furni class identifier of the product, or 0 for a badge.</param>
/// <param name="ExtraParam">The extra parameter of the product, which holds the badge code for a badge.</param>
/// <param name="ProductCount">The number of items the product gives, always 1 for a badge.</param>
/// <param name="UniqueLimitedItem">Whether the product is a limited edition item.</param>
/// <param name="UniqueLimitedItemSeriesSize">
/// The total number of items in the limited edition series, or 0 when the product is not limited.
/// </param>
/// <param name="UniqueLimitedItemsLeft">
/// The number of limited edition items left, or 0 when the product is not limited.
/// </param>
public sealed record CatalogProduct(
    string ProductType,
    int FurniClassId,
    string ExtraParam,
    int ProductCount,
    bool UniqueLimitedItem,
    int UniqueLimitedItemSeriesSize,
    int UniqueLimitedItemsLeft) : IParserComposer<CatalogProduct>
{
    /// <summary>The product type code of a wall item.</summary>
    public const string TypeItem = "i";
    /// <summary>The product type code of a floor item.</summary>
    public const string TypeStuff = "s";
    /// <summary>The product type code of an avatar effect.</summary>
    public const string TypeEffect = "e";
    /// <summary>The product type code of a badge.</summary>
    public const string TypeBadge = "b";

    /// <summary>Parses a catalog product from a packet.</summary>
    /// <param name="p">The packet reader.</param>
    /// <remarks>A badge product carries only its type and badge code on the wire.</remarks>
    public static CatalogProduct Parse(in PacketReader p)
    {
        string productType = p.ReadString();
        bool isBadge = productType == TypeBadge;
        if (!isBadge)
        {
            int furniClassId = p.ReadInt();
            string extraParam = p.ReadString();
            int productCount = p.ReadInt();
            bool uniqueLimited = p.ReadBool();
            int seriesSize = 0, itemsLeft = 0;
            if (uniqueLimited)
            {
                seriesSize = p.ReadInt();
                itemsLeft = p.ReadInt();
            }
            return new CatalogProduct(productType, furniClassId, extraParam, productCount, uniqueLimited, seriesSize, itemsLeft);
        }

        return new CatalogProduct(productType, 0, p.ReadString(), 1, false, 0, 0);
    }

    /// <summary>Composes the catalog product into a packet.</summary>
    /// <param name="p">The packet writer.</param>
    public void Compose(in PacketWriter p)
    {
        p.WriteString(ProductType);
        bool isBadge = ProductType == TypeBadge;
        if (!isBadge)
        {
            p.WriteInt(FurniClassId);
            p.WriteString(ExtraParam);
            p.WriteInt(ProductCount);
            p.WriteBool(UniqueLimitedItem);
            if (UniqueLimitedItem)
            {
                p.WriteInt(UniqueLimitedItemSeriesSize);
                p.WriteInt(UniqueLimitedItemsLeft);
            }
        }
        else
        {
            p.WriteString(ExtraParam);
        }
    }
}

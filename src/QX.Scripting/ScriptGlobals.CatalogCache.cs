using Qx.Game;
using Qx.Model;
using Qx.Model.Messages.Incoming;

namespace Qx.Scripting;

public partial class ScriptGlobals
{
    /// <summary>
    /// Loads the whole catalog once and keeps it, so later searches answer from memory.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is the expensive call: a full catalog is well over a hundred round trips. The index is
    /// fetched first, then every page node that has offers is loaded in index order, with a
    /// timeout of 15000 milliseconds per request. A page request that times out is counted as
    /// refused and the walk continues. <see cref="FindCatalogOffers"/> and
    /// <see cref="CachedCatalogPages"/> answer from what this collected, and
    /// <see cref="GetCatalogPage"/> answers from it while a page is still fresh.
    /// </para>
    /// <para>
    /// Safe to call again. Pages already held and still current are skipped, so a second call after
    /// an interrupted walk finishes it rather than repeating it, and the cache is cleared by itself
    /// when the hotel announces a republish.
    /// </para>
    /// </remarks>
    /// <param name="catalogType">The catalog mode, <c>NORMAL</c> or <c>BUILDERS_CLUB</c>, ignoring case.</param>
    /// <param name="onlyVisible"><see langword="true"/> to skip hidden index nodes and everything below them; otherwise, <see langword="false"/>.</param>
    /// <param name="delayMs">The pause after each page request, in milliseconds.</param>
    /// <param name="maxAgeMinutes">
    /// The maximum age of a cached page, in minutes, before it is fetched again. Zero forces a
    /// full refetch.
    /// </param>
    /// <param name="onProgress">
    /// The callback to call with the number of pages done and the total after each page, or
    /// <see langword="null"/> for none.
    /// </param>
    /// <returns>A report of how many pages were loaded, cached and refused.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="maxAgeMinutes"/> is negative, another argument is out of range, or the
    /// catalog type is not known.
    /// </exception>
    /// <exception cref="InvalidOperationException">Thrown when there is no active hotel session.</exception>
    /// <exception cref="Qx.Game.RequestTimeoutException">Thrown when the catalog index did not arrive in time.</exception>
    public Task<CatalogLoadReport> LoadCatalog(
        string catalogType = "NORMAL",
        bool onlyVisible = true,
        int delayMs = 0,
        double maxAgeMinutes = 5,
        Action<int, int>? onProgress = null)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(maxAgeMinutes);
        IProgress<(int Loaded, int Total)>? progress = onProgress is null
            ? null
            : new Progress<(int Loaded, int Total)>(step => onProgress(step.Loaded, step.Total));

        return Game.Catalog.LoadAllPagesAsync(
            catalogType,
            onlyVisible,
            delayMs,
            TimeSpan.FromMinutes(maxAgeMinutes),
            15000,
            progress,
            Ct);
    }

    /// <summary>
    /// Searches the cached catalog by text, without asking the hotel.
    /// </summary>
    /// <remarks>
    /// Searches only what is cached, so run <see cref="LoadCatalog"/> first. An offer matches when
    /// its localization id, or the product type or extra parameter of one of its products,
    /// contains the text, when a product's furni class id equals it, or when the product's
    /// <see cref="ProductDisplayName"/> contains it. The display name is what makes a search for a
    /// furni name work.
    /// </remarks>
    /// <param name="text">The text to look for, matched ignoring case.</param>
    /// <param name="catalogType">The catalog mode, <c>NORMAL</c> or <c>BUILDERS_CLUB</c>, ignoring case.</param>
    /// <returns>The matching offers with the page that holds each of them.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="text"/> is empty.</exception>
    public IReadOnlyList<CatalogOfferMatch> FindCatalogOffers(
        string text,
        string catalogType = "NORMAL") =>
        Game.Catalog.FindOffers(text, catalogType, ProductDisplayName);

    /// <summary>Gets every catalog page currently held in the cache, ordered by page id.</summary>
    /// <remarks>Pages are returned regardless of their age.</remarks>
    /// <param name="catalogType">The catalog mode, <c>NORMAL</c> or <c>BUILDERS_CLUB</c>, ignoring case.</param>
    /// <returns>The cached pages.</returns>
    public IReadOnlyList<CatalogPage> CachedCatalogPages(string catalogType = "NORMAL") =>
        Game.Catalog.CachedPages(catalogType);

    /// <summary>
    /// Gets what the catalog cache holds and how old it is, for deciding whether to reload.
    /// </summary>
    /// <param name="catalogType">The catalog mode, <c>NORMAL</c> or <c>BUILDERS_CLUB</c>, ignoring case.</param>
    /// <returns>The cache state of the catalog mode.</returns>
    public CatalogCacheState CatalogCache(string catalogType = "NORMAL") =>
        Game.Catalog.CacheState(catalogType);

    /// <summary>Clears the cached catalog so the next read fetches it again.</summary>
    /// <param name="catalogType">The catalog mode to clear, or <see langword="null"/> to clear every mode.</param>
    public void ClearCatalogCache(string? catalogType = null) =>
        Game.Catalog.ClearCache(catalogType);

    /// <summary>
    /// Gets the display name of a catalog product, resolved through the downloaded game data.
    /// </summary>
    /// <remarks>
    /// Floor and wall products resolve through the furni data, badge products to the badge name
    /// and effect products to the effect name. Anything else, or anything the game data does not
    /// know, gives <see langword="null"/>, which keeps it usable as a search input.
    /// </remarks>
    /// <param name="product">The product.</param>
    /// <returns>The display name, or <see langword="null"/> when it is not known.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="product"/> is <see langword="null"/>.</exception>
    public string? ProductDisplayName(CatalogProduct product)
    {
        ArgumentNullException.ThrowIfNull(product);
        return product.ProductType switch
        {
            "s" => Game.GameData.Furni?.GetInfo(ItemType.Floor, product.FurniClassId)?.Name,
            "i" => Game.GameData.Furni?.GetInfo(ItemType.Wall, product.FurniClassId)?.Name,
            "b" => Game.GameData.Texts?.BadgeName(product.ExtraParam),
            "e" => Game.GameData.Texts?.EffectName(product.FurniClassId),
            _ => null
        };
    }
}

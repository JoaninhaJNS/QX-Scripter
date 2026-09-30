using Qx.Model.Messages.Incoming;

namespace Qx.Game;

public sealed partial class CatalogManager
{
    /// <summary>The maximum age of a cached index or page before it is requested again, used when no age is given.</summary>
    public static readonly TimeSpan DefaultMaxAge = TimeSpan.FromMinutes(5);

    /// <summary>Gets the catalog index from the cache, or requests it from the server when the cached one is missing or too old.</summary>
    /// <remarks>
    /// Concurrent callers for the same catalog type share one request. Receiving a new index clears
    /// the cached pages of that catalog type.
    /// </remarks>
    /// <param name="catalogType">The catalog type, <c>NORMAL</c> or <c>BUILDERS_CLUB</c>, ignoring case.</param>
    /// <param name="maxAge">
    /// The maximum age of a cached index, or <see langword="null"/> for <see cref="DefaultMaxAge"/>.
    /// <see cref="Timeout.InfiniteTimeSpan"/> accepts any cached index and <see cref="TimeSpan.Zero"/> always requests a new one.
    /// </param>
    /// <param name="timeoutMs">The time to wait for the server, in milliseconds, from 1 to 120000.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>A task that completes with the catalog index.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when an argument is outside its allowed range or the catalog type is not known.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the application runtime is not active or there is no hotel session.</exception>
    /// <exception cref="RequestTimeoutException">Thrown when the index does not arrive within <paramref name="timeoutMs"/>.</exception>
    public Task<CatalogIndex> GetIndexAsync(
        string catalogType = "NORMAL",
        TimeSpan? maxAge = null,
        int timeoutMs = 10000,
        CancellationToken cancellationToken = default) =>
        BrowseOperations().GetIndexAsync(
            catalogType,
            maxAge,
            timeoutMs,
            cancellationToken);

    /// <summary>Gets a catalog page from the cache, or requests it from the server when the cached one is missing or too old.</summary>
    /// <remarks>Concurrent callers for the same page share one request.</remarks>
    /// <param name="pageId">The id of the page.</param>
    /// <param name="catalogType">The catalog type, <c>NORMAL</c> or <c>BUILDERS_CLUB</c>, ignoring case.</param>
    /// <param name="maxAge">
    /// The maximum age of a cached page, or <see langword="null"/> for <see cref="DefaultMaxAge"/>.
    /// <see cref="Timeout.InfiniteTimeSpan"/> accepts any cached page and <see cref="TimeSpan.Zero"/> always requests a new one.
    /// </param>
    /// <param name="offerId">The offer id sent with the page request, or -1 for none.</param>
    /// <param name="timeoutMs">The time to wait for the server, in milliseconds, from 1 to 120000.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>A task that completes with the catalog page.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when an argument is outside its allowed range or the catalog type is not known.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the application runtime is not active or there is no hotel session.</exception>
    /// <exception cref="RequestTimeoutException">Thrown when the page does not arrive within <paramref name="timeoutMs"/>.</exception>
    public Task<CatalogPage> GetPageAsync(
        int pageId,
        string catalogType = "NORMAL",
        TimeSpan? maxAge = null,
        int offerId = -1,
        int timeoutMs = 10000,
        CancellationToken cancellationToken = default) =>
        BrowseOperations().GetPageAsync(
            pageId,
            catalogType,
            maxAge,
            offerId,
            timeoutMs,
            cancellationToken);

    /// <summary>Loads every page listed in the catalog index into the cache.</summary>
    /// <remarks>
    /// The index is fetched first, then each page node that has offers is loaded in index order.
    /// Pages still fresh in the cache are not requested again. A page request that times out is
    /// counted as refused and the walk continues.
    /// </remarks>
    /// <param name="catalogType">The catalog type, <c>NORMAL</c> or <c>BUILDERS_CLUB</c>, ignoring case.</param>
    /// <param name="onlyVisible">Whether to skip hidden index nodes and everything below them.</param>
    /// <param name="delayMs">The pause after each page request, in milliseconds.</param>
    /// <param name="maxAge">
    /// The maximum age of a cached index or page, or <see langword="null"/> for <see cref="DefaultMaxAge"/>.
    /// </param>
    /// <param name="timeoutMs">The time to wait for each request, in milliseconds, from 1 to 120000.</param>
    /// <param name="progress">An optional receiver of the number of pages done and the total, reported after each page.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>A task that completes with a report of how many pages were loaded, cached and refused.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when an argument is outside its allowed range or the catalog type is not known.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the application runtime is not active or there is no hotel session.</exception>
    /// <exception cref="RequestTimeoutException">Thrown when the index does not arrive within <paramref name="timeoutMs"/>.</exception>
    public Task<CatalogLoadReport> LoadAllPagesAsync(
        string catalogType = "NORMAL",
        bool onlyVisible = true,
        int delayMs = 0,
        TimeSpan? maxAge = null,
        int timeoutMs = 15000,
        IProgress<(int Loaded, int Total)>? progress = null,
        CancellationToken cancellationToken = default) =>
        BrowseOperations().LoadAllPagesAsync(
            catalogType,
            onlyVisible,
            delayMs,
            maxAge,
            timeoutMs,
            progress,
            cancellationToken);

    /// <summary>Gets the cached pages of a catalog type, ordered by page id.</summary>
    /// <remarks>Pages are returned regardless of their age.</remarks>
    /// <param name="catalogType">The catalog type, <c>NORMAL</c> or <c>BUILDERS_CLUB</c>, ignoring case.</param>
    /// <returns>The cached pages.</returns>
    public IReadOnlyList<CatalogPage> CachedPages(string catalogType = "NORMAL") =>
        BrowseOperations().CachedPages(catalogType);

    /// <summary>Gets every offer on the cached pages of a catalog type.</summary>
    /// <param name="catalogType">The catalog type, <c>NORMAL</c> or <c>BUILDERS_CLUB</c>, ignoring case.</param>
    /// <returns>The offers with the page that holds each of them.</returns>
    public IReadOnlyList<CatalogOfferMatch> CachedOffers(string catalogType = "NORMAL") =>
        BrowseOperations().CachedOffers(catalogType);

    /// <summary>Gets what the cache holds for a catalog type.</summary>
    /// <param name="catalogType">The catalog type, <c>NORMAL</c> or <c>BUILDERS_CLUB</c>, ignoring case.</param>
    /// <returns>The cache state of the catalog type.</returns>
    public CatalogCacheState CacheState(string catalogType = "NORMAL") =>
        BrowseOperations().CacheState(catalogType);

    /// <summary>Searches the offers on the cached pages of a catalog type.</summary>
    /// <remarks>
    /// An offer matches when its localization id, or the product type or extra parameter of one of its
    /// products, contains <paramref name="text"/>, when a product's furni class id equals it, or when
    /// <paramref name="describe"/> returns text for a product that contains it. Comparisons ignore case.
    /// Only cached pages are searched; no request is sent.
    /// </remarks>
    /// <param name="text">The text to search for.</param>
    /// <param name="catalogType">The catalog type, <c>NORMAL</c> or <c>BUILDERS_CLUB</c>, ignoring case.</param>
    /// <param name="describe">An optional function that returns a searchable description of a product, such as its furni name.</param>
    /// <returns>The matching offers with the page that holds each of them.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="text"/> is empty.</exception>
    public IReadOnlyList<CatalogOfferMatch> FindOffers(
        string text,
        string catalogType = "NORMAL",
        Func<CatalogProduct, string?>? describe = null) =>
        BrowseOperations().FindOffers(text, catalogType, describe);

    /// <summary>Clears the cached index and pages of a catalog type, or of every type.</summary>
    /// <param name="catalogType">The catalog type to clear, or <see langword="null"/> to clear every type.</param>
    public void ClearCache(string? catalogType = null) =>
        BrowseOperations().ClearCache(catalogType);
}

/// <summary>Represents the result of loading every catalog page into the cache.</summary>
/// <param name="Loaded">The number of pages requested and received.</param>
/// <param name="AlreadyCached">The number of pages that were fresh in the cache.</param>
/// <param name="Refused">The number of pages whose request timed out.</param>
/// <param name="Total">The number of pages the index listed for loading.</param>
public sealed record CatalogLoadReport(int Loaded, int AlreadyCached, int Refused, int Total)
{
    /// <summary>Gets the number of pages available in the cache after the load, <see cref="Loaded"/> plus <see cref="AlreadyCached"/>.</summary>
    public int Available => Loaded + AlreadyCached;
}

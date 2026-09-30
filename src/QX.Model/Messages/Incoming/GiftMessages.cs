using Qx.Messages;

namespace Qx.Model.Messages.Incoming;

/// <summary>
/// Represents the <c>GiftWrappingConfiguration</c> message, received with the gift wrapping options.
/// </summary>
public sealed record GiftWrappingConfiguration : IParserComposer<GiftWrappingConfiguration>
{
    private IReadOnlyList<int> _stuff_types = Array.AsReadOnly(Array.Empty<int>());
    private IReadOnlyList<int> _box_types = Array.AsReadOnly(Array.Empty<int>());
    private IReadOnlyList<int> _ribbon_types = Array.AsReadOnly(Array.Empty<int>());
    private IReadOnlyList<int> _default_stuff_types = Array.AsReadOnly(Array.Empty<int>());

    /// <summary>Initializes a new instance of the <see cref="GiftWrappingConfiguration"/> record.</summary>
    /// <param name="IsWrappingEnabled">Whether gift wrapping is enabled.</param>
    /// <param name="WrappingPrice">The price of gift wrapping.</param>
    /// <param name="StuffTypes">The gift furni sprite identifiers offered as wrapping.</param>
    /// <param name="BoxTypes">The box types offered for wrapping.</param>
    /// <param name="RibbonTypes">The ribbon types offered for wrapping.</param>
    /// <param name="DefaultStuffTypes">The default gift furni sprite identifiers.</param>
    public GiftWrappingConfiguration(
        bool IsWrappingEnabled,
        int WrappingPrice,
        IReadOnlyList<int> StuffTypes,
        IReadOnlyList<int> BoxTypes,
        IReadOnlyList<int> RibbonTypes,
        IReadOnlyList<int> DefaultStuffTypes)
    {
        this.IsWrappingEnabled = IsWrappingEnabled;
        this.WrappingPrice = WrappingPrice;
        this.StuffTypes = StuffTypes;
        this.BoxTypes = BoxTypes;
        this.RibbonTypes = RibbonTypes;
        this.DefaultStuffTypes = DefaultStuffTypes;
    }

    /// <summary>Gets whether gift wrapping is enabled.</summary>
    public bool IsWrappingEnabled { get; init; }

    /// <summary>Gets the price of gift wrapping.</summary>
    public int WrappingPrice { get; init; }

    /// <summary>
    /// Gets the gift furni sprite identifiers offered as wrapping, as a read only copy.
    /// </summary>
    /// <remarks>The list may hold at most 65535 entries.</remarks>
    public IReadOnlyList<int> StuffTypes
    {
        get => _stuff_types;
        init => _stuff_types = GiftWire.FreezeValues(value, nameof(StuffTypes));
    }

    /// <summary>Gets the box types offered for wrapping, as a read only copy.</summary>
    /// <remarks>The list may hold at most 65535 entries.</remarks>
    public IReadOnlyList<int> BoxTypes
    {
        get => _box_types;
        init => _box_types = GiftWire.FreezeValues(value, nameof(BoxTypes));
    }

    /// <summary>Gets the ribbon types offered for wrapping, as a read only copy.</summary>
    /// <remarks>The list may hold at most 65535 entries.</remarks>
    public IReadOnlyList<int> RibbonTypes
    {
        get => _ribbon_types;
        init => _ribbon_types = GiftWire.FreezeValues(value, nameof(RibbonTypes));
    }

    /// <summary>Gets the default gift furni sprite identifiers, as a read only copy.</summary>
    /// <remarks>The list may hold at most 65535 entries.</remarks>
    public IReadOnlyList<int> DefaultStuffTypes
    {
        get => _default_stuff_types;
        init => _default_stuff_types = GiftWire.FreezeValues(value, nameof(DefaultStuffTypes));
    }

    /// <summary>Parses the message from a packet.</summary>
    /// <param name="p">The packet reader.</param>
    public static GiftWrappingConfiguration Parse(in PacketReader p) =>
        FlashWire.Parse(in p, ParseFlash);

    private static GiftWrappingConfiguration ParseFlash(in PacketReader p) =>
        GiftWire.ParseWrappingConfiguration(in p);

    /// <summary>Composes the message into a packet.</summary>
    /// <param name="p">The packet writer.</param>
    public void Compose(in PacketWriter p) =>
        FlashWire.Compose(this, in p, ComposeFlash);

    private static void ComposeFlash(GiftWrappingConfiguration value, in PacketWriter p) =>
        GiftWire.ComposeWrappingConfiguration(value, in p);

    /// <summary>Deconstructs the configuration into its values.</summary>
    /// <param name="IsWrappingEnabled">Whether gift wrapping is enabled.</param>
    /// <param name="WrappingPrice">The price of gift wrapping.</param>
    /// <param name="StuffTypes">The gift furni sprite identifiers offered as wrapping.</param>
    /// <param name="BoxTypes">The box types offered for wrapping.</param>
    /// <param name="RibbonTypes">The ribbon types offered for wrapping.</param>
    /// <param name="DefaultStuffTypes">The default gift furni sprite identifiers.</param>
    public void Deconstruct(
        out bool IsWrappingEnabled,
        out int WrappingPrice,
        out IReadOnlyList<int> StuffTypes,
        out IReadOnlyList<int> BoxTypes,
        out IReadOnlyList<int> RibbonTypes,
        out IReadOnlyList<int> DefaultStuffTypes)
    {
        IsWrappingEnabled = this.IsWrappingEnabled;
        WrappingPrice = this.WrappingPrice;
        StuffTypes = this.StuffTypes;
        BoxTypes = this.BoxTypes;
        RibbonTypes = this.RibbonTypes;
        DefaultStuffTypes = this.DefaultStuffTypes;
    }
}

/// <summary>
/// Represents the <c>PresentOpened</c> message, received when a present has been opened and its contents
/// are revealed.
/// </summary>
/// <param name="ItemType">The type of the item inside the present.</param>
/// <param name="ClassId">The class identifier of the item inside the present.</param>
/// <param name="ProductCode">The product code of the present's contents.</param>
/// <param name="PlacedItemId">The identifier of the item that was placed, sent by Flash as a 32 bit integer.</param>
/// <param name="PlacedItemType">The type of the item that was placed.</param>
/// <param name="PlacedInRoom">Whether the item was placed straight into the room.</param>
/// <param name="PetFigureString">The pet figure when the present held a pet.</param>
public sealed record PresentOpened(
    string ItemType,
    int ClassId,
    string ProductCode,
    Id PlacedItemId,
    string PlacedItemType,
    bool PlacedInRoom,
    string PetFigureString) : IParserComposer<PresentOpened>
{
    /// <summary>Parses the message from a packet.</summary>
    /// <param name="p">The packet reader.</param>
    public static PresentOpened Parse(in PacketReader p) =>
        FlashWire.Parse(in p, ParseFlash);

    private static PresentOpened ParseFlash(in PacketReader p) =>
        GiftWire.ParsePresentOpened(in p);

    /// <summary>Composes the message into a packet.</summary>
    /// <param name="p">The packet writer.</param>
    public void Compose(in PacketWriter p) =>
        FlashWire.Compose(this, in p, ComposeFlash);

    private static void ComposeFlash(PresentOpened value, in PacketWriter p) =>
        GiftWire.ComposePresentOpened(value, in p);
}

/// <summary>Represents the eligibility of a club gift offer.</summary>
/// <param name="OfferId">The identifier of the catalog offer the entry applies to.</param>
/// <param name="IsVip">
/// Whether the gift is a VIP club gift. Flash always sends it, and composing throws when it is
/// <see langword="null"/>.
/// </param>
/// <param name="DaysRequired">The number of club days the gift requires.</param>
/// <param name="IsSelectable">Whether the gift can be selected.</param>
public sealed record ClubGiftEligibility(
    int OfferId,
    bool? IsVip,
    int DaysRequired,
    bool IsSelectable) : IParserComposer<ClubGiftEligibility>
{
    /// <summary>Parses a club gift eligibility entry from a packet.</summary>
    /// <param name="p">The packet reader.</param>
    public static ClubGiftEligibility Parse(in PacketReader p) =>
        FlashWire.Parse(in p, ParseFlash);

    private static ClubGiftEligibility ParseFlash(in PacketReader p) =>
        GiftWire.ParseEligibility(in p);

    /// <summary>Composes the club gift eligibility entry into a packet.</summary>
    /// <param name="p">The packet writer.</param>
    public void Compose(in PacketWriter p) =>
        FlashWire.Compose(this, in p, ComposeFlash);

    private static void ComposeFlash(ClubGiftEligibility value, in PacketWriter p) =>
        GiftWire.ComposeEligibility(value, in p);
}

/// <summary>Represents the <c>ClubGiftInfo</c> message, received with the club gifts that can be selected.</summary>
public sealed record ClubGiftInfo : IParserComposer<ClubGiftInfo>
{
    private IReadOnlyList<CatalogPageOffer> _offers = Array.AsReadOnly(Array.Empty<CatalogPageOffer>());
    private IReadOnlyList<ClubGiftEligibility> _gift_eligibility =
        Array.AsReadOnly(Array.Empty<ClubGiftEligibility>());

    /// <summary>Initializes a new instance of the <see cref="ClubGiftInfo"/> record.</summary>
    /// <param name="DaysUntilNextGift">The number of days until the next club gift.</param>
    /// <param name="GiftsAvailable">The number of club gifts that can be selected now.</param>
    /// <param name="Offers">The catalog offers that can be chosen as a club gift.</param>
    /// <param name="GiftEligibility">The eligibility of each club gift offer.</param>
    public ClubGiftInfo(
        int DaysUntilNextGift,
        int GiftsAvailable,
        IReadOnlyList<CatalogPageOffer> Offers,
        IReadOnlyList<ClubGiftEligibility> GiftEligibility)
    {
        this.DaysUntilNextGift = DaysUntilNextGift;
        this.GiftsAvailable = GiftsAvailable;
        this.Offers = Offers;
        this.GiftEligibility = GiftEligibility;
    }

    /// <summary>Gets the number of days until the next club gift.</summary>
    public int DaysUntilNextGift { get; init; }

    /// <summary>Gets the number of club gifts that can be selected now.</summary>
    public int GiftsAvailable { get; init; }

    /// <summary>
    /// Gets the catalog offers that can be chosen as a club gift, as a read only copy.
    /// </summary>
    /// <remarks>The list may hold at most 4096 entries.</remarks>
    public IReadOnlyList<CatalogPageOffer> Offers
    {
        get => _offers;
        init => _offers = CatalogWire.FreezeReferences(
            value,
            CatalogPageWire.MaximumOffers,
            nameof(Offers));
    }

    /// <summary>Gets the eligibility of each club gift offer, as a read only copy.</summary>
    /// <remarks>The list may hold at most 65535 entries.</remarks>
    public IReadOnlyList<ClubGiftEligibility> GiftEligibility
    {
        get => _gift_eligibility;
        init => _gift_eligibility = CatalogWire.FreezeReferences(
            value,
            GiftWire.MaximumCollectionCount,
            nameof(GiftEligibility));
    }

    /// <summary>Parses the message from a packet.</summary>
    /// <param name="p">The packet reader.</param>
    public static ClubGiftInfo Parse(in PacketReader p) =>
        FlashWire.Parse(in p, ParseFlash);

    private static ClubGiftInfo ParseFlash(in PacketReader p) =>
        GiftWire.ParseClubGiftInfo(in p);

    /// <summary>Composes the message into a packet.</summary>
    /// <param name="p">The packet writer.</param>
    public void Compose(in PacketWriter p) =>
        FlashWire.Compose(this, in p, ComposeFlash);

    private static void ComposeFlash(ClubGiftInfo value, in PacketWriter p) =>
        GiftWire.ComposeClubGiftInfo(value, in p);

    /// <summary>Deconstructs the club gift information into its values.</summary>
    /// <param name="DaysUntilNextGift">The number of days until the next club gift.</param>
    /// <param name="GiftsAvailable">The number of club gifts that can be selected now.</param>
    /// <param name="Offers">The catalog offers that can be chosen as a club gift.</param>
    /// <param name="GiftEligibility">The eligibility of each club gift offer.</param>
    public void Deconstruct(
        out int DaysUntilNextGift,
        out int GiftsAvailable,
        out IReadOnlyList<CatalogPageOffer> Offers,
        out IReadOnlyList<ClubGiftEligibility> GiftEligibility)
    {
        DaysUntilNextGift = this.DaysUntilNextGift;
        GiftsAvailable = this.GiftsAvailable;
        Offers = this.Offers;
        GiftEligibility = this.GiftEligibility;
    }
}

/// <summary>
/// Represents the <c>ClubGiftSelected</c> message, received when the server confirms a selected club gift.
/// </summary>
public sealed record ClubGiftSelected : IParserComposer<ClubGiftSelected>
{
    private string _product_code = "";
    private IReadOnlyList<CatalogProduct> _products = Array.AsReadOnly(Array.Empty<CatalogProduct>());

    /// <summary>Initializes a new instance of the <see cref="ClubGiftSelected"/> record.</summary>
    /// <param name="ProductCode">The product code of the selected club gift.</param>
    /// <param name="Products">The products the club gift granted.</param>
    public ClubGiftSelected(
        string ProductCode,
        IReadOnlyList<CatalogProduct> Products)
    {
        this.ProductCode = ProductCode;
        this.Products = Products;
    }

    /// <summary>Gets the product code of the selected club gift.</summary>
    public string ProductCode
    {
        get => _product_code;
        init => _product_code = CatalogWire.RequireReference(value, nameof(ProductCode));
    }

    /// <summary>Gets the products the club gift granted, as a read only copy.</summary>
    /// <remarks>The list may hold at most 65535 entries.</remarks>
    public IReadOnlyList<CatalogProduct> Products
    {
        get => _products;
        init => _products = CatalogWire.FreezeReferences(
            value,
            CatalogPageWire.MaximumProducts,
            nameof(Products));
    }

    /// <summary>Parses the message from a packet.</summary>
    /// <param name="p">The packet reader.</param>
    public static ClubGiftSelected Parse(in PacketReader p) =>
        FlashWire.Parse(in p, ParseFlash);

    private static ClubGiftSelected ParseFlash(in PacketReader p) =>
        GiftWire.ParseClubGiftSelected(in p);

    /// <summary>Composes the message into a packet.</summary>
    /// <param name="p">The packet writer.</param>
    public void Compose(in PacketWriter p) =>
        FlashWire.Compose(this, in p, ComposeFlash);

    private static void ComposeFlash(ClubGiftSelected value, in PacketWriter p) =>
        GiftWire.ComposeClubGiftSelected(value, in p);

    /// <summary>Deconstructs the selection into its product code and products.</summary>
    /// <param name="ProductCode">The product code of the selected club gift.</param>
    /// <param name="Products">The products the club gift granted.</param>
    public void Deconstruct(
        out string ProductCode,
        out IReadOnlyList<CatalogProduct> Products)
    {
        ProductCode = this.ProductCode;
        Products = this.Products;
    }
}

/// <summary>
/// Represents the <c>GiftReceiverNotFound</c> message, received when the receiver of a gift purchase does not exist.
/// </summary>
public sealed record GiftReceiverNotFound : IParserComposer<GiftReceiverNotFound>
{
    /// <summary>Parses the message from a packet.</summary>
    /// <param name="p">The packet reader.</param>
    public static GiftReceiverNotFound Parse(in PacketReader p) =>
        FlashWire.Parse(in p, ParseFlash);

    private static GiftReceiverNotFound ParseFlash(in PacketReader p)
    {
        CatalogWire.RequireEmpty(in p, nameof(GiftReceiverNotFound));
        return new GiftReceiverNotFound();
    }

    /// <summary>Composes the message into a packet.</summary>
    /// <param name="p">The packet writer.</param>
    public void Compose(in PacketWriter p) =>
        FlashWire.Compose(this, in p, ComposeFlash);

    private static void ComposeFlash(GiftReceiverNotFound value, in PacketWriter p) { }
}

/// <summary>Represents the <c>ClubGiftNotification</c> message, received when club gifts can be selected.</summary>
/// <param name="NumGifts">The number of club gifts that can be selected.</param>
public sealed record ClubGiftNotification(int NumGifts) : IParserComposer<ClubGiftNotification>
{
    /// <summary>Parses the message from a packet.</summary>
    /// <param name="p">The packet reader.</param>
    public static ClubGiftNotification Parse(in PacketReader p) =>
        FlashWire.Parse(in p, ParseFlash);

    private static ClubGiftNotification ParseFlash(in PacketReader p)
    {
        var value = new ClubGiftNotification(p.ReadInt());
        CatalogWire.RequireEmpty(in p, nameof(ClubGiftNotification));
        return value;
    }

    /// <summary>Composes the message into a packet.</summary>
    /// <param name="p">The packet writer.</param>
    public void Compose(in PacketWriter p) =>
        FlashWire.Compose(this, in p, ComposeFlash);

    private static void ComposeFlash(ClubGiftNotification value, in PacketWriter p) =>
        p.WriteInt(value.NumGifts);
}

/// <summary>
/// Represents the <c>IsOfferGiftable</c> message, received with whether a catalog offer can be sent as a gift.
/// </summary>
/// <param name="OfferId">The identifier of the catalog offer.</param>
/// <param name="IsGiftable">Whether the offer can be sent as a gift.</param>
public sealed record IsOfferGiftable(
    int OfferId,
    bool IsGiftable) : IParserComposer<IsOfferGiftable>
{
    /// <summary>Parses the message from a packet.</summary>
    /// <param name="p">The packet reader.</param>
    public static IsOfferGiftable Parse(in PacketReader p) =>
        FlashWire.Parse(in p, ParseFlash);

    private static IsOfferGiftable ParseFlash(in PacketReader p)
    {
        var value = new IsOfferGiftable(p.ReadInt(), p.ReadBool());
        CatalogWire.RequireEmpty(in p, nameof(IsOfferGiftable));
        return value;
    }

    /// <summary>Composes the message into a packet.</summary>
    /// <param name="p">The packet writer.</param>
    public void Compose(in PacketWriter p) =>
        FlashWire.Compose(this, in p, ComposeFlash);

    private static void ComposeFlash(IsOfferGiftable value, in PacketWriter p)
    {
        p.WriteInt(value.OfferId);
        p.WriteBool(value.IsGiftable);
    }
}

/// <summary>Represents a product inside a new user gift option.</summary>
/// <param name="ProductCode">The product code.</param>
/// <param name="LocalizationKey">
/// The localization key of the product, or <see langword="null"/> when the server sends an empty string.
/// </param>
public sealed record NuxGiftProduct(
    string ProductCode,
    string? LocalizationKey) : IParserComposer<NuxGiftProduct>
{
    /// <summary>Parses a new user gift product from a packet.</summary>
    /// <param name="p">The packet reader.</param>
    public static NuxGiftProduct Parse(in PacketReader p) =>
        FlashWire.Parse(in p, ParseFlash);

    private static NuxGiftProduct ParseFlash(in PacketReader p) =>
        GiftWire.ParseNuxProduct(in p);

    /// <summary>Composes the new user gift product into a packet.</summary>
    /// <param name="p">The packet writer.</param>
    public void Compose(in PacketWriter p) =>
        FlashWire.Compose(this, in p, ComposeFlash);

    private static void ComposeFlash(NuxGiftProduct value, in PacketWriter p) =>
        GiftWire.ComposeNuxProduct(value, in p);
}

/// <summary>Represents one choice in a step of the new user gift offer.</summary>
public sealed record NuxGiftOption : IParserComposer<NuxGiftOption>
{
    private IReadOnlyList<NuxGiftProduct> _products = Array.AsReadOnly(Array.Empty<NuxGiftProduct>());

    /// <summary>Initializes a new instance of the <see cref="NuxGiftOption"/> record.</summary>
    /// <param name="ThumbnailUrl">The thumbnail image URL, or <see langword="null"/> when there is none.</param>
    /// <param name="Products">The products the option grants.</param>
    public NuxGiftOption(string? ThumbnailUrl, IReadOnlyList<NuxGiftProduct> Products)
    {
        this.ThumbnailUrl = ThumbnailUrl;
        this.Products = Products;
    }

    /// <summary>
    /// Gets the thumbnail image URL, or <see langword="null"/> when the server sends an empty string.
    /// </summary>
    public string? ThumbnailUrl { get; init; }

    /// <summary>Gets the products the option grants, as a read only copy.</summary>
    /// <remarks>The list may hold at most 65535 entries.</remarks>
    public IReadOnlyList<NuxGiftProduct> Products
    {
        get => _products;
        init => _products = CatalogWire.FreezeReferences(
            value,
            GiftWire.MaximumNuxProducts,
            nameof(Products));
    }

    /// <summary>Parses a new user gift option from a packet.</summary>
    /// <param name="p">The packet reader.</param>
    public static NuxGiftOption Parse(in PacketReader p) =>
        FlashWire.Parse(in p, ParseFlash);

    private static NuxGiftOption ParseFlash(in PacketReader p) =>
        GiftWire.ParseNuxOption(in p);

    /// <summary>Composes the new user gift option into a packet.</summary>
    /// <param name="p">The packet writer.</param>
    public void Compose(in PacketWriter p) =>
        FlashWire.Compose(this, in p, ComposeFlash);

    private static void ComposeFlash(NuxGiftOption value, in PacketWriter p) =>
        GiftWire.ComposeNuxOption(value, in p);

    /// <summary>Deconstructs the option into its thumbnail and products.</summary>
    /// <param name="ThumbnailUrl">The thumbnail image URL, or <see langword="null"/> when there is none.</param>
    /// <param name="Products">The products the option grants.</param>
    public void Deconstruct(out string? ThumbnailUrl, out IReadOnlyList<NuxGiftProduct> Products)
    {
        ThumbnailUrl = this.ThumbnailUrl;
        Products = this.Products;
    }
}

/// <summary>Represents one step of the new user gift offer, with the options to choose from.</summary>
public sealed record NuxGiftStep : IParserComposer<NuxGiftStep>
{
    private IReadOnlyList<NuxGiftOption> _options = Array.AsReadOnly(Array.Empty<NuxGiftOption>());

    /// <summary>Initializes a new instance of the <see cref="NuxGiftStep"/> record.</summary>
    /// <param name="DayIndex">The day index of the step.</param>
    /// <param name="StepIndex">The step index.</param>
    /// <param name="Options">The options to choose from.</param>
    public NuxGiftStep(int DayIndex, int StepIndex, IReadOnlyList<NuxGiftOption> Options)
    {
        this.DayIndex = DayIndex;
        this.StepIndex = StepIndex;
        this.Options = Options;
    }

    /// <summary>Gets the day index of the step.</summary>
    public int DayIndex { get; init; }

    /// <summary>Gets the step index.</summary>
    public int StepIndex { get; init; }

    /// <summary>Gets the options to choose from, as a read only copy.</summary>
    /// <remarks>The list may hold at most 65535 entries.</remarks>
    public IReadOnlyList<NuxGiftOption> Options
    {
        get => _options;
        init => _options = CatalogWire.FreezeReferences(
            value,
            GiftWire.MaximumNuxOptions,
            nameof(Options));
    }

    /// <summary>Parses a new user gift step from a packet.</summary>
    /// <param name="p">The packet reader.</param>
    public static NuxGiftStep Parse(in PacketReader p) =>
        FlashWire.Parse(in p, ParseFlash);

    private static NuxGiftStep ParseFlash(in PacketReader p) =>
        GiftWire.ParseNuxStep(in p);

    /// <summary>Composes the new user gift step into a packet.</summary>
    /// <param name="p">The packet writer.</param>
    public void Compose(in PacketWriter p) =>
        FlashWire.Compose(this, in p, ComposeFlash);

    private static void ComposeFlash(NuxGiftStep value, in PacketWriter p) =>
        GiftWire.ComposeNuxStep(value, in p);

    /// <summary>Deconstructs the step into its indexes and options.</summary>
    /// <param name="DayIndex">The day index of the step.</param>
    /// <param name="StepIndex">The step index.</param>
    /// <param name="Options">The options to choose from.</param>
    public void Deconstruct(
        out int DayIndex,
        out int StepIndex,
        out IReadOnlyList<NuxGiftOption> Options)
    {
        DayIndex = this.DayIndex;
        StepIndex = this.StepIndex;
        Options = this.Options;
    }
}

/// <summary>
/// Represents the <c>NewUserExperienceGiftOffer</c> message, received with the gifts offered to a new user.
/// </summary>
public sealed record NuxGiftOffer : IParserComposer<NuxGiftOffer>
{
    private IReadOnlyList<NuxGiftStep> _steps = Array.AsReadOnly(Array.Empty<NuxGiftStep>());

    /// <summary>Initializes a new instance of the <see cref="NuxGiftOffer"/> record.</summary>
    /// <param name="Steps">The steps of the offer.</param>
    public NuxGiftOffer(IReadOnlyList<NuxGiftStep> Steps) => this.Steps = Steps;

    /// <summary>Gets the steps of the offer, as a read only copy.</summary>
    /// <remarks>The list may hold at most 4096 entries.</remarks>
    public IReadOnlyList<NuxGiftStep> Steps
    {
        get => _steps;
        init => _steps = CatalogWire.FreezeReferences(
            value,
            GiftWire.MaximumNuxSteps,
            nameof(Steps));
    }

    /// <summary>Parses the message from a packet.</summary>
    /// <param name="p">The packet reader.</param>
    public static NuxGiftOffer Parse(in PacketReader p) =>
        FlashWire.Parse(in p, ParseFlash);

    private static NuxGiftOffer ParseFlash(in PacketReader p) =>
        GiftWire.ParseNuxOffer(in p);

    /// <summary>Composes the message into a packet.</summary>
    /// <param name="p">The packet writer.</param>
    public void Compose(in PacketWriter p) =>
        FlashWire.Compose(this, in p, ComposeFlash);

    private static void ComposeFlash(NuxGiftOffer value, in PacketWriter p) =>
        GiftWire.ComposeNuxOffer(value, in p);

    /// <summary>Deconstructs the offer into its steps.</summary>
    /// <param name="Steps">The steps of the offer.</param>
    public void Deconstruct(out IReadOnlyList<NuxGiftStep> Steps) => Steps = this.Steps;
}

/// <summary>Represents the gift chosen at one step of the new user gift offer.</summary>
/// <param name="DayIndex">The day index of the step.</param>
/// <param name="StepIndex">The step index.</param>
/// <param name="GiftIndex">The zero based index of the chosen option within the step.</param>
public readonly record struct NuxGiftSelection(
    int DayIndex,
    int StepIndex,
    int GiftIndex) : IParserComposer<NuxGiftSelection>
{
    /// <summary>Parses a new user gift selection from a packet.</summary>
    /// <param name="p">The packet reader.</param>
    public static NuxGiftSelection Parse(in PacketReader p) =>
        FlashWire.Parse(in p, ParseFlash);

    private static NuxGiftSelection ParseFlash(in PacketReader p) =>
        new(p.ReadInt(), p.ReadInt(), p.ReadInt());

    /// <summary>Composes the new user gift selection into a packet.</summary>
    /// <param name="p">The packet writer.</param>
    public void Compose(in PacketWriter p) =>
        FlashWire.Compose(this, in p, ComposeFlash);

    private static void ComposeFlash(NuxGiftSelection value, in PacketWriter p) =>
        GiftWire.WriteSelection(value, in p);
}

/// <summary>
/// Represents the <c>NewUserExperienceNotComplete</c> message, received when the account has not finished
/// the new user flow.
/// </summary>
public sealed record NuxNotComplete : IParserComposer<NuxNotComplete>
{
    /// <summary>Parses the message from a packet.</summary>
    /// <param name="p">The packet reader.</param>
    public static NuxNotComplete Parse(in PacketReader p) =>
        FlashWire.Parse(in p, ParseFlash);

    private static NuxNotComplete ParseFlash(in PacketReader p) =>
        GiftWire.ParseEmpty<NuxNotComplete>(in p, static () => new NuxNotComplete());

    /// <summary>Composes the message into a packet.</summary>
    /// <param name="p">The packet writer.</param>
    public void Compose(in PacketWriter p) =>
        FlashWire.Compose(this, in p, ComposeFlash);

    private static void ComposeFlash(NuxNotComplete value, in PacketWriter p) { }
}

/// <summary>
/// Represents the <c>NewUserExperienceGetGifts</c> message, sent to claim gifts from the new user gift offer.
/// </summary>
/// <remarks>
/// The wire count covers every value, three per selection, so parsing fails when it is not a multiple of
/// three.
/// </remarks>
public sealed record NuxGetGifts : IParserComposer<NuxGetGifts>
{
    private IReadOnlyList<NuxGiftSelection> _selections =
        Array.AsReadOnly(Array.Empty<NuxGiftSelection>());

    /// <summary>Initializes a new instance of the <see cref="NuxGetGifts"/> record.</summary>
    /// <param name="Selections">The chosen gifts, one per step.</param>
    public NuxGetGifts(IReadOnlyList<NuxGiftSelection> Selections) => this.Selections = Selections;

    /// <summary>Gets the chosen gifts, as a read only copy.</summary>
    /// <remarks>The list may hold at most 21845 entries.</remarks>
    public IReadOnlyList<NuxGiftSelection> Selections
    {
        get => _selections;
        init => _selections = CatalogWire.FreezeValues(
            value,
            GiftWire.MaximumNuxSelections,
            nameof(Selections));
    }

    /// <summary>Parses the message from a packet.</summary>
    /// <param name="p">The packet reader.</param>
    public static NuxGetGifts Parse(in PacketReader p) =>
        FlashWire.Parse(in p, ParseFlash);

    private static NuxGetGifts ParseFlash(in PacketReader p) => GiftWire.ParseNuxGetGifts(in p);

    /// <summary>Composes the message into a packet.</summary>
    /// <param name="p">The packet writer.</param>
    public void Compose(in PacketWriter p) =>
        FlashWire.Compose(this, in p, ComposeFlash);

    private static void ComposeFlash(NuxGetGifts value, in PacketWriter p) =>
        GiftWire.ComposeNuxGetGifts(value, in p);

    /// <summary>Deconstructs the request into its selections.</summary>
    /// <param name="Selections">The chosen gifts, one per step.</param>
    public void Deconstruct(out IReadOnlyList<NuxGiftSelection> Selections) =>
        Selections = this.Selections;
}

/// <summary>Represents the <c>PresentOpen</c> message, sent to open a present in the room.</summary>
/// <param name="FurniId">
/// The room item identifier of the present. Flash sends it as a 32 bit integer, and composing throws
/// when it does not fit.
/// </param>
public sealed record PresentOpen(Id FurniId) : IParserComposer<PresentOpen>
{
    /// <summary>Parses the message from a packet.</summary>
    /// <param name="p">The packet reader.</param>
    public static PresentOpen Parse(in PacketReader p) =>
        FlashWire.Parse(in p, ParseFlash);

    private static PresentOpen ParseFlash(in PacketReader p) =>
        GiftWire.ParsePresentOpen(in p);

    /// <summary>Composes the message into a packet.</summary>
    /// <param name="p">The packet writer.</param>
    public void Compose(in PacketWriter p) =>
        FlashWire.Compose(this, in p, ComposeFlash);

    private static void ComposeFlash(PresentOpen value, in PacketWriter p) =>
        GiftWire.ComposePresentOpen(value, in p);
}

/// <summary>
/// Represents the <c>PurchaseFromCatalogAsGift</c> message, sent to buy a catalog offer as a gift for another user.
/// </summary>
/// <param name="PageId">The catalog page identifier.</param>
/// <param name="OfferId">The catalog offer identifier.</param>
/// <param name="ExtraData">The extra data the offer expects, sent unchanged.</param>
/// <param name="ReceiverName">The name of the user who receives the gift.</param>
/// <param name="GiftMessage">The message attached to the gift.</param>
/// <param name="SpriteId">The sprite identifier of the gift box furni.</param>
/// <param name="BoxType">The gift box type, or 0 with the default box.</param>
/// <param name="RibbonType">The ribbon type, or 0 with the default box.</param>
/// <param name="ShowPurchaserName">Whether the receiver sees who sent the gift.</param>
public sealed record PurchaseFromCatalogAsGift(
    int PageId,
    int OfferId,
    string ExtraData,
    string ReceiverName,
    string GiftMessage,
    int SpriteId,
    int BoxType,
    int RibbonType,
    bool ShowPurchaserName) : IParserComposer<PurchaseFromCatalogAsGift>
{
    /// <summary>Parses the message from a packet.</summary>
    /// <param name="p">The packet reader.</param>
    public static PurchaseFromCatalogAsGift Parse(in PacketReader p) =>
        FlashWire.Parse(in p, ParseFlash);

    private static PurchaseFromCatalogAsGift ParseFlash(in PacketReader p) =>
        GiftWire.ParsePurchase(in p);

    /// <summary>Composes the message into a packet.</summary>
    /// <param name="p">The packet writer.</param>
    public void Compose(in PacketWriter p) =>
        FlashWire.Compose(this, in p, ComposeFlash);

    private static void ComposeFlash(PurchaseFromCatalogAsGift value, in PacketWriter p) =>
        GiftWire.ComposePurchase(value, in p);
}

/// <summary>
/// Represents the <c>GetGiftWrappingConfiguration</c> message, sent to request the gift wrapping options.
/// </summary>
public sealed record GetGiftWrappingConfiguration : IParserComposer<GetGiftWrappingConfiguration>
{
    /// <summary>Parses the message from a packet.</summary>
    /// <param name="p">The packet reader.</param>
    public static GetGiftWrappingConfiguration Parse(in PacketReader p) =>
        FlashWire.Parse(in p, ParseFlash);

    private static GetGiftWrappingConfiguration ParseFlash(in PacketReader p) =>
        GiftWire.ParseEmpty<GetGiftWrappingConfiguration>(
            in p,
            static () => new GetGiftWrappingConfiguration());

    /// <summary>Composes the message into a packet.</summary>
    /// <param name="p">The packet writer.</param>
    public void Compose(in PacketWriter p) =>
        FlashWire.Compose(this, in p, ComposeFlash);

    private static void ComposeFlash(GetGiftWrappingConfiguration value, in PacketWriter p) { }
}

/// <summary>Represents the <c>GetClubGift</c> message, sent to request the club gifts that can be selected.</summary>
public sealed record GetClubGift : IParserComposer<GetClubGift>
{
    /// <summary>Parses the message from a packet.</summary>
    /// <param name="p">The packet reader.</param>
    public static GetClubGift Parse(in PacketReader p) =>
        FlashWire.Parse(in p, ParseFlash);

    private static GetClubGift ParseFlash(in PacketReader p) =>
        GiftWire.ParseEmpty<GetClubGift>(in p, static () => new GetClubGift());

    /// <summary>Composes the message into a packet.</summary>
    /// <param name="p">The packet writer.</param>
    public void Compose(in PacketWriter p) =>
        FlashWire.Compose(this, in p, ComposeFlash);

    private static void ComposeFlash(GetClubGift value, in PacketWriter p) { }
}

/// <summary>Represents the <c>SelectClubGift</c> message, sent to select a club gift.</summary>
/// <param name="ProductCode">The product code of the club gift to select.</param>
public sealed record SelectClubGift(string ProductCode) : IParserComposer<SelectClubGift>
{
    /// <summary>Parses the message from a packet.</summary>
    /// <param name="p">The packet reader.</param>
    public static SelectClubGift Parse(in PacketReader p) =>
        FlashWire.Parse(in p, ParseFlash);

    private static SelectClubGift ParseFlash(in PacketReader p) => GiftWire.ParseSelectClubGift(in p);

    /// <summary>Composes the message into a packet.</summary>
    /// <param name="p">The packet writer.</param>
    public void Compose(in PacketWriter p) =>
        FlashWire.Compose(this, in p, ComposeFlash);

    private static void ComposeFlash(SelectClubGift value, in PacketWriter p) =>
        GiftWire.ComposeSelectClubGift(value, in p);
}

/// <summary>
/// Represents the <c>GetIsOfferGiftable</c> message, sent to request whether a catalog offer can be sent as a gift.
/// </summary>
/// <param name="OfferId">The identifier of the catalog offer.</param>
public sealed record GetIsOfferGiftable(int OfferId) : IParserComposer<GetIsOfferGiftable>
{
    /// <summary>Parses the message from a packet.</summary>
    /// <param name="p">The packet reader.</param>
    public static GetIsOfferGiftable Parse(in PacketReader p) =>
        FlashWire.Parse(in p, ParseFlash);

    private static GetIsOfferGiftable ParseFlash(in PacketReader p) =>
        GiftWire.ParseOfferGiftabilityRequest(in p);

    /// <summary>Composes the message into a packet.</summary>
    /// <param name="p">The packet writer.</param>
    public void Compose(in PacketWriter p) =>
        FlashWire.Compose(this, in p, ComposeFlash);

    private static void ComposeFlash(GetIsOfferGiftable value, in PacketWriter p) =>
        p.WriteInt(value.OfferId);
}

/// <summary>
/// Represents the <c>NewUserExperienceScriptProceed</c> message, sent to advance the new user flow to its next step.
/// </summary>
public sealed record AdvanceNewUserFlowRequest : IParserComposer<AdvanceNewUserFlowRequest>
{
    /// <summary>Parses the message from a packet.</summary>
    /// <param name="p">The packet reader.</param>
    public static AdvanceNewUserFlowRequest Parse(in PacketReader p) =>
        FlashWire.Parse(in p, ParseFlash);

    private static AdvanceNewUserFlowRequest ParseFlash(in PacketReader p) =>
        GiftWire.ParseEmpty<AdvanceNewUserFlowRequest>(
            in p,
            static () => new AdvanceNewUserFlowRequest());

    /// <summary>Composes the message into a packet.</summary>
    /// <param name="p">The packet writer.</param>
    public void Compose(in PacketWriter p) =>
        FlashWire.Compose(this, in p, ComposeFlash);

    private static void ComposeFlash(AdvanceNewUserFlowRequest value, in PacketWriter p) { }
}

internal static class GiftWire
{
    internal const int MaximumCollectionCount = ushort.MaxValue;
    internal const int MaximumNuxSteps = CatalogPageWire.MaximumOffers;
    internal const int MaximumNuxOptions = ushort.MaxValue;
    internal const int MaximumNuxProducts = ushort.MaxValue;
    internal const int MaximumNuxSelections = ushort.MaxValue / 3;

    private const int FlashEligibilityBytes = sizeof(int) + sizeof(byte) + sizeof(int) + sizeof(byte);
    private const int NuxProductMinimumBytes = CatalogWire.StringMinimumBytes * 2;
    private const int NuxOptionMinimumBytes = CatalogWire.StringMinimumBytes + sizeof(int);
    private const int NuxStepMinimumBytes = sizeof(int) * 3;

    public static IReadOnlyList<int> FreezeValues(IReadOnlyList<int> values, string name) =>
        CatalogWire.FreezeValues(values, MaximumCollectionCount, name);

    public static GiftWrappingConfiguration ParseWrappingConfiguration(in PacketReader p)
    {
        bool enabled = p.ReadBool();
        int price = p.ReadInt();
        int count_width = CatalogWire.CountWidth(p.Client);
        int[] stuff_types = ReadIntValues(
            in p,
            checked(count_width * 3),
            nameof(GiftWrappingConfiguration.StuffTypes));
        int[] box_types = ReadIntValues(
            in p,
            checked(count_width * 2),
            nameof(GiftWrappingConfiguration.BoxTypes));
        int[] ribbon_types = ReadIntValues(
            in p,
            count_width,
            nameof(GiftWrappingConfiguration.RibbonTypes));
        int[] default_stuff_types = ReadIntValues(
            in p,
            0,
            nameof(GiftWrappingConfiguration.DefaultStuffTypes));
        CatalogWire.RequireEmpty(in p, nameof(GiftWrappingConfiguration));
        return new GiftWrappingConfiguration(
            enabled,
            price,
            stuff_types,
            box_types,
            ribbon_types,
            default_stuff_types);
    }

    public static void ComposeWrappingConfiguration(
        GiftWrappingConfiguration value,
        in PacketWriter p)
    {
        ArgumentNullException.ThrowIfNull(value);
        int[] stuff_types = SnapshotValues(value.StuffTypes, nameof(value.StuffTypes));
        int[] box_types = SnapshotValues(value.BoxTypes, nameof(value.BoxTypes));
        int[] ribbon_types = SnapshotValues(value.RibbonTypes, nameof(value.RibbonTypes));
        int[] default_stuff_types = SnapshotValues(
            value.DefaultStuffTypes,
            nameof(value.DefaultStuffTypes));

        p.WriteBool(value.IsWrappingEnabled);
        p.WriteInt(value.WrappingPrice);
        WriteIntValues(stuff_types, in p);
        WriteIntValues(box_types, in p);
        WriteIntValues(ribbon_types, in p);
        WriteIntValues(default_stuff_types, in p);
    }

    public static PresentOpened ParsePresentOpened(in PacketReader p)
    {
        var strings = NewStringBudget();
        int id_width = sizeof(int);
        string item_type = strings.Read(
            in p,
            nameof(PresentOpened.ItemType),
            checked(sizeof(int) + CatalogWire.StringMinimumBytes + id_width +
                CatalogWire.StringMinimumBytes + sizeof(byte) + CatalogWire.StringMinimumBytes));
        int class_id = p.ReadInt();
        string product_code = strings.Read(
            in p,
            nameof(PresentOpened.ProductCode),
            checked(id_width + CatalogWire.StringMinimumBytes + sizeof(byte) +
                CatalogWire.StringMinimumBytes));
        Id placed_item_id = ReadFlashId(in p);
        string placed_item_type = strings.Read(
            in p,
            nameof(PresentOpened.PlacedItemType),
            checked(sizeof(byte) + CatalogWire.StringMinimumBytes));
        bool placed_in_room = p.ReadBool();
        string pet_figure = strings.Read(in p, nameof(PresentOpened.PetFigureString));
        CatalogWire.RequireEmpty(in p, nameof(PresentOpened));
        return new PresentOpened(
            item_type,
            class_id,
            product_code,
            placed_item_id,
            placed_item_type,
            placed_in_room,
            pet_figure);
    }

    public static void ComposePresentOpened(PresentOpened value, in PacketWriter p)
    {
        ArgumentNullException.ThrowIfNull(value);
        var strings = NewStringBudget();
        strings.Require(value.ItemType, nameof(value.ItemType), in p);
        strings.Require(value.ProductCode, nameof(value.ProductCode), in p);
        strings.Require(value.PlacedItemType, nameof(value.PlacedItemType), in p);
        strings.Require(value.PetFigureString, nameof(value.PetFigureString), in p);
        RequireFlashId(value.PlacedItemId);

        p.WriteString(value.ItemType);
        p.WriteInt(value.ClassId);
        p.WriteString(value.ProductCode);
        WriteFlashId(in p, value.PlacedItemId);
        p.WriteString(value.PlacedItemType);
        p.WriteBool(value.PlacedInRoom);
        p.WriteString(value.PetFigureString);
    }

    public static ClubGiftEligibility ParseEligibility(in PacketReader p) => new ClubGiftEligibility(p.ReadInt(), p.ReadBool(), p.ReadInt(), p.ReadBool());

    public static void ComposeEligibility(ClubGiftEligibility value, in PacketWriter p)
    {
        PrepareEligibility(value);
        WriteEligibility(value, in p);
    }

    public static ClubGiftInfo ParseClubGiftInfo(in PacketReader p)
    {
        int days_until_next_gift = p.ReadInt();
        int gifts_available = p.ReadInt();
        int count_width = CatalogWire.CountWidth(p.Client);
        int minimum_offer_bytes = CatalogPageWire.MinimumOfferBytes();
        var catalog_budget = new CatalogPageBudget();
        var strings = NewStringBudget();
        int offer_count = CatalogWire.ReadCount(
            in p,
            minimum_offer_bytes,
            count_width,
            CatalogPageWire.MaximumOffers,
            nameof(ClubGiftInfo.Offers));
        catalog_budget.TakeOffers(offer_count);
        var offers = new CatalogPageOffer[offer_count];
        for (int index = 0; index < offers.Length; index++)
        {
            int sibling_bytes = checked((offers.Length - index - 1) * minimum_offer_bytes);
            offers[index] = CatalogPageWire.ParseOffer(
                in p,
                checked(count_width + sibling_bytes),
                ref catalog_budget,
                ref strings);
        }

        int eligibility_bytes = FlashEligibilityBytes;
        int eligibility_count = CatalogWire.ReadCount(
            in p,
            eligibility_bytes,
            0,
            MaximumCollectionCount,
            nameof(ClubGiftInfo.GiftEligibility));
        var eligibility = new ClubGiftEligibility[eligibility_count];
        for (int index = 0; index < eligibility.Length; index++)
            eligibility[index] = ParseEligibility(in p);
        CatalogWire.RequireEmpty(in p, nameof(ClubGiftInfo));
        return new ClubGiftInfo(days_until_next_gift, gifts_available, offers, eligibility);
    }

    public static void ComposeClubGiftInfo(ClubGiftInfo value, in PacketWriter p)
    {
        ArgumentNullException.ThrowIfNull(value);
        CatalogPageOffer[] offers = CatalogWire.SnapshotReferences(
            value.Offers,
            CatalogPageWire.MaximumOffers,
            nameof(value.Offers));
        ClubGiftEligibility[] eligibility = CatalogWire.SnapshotReferences(
            value.GiftEligibility,
            MaximumCollectionCount,
            nameof(value.GiftEligibility));
        var catalog_budget = new CatalogPageBudget();
        catalog_budget.TakeOffers(offers.Length);
        var strings = NewStringBudget();
        for (int index = 0; index < offers.Length; index++)
        {
            offers[index] = CatalogPageWire.PrepareOffer(
                offers[index],
                true,
                ref catalog_budget,
                ref strings,
                in p);
        }
        foreach (ClubGiftEligibility item in eligibility)
            PrepareEligibility(item);

        p.WriteInt(value.DaysUntilNextGift);
        p.WriteInt(value.GiftsAvailable);
        CatalogWire.WriteCount(offers.Length, in p);
        foreach (CatalogPageOffer offer in offers)
            CatalogPageWire.WriteOffer(offer, in p);
        CatalogWire.WriteCount(eligibility.Length, in p);
        foreach (ClubGiftEligibility item in eligibility)
            WriteEligibility(item, in p);
    }

    public static ClubGiftSelected ParseClubGiftSelected(in PacketReader p)
    {
        var strings = NewStringBudget();
        int count_width = CatalogWire.CountWidth(p.Client);
        string product_code = strings.Read(
            in p,
            nameof(ClubGiftSelected.ProductCode),
            count_width);
        int minimum_product_bytes = CatalogPageWire.FlashProductMinimumBytes;
        int product_count = CatalogWire.ReadCount(
            in p,
            minimum_product_bytes,
            0,
            CatalogPageWire.MaximumProducts,
            nameof(ClubGiftSelected.Products));
        var catalog_budget = new CatalogPageBudget();
        catalog_budget.TakeProducts(product_count);
        var products = new CatalogProduct[product_count];
        for (int index = 0; index < products.Length; index++)
        {
            int sibling_bytes = checked((products.Length - index - 1) * minimum_product_bytes);
            {
                products[index] = CatalogPageWire.ParseFlashProduct(
                    in p,
                    sibling_bytes,
                    ref strings);
            }
        }
        CatalogWire.RequireEmpty(in p, nameof(ClubGiftSelected));
        return new ClubGiftSelected(product_code, products);
    }

    public static void ComposeClubGiftSelected(
        ClubGiftSelected value,
        in PacketWriter p)
    {
        ArgumentNullException.ThrowIfNull(value);
        var strings = NewStringBudget();
        strings.Require(value.ProductCode, nameof(value.ProductCode), in p);
        CatalogProduct[] products = CatalogWire.SnapshotReferences(
            value.Products,
            CatalogPageWire.MaximumProducts,
            nameof(value.Products));
        var catalog_budget = new CatalogPageBudget();
        catalog_budget.TakeProducts(products.Length);
        {
            foreach (CatalogProduct product in products)
                CatalogPageWire.PrepareFlashProduct(product, true, ref strings, in p);
        }

        p.WriteString(value.ProductCode);
        CatalogWire.WriteCount(products.Length, in p);
        {
            foreach (CatalogProduct product in products)
                CatalogPageWire.WriteFlashProduct(product, in p);
        }
    }

    public static NuxGiftProduct ParseNuxProduct(in PacketReader p)
    {
        var strings = NewStringBudget();
        return ParseNuxProduct(in p, 0, ref strings);
    }

    public static void ComposeNuxProduct(NuxGiftProduct value, in PacketWriter p)
    {
        var strings = NewStringBudget();
        PrepareNuxProduct(value, ref strings, in p);
        WriteNuxProduct(value, in p);
    }

    public static NuxGiftOption ParseNuxOption(in PacketReader p)
    {
        var budget = new GiftBudget();
        budget.TakeOptions(1);
        var strings = NewStringBudget();
        return ParseNuxOption(in p, 0, ref budget, ref strings);
    }

    public static void ComposeNuxOption(NuxGiftOption value, in PacketWriter p)
    {
        var budget = new GiftBudget();
        budget.TakeOptions(1);
        var strings = NewStringBudget();
        PrepareNuxOption(value, ref budget, ref strings, in p);
        WriteNuxOption(value, in p);
    }

    public static NuxGiftStep ParseNuxStep(in PacketReader p)
    {
        var budget = new GiftBudget();
        budget.TakeSteps(1);
        var strings = NewStringBudget();
        return ParseNuxStep(in p, 0, ref budget, ref strings);
    }

    public static void ComposeNuxStep(NuxGiftStep value, in PacketWriter p)
    {
        var budget = new GiftBudget();
        budget.TakeSteps(1);
        var strings = NewStringBudget();
        PrepareNuxStep(value, ref budget, ref strings, in p);
        WriteNuxStep(value, in p);
    }

    public static NuxGiftOffer ParseNuxOffer(in PacketReader p)
    {
        var budget = new GiftBudget();
        var strings = NewStringBudget();
        int step_count = CatalogWire.ReadCount(
            in p,
            NuxStepMinimumBytes,
            0,
            MaximumNuxSteps,
            nameof(NuxGiftOffer.Steps));
        budget.TakeSteps(step_count);
        var steps = new NuxGiftStep[step_count];
        for (int index = 0; index < steps.Length; index++)
        {
            int sibling_bytes = checked((steps.Length - index - 1) * NuxStepMinimumBytes);
            steps[index] = ParseNuxStep(in p, sibling_bytes, ref budget, ref strings);
        }
        CatalogWire.RequireEmpty(in p, nameof(NuxGiftOffer));
        return new NuxGiftOffer(steps);
    }

    public static void ComposeNuxOffer(NuxGiftOffer value, in PacketWriter p)
    {
        ArgumentNullException.ThrowIfNull(value);
        int step_count = CatalogWire.RequireListCount(
            value.Steps,
            MaximumNuxSteps,
            nameof(value.Steps));
        var budget = new GiftBudget();
        budget.TakeSteps(step_count);
        var strings = NewStringBudget();
        foreach (NuxGiftStep step in value.Steps)
            PrepareNuxStep(step, ref budget, ref strings, in p);

        CatalogWire.WriteCount(step_count, in p);
        foreach (NuxGiftStep step in value.Steps)
            WriteNuxStep(step, in p);
    }

    public static NuxGetGifts ParseNuxGetGifts(in PacketReader p)
    {
        int value_count = CatalogWire.ReadCount(
            in p,
            sizeof(int),
            0,
            MaximumCollectionCount,
            nameof(NuxGetGifts.Selections));
        if (value_count % 3 != 0)
            throw new InvalidDataException("NUX gift selections must contain complete day, step and gift triples.");
        var selections = new NuxGiftSelection[value_count / 3];
        for (int index = 0; index < selections.Length; index++)
            selections[index] = new NuxGiftSelection(p.ReadInt(), p.ReadInt(), p.ReadInt());
        CatalogWire.RequireEmpty(in p, nameof(NuxGetGifts));
        return new NuxGetGifts(selections);
    }

    public static void ComposeNuxGetGifts(NuxGetGifts value, in PacketWriter p)
    {
        ArgumentNullException.ThrowIfNull(value);
        NuxGiftSelection[] selections = CatalogWire.SnapshotValues(
            value.Selections,
            MaximumNuxSelections,
            nameof(value.Selections));
        int value_count = checked(selections.Length * 3);
        CatalogWire.RequireCount(value_count, MaximumCollectionCount, nameof(value.Selections));

        CatalogWire.WriteCount(value_count, in p);
        foreach (NuxGiftSelection selection in selections)
            WriteSelection(selection, in p);
    }

    public static PresentOpen ParsePresentOpen(in PacketReader p)
    {
        var value = new PresentOpen(ReadFlashId(in p));
        CatalogWire.RequireEmpty(in p, nameof(PresentOpen));
        return value;
    }

    public static void ComposePresentOpen(PresentOpen value, in PacketWriter p)
    {
        ArgumentNullException.ThrowIfNull(value);
        RequireFlashId(value.FurniId);
        WriteFlashId(in p, value.FurniId);
    }

    public static PurchaseFromCatalogAsGift ParsePurchase(in PacketReader p)
    {
        var strings = NewStringBudget();
        int page_id = p.ReadInt();
        int offer_id = p.ReadInt();
        string extra_data = strings.Read(
            in p,
            nameof(PurchaseFromCatalogAsGift.ExtraData),
            checked(CatalogWire.StringMinimumBytes * 2 + sizeof(int) * 3 + sizeof(byte)));
        string receiver_name = strings.Read(
            in p,
            nameof(PurchaseFromCatalogAsGift.ReceiverName),
            checked(CatalogWire.StringMinimumBytes + sizeof(int) * 3 + sizeof(byte)));
        string gift_message = strings.Read(
            in p,
            nameof(PurchaseFromCatalogAsGift.GiftMessage),
            checked(sizeof(int) * 3 + sizeof(byte)));
        var value = new PurchaseFromCatalogAsGift(
            page_id,
            offer_id,
            extra_data,
            receiver_name,
            gift_message,
            p.ReadInt(),
            p.ReadInt(),
            p.ReadInt(),
            p.ReadBool());
        CatalogWire.RequireEmpty(in p, nameof(PurchaseFromCatalogAsGift));
        return value;
    }

    public static void ComposePurchase(
        PurchaseFromCatalogAsGift value,
        in PacketWriter p)
    {
        ArgumentNullException.ThrowIfNull(value);
        var strings = NewStringBudget();
        strings.Require(value.ExtraData, nameof(value.ExtraData), in p);
        strings.Require(value.ReceiverName, nameof(value.ReceiverName), in p);
        strings.Require(value.GiftMessage, nameof(value.GiftMessage), in p);

        p.WriteInt(value.PageId);
        p.WriteInt(value.OfferId);
        p.WriteString(value.ExtraData);
        p.WriteString(value.ReceiverName);
        p.WriteString(value.GiftMessage);
        p.WriteInt(value.SpriteId);
        p.WriteInt(value.BoxType);
        p.WriteInt(value.RibbonType);
        p.WriteBool(value.ShowPurchaserName);
    }

    public static SelectClubGift ParseSelectClubGift(in PacketReader p)
    {
        var strings = NewStringBudget();
        string product_code = strings.Read(in p, nameof(SelectClubGift.ProductCode));
        CatalogWire.RequireEmpty(in p, nameof(SelectClubGift));
        return new SelectClubGift(product_code);
    }

    public static void ComposeSelectClubGift(SelectClubGift value, in PacketWriter p)
    {
        ArgumentNullException.ThrowIfNull(value);
        var strings = NewStringBudget();
        strings.Require(value.ProductCode, nameof(value.ProductCode), in p);
        p.WriteString(value.ProductCode);
    }

    public static GetIsOfferGiftable ParseOfferGiftabilityRequest(in PacketReader p)
    {
        var value = new GetIsOfferGiftable(p.ReadInt());
        CatalogWire.RequireEmpty(in p, nameof(GetIsOfferGiftable));
        return value;
    }

    public static T ParseEmpty<T>(in PacketReader p, Func<T> factory)
    {
        CatalogWire.RequireEmpty(in p, typeof(T).Name);
        return factory();
    }

    public static void WriteSelection(NuxGiftSelection value, in PacketWriter p)
    {
        p.WriteInt(value.DayIndex);
        p.WriteInt(value.StepIndex);
        p.WriteInt(value.GiftIndex);
    }

    private static int[] ReadIntValues(in PacketReader p, int trailing_bytes, string name)
    {
        int count = CatalogWire.ReadCount(
            in p,
            sizeof(int),
            trailing_bytes,
            MaximumCollectionCount,
            name);
        var values = new int[count];
        for (int index = 0; index < values.Length; index++)
            values[index] = p.ReadInt();
        return values;
    }

    private static int[] SnapshotValues(IReadOnlyList<int> values, string name) =>
        CatalogWire.SnapshotValues(values, MaximumCollectionCount, name);

    private static void WriteIntValues(IReadOnlyList<int> values, in PacketWriter p)
    {
        CatalogWire.WriteCount(values.Count, in p);
        foreach (int value in values)
            p.WriteInt(value);
    }

    private static void PrepareEligibility(ClubGiftEligibility value)
    {
        ArgumentNullException.ThrowIfNull(value);
        if (value.IsVip is null)
            throw new InvalidDataException("Flash club gift eligibility requires the VIP flag.");
    }

    private static void WriteEligibility(
        ClubGiftEligibility value,
        in PacketWriter p)
    {
        p.WriteInt(value.OfferId);
        p.WriteBool(value.IsVip!.Value);
        p.WriteInt(value.DaysRequired);
        p.WriteBool(value.IsSelectable);
    }

    private static NuxGiftStep ParseNuxStep(
        in PacketReader p,
        int trailing_bytes,
        ref GiftBudget budget,
        ref CatalogStringBudget strings)
    {
        int day_index = p.ReadInt();
        int step_index = p.ReadInt();
        int option_count = CatalogWire.ReadCount(
            in p,
            NuxOptionMinimumBytes,
            trailing_bytes,
            MaximumNuxOptions,
            nameof(NuxGiftStep.Options));
        budget.TakeOptions(option_count);
        var options = new NuxGiftOption[option_count];
        for (int index = 0; index < options.Length; index++)
        {
            int sibling_bytes = checked((options.Length - index - 1) * NuxOptionMinimumBytes);
            options[index] = ParseNuxOption(
                in p,
                checked(trailing_bytes + sibling_bytes),
                ref budget,
                ref strings);
        }
        return new NuxGiftStep(day_index, step_index, options);
    }

    private static NuxGiftOption ParseNuxOption(
        in PacketReader p,
        int trailing_bytes,
        ref GiftBudget budget,
        ref CatalogStringBudget strings)
    {
        int count_width = CatalogWire.CountWidth(p.Client);
        string thumbnail = strings.Read(
            in p,
            nameof(NuxGiftOption.ThumbnailUrl),
            checked(trailing_bytes + count_width));
        int product_count = CatalogWire.ReadCount(
            in p,
            NuxProductMinimumBytes,
            trailing_bytes,
            MaximumNuxProducts,
            nameof(NuxGiftOption.Products));
        budget.TakeProducts(product_count);
        var products = new NuxGiftProduct[product_count];
        for (int index = 0; index < products.Length; index++)
        {
            int sibling_bytes = checked((products.Length - index - 1) * NuxProductMinimumBytes);
            products[index] = ParseNuxProduct(
                in p,
                checked(trailing_bytes + sibling_bytes),
                ref strings);
        }
        return new NuxGiftOption(thumbnail.Length == 0 ? null : thumbnail, products);
    }

    private static NuxGiftProduct ParseNuxProduct(
        in PacketReader p,
        int trailing_bytes,
        ref CatalogStringBudget strings)
    {
        string product_code = strings.Read(
            in p,
            nameof(NuxGiftProduct.ProductCode),
            checked(trailing_bytes + CatalogWire.StringMinimumBytes));
        string localization_key = strings.Read(
            in p,
            nameof(NuxGiftProduct.LocalizationKey),
            trailing_bytes);
        return new NuxGiftProduct(
            product_code,
            localization_key.Length == 0 ? null : localization_key);
    }

    private static void PrepareNuxStep(
        NuxGiftStep value,
        ref GiftBudget budget,
        ref CatalogStringBudget strings,
        in PacketWriter p)
    {
        ArgumentNullException.ThrowIfNull(value);
        int option_count = CatalogWire.RequireListCount(
            value.Options,
            MaximumNuxOptions,
            nameof(value.Options));
        budget.TakeOptions(option_count);
        foreach (NuxGiftOption option in value.Options)
            PrepareNuxOption(option, ref budget, ref strings, in p);
    }

    private static void PrepareNuxOption(
        NuxGiftOption value,
        ref GiftBudget budget,
        ref CatalogStringBudget strings,
        in PacketWriter p)
    {
        ArgumentNullException.ThrowIfNull(value);
        strings.Require(value.ThumbnailUrl ?? "", nameof(value.ThumbnailUrl), in p);
        int product_count = CatalogWire.RequireListCount(
            value.Products,
            MaximumNuxProducts,
            nameof(value.Products));
        budget.TakeProducts(product_count);
        foreach (NuxGiftProduct product in value.Products)
            PrepareNuxProduct(product, ref strings, in p);
    }

    private static void PrepareNuxProduct(
        NuxGiftProduct value,
        ref CatalogStringBudget strings,
        in PacketWriter p)
    {
        ArgumentNullException.ThrowIfNull(value);
        strings.Require(value.ProductCode, nameof(value.ProductCode), in p);
        strings.Require(value.LocalizationKey ?? "", nameof(value.LocalizationKey), in p);
    }

    private static void WriteNuxStep(NuxGiftStep value, in PacketWriter p)
    {
        p.WriteInt(value.DayIndex);
        p.WriteInt(value.StepIndex);
        CatalogWire.WriteCount(value.Options.Count, in p);
        foreach (NuxGiftOption option in value.Options)
            WriteNuxOption(option, in p);
    }

    private static void WriteNuxOption(NuxGiftOption value, in PacketWriter p)
    {
        p.WriteString(value.ThumbnailUrl ?? "");
        CatalogWire.WriteCount(value.Products.Count, in p);
        foreach (NuxGiftProduct product in value.Products)
            WriteNuxProduct(product, in p);
    }

    private static void WriteNuxProduct(NuxGiftProduct value, in PacketWriter p)
    {
        p.WriteString(value.ProductCode);
        p.WriteString(value.LocalizationKey ?? "");
    }

    private static CatalogStringBudget NewStringBudget() =>
        CatalogPageWire.NewStringBudget();

    private static Id ReadFlashId(in PacketReader p) => p.ReadInt();

    private static void RequireFlashId(Id value)
    {
        long id = value;
        if (id is < int.MinValue or > int.MaxValue)
            throw new InvalidDataException(
                "Flash cannot represent a gift furni identifier outside the signed 32-bit range.");
    }

    private static void WriteFlashId(in PacketWriter p, Id value) => p.WriteInt((int)(long)value);
}

internal struct GiftBudget
{
    private int _steps;
    private int _options;
    private int _products;

    public void TakeSteps(int count) =>
        Take(ref _steps, count, GiftWire.MaximumNuxSteps, "NUX gift steps");

    public void TakeOptions(int count) =>
        Take(ref _options, count, GiftWire.MaximumNuxOptions, "NUX gift options");

    public void TakeProducts(int count) =>
        Take(ref _products, count, GiftWire.MaximumNuxProducts, "NUX gift products");

    private static void Take(ref int current, int count, int maximum, string name)
    {
        CatalogWire.RequireCount(count, maximum, name);
        if (count > maximum - current)
            throw new InvalidDataException($"{name} exceed the global limit {maximum}.");
        current += count;
    }
}

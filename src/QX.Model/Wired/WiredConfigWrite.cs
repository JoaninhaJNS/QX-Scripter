using Qx.Messages;

namespace Qx.Model.Wired;

/// <summary>Represents the configuration of a wired furni item as the client sends it when saving.</summary>
/// <remarks>On the wire the fields follow the order <see cref="WiredConfigWrite.FurniId"/>, <see cref="WiredConfigWrite.IntParams"/>, <see cref="WiredConfigWrite.StringParam"/>, <see cref="WiredConfigWrite.StuffIds"/>, the fields a derived message adds, <see cref="WiredConfigWrite.FurniSourceTypes"/>, <see cref="WiredConfigWrite.UserSourceTypes"/>, <see cref="WiredConfigWrite.VariableIds"/> and <see cref="WiredConfigWrite.StuffIds2"/>.</remarks>
public abstract record WiredConfigWrite : IComposer
{
    /// <summary>Gets or sets the id of the wired furni item, written as a 32 bit integer.</summary>
    public Id FurniId { get; set; }
    /// <summary>Gets or sets the integer parameters.</summary>
    public IReadOnlyList<int> IntParams { get; set; } = [];
    /// <summary>Gets or sets the string parameter, with multiple values separated by tabs.</summary>
    public string StringParam { get; set; } = "";
    /// <summary>Gets or sets the ids of the selected furni.</summary>
    public IReadOnlyList<Id> StuffIds { get; set; } = [];
    /// <summary>Gets or sets the ids of the furni in the second selection.</summary>
    public IReadOnlyList<Id> StuffIds2 { get; set; } = [];
    /// <summary>Gets or sets the selected furni source types.</summary>
    public IReadOnlyList<int> FurniSourceTypes { get; set; } = [];
    /// <summary>Gets or sets the selected user source types.</summary>
    public IReadOnlyList<int> UserSourceTypes { get; set; } = [];
    /// <summary>Gets or sets the ids of the variables the configuration references.</summary>
    public IReadOnlyList<string> VariableIds { get; set; } = [];

    /// <summary>Composes the message into a packet.</summary>
    /// <param name="p">The packet writer.</param>
    public abstract void Compose(in PacketWriter p);

    /// <summary>Reads every field from a Flash packet into the properties.</summary>
    /// <param name="p">The packet reader.</param>
    protected void ReadFlash(in PacketReader p)
    {
        FurniId = p.ReadInt();
        IntParams = p.ReadIntArray();
        StringParam = p.ReadString();
        StuffIds = p.ReadIdArray();
        ReadExtra(in p);
        FurniSourceTypes = p.ReadIntArray();
        UserSourceTypes = p.ReadIntArray();
        VariableIds = p.ReadStringArray();
        StuffIds2 = p.ReadIdArray();
    }

    /// <summary>Validates the properties and writes every field to a Flash packet.</summary>
    /// <param name="p">The packet writer.</param>
    protected void ComposeFlash(in PacketWriter p)
    {
        ValidateFlash(in p);
        p.WriteInt(WiredWire.FlashId(FurniId));
        p.WriteIntArray(IntParams);
        p.WriteString(StringParam);
        p.WriteIdArray(StuffIds);
        WriteExtra(in p);
        p.WriteIntArray(FurniSourceTypes);
        p.WriteIntArray(UserSourceTypes);
        p.WriteStringArray(VariableIds);
        p.WriteIdArray(StuffIds2);
    }

    /// <summary>Reads the fields a derived message adds after <see cref="StuffIds"/>.</summary>
    /// <param name="p">The packet reader.</param>
    protected virtual void ReadExtra(in PacketReader p) { }

    /// <summary>Writes the fields a derived message adds after <see cref="StuffIds"/>.</summary>
    /// <param name="p">The packet writer.</param>
    protected virtual void WriteExtra(in PacketWriter p) { }

    private void ValidateFlash(in PacketWriter p)
    {
        ValidateCommon(in p);
        _ = WiredWire.FlashId(FurniId);
        foreach (Id id in StuffIds)
            _ = WiredWire.FlashId(id);
        foreach (Id id in StuffIds2)
            _ = WiredWire.FlashId(id);
        foreach (string variable_id in VariableIds)
            WiredWire.RequireString(variable_id, nameof(VariableIds), in p);
    }

    private void ValidateCommon(in PacketWriter p)
    {
        ArgumentNullException.ThrowIfNull(IntParams);
        ArgumentNullException.ThrowIfNull(StringParam);
        ArgumentNullException.ThrowIfNull(StuffIds);
        ArgumentNullException.ThrowIfNull(StuffIds2);
        ArgumentNullException.ThrowIfNull(FurniSourceTypes);
        ArgumentNullException.ThrowIfNull(UserSourceTypes);
        ArgumentNullException.ThrowIfNull(VariableIds);
        WiredWire.RequireString(StringParam, nameof(StringParam), in p);
    }
}

/// <summary>Sent when the user saves the configuration of a wired trigger.</summary>
/// <remarks>Sent as the Flash <c>UpdateTrigger</c> message. The save replaces the whole configuration, and the hotel answers with <see cref="WiredSaveSuccess"/> or <see cref="WiredValidationError"/>.</remarks>
public sealed record UpdateTrigger : WiredConfigWrite, IParserComposer<UpdateTrigger>
{
    /// <summary>Parses the message from a packet.</summary>
    /// <param name="p">The packet reader.</param>
    public static UpdateTrigger Parse(in PacketReader p) =>
        FlashWire.Parse(in p, ParseFlash);

    private static UpdateTrigger ParseFlash(in PacketReader p)
    {
        var value = new UpdateTrigger();
        value.ReadFlash(in p);
        return value;
    }

    /// <summary>Composes the message into a packet.</summary>
    /// <param name="p">The packet writer.</param>
    public override void Compose(in PacketWriter p) =>
        FlashWire.Compose(this, in p, ComposeFlash);

    private static void ComposeFlash(UpdateTrigger value, in PacketWriter p) =>
        value.ComposeFlash(in p);
}

/// <summary>Sent when the user saves the configuration of a wired action, also called an effect.</summary>
/// <remarks>Sent as the Flash <c>UpdateAction</c> message. The save replaces the whole configuration, and the hotel answers with <see cref="WiredSaveSuccess"/> or <see cref="WiredValidationError"/>.</remarks>
public sealed record UpdateAction : WiredConfigWrite, IParserComposer<UpdateAction>
{
    /// <summary>Gets or sets the delay of the action in pulses.</summary>
    public int Delay { get; set; }

    /// <summary>Parses the message from a packet.</summary>
    /// <param name="p">The packet reader.</param>
    public static UpdateAction Parse(in PacketReader p) =>
        FlashWire.Parse(in p, ParseFlash);

    private static UpdateAction ParseFlash(in PacketReader p)
    {
        var value = new UpdateAction();
        value.ReadFlash(in p);
        return value;
    }

    /// <summary>Composes the message into a packet.</summary>
    /// <param name="p">The packet writer.</param>
    public override void Compose(in PacketWriter p) =>
        FlashWire.Compose(this, in p, ComposeFlash);

    /// <summary>Reads <see cref="Delay"/>.</summary>
    /// <param name="p">The packet reader.</param>
    protected override void ReadExtra(in PacketReader p) => Delay = p.ReadInt();

    /// <summary>Writes <see cref="Delay"/>.</summary>
    /// <param name="p">The packet writer.</param>
    protected override void WriteExtra(in PacketWriter p) => p.WriteInt(Delay);

    private static void ComposeFlash(UpdateAction value, in PacketWriter p) =>
        value.ComposeFlash(in p);
}

/// <summary>Sent when the user saves the configuration of a wired condition.</summary>
/// <remarks>Sent as the Flash <c>UpdateCondition</c> message. The save replaces the whole configuration, and the hotel answers with <see cref="WiredSaveSuccess"/> or <see cref="WiredValidationError"/>.</remarks>
public sealed record UpdateCondition : WiredConfigWrite, IParserComposer<UpdateCondition>
{
    /// <summary>Gets or sets the quantifier of the condition.</summary>
    public int Quantifier { get; set; }

    /// <summary>Parses the message from a packet.</summary>
    /// <param name="p">The packet reader.</param>
    public static UpdateCondition Parse(in PacketReader p) =>
        FlashWire.Parse(in p, ParseFlash);

    private static UpdateCondition ParseFlash(in PacketReader p)
    {
        var value = new UpdateCondition();
        value.ReadFlash(in p);
        return value;
    }

    /// <summary>Composes the message into a packet.</summary>
    /// <param name="p">The packet writer.</param>
    public override void Compose(in PacketWriter p) =>
        FlashWire.Compose(this, in p, ComposeFlash);

    /// <summary>Reads <see cref="Quantifier"/>.</summary>
    /// <param name="p">The packet reader.</param>
    protected override void ReadExtra(in PacketReader p) => Quantifier = p.ReadInt();

    /// <summary>Writes <see cref="Quantifier"/>.</summary>
    /// <param name="p">The packet writer.</param>
    protected override void WriteExtra(in PacketWriter p) => p.WriteInt(Quantifier);

    private static void ComposeFlash(UpdateCondition value, in PacketWriter p) =>
        value.ComposeFlash(in p);
}

/// <summary>Sent when the user saves the configuration of a wired add-on.</summary>
/// <remarks>Sent as the Flash <c>UpdateAddon</c> message. The save replaces the whole configuration, and the hotel answers with <see cref="WiredSaveSuccess"/> or <see cref="WiredValidationError"/>.</remarks>
public sealed record UpdateAddon : WiredConfigWrite, IParserComposer<UpdateAddon>
{
    /// <summary>Parses the message from a packet.</summary>
    /// <param name="p">The packet reader.</param>
    public static UpdateAddon Parse(in PacketReader p) =>
        FlashWire.Parse(in p, ParseFlash);

    private static UpdateAddon ParseFlash(in PacketReader p)
    {
        var value = new UpdateAddon();
        value.ReadFlash(in p);
        return value;
    }

    /// <summary>Composes the message into a packet.</summary>
    /// <param name="p">The packet writer.</param>
    public override void Compose(in PacketWriter p) =>
        FlashWire.Compose(this, in p, ComposeFlash);

    private static void ComposeFlash(UpdateAddon value, in PacketWriter p) =>
        value.ComposeFlash(in p);
}

/// <summary>Sent when the user saves the configuration of a wired selector.</summary>
/// <remarks>Sent as the Flash <c>UpdateSelector</c> message. The save replaces the whole configuration, and the hotel answers with <see cref="WiredSaveSuccess"/> or <see cref="WiredValidationError"/>.</remarks>
public sealed record UpdateSelector : WiredConfigWrite, IParserComposer<UpdateSelector>
{
    /// <summary>Gets or sets whether the selector is a filter.</summary>
    public bool IsFilter { get; set; }
    /// <summary>Gets or sets whether the selector is inverted.</summary>
    public bool IsInvert { get; set; }

    /// <summary>Parses the message from a packet.</summary>
    /// <param name="p">The packet reader.</param>
    public static UpdateSelector Parse(in PacketReader p) =>
        FlashWire.Parse(in p, ParseFlash);

    private static UpdateSelector ParseFlash(in PacketReader p)
    {
        var value = new UpdateSelector();
        value.ReadFlash(in p);
        return value;
    }

    /// <summary>Composes the message into a packet.</summary>
    /// <param name="p">The packet writer.</param>
    public override void Compose(in PacketWriter p) =>
        FlashWire.Compose(this, in p, ComposeFlash);

    /// <summary>Reads <see cref="IsFilter"/>, then <see cref="IsInvert"/>.</summary>
    /// <param name="p">The packet reader.</param>
    protected override void ReadExtra(in PacketReader p)
    {
        IsFilter = p.ReadBool();
        IsInvert = p.ReadBool();
    }

    /// <summary>Writes <see cref="IsFilter"/>, then <see cref="IsInvert"/>.</summary>
    /// <param name="p">The packet writer.</param>
    protected override void WriteExtra(in PacketWriter p)
    {
        p.WriteBool(IsFilter);
        p.WriteBool(IsInvert);
    }

    private static void ComposeFlash(UpdateSelector value, in PacketWriter p) =>
        value.ComposeFlash(in p);
}

/// <summary>Sent when the user saves the configuration of a wired variable furni.</summary>
/// <remarks>Sent as the Flash <c>UpdateVariable</c> message. The save replaces the whole configuration, and the hotel answers with <see cref="WiredSaveSuccess"/> or <see cref="WiredValidationError"/>.</remarks>
public sealed record UpdateVariable : WiredConfigWrite, IParserComposer<UpdateVariable>
{
    /// <summary>Parses the message from a packet.</summary>
    /// <param name="p">The packet reader.</param>
    public static UpdateVariable Parse(in PacketReader p) =>
        FlashWire.Parse(in p, ParseFlash);

    private static UpdateVariable ParseFlash(in PacketReader p)
    {
        var value = new UpdateVariable();
        value.ReadFlash(in p);
        return value;
    }

    /// <summary>Composes the message into a packet.</summary>
    /// <param name="p">The packet writer.</param>
    public override void Compose(in PacketWriter p) =>
        FlashWire.Compose(this, in p, ComposeFlash);

    private static void ComposeFlash(UpdateVariable value, in PacketWriter p) =>
        value.ComposeFlash(in p);
}

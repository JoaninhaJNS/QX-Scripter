using System.Globalization;
using Qx.Messages;
using Qx.Model.Wired;

namespace Qx.Model.Messages.Incoming;

/// <summary>
/// How one wired variable is drawn above avatars or furni, as the Fx bar configuration of the room
/// defines it. <see cref="Extra"/> carries the renderer settings, among them <c>icon</c>.
/// </summary>
public sealed record VariableFxConfigEntry(
    int ConfigId,
    bool IsUserFx,
    int ShowMode,
    int VisibilityMask,
    bool ShowOnMouseHover,
    int ShowDuration,
    int CategoryId,
    int StyleId,
    int ColorId,
    int WidthId,
    int RendererId,
    long DefaultMinValue,
    long DefaultMaxValue,
    IReadOnlyDictionary<string, string> Extra)
{
    /// <summary>The icon the room gave this variable, such as <c>gold</c> or <c>ranch.tomato</c>.</summary>
    public string? Icon => Extra.GetValueOrDefault("icon");

    internal static VariableFxConfigEntry Parse(in PacketReader p) => new(
        p.ReadInt(),
        p.ReadBool(),
        p.ReadInt(),
        p.ReadInt(),
        p.ReadBool(),
        p.ReadInt(),
        p.ReadInt(),
        p.ReadInt(),
        p.ReadInt(),
        p.ReadInt(),
        p.ReadInt(),
        p.ReadLong(),
        p.ReadLong(),
        VariableFxWire.ReadExtra(in p));

    internal void Compose(in PacketWriter p)
    {
        p.WriteInt(ConfigId);
        p.WriteBool(IsUserFx);
        p.WriteInt(ShowMode);
        p.WriteInt(VisibilityMask);
        p.WriteBool(ShowOnMouseHover);
        p.WriteInt(ShowDuration);
        p.WriteInt(CategoryId);
        p.WriteInt(StyleId);
        p.WriteInt(ColorId);
        p.WriteInt(WidthId);
        p.WriteInt(RendererId);
        p.WriteLong(DefaultMinValue);
        p.WriteLong(DefaultMaxValue);
        VariableFxWire.WriteExtra(Extra, in p);
    }
}

/// <summary>
/// The value of one wired variable on one avatar or furni, as shown in the Fx bar. The key reads
/// <c>{configId}|{variableId}</c>; users are identified by their room index, furni by their item id.
/// </summary>
public sealed record VariableFxStatusEntry(
    string Key,
    bool IsInitialize,
    bool IsUserEntity,
    int EntityId,
    long Value,
    long? MinValue,
    long? MaxValue,
    IReadOnlyDictionary<string, string> Extra)
{
    /// <summary>The Fx configuration the value is drawn with.</summary>
    public int ConfigId => VariableFxSlot.ConfigIdOf(Key);

    /// <summary>The wired variable the value belongs to.</summary>
    public string VariableId => VariableFxSlot.VariableIdOf(Key);

    /// <summary>Which avatar or furni and which variable the value belongs to.</summary>
    public VariableFxSlot Slot => new(IsUserEntity, EntityId, ConfigId, VariableId);

    internal static VariableFxStatusEntry Parse(in PacketReader p)
    {
        string key = p.ReadString();
        bool initialize = p.ReadBool();
        bool is_user = p.ReadBool();
        int entity_id = p.ReadInt();
        long value = p.ReadLong();
        long? min_value = null;
        long? max_value = null;
        if (p.ReadBool())
        {
            min_value = p.ReadLong();
            max_value = p.ReadLong();
        }
        return new(key, initialize, is_user, entity_id, value, min_value, max_value, VariableFxWire.ReadExtra(in p));
    }

    internal void Compose(in PacketWriter p)
    {
        p.WriteString(Key);
        p.WriteBool(IsInitialize);
        p.WriteBool(IsUserEntity);
        p.WriteInt(EntityId);
        p.WriteLong(Value);
        bool has_range = MinValue is not null || MaxValue is not null;
        p.WriteBool(has_range);
        if (has_range)
        {
            p.WriteLong(MinValue ?? 0);
            p.WriteLong(MaxValue ?? 0);
        }
        VariableFxWire.WriteExtra(Extra, in p);
    }
}

/// <summary>The Fx bar configurations a room adds or replaces.</summary>
public sealed record VariableFxConfigUpdate(IReadOnlyList<VariableFxConfigEntry> Configs)
    : IParserComposer<VariableFxConfigUpdate>
{
    public static VariableFxConfigUpdate Parse(in PacketReader p) =>
        FlashWire.Parse(in p, ParseFlash);

    private static VariableFxConfigUpdate ParseFlash(in PacketReader p)
    {
        var configs = new VariableFxConfigEntry[VariableFxWire.ReadCount(in p, VariableFxWire.ConfigBytes, nameof(Configs))];
        for (int i = 0; i < configs.Length; i++)
            configs[i] = VariableFxConfigEntry.Parse(in p);
        return new(configs);
    }

    public void Compose(in PacketWriter p) =>
        FlashWire.Compose(this, in p, ComposeFlash);

    private static void ComposeFlash(VariableFxConfigUpdate value, in PacketWriter p)
    {
        p.WriteInt(value.Configs.Count);
        foreach (VariableFxConfigEntry config in value.Configs)
            config.Compose(in p);
    }
}

/// <summary>The Fx bar configurations a room removes.</summary>
public sealed record VariableFxConfigRemoval(IReadOnlyList<int> ConfigIds)
    : IParserComposer<VariableFxConfigRemoval>
{
    public static VariableFxConfigRemoval Parse(in PacketReader p) =>
        FlashWire.Parse(in p, ParseFlash);

    private static VariableFxConfigRemoval ParseFlash(in PacketReader p)
    {
        var ids = new int[VariableFxWire.ReadCount(in p, sizeof(int), nameof(ConfigIds))];
        for (int i = 0; i < ids.Length; i++)
            ids[i] = p.ReadInt();
        return new(ids);
    }

    public void Compose(in PacketWriter p) =>
        FlashWire.Compose(this, in p, ComposeFlash);

    private static void ComposeFlash(VariableFxConfigRemoval value, in PacketWriter p)
    {
        p.WriteInt(value.ConfigIds.Count);
        foreach (int id in value.ConfigIds)
            p.WriteInt(id);
    }
}

/// <summary>
/// Fx bar values that changed. A message marked <see cref="IsInitialize"/> restates values on entry
/// rather than reporting a change, which the client shows without animation.
/// </summary>
public sealed record VariableFxStatusUpdate(bool IsInitialize, IReadOnlyList<VariableFxStatusEntry> Statuses)
    : IParserComposer<VariableFxStatusUpdate>
{
    public static VariableFxStatusUpdate Parse(in PacketReader p) =>
        FlashWire.Parse(in p, ParseFlash);

    private static VariableFxStatusUpdate ParseFlash(in PacketReader p)
    {
        bool is_initialize = p.ReadBool();
        var statuses = new VariableFxStatusEntry[VariableFxWire.ReadCount(in p, VariableFxWire.StatusBytes, nameof(Statuses))];
        for (int i = 0; i < statuses.Length; i++)
            statuses[i] = VariableFxStatusEntry.Parse(in p);
        return new(is_initialize, statuses);
    }

    public void Compose(in PacketWriter p) =>
        FlashWire.Compose(this, in p, ComposeFlash);

    private static void ComposeFlash(VariableFxStatusUpdate value, in PacketWriter p)
    {
        p.WriteBool(value.IsInitialize);
        p.WriteInt(value.Statuses.Count);
        foreach (VariableFxStatusEntry status in value.Statuses)
            status.Compose(in p);
    }
}

/// <summary>
/// Fx bar values that were removed. Each key reads <c>{configId}|{variableId}|{u or f}|{entityId}</c>.
/// </summary>
public sealed record VariableFxStatusRemoval(IReadOnlyList<string> Keys)
    : IParserComposer<VariableFxStatusRemoval>
{
    /// <summary>The removed values, read from the keys the way the client reads them.</summary>
    public IEnumerable<VariableFxSlot> Slots
    {
        get
        {
            foreach (string key in Keys)
            {
                if (VariableFxSlot.TryFromRemovalKey(key, out VariableFxSlot slot))
                    yield return slot;
            }
        }
    }

    public static VariableFxStatusRemoval Parse(in PacketReader p) =>
        FlashWire.Parse(in p, ParseFlash);

    private static VariableFxStatusRemoval ParseFlash(in PacketReader p)
    {
        var keys = new string[VariableFxWire.ReadCount(in p, VariableFxWire.StringBytes, nameof(Keys))];
        for (int i = 0; i < keys.Length; i++)
            keys[i] = p.ReadString();
        return new(keys);
    }

    public void Compose(in PacketWriter p) =>
        FlashWire.Compose(this, in p, ComposeFlash);

    private static void ComposeFlash(VariableFxStatusRemoval value, in PacketWriter p)
    {
        p.WriteInt(value.Keys.Count);
        foreach (string key in value.Keys)
            p.WriteString(key);
    }
}

/// <summary>
/// Which avatar or furni and which wired variable one Fx bar value belongs to, which is how the
/// client files the values of every room object.
/// </summary>
public readonly record struct VariableFxSlot(bool IsUserEntity, int EntityId, int ConfigId, string VariableId)
{
    /// <summary>The config id in a status key: everything before the first separator.</summary>
    public static int ConfigIdOf(string key)
    {
        ArgumentNullException.ThrowIfNull(key);
        int separator = key.IndexOf('|');
        ReadOnlySpan<char> text = separator < 0 ? key : key.AsSpan(0, separator);
        return int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int id) ? id : 0;
    }

    /// <summary>The variable id in a status key: everything after the first separator.</summary>
    public static string VariableIdOf(string key)
    {
        ArgumentNullException.ThrowIfNull(key);
        int separator = key.IndexOf('|');
        return separator < 0 ? "" : key[(separator + 1)..];
    }

    /// <summary>
    /// Reads a removal key: the config id before the first separator, the entity kind and id after
    /// the last two, and the variable id between them.
    /// </summary>
    /// <returns><see langword="false"/> when the key does not have those four parts.</returns>
    public static bool TryFromRemovalKey(string key, out VariableFxSlot slot)
    {
        ArgumentNullException.ThrowIfNull(key);
        slot = default;
        int first = key.IndexOf('|');
        int last = key.LastIndexOf('|');
        int kind = last > 0 ? key.LastIndexOf('|', last - 1) : -1;
        if (first < 0 || kind <= first)
            return false;
        slot = new(
            key.AsSpan(kind + 1, last - kind - 1).SequenceEqual("u"),
            int.TryParse(key.AsSpan(last + 1), NumberStyles.Integer, CultureInfo.InvariantCulture, out int entity) ? entity : 0,
            ConfigIdOf(key[..first]),
            key[(first + 1)..kind]);
        return true;
    }
}

static class VariableFxWire
{
    public const int StringBytes = sizeof(short);
    public const int ExtraBytes = StringBytes * 2;
    public const int ConfigBytes = sizeof(int) * 9 + sizeof(bool) * 2 + sizeof(long) * 2 + sizeof(int);
    public const int StatusBytes = StringBytes + sizeof(bool) * 3 + sizeof(int) + sizeof(long) + sizeof(int);

    public static int ReadCount(in PacketReader p, int minimum_bytes, string name)
    {
        int count = p.ReadInt();
        WiredWire.RequireBoundedCount(count, p.Available, minimum_bytes, name);
        return count;
    }

    public static IReadOnlyDictionary<string, string> ReadExtra(in PacketReader p)
    {
        int count = ReadCount(in p, ExtraBytes, nameof(VariableFxStatusEntry.Extra));
        var extra = new Dictionary<string, string>(count, StringComparer.Ordinal);
        for (int i = 0; i < count; i++)
        {
            string key = p.ReadString();
            extra[key] = p.ReadString();
        }
        return extra;
    }

    public static void WriteExtra(IReadOnlyDictionary<string, string> extra, in PacketWriter p)
    {
        p.WriteInt(extra.Count);
        foreach ((string key, string value) in extra)
        {
            p.WriteString(key);
            p.WriteString(value);
        }
    }
}

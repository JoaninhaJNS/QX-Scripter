using Qx.Messages;

namespace Qx.Model.Messages.Incoming;

/// <summary>Represents the <c>CameraStorageUrl</c> message, received with the storage URL of a camera photo.</summary>
/// <param name="Url">The URL of the stored photo.</param>
public sealed record CameraStorageUrl(string Url) : IParserComposer<CameraStorageUrl>
{
    /// <summary>Parses the message from a packet.</summary>
    /// <param name="p">The packet reader.</param>
    public static CameraStorageUrl Parse(in PacketReader p)
    {
        RequireFlash(p.Client);
        return new CameraStorageUrl(p.ReadString());
    }

    /// <summary>Composes the message into a packet.</summary>
    /// <param name="p">The packet writer.</param>
    public void Compose(in PacketWriter p)
    {
        RequireFlash(p.Client);
        p.WriteString(Url);
    }

    private static void RequireFlash(ClientType client)
    {
    }
}

/// <summary>
/// Represents the <c>CameraPublishStatus</c> message, received with the result of publishing a camera photo.
/// </summary>
/// <param name="IsOk">Whether the photo was published.</param>
/// <param name="SecondsToWait">The number of seconds to wait before publishing again.</param>
/// <param name="ExtraDataId">
/// The extra data identifier of the published photo, or <see langword="null"/> when the publish failed or the
/// message does not carry one.
/// </param>
public sealed record CameraPublishStatus(bool IsOk, int SecondsToWait, string? ExtraDataId)
    : IParserComposer<CameraPublishStatus>
{
    /// <summary>Parses the message from a packet.</summary>
    /// <param name="p">The packet reader.</param>
    public static CameraPublishStatus Parse(in PacketReader p)
    {
        RequireFlash(p.Client);
        bool is_ok = p.ReadBool();
        int seconds_to_wait = p.ReadInt();
        string? extra_data_id = is_ok && p.Available > 0 ? p.ReadString() : null;
        return new CameraPublishStatus(is_ok, seconds_to_wait, extra_data_id);
    }

    /// <summary>Composes the message into a packet.</summary>
    /// <param name="p">The packet writer.</param>
    /// <exception cref="InvalidDataException">
    /// Thrown when <see cref="IsOk"/> is <see langword="false"/> and <see cref="ExtraDataId"/> is set.
    /// </exception>
    public void Compose(in PacketWriter p)
    {
        RequireFlash(p.Client);
        if (!IsOk && ExtraDataId is not null)
            throw new InvalidDataException("Failed camera publish status cannot contain an extra data id.");
        p.WriteBool(IsOk);
        p.WriteInt(SecondsToWait);
        if (ExtraDataId is not null)
            p.WriteString(ExtraDataId);
    }

    private static void RequireFlash(ClientType client)
    {
    }
}

/// <summary>Represents the <c>CameraPurchaseOk</c> message, received when a camera photo purchase succeeds.</summary>
/// <remarks>The message has no payload.</remarks>
public sealed record CameraPurchaseOk : IParserComposer<CameraPurchaseOk>
{
    /// <summary>Parses the message from a packet.</summary>
    /// <param name="p">The packet reader.</param>
    public static CameraPurchaseOk Parse(in PacketReader p)
    {
        RequireFlash(p.Client);
        return new CameraPurchaseOk();
    }

    /// <summary>Composes the message into a packet.</summary>
    /// <param name="p">The packet writer.</param>
    public void Compose(in PacketWriter p) => RequireFlash(p.Client);

    private static void RequireFlash(ClientType client)
    {
    }
}

/// <summary>Represents the <c>InitCamera</c> message, received with the prices of the camera.</summary>
/// <param name="CreditPrice">The price of a photo in credits.</param>
/// <param name="DucketPrice">The price of a photo in duckets.</param>
/// <param name="PublishDucketPrice">
/// The price of publishing a photo in duckets, or <see langword="null"/> when the message does not carry it.
/// </param>
public sealed record InitCamera(int CreditPrice, int DucketPrice, int? PublishDucketPrice)
    : IParserComposer<InitCamera>
{
    /// <summary>Parses the message from a packet.</summary>
    /// <param name="p">The packet reader.</param>
    public static InitCamera Parse(in PacketReader p)
    {
        RequireFlash(p.Client);
        int credit_price = p.ReadInt();
        int ducket_price = p.ReadInt();
        int? publish_ducket_price = p.Available > 0 ? p.ReadInt() : null;
        return new InitCamera(credit_price, ducket_price, publish_ducket_price);
    }

    /// <summary>Composes the message into a packet.</summary>
    /// <param name="p">The packet writer.</param>
    public void Compose(in PacketWriter p)
    {
        RequireFlash(p.Client);
        p.WriteInt(CreditPrice);
        p.WriteInt(DucketPrice);
        if (PublishDucketPrice is int publish_ducket_price)
            p.WriteInt(publish_ducket_price);
    }

    private static void RequireFlash(ClientType client)
    {
    }
}

/// <summary>
/// Represents the <c>RequestCameraConfiguration</c> message, sent to request the camera configuration.
/// </summary>
/// <remarks>The message has no payload.</remarks>
public sealed record RequestCameraConfiguration : IParserComposer<RequestCameraConfiguration>
{
    /// <summary>Parses the message from a packet.</summary>
    /// <param name="p">The packet reader.</param>
    public static RequestCameraConfiguration Parse(in PacketReader p)
    {
        RequireSupportedClient(p.Client);
        return new RequestCameraConfiguration();
    }

    /// <summary>Composes the message into a packet.</summary>
    /// <param name="p">The packet writer.</param>
    public void Compose(in PacketWriter p) => RequireSupportedClient(p.Client);

    private static void RequireSupportedClient(ClientType client)
    {
        if (client is not (ClientType.Flash))
            throw new UnsupportedClientException(client);
    }
}

/// <summary>Represents the <c>PurchasePhoto</c> message, sent to buy the current camera photo.</summary>
/// <remarks>The message has no payload.</remarks>
public sealed record PurchasePhoto : IParserComposer<PurchasePhoto>
{
    /// <summary>Parses the message from a packet.</summary>
    /// <param name="p">The packet reader.</param>
    public static PurchasePhoto Parse(in PacketReader p)
    {
        RequireSupportedClient(p.Client);
        return new PurchasePhoto();
    }

    /// <summary>Composes the message into a packet.</summary>
    /// <param name="p">The packet writer.</param>
    public void Compose(in PacketWriter p) => RequireSupportedClient(p.Client);

    private static void RequireSupportedClient(ClientType client)
    {
        if (client is not (ClientType.Flash))
            throw new UnsupportedClientException(client);
    }
}

/// <summary>Represents the <c>PublishPhoto</c> message, sent to publish the current camera photo.</summary>
/// <remarks>The message has no payload.</remarks>
public sealed record PublishPhoto : IParserComposer<PublishPhoto>
{
    /// <summary>Parses the message from a packet.</summary>
    /// <param name="p">The packet reader.</param>
    public static PublishPhoto Parse(in PacketReader p)
    {
        RequireSupportedClient(p.Client);
        return new PublishPhoto();
    }

    /// <summary>Composes the message into a packet.</summary>
    /// <param name="p">The packet writer.</param>
    public void Compose(in PacketWriter p) => RequireSupportedClient(p.Client);

    private static void RequireSupportedClient(ClientType client)
    {
        if (client is not (ClientType.Flash))
            throw new UnsupportedClientException(client);
    }
}

/// <summary>Represents the <c>PhotoCompetition</c> message, sent to enter the current photo in a competition.</summary>
/// <remarks>The message has no payload.</remarks>
public sealed record PhotoCompetition : IParserComposer<PhotoCompetition>
{
    /// <summary>Parses the message from a packet.</summary>
    /// <param name="p">The packet reader.</param>
    public static PhotoCompetition Parse(in PacketReader p)
    {
        RequireSupportedClient(p.Client);
        return new PhotoCompetition();
    }

    /// <summary>Composes the message into a packet.</summary>
    /// <param name="p">The packet writer.</param>
    public void Compose(in PacketWriter p) => RequireSupportedClient(p.Client);

    private static void RequireSupportedClient(ClientType client)
    {
        if (client is not (ClientType.Flash))
            throw new UnsupportedClientException(client);
    }
}

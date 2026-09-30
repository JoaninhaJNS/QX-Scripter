namespace Qx.Model;

/// <summary>Specifies the kind of avatar in a room.</summary>
public enum AvatarType
{
    /// <summary>A user.</summary>
    User = 1,
    /// <summary>A pet.</summary>
    Pet = 2,
    /// <summary>A hotel-owned public bot.</summary>
    PublicBot = 3,
    /// <summary>A user-owned private bot.</summary>
    PrivateBot = 4
}

/// <summary>Specifies the gender of a user or bot.</summary>
public enum Gender
{
    /// <summary>A gender value that was not recognized.</summary>
    None = -1,
    /// <summary>Female.</summary>
    Female = 0,
    /// <summary>Male.</summary>
    Male = 1,
    /// <summary>Unisex.</summary>
    Unisex = 2
}

/// <summary>Provides conversions between <see cref="Gender"/> and the letter codes the client uses.</summary>
public static class Genders
{
    /// <summary>Converts a gender code to a <see cref="Gender"/>.</summary>
    /// <param name="value">The code, matched case-insensitively: <c>m</c>, <c>male</c>, <c>f</c>, <c>female</c>, <c>u</c> or <c>unisex</c>.</param>
    /// <returns>The matching gender, or <see cref="Gender.None"/> for any other value.</returns>
    public static Gender Parse(string value) => value.ToLowerInvariant() switch
    {
        "m" or "male" => Gender.Male,
        "f" or "female" => Gender.Female,
        "u" or "unisex" => Gender.Unisex,
        _ => Gender.None
    };

    /// <summary>Converts a gender to the one-letter code the client uses.</summary>
    /// <param name="gender">The gender to convert.</param>
    /// <returns><c>F</c> for female, <c>M</c> for male and <c>U</c> for any other value.</returns>
    public static string ToClientString(this Gender gender) => gender switch
    {
        Gender.Female => "F",
        Gender.Male => "M",
        _ => "U"
    };
}

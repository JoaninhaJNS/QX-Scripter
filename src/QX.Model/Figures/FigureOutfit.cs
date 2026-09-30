namespace Qx.Model.Figures;

/// <summary>
/// Represents a figure together with the gender it is worn as.
/// </summary>
/// <remarks>
/// The client models this pairing as an outfit because the figure string itself carries no gender:
/// every message that transports a figure transports the gender next to it.
/// </remarks>
public sealed record FigureOutfit
{
    /// <summary>Gets the empty outfit, an empty figure with <see cref="FigureGender.Undefined"/>.</summary>
    public static FigureOutfit Empty { get; } = new(Figures.Figure.Empty, FigureGender.Undefined);

    /// <summary>Gets the figure.</summary>
    public Figure Figure { get; init; }
    /// <summary>Gets the gender the figure is worn as.</summary>
    public FigureGender Gender { get; init; }

    /// <summary>Gets the gender code as sent on the wire, falling back to <c>U</c>.</summary>
    public string GenderCode => this.Gender.ToClientString();

    /// <summary>Initializes a new instance of the <see cref="FigureOutfit"/> record.</summary>
    /// <param name="figure">The figure.</param>
    /// <param name="gender">The gender the figure is worn as.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="figure"/> is <see langword="null"/>.</exception>
    public FigureOutfit(Figure figure, FigureGender gender)
    {
        ArgumentNullException.ThrowIfNull(figure);

        Figure = figure;
        Gender = gender;
    }

    /// <summary>Initializes a new instance of the <see cref="FigureOutfit"/> record from a gender code.</summary>
    /// <param name="figure">The figure.</param>
    /// <param name="gender">The gender code (<c>M</c>, <c>F</c> or <c>U</c>). Any other value gives <see cref="FigureGender.Undefined"/>.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="figure"/> is <see langword="null"/>.</exception>
    public FigureOutfit(Figure figure, string? gender)
        : this(figure, FigureGenderCode.TryParse(gender, out FigureGender parsed)
            ? parsed
            : FigureGender.Undefined)
    {
    }

    /// <summary>Parses the figure string and gender code as received from the server.</summary>
    /// <param name="figure">The figure string.</param>
    /// <param name="gender">The gender code (<c>M</c>, <c>F</c> or <c>U</c>).</param>
    /// <returns>The parsed outfit.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="figure"/> or <paramref name="gender"/> is <see langword="null"/>.</exception>
    /// <exception cref="FormatException">Thrown when the figure string or the gender code is invalid.</exception>
    public static FigureOutfit Parse(string figure, string gender)
    {
        ArgumentNullException.ThrowIfNull(figure);
        ArgumentNullException.ThrowIfNull(gender);
        return new FigureOutfit(Figures.Figure.Parse(figure), FigureGenderCode.Parse(gender));
    }

    /// <summary>Tries to parse the figure string and gender code as received from the server.</summary>
    /// <param name="figure">The figure string.</param>
    /// <param name="gender">The gender code (<c>M</c>, <c>F</c> or <c>U</c>).</param>
    /// <param name="outfit">The parsed outfit, or <see cref="Empty"/> when parsing fails.</param>
    /// <returns><see langword="true"/> if both values were parsed; otherwise, <see langword="false"/>.</returns>
    public static bool TryParse(string? figure, string? gender, out FigureOutfit outfit)
    {
        if (!Figures.Figure.TryParse(figure, out Figure parsedFigure) ||
            !FigureGenderCode.TryParse(gender, out FigureGender parsedGender))
        {
            outfit = Empty;
            return false;
        }

        outfit = new FigureOutfit(parsedFigure, parsedGender);
        return true;
    }

    /// <summary>Returns a copy of the outfit with a different figure.</summary>
    /// <param name="figure">The new figure.</param>
    /// <returns>A new outfit with <paramref name="figure"/> and the same gender.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="figure"/> is <see langword="null"/>.</exception>
    public FigureOutfit WithFigure(Figure figure)
    {
        ArgumentNullException.ThrowIfNull(figure);
        return this with { Figure = figure };
    }

    /// <summary>Returns a copy of the outfit with a different gender.</summary>
    /// <param name="gender">The new gender.</param>
    /// <returns>A new outfit with the same figure and <paramref name="gender"/>.</returns>
    public FigureOutfit WithGender(FigureGender gender) => this with { Gender = gender };

    /// <summary>Deconstructs the outfit into its figure and gender.</summary>
    /// <param name="figure">The figure.</param>
    /// <param name="gender">The gender the figure is worn as.</param>
    public void Deconstruct(out Figure figure, out FigureGender gender)
    {
        figure = this.Figure;
        gender = this.Gender;
    }

    /// <summary>Returns the figure string without the gender.</summary>
    /// <returns>The figure string.</returns>
    public override string ToString() => this.Figure.ToString();
}

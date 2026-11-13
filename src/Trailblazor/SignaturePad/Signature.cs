using System.Globalization;
using System.Net;
using System.Text;

namespace Trailblazor;

/// <summary>A signature from a <see cref="TbSignaturePad"/>: the strokes drawn on it, in a space
/// <see cref="Width"/> wide and <see cref="Height"/> high, whatever size the pad is shown at, or the name typed instead.
/// Two signatures are equal when their strokes and names are.</summary>
/// <param name="Strokes">Each stroke, in the order drawn.</param>
/// <param name="TypedName">The name typed instead of drawing, or null when the signature is drawn.</param>
public sealed record Signature(IReadOnlyList<SignatureStroke> Strokes, string? TypedName = null)
{
    /// <summary>How wide the space the strokes are drawn in is.</summary>
    public const double Width = 600;

    /// <summary>How high the space the strokes are drawn in is.</summary>
    public const double Height = 200;

    /// <summary>A signature with nothing drawn or typed.</summary>
    public static Signature Empty { get; } = new([]);

    /// <summary>Whether nothing is drawn and no name is typed.</summary>
    public bool IsEmpty => Strokes.Count == 0 && string.IsNullOrWhiteSpace(TypedName);

    /// <summary>The signature as an SVG image of the whole space: the strokes as round-ended lines, or the typed name in
    /// a handwriting-style font.</summary>
    /// <param name="color">The color to draw in, as a CSS color. Defaults to black.</param>
    public string ToSvg(string color = "#000")
    {
        var svg = new StringBuilder().Append(CultureInfo.InvariantCulture,
            $"<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 {Width} {Height}\" width=\"{Width}\" height=\"{Height}\">");
        var paint = WebUtility.HtmlEncode(color);
        if (!string.IsNullOrWhiteSpace(TypedName))
        {
            svg.Append(CultureInfo.InvariantCulture, $"<text x=\"{Width / 2}\" y=\"{Height * 0.6}\" text-anchor=\"middle\" ")
                .Append(CultureInfo.InvariantCulture, $"font-family=\"cursive\" font-size=\"48\" fill=\"{paint}\">")
                .Append(WebUtility.HtmlEncode(TypedName.Trim())).Append("</text>");
        }
        else
        {
            foreach (var stroke in Strokes)
            {
                svg.Append(CultureInfo.InvariantCulture, $"<path d=\"{stroke.PathData}\" fill=\"none\" stroke=\"{paint}\" ")
                    .Append("stroke-width=\"3\" stroke-linecap=\"round\" stroke-linejoin=\"round\"/>");
            }
        }

        return svg.Append("</svg>").ToString();
    }

    /// <summary>Whether the other signature has the same strokes, point by point, and the same typed name.</summary>
    public bool Equals(Signature? other) => other is not null && TypedName == other.TypedName
        && Strokes.SequenceEqual(other.Strokes);

    /// <summary>A hash code that equal signatures share.</summary>
    public override int GetHashCode() => HashCode.Combine(TypedName, Strokes.Count);
}

/// <summary>One stroke of a <see cref="Signature"/>: the points the pointer passed through while pressed. Two strokes are
/// equal when their points are.</summary>
/// <param name="Points">Each point, in the order drawn.</param>
public sealed record SignatureStroke(IReadOnlyList<SignaturePoint> Points)
{
    /// <summary>The stroke as SVG path data; a single point draws a dot.</summary>
    public string PathData
    {
        get
        {
            if (Points.Count == 0)
            {
                return "";
            }

            var path = new StringBuilder().Append(CultureInfo.InvariantCulture, $"M{N(Points[0].X)} {N(Points[0].Y)}");
            foreach (var point in Points.Count == 1 ? Points : Points.Skip(1))
            {
                path.Append(CultureInfo.InvariantCulture, $" L{N(point.X)} {N(point.Y)}");
            }

            return path.ToString();
        }
    }

    private static string N(double value) => value.ToString("0.#", CultureInfo.InvariantCulture);

    /// <summary>Whether the other stroke has the same points, in the same order.</summary>
    public bool Equals(SignatureStroke? other) => other is not null && Points.SequenceEqual(other.Points);

    /// <summary>A hash code that equal strokes share.</summary>
    public override int GetHashCode() => Points.Count;
}

/// <summary>A point of a <see cref="SignatureStroke"/>, in the signature's space.</summary>
/// <param name="X">How far from the left, from 0 to <see cref="Signature.Width"/>.</param>
/// <param name="Y">How far from the top, from 0 to <see cref="Signature.Height"/>.</param>
public readonly record struct SignaturePoint(double X, double Y);

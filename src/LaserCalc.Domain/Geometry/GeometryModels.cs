namespace LaserCalc.Domain.Geometry;

public enum GeometryPrimitiveType
{
    Line,
    Arc,
    Circle
}

public readonly record struct Point2(double X, double Y)
{
    public double DistanceTo(Point2 other)
    {
        var dx = X - other.X;
        var dy = Y - other.Y;
        return Math.Sqrt(dx * dx + dy * dy);
    }
}

/// <summary>
/// One planar cutting primitive. For arcs, SweepRadians is signed (CCW positive).
/// For a circle, Center and Radius are populated and SweepRadians is 2*pi.
/// </summary>
public sealed record GeometryPrimitive(
    GeometryPrimitiveType Type,
    Point2 Start,
    Point2 End,
    Point2? Center,
    double Radius,
    double SweepRadians,
    string Layer = "0",
    int ContourId = 0)
{
    public double LengthMm => Type switch
    {
        GeometryPrimitiveType.Line => Start.DistanceTo(End),
        GeometryPrimitiveType.Arc => Math.Abs(Radius * SweepRadians),
        GeometryPrimitiveType.Circle => 2.0 * Math.PI * Radius,
        _ => 0.0
    };

    public GeometryPrimitive Reversed() => Type switch
    {
        GeometryPrimitiveType.Line => this with { Start = End, End = Start },
        GeometryPrimitiveType.Arc => this with { Start = End, End = Start, SweepRadians = -SweepRadians },
        _ => this
    };

    public IReadOnlyList<Point2> Sample(double maximumAngleDegrees = 4.0)
    {
        if (Type == GeometryPrimitiveType.Line)
            return new[] { Start, End };

        var center = Center ?? new Point2(0, 0);
        var startAngle = Type == GeometryPrimitiveType.Circle
            ? 0.0
            : Math.Atan2(Start.Y - center.Y, Start.X - center.X);
        var sweep = Type == GeometryPrimitiveType.Circle ? Math.PI * 2.0 : SweepRadians;
        var steps = Math.Max(2, (int)Math.Ceiling(Math.Abs(sweep) / (maximumAngleDegrees * Math.PI / 180.0)));
        var points = new List<Point2>(steps + 1);
        for (var i = 0; i <= steps; i++)
        {
            var angle = startAngle + sweep * i / steps;
            points.Add(new Point2(center.X + Radius * Math.Cos(angle), center.Y + Radius * Math.Sin(angle)));
        }
        return points;
    }

    /// <summary>Green's-theorem contribution to signed area for a directed line or arc.</summary>
    public double SignedAreaContribution()
    {
        if (Type == GeometryPrimitiveType.Line)
            return 0.5 * (Start.X * End.Y - End.X * Start.Y);
        if (Type != GeometryPrimitiveType.Arc || Center is null)
            return 0.0;

        var c = Center.Value;
        var a = Math.Atan2(Start.Y - c.Y, Start.X - c.X);
        var b = a + SweepRadians;
        var integral = c.X * Radius * (Math.Sin(b) - Math.Sin(a))
                     + c.Y * Radius * (Math.Cos(a) - Math.Cos(b))
                     + Radius * Radius * SweepRadians;
        return 0.5 * integral;
    }
}

public sealed class PartGeometry
{
    public required string SourceFile { get; init; }
    public required string Format { get; init; }
    public double WidthMm { get; init; }
    public double HeightMm { get; init; }
    public double? NetAreaMm2 { get; init; }
    /// <summary>Area value stored in the source file (e.g. TruTops metadata), kept for cross-checks only.</summary>
    public double? ReportedNetAreaMm2 { get; init; }
    public double CutLengthMm { get; init; }
    public int PointCount { get; init; }
    public int ContourCount { get; init; }
    public int OpenContourCount { get; init; }
    public int EstimatedPierceCount { get; init; }
    public int UnknownEntityCount { get; init; }
    public bool UnitsAssumedMillimeters { get; init; }
    public bool RequiresUnitVerification { get; init; }
    public bool RequiresAreaVerification { get; init; }
    public bool IsSafeForPreliminaryCalculation { get; init; }
    public IReadOnlyList<GeometryPrimitive> Primitives { get; init; } = Array.Empty<GeometryPrimitive>();
    public IReadOnlyList<string> Warnings { get; init; } = Array.Empty<string>();
}

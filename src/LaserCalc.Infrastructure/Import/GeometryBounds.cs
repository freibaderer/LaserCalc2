using LaserCalc.Domain.Geometry;

namespace LaserCalc.Infrastructure.Import;

internal readonly record struct GeometryBounds(double MinX, double MinY, double MaxX, double MaxY)
{
    public double Width => MaxX - MinX;
    public double Height => MaxY - MinY;

    public static GeometryBounds Calculate(IEnumerable<GeometryPrimitive> primitives)
    {
        var points = new List<Point2>();
        foreach (var primitive in primitives)
        {
            switch (primitive.Type)
            {
                case GeometryPrimitiveType.Line:
                    points.Add(primitive.Start);
                    points.Add(primitive.End);
                    break;
                case GeometryPrimitiveType.Circle:
                {
                    var center = primitive.Center ?? primitive.Start;
                    points.Add(new Point2(center.X - primitive.Radius, center.Y - primitive.Radius));
                    points.Add(new Point2(center.X + primitive.Radius, center.Y + primitive.Radius));
                    break;
                }
                case GeometryPrimitiveType.Arc:
                {
                    var center = primitive.Center ?? primitive.Start;
                    points.Add(primitive.Start);
                    points.Add(primitive.End);
                    foreach (var angle in new[] { 0.0, Math.PI / 2.0, Math.PI, Math.PI * 1.5 })
                    {
                        if (!AngleOnSweep(angle, Math.Atan2(primitive.Start.Y - center.Y, primitive.Start.X - center.X), primitive.SweepRadians))
                            continue;
                        points.Add(new Point2(center.X + primitive.Radius * Math.Cos(angle), center.Y + primitive.Radius * Math.Sin(angle)));
                    }
                    break;
                }
            }
        }

        if (points.Count == 0) return new GeometryBounds(0, 0, 0, 0);
        return new GeometryBounds(points.Min(x => x.X), points.Min(x => x.Y), points.Max(x => x.X), points.Max(x => x.Y));
    }

    private static bool AngleOnSweep(double angle, double start, double sweep)
    {
        const double epsilon = 1e-10;
        if (sweep >= 0)
        {
            var delta = PositiveAngle(angle - start);
            return delta <= sweep + epsilon;
        }
        else
        {
            var delta = PositiveAngle(start - angle);
            return delta <= -sweep + epsilon;
        }
    }

    private static double PositiveAngle(double value)
    {
        value %= 2.0 * Math.PI;
        return value < 0 ? value + 2.0 * Math.PI : value;
    }
}

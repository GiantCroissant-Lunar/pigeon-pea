using SkiaSharp;

namespace PigeonPea.Shared.Rendering.Primitives;

public static class CurveSmoothing
{
    public static SKPath CreateCatmullRomSpline(List<SKPoint> points, bool closed = false, float tension = 0.5f)
    {
        var path = new SKPath();
        if (points.Count < 2) return path;
        if (points.Count == 2) { path.MoveTo(points[0]); path.LineTo(points[1]); return path; }
        path.MoveTo(points[0]);
        for (int i = 0; i < points.Count - 1; i++)
        {
            var p0 = i > 0 ? points[i - 1] : (closed ? points[^1] : points[i]);
            var p1 = points[i];
            var p2 = points[i + 1];
            var p3 = i + 2 < points.Count ? points[i + 2] : (closed ? points[0] : points[i + 1]);
            AddCatmullRomSegment(path, p0, p1, p2, p3, tension);
        }
        if (closed)
        {
            var p0 = points[^2]; var p1 = points[^1]; var p2 = points[0]; var p3 = points[1];
            AddCatmullRomSegment(path, p0, p1, p2, p3, tension);
            path.Close();
        }
        return path;
    }

    private static void AddCatmullRomSegment(SKPath path, SKPoint p0, SKPoint p1, SKPoint p2, SKPoint p3, float tension)
    {
        float t = tension;
        var c1 = new SKPoint(p1.X + (p2.X - p0.X) / 6f * t, p1.Y + (p2.Y - p0.Y) / 6f * t);
        var c2 = new SKPoint(p2.X - (p3.X - p1.X) / 6f * t, p2.Y - (p3.Y - p1.Y) / 6f * t);
        path.CubicTo(c1, c2, p2);
    }

    public static SKPath CreateBSpline(List<SKPoint> points, int degree = 3)
    {
        var path = new SKPath();
        if (points.Count < degree + 1)
        {
            if (points.Count > 0) { path.MoveTo(points[0]); foreach (var point in points.Skip(1)) path.LineTo(point); }
            return path;
        }
        path.MoveTo(points[0]);
        for (int i = 0; i < points.Count - 3; i++)
        {
            var p0 = points[i]; var p1 = points[i + 1]; var p2 = points[i + 2]; var p3 = points[i + 3];
            var c1 = new SKPoint((2 * p1.X + p2.X) / 3f, (2 * p1.Y + p2.Y) / 3f);
            var c2 = new SKPoint((p1.X + 2 * p2.X) / 3f, (p1.Y + 2 * p2.Y) / 3f);
            var end = new SKPoint((p1.X + 4 * p2.X + p3.X) / 6f, (p1.Y + 4 * p2.Y + p3.Y) / 6f);
            path.CubicTo(c1, c2, end);
        }
        if (points.Count > 0) path.LineTo(points[^1]);
        return path;
    }

    public static List<SKPoint> SmoothPoints(List<SKPoint> points, int windowSize = 3, int iterations = 1)
    {
        if (points.Count < windowSize || windowSize < 2) return new List<SKPoint>(points);
        var smoothed = new List<SKPoint>(points);
        for (int iter = 0; iter < iterations; iter++)
        {
            var temp = new List<SKPoint>();
            int halfWindow = windowSize / 2;
            for (int i = 0; i < smoothed.Count; i++)
            {
                float sumX = 0, sumY = 0; int count = 0;
                for (int j = Math.Max(0, i - halfWindow); j <= Math.Min(smoothed.Count - 1, i + halfWindow); j++)
                { sumX += smoothed[j].X; sumY += smoothed[j].Y; count++; }
                temp.Add(new SKPoint(sumX / count, sumY / count));
            }
            smoothed = temp;
        }
        return smoothed;
    }

    public static List<SKPoint> SimplifyPath(List<SKPoint> points, float epsilon = 1.0f)
    {
        if (points.Count < 3) return new List<SKPoint>(points);
        return DouglasPeucker(points, 0, points.Count - 1, epsilon);
    }

    private static List<SKPoint> DouglasPeucker(List<SKPoint> points, int start, int end, float epsilon)
    {
        float maxDist = 0; int maxIndex = 0;
        for (int i = start + 1; i < end; i++)
        {
            float dist = PerpendicularDistance(points[i], points[start], points[end]);
            if (dist > maxDist) { maxDist = dist; maxIndex = i; }
        }
        if (maxDist > epsilon)
        {
            var left = DouglasPeucker(points, start, maxIndex, epsilon);
            var right = DouglasPeucker(points, maxIndex, end, epsilon);
            var result = new List<SKPoint>(left); result.AddRange(right.Skip(1)); return result;
        }
        else { return new List<SKPoint> { points[start], points[end] }; }
    }

    private static float PerpendicularDistance(SKPoint point, SKPoint lineStart, SKPoint lineEnd)
    {
        float dx = lineEnd.X - lineStart.X; float dy = lineEnd.Y - lineStart.Y;
        if (dx == 0 && dy == 0) return Distance(point, lineStart);
        float t = ((point.X - lineStart.X) * dx + (point.Y - lineStart.Y) * dy) / (dx * dx + dy * dy);
        t = Math.Max(0, Math.Min(1, t));
        var projection = new SKPoint(lineStart.X + t * dx, lineStart.Y + t * dy);
        return Distance(point, projection);
    }

    private static float Distance(SKPoint p1, SKPoint p2)
    { float dx = p2.X - p1.X; float dy = p2.Y - p1.Y; return (float)Math.Sqrt(dx * dx + dy * dy); }

    public static SKPath CreateSmoothContour(List<SKPoint> boundaryPoints, bool closed = true)
    {
        if (boundaryPoints.Count < 3)
        {
            var simplePath = new SKPath();
            if (boundaryPoints.Count > 0)
            { simplePath.MoveTo(boundaryPoints[0]); foreach (var pt in boundaryPoints.Skip(1)) simplePath.LineTo(pt); if (closed) simplePath.Close(); }
            return simplePath;
        }
        var simplified = SimplifyPath(boundaryPoints, epsilon: 2.0f);
        var smoothed = SmoothPoints(simplified, windowSize: 3, iterations: 2);
        var path = CreateCatmullRomSpline(smoothed, closed, tension: 0.6f);
        return path;
    }
}

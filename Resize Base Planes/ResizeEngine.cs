using System;

namespace ResizeBasePlanes
{
    internal struct AxisReading
    {
        internal double Value;
        internal double Resolution;
        internal AxisReading(double value, double resolution) { Value = value; Resolution = resolution; }
        internal bool Matches(double target)
        {
            // Plane dimensions are available only through the displayed fields.
            // Respect that precision, so repeated runs cannot slowly move a
            // boundary when a requested value rounds to the same display value.
            return Math.Abs(Value - target) <= Math.Max(1e-10, Resolution * 0.500001);
        }
    }

    internal interface IPlane
    {
        string Name { get; }
        AxisReading[] Read();
        void MoveBoundary(bool alongX, double delta);
    }

    internal static class ResizeEngine
    {
        internal static void Apply(IPlane[] planes, double x, double y)
        {
            if (!(x > 0) || !(y > 0) || Double.IsInfinity(x) || Double.IsInfinity(y))
                throw new ArgumentException("Both plane sizes must be finite and greater than zero.");
            if (planes == null || planes.Length < 3)
                throw new ArgumentException("Three base planes are required.");

            // Siemens' ResizeRefPlaneByDirDist takes a displacement. Boundary 0
            // moves both sides inward; halve the full-size difference. Negative
            // differences enlarge the plane. Never pass the target as a delta.
            for (int i = 0; i < 3; i++)
            {
                IPlane plane = planes[i];
                AxisReading[] current = plane.Read();
                if (!current[0].Matches(x)) plane.MoveBoundary(true, (current[0].Value - x) / 2);
                if (!current[1].Matches(y)) plane.MoveBoundary(false, (current[1].Value - y) / 2);
                AxisReading[] actual = plane.Read();
                if (!actual[0].Matches(x) || !actual[1].Matches(y))
                    throw new InvalidOperationException("The requested X/Y sizes were not confirmed for " + plane.Name + ".");
            }
        }
    }
}

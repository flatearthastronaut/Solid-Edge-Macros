using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using HoleTableFormatter;

namespace TurboCallout
{
    // SDK GetSegmentedStyle returns IDispatch**, not VARIANT*. Explicit COM
    // signatures avoid the dynamic binder's argument-conversion error. GUIDs
    // and DISPIDs come from the supplied Solid Edge Draft SDK interop library.
    [ComImport, Guid("a980f5d8-2c32-11d4-ab5c-00c04f72e275"), InterfaceType(ComInterfaceType.InterfaceIsIDispatch)]
    internal interface ILineSegments
    { [DispId(14437)] void GetSegmentedStyle(int index, out double x1, out double y1, out double x2, out double y2, [MarshalAs(UnmanagedType.IDispatch)] out object style); }

    [ComImport, Guid("91b3ee92-3024-11d4-ab5c-00c04f72e275"), InterfaceType(ComInterfaceType.InterfaceIsIDispatch)]
    internal interface ICircleSegments
    { [DispId(24677)] void GetSegmentedStyle(int index, out double x1, out double y1, out double x2, out double y2, [MarshalAs(UnmanagedType.IDispatch)] out object style); }

    [ComImport, Guid("325840ec-3024-11d4-ab5c-00c04f72e275"), InterfaceType(ComInterfaceType.InterfaceIsIDispatch)]
    internal interface IArcSegments
    { [DispId(14437)] void GetSegmentedStyle(int index, out double x1, out double y1, out double x2, out double y2, [MarshalAs(UnmanagedType.IDispatch)] out object style); }

    [ComImport, Guid("ba0c398a-3024-11d4-ab5c-00c04f72e275"), InterfaceType(ComInterfaceType.InterfaceIsIDispatch)]
    internal interface IEllipseSegments
    { [DispId(24677)] void GetSegmentedStyle(int index, out double x1, out double y1, out double x2, out double y2, [MarshalAs(UnmanagedType.IDispatch)] out object style); }

    [ComImport, Guid("dc605566-3024-11d4-ab5c-00c04f72e275"), InterfaceType(ComInterfaceType.InterfaceIsIDispatch)]
    internal interface IEllipticalArcSegments
    { [DispId(14437)] void GetSegmentedStyle(int index, out double x1, out double y1, out double x2, out double y2, [MarshalAs(UnmanagedType.IDispatch)] out object style); }

    [ComImport, Guid("09b1c81a-3025-11d4-ab5c-00c04f72e275"), InterfaceType(ComInterfaceType.InterfaceIsIDispatch)]
    internal interface ILineStringSegments
    { [DispId(24677)] void GetSegmentedStyle(int index, out double x1, out double y1, out double x2, out double y2, [MarshalAs(UnmanagedType.IDispatch)] out object style); }

    [ComImport, Guid("7a0e7bf4-3024-11d4-ab5c-00c04f72e275"), InterfaceType(ComInterfaceType.InterfaceIsIDispatch)]
    internal interface ISplineSegments
    { [DispId(14437)] void GetSegmentedStyle(int index, out double x1, out double y1, out double x2, out double y2, [MarshalAs(UnmanagedType.IDispatch)] out object style); }

    internal sealed class EdgeSegment
    {
        internal double X1, Y1, X2, Y2;
        internal bool Hidden;
    }

    internal static class LeaderSide
    {
        const int ReferenceType = -768828720, LineType = 877758166, CircleType = -2074243498,
            ArcType = -1804156302, EllipseType = -429893932, EllipticalArcType = -1799795820,
            LineStringType = 1720171476, SplineType = 1550814640;

        internal static bool IsHidden(object note)
        {
            if (!(bool)((dynamic)note).IsTerminatorAttachedToEntity)
                throw new InvalidOperationException("Cannot determine leader side without an attached edge.");
            var held = new List<object>();
            try
            {
                object edge; double x, y, z; bool keyPoint;
                ((ICalloutTerminator)note).GetTerminator(out edge, out x, out y, out z, out keyPoint); held.Add(edge);
                // Follow the actual projected reference, not the 3D feature or
                // another coincident edge. Hidden status belongs to this view.
                for (int depth = 0; (int)Property.Get(edge, "Type") == ReferenceType; depth++)
                {
                    if (depth == 8) throw new InvalidOperationException("Cannot resolve the leader's projected edge.");
                    edge = Property.Get(edge, "Object"); held.Add(edge);
                }
                int type = (int)Property.Get(edge, "Type");
                object member = Property.Get(edge, "ModelMember"); held.Add(member);
                string hidden = Convert.ToString(Property.Get(member, "HiddenEdgeStyleName"));
                string visible = Convert.ToString(Property.Get(member, "VisibleEdgeStyleName"));
                string tangent = Convert.ToString(Property.Get(member, "TangentEdgeStyleName"));
                if (String.IsNullOrWhiteSpace(hidden) || hidden == visible || hidden == tangent)
                    throw new InvalidOperationException("Hidden and visible/tangent edge styles are indistinguishable; review leader side.");
                int count = (int)Property.Get(edge, "SegmentedStyleCount");
                if (count < 1) throw new InvalidOperationException("The attached edge has no readable display segments.");
                var segments = new List<EdgeSegment>();
                for (int index = 1; index <= count; index++)
                {
                    object style = null; var segment = new EdgeSegment();
                    try
                    {
                        ReadSegment(edge, type, index, segment, out style);
                        segment.Hidden = String.Equals(Convert.ToString(Property.Get(style, "Name")), hidden, StringComparison.Ordinal);
                        segments.Add(segment);
                    }
                    finally { Com.Release(style); }
                }
                if (segments.All(s => s.Hidden == segments[0].Hidden)) return segments[0].Hidden;
                // A projected edge can contain both visible and hidden pieces.
                // Use the arrow's attachment point in view coordinates. Points
                // on a visibility boundary remain for review instead of guessing.
                object view = Property.Get(edge, "DrawingView"); held.Add(view);
                double px, py; ((dynamic)view).SheetToView(x, y, out px, out py);
                bool? result;
                if (type == LineType) result = OnLine(segments, px, py);
                else if (type == CircleType || type == ArcType || type == EllipseType || type == EllipticalArcType)
                {
                    double cx, cy, ax, ay;
                    ((dynamic)edge).GetCenterPoint(out cx, out cy);
                    ((dynamic)edge).GetMajorAxis(out ax, out ay);
                    bool ellipse = type == EllipseType || type == EllipticalArcType;
                    double major = Convert.ToDouble(Property.Get(edge, ellipse ? "MajorRadius" : "Radius"));
                    double minor = ellipse ? Convert.ToDouble(Property.Get(edge, "MinorRadius")) : major;
                    result = OnEllipse(segments, px, py, cx, cy, ax, ay, major, minor);
                }
                else result = null;
                if (!result.HasValue) throw new InvalidOperationException("Leader touches mixed or unresolved hidden/visible segments; review leader side.");
                return result.Value;
            }
            finally { for (int i = held.Count - 1; i >= 0; i--) Com.Release(held[i]); }
        }

        static void ReadSegment(object edge, int type, int index, EdgeSegment s, out object style)
        {
            switch (type)
            {
                case LineType: ((ILineSegments)edge).GetSegmentedStyle(index, out s.X1, out s.Y1, out s.X2, out s.Y2, out style); break;
                case CircleType: ((ICircleSegments)edge).GetSegmentedStyle(index, out s.X1, out s.Y1, out s.X2, out s.Y2, out style); break;
                case ArcType: ((IArcSegments)edge).GetSegmentedStyle(index, out s.X1, out s.Y1, out s.X2, out s.Y2, out style); break;
                case EllipseType: ((IEllipseSegments)edge).GetSegmentedStyle(index, out s.X1, out s.Y1, out s.X2, out s.Y2, out style); break;
                case EllipticalArcType: ((IEllipticalArcSegments)edge).GetSegmentedStyle(index, out s.X1, out s.Y1, out s.X2, out s.Y2, out style); break;
                case LineStringType: ((ILineStringSegments)edge).GetSegmentedStyle(index, out s.X1, out s.Y1, out s.X2, out s.Y2, out style); break;
                case SplineType: ((ISplineSegments)edge).GetSegmentedStyle(index, out s.X1, out s.Y1, out s.X2, out s.Y2, out style); break;
                default: throw new InvalidOperationException("Unsupported leader edge for hidden-line detection.");
            }
        }

        // Pure geometry selectors are also used by regression tests. One micron
        // in view space is only a numerical/attachment tolerance, not a reason
        // to choose one side of a visibility transition over the other.
        const double Tolerance = 0.000001;
        static bool? Unique(IEnumerable<EdgeSegment> hits)
        { var values = hits.Select(s => s.Hidden).Distinct().ToArray(); return values.Length == 1 ? (bool?)values[0] : null; }
        internal static bool? OnLine(IEnumerable<EdgeSegment> segments, double x, double y)
        {
            return Unique(segments.Where(s =>
            {
                double dx = s.X2 - s.X1, dy = s.Y2 - s.Y1, length2 = dx * dx + dy * dy;
                if (length2 <= 0) return false;
                double t = ((x - s.X1) * dx + (y - s.Y1) * dy) / length2;
                double slack = Tolerance / Math.Sqrt(length2);
                return t >= -slack && t <= 1 + slack && Math.Abs((x - s.X1) * dy - (y - s.Y1) * dx) / Math.Sqrt(length2) <= Tolerance;
            }));
        }
        static double Angle(double x, double y)
        { double a = Math.Atan2(y, x); return a < 0 ? a + 2 * Math.PI : a; }
        static double Wrap(double a) { a %= 2 * Math.PI; return a < 0 ? a + 2 * Math.PI : a; }
        internal static bool? OnEllipse(IEnumerable<EdgeSegment> segments, double x, double y, double cx, double cy,
            double ax, double ay, double major, double minor)
        {
            double length = Math.Sqrt(ax * ax + ay * ay);
            if (!(length > 0 && major > 0 && minor > 0)) return null;
            ax /= length; ay /= length;
            Func<double, double, double[]> local = (px, py) => new[] { ((px - cx) * ax + (py - cy) * ay) / major,
                (-(px - cx) * ay + (py - cy) * ax) / minor };
            double[] p = local(x, y); double slack = Tolerance / Math.Min(major, minor);
            if (Math.Abs(Math.Sqrt(p[0] * p[0] + p[1] * p[1]) - 1) > slack) return null;
            double angle = Angle(p[0], p[1]);
            return Unique(segments.Where(s =>
            {
                double[] a = local(s.X1, s.Y1), b = local(s.X2, s.Y2);
                double start = Angle(a[0], a[1]), sweep = Wrap(Angle(b[0], b[1]) - start), offset = Wrap(angle - start);
                return sweep < 1e-10 || offset <= sweep + slack || offset >= 2 * Math.PI - slack;
            }));
        }
    }
}
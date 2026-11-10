using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using HoleTableFormatter;

namespace TurboCallout
{
    [ComImport, Guid("4A603C12-E750-11CE-BB40-080036776C02"), InterfaceType(ComInterfaceType.InterfaceIsIDispatch)]
    internal interface ICalloutTerminator
    {
        [DispId(27)] void GetTerminator([MarshalAs(UnmanagedType.IDispatch)] out object entity, out double x, out double y, out double z,
            [MarshalAs(UnmanagedType.VariantBool)] out bool keyPoint);
    }
    internal sealed class NativeHole : IDisposable
    {
        readonly object draft;
        readonly Dictionary<string, object> models = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        internal NativeHole(object draft) { this.draft = draft; }

        internal HoleFacts Read(object note)
        {
            // A projected edge's owning feature can differ from the hole that
            // Solid Edge evaluates for %HS/%BS/%TS (notably separate SAE port
            // entrances and spotfaces). Probe the native annotation evaluator,
            // just as the hole-table macro probes native table expressions.
            // A disposable duplicate preserves the exact leader association;
            // the original note's text, style and placement are never touched.
            object probe = null, styles = null, style = null, local = null, target = null;
            string name = "Turbo Callout Probe " + Guid.NewGuid().ToString("N");
            try
            {
                if (!(bool)((dynamic)note).IsTerminatorAttachedToEntity) throw new InvalidOperationException("Leader is not attached to model geometry.");
                probe = ((dynamic)note).Duplicate(0.0, 0.0);
                double x, y, z; bool keyPoint;
                ((ICalloutTerminator)note).GetTerminator(out target, out x, out y, out z, out keyPoint);
                ((dynamic)probe).SetTerminator(target, x, y, z, keyPoint);
                local = ((dynamic)probe).Style;
                styles = ((dynamic)draft).DimensionStyles;
                style = ((dynamic)styles).Add(name, (string)((dynamic)local).Name);
                Property.Set(style, "PrimaryUnits", 5); Property.Set(style, "RoundOffType", 1); Property.Set(style, "DualDisplay", false);
                string fields = "%{%HS/NU/@8}|%{%HD/NU/@8}|%{%BS/NU/@8}|%{%BD/NU/@8}|%{%SS/NU/@8}|%{%SA/NU/@8}|%TS|%{%TD/NU/@8}";
                string[] names = { "Simple", "Counterbore", "Countersink", "SimpleThreaded", "CounterboreThreaded", "CountersinkThreaded", "Counterdrill", "CounterdrillThreaded" };
                for (int i = 0; i < names.Length; i++) Property.Set(style, "HoleCallout" + names[i], (i + 1) + "|" + fields);
                ((dynamic)local).Name = name; ((dynamic)probe).BalloonText = "%HC";
                string evaluated = (string)((dynamic)probe).BalloonDisplayedText;
                int split = evaluated.IndexOf('|'), code;
                if (split < 1 || !Int32.TryParse(evaluated.Substring(0, split).Trim(), out code) || code < 1 || code > 8)
                    throw new InvalidOperationException("Solid Edge cannot resolve the leader to a native hole callout: " + evaluated);
                string[] types = { "Simple", "Counterbore", "Countersink", "Simple Threaded", "Counterbore Threaded", "Countersink Threaded", "Counterdrill", "Counterdrill Threaded" };
                string[] values = evaluated.Substring(split + 1).Split('|');
                if (values.Length != 8) throw new InvalidOperationException("Incomplete native hole fields.");
                values[5] = values[5].Replace("°", "").Replace("%DG", "");
                HoleFacts facts = HoleFacts.Parse(types[code - 1], String.Join("|", values));
                if (facts.Depth.HasValue && Math.Abs(facts.Depth.Value) < RuleBook.Epsilon) facts.Depth = null;
                if (facts.ThreadDepth.HasValue && Math.Abs(facts.ThreadDepth.Value) < RuleBook.Epsilon) facts.ThreadDepth = null;
                return facts;
            }
            finally
            {
                // Only objects created by this read are removed. An unsuccessful
                // probe must not leave diagnostic text or a shared style behind.
                try { if (probe != null) ((dynamic)probe).Delete(); }
                finally
                {
                    try { if (style != null) ((dynamic)styles).Remove(name); }
                    finally { Com.Release(target); Com.Release(local); Com.Release(style); Com.Release(styles); Com.Release(probe); }
                }
            }
        }

        public void Dispose() { }
    }
}

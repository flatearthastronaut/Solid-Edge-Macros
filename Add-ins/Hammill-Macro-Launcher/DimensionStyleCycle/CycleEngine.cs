using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using Hammill.Automation;

namespace Hammill.DimensionStyleCycle
{
    internal static class CycleEngine
    {
        // ObjectType.igDimension, verified from the bundled SDK framework interop.
        internal const int DimensionObjectType = 488188096;

        public static string Run(object document, StyleSequence sequence, StyleIndex index)
        {
            // The caller owns document. This scope owns only getter/Item results;
            // never FinalReleaseComObject or keep COM references between clicks.
            if (document == null || (int)((dynamic)document).Type != 2)
                throw new InvalidOperationException("Activate a draft and select one existing dimension.");
            using (var scope = new ComScope())
            {
                dynamic selection = scope.Own(((dynamic)document).SelectSet);
                if ((int)selection.Count != 1) throw new InvalidOperationException("Select exactly one existing dimension, then click Cycle Places.");
                dynamic dimension = scope.Own(selection.Item(1));
                if ((int)dimension.Type != DimensionObjectType)
                    throw new InvalidOperationException("The selected object is not a dimension. Select one existing dimension first.");
                int kind = (int)dimension.DimensionType;
                if (kind == 3 || kind == 7 || kind == 11)
                    throw new InvalidOperationException("This command cycles inch-based dimension styles. Angular dimensions are left unchanged.");
                dynamic style = scope.Own(dimension.Style);
                string source = (string)style.Name;
                dynamic definitions = scope.Own(((dynamic)document).DimensionStyles);
                string target = sequence.Next(source);
                dynamic targetDefinition = scope.Own(index.Find((object)definitions, target));
                target = (string)targetDefinition.Name;
                // A misplaced entry in the text file must not silently switch
                // inch/dual units or reverse the primary/secondary unit order.
                if ((bool)style.DualDisplay != (bool)targetDefinition.DualDisplay ||
                    (int)style.PrimaryUnits != (int)targetDefinition.PrimaryUnits ||
                    (int)style.TextOrientation != (int)targetDefinition.TextOrientation ||
                    ((bool)style.DualDisplay && (int)style.SecondaryUnits != (int)targetDefinition.SecondaryUnits))
                    throw new InvalidOperationException("The next style has different units or text orientation. Put it in the appropriate family in Hammill.DimensionStyles.txt. The dimension was not changed.");

                var original = new DimensionState((object)dimension);
                try
                {
                    // Name on the dimension's private DimStyle chooses an existing
                    // definition. Setting Name on targetDefinition would rename the
                    // shared style, so that object is strictly read-only here.
                    style.Name = target;
                    original.RestoreAndVerify((object)dimension);
                    if (!String.Equals((string)style.Name, target, StringComparison.OrdinalIgnoreCase))
                        throw new InvalidOperationException("Solid Edge did not retain the requested style.");
                    // No SelectSet.Clear, new command, or selection tool: the same
                    // native dimension remains selected for the next button press.
                    return target;
                }
                catch (Exception error)
                {
                    string recovery;
                    try
                    {
                        style.Name = source;
                        original.RestoreAndVerify((object)dimension);
                        if (!String.Equals((string)style.Name, source, StringComparison.OrdinalIgnoreCase))
                            throw new InvalidOperationException("Original style was not retained.");
                        recovery = " Original style restored; review any local style overrides.";
                    }
                    catch (Exception restoreError) { recovery = " Recovery needs review: " + restoreError.Message; }
                    throw new InvalidOperationException("Could not change the dimension style: " + error.Message + recovery, error);
                }
            }
        }
    }

    internal sealed class DimensionState
    {
        // Named styles can replace local presentation. Preserve the engineering
        // text, explicit tolerance amounts, display mode and manual leader position
        // on the dimension while allowing the new style to supply decimal precision.
        private static readonly string[] Fields = {
            "PrefixString", "SuffixString", "SubfixString", "SuperfixString", "SubfixString2", "OverrideString",
            "PrimaryUpperTolerance", "PrimaryLowerTolerance", "PrimaryToleranceSymbol",
            "SecondaryUpperTolerance", "SecondaryLowerTolerance", "SecondaryToleranceSymbol",
            "DisplayType", "TrackDistance", "TrackAngle", "BreakPosition", "BreakDistance"
        };
        private readonly Dictionary<string, object> values = new Dictionary<string, object>();
        private readonly double measured;
        internal DimensionState(object dimension, params string[] excluded)
        {
            measured = (double)((dynamic)dimension).Value;
            foreach (string field in Fields)
                if (Array.IndexOf(excluded, field) < 0) values.Add(field, Read(dimension, field));
        }
        private static object Read(object target, string field)
        { return target.GetType().InvokeMember(field, BindingFlags.GetProperty, null, target, null, CultureInfo.InvariantCulture); }
        internal void RestoreAndVerify(object dimension)
        {
            foreach (var field in values)
            {
                if (!Object.Equals(Read(dimension, field.Key), field.Value))
                    dimension.GetType().InvokeMember(field.Key, BindingFlags.SetProperty, null, dimension, new[] { field.Value }, CultureInfo.InvariantCulture);
                if (!Object.Equals(Read(dimension, field.Key), field.Value))
                    throw new InvalidOperationException("Could not preserve " + field.Key + ".");
            }
            // Never assign Value: that could drive model geometry. A style change
            // must preserve the measured value and the original native dimension.
            if ((double)((dynamic)dimension).Value != measured) throw new InvalidOperationException("The measured value changed unexpectedly.");
        }
    }
}

using System;
using System.IO;

namespace SketchToModels
{
    /// <summary>
    /// O-ring parts reuse an ordered feature from the template. They share the
    /// normal in-place insertion and linked-copy steps, but never start a new
    /// revolve command or change the template feature's profile/parameters.
    /// All CAD access stays on the existing UI STA thread and message filter.
    /// </summary>
    public static class ORingWorkflow
    {
        public const string TemplateName = "O-ring Template.par";
        public const string FeatureName = "Protrusion 6";
        const int IgFeatureOK = 1216476310;

        // An empty override follows the normal template's folder, so changing
        // that folder also changes the default O-ring template location.
        public static string TemplatePath(string normalTemplate, string overridePath)
        {
            return String.IsNullOrWhiteSpace(overridePath)
                ? Path.Combine(Path.GetDirectoryName(normalTemplate) ?? "", TemplateName)
                : overridePath.Trim();
        }

        public static string TemplateFor(SketchItem item, string normalTemplate, string overridePath)
        {
            if (item.ORingStyle && item.BushingStyle)
                throw new InvalidOperationException("Choose either Bushing Style or O-ring for '" + item.Name + "'.");
            return item.ORingStyle ? TemplatePath(normalTemplate, overridePath) : normalTemplate;
        }

        /// <summary>
        /// Search the document's ORDERED history, not a hardcoded model/index.
        /// The number in the feature's name is not its collection position.
        /// SDK DesignEdgebarFeatures includes sketches and modeling features;
        /// matching Name or the displayed EdgebarName supports custom naming.
        /// Reject ambiguous names before changing feature order.
        /// </summary>
        public static object FindProtrusion(dynamic part)
        {
            dynamic history = part.DesignEdgebarFeatures;
            object found = null;
            for (int i = 1; i <= (int)history.Count; i++)
            {
                dynamic feature = history.Item(i);
                if (!String.Equals(Convert.ToString(feature.Name), FeatureName, StringComparison.OrdinalIgnoreCase)
                    && !String.Equals(Convert.ToString(feature.EdgebarName), FeatureName, StringComparison.OrdinalIgnoreCase))
                    continue;
                if (found != null)
                    throw new InvalidOperationException("More than one ordered feature is named '" + FeatureName + "' in the O-ring template.");
                found = feature;
            }
            if (found == null)
                throw new InvalidOperationException("The O-ring template must contain an ordered feature named '" + FeatureName + "'.");
            return found;
        }

        /// <summary>
        /// Call only after the assembly has copied its sketch and the new part
        /// has been reopened for editing. Reacquire all objects from that open
        /// document; wrappers retained across Close/Open can be disconnected.
        /// SDK Reorder(target, false) moves THIS feature AFTER the target.
        /// Read the updated history back to catch a no-op/rejected reorder.
        /// Local COM wrappers are left to the batch's existing cleanup pass;
        /// force-releasing aliases can invalidate Solid Edge's shared wrappers.
        /// </summary>
        public static void Finish(dynamic part, int initialSketchCount)
        {
            dynamic sketches = part.Sketches;
            if ((int)sketches.Count != initialSketchCount + 1)
                throw new InvalidOperationException("Expected exactly one new linked O-ring sketch; cannot safely choose the reorder target.");
            dynamic sketch = sketches.Item(initialSketchCount + 1);
            dynamic protrusion = FindProtrusion(part);
            string sketchId = Convert.ToString(sketch.SystemName);
            string featureId = Convert.ToString(protrusion.SystemName);
            if (String.IsNullOrEmpty(sketchId) || String.IsNullOrEmpty(featureId) || sketchId == featureId)
                throw new InvalidOperationException("Cannot identify the copied sketch and template protrusion in the ordered history.");

            protrusion.Reorder((object)sketch, false);

            dynamic history = part.DesignEdgebarFeatures;
            int sketchPosition = 0, featurePosition = 0;
            for (int i = 1; i <= (int)history.Count; i++)
            {
                dynamic feature = history.Item(i);
                string id = Convert.ToString(feature.SystemName);
                if (id == sketchId) sketchPosition = i;
                if (id == featureId) featurePosition = i;
            }
            if (sketchPosition == 0 || featurePosition != sketchPosition + 1)
                throw new InvalidOperationException("Solid Edge did not place '" + FeatureName + "' immediately after the copied sketch.");
            if (Convert.ToInt32(protrusion.Status) != IgFeatureOK)
                throw new InvalidOperationException("'" + FeatureName + "' reports a feature error after reordering. Inspect the new O-ring part before retrying.");
        }
    }
}

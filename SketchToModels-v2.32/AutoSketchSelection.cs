using System;

namespace SketchToModels
{
    /// <summary>
    /// Optional preselection for the existing Project to Sketch command. This
    /// class never creates geometry, closes a profile, accepts a projection or
    /// finishes a feature. Solid Edge must consume the selection in its native
    /// command, and the user still reviews/accepts the result. In particular,
    /// SelectSet membership is NOT treated as proof of an associative projection.
    /// </summary>
    public sealed class AutoSketchSelection
    {
        readonly int sourceIndex;
        bool attempted;
        public bool Enabled { get; private set; }
        public string Message { get; private set; }

        public AutoSketchSelection(int copiedSketchIndex, bool enabled)
        {
            sourceIndex = copiedSketchIndex;
            Enabled = enabled;
            Message = "";
        }

        /// <summary>
        /// Run once, on the same STA as the feature/profile command, immediately
        /// before StartCommand(Project to Sketch). Use the exact sketch appended
        /// by CopySketch, not the assembly layout, the first template sketch or
        /// ActiveSketch (which is the new revolve's destination profile).
        /// Reacquiring from the reopened part avoids retaining a disconnected RCW.
        /// Failure falls back to the existing manual workflow; no model edits or
        /// repeated timer attempts are made. The existing batch cleanup owns RCWs.
        /// </summary>
        public void Prepare(dynamic part, int expectedSketchCount)
        {
            if (!Enabled || attempted) return;
            attempted = true;
            bool selectionTouched = false;
            try
            {
                if (sourceIndex < 1 || sourceIndex > expectedSketchCount ||
                    (int)part.Sketches.Count != expectedSketchCount)
                    throw new InvalidOperationException("The copied sketch cannot be identified safely.");
                dynamic source = part.Sketches.Item(sourceIndex);
                // This must be the standalone linked Sketch, never a Profile.
                if ((int)source.Type != 1689979564)
                    throw new InvalidOperationException("The source is not a standalone sketch.");
                dynamic selected = part.SelectSet;
                selectionTouched = true;
                selected.RemoveAll();
                selected.Add((object)source);
                if ((int)selected.Count != 1)
                    throw new InvalidOperationException("Solid Edge did not retain the sketch selection.");
                Message = "Automatic source selection requested. Check the outline preview and accept it. " +
                    "If nothing is projected, click Select manually and pick the original outline.";
            }
            catch (Exception error)
            {
                Enabled = false;
                // Only clear a selection we tried to change. A failure before
                // that point must not remove a selection the user already made.
                if (selectionTouched) try { part.SelectSet.RemoveAll(); } catch { }
                Message = "Automatic selection unavailable: " + error.Message +
                    " Pick the original outline manually and accept.";
            }
        }

        public void UseManual(dynamic part)
        {
            // The user explicitly requested a manual restart. Forget the
            // automatic preselection; never reselect it on later timer ticks.
            Enabled = false;
            attempted = true;
            Message = "Manual mode: pick the original outline and accept.";
            part.SelectSet.RemoveAll();
        }
    }
}

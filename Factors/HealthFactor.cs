﻿namespace KerbalHealth
{
    public abstract class HealthFactor
    {
        bool enabledInEditor = true;

        /// <summary>
        /// Internal name of the factor
        /// </summary>
        abstract public string Name { get; }

        /// <summary>
        /// Display name of the factor
        /// </summary>
        virtual public string Title => Name;

        /// <summary>
        /// This factor doesn't change for unloaded vessels (or can't be recalculated for them)
        /// </summary>
        virtual public bool ConstantForUnloaded => true;

        /// <summary>
        /// Returns factor's HP change per day as set in the Settings (use KerbalHealthFactorsSettings.[factorName])
        /// </summary>
        abstract public double BaseChangePerDay { get; }

        public HealthFactor() => ResetEnabledInEditor();

        /// <summary>
        /// Is the factor considered when calculating estimated HP change in Health Report
        /// </summary>
        public bool IsEnabledInEditor() => enabledInEditor;

        public void SetEnabledInEditor(bool state) => enabledInEditor = state;

        virtual public void ResetEnabledInEditor() => SetEnabledInEditor(true);

        /// <summary>
        /// Returns actual factor's HP change per day for a given kerbal, before factor multipliers
        /// </summary>
        /// <param name="khs"></param>
        /// <returns></returns>
        abstract public double ChangePerDay(KerbalHealthStatus khs);
    }
}

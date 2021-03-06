﻿using KSP.Localization;

namespace KerbalHealth
{
    class HomeFactor : HealthFactor
    {
        public override string Name => "Home";

        public override string Title => Localizer.Format("#KH_Factor_Home");//Home

        public override void ResetEnabledInEditor() => SetEnabledInEditor(false);

        public override double BaseChangePerDay => KerbalHealthFactorsSettings.Instance.HomeFactor;

        public override double ChangePerDay(KerbalHealthStatus khs)
        {
            if (Core.IsInEditor)
                return IsEnabledInEditor() ? BaseChangePerDay : 0;
            if (khs.PCM.rosterStatus != ProtoCrewMember.RosterStatus.Assigned)
            {
                Core.Log("Home factor is off when kerbal is not assigned.");
                return 0;
            }
            Vessel vessel = khs.PCM.GetVessel();
            CelestialBody body = vessel?.mainBody;
            if (body == null)
            {
                Core.Log($"Could not find main body for {khs.Name}.", LogLevel.Error);
                return 0;
            }
            if (body.isHomeWorld && (vessel.altitude < body.scienceValues.flyingAltitudeThreshold))
            {
                Core.Log("Home factor is on.");
                return BaseChangePerDay;
            }
            Core.Log($"Home factor is off. Main body: {body.name}; altitude: {vessel.altitude:N0}.");
            return 0;
        }
    }
}

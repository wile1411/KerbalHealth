using System;
using System.Collections.Generic;
using System.Linq;
using KSP.Localization;

namespace KerbalHealth
{
    public class ModuleKerbalHealth : PartModule, IResourceConsumer
    {
        [KSPField]
        List<ModuleConfiguration> configs = new List<ModuleConfiguration>(1);

        [KSPField]
        ModuleConfiguration defaultConfig;

        [KSPField]
        // 0 if no training needed for this part, 1 for standard training complexity
        public float complexity = 0;

        [KSPField(isPersistant = true)]
        public uint id = 0;

        [KSPField(isPersistant = true, guiName = "Health Module")]
        string title = "";

        [KSPField(isPersistant = true)]
        public int configIndex = 0;

        [KSPField(isPersistant = true)]
        // If not alwaysActive, this determines if the module is active
        public bool isActive = true;

        [KSPField(isPersistant = true)]
        // Determines if the module is disabled due to the lack of the resource
        public bool starving = false;

        [KSPField(guiName = "", guiActive = true, guiActiveEditor = true, guiUnits = "#KH_Module_ecPersec")] // /sec
        // Electric Charge usage per second
        public float ecPerSec = 0;

        double lastUpdated;

        public ModuleConfiguration Configuration => part.partInfo.partPrefab.FindModuleImplementing<ModuleKerbalHealth>().defaultConfig;//configs[configIndex]; // configs.Count > configIndex ? configs[configIndex] : null;

        public bool IsAlwaysActive => (Configuration.resourceConsumption == 0) && (Configuration.resourceConsumptionPerKerbal == 0);

        public bool IsModuleActive => IsAlwaysActive || (isActive && (!Core.IsInEditor || KerbalHealthEditorReport.HealthModulesEnabled) && !starving);

        /// <summary>
        /// Returns total # of kerbals affected by this module
        /// </summary>
        public int TotalAffectedCrewCount
        {
            get
            {
                if (Core.IsInEditor)
                    return Configuration.partCrewOnly
                        ? ShipConstruction.ShipManifest.GetPartCrewManifest(part.craftID).GetPartCrew().Where(pcm => pcm != null).Count()
                        : ShipConstruction.ShipManifest.CrewCount;
                if (vessel == null || part?.protoModuleCrew == null)
                {
                    Core.Log($"TotalAffectedCrewCount: vessel: {vessel?.vesselName ?? "NULL"}; part: {part?.partName ?? "NULL"}; protoModuleCrew: {(part?.protoModuleCrew ?? new List<ProtoCrewMember>()).Count()} members.", LogLevel.Error);
                    return 0;
                }
                return Configuration.partCrewOnly ? part.protoModuleCrew.Count : vessel.GetCrewCount();
            }
        }

        /// <summary>
        /// Returns # of kerbals affected by this module, capped by crewCap
        /// </summary>
        public int CappedAffectedCrewCount => Configuration.crewCap > 0 ? Math.Min(TotalAffectedCrewCount, Configuration.crewCap) : TotalAffectedCrewCount;

        public List<PartResourceDefinition> GetConsumedResources() =>
            (Configuration.resourceConsumption != 0 || Configuration.resourceConsumptionPerKerbal != 0)
            ? new List<PartResourceDefinition>() { Configuration.ResourceDefinition }
            : new List<PartResourceDefinition>();

        public float TotalResourceConsumption => Configuration.resourceConsumption + Configuration.resourceConsumptionPerKerbal * CappedAffectedCrewCount;

        public double RecuperationPower =>
            Configuration.crewCap > 0 ? Configuration.recuperation * Math.Min((double)Configuration.crewCap / TotalAffectedCrewCount, 1) : Configuration.recuperation;

        public double DecayPower =>
            Configuration.crewCap > 0 ? Configuration.decay * Math.Min((double)Configuration.crewCap / TotalAffectedCrewCount, 1) : Configuration.decay;

        int numLoaded = 0;

        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);
            numLoaded++;
            if (Core.IsInEditor)
                Core.Log($"ModuleKerbalHealth.OnLoad with this node: {node}");
            defaultConfig = new ModuleConfiguration(node);
            //configs.Clear();
            //if (node.HasNode(ModuleConfiguration.ConfigNodeName))
            //    configs.AddRange(node.GetNodes(ModuleConfiguration.ConfigNodeName).Select(n => new ModuleConfiguration(n)));
            //else
            //{
            //    configs.Add(new ModuleConfiguration(node));
            //    //Core.Log($"Only one configuration found: {configs.Last()}");
            //}
        }

        public override void OnSave(ConfigNode node)
        {
            base.OnSave(node);
            Core.Log("ModuleKerbalHealth.OnSave");
            //Core.Log($"This module has been loaded {numLoaded} times.");
            //foreach (ModuleConfiguration config in configs)
            //{
            //    Core.Log($"Saving configuration for {config.Title}.");
            //    node.AddNode(config.ConfigNode);
            //}
        }

        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            Core.Log($"ModuleKerbalHealth.OnStart({state}) for {part.name}");
            Core.Log($"This module has been loaded {numLoaded} times.");
            Core.Log($"Module has {configs.Count} configurations:");
            Core.Log($"Part prefab has {part.partInfo.partPrefab.FindModuleImplementing<ModuleKerbalHealth>().configs.Count} configs.");
            for (int i = 0; i < configs.Count; i++)
                Core.Log($"Config {i + 1}. {configs[i]}");
            Core.Log($"Configuration index is {configIndex}.");
            if ((complexity != 0) && (id == 0))
                id = part.persistentId;
            if (IsAlwaysActive)
            {
                isActive = true;
                Events["OnToggleActive"].guiActive = false;
                Events["OnToggleActive"].guiActiveEditor = false;
            }
            if (Core.IsInEditor && (Configuration.resource == "ElectricCharge"))
                ecPerSec = TotalResourceConsumption;
            Fields["ecPerSec"].guiName = Localizer.Format("#KH_Module_ECUsage", Title); // + EC Usage:
            UpdateGUIName();
            lastUpdated = Planetarium.GetUniversalTime();
        }

        public void FixedUpdate()
        {
            if (Core.IsInEditor || !KerbalHealthGeneralSettings.Instance.modEnabled)
                return;
            double time = Planetarium.GetUniversalTime();
            if (isActive && ((Configuration.resourceConsumption != 0) || (Configuration.resourceConsumptionPerKerbal != 0)))
            {
                ecPerSec = TotalResourceConsumption;
                double requiredAmount = ecPerSec * (time - lastUpdated), providedAmount;
                if (Configuration.resource != "ElectricCharge")
                    ecPerSec = 0;
                starving = (providedAmount = vessel.RequestResource(part, Configuration.ResourceDefinition.id, requiredAmount, false)) * 2 < requiredAmount;
                if (starving)
                    Core.Log($"{Title} Module is starving of {Configuration.resource} ({requiredAmount} needed, {providedAmount} provided).");
            }
            else ecPerSec = 0;
            lastUpdated = time;
        }

        /// <summary>
        /// Kerbalism background processing compatibility method
        /// </summary>
        /// <param name="v"></param>
        /// <param name="part_snapshot"></param>
        /// <param name="module_snapshot"></param>
        /// <param name="proto_part_module"></param>
        /// <param name="proto_part"></param>
        /// <param name="availableResources"></param>
        /// <param name="resourceChangeRequest"></param>
        /// <param name="elapsed_s"></param>
        /// <returns></returns>
        public static string BackgroundUpdate(Vessel v, ProtoPartSnapshot part_snapshot, ProtoPartModuleSnapshot module_snapshot, PartModule proto_part_module, Part proto_part, Dictionary<string, double> availableResources, List<KeyValuePair<string, double>> resourceChangeRequest, double elapsed_s)
        {
            if (!KerbalHealthGeneralSettings.Instance.modEnabled)
                return null;
            ModuleKerbalHealth mkh = proto_part_module as ModuleKerbalHealth;
            if (mkh.isActive && ((mkh.Configuration.resourceConsumption != 0) || (mkh.Configuration.resourceConsumptionPerKerbal != 0)))
            {
                mkh.part = proto_part;
                mkh.part.vessel = v;
                mkh.ecPerSec = mkh.TotalResourceConsumption;
                double requiredAmount = mkh.ecPerSec * elapsed_s;
                if (mkh.Configuration.resource != "ElectricCharge")
                    mkh.ecPerSec = 0;
                availableResources.TryGetValue(mkh.Configuration.resource, out double availableAmount);
                if (availableAmount <= 0)
                {
                    Core.Log($"{mkh.Title} Module is starving of {mkh.Configuration.resource} ({requiredAmount} @ {mkh.ecPerSec} EC/sec needed, {availableAmount} available.");
                    mkh.starving = true;
                }
                resourceChangeRequest.Add(new KeyValuePair<string, double>(mkh.Configuration.resource, -mkh.ecPerSec));
            }
            else mkh.ecPerSec = 0;
            return mkh.Title.ToLower();
        }

        /// <summary>
        /// Kerbalism Planner compatibility method
        /// </summary>
        /// <param name="resources">A list of resource names and production/consumption rates. Production is a positive rate, consumption is negatvie. Add all resources your module is going to produce/consume.</param>
        /// <param name="body">The currently selected body in the Kerbalism planner</param>
        /// <param name="environment">Environment variables guesstimated by Kerbalism, based on the current selection of body and vessel situation. See above.</param>
        /// <returns>The title to display in the tooltip of the planner UI.</returns>
        public string PlannerUpdate(List<KeyValuePair<string, double>> resources, CelestialBody body, Dictionary<string, double> environment)
        {
            if (!KerbalHealthGeneralSettings.Instance.modEnabled || !isActive || IsAlwaysActive)
                return null;
            resources.Add(new KeyValuePair<string, double>(Configuration.resource, -ecPerSec));
            return Title.ToLower();
        }

        public string Title => Configuration.Title;

        void UpdateGUIName()
        {
            Core.Log("UpdateGUIName");
            Events["OnToggleActive"].guiName = Localizer.Format(isActive ? "#KH_Module_Disable" : "#KH_Module_Enable", Title);//"Disable ""Enable "
            Events["OnSwitchConfiguration"].guiActive = configs.Count > 1;
            Fields["title"].SetValue(Title, this);
            Fields["ecPerSec"].guiActive = Fields["ecPerSec"].guiActiveEditor = KerbalHealthGeneralSettings.Instance.modEnabled && isActive && ecPerSec != 0;
        }
        
        [KSPEvent(name = "OnToggleActive", guiActive = true, guiName = "#KH_Module_Toggle", guiActiveEditor = true)] //Toggle Health Module
        public void OnToggleActive()
        {
            isActive = IsAlwaysActive || !isActive;
            UpdateGUIName();
        }

        [KSPEvent(name = "OnSwitchConfiguration", guiName = "Switch Health Module", guiActive = false, guiActiveEditor = true)]
        public void OnSwitchConfiguration()
        {
            Core.Log($"ModuleKerbalHealth.OnSwitchConfiguration for {part.partName}");
            Core.Log($"Old config # {configIndex} / {configs.Count}. Configuration: {Configuration.Title}.");
            configIndex = (configIndex + 1) % configs.Count;
            Core.Log($"New config # {configIndex} / {configs.Count}. Configuration: {Configuration.Title}.");
        }

        public override string GetInfo()
        {
            if (configs.Count == 0)
                return "";
            string res = "";
            if (configs.Count == 1)
                res = $"{configs[0]}\n";
            else
            {
                for (int i = 0; i < configs.Count; i++)
                    res += $"<b>Configuration #{i + 1}</b>\n{configs[i]}\n\n";
            }
            if (complexity != 0)
                res += Localizer.Format("#KH_Module_info11", (complexity * 100).ToString("N0"));// "\nTraining complexity: " + (complexity * 100).ToString("N0") + "%"
            return res.Trim();
        }
    }
}

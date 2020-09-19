using KSP.Localization;

namespace KerbalHealth
{
    public class ModuleConfiguration : IConfigNode
    {
        public const string ConfigNodeName = "CONFIG";

        // Module title displayed in right-click menu (empty string for auto)
        public string title = "";

        // How many raw HP per day every affected kerbal gains
        public float hpChangePerDay = 0;

        // Will increase HP by this % of (MaxHP - HP) per day
        public float recuperation = 0;

        // Will decrease by this % of (HP - MinHP) per day
        public float decay = 0;

        // Does the module affect health of only crew in this part or the entire vessel?
        public bool partCrewOnly = false;

        // Name of factor whose effect is multiplied
        public string multiplyFactor = "All";

        // How the factor is changed (e.g., 0.5 means factor's effect is halved)
        public float multiplier = 1;

        // Max crew this module's multiplier applies to without penalty, 0 for unlimited (a.k.a. free multiplier)
        public int crewCap = 0;

        // Points of living space provided by the part (used to calculate Confinement factor)
        public double space = 0;

        // Number of halving-thicknesses
        public float shielding = 0;

        // Radioactive emission, bananas/day
        public float radioactivity = 0;

        // Determines, which resource is consumed by the module
        public string resource = "ElectricCharge";

        // Flat EC consumption (units per second)
        public float resourceConsumption = 0;

        // EC consumption per affected kerbal (units per second)
        public float resourceConsumptionPerKerbal = 0;

        public string Title
        {
            get
            {
                if (!string.IsNullOrEmpty(title))
                    return title;
                if (recuperation > 0)
                    return Localizer.Format("#KH_Module_type1");//"R&R"
                if (decay > 0)
                    return Localizer.Format("#KH_Module_type2");//"Health Poisoning"
                switch (multiplyFactor.ToLowerInvariant())
                {
                    case "stress":
                        return Localizer.Format("#KH_Module_type3");  //"Stress Relief"
                    case "confinement":
                        return Localizer.Format("#KH_Module_type4");//"Comforts"
                    case "loneliness":
                        return Localizer.Format("#KH_Module_type5");//"Meditation"
                    case "microgravity":
                        return (multiplier <= 0.25) ? Localizer.Format("#KH_Module_type6") : Localizer.Format("#KH_Module_type7");//"Paragravity""Exercise Equipment"
                    case "connected":
                        return Localizer.Format("#KH_Module_type8");//"TV Set"
                    case "conditions":
                        return Localizer.Format("#KH_Module_type9");//"Sick Bay"
                }
                if (space > 0)
                    return Localizer.Format("#KH_Module_type10");//"Living Quarters"
                if (shielding > 0)
                    return Localizer.Format("#KH_Module_type11");//"RadShield"
                if (radioactivity > 0)
                    return Localizer.Format("#KH_Module_type12");//"Radiation"
                return Localizer.Format("#KH_Module_title");//"Health Module"
            }
        }

        public PartResourceDefinition ResourceDefinition
        {
            get => PartResourceLibrary.Instance.GetDefinition(resource);
            set => resource = value?.name;
        }

        public void Save(ConfigNode node)
        {
            //ConfigNode node = new ConfigNode(ConfigNodeName);
            Core.Log($"Saving ModuleConfiguration to node: {node}");
            //node.name = ConfigNodeName;
            if (!string.IsNullOrEmpty(title))
                node.AddValue("title", title);
            if (hpChangePerDay != 0)
                node.AddValue("hpChangePerDay", hpChangePerDay);
            if (recuperation != 0)
                node.AddValue("recuperation", recuperation);
            if (decay != 0)
                node.AddValue("decay", decay);
            if (partCrewOnly)
                node.AddValue("partCrewOnly", true);
            if (multiplier != 1)
            {
                node.AddValue("multiplyFactor", multiplyFactor ?? "All");
                node.AddValue("multiplier", multiplier);
            }
            if (crewCap > 0)
                node.AddValue("crewCap", crewCap);
            if (space != 0)
                node.AddValue("space", space);
            if (shielding != 0)
                node.AddValue("shielding", shielding);
            if (radioactivity != 0)
                node.AddValue("radioactivity", radioactivity);
            if (resource != "ElectricCharge" && (resourceConsumption != 0 || resourceConsumptionPerKerbal != 0))
                node.AddValue("resource", resource);
            if (resourceConsumption != 0)
                node.AddValue("resourceConsumption", resourceConsumption);
            if (resourceConsumptionPerKerbal != 0)
                node.AddValue("resourceConsumptionPerKerbal", resourceConsumptionPerKerbal);
            Core.Log($"Resulting node: {node}");
        }

        public void Load(ConfigNode node)
        {
            //Core.Log($"ModuleConfiguration.Load() from node: {node}");
            title = node.GetString("title", "");
            hpChangePerDay = node.GetFloat("hpChangePerDay");
            recuperation = node.GetFloat("recuperation");
            decay = node.GetFloat("decay");
            partCrewOnly = node.GetBool("partCrewOnly");
            multiplyFactor = node.GetString("multiplyFactor", "All");
            multiplier = node.GetFloat("multiplier", 1);
            crewCap = node.GetInt("crewCap");
            space = node.GetFloat("space");
            shielding = node.GetFloat("shielding");
            radioactivity = node.GetFloat("radioactivity");
            resource = node.GetString("resource", "ElectricCharge");
            resourceConsumption = node.GetFloat("resourceConsumption");
            resourceConsumptionPerKerbal = node.GetFloat("resourceConsumptionPerKerbal");
        }

        public ModuleConfiguration()
        { }

        public ModuleConfiguration(ConfigNode node) => Load(node); 

        public override string ToString()
        {
            string res = "";
            if (hpChangePerDay != 0)
                res = Localizer.Format("#KH_Module_info1", hpChangePerDay.ToString("F1"));//"\nHealth points: " +  + "/day"
            if (recuperation != 0)
                res += Localizer.Format("#KH_Module_info2", recuperation.ToString("F1"));//"\nRecuperation: " +  + "%/day"
            if (decay != 0)
                res += Localizer.Format("#KH_Module_info3", decay.ToString("F1"));//"\nHealth decay: " +  + "%/day"
            if (multiplier != 1)
                res += Localizer.Format("#KH_Module_info4", multiplier.ToString("F2"), multiplyFactor);//"\n" +  + "x " +
            if (crewCap > 0)
                res += Localizer.Format("#KH_Module_info5", crewCap);//" for up to " +  + " kerbals
            if (space != 0)
                res += Localizer.Format("#KH_Module_info6", space.ToString("F1"));//"\nSpace: " +
            if (resourceConsumption != 0)
                res += Localizer.Format("#KH_Module_info7", ResourceDefinition.abbreviation, resourceConsumption.ToString("F2"));//"\n" +  + ": " +  + "/sec."
            if (resourceConsumptionPerKerbal != 0)
                res += Localizer.Format("#KH_Module_info8", ResourceDefinition.abbreviation, resourceConsumptionPerKerbal.ToString("F2"));//"\n" +  + " per Kerbal: " +  + "/sec."
            if (shielding != 0)
                res += Localizer.Format("#KH_Module_info9", shielding.ToString("F1"));//"\nShielding rating: " +
            if (radioactivity != 0)
                res += Localizer.Format("#KH_Module_info10", radioactivity.ToString("N0"));//"\nRadioactive emission: " +  + "/day"
            if (string.IsNullOrEmpty(res))
                return "";
            return Localizer.Format("#KH_Module_typetitle", Title) + res;//"Module type: " +
        }
    }
}

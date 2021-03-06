﻿using System.Collections.Generic;
using System.IO;
using UnityEngine;
using KSP.UI.Screens;
using KSP.Localization;
using System.Linq;

namespace KerbalHealth
{
    [KSPAddon(KSPAddon.Startup.EditorAny, false)]
    class KerbalHealthEditorReport : MonoBehaviour
    {
        ApplicationLauncherButton appLauncherButton;
        IButton toolbarButton;
        bool dirty = false;
        Rect reportPosition = new Rect(0.5f, 0.5f, 420, 50);
        
        // Health Report window
        PopupDialog reportWindow;
        
        // Health Report grid's labels
        System.Collections.Generic.List<DialogGUIBase> gridContent;
        
        DialogGUILabel spaceLbl, recupLbl, shieldingLbl, exposureLbl, shelterExposureLbl;

        // # of columns in Health Report
        int colNum = 4;
        
        static bool healthModulesEnabled = true;
        static bool trainingEnabled = true;

        public void Start()
        {
            if (!KerbalHealthGeneralSettings.Instance.modEnabled)
                return;
            Core.Log("KerbalHealthEditorReport.Start", LogLevel.Important);

            GameEvents.onEditorShipModified.Add(_ => Invalidate());
            GameEvents.onEditorPodDeleted.Add(Invalidate);
            GameEvents.onEditorScreenChange.Add(_ => Invalidate());

            if (KerbalHealthGeneralSettings.Instance.ShowAppLauncherButton)
            {
                Core.Log("Registering AppLauncher button...");
                Texture2D icon = new Texture2D(38, 38);
                icon.LoadImage(System.IO.File.ReadAllBytes(Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "icon.png")));
                appLauncherButton = ApplicationLauncher.Instance.AddModApplication(DisplayData, UndisplayData, null, null, null, null, ApplicationLauncher.AppScenes.ALWAYS, icon);
            }

            if (ToolbarManager.ToolbarAvailable)
            {
                Core.Log("Registering Toolbar button...");
                toolbarButton = ToolbarManager.Instance.add("KerbalHealth", "HealthReport");
                toolbarButton.Text = Localizer.Format("#KH_ER_ButtonTitle");
                toolbarButton.TexturePath = "KerbalHealth/toolbar";
                toolbarButton.ToolTip = "Kerbal Health";
                toolbarButton.OnClick += e =>
                {
                    if (reportWindow == null)
                        DisplayData();
                    else UndisplayData();
                };
            }

            Core.KerbalHealthList.RegisterKerbals();

            Core.Log("KerbalHealthEditorReport.Start finished.", LogLevel.Important);
        }

        public void DisplayData()
        {
            Core.Log("KerbalHealthEditorReport.DisplayData");
            if ((ShipConstruction.ShipManifest == null) || !ShipConstruction.ShipManifest.HasAnyCrew())
            {
                Core.Log("The ship construction is null.", LogLevel.Important);
                return;
            }

            gridContent = new List<DialogGUIBase>((Core.KerbalHealthList.Count + 1) * colNum)
            {
                // Creating column titles
                 new DialogGUILabel($"<b><color=\"white\">{Localizer.Format("#KH_ER_Name")}</color></b>", true),//Name
                 new DialogGUILabel($"<b><color=\"white\">{Localizer.Format("#KH_ER_Trend")}</color></b>", true),//Trend
                 new DialogGUILabel($"<b><color=\"white\">{Localizer.Format("#KH_ER_MissionTime")}</color></b>", true),//Mission Time
                 new DialogGUILabel($"<b><color=\"white\">{Localizer.Format("#KH_ER_TrainingTime")}</color></b>", true)//Training Time
            };

            // Initializing Health Report's grid with empty labels, to be filled in Update()
            for (int i = 0; i < ShipConstruction.ShipManifest.CrewCount * colNum; i++)
                gridContent.Add(new DialogGUILabel("", true));

            // Preparing factors checklist
            List<DialogGUIToggle> checklist = new List<DialogGUIToggle>();
            checklist.AddRange(Core.Factors.Select(f => new DialogGUIToggle(f.IsEnabledInEditor, f.Title, state =>
            {
                f.SetEnabledInEditor(state);
                Invalidate();
            })));
            if (KerbalHealthFactorsSettings.Instance.TrainingEnabled)
                checklist.Add(new DialogGUIToggle(trainingEnabled, Localizer.Format("#KH_ER_Trained"), state =>
                {
                    trainingEnabled = state;
                    Invalidate();
                }));
            checklist.Add(new DialogGUIToggle(healthModulesEnabled, Localizer.Format("#KH_ER_HealthModules"), state =>
            {
                healthModulesEnabled = state;
                Invalidate();
            }));

            reportWindow = PopupDialog.SpawnPopupDialog(
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new MultiOptionDialog(
                    "HealthReport",
                    "",
                    Localizer.Format("#KH_ER_Windowtitle"),//Health Report
                    HighLogic.UISkin,
                    reportPosition,
                    new DialogGUIGridLayout(
                        new RectOffset(3, 3, 3, 3),
                        new Vector2(90, 30),
                        new Vector2(10, 0),
                        UnityEngine.UI.GridLayoutGroup.Corner.UpperLeft,
                        UnityEngine.UI.GridLayoutGroup.Axis.Horizontal,
                        TextAnchor.MiddleCenter,
                        UnityEngine.UI.GridLayoutGroup.Constraint.FixedColumnCount,
                        colNum,
                        gridContent.ToArray()),
                    new DialogGUIHorizontalLayout(
                        new DialogGUILabel($"<color=\"white\">{Localizer.Format("#KH_ER_Space")}</color>", false),//Space: 
                        spaceLbl = new DialogGUILabel(Localizer.Format("#KH_NA"), true),
                        new DialogGUILabel($"<color=\"white\">{Localizer.Format("#KH_ER_Recuperation")}</color>", false),//Recuperation: 
                        recupLbl = new DialogGUILabel(Localizer.Format("#KH_NA"), true)),
                    new DialogGUIHorizontalLayout(
                        new DialogGUILabel($"<color=\"white\">{Localizer.Format("#KH_ER_Shielding")}</color>", false),//Shielding: 
                        shieldingLbl = new DialogGUILabel(Localizer.Format("#KH_NA"), true),
                        new DialogGUILabel($"<color=\"white\">{Localizer.Format("#KH_ER_Exposure")}</color>", false),
                        exposureLbl = new DialogGUILabel(Localizer.Format("#KH_NA"), true),
                        new DialogGUILabel($"<color=\"white\">{Localizer.Format("#KH_ER_ShelterExposure")}</color>", false),
                        shelterExposureLbl = new DialogGUILabel(Localizer.Format("#KH_NA"), true)),
                    new DialogGUIHorizontalLayout(
                        new DialogGUILabel("", true),
                        new DialogGUILabel(Localizer.Format("#KH_ER_Factors"), true),
                        new DialogGUIButton(Localizer.Format("#KH_ER_Train"), OnTrainButtonSelected, () => KerbalHealthFactorsSettings.Instance.TrainingEnabled, false),
                        new DialogGUIButton(Localizer.Format("#KH_ER_Reset"), OnResetButtonSelected, false)),
                    new DialogGUIGridLayout(
                        new RectOffset(3, 3, 3, 3),
                        new Vector2(130, 30),
                        new Vector2(10, 0),
                        UnityEngine.UI.GridLayoutGroup.Corner.UpperLeft,
                        UnityEngine.UI.GridLayoutGroup.Axis.Horizontal,
                        TextAnchor.MiddleCenter,
                        UnityEngine.UI.GridLayoutGroup.Constraint.FixedColumnCount,
                        3,
                        checklist.ToArray())),
                false,
                HighLogic.UISkin,
                false);
            Invalidate();
        }

        public static bool HealthModulesEnabled => healthModulesEnabled;

        public static bool TrainingEnabled => trainingEnabled;

        public void OnResetButtonSelected()
        {
            foreach (HealthFactor f in Core.Factors)
                f.ResetEnabledInEditor();
            healthModulesEnabled = true;
            trainingEnabled = true;
            Invalidate();
        }

        public void OnTrainButtonSelected()
        {
            Core.Log("OnTrainButtonSelected");
            if (!KerbalHealthFactorsSettings.Instance.TrainingEnabled)
                return;

            List<string> s = new List<string>();
            List<string> f = new List<string>();
            foreach (KerbalHealthStatus khs in ShipConstruction.ShipManifest.GetAllCrew(false)
                .Select(pcm => Core.KerbalHealthList[pcm])
                .Where(khs => khs != null))
                if (khs.CanTrainAtKSC)
                {
                    khs.StartTraining(EditorLogic.SortedShipList, EditorLogic.fetch.ship.shipName);
                    khs.AddCondition("Training");
                    s.Add(khs.Name);
                }
                else
                {
                    Core.Log($"{khs.Name} can't train. They are {khs.PCM.rosterStatus} and at {khs.Health:P1} health.", LogLevel.Important);
                    f.Add(khs.Name);
                }

            string msg = "";
            if (s.Count > 0)
                if (s.Count == 1)
                    msg = Localizer.Format("#KH_ER_KerbalStartedTraining", s[0]); // + " started training.
                else
                {
                    msg = Localizer.Format("#KH_ER_KerbalsStartedTraining"); //The following kerbals started training:
                    foreach (string k in s)
                        msg += $"\r\n- {k}";
                }

            if (f.Count > 0)
            {
                if (msg.Length != 0)
                    msg += "\r\n\n";
                if (f.Count == 1)
                    msg += $"<color=\"red\">{Localizer.Format("#KH_ER_KerbalCantTrain", f[0])}"; //* can't train.
                else
                {
                    msg += $"<color=\"red\">{Localizer.Format("#KH_ER_KerbalsCantTrain")}";  //The following kerbals can't train:
                    foreach (string k in f)
                        msg += $"\r\n- {k}";
                }
                msg += "</color>";
            }
            Core.ShowMessage(msg, false);
        }

        public void UndisplayData()
        {
            if (reportWindow != null)
            {
                Vector3 v = reportWindow.RTrf.position;
                reportPosition = new Rect(v.x / Screen.width + 0.5f, v.y / Screen.height + 0.5f, 420, 50);
                reportWindow.Dismiss();
            }
        }

        double TrainingTime(KerbalHealthStatus khs, List<ModuleKerbalHealth> modules) =>
            modules.Sum(mkh => (Core.TrainingCap - khs.TrainingLevelForPart(mkh.id)) * khs.GetPartTrainingComplexity(mkh)) / khs.TrainingPerDay * KSPUtil.dateTimeFormatter.Day;

        public void Update()
        {
            if (!KerbalHealthGeneralSettings.Instance.modEnabled || ShipConstruction.ShipManifest == null || ShipConstruction.ShipManifest.CrewCount == 0)
            {
                if (reportWindow != null)
                    reportWindow.Dismiss();
                return;
            }

            if ((reportWindow != null) && dirty)
            {
                if (gridContent == null)
                {
                    Core.Log("gridContent is null.", LogLevel.Error);
                    return;
                }

                // # of tracked kerbals has changed => close & reopen the window
                if (gridContent.Count != (ShipConstruction.ShipManifest.CrewCount + 1) * colNum)
                {
                    Core.Log("Kerbals' number has changed. Recreating the Health Report window.", LogLevel.Important);
                    UndisplayData();
                    DisplayData();
                }

                // Fill the Health Report's grid with kerbals' health data
                int i = 0;
                KerbalHealthStatus khs = null;
                HealthEffect.VesselCache.Clear();

                List<ModuleKerbalHealth> trainingParts = Core.GetTrainingCapableParts(EditorLogic.SortedShipList);

                foreach (ProtoCrewMember pcm in ShipConstruction.ShipManifest.GetAllCrew(false).Where(pcm => pcm != null))
                {
                    khs = Core.KerbalHealthList[pcm]?.Clone();
                    if (khs == null)
                    {
                        Core.Log($"Could not create a clone of KerbalHealthStatus for {pcm.name}. It is {((Core.KerbalHealthList[pcm] == null) ? "not " : "")}found in KerbalHealthList, which contains {Core.KerbalHealthList.Count} records.", LogLevel.Error);
                        i++;
                        continue;
                    }

                    khs.SetDirty();
                    gridContent[(i + 1) * colNum].SetOptionText(khs.FullName);
                    khs.HP = khs.MaxHP;
                    // Making this call here, so that GetBalanceHP doesn't have to:
                    double changePerDay = khs.HPChangeTotal;
                    double balanceHP = khs.GetBalanceHP();
                    string s = balanceHP > 0
                        ? $"-> {balanceHP:F0} HP ({balanceHP / khs.MaxHP * 100:F0}%)"
                        : Localizer.Format("#KH_ER_HealthPerDay", changePerDay.ToString("F1")); // + " HP/day"
                    gridContent[(i + 1) * colNum + 1].SetOptionText(s);
                    s = balanceHP > khs.NextConditionHP()
                        ? "—"
                        : ((khs.Recuperation > khs.Decay) ? "> " : "") + Core.ParseUT(khs.TimeToNextCondition(), false, 100);
                    gridContent[(i + 1) * colNum + 2].SetOptionText(s);
                    gridContent[(i + 1) * colNum + 3].SetOptionText(KerbalHealthFactorsSettings.Instance.TrainingEnabled ? Core.ParseUT(TrainingTime(khs, trainingParts), false, 100) : Localizer.Format("#KH_NA"));
                    i++;
                }

                HealthEffect vesselEffects = new HealthEffect(EditorLogic.SortedShipList, ShipConstruction.ShipManifest.CrewCount);
                Core.Log($"Vessel effects: {vesselEffects}");

                spaceLbl.SetOptionText($"<color=\"white\">{vesselEffects.Space:F1}</color>");
                recupLbl.SetOptionText($"<color=\"white\">{vesselEffects.EffectiveRecuperation:F1}%</color>");
                shieldingLbl.SetOptionText($"<color=\"white\">{vesselEffects.Shielding:F1}</color>");
                exposureLbl.SetOptionText($"<color=\"white\">{vesselEffects.VesselExposure:P1}</color>");
                shelterExposureLbl.SetOptionText($"<color=\"white\">{vesselEffects.ShelterExposure:P1}</color>");

                dirty = false;
            }
        }

        public void Invalidate() => dirty = true;

        public void OnDisable()
        {
            Core.Log("KerbalHealthEditorReport.OnDisable", LogLevel.Important);
            UndisplayData();
            if (toolbarButton != null)
                toolbarButton.Destroy();
            if ((appLauncherButton != null) && (ApplicationLauncher.Instance != null))
                ApplicationLauncher.Instance.RemoveModApplication(appLauncherButton);
            Core.Log("KerbalHealthEditorReport.OnDisable finished.");
        }
    }
}

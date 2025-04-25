using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.IO;
using KSP.UI.Screens;
using KSP.Localization;
using System.IO;

namespace Blueshift
{
    [KSPAddon(KSPAddon.Startup.TrackingStation, false)]
    public class BlueshiftExportApp : MonoBehaviour
    {
        static protected ApplicationLauncherButton appLauncherButton = null;
        static public Texture2D appIcon = null;

        public void Awake()
        {
            appIcon = GameDatabase.Instance.GetTexture("WildBlueIndustries/Blueshift/Icons/CopyIcon", false);
            GameEvents.onGUIApplicationLauncherReady.Add(SetupGUI);
        }

        public void OnDestroy()
        {
            if (appLauncherButton != null)
            {
                ApplicationLauncher.Instance.RemoveModApplication(appLauncherButton);
                appLauncherButton = null;
            }
            GameEvents.onGUIApplicationLauncherReady.Remove(SetupGUI);
        }

        private void SetupGUI()
        {
            // Remove previous button.
            if (appLauncherButton != null)
            {
                ApplicationLauncher.Instance.RemoveModApplication(appLauncherButton);
                appLauncherButton = null;
            }

            appLauncherButton = ApplicationLauncher.Instance.AddModApplication(exportSelectedVessel, exportSelectedVessel, null, null, null, null, ApplicationLauncher.AppScenes.ALWAYS, appIcon);
        }

        private void exportSelectedVessel()
        {
            Vessel selectedVessel = SpaceTracking.Instance.SelectedVessel;
            if (selectedVessel == null)
            {
                if (BlueshiftScenario.debugMode)
                    Debug.Log("[Blueshift] - No vessel selected, cannot export the vessel.");
                return;
            }

            // Setup the file path
            string vesselName = selectedVessel.vesselName;
            vesselName = vesselName.Replace(".", "_");
            string dir = $"{KSPUtil.ApplicationRootPath}saves/{HighLogic.SaveFolder}/Blueshift/";
            string filePath = $"{dir}/{vesselName}.ship";

            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            if (System.IO.File.Exists(filePath))
                System.IO.File.Delete(filePath);

            // Generate vessel node
            ConfigNode vesselNode = new ConfigNode("VESSEL");
            ProtoVessel protoVessel = selectedVessel.BackupVessel();
            protoVessel.Save(vesselNode);

            // Now save the file
            ConfigNode node = new ConfigNode();
            node.AddNode("VESSEL", vesselNode);
            node.Save(filePath);

            // Let the user know.
            string message = selectedVessel.vesselName + " " + Localizer.Format("#LOC_BLUESHIFT_shipExported") + " " + filePath;
            ScreenMessages.PostScreenMessage(message, 5.0f, ScreenMessageStyle.UPPER_CENTER);
        }
    }
}

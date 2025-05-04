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
    [KSPAddon(KSPAddon.Startup.FlightAndEditor, false)]
    public class BlueshiftWarpPlannerApp: MonoBehaviour
    {
        static private ApplicationLauncherButton appLauncherButton = null;
        static private Texture2D appIcon = null;
        private WarpTravelPlanner travelPlanner;

        public void Awake()
        {
            travelPlanner = new WarpTravelPlanner();
            appIcon = GameDatabase.Instance.GetTexture("WildBlueIndustries/Blueshift/Icons/BlueshiftSelected", false);
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

            appLauncherButton = ApplicationLauncher.Instance.AddModApplication(openTravelPlanner, closeTravelPlanner, null, null, null, null, ApplicationLauncher.AppScenes.ALWAYS, appIcon);
        }

        private void openTravelPlanner()
        {
            if ((HighLogic.LoadedSceneIsEditor && EditorLogic.fetch.ship != null) || (HighLogic.LoadedSceneIsFlight && FlightGlobals.fetch.activeVessel != null))
            {
                travelPlanner.SetVisible(true);
            }
        }

        private void closeTravelPlanner()
        {
            travelPlanner.SetVisible(false);
        }
    }
}

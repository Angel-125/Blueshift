using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using KSP.Localization;

namespace Blueshift
{
    internal class WarpTravelPlanner : Dialog<WarpTravelPlanner>
    {
        #region Housekeeping
        static private Texture2D greenCheckMark = null;
        static private Texture2D redXIcon = null;

        private Vector2 scrollPosWarpSpeed;
        private Vector2 scrollPosResourceBalancing;
        private Vector2 scrollPosDestinations;
        private string kDialogTitle;
        private string kBurnTimeTitle;
        private string kMaxWarpFactorTitle;
        private string kRangeTitle;
        private string kResourceBalancingTitle;
        private string kDestinationsTitle;
        private string kResourceBalancingIdealDesc;
        private string kResourceBalancingCurrentDesc;

        GUILayoutOption[] warpSpeedInfoHeight = new GUILayoutOption[] { GUILayout.Height(110) };
        GUILayoutOption[] iconDimensions = new GUILayoutOption[] { GUILayout.Width(16), GUILayout.Height(16) };
        private string status = "";
        private double burnTime = 0;
        private double maxWarpFactor = 0;
        private double rangeLightYears = 0;
        private double distanceTraveledMeters = 0;
        private List<CelestialBody> stars;
        Dictionary<string, double> balancedResourceAmounts = null;
        Dictionary<string, double> currentResourceAmounts = null;
        WBISpatialLocations spatialLocation = WBISpatialLocations.Unknown;
        #endregion

        public WarpTravelPlanner() :
        base("Warp Travel Planner", 375, 600)
        {
            Resizable = false;
            cacheLocalizedStrings();
            WindowTitle = kDialogTitle;
        }

        public override void SetVisible(bool newValue)
        {
            base.SetVisible(newValue);

            if (newValue)
            {
                greenCheckMark = GameDatabase.Instance.GetTexture("WildBlueIndustries/Blueshift/Icons/greenCheckMark", false);
                redXIcon = GameDatabase.Instance.GetTexture("WildBlueIndustries/Blueshift/Icons/redXIcon", false);

                if (HighLogic.LoadedSceneIsEditor)
                {
                    GameEvents.onEditorShipModified.Add(onEditorShipModified);
                }

                cacheLocalizedStrings();

                stars = BlueshiftScenario.shared.GetStars();
                if (BlueshiftScenario.debugMode)
                    Debug.Log("[Blueshfit] - Stars detected: " + stars.Count);

                updateStats();
            }
            else
            {
                if (HighLogic.LoadedSceneIsEditor)
                {
                    GameEvents.onEditorShipModified.Remove(onEditorShipModified);
                }
            }
        }

        private void onEditorShipModified(ShipConstruct ship)
        {
        }

        protected override void DrawWindowContents(int windowId)
        {
            updateStats();

            // Warp speed info
            scrollPosWarpSpeed = GUILayout.BeginScrollView(scrollPosWarpSpeed, warpSpeedInfoHeight);
            drawLineItem(kBurnTimeTitle, "#LOC_BLUESHIFT_plannerBurnTimeDesc", BlueshiftUtilities.FormatTime(burnTime));
            drawLineItem(kMaxWarpFactorTitle, "#LOC_BLUESHIFT_plannerMaxWarpFactorDesc", maxWarpFactor);
            drawLineItem(kRangeTitle, "#LOC_BLUESHIFT_plannerRangeDesc", rangeLightYears, "N5");

            if (string.IsNullOrEmpty(status) == false)
            {
                GUILayout.Label("<color=orange>" + status + "</color>");
            }
            GUILayout.EndScrollView();

            // Resource balancing - Editor only
            if (HighLogic.LoadedSceneIsEditor && balancedResourceAmounts != null && currentResourceAmounts != null)
            {
                drawResourceBalancing();
            }

            // Destinations
            if (stars.Count > 1)
            {
                GUILayout.Label(kDestinationsTitle);
                scrollPosDestinations = GUILayout.BeginScrollView(scrollPosDestinations);
                drawDestinations();
                GUILayout.EndScrollView();
            }
        }

        private void drawLineItem(string title, string description, double value, string format = "N2")
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format(title));
            GUILayout.FlexibleSpace();
            GUILayout.Label(Localizer.Format(description, new string[] { value.ToString(format) }));
            GUILayout.EndHorizontal();
        }

        private void drawLineItem(string title, string description, string value)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format(title));
            GUILayout.FlexibleSpace();
            GUILayout.Label(Localizer.Format(description, new string[] { value }));
            GUILayout.EndHorizontal();
        }

        private void drawDestinations()
        {
            //Find homeworld
            int count = FlightGlobals.Bodies.Count;
            CelestialBody body = null;
            CelestialBody homeStar = null;
            for (int index = 0; index < count; index++)
            {
                body = FlightGlobals.Bodies[index];
                if (body.isHomeWorld)
                {
                    homeStar = BlueshiftScenario.shared.GetParentStar(body);
                    break;
                }
            }
            if (homeStar == null)
            {
                if (BlueshiftScenario.debugMode)
                    Debug.Log("[Blueshfit] - Can't find home star. Ragequitting...");
                return;
            }

            // Calculate the distance to the selected target (if any).
            double distanceMeters;
            double distanceLightYears;
            bool canReachStar = false;
            string colorNotInRange = "grey";
            string colorInRange = "white";
            string textColor = "white";
            if (HighLogic.LoadedSceneIsFlight)
            {
                if (FlightGlobals.ActiveVessel.targetObject != null)
                {
                    Vessel activeVessel = FlightGlobals.ActiveVessel;
                    ITargetable targetObject = activeVessel.targetObject;
                    string units;
                    string targetName;
                    distanceMeters = BlueshiftScenario.shared.GetDistanceToTarget(FlightGlobals.ActiveVessel, out units, out targetName);
                    distanceLightYears = distanceMeters / BlueshiftScenario.shared.kLightYear;

                    bool canReachTarget = rangeLightYears >= distanceLightYears;
                    textColor = canReachTarget ? colorInRange : colorNotInRange;

                    GUILayout.BeginHorizontal();
                    GUILayout.Label(canReachTarget ? greenCheckMark : redXIcon, iconDimensions);
                    GUILayout.Label("<color=" + textColor + "><b>" + targetName + "</b></color>");
                    GUILayout.FlexibleSpace();
                    GUILayout.Label("<color=" + textColor + "><b>" + distanceMeters.ToString("N5") + " " + units + "</b></color>");
                    GUILayout.EndHorizontal();
                }

                // Calculate distance to the home star
                if (spatialLocation == WBISpatialLocations.Interstellar || BlueshiftScenario.shared.GetParentStar(FlightGlobals.ActiveVessel.mainBody) != homeStar)
                {
                    distanceMeters = Math.Abs((FlightGlobals.ActiveVessel.GetWorldPos3D() - (Vector3d)homeStar.GetTransform().position).magnitude);
                    distanceLightYears = distanceMeters / BlueshiftScenario.shared.kLightYear;

                    canReachStar = rangeLightYears >= distanceLightYears;
                    textColor = canReachStar ? colorInRange : colorNotInRange;

                    GUILayout.BeginHorizontal();
                    GUILayout.Label(canReachStar ? greenCheckMark : redXIcon, iconDimensions);
                    GUILayout.Label("<color=" + textColor + ">" + homeStar.displayName.Replace("^N", "") + "</color>");
                    GUILayout.FlexibleSpace();
                    GUILayout.Label("<color=" + textColor + ">" + distanceLightYears.ToString("N5") + " ly</color>");
                    GUILayout.EndHorizontal();
                }
            }

            // Calculate the distances to the stars and list them.
            count = stars.Count;
            for (int index = 0; index < count; index++)
            {
                if (stars[index] == homeStar)
                    continue;

                if (HighLogic.LoadedSceneIsEditor)
                    distanceMeters = Math.Abs(((Vector3d)stars[index].GetTransform().position - (Vector3d)homeStar.GetTransform().position).magnitude);
                else
                    distanceMeters = Math.Abs((FlightGlobals.ActiveVessel.GetWorldPos3D() - (Vector3d)stars[index].GetTransform().position).magnitude);
                distanceLightYears = distanceMeters / BlueshiftScenario.shared.kLightYear;

                canReachStar = rangeLightYears >= distanceLightYears;
                textColor = canReachStar ? colorInRange : colorNotInRange;

                GUILayout.BeginHorizontal();
                GUILayout.Label(canReachStar ? greenCheckMark : redXIcon, iconDimensions);
                GUILayout.Label("<color=" + textColor + ">" + stars[index].displayName.Replace("^N", "") + "</color>");
                GUILayout.FlexibleSpace();
                GUILayout.Label("<color=" + textColor + ">" + distanceLightYears.ToString("N5") + " ly</color>");
                GUILayout.EndHorizontal();
            }
        }

        private void drawResourceBalancing()
        {
            string[] resourceNameKeys = balancedResourceAmounts.Keys.ToArray();
            PartResourceDefinitionList definitions = PartResourceLibrary.Instance.resourceDefinitions;
            PartResourceDefinition resourceDef;

            GUILayout.Label(kResourceBalancingTitle);
            scrollPosResourceBalancing = GUILayout.BeginScrollView(scrollPosResourceBalancing);

            GUILayout.Label(kResourceBalancingIdealDesc);

            for (int index = 0; index < resourceNameKeys.Length; index++)
            {
                resourceDef = definitions[resourceNameKeys[index]];
                GUILayout.BeginHorizontal();
                GUILayout.Label("<color=white>" + resourceDef.displayName + "</color>");
                GUILayout.FlexibleSpace();
                GUILayout.Label("<color=white>" + balancedResourceAmounts[resourceNameKeys[index]].ToString("N2") + "</color>");
                GUILayout.EndHorizontal();
            }

            GUILayout.Label(kResourceBalancingCurrentDesc);
            resourceNameKeys = currentResourceAmounts.Keys.ToArray();
            for (int index = 0; index < resourceNameKeys.Length; index++)
            {
                resourceDef = definitions[resourceNameKeys[index]];
                GUILayout.BeginHorizontal();
                GUILayout.Label("<color=white>" + resourceDef.displayName + "</color>");
                GUILayout.FlexibleSpace();
                GUILayout.Label("<color=white>" + currentResourceAmounts[resourceNameKeys[index]].ToString("N2") + "</color>");
                GUILayout.EndHorizontal();
            }

            GUILayout.EndScrollView();
        }

        private void updateStats()
        {
            maxWarpFactor = 0;
            rangeLightYears = 0;
            distanceTraveledMeters = 0;
            balancedResourceAmounts = null;
            currentResourceAmounts = null;

            if (HighLogic.LoadedSceneIsFlight == false && HighLogic.LoadedSceneIsEditor == false)
                return;

            // Burn Time
            burnTime = HighLogic.LoadedSceneIsEditor ? BlueshiftUtilities.ComputeBurnTime(EditorLogic.fetch.ship, out status) : BlueshiftUtilities.ComputeBurnTime(FlightGlobals.ActiveVessel, out status);

            if (!string.IsNullOrEmpty(status))
                return;

            // Max warp factor
            List<WBIWarpEngine> warpEngines = HighLogic.LoadedSceneIsEditor ? BlueshiftUtilities.getWarpEngines(EditorLogic.fetch.ship) : BlueshiftUtilities.getWarpEngines(FlightGlobals.ActiveVessel);
            int count = warpEngines.Count;
            if (count <= 0)
            {
                status = Localizer.Format("#LOC_BLUESHIFT_noEnginesFound");
                return;
            }

            WBIWarpEngine engine = warpEngines[0];
            spatialLocation = engine.spatialLocation;
            maxWarpFactor = engine.maxWarpSpeed;
            if (HighLogic.LoadedSceneIsFlight)
                maxWarpFactor = engine.CalculateBestSpeedSimulated();

            // Warp Range
            rangeLightYears = BlueshiftUtilities.CalculateRange(burnTime, maxWarpFactor, out distanceTraveledMeters);

            // Balanced resources
            balancedResourceAmounts = HighLogic.LoadedSceneIsEditor ? BlueshiftUtilities.GetBalancedResources(EditorLogic.fetch.ship) : BlueshiftUtilities.GetBalancedResources(FlightGlobals.ActiveVessel);
            if (balancedResourceAmounts != null)
                currentResourceAmounts = HighLogic.LoadedSceneIsEditor ? BlueshiftUtilities.GetResourceAmounts(EditorLogic.fetch.ship, balancedResourceAmounts.Keys.ToArray()) : BlueshiftUtilities.GetResourceAmounts(FlightGlobals.ActiveVessel, balancedResourceAmounts.Keys.ToArray());
        }

        private void cacheLocalizedStrings()
        {
            kDialogTitle = Localizer.Format("#LOC_BLUESHIFT_warpTravelPlannerTitle");
            kBurnTimeTitle = Localizer.Format("#LOC_BLUESHIFT_plannerBurnTimeTitle");
            kMaxWarpFactorTitle = Localizer.Format("#LOC_BLUESHIFT_plannerMaxWarpFactorTitle");
            kRangeTitle = Localizer.Format("#LOC_BLUESHIFT_plannerRangeTitle");
            kResourceBalancingTitle = Localizer.Format("#LOC_BLUESHIFT_plannerResourceBalancingTitle");
            kResourceBalancingIdealDesc = Localizer.Format("#LOC_BLUESHIFT_plannerResourceBalancingIdealDesc");
            kResourceBalancingCurrentDesc = Localizer.Format("#LOC_BLUESHIFT_plannerResourceBalancingCurrentDesc");
            kDestinationsTitle = Localizer.Format("#LOC_BLUESHIFT_plannerDestinationsTitle");
        }
    }
}

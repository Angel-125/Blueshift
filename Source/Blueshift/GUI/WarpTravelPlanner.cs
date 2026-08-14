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
        private string kTimeToTarget;
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
        private double distanceToTargetMeters = 0;
        private double timeToTarget = 0;
        private double maxWarpFactor = 0;
        private double rangeLightYears = 0;
        private double distanceTraveledMeters = 0;
        private List<CelestialBody> stars = new List<CelestialBody>();
        Dictionary<string, double> balancedResourceAmounts = null;
        Dictionary<string, double> currentResourceAmounts = null;
        WBISpatialLocations spatialLocation = WBISpatialLocations.Unknown;
        private string lastLoggedError = "";
        private const float statsUpdateInterval = 0.25f;
        private float nextStatsUpdateTime = 0f;
        private bool statsAreDirty = true;
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

                if (BlueshiftScenario.shared != null)
                    stars = BlueshiftScenario.shared.GetStars() ?? new List<CelestialBody>();
                else
                    stars = new List<CelestialBody>();

                if (BlueshiftScenario.debugMode)
                    Debug.Log("[Blueshfit] - Stars detected: " + stars.Count);

                updateStatsIfNeeded(true);
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
            statsAreDirty = true;
        }

        protected override void DrawWindowContents(int windowId)
        {
            updateStatsIfNeeded();

            // Warp speed info
            scrollPosWarpSpeed = GUILayout.BeginScrollView(scrollPosWarpSpeed, warpSpeedInfoHeight);
            drawLineItem(kBurnTimeTitle, "#LOC_BLUESHIFT_plannerBurnTimeDesc", BlueshiftUtilities.FormatTime(burnTime));
            drawLineItem(kMaxWarpFactorTitle, "#LOC_BLUESHIFT_plannerMaxWarpFactorDesc", maxWarpFactor);
            drawLineItem(kRangeTitle, "#LOC_BLUESHIFT_plannerRangeDesc", rangeLightYears, "N5");
            if (HighLogic.LoadedSceneIsFlight)
                drawLineItem(kTimeToTarget, "#LOC_BLUESHIFT_plannerTimeToTargetDesc", BlueshiftUtilities.FormatTime(timeToTarget));

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
            if (stars != null && stars.Count > 1)
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
            if (FlightGlobals.Bodies == null || BlueshiftScenario.shared == null)
                return;

            Vessel activeVessel = HighLogic.LoadedSceneIsFlight ? FlightGlobals.ActiveVessel : null;
            if (HighLogic.LoadedSceneIsFlight && activeVessel == null)
                return;

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
            double distanceToTarget;
            double distanceLightYears;
            bool canReachStar = false;
            string colorNotInRange = "grey";
            string colorInRange = "white";
            string textColor = "white";
            if (HighLogic.LoadedSceneIsFlight)
            {
                if (activeVessel.targetObject != null)
                {
                    ITargetable targetObject = activeVessel.targetObject;
                    Transform targetTransform = targetObject.GetTransform();
                    if (targetTransform == null)
                        return;

                    string units;
                    string targetName;
                    distanceToTarget = BlueshiftScenario.shared.GetDistanceToTarget(activeVessel, out units, out targetName);
                    distanceMeters = Math.Abs((activeVessel.GetWorldPos3D() - (Vector3d)targetTransform.position).magnitude);
                    distanceLightYears = distanceMeters / BlueshiftScenario.shared.kLightYear;

                    distanceToTargetMeters = distanceMeters;
                    if (maxWarpFactor > 0)
                        timeToTarget = distanceToTargetMeters / (maxWarpFactor * BlueshiftScenario.shared.kLightSpeed);

                    bool canReachTarget = rangeLightYears >= distanceLightYears;
                    textColor = canReachTarget ? colorInRange : colorNotInRange;

                    GUILayout.BeginHorizontal();
                    GUILayout.Label(canReachTarget ? greenCheckMark : redXIcon, iconDimensions);
                    GUILayout.Label("<color=" + textColor + "><b>" + targetName + "</b></color>");
                    GUILayout.FlexibleSpace();
                    GUILayout.Label("<color=" + textColor + "><b>" + distanceToTarget.ToString("N5") + " " + units + "</b></color>");
                    GUILayout.EndHorizontal();
                }

                // Calculate distance to the home star
                if (spatialLocation == WBISpatialLocations.Interstellar || BlueshiftScenario.shared.GetParentStar(activeVessel.mainBody) != homeStar)
                {
                    Transform homeStarTransform = homeStar.GetTransform();
                    if (homeStarTransform == null)
                        return;

                    distanceMeters = Math.Abs((activeVessel.GetWorldPos3D() - (Vector3d)homeStarTransform.position).magnitude);
                    distanceLightYears = distanceMeters / BlueshiftScenario.shared.kLightYear;

                    canReachStar = rangeLightYears >= distanceLightYears;
                    textColor = canReachStar ? colorInRange : colorNotInRange;

                    GUILayout.BeginHorizontal();
                    GUILayout.Label(canReachStar ? greenCheckMark : redXIcon, iconDimensions);
                    string homeStarName = string.IsNullOrEmpty(homeStar.displayName) ? homeStar.bodyName : homeStar.displayName.Replace("^N", "");
                    GUILayout.Label("<color=" + textColor + ">" + homeStarName + "</color>");
                    GUILayout.FlexibleSpace();
                    GUILayout.Label("<color=" + textColor + ">" + distanceLightYears.ToString("N5") + " ly</color>");
                    GUILayout.EndHorizontal();
                }
            }

            // Calculate the distances to the stars and list them.
            count = stars.Count;
            for (int index = 0; index < count; index++)
            {
                CelestialBody star = stars[index];
                if (star == null || star == homeStar)
                    continue;

                Transform starTransform = star.GetTransform();
                Transform homeStarTransform = homeStar.GetTransform();
                if (starTransform == null || homeStarTransform == null)
                    continue;

                if (HighLogic.LoadedSceneIsEditor)
                    distanceMeters = Math.Abs(((Vector3d)starTransform.position - (Vector3d)homeStarTransform.position).magnitude);
                else
                    distanceMeters = Math.Abs((activeVessel.GetWorldPos3D() - (Vector3d)starTransform.position).magnitude);
                distanceLightYears = distanceMeters / BlueshiftScenario.shared.kLightYear;

                canReachStar = rangeLightYears >= distanceLightYears;
                textColor = canReachStar ? colorInRange : colorNotInRange;

                GUILayout.BeginHorizontal();
                GUILayout.Label(canReachStar ? greenCheckMark : redXIcon, iconDimensions);
                string displayName = string.IsNullOrEmpty(star.displayName) ? star.bodyName : star.displayName.Replace("^N", "");
                GUILayout.Label("<color=" + textColor + ">" + displayName + "</color>");
                GUILayout.FlexibleSpace();
                GUILayout.Label("<color=" + textColor + ">" + distanceLightYears.ToString("N5") + " ly</color>");
                GUILayout.EndHorizontal();
            }
        }

        private void drawResourceBalancing()
        {
            string[] resourceNameKeys = balancedResourceAmounts.Keys.ToArray();
            PartResourceDefinition resourceDef;

            GUILayout.Label(kResourceBalancingTitle);
            scrollPosResourceBalancing = GUILayout.BeginScrollView(scrollPosResourceBalancing);

            GUILayout.Label(kResourceBalancingIdealDesc);

            for (int index = 0; index < resourceNameKeys.Length; index++)
            {
                resourceDef = PartResourceLibrary.Instance.GetDefinition(resourceNameKeys[index]);
                GUILayout.BeginHorizontal();
                GUILayout.Label("<color=white>" + (resourceDef != null ? resourceDef.displayName : resourceNameKeys[index]) + "</color>");
                GUILayout.FlexibleSpace();
                GUILayout.Label("<color=white>" + balancedResourceAmounts[resourceNameKeys[index]].ToString("N2") + "</color>");
                GUILayout.EndHorizontal();
            }

            GUILayout.Label(kResourceBalancingCurrentDesc);
            resourceNameKeys = currentResourceAmounts.Keys.ToArray();
            for (int index = 0; index < resourceNameKeys.Length; index++)
            {
                resourceDef = PartResourceLibrary.Instance.GetDefinition(resourceNameKeys[index]);
                GUILayout.BeginHorizontal();
                GUILayout.Label("<color=white>" + (resourceDef != null ? resourceDef.displayName : resourceNameKeys[index]) + "</color>");
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

            ShipConstruct editorShip = HighLogic.LoadedSceneIsEditor && EditorLogic.fetch != null ? EditorLogic.fetch.ship : null;
            Vessel activeVessel = HighLogic.LoadedSceneIsFlight ? FlightGlobals.ActiveVessel : null;
            if ((HighLogic.LoadedSceneIsEditor && editorShip == null) ||
                (HighLogic.LoadedSceneIsFlight && activeVessel == null))
            {
                burnTime = 0;
                status = "No active vessel is available.";
                return;
            }

            // Burn Time
            burnTime = HighLogic.LoadedSceneIsEditor ? BlueshiftUtilities.ComputeBurnTime(editorShip, out status) : BlueshiftUtilities.ComputeBurnTime(activeVessel, out status);

            if (!string.IsNullOrEmpty(status))
                return;

            // Max warp factor
            List<WBIWarpEngine> warpEngines = HighLogic.LoadedSceneIsEditor ? BlueshiftUtilities.getWarpEngines(editorShip) : BlueshiftUtilities.getWarpEngines(activeVessel);
            int count = warpEngines.Count;
            if (count <= 0)
            {
                status = Localizer.Format("#LOC_BLUESHIFT_noEnginesFound");
                return;
            }

            WBIWarpEngine engine = warpEngines[0];
            if (engine == null)
            {
                status = Localizer.Format("#LOC_BLUESHIFT_noEnginesFound");
                return;
            }

            spatialLocation = engine.spatialLocation;
            maxWarpFactor = engine.maxWarpSpeed;
            if (HighLogic.LoadedSceneIsFlight)
                maxWarpFactor = engine.CalculateBestSpeedSimulated();

            // Warp Range
            if (BlueshiftScenario.shared == null)
            {
                status = "Blueshift scenario data is not available.";
                return;
            }
            rangeLightYears = BlueshiftUtilities.CalculateRange(burnTime, maxWarpFactor, out distanceTraveledMeters);

            // Flight Time

            // Balanced resources
            balancedResourceAmounts = HighLogic.LoadedSceneIsEditor ? BlueshiftUtilities.GetBalancedResources(editorShip) : BlueshiftUtilities.GetBalancedResources(activeVessel);
            if (balancedResourceAmounts != null)
                currentResourceAmounts = HighLogic.LoadedSceneIsEditor ? BlueshiftUtilities.GetResourceAmounts(editorShip, balancedResourceAmounts.Keys.ToArray()) : BlueshiftUtilities.GetResourceAmounts(activeVessel, balancedResourceAmounts.Keys.ToArray());
        }

        private void updateStatsSafely()
        {
            try
            {
                updateStats();
                lastLoggedError = "";
            }
            catch (Exception ex)
            {
                burnTime = 0;
                maxWarpFactor = 0;
                rangeLightYears = 0;
                timeToTarget = 0;
                balancedResourceAmounts = null;
                currentResourceAmounts = null;
                status = "The warp travel planner could not update: " + ex.GetType().Name;

                string error = ex.ToString();
                if (lastLoggedError != error)
                {
                    lastLoggedError = error;
                    Debug.LogError("[Blueshift] - WarpTravelPlanner failed to update.\n" + error);
                }
            }
        }

        private void updateStatsIfNeeded(bool forceUpdate = false)
        {
            float currentTime = Time.realtimeSinceStartup;
            if (!forceUpdate && !statsAreDirty && currentTime < nextStatsUpdateTime)
                return;

            statsAreDirty = false;
            nextStatsUpdateTime = currentTime + statsUpdateInterval;
            updateStatsSafely();
        }

        private void cacheLocalizedStrings()
        {
            kDialogTitle = Localizer.Format("#LOC_BLUESHIFT_warpTravelPlannerTitle");
            kBurnTimeTitle = Localizer.Format("#LOC_BLUESHIFT_plannerBurnTimeTitle");
            kTimeToTarget = Localizer.Format("#LOC_BLUESHIFT_plannerTimeToTargetTitle");
            kMaxWarpFactorTitle = Localizer.Format("#LOC_BLUESHIFT_plannerMaxWarpFactorTitle");
            kRangeTitle = Localizer.Format("#LOC_BLUESHIFT_plannerRangeTitle");
            kResourceBalancingTitle = Localizer.Format("#LOC_BLUESHIFT_plannerResourceBalancingTitle");
            kResourceBalancingIdealDesc = Localizer.Format("#LOC_BLUESHIFT_plannerResourceBalancingIdealDesc");
            kResourceBalancingCurrentDesc = Localizer.Format("#LOC_BLUESHIFT_plannerResourceBalancingCurrentDesc");
            kDestinationsTitle = Localizer.Format("#LOC_BLUESHIFT_plannerDestinationsTitle");
        }
    }
}

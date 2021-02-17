using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Blueshift
{
    /// <summary>
    ///  This class helps starships determine when they're in interstellar space.
    /// </summary>
    [KSPScenario(ScenarioCreationOptions.AddToAllGames, GameScenes.SPACECENTER, GameScenes.EDITOR, GameScenes.FLIGHT, GameScenes.TRACKSTATION)]
    public class BlueshiftScenario: ScenarioModule
    {
        #region Constants
        /// <summary>
        /// Light-year unit of measurement. Abbreviated "Ly."
        /// </summary>
        public double kLightYear = 9460700000000000;

        /// <summary>
        /// Gigameter unit of measurement. Abbreviate "Gm."
        /// </summary>
        public double kGigaMeter = 1000000000;

        /// <summary>
        /// Megameter unit of measurement. Abbreviated "Mm."
        /// </summary>
        public double kMegaMeter = 1000000;

        private static string kBlueshiftSettings = "BLUESHIFT_SETTINGS";
        private static string kInterstellarWarpSpeedMultiplier = "interstellarWarpSpeedMultiplier";
        private static string kSOIMultiplier = "soiMultiplier";
        private static string kCircularizationResource = "circularizationResource";
        private static string kCircularizationCostPerTonne = "circularizationCostPerTonne";
        private static string kAnomalyCheckSeconds = "anomalyCheckSeconds";
        private static string kCelestialBlacklist = "celestialBlacklist";
        private static string kLastPlanetNode = "LAST_PLANET";
        private static string kName = "name";
        private static string kStarName = "starName";
        private static string kSoiNoPlanetsMultiplier = "soiNoPlanetsMultiplier";
        #endregion

        #region Housekeeping
        /// <summary>
        /// Shared instance of the helper.
        /// </summary>
        public static BlueshiftScenario shared;

        /// <summary>
        /// When in intersteller space, vessels can go much faster. This multiplier tells us how much faster we can go.
        /// For comparison, Mass Effect Andromeda's Tempest can cruise at 4745 times light speed, or 13 light-years per day.
        /// </summary>
        public static float interstellarWarpSpeedMultiplier = 1000;

        /// <summary>
        /// Flag to indicate whether or not to auto-circularize the orbit.
        /// </summary>
        public static bool autoCircularize = false;

        /// <summary>
        /// It can cost resources to auto-circularize a ship after warp.
        /// </summary>
        public static PartResourceDefinition circularizationResourceDef = null;

        /// <summary>
        /// How much circularizationResource does it cost per metric ton of ship to circularize its orbit.
        /// </summary>
        public static double circularizationCostPerTonne = 0;

        /// <summary>
        /// Flag to indicate whether or not Space Anomalies are enabled.
        /// </summary>
        public static bool spawnSpaceAnomalies = true;

        private double soiMultiplier = 1.1;
        private double soiNoPlanetsMultiplier = 100;
        private List<WBISpaceAnomaly> spaceAnomalies;
        private List<WBISpaceAnomaly> anomalyTemplates;
        private double anomalyCheckSeconds = 600;
        private double anomalyTimer = 0;
        private double anomalyCleanerSeconds = 60;
        private double anomalyCleanerTimer = 0;
        private List<CelestialBody> lastPlanets;
        private Dictionary<CelestialBody, CelestialBody> lastPlanetByStar;
        private List<CelestialBody> stars;
        private List<CelestialBody> planets;
        private string[] celestialBlacklists;
        private Dictionary<string, string> lastPlanetOverrides;
        private Dictionary<CelestialBody, double> solarSOIs;
        #endregion

        #region Overrides
        public void FixedUpdate()
        {
            double currentTime = Planetarium.GetUniversalTime();

            // Check for anomaly spawns
            if (spawnSpaceAnomalies && (anomalyTimer == 0 || currentTime - anomalyTimer >= anomalyCheckSeconds))
            {
                anomalyTimer = currentTime;
                checkForNewAnomalies();
            }
        }

        public override void OnAwake()
        {
            base.OnAwake();
            shared = this;

            lastPlanets = new List<CelestialBody>();
            lastPlanetByStar = new Dictionary<CelestialBody, CelestialBody>();
            stars = new List<CelestialBody>();
            planets = new List<CelestialBody>();
            solarSOIs = new Dictionary<CelestialBody, double>();

            loadSettings();
            loadLastPlanetOverrides();
            GetEveryLastPlanet();
            calculateSolarSOIs();

            autoCircularize = BlueshiftSettings.AutoCircularize;
            spawnSpaceAnomalies = BlueshiftSettings.SpaceAnomaliesEnabled;
            GameEvents.OnGameSettingsApplied.Add(onGameSettingsApplied);

            if (!spawnSpaceAnomalies)
                removeSpaceAnomalies();
        }

        public void OnDestroy()
        {
            GameEvents.OnGameSettingsApplied.Remove(onGameSettingsApplied);
        }

        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);

            // Load anomalies
            spaceAnomalies = new List<WBISpaceAnomaly>();
            if (node.HasNode(WBISpaceAnomaly.kNodeName))
            {
                ConfigNode[] nodes = node.GetNodes(WBISpaceAnomaly.kNodeName);
                for (int index = 0; index < nodes.Length; index++)
                    spaceAnomalies.Add(WBISpaceAnomaly.CreateFromNode(nodes[index]));
            }

            // Load anomaly templates
            anomalyTemplates = new List<WBISpaceAnomaly>();
            ConfigNode[] templateNodes = GameDatabase.Instance.GetConfigNodes(WBISpaceAnomaly.kNodeName);
            WBISpaceAnomaly anomaly;
            if (templateNodes != null)
            {
                for (int index = 0; index < templateNodes.Length; index++)
                {
                    anomaly = WBISpaceAnomaly.CreateFromNode(templateNodes[index]);

                    anomalyTemplates.Add(anomaly);
                }
            }
        }

        public override void OnSave(ConfigNode node)
        {
            base.OnSave(node);

            int count = spaceAnomalies.Count;
            for (int index = 0; index < count; index++)
                node.AddNode(spaceAnomalies[index].Save());
        }
        #endregion

        #region API
        /// <summary>
        /// Determines thevessel's spatial location.
        /// </summary>
        /// <param name="vessel">The Vessel to check.</param>
        /// <returns>A WBISpatialLocations withe spatial location.</returns>
        public WBISpatialLocations GetSpatialLocation(Vessel vessel)
        {
            // If we're not in space then our spatial location is unknown.
            if (!IsInSpace(vessel))
                return WBISpatialLocations.Unknown;

            // If the mainBody is on our solarSOIs list then check altitude. If altitude > soi then we're interstellar. Otherwise, we're interplanetary.
            if (solarSOIs.ContainsKey(vessel.mainBody))
                return vessel.altitude > solarSOIs[vessel.mainBody] ? WBISpatialLocations.Interstellar : WBISpatialLocations.Interplanetary;

            // If the mainBody is on the blacklist then we're interstellar. Otherwise we're planetary.
            return isOnBlackList(vessel.mainBody) ? WBISpatialLocations.Interstellar : WBISpatialLocations.Planetary;
        }

        /// <summary>
        /// Determines whether or not the celestial body is a star.
        /// </summary>
        /// <param name="body">The body to test.</param>
        /// <returns>true if the body is a star, false if not.</returns>
        public bool IsAStar(CelestialBody body)
        {
            return body.scaledBody.GetComponentsInChildren<SunShaderController>(true).Length > 0;
        }

        /// <summary>
        /// Determines whether or not the vessel is in interstellar space.
        /// </summary>
        /// <param name="vessel"></param>
        /// <returns></returns>
        public bool IsInInterstellarSpace(Vessel vessel)
        {
            if (!IsInSpace(vessel))
                return false;

            // If the mainBody is on our soi list then check altitude.
            if (solarSOIs.ContainsKey(vessel.mainBody))
                return vessel.altitude > solarSOIs[vessel.mainBody];

            // If we're orbiting a blacklisted body then we're in interstellar space. Otherwise we're orbiting a planet.
            return isOnBlackList(vessel.mainBody);
        }

        /// <summary>
        /// Determines whether or not the vessel is in space.
        /// </summary>
        /// <param name="vessel">The Vessel to check.</param>
        /// <returns>true if the vessel is in space, false if not.</returns>
        public bool IsInSpace(Vessel vessel)
        {
            return vessel.situation == Vessel.Situations.SUB_ORBITAL ||
                vessel.situation == Vessel.Situations.ORBITING ||
                vessel.situation == Vessel.Situations.ESCAPING;
        }

        /// <summary>
        /// Finds every last planet in every star system.
        /// </summary>
        /// <returns>A List of CelestialBody</returns>
        public List<CelestialBody> GetEveryLastPlanet()
        {
            if (lastPlanets.Count > 0)
                return lastPlanets;
            if (stars.Count == 0)
                GetStars();

            int count = stars.Count;
            CelestialBody body;
            for (int index = 0; index < count; index++)
            {
                // Check for override first.
                if (lastPlanetOverrides.ContainsKey(stars[index].bodyName))
                {
                    body = FlightGlobals.GetBodyByName(lastPlanetOverrides[stars[index].bodyName]);
                    if (body != null && !lastPlanetByStar.ContainsKey(stars[index]))
                    {
                        lastPlanets.Add(body);
                        lastPlanetByStar.Add(stars[index], body);
                    }
                }

                // Try to figure it out based on distance.
                body = GetLastPlanet(stars[index]);
                if (body != null && !lastPlanetByStar.ContainsKey(stars[index]))
                {
                    lastPlanets.Add(body);
                    lastPlanetByStar.Add(stars[index], body);
                }
            }

            return lastPlanets;
        }

        /// <summary>
        /// Finds all the stars in the game.
        /// </summary>
        /// <returns>A Listcontaining all the stars in the game. Celestial bodies that are on the celestialBlacklist are ignored.</returns>
        public List<CelestialBody> GetStars()
        {
            if (stars.Count > 0)
                return stars;

            List<CelestialBody> bodies = FlightGlobals.fetch.bodies;
            int count = bodies.Count;
            for (int index = 0; index < count; index++)
            {
                if (IsAStar(bodies[index]))
                    stars.Add(bodies[index]);
            }

            return stars;
        }

        /// <summary>
        /// Returns a list of all the planets in the game.
        /// </summary>
        /// <returns>A Listcontaining all the planets in the game. Celestial bodies that are on the celestialBlacklist are ignored.</returns>
        public List<CelestialBody> GetPlanets()
        {
            if (planets.Count > 0)
                return planets;

            List<CelestialBody> bodies = FlightGlobals.fetch.bodies;
            int count = bodies.Count;
            for (int index = 0; index < count; index++)
            {
                if (!IsAStar(bodies[index]) && !isOnBlackList(bodies[index]))
                    planets.Add(bodies[index]);
            }

            return planets;
        }

        /// <summary>
        /// Finds the last planet in the supplied star system.
        /// </summary>
        /// <param name="star">A Celestial Body that is the star to check.</param>
        /// <returns>A CelestialBody representing the last planet in the star system (if any)</returns>
        public CelestialBody GetLastPlanet(CelestialBody star)
        {
            if (!IsAStar(star))
                return null;

            List<CelestialBody> orbitingBodies = star.orbitingBodies;
            CelestialBody body, furthestBody = null;
            int count = orbitingBodies.Count;
            double furthestDistance = 0;
            bool isAStar = false;
            bool blacklisted = false;

            // First find the last planet around the star.
            for (int index = 0; index < count; index++)
            {
                body = orbitingBodies[index];

                // If the celestial body is a planet then check to see if it is the furthest.
                isAStar = IsAStar(body);
                blacklisted = isOnBlackList(body);
                if (!isAStar && !blacklisted && body.orbit.semiMajorAxis > furthestDistance)
                    furthestBody = body;
            }

            // Ok, we can use the calculated furthest body if we found one and it's not on the blacklist.
            if (furthestBody != null)
                Debug.Log("[Blueshift] Last planet in the " + star.name + " system is: " + furthestBody.name);

            return furthestBody;
        }

        /// <summary>
        /// Determines whether or not the celestial body has planets orbiting it.
        /// </summary>
        /// <param name="celestialBody">The CelestialBody to check for planets.</param>
        /// <returns>true if the celestialBody has orbiting planets, false if not.</returns>
        public bool HasPlanets(CelestialBody celestialBody)
        {
            List<CelestialBody> orbitingBodies = celestialBody.orbitingBodies;
            CelestialBody body;
            int count = orbitingBodies.Count;
            bool isAStar = false;
            bool blacklisted = false;

            for (int index = 0; index < count; index++)
            {
                body = orbitingBodies[index];

                // If the celestial body is not a star and it isn't blacklisted then we have planets.
                isAStar = IsAStar(body);
                blacklisted = isOnBlackList(body);
                if (!isAStar && !blacklisted)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Calculates the distance and units of measurement to the vessel's target (if any).
        /// </summary>
        /// <param name="vessel">The Vessel to check for targets.</param>
        /// <param name="units">A string representing the units of measurement computed for the distance.</param>
        /// <param name="targetName">A string representing the name of the vessel's target.</param>
        /// <returns>A double containing the distance. If there is no target then the distance is 0.</returns>
        public double GetDistanceToTarget(Vessel vessel, out string units, out string targetName)
        {
            ITargetable targetObject = vessel.targetObject;
            double targetDistance = 0;
            units = "m";
            targetName = "None";

            //First check to see if the vessel has selected a target.
            if (targetObject != null)
            {
                targetName = targetObject.GetDisplayName().Replace("^N", "");
                targetDistance = Math.Abs((vessel.GetWorldPos3D() - (Vector3d)targetObject.GetTransform().position).magnitude);

                // Light-years
                if (targetDistance > (kGigaMeter * 1000))
                {
                    targetDistance /= kLightYear;
                    units = "Ly";
                }

                // Giga-meters
                else if (targetDistance > (kMegaMeter * 1000))
                {
                    targetDistance /= kGigaMeter;
                    units = "Gm";
                }

                // Mega-meters
                else if (targetDistance > 1000 * 1000)
                {
                    targetDistance /= kMegaMeter;
                    units = "Mm";
                }

                else
                {
                    targetDistance /= 1000;
                    units = "Km";
                }

            }

            return targetDistance;
        }
        #endregion

        #region Helpers
        private void removeSpaceAnomalies()
        {
            int count = FlightGlobals.VesselsUnloaded.Count;
            Dictionary<string, Vessel> unloadedVessels = new Dictionary<string, Vessel>();
            WBISpaceAnomaly anomaly;
            Vessel vessel;

            for (int index = 0; index < count; index++)
            {
                vessel = FlightGlobals.VesselsUnloaded[index];
                unloadedVessels.Add(vessel.persistentId.ToString(), vessel);
            }

            count = spaceAnomalies.Count;
            for (int index = 0; index < count; index++)
            {
                anomaly = spaceAnomalies[index];
                if (unloadedVessels.ContainsKey(anomaly.vesselId))
                {
                    vessel = unloadedVessels[anomaly.vesselId];
                    unloadedVessels.Remove(anomaly.vesselId);
                    FlightGlobals.VesselsUnloaded.Remove(vessel);
                }
            }

            spaceAnomalies.Clear();
        }

        private void checkForNewAnomalies()
        {
            int count = anomalyTemplates.Count;
            WBISpaceAnomaly anomalyTemplate;

            for (int index = 0; index < count; index++)
            {
                anomalyTemplate = anomalyTemplates[index];
                anomalyTemplate.CreateNewInstancesIfNeeded(spaceAnomalies);
            }
        }

        private bool isOnBlackList(CelestialBody body)
        {
            if (celestialBlacklists == null || celestialBlacklists.Length == 0)
                return false;

            string bodyName = body.bodyName.ToLower();
            for (int index = 0; index < celestialBlacklists.Length; index++)
            {
                if (bodyName.Contains(celestialBlacklists[index].ToLower()))
                    return true;
            }

            return false;
        }

        private void onGameSettingsApplied()
        {
            autoCircularize = BlueshiftSettings.AutoCircularize;
            spawnSpaceAnomalies = BlueshiftSettings.SpaceAnomaliesEnabled;

            if (!spawnSpaceAnomalies)
                removeSpaceAnomalies();
        }

        private void loadSettings()
        {
            // Load the settings we need for interstellar travel.
            ConfigNode[] nodes = GameDatabase.Instance.GetConfigNodes(kBlueshiftSettings);
            if (nodes != null)
            {
                ConfigNode nodeSettings = nodes[0];

                if (nodeSettings.HasValue(kInterstellarWarpSpeedMultiplier))
                    float.TryParse(nodeSettings.GetValue(kInterstellarWarpSpeedMultiplier), out interstellarWarpSpeedMultiplier);

                if (nodeSettings.HasValue(kSOIMultiplier))
                    double.TryParse(nodeSettings.GetValue(kSOIMultiplier), out soiMultiplier);

                if (nodeSettings.HasValue(kCircularizationResource))
                {
                    PartResourceDefinitionList definitions = PartResourceLibrary.Instance.resourceDefinitions;

                    string resourceName = nodeSettings.GetValue(kCircularizationResource);
                    if (definitions.Contains(resourceName))
                        circularizationResourceDef = definitions[resourceName];
                }

                if (nodeSettings.HasValue(kCircularizationCostPerTonne))
                    double.TryParse(nodeSettings.GetValue(kCircularizationCostPerTonne), out circularizationCostPerTonne);

                if (nodeSettings.HasValue(kAnomalyCheckSeconds))
                    double.TryParse(nodeSettings.GetValue(kAnomalyCheckSeconds), out anomalyCheckSeconds);

                if (nodeSettings.HasValue(kCelestialBlacklist))
                    celestialBlacklists = nodeSettings.GetValues(kCelestialBlacklist);

                if (nodeSettings.HasValue(kSoiNoPlanetsMultiplier))
                    double.TryParse(nodeSettings.GetValue(kSoiNoPlanetsMultiplier), out soiNoPlanetsMultiplier);
            }
        }

        private void loadLastPlanetOverrides()
        {
            lastPlanetOverrides = new Dictionary<string, string>();

            ConfigNode[] nodes = GameDatabase.Instance.GetConfigNodes(kLastPlanetNode);
            ConfigNode node;
            if (nodes != null)
            {
                for (int index = 0; index < nodes.Length; index++)
                {
                    node = nodes[index];
                    if (node.HasValue(kName) && node.HasValue(kStarName))
                    {
                        lastPlanetOverrides.Add(node.GetValue(kStarName), node.GetValue(kName));
                    }
                }
            }
        }

        private void calculateSolarSOIs()
        {
            if (lastPlanets.Count == 0 || stars.Count == 0)
                GetEveryLastPlanet();

            CelestialBody solarBody;
            CelestialBody lastPlanet;
            int count = stars.Count;

            for (int index = 0; index < count; index++)
            {
                solarBody = stars[index];

                // If we were able to determine the last planet for the star then we can use the last planet's SMA to determine the solar SOI.
                if (lastPlanetByStar.ContainsKey(solarBody))
                {
                    lastPlanet = lastPlanetByStar[solarBody];
                    solarSOIs.Add(solarBody, lastPlanet.orbit.semiMajorAxis * soiMultiplier);
                }

                // Either we could not determine the star's last planet, or the star has no planets. In this case, we create an arbitrary SOI based on soiNoPlanetsMultiplier.
                else
                {
                    solarSOIs.Add(solarBody, solarBody.Radius * soiNoPlanetsMultiplier * soiMultiplier);
                }
            }
        }
        #endregion
    }
}

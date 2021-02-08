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
        private static string kBlueshiftSettings = "BLUESHIFT_SETTINGS";
        private static string kInterstellarWarpSpeedMultiplier = "interstellarWarpSpeedMultiplier";
        private static string kHomeSOIMultiplier = "homeSOIMultiplier";
        private static string kAutoCircularizationDelay = "autoCircularizationDelay";
        private static string kCircularizationResource = "circularizationResource";
        private static string kCircularizationCostPerTonne = "circularizationCostPerTonne";
        private static string kAnomalyCheckSeconds = "anomalyCheckSeconds";
        private static string kCelestialBlacklist = "celestialBlacklist";
        private static string kLastPlanetNode = "LAST_PLANET";
        private static string kStarName = "starName";
        private static string kLastPlanetName = "lastPlanetName";
        #endregion

        #region Housekeeping
        /// <summary>
        /// Shared instance of the helper.
        /// </summary>
        public static BlueshiftScenario shared;

        /// <summary>
        /// Sphere of influence radius of the home system.
        /// </summary>
        public static double homeSystemSOI = 0;

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
        /// In seconds, how long to wait between cutting the warp engine throttle and automatically circularizing the ship's orbit.
        /// </summary>
        public static float autoCircularizationDelay = 5;

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

        private double homeSOIMultiplier = 1.1;
        private List<WBISpaceAnomaly> spaceAnomalies;
        private List<WBISpaceAnomaly> anomalyTemplates;
        private double anomalyCheckSeconds = 600;
        private double anomalyTimer = 0;
        private double anomalyCleanerSeconds = 60;
        private double anomalyCleanerTimer = 0;
        private List<CelestialBody> lastPlanets;
        private List<CelestialBody> stars;
        private List<CelestialBody> planets;
        private string[] celestialBlacklists;
        private Dictionary<string, string> lastPlanetsMap;
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
            stars = new List<CelestialBody>();
            planets = new List<CelestialBody>();

            loadSettings();
            loadLastPlanetsMap();

            autoCircularize = BlueshiftSettings.AutoCircularize;
            spawnSpaceAnomalies = BlueshiftSettings.SpaceAnomaliesEnabled;
            GameEvents.OnGameSettingsApplied.Add(onGameSettingsApplied);

            if (HighLogic.LoadedSceneIsFlight)
                calculateHomeSOI();

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

            // Since the Sun has infinite SOI in the game, and satellite stars have a finite SOI, we just need to know if we're orbiting the Sun, and if we're out past its artificial SOI radius.
            if (vessel.mainBody != Planetarium.fetch.Sun)
                return false;

            return vessel.orbit.altitude > homeSystemSOI;
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

            getLastPlanet(Planetarium.fetch.Sun);

            return lastPlanets;
        }

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

        private void getLastPlanet(CelestialBody star)
        {
            List<CelestialBody> orbitingBodies = star.orbitingBodies;
            List<CelestialBody> orbitingStars = new List<CelestialBody>();
            CelestialBody body, furthestBody = null;
            int count = orbitingBodies.Count;
            double furthestDistance = 0;
            bool isAStar = false;
            bool blacklisted = false;

            // First find the last planet around the star as well as orbiting stars.
            for (int index = 0; index < count; index++)
            {
                body = orbitingBodies[index];

                // If the celestial body is a planet then check to see if it is the furthest.
                isAStar = IsAStar(body);
                blacklisted = isOnBlackList(body);
                if (!isAStar && !blacklisted && body.orbit.semiMajorAxis > furthestDistance)
                    furthestBody = body;

                // Add the star to our list.
                else if (isAStar && !blacklisted)
                    orbitingStars.Add(body);
            }

            // We might have a helper to tell us what the last planet is. Use that instead.
            if (lastPlanetsMap.ContainsKey(star.bodyName))
            {
                lastPlanets.Add(FlightGlobals.GetBodyByName(lastPlanetsMap[star.bodyName]));
                Debug.Log("[Blueshift] Last planet in the " + star.name + " system is: " + lastPlanetsMap[star.bodyName]);
            }

            // Ok, we can use the calculated furthest body if we found one and it's not on the blacklist.
            else if (furthestBody != null)
            {
                lastPlanets.Add(furthestBody);
                Debug.Log("[Blueshift] Last planet in the " + star.name + " system is: " + furthestBody.name);
            }

            // Now check for other starts
            count = orbitingStars.Count;
            for (int index = 0; index < count; index++)
            {
                getLastPlanet(orbitingStars[index]);
            }
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

                if (nodeSettings.HasValue(kHomeSOIMultiplier))
                    double.TryParse(nodeSettings.GetValue(kHomeSOIMultiplier), out homeSOIMultiplier);

                if (nodeSettings.HasValue(kAutoCircularizationDelay))
                    float.TryParse(nodeSettings.GetValue(kAutoCircularizationDelay), out autoCircularizationDelay);

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
            }
        }

        private void loadLastPlanetsMap()
        {
            lastPlanetsMap = new Dictionary<string, string>();

            ConfigNode[] nodes = GameDatabase.Instance.GetConfigNodes(kLastPlanetNode);
            ConfigNode node;
            if (nodes != null)
            {
                for (int index = 0; index < nodes.Length; index++)
                {
                    node = nodes[index];
                    if (node.HasValue(kStarName) && node.HasValue(kLastPlanetName))
                    {
                        lastPlanetsMap.Add(node.GetValue(kStarName), node.GetValue(kLastPlanetName));
                    }
                }
            }
        }

        private void calculateHomeSOI()
        {
            // Calculate the SOI of the home system. Technically it is infinite but we will will arbitrarily set it past the furthest planet from the sun.
            List<CelestialBody> orbitingBodies = Planetarium.fetch.Sun.orbitingBodies;
            CelestialBody body;
            int bodyCount = orbitingBodies.Count;
            homeSystemSOI = 0;
            for (int index = 0; index < bodyCount; index++)
            {
                body = orbitingBodies[index];

                // If the celestial body is a planet then calculate its average distance from the sun.
                if (!IsAStar(body) && !isOnBlackList(body) && body.orbit.semiMajorAxis > homeSystemSOI)
                    homeSystemSOI = body.orbit.semiMajorAxis;
            }

            // Add our home SOI multiplier.
            homeSystemSOI *= homeSOIMultiplier;
        }
        #endregion
    }
}

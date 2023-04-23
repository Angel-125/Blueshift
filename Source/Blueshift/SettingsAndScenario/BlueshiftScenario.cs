using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using KSP.Localization;

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

        /// <summary>
        /// How long to display a screen message.
        /// </summary>
        public static float messageDuration = 3f;

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
        private static string kUniversalNetworkID = "Any";
        private static string kGateNetworkNode = "JUMPGATE_NETWORK";
        private static string kNetworkID = "networkID";
        private static string kJumpgateID = "jumpgateID";
        private static string kAnomalyTimer = "anomalyTimer";
        private static string kMinRendezvousDistancePlanetary = "minRendezvousDistancePlanetary";
        private static string kMinRendezvousDistanceInterplanetary = "minRendezvousDistanceInterplanetary";
        private static string kRendezvousDistance = "rendezvousDistance";
        private static string kDebugMode = "debugMode";
        private static string kWarpEngineerSkill = "warpEngineerSkill";
        private static string kWarpSpeedBoostRank = "warpSpeedBoostRank";
        private static string kWarpSpeedSkillMultiplier = "warpSpeedSkillMultiplier";
        private static string kJumpGateSourceId = "jumpGateSourceId";
        private static string kDestinationGateId = "destinationGateId";
        private static string kInterstellarResourceConsumptionModifier = "interstellarResourceConsumptionModifier";
        #endregion

        #region Housekeeping
        /// <summary>
        /// Shared instance of the helper.
        /// </summary>
        public static BlueshiftScenario shared;

        /// <summary>
        /// Flag to indicate that the mod is in debug mode.
        /// </summary>
        public static bool debugMode = false;

        /// <summary>
        /// When in intersteller space, vessels can go much faster. This multiplier tells us how much faster we can go.
        /// For comparison, Mass Effect Andromeda's Tempest can cruise at 4745 times light speed, or 13 light-years per day.
        /// </summary>
        public static float interstellarWarpSpeedMultiplier = 1000;

        /// <summary>
        /// Skill to use for improving warp engine performance.
        /// </summary>
        public static string warpEngineerSkill = "ConverterSkill";

        /// <summary>
        /// Minimum skill rank required to improve warp engine performance.
        /// </summary>
        public static int warpSpeedBoostRank = 3;

        /// <summary>
        /// Skill multiplier to use when improving warp engine performance.
        /// </summary>
        public static float warpSpeedSkillMultiplier = 0.1f;

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

        /// <summary>
        /// Flag to indicate whether or not Jumpgate anomalies are enabled.
        /// </summary>
        public static bool spawnJumpgates = false;

        /// <summary>
        /// The jumpgate startup sequence is destructive. Stay clear!
        /// </summary>
        public static bool jumpgateStartupIsDestructive = false;

        /// <summary>
        /// Flag to indicate if parts require maintenance.
        /// </summary>
        public static bool maintenanceEnabled = false;

        /// <summary>
        /// In meters, minimum distance in planetary space that's required to rendezvous with a vessel via auto-circularization.
        /// </summary>
        public static double minRendezvousDistancePlanetary;

        /// <summary>
        /// In meters, minimum distance in interplanetary space that's required to rendezvous with a vessel via auto-circularization.
        /// </summary>
        public static double minRendezvousDistanceInterplanetary;

        /// <summary>
        /// In meters, how close to the targed vessel should you end up at when you rendezvous with it during auto-circularization or a jump.
        /// </summary>
        public static float rendezvousDistance = 100;

        /// <summary>
        /// This modifier reduces the resources required to power warp engines while in interstellar space. It is a percentage value between 0 and 99.999.
        /// The default value is 10. You can override this global setting by specifying this value in the WBIWarpEngine config.
        /// </summary>
        public static float interstellarResourceConsumptionModifier = 10f;

        /// <summary>
        /// The source jumpgate that the traveler is traveling from. This is primarily used to set focus back to the source gate to jump something else.
        /// </summary>
        public string jumpGateSourceId = string.Empty;

        /// <summary>
        /// The destination gate that the traveler is traviling to. This is primarily used to set focus back to the source gate to jump something else.
        /// </summary>
        public string destinationGateId = string.Empty;

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
        Dictionary<string, List<string>> jumpgateNetwork;
        private bool firstTimeStart = true;
        #endregion

        #region Overrides
        public void FixedUpdate()
        {
            double currentTime = Planetarium.GetUniversalTime();
            if (anomalyTimer == 0 || firstTimeStart)
            {
                firstTimeStart = false;
                anomalyTimer = currentTime + anomalyCheckSeconds;
                StartCoroutine(handleAnomalyChecks());
            }

            // Check for anomaly spawns
            if (spawnSpaceAnomalies && currentTime > anomalyTimer)
            {
                anomalyTimer = currentTime + anomalyCheckSeconds;
                StartCoroutine(handleAnomalyChecks());
            }
        }

        protected IEnumerator<YieldInstruction> handleAnomalyChecks()
        {
            checkForNewAnomalies();
            yield return new WaitForFixedUpdate();

            removeExpiredAnomalies();
            yield return new WaitForFixedUpdate();
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
            jumpgateNetwork = new Dictionary<string, List<string>>();
            spaceAnomalies = new List<WBISpaceAnomaly>();
            anomalyTemplates = new List<WBISpaceAnomaly>();

            loadSettings();
            loadLastPlanetOverrides();
            GetEveryLastPlanet();
            calculateSolarSOIs();

            autoCircularize = BlueshiftSettings.AutoCircularize;
            spawnSpaceAnomalies = BlueshiftSettings.SpaceAnomaliesEnabled;
            spawnJumpgates = BlueshiftSettings.JumpgatesEnabled;
            GameEvents.OnGameSettingsApplied.Add(onGameSettingsApplied);
            GameEvents.onVesselWasModified.Add(onVesselWasModified);

            if (!spawnSpaceAnomalies)
                removeSpaceAnomalies();
            else if (!spawnJumpgates)
                removeJumpgates();
        }

        public void OnDestroy()
        {
            GameEvents.OnGameSettingsApplied.Remove(onGameSettingsApplied);
            GameEvents.onVesselWasModified.Remove(onVesselWasModified);
        }

        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);

            // Timer
            if (node.HasValue(kAnomalyTimer))
                double.TryParse(node.GetValue(kAnomalyTimer), out anomalyTimer);

            // Load anomalies
            if (node.HasNode(WBISpaceAnomaly.kNodeName))
            {
                ConfigNode[] nodes = node.GetNodes(WBISpaceAnomaly.kNodeName);
                for (int index = 0; index < nodes.Length; index++)
                    spaceAnomalies.Add(WBISpaceAnomaly.CreateFromNode(nodes[index]));
            }

            // Load anomaly templates
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

            // Load Jumpgate anomalies
            if (node.HasNode(kGateNetworkNode))
            {
                ConfigNode[] gateNodes = node.GetNodes(kGateNetworkNode);
                ConfigNode gateNode;
                List<string> jumpgates;
                string networkID;
                string jumpgateID;
                string[] jumpgateIDs;
                for (int index = 0; index < gateNodes.Length; index++)
                {
                    gateNode = gateNodes[index];
                    if (!gateNode.HasValue(kNetworkID) || !gateNode.HasValue(kJumpgateID))
                        continue;

                    networkID = gateNode.GetValue(kNetworkID);
                    if (!jumpgateNetwork.ContainsKey(networkID))
                        jumpgateNetwork.Add(networkID, new List<string>());
                    jumpgates = jumpgateNetwork[networkID];

                    jumpgateIDs = gateNode.GetValues(kJumpgateID);
                    for (int gateIndex = 0; gateIndex < jumpgateIDs.Length; gateIndex++)
                    {
                        jumpgateID = jumpgateIDs[gateIndex];

                        // Make sure the vessel exists
                        if (!jumpgates.Contains(jumpgateID))
                            jumpgates.Add(jumpgateID);
                    }
                }
            }

            if (node.HasValue(kJumpGateSourceId))
                jumpGateSourceId = node.GetValue(kJumpGateSourceId);

            if (node.HasValue(kDestinationGateId))
                destinationGateId = node.GetValue(kDestinationGateId);

            if (node.HasValue(kInterstellarResourceConsumptionModifier))
                float.TryParse(node.GetValue(kInterstellarResourceConsumptionModifier), out interstellarResourceConsumptionModifier);
        }

        public override void OnSave(ConfigNode node)
        {
            base.OnSave(node);

            // Anomaly timer
            node.AddValue(kAnomalyTimer, anomalyTimer.ToString());

            // Space anomalies
            int count = spaceAnomalies.Count;
            for (int index = 0; index < count; index++)
                node.AddNode(spaceAnomalies[index].Save());

            // Jumpgates
            string[] keys = jumpgateNetwork.Keys.ToArray();
            string networkID;
            List<string> jumpgates;
            ConfigNode gateNetworkNode;
            for (int index = 0; index < keys.Length; index++)
            {
                networkID = keys[index];

                jumpgates = jumpgateNetwork[networkID];
                count = jumpgates.Count;
                if (count == 0)
                    continue;

                gateNetworkNode = new ConfigNode(kGateNetworkNode);
                gateNetworkNode.AddValue(kNetworkID, networkID);

                for (int jumpgateIndex = 0; jumpgateIndex < count; jumpgateIndex++)
                {
                    gateNetworkNode.AddValue(kJumpgateID, jumpgates[jumpgateIndex]);
                }

                node.AddNode(gateNetworkNode);
            }

            // Source gate
            if (!string.IsNullOrEmpty(jumpGateSourceId))
                node.AddValue(kJumpGateSourceId, jumpGateSourceId);

            // Destination gate
            if (!string.IsNullOrEmpty(destinationGateId))
                node.AddValue(kDestinationGateId, destinationGateId);
        }
        #endregion

        #region API
        /// <summary>
        /// Determines the spatial location of the destination celestial body relative to the source body.
        /// </summary>
        /// <param name="sourceBody">A CelestialBody representing the source. Typically this is the active vessel's mainBody.</param>
        /// <param name="destinationBody">A CelestialBody containing the desired destination.</param>
        /// <returns>A WBISpacialLocations enum with the relative location of the destination.</returns>
        public WBISpatialLocations GetDestinationLocation(CelestialBody sourceBody, CelestialBody destinationBody)
        {
            CelestialBody sourceParentBody = sourceBody.referenceBody;
            CelestialBody destinationParentBody = destinationBody.referenceBody;
            CelestialBody sourceBodyStar = GetParentStar(sourceBody);
            CelestialBody destinationBodyStar = GetParentStar(destinationParentBody);
            bool sourceBodyisAStar = IsAStar(sourceBody);
            bool sourceParentBodyisAStar = IsAStar(sourceParentBody);
            bool destinationBodyIsAStar = IsAStar(destinationParentBody);
            bool destinationParentBodyIsAStar = IsAStar(destinationParentBody);

            // Source is a planet.
            if (!sourceBodyisAStar)
            {
                // Same planet
                if (sourceBody == destinationBody)
                    return WBISpatialLocations.Planetary;

                // If the destination's body is a moon of the source, or vice-versa, then we're planetary.
                else if (destinationParentBody == sourceBody || (sourceParentBody == destinationBody && !destinationBodyIsAStar))
                    return WBISpatialLocations.Planetary;

                // If the destination's parent body is the same as the source gate's parent body, then we're planetary.
                else if (sourceParentBody == destinationParentBody && !sourceParentBodyisAStar)
                    return WBISpatialLocations.Planetary;

                // If the destination is in the same solar system then we're interplanetary.
                else if ((sourceBodyStar == destinationBodyStar && sourceBodyStar != null) || sourceBodyStar == destinationBody)
                    return WBISpatialLocations.Interplanetary;

                // We're interstellar
                else
                    return WBISpatialLocations.Interstellar;
            }

            // Source is a star.
            else
            {
                // If the source and destination bodys are the same then we're interplanetary.
                if (sourceBody == destinationBody)
                    return WBISpatialLocations.Interplanetary;

                // If the destination's parent star is the source's star, then we're interplanetary.
                else if (destinationBodyStar == sourceBody)
                    return WBISpatialLocations.Interplanetary;

                else
                    return WBISpatialLocations.Interstellar;
            }
        }

        /// <summary>
        /// Returns the highest ranking astronaut in the vessel that has the required skill.
        /// </summary>
        /// <param name="vessel">The vessel to check for the highest ranking kerbal.</param>
        /// <param name="skillName">The name of the skill to look for. Examples include RepairSkill and ScienceSkill.</param>
        /// <param name="astronaut">The astronaut that has the highest ranking skill.</param>
        /// <returns>The skill rank rating of the highest ranking astronaut (if any)</returns>
        public int GetHighestRank(Vessel vessel, string skillName, out ProtoCrewMember astronaut)
        {
            astronaut = null;
            if (string.IsNullOrEmpty(skillName))
                return 0;
            try
            {
                if (vessel.GetCrewCount() == 0)
                    return 0;
            }
            catch
            {
                return 0;
            }

            string[] skillsToCheck = skillName.Split(new char[] { ';' });
            string checkSkill;
            int highestRank = 0;
            int crewRank = 0;
            ProtoCrewMember[] vesselCrew = vessel.GetVesselCrew().ToArray();
            for (int skillIndex = 0; skillIndex < skillsToCheck.Length; skillIndex++)
            {
                checkSkill = skillsToCheck[skillIndex];

                //Find the highest racking kerbal with the desired skill (if any)
                crewRank = GetHighestRank(vesselCrew, checkSkill, out astronaut);
                if (crewRank > highestRank)
                    highestRank = crewRank;
            }

            return highestRank;
        }

        public int GetHighestRank(ShipConstruct ship, string skillName, out ProtoCrewMember astronaut)
        {
            astronaut = null;
            return 0;
            if (string.IsNullOrEmpty(skillName))
                return 0;

            string[] skillsToCheck = skillName.Split(new char[] { ';' });
            string checkSkill;
            int highestRank = 0;
            int crewRank = 0;
            Part[] parts;
            ProtoCrewMember[] crewMembers;
            for (int skillIndex = 0; skillIndex < skillsToCheck.Length; skillIndex++)
            {
                checkSkill = skillsToCheck[skillIndex];

                //Find the highest racking kerbal with the desired skill (if any)
                parts = ship.Parts.ToArray();
                for (int partIndex = 0; partIndex < parts.Length; partIndex++)
                {
                    crewMembers = parts[partIndex].protoModuleCrew.ToArray();
                    crewRank = GetHighestRank(crewMembers, checkSkill, out astronaut);
                    if (crewRank > highestRank)
                        highestRank = crewRank;
                }
            }

            return highestRank;
        }

        public int GetHighestRank(ProtoCrewMember[] crewMembers, string checkSkill, out ProtoCrewMember astronaut)
        {
            astronaut = null;
            int highestRank = 0;
            int crewRank = 0;

            for (int index = 0; index < crewMembers.Length; index++)
            {
                if (crewMembers[index].HasEffect(checkSkill))
                {
                    crewRank = crewMembers[index].experienceTrait.CrewMemberExperienceLevel();
                    if (crewRank > highestRank)
                    {
                        highestRank = crewRank;
                        astronaut = crewMembers[index];

                        if (crewMembers[index].isBadass)
                            highestRank += 1;
                        if (crewMembers[index].veteran)
                            highestRank += 1;
                        if (crewMembers[index].isHero)
                            highestRank += 1;
                    }
                }
            }

            return highestRank;
        }

        #region Jumpgates
        /// <summary>
        /// Adds the jumpgate anomaly to the network.
        /// </summary>
        /// <param name="anomaly">The WBISpaceAnomaly to add.</param>
        public void AddJumpgateToNetwork(WBISpaceAnomaly anomaly)
        {
            if (string.IsNullOrEmpty(anomaly.networkID))
                anomaly.networkID = kUniversalNetworkID;
            string networkID = anomaly.networkID;

            AddJumpgateToNetwork(anomaly.vesselId, networkID);
        }

        /// <summary>
        /// Adds the jumpgate to the network.
        /// </summary>
        /// <param name="vesselID">A string containing the ID of the jumpgate vessel.</param>
        /// <param name="networkID">A string containing the ID of the jumpgate network.</param>
        public void AddJumpgateToNetwork(string vesselID, string networkID)
        {
            List<string> jumpgates = null;
            string gateNetwork = networkID;
            if (string.IsNullOrEmpty(gateNetwork))
                gateNetwork = kUniversalNetworkID;

            if (!jumpgateNetwork.ContainsKey(gateNetwork))
            {
                jumpgates = new List<string>();
                jumpgateNetwork.Add(gateNetwork, jumpgates);
            }

            jumpgates = jumpgateNetwork[gateNetwork];
            string vesselPID = vesselID.Replace("-", "");
            if (!jumpgates.Contains(vesselPID))
                jumpgates.Add(vesselPID);
        }

        /// <summary>
        /// Returns the list of possible destination gates that are in range of the specified origin point.
        /// </summary>
        /// <param name="networkID">A string containing the network ID.</param>
        /// <param name="originPoint">A Vector3d containing the origin point to check for gates in range.</param>
        /// <param name="maxJumpRange">A double containing the maximum jump range, measured in light-years. Set to -1 to ignore max jump range.</param>
        /// <returns>A List of Vessel containing the vessels in the network that are in range, or null if no network or vessels in range could be found.</returns>
        public List<Vessel> GetDestinationGates(string networkID, Vector3d originPoint, double maxJumpRange = -1)
        {
            string gateNetwork = networkID;
            if (string.IsNullOrEmpty(gateNetwork))
                gateNetwork = kUniversalNetworkID;

            if (!jumpgateNetwork.ContainsKey(gateNetwork))
                return null;
            List<string> jumpgateIDs = jumpgateNetwork[gateNetwork];
            List<Vessel> jumpgates = new List<Vessel>();
            List<string> doomed = new List<string>();
            int count = jumpgateIDs.Count;
            string vesselID = string.Empty;
            Vessel vessel;
            double jumpRange = 0;

            for (int index = 0; index < count; index++)
            {
                vessel = GetVessel(jumpgateIDs[index]);
                if (vessel != null)
                {
                    jumpRange = Math.Abs((originPoint - vessel.GetWorldPos3D()).magnitude);

                    if (maxJumpRange < 0 && jumpRange > 0)
                        jumpgates.Add(vessel);
                    else if (jumpRange > 0 && jumpRange <= (maxJumpRange * kLightYear))
                        jumpgates.Add(vessel);
                }

                // Invalid jumpgate
                else
                {
                    doomed.Add(jumpgateIDs[index]);
                }
            }

            // Clean invalid gates
            count = doomed.Count;
            for (int index = 0; index < count; index++)
            {
                jumpgateIDs.Remove(doomed[index]);
            }

            return jumpgates.Count > 0 ? jumpgates : null;
        }

        /// <summary>
        /// Returns the anomaly matching the desired vesselID.
        /// </summary>
        /// <param name="vesselID">A string containing the vessel ID.</param>
        /// <returns>A WBISpaceAnomaly if the anomaly can be found, or null if not.</returns>
        public WBISpaceAnomaly GetAnomaly(string vesselID)
        {
            if (spaceAnomalies.Count == 0)
                return null;

            int count = spaceAnomalies.Count;
            string vesselPID = vesselID.Replace("-", "");
            WBISpaceAnomaly anomaly;
            for (int index = 0; index < count; index++)
            {
                anomaly = spaceAnomalies[index];
                if (anomaly.vesselId == vesselPID)
                    return anomaly;
            }

            return null;
        }

        /// <summary>
        /// Attempts to locate the destination vessel based on the ID supplied.
        /// </summary>
        /// <param name="vesselID">A string containing the vessel ID</param>
        /// <returns>A Vessel if one can be found, null if not.</returns>
        public Vessel GetVessel(string vesselID)
        {
            // Check unloaded vessels first.
            int count = FlightGlobals.VesselsUnloaded.Count;
            string pid;
            string vesselPID = vesselID.Replace("-", "");
            Vessel vessel;

            for (int index = 0; index < count; index++)
            {
                vessel = FlightGlobals.VesselsUnloaded[index];
                pid = vessel.id.ToString().Replace("-", "");
                if (pid == vesselPID)
                    return vessel;
            }

            // Check loaded vessels.
            count = FlightGlobals.VesselsLoaded.Count;
            for (int index = 0; index < count; index++)
            {
                vessel = FlightGlobals.VesselsLoaded[index];
                pid = vessel.id.ToString().Replace("-", "");
                if (pid == vesselPID)
                    return vessel;
            }

            return null;
        }
        #endregion

        #region WarpTech
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

        public bool IsLandedOrSplashed(Vessel vessel)
        {
            return vessel.situation == Vessel.Situations.LANDED || vessel.situation == Vessel.Situations.SPLASHED || vessel.situation == Vessel.Situations.PRELAUNCH;
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

        /// <summary>
        /// Find the parent star of the celestial body.
        /// </summary>
        /// <param name="body">The celestial body to check.</param>
        /// <returns>A CelestialBody that is the query parameter's star, or null.</returns>
        public CelestialBody GetParentStar(CelestialBody body)
        {
            CelestialBody parent = body.referenceBody;
            while (parent != null && !IsAStar(parent))
            {
                parent = parent.referenceBody;
            }

            return IsAStar(parent) ? parent : null;
        }

        public double ConverToMeters(double distance, string units)
        {
            double distanceMeters = distance;

            if (units == "Km")
                distanceMeters *= 1000;
            else if (units == "Mm")
                distanceMeters *= kMegaMeter;
            else if (units == "Gm")
                distanceMeters *= kGigaMeter;
            else
                distanceMeters *= kLightYear;

            return distanceMeters;
        }
        #endregion

        #endregion

        #region Helpers
        private void removeJumpgates()
        {
            int count = spaceAnomalies.Count;
            List<WBISpaceAnomaly> doomedAnomalies = new List<WBISpaceAnomaly>();
            WBISpaceAnomaly anomaly;
            Vessel vessel;

            for (int index = 0; index < count; index++)
            {
                anomaly = spaceAnomalies[index];
                if (anomaly.anomalyType != WBIAnomalyTypes.jumpGate)
                    continue;

                vessel = GetVessel(anomaly.vesselId);
                if (vessel != null)
                    FlightGlobals.RemoveVessel(vessel);
                doomedAnomalies.Add(anomaly);
            }

            // Now clean up our anomalies and jumpgates list.
            count = doomedAnomalies.Count;
            for (int index = 0; index < count; index++)
            {
                removeAnomalyAndJumpgate(doomedAnomalies[index]);
            }
        }

        private void removeSpaceAnomalies()
        {
            if (spaceAnomalies == null)
                return;
            int count = spaceAnomalies.Count;
            List<WBISpaceAnomaly> doomedAnomalies = new List<WBISpaceAnomaly>();
            WBISpaceAnomaly anomaly;
            List<Vessel> doomedVessels = new List<Vessel>();
            Vessel vessel;

            for (int index = 0; index < count; index++)
            {
                anomaly = spaceAnomalies[index];
                vessel = GetVessel(anomaly.vesselId);
                if (vessel != null)
                    FlightGlobals.RemoveVessel(vessel);
                doomedAnomalies.Add(anomaly);
            }

            // Now clean up our anomalies and jumpgates list.
            count = doomedAnomalies.Count;
            for (int index = 0; index < count; index++)
            {
                removeAnomalyAndJumpgate(doomedAnomalies[index]);
            }

            // Finally, find any orphaned anomalies and remove them.
            count = FlightGlobals.Vessels.Count;
            for (int index = 0; index < count; index++)
            {
                vessel = FlightGlobals.Vessels[index];
                if (vessel.vesselName.Contains(WBISpaceAnomaly.kAnomalyPrefix))
                    doomedVessels.Add(vessel);
            }
            count = doomedVessels.Count;
            for (int index = 0; index < count; index++)
            {
                vessel = doomedVessels[index];
                FlightGlobals.RemoveVessel(vessel);
            }
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

        private void removeExpiredAnomalies()
        {
            WBISpaceAnomaly anomaly;
            int count = spaceAnomalies.Count;
            double currentTime = Planetarium.GetUniversalTime();
            List<WBISpaceAnomaly> doomedAnomalies = new List<WBISpaceAnomaly>();

            for (int index = 0; index < count; index++)
            {
                anomaly = spaceAnomalies[index];

                // If expiration date hasn't been set then set it now.
                if (anomaly.expirationDate == 0)
                {
                    // Check for range between minimum and maximum lifetime.
                    if (anomaly.minLifetime > 0 && anomaly.maxLifetime > 0)
                    {
                        if (anomaly.minLifetime > 0)
                            anomaly.expirationDate = currentTime + UnityEngine.Random.Range((float)anomaly.minLifetime, (float)anomaly.maxLifetime);
                    }

                    // Check for max lifetime
                    else if (anomaly.maxLifetime > 0)
                    {
                        anomaly.expirationDate = currentTime + anomaly.maxLifetime;
                    }

                    // Infinite lifetime
                    else
                    {
                        anomaly.expirationDate = -1;
                    }
                }

                // If we've passed the expiration date and the anomaly hasn't been discovered then remove the anomaly.
                else if (anomaly.expirationDate > 0 && currentTime >= anomaly.expirationDate)
                {
                    Vessel doomed = GetVessel(anomaly.vesselId);
                    if (doomed != null && doomed.DiscoveryInfo.Level == DiscoveryLevels.Presence)
                        FlightGlobals.RemoveVessel(doomed);

                    doomedAnomalies.Add(anomaly);
                }
            }

            // Now clean up our anomalies and jumpgates list.
            count = doomedAnomalies.Count;
            for (int index = 0; index < count; index++)
            {
                removeAnomalyAndJumpgate(doomedAnomalies[index]);
            }
        }

        private void removeAnomalyAndJumpgate(WBISpaceAnomaly doomed)
        {
            if (spaceAnomalies.Contains(doomed))
                spaceAnomalies.Remove(doomed);

            string[] networkIDs = jumpgateNetwork.Keys.ToArray();
            List<string> jumpgates;
            List<string> doomedNetworks = new List<string>();
            for (int index = 0; index < networkIDs.Length; index++)
            {
                jumpgates = jumpgateNetwork[networkIDs[index]];
                if (jumpgates.Contains(doomed.vesselId))
                    jumpgates.Remove(doomed.vesselId);
                if (jumpgates.Count == 0)
                    doomedNetworks.Add(networkIDs[index]);
            }

            int count = doomedNetworks.Count;
            for (int index = 0; index < count; index++)
                jumpgateNetwork.Remove(doomedNetworks[index]);
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
            spawnJumpgates = BlueshiftSettings.JumpgatesEnabled;
            jumpgateStartupIsDestructive = BlueshiftSettings.JumpgateStartupIsDestructive;

            if (!spawnSpaceAnomalies)
            {
                removeSpaceAnomalies();
            }
            else if (!spawnJumpgates)
            {
                removeJumpgates();
            }
            else
            {
                anomalyTimer = 0;
            }
        }

        private void onVesselWasModified(Vessel vessel)
        {
            WBIJumpGate jumpGate = vessel.FindPartModuleImplementing<WBIJumpGate>();
            if (jumpGate == null)
                return;

            AddJumpgateToNetwork(vessel.id.ToString(), jumpGate.networkID);
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

                if (nodeSettings.HasValue(kMinRendezvousDistancePlanetary))
                    double.TryParse(nodeSettings.GetValue(kMinRendezvousDistancePlanetary), out minRendezvousDistancePlanetary);

                if (nodeSettings.HasValue(kMinRendezvousDistanceInterplanetary))
                    double.TryParse(nodeSettings.GetValue(kMinRendezvousDistanceInterplanetary), out minRendezvousDistanceInterplanetary);

                if (nodeSettings.HasValue(kRendezvousDistance))
                    float.TryParse(nodeSettings.GetValue(kRendezvousDistance), out rendezvousDistance);

                if (nodeSettings.HasValue(kDebugMode))
                    bool.TryParse(nodeSettings.GetValue(kDebugMode), out debugMode);

                if (nodeSettings.HasValue(kWarpEngineerSkill))
                    warpEngineerSkill = nodeSettings.GetValue(kWarpEngineerSkill);

                if (nodeSettings.HasValue(kWarpSpeedBoostRank))
                    int.TryParse(nodeSettings.GetValue(kWarpSpeedBoostRank), out warpSpeedBoostRank);

                if (nodeSettings.HasValue(kWarpSpeedSkillMultiplier))
                    float.TryParse(nodeSettings.GetValue(kWarpSpeedSkillMultiplier), out warpSpeedSkillMultiplier);
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

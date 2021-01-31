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
        private static string kInterstellarWarpSpeedMultiplier = "interstellarWarpSpeedMultiplier";
        private static string kHomeSOIMultiplier = "homeSOIMultiplier";
        private static string kBlueshiftSettings = "BLUESHIFT_SETTINGS";
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
        /// In game, the Sun has infinite Sphere of Influence, so we compute an artificial one based on the furthest planet from the Sun. To give a little wiggle room,
        /// we multiply the computed value by this multiplier.
        /// </summary>
        private double homeSOIMultiplier = 1.1;
        #endregion

        #region Overrides
        /// <summary>
        /// Handles the awake event.
        /// </summary>
        public override void OnAwake()
        {
            base.OnAwake();
            shared = this;
            loadSettings();

            if (HighLogic.LoadedSceneIsFlight)
                calculateHomeSOI();
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
        #endregion

        #region Helpers
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
                if (!IsAStar(body) && body.orbit.semiMajorAxis > homeSystemSOI)
                    homeSystemSOI = body.orbit.semiMajorAxis;
            }

            // Add our home SOI multiplier.
            homeSystemSOI *= homeSOIMultiplier;
        }
        #endregion
    }
}

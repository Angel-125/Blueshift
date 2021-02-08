using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Blueshift
{
    /// <summary>
    /// Space anomalies can be set up in a variety of different orbits.
    /// </summary>
    public enum WBIAnomalySpawnModes
    {
        #region Modes
        /// <summary>
        /// Spawns in a random solar or planetary orbit.
        /// </summary>
        randomOrbit,

        /// <summary>
        /// Spawns in a random solar orbit.
        /// </summary>
        randomSolarOrbit,

        /// <summary>
        /// Spawns in a random planetary orbit.
        /// </summary>
        randomPlanetOrbit,

        /// <summary>
        /// Spawns in random orbit of every last planet in each star system. One per each planet. Ignores maxInstances.
        /// </summary>
        everyLastPlanet,

        /// <summary>
        /// Spawns in a fixed orbit. One anomaly per orbit. Ignores maxInstances and orbitType.
        /// </summary>
        fixedOrbit
        #endregion
    }

    /// <summary>
    /// Describes the type of orbit to create when spawning a space anomaly.
    /// </summary>
    public enum WBIAnomalyOrbitTypes
    {
        /// <summary>
        /// Your garden variety elliptical orbit.
        /// </summary>
        elliptical,

        /// <summary>
        /// Fly-by orbit
        /// </summary>
        flyBy,

        /// <summary>
        /// Randomly select either elliptical or flyBy.
        /// </summary>
        random
    }
}

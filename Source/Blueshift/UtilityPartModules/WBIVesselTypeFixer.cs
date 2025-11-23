using System.Collections.Generic;
using System.Text;
using UnityEngine;
using KSP.Localization;

namespace Blueshift
{
    /// <summary>
    /// This part module is a small helper class to fix an issue where the the player claims the space anomaly and it's uncontrollable.
    /// </summary>
    public class WBIVesselTypeFixer: PartModule
    {
        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            // Fix an issue where the the player claims the space anomaly and it's uncontrollable.
            if (vessel.vesselType == VesselType.SpaceObject)
            {
                WBISpaceAnomaly anomaly = BlueshiftScenario.shared.GetAnomaly(vessel.id.ToString());
                if (anomaly != null)
                {
                    vessel.vesselType = anomaly.vesselType;
                }
            }
        }
    }
}

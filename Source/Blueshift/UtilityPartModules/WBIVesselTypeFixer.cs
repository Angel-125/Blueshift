using System.Collections.Generic;
using System.Text;
using UnityEngine;
using KSP.Localization;

namespace Blueshift
{
    /// <summary>
    /// This part module is a small helper class to fix 
    /// </summary>
    public class WBIVesselTypeFixer: VesselModule
    {
        protected override void OnStart()
        {
            base.OnStart();

            // Fix an issue where the the player claims the space anomaly and it's uncontrollable.
            if (vessel.vesselType == VesselType.SpaceObject)
            {
                WBISpaceAnomaly anomaly = BlueshiftScenario.shared.GetAnomaly(vessel.id.ToString());
                if (anomaly != null)
                {
                    vessel.vesselType = anomaly.vesselType;
                }
                else
                {
                    vessel.vesselType = VesselType.Debris;
                }
            }
        }

        public override bool ShouldBeActive()
        {
            if (HighLogic.LoadedSceneIsFlight)
                return true;

            return base.ShouldBeActive();
        }
    }
}

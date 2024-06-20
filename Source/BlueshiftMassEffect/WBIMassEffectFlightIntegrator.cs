using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using ModularFI;

namespace BlueshiftMassEffect
{
    [KSPAddon(KSPAddon.Startup.SpaceCentre, false)]
    class WBIMassEffectFlightIntegrator: MonoBehaviour
    {
        public void Awake()
        {
            //ModularFlightIntegrator.RegisterUpdateMassStatsOverride(UpdateMassStats);
        }

        private void UpdateMassStats(ModularFlightIntegrator flightIntegrator)
        {
            flightIntegrator.BaseFIUpdateMassStats();

            Part part = flightIntegrator.PartRef;
            Vessel vessel = part.vessel;
            int count = vessel.parts.Count;

            for (int index = 0; index < count; index++)
            {
                if (part.rb != null)
                {
                    part.physicsMass *= 0.01f;
                    if (!part.packed)
                        part.rb.mass = (float)part.physicsMass;
                }
            }
        }
    }
}

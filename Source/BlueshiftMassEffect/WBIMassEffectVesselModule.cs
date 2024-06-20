using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlueshiftMassEffect
{
    public class WBIMassEffectVesselModule: VesselModule
    {
        public float massReductionFactor = 1f;

        #region LifecycleMethods
        public override Activation GetActivation()
        {
            return Activation.LoadedVessels;
        }

        public override bool ShouldBeActive()
        {
            return vessel.loaded;
        }

        protected override void OnStart()
        {
            base.OnStart();
        }

        protected override void OnSave(ConfigNode node)
        {
            base.OnSave(node);
        }

        protected override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);
        }

        public void FixedUpdate()
        {
            if (!HighLogic.LoadedSceneIsFlight)
                return;

            WBIMassEffectCore[] massEffectCores = WBIMassEffectUtilities.getActiveCores(Vessel);
            massReductionFactor = WBIMassEffectUtilities.calculateMassReduction(massEffectCores);
        }
        #endregion

        #region API
        #endregion
    }
}

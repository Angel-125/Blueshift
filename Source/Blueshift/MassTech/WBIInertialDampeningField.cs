using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using KSP.IO;
using KSP.Localization;

namespace Blueshift.MassTech
{
    /// <summary>
    /// Applies the Higgs field to engines and RCS thrusters, improving their thrust and Isp.
    /// </summary>
    public class WBIInertialDampeningField: WBIPartModule
    {
        #region Housekeeping
        float totalInertialDampeningFactor;
        List<EngineCurves> engineModuleCurves;
        List<ModuleRCSFX> rcsModules;
        #endregion

        #region Overrides
        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            if (!HighLogic.LoadedSceneIsEditor && !HighLogic.LoadedSceneIsFlight)
                return;
            if (HighLogic.LoadedSceneIsFlight)
                WBIInertialDampener.onDampenerUpdated.Add(onDampenerUpdated);
            else if (HighLogic.LoadedSceneIsEditor)
                WBIInertialDampener.onDampenerUpdatedEditor.Add(DampenerUpdated);

            findEngineModuleCurves();

            WBIInertialDampener attenuator = getPrimaryDampener();
            if (attenuator != null && attenuator.IsActivated && !attenuator.isMissingResources)
                DampenerUpdated(attenuator);
        }

        public void OnDestroy()
        {
            if (HighLogic.LoadedSceneIsFlight)
                WBIInertialDampener.onDampenerUpdated.Remove(onDampenerUpdated);
            else if (HighLogic.LoadedSceneIsEditor)
                WBIInertialDampener.onDampenerUpdatedEditor.Remove(DampenerUpdated);
        }
        #endregion

        #region Helpers
        void DampenerUpdated(WBIInertialDampener attenuator)
        {
            totalInertialDampeningFactor = attenuator.totalInertialDampeningFactor;
            if (totalInertialDampeningFactor <= 0)
                totalInertialDampeningFactor = 0;

            updateEngineModuleCurves();
        }

        void onDampenerUpdated(WBIInertialDampener attenuator, Vessel vesselAffected)
        {
            if (vesselAffected != part.vessel)
                return;
            DampenerUpdated(attenuator);
        }

        void updateEngineModuleCurves()
        {
            EngineCurves engineCurve;
            int count = engineModuleCurves.Count;
            for (int index = 0; index < count; index++)
            {
                engineCurve = engineModuleCurves[index];
                engineCurve.UpdateCurves(totalInertialDampeningFactor);
            }
        }

        void findEngineModuleCurves()
        {
            engineModuleCurves = new List<EngineCurves>();
            List<ModuleEngines> engines = part.FindModulesImplementing<ModuleEngines>();
            ModuleEngines engine;
            int count = engines.Count;
            EngineCurves engineCurve;
            for (int index = 0; index < count; index++)
            {
                engine = engines[index];
                engineCurve = new EngineCurves(engine);
                engineModuleCurves.Add(engineCurve);
            }
        }

        WBIInertialDampener getPrimaryDampener()
        {
            if (HighLogic.LoadedSceneIsFlight)
            {
                List<WBIInertialDampener> dampeners = part.FindModulesImplementing<WBIInertialDampener>();
                if (dampeners.Count > 0)
                    return dampeners[0];
            }
            else if (HighLogic.LoadedSceneIsEditor)
            {
                ShipConstruct ship = EditorLogic.fetch.ship;
                List<WBIInertialDampener> dampeners = new List<WBIInertialDampener>();
                int count = ship.parts.Count;
                for (int index = 0; index < count; index++)
                {
                    if (ship.parts[index].HasModuleImplementing<WBIInertialDampener>())
                        dampeners.AddRange(ship.parts[index].FindModulesImplementing<WBIInertialDampener>());
                }
                if (dampeners.Count > 0)
                    return dampeners[0];
            }

            return null;
        }
        #endregion
    }
}

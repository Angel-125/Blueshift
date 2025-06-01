using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using KSP.IO;
using KSP.Localization;

namespace Blueshift
{
    internal class EngineCurves
    {
        public ModuleEngines engine;
        public FloatCurve originalThrustCurve;
        public FloatCurve originalAtmosphereCurve;
        public float originalMaxThrust;
        public float originalMinThrust;
        public float originalMaxFuelFlow;
        public float originalMinFuelFlow;

        public EngineCurves(ModuleEngines moduleEngines)
        {
            engine = moduleEngines;
            originalMaxThrust = engine.maxThrust;
            originalMinThrust = engine.minThrust;
            originalMinFuelFlow = engine.minFuelFlow;
            originalMaxFuelFlow = engine.maxFuelFlow;
            originalThrustCurve = CopyFloatCurve(engine.thrustCurve);
            originalAtmosphereCurve = CopyFloatCurve(engine.atmosphereCurve);
        }

        public FloatCurve CopyFloatCurve(FloatCurve source)
        {
            FloatCurve floatCurveCopy = new FloatCurve();
            ConfigNode node = new ConfigNode("FloatCurve");

            source.Save(node);
            floatCurveCopy.Load(node);

            return floatCurveCopy;
        }

        public void UpdateCurves(float inertialDampeningFactor)
        {
            if (inertialDampeningFactor <= 0)
            {
                engine.minThrust = originalMinThrust;
                engine.maxThrust = originalMaxThrust;
                engine.minFuelFlow = originalMinFuelFlow;
                engine.maxFuelFlow = originalMaxFuelFlow;
                engine.thrustCurve = originalThrustCurve;
                engine.atmosphereCurve = originalAtmosphereCurve;
                GameEvents.onEngineThrustPercentageChanged.Fire(engine);
                return;
            }

            // Useful for reporting the engine thrust on some displays like Kerbal Engineer.
            engine.minThrust = originalMinThrust * inertialDampeningFactor;
            engine.maxThrust = originalMaxThrust * inertialDampeningFactor;

            // We're trying to simulate what happens when inertia is cancelled out. We can't do that directly- it really messes up KSP's performance calculations.
            // My research shows that we can simulate the effects of reduced inertial mass by increasing Isp.
            // With the inertial dampener:
            // 1. Particles in the exhaust accelerate more easily, needing less energy to reach higher speeds.
            // 2. This causes the exhaust velocity (Ve) to increase.
            // How do we do this in KSP? DON'T MESS WITH MASS FLOW RATE. Just modify Isp.
            // When you divide the fuel flow and subsequently multiply the Isp,
            // Your engine thrust will stay the same but your burn time and delta-v will go up.
            // If you just modify Isp, your burn time will remain the same but your thrust and delta-v will increase.
            // Increased delta-v and thrust is what we want to do to simulate the reduction in inertial mass.
            // Hence, there's no point in fiddling with both Isp and mass flow to balance it all out.
            // Just modify ISP, which will ultimately affect thrust by:
            // Thrust = mass flow rate * exhaust velocity, where exhaust velocity = Isp * 9.80665
            //engine.minFuelFlow = originalMinFuelFlow / inertialDampeningFactor;
            //engine.maxFuelFlow = originalMaxFuelFlow / inertialDampeningFactor;

            FloatCurve floatCurve = CopyFloatCurve(originalThrustCurve);
            multiplyCurve(ref floatCurve, inertialDampeningFactor);
            engine.thrustCurve = floatCurve;

            floatCurve = CopyFloatCurve(originalAtmosphereCurve);
            multiplyCurve(ref floatCurve, inertialDampeningFactor);
            engine.atmosphereCurve = floatCurve;
            GameEvents.onEngineThrustPercentageChanged.Fire(engine);
        }

        private void multiplyCurve(ref FloatCurve floatCurve, float attenuationFactor)
        {
            if (floatCurve.Curve.length <= 0 || floatCurve.Curve.keys.Length <= 0)
                return;

            AnimationCurve animationCurve = floatCurve.Curve;
            AnimationCurve modifiedCurve = new AnimationCurve();
            Keyframe keyFrame;
            for (int index = 0; index < animationCurve.keys.Length; index++)
            {
                // Why are we doing this? because modifying the key frames directly doesn't get them updated.
                // Don't ask, I don't know why that is.
                keyFrame = new Keyframe();
                keyFrame.time = animationCurve.keys[index].time;
                keyFrame.value = animationCurve.keys[index].value * attenuationFactor;
                keyFrame.inTangent = animationCurve.keys[index].inTangent * attenuationFactor;
                keyFrame.outTangent = animationCurve.keys[index].outTangent * attenuationFactor;
                keyFrame.inWeight = animationCurve.keys[index].inWeight * attenuationFactor;
                keyFrame.outWeight = animationCurve.keys[index].outWeight * attenuationFactor;
                modifiedCurve.AddKey(keyFrame);
            }
            floatCurve.Curve = modifiedCurve;
        }
    }
}

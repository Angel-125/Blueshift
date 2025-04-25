using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Blueshift
{
    public class WBIWarpEffectsAligner: WBIPartModule
    {
        [KSPField]
        public string effectsTransformName;

        [KSPField]
        public string rootTransformName;

        Transform effectTransform;
        Transform rootTransform;

        public override void OnAwake()
        {
            base.OnAwake();

            if (!string.IsNullOrEmpty(effectsTransformName))
                effectTransform = part.FindModelTransform(effectsTransformName);

            if (!string.IsNullOrEmpty(rootTransformName))
                rootTransform = part.FindModelTransform(rootTransformName);
        }

        public override void OnUpdate()
        {
            base.OnUpdate();
            if (!HighLogic.LoadedSceneIsFlight)
                return;

            if (effectTransform != null)
            {
                effectTransform.rotation = part.vessel.transform.rotation;
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.IO;

namespace Blueshift
{
    public class WBIStaticAttach : WBIPartModule
    {
        private const string kStaticAttachName = "StaticAttachBody";

        [KSPField(isPersistant = true)]
        protected bool isDeployed;

        GameObject staticAttachObject;
        FixedJoint staticAttachJoint;

        [KSPEvent(guiActive = true, guiName = "#LOC_BLUESHIFT_enableGroundAttach")]
        public void EnableGroundAttachment()
        {
            isDeployed = true;

            Events["DisableGroundAttachment"].active = true;
            Events["EnableGroundAttachment"].active = false;

            SetStaticAttach();
        }

        [KSPEvent(guiActive = true, guiName = "#LOC_BLUESHIFT_disableGroundAttach")]
        public void DisableGroundAttachment()
        {
            isDeployed = false;

            SetStaticAttach(false);

            Events["DisableGroundAttachment"].active = false;
            Events["EnableGroundAttachment"].active = true;
        }

        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            if (!HighLogic.LoadedSceneIsFlight)
                return;

            if (isDeployed)
            {
                SetStaticAttach();
                Events["DisableGroundAttachment"].active = true;
                Events["EnableGroundAttachment"].active = false;
            }
            else
            {
                Events["DisableGroundAttachment"].active = false;
                Events["EnableGroundAttachment"].active = true;
            }
        }

        public void SetStaticAttach(bool isAttached = true)
        {
            if (!HighLogic.LoadedSceneIsFlight)
                return;
            if (part.vessel.situation != Vessel.Situations.LANDED &&
                part.vessel.situation != Vessel.Situations.PRELAUNCH)
                return;

            //Set ground contact
            part.PermanentGroundContact = isAttached;

            //Zero velocity
            int partCount = part.vessel.parts.Count;
            Rigidbody rigidBody;
            for (int index = 0; index < partCount; index++)
            {
                rigidBody = part.vessel.parts[index].Rigidbody;
                if (rigidBody == null)
                    continue;

                rigidBody.velocity *= 0;
                rigidBody.angularVelocity *= 0f;
            }

            //Destroy the static attach if needed
            if (staticAttachJoint != null)
                Destroy(staticAttachJoint);
            if (staticAttachObject != null)
                Destroy(staticAttachObject);

            //Setup static attach object
            if (isAttached)
            {
                staticAttachObject = new GameObject(kStaticAttachName + part.GetInstanceID());
                Rigidbody staticRigidBody = staticAttachObject.AddComponent<Rigidbody>();
                staticRigidBody.isKinematic = true;
                staticAttachObject.transform.position = part.transform.position;
                staticAttachObject.transform.rotation = part.transform.rotation;

                //Setup the attachment joint
                staticAttachJoint = staticAttachObject.AddComponent<FixedJoint>();
                staticAttachJoint.connectedBody = part.Rigidbody;
                staticAttachJoint.breakForce = float.MaxValue;
                staticAttachJoint.breakTorque = float.MaxValue;
            }
        }
    }
}

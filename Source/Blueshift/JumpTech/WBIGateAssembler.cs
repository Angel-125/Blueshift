using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using KSP.Localization;

namespace Blueshift
{
    /// <summary>
    /// This is a helper module for jumpgates that are built insted of ones that are single-piece.
    /// </summary>
    public class WBIGateAssembler: WBIPartModule
    {
        #region Fields
        /// <summary>
        /// Name of the part that forms part of the ring. This part will be decoupled and deleted from the
        /// vessel when assembling the jumpgate. When that happens, one of the segmentMesh items will be
        /// enabled. Once all the segmentMesh entries are enabled, the ring becomes fully operational.
        /// </summary>
        [KSPField]
        public string supportSegmentPartName = string.Empty;

        /// <summary>
        /// When fully assembed, where to place the center of mass
        /// </summary>
        [KSPField]
        public string assembledCoM = string.Empty;

        /// <summary>
        /// Current count of enabled mesh segments.
        /// </summary>
        [KSPField(isPersistant = true)]
        public int enabledMeshCount = 0;

        /// <summary>
        /// Name of the portal trigger for the jumpgate.
        /// </summary>
        [KSPField]
        public string portalTriggerName = "portalTrigger";
        #endregion

        #region Housekeeping
        WBIJumpGate jumpgateController;
        string[] segmentMeshNames = null;
        List<Transform> segmentMeshes = new List<Transform>();
        Transform portalTrigger = null;
        #endregion

        #region Overrides
        public override void OnStart(StartState state)
        {
            base.OnStart(state);

            // Find the jumpgate controller
            jumpgateController = part.FindModuleImplementing<WBIJumpGate>();

            // Get mesh segments
            getMeshSegments();
            portalTrigger = part.FindModelTransform(portalTriggerName);

            // Enable all the segments that we've installed.
            for (int index = 0; index < enabledMeshCount; index++)
                setMeshEnabled(index, true);

            // Enable the controller if we've enabled all the segments
            enableJumpgateIfNeeded();
        }
        #endregion

        #region Events
        /// <summary>
        /// Adds new segment to the jumpgate if one can be found. The located segment will be destroyed.
        /// </summary>
        [KSPEvent(guiName = "#LOC_BLUESHIFT_jumpGateInstallSegment", guiActive = true)]
        public void AddSegment()
        {
            // Make sure that we have a segment to add.
            if (!vesselHasSegmentToAdd())
            {
                ScreenMessages.PostScreenMessage(Localizer.Format("#LOC_BLUESHIFT_jumpGateSegmentNotFound"), 5.0f, ScreenMessageStyle.UPPER_CENTER);
                return;
            }

            // Increment enabled mesh count
            enabledMeshCount += 1;
            if (enabledMeshCount > segmentMeshes.Count)
                enabledMeshCount = segmentMeshes.Count;

            // Enable the mesh
            setMeshEnabled(enabledMeshCount - 1, true);

            // if we've added all the segments then enable the jumpgate controller.
            enableJumpgateIfNeeded(false);
            ScreenMessages.PostScreenMessage(Localizer.Format("#LOC_BLUESHIFT_jumpGateSegmentAdded"), 5.0f, ScreenMessageStyle.UPPER_CENTER);
        }
        #endregion

        #region Private methods
        private bool vesselHasSegmentToAdd()
        {
            if (string.IsNullOrEmpty(supportSegmentPartName))
                return false;

            // Find a vessel that has a ring segment to install.
            Vessel[] vesselsLoaded = FlightGlobals.VesselsLoaded.ToArray();
            Vessel loadedVessel;
            int partCount;
            Part doomed;
            for (int index = 0; index < vesselsLoaded.Length; index++)
            {
                loadedVessel = vesselsLoaded[index];
                partCount = loadedVessel.parts.Count;
                for (int partIndex = 0; partIndex < partCount; partIndex++)
                {
                    if (loadedVessel.parts[partIndex].partInfo.name == supportSegmentPartName)
                    {
                        // We got one, kill it!
                        doomed = loadedVessel.parts[partIndex];
                        doomed.decouple();
                        doomed.Die();
                        return true;
                    }
                }
            }

            return false;
        }

        private void enableJumpgateIfNeeded(bool verbose = true)
        {
            if (enabledMeshCount >= segmentMeshes.Count)
            {
                updateCoM();
                if (HighLogic.LoadedSceneIsFlight)
                    jumpgateController.SetGateEnabled(true);
                setPortalTriggerEnabled(true);
                Events["AddSegment"].guiActive = false;
                Events["AddSegment"].guiActiveEditor = false;
                if (verbose)
                    ScreenMessages.PostScreenMessage(Localizer.Format("#LOC_BLUESHIFT_jumpGateCompleted"), 5.0f, ScreenMessageStyle.UPPER_CENTER);
            }
            else
            {
                if (HighLogic.LoadedSceneIsFlight)
                    jumpgateController.SetGateEnabled(false);
            }
        }

        private void updateCoM()
        {
            if (string.IsNullOrEmpty(assembledCoM))
                return;
            string[] dimensions = assembledCoM.Split(new char[] { ',' });
            if (dimensions.Length < 3)
                return;

            float x = 0;
            float y = 0;
            float z = 0;
            float.TryParse(dimensions[0], out x);
            float.TryParse(dimensions[1], out y);
            float.TryParse(dimensions[2], out z);

            part.CoMOffset = new Vector3(x, y, z);
        }

        private void getMeshSegments()
        {
            ConfigNode node = getPartConfigNode();
            if (node == null || !node.HasValue("segmentMesh"))
                return;

            segmentMeshNames = node.GetValues("segmentMesh");
            segmentMeshes.Clear();
            Transform mesh;
            for (int index = 0; index < segmentMeshNames.Length; index++)
            {
                mesh = part.FindModelTransform(segmentMeshNames[index]);
                if (mesh != null)
                {
                    segmentMeshes.Add(mesh);
                    setMeshEnabled(index, false);
                }
            }
            setPortalTriggerEnabled(false);
        }

        private void setMeshEnabled(int index, bool isEnabled)
        {
            if (index >= segmentMeshes.Count || index < 0)
                return;

            segmentMeshes[index].gameObject.SetActive(isEnabled);

            Collider[] colliders = segmentMeshes[index].GetComponentsInChildren<Collider>();
            for (int colliderIndex = 0; colliderIndex < colliders.Length; colliderIndex++)
                colliders[colliderIndex].enabled = isEnabled;
        }

        private void setPortalTriggerEnabled(bool isEnabled)
        {
            if (portalTrigger == null)
                return;

            portalTrigger.gameObject.SetActive(isEnabled);

            Collider[] colliders = portalTrigger.GetComponentsInChildren<Collider>();

            for (int colliderIndex = 0; colliderIndex < colliders.Length; colliderIndex++)
                colliders[colliderIndex].enabled = isEnabled;
        }
        #endregion
    }
}

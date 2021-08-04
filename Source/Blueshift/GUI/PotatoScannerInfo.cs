using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.Localization;

namespace Blueshift
{
    struct WBIAsteroidCometData
    {
        public ModuleAsteroid moduleAsteroid;
        public ModuleComet moduleComet;
        public ModuleSpaceObjectInfo spaceObjectInfo;
        public List<ModuleSpaceObjectResource> resources;
    }

    public class PotatoScannerInfo : Dialog<PotatoScannerInfo>
    {
        public Part part;

        Vector2 scrollPosition, scrollPos2 =  new Vector2();
        List<WBIAsteroidCometData> astroData;

        public PotatoScannerInfo() :
            base("Asteroid Analysis", 380, 450)
        {
            Resizable = false;
            WindowTitle = Localizer.Format("#LOC_BLUESHIFT_scannerTitle");
            astroData = new List<WBIAsteroidCometData>();
        }

        public override void SetVisible(bool newValue)
        {
            base.SetVisible(newValue);
            if (newValue)
            {
                astroData = new List<WBIAsteroidCometData>();
                List<ModuleAsteroid> moduleAsteroids = part.vessel.FindPartModulesImplementing<ModuleAsteroid>();
                if (moduleAsteroids != null)
                {
                    int count = moduleAsteroids.Count;
                    WBIAsteroidCometData asteroid;
                    for (int index = 0; index < count; index++)
                    {
                        asteroid = new WBIAsteroidCometData();
                        asteroid.moduleAsteroid = moduleAsteroids[index];
                        asteroid.spaceObjectInfo = asteroid.moduleAsteroid.part.FindModuleImplementing<ModuleSpaceObjectInfo>();
                        asteroid.resources = asteroid.moduleAsteroid.part.FindModulesImplementing<ModuleSpaceObjectResource>();

                        astroData.Add(asteroid);
                    }
                }

                List<ModuleComet> moduleComets = part.vessel.FindPartModulesImplementing<ModuleComet>();
                if (moduleComets != null)
                {
                    int count = moduleComets.Count;
                    WBIAsteroidCometData comet;
                    for (int index = 0; index < count; index++)
                    {
                        comet = new WBIAsteroidCometData();
                        comet.moduleComet = moduleComets[index];
                        comet.spaceObjectInfo = comet.moduleComet.part.FindModuleImplementing<ModuleSpaceObjectInfo>();
                        comet.resources = comet.moduleComet.part.FindModulesImplementing<ModuleSpaceObjectResource>();

                        astroData.Add(comet);
                    }
                }
            }
        }

        protected override void DrawWindowContents(int windowId)
        {
            GUILayout.BeginVertical();
            scrollPosition = GUILayout.BeginScrollView(scrollPosition);

            if (astroData.Count == 0)
            {
                GUILayout.Label(Localizer.Format("#LOC_BLUESHIFT_noPotato"));
                GUILayout.EndScrollView();
                GUILayout.EndVertical();
                return;
            }

            int count = astroData.Count;
            WBIAsteroidCometData astroDataItem;
            string objectName = string.Empty;
            string resourceString = string.Empty;
            string resourcePercentage = string.Empty;
            string displayMass = string.Empty;
            float density = 0;
            for (int index = 0; index < count; index++)
            {
                astroDataItem = astroData[index];

                if (astroDataItem.moduleAsteroid != null)
                {
                    objectName = astroDataItem.moduleAsteroid.AsteroidName;
                    density = astroDataItem.moduleAsteroid.density;
                }
                else if (astroDataItem.moduleComet != null)
                {
                    objectName = astroDataItem.moduleComet.CometName;
                    density = astroDataItem.moduleComet.density;
                }

                displayMass = astroDataItem.spaceObjectInfo.displayMass;
                resourcePercentage = astroDataItem.spaceObjectInfo.resources;

                drawSpaceObjectInfo(objectName, density, displayMass, resourcePercentage, astroDataItem.resources);
            }

            GUILayout.EndScrollView();
            GUILayout.EndVertical();
        }

        private void drawSpaceObjectInfo(string name, float density, string displayMass, string resourcePercentage, List<ModuleSpaceObjectResource> resources)
        {
            GUILayout.Label(Localizer.Format("#LOC_BLUESHIFT_objectName", new string[1] { name }));
            GUILayout.Label(Localizer.Format("#LOC_BLUESHIFT_objectDensity", new string[1] { string.Format("{0:n2}", density) }));
            GUILayout.Label(Localizer.Format("#LOC_BLUESHIFT_objectMass", new string[1] { displayMass }));
            GUILayout.Label(Localizer.Format("#LOC_BLUESHIFT_objectResourceMass", new string[1] { resourcePercentage }));

            if (resources != null && resources.Count > 0)
            {
                GUILayout.Label(Localizer.Format("#LOC_BLUESHIFT_objectResources"));
                int count = resources.Count;
                ModuleSpaceObjectResource resource;
                string resourceDisplay;
                PartResourceDefinitionList definitions = PartResourceLibrary.Instance.resourceDefinitions;
                PartResourceDefinition resourceDef;
                for (int index = 0; index < count; index++)
                {
                    resource = resources[index];
                    resourceDef = definitions[resource.resourceName];
                    if (resourceDef == null)
                        continue;
                    if (resource.abundance > 0)
                    {
                        resourceDisplay = string.Format("{0:f2}%", resource.displayAbundance * 100f);
                        GUILayout.Label(Localizer.Format("#LOC_BLUESHIFT_objectResourceInfo", new string[2] { resourceDef.displayName, resourceDisplay }));
                    }
                }
                GUILayout.Label(" ");
            }
        }
    }
}

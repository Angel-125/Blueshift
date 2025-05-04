using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using KSP.Localization;

namespace Blueshift
{
    /// <summary>
    /// A collection of utility functions that Blueshift uses.
    /// </summary>
    public static class BlueshiftUtilities
    {
        /// <summary>
        /// Balances resource amounts so that all resources run out at the same time.
        /// 
        /// Given an array of consumption rates (units/second) and a total storage capacity (units),
        /// this function distributes the available storage across the resources so that each one depletes simultaneously.
        /// 
        /// The idea:
        ///  - For each resource, TimeToEmpty = Amount / ConsumptionRate
        ///  - All TimeToEmpty values must be equal.
        ///  - Solve for the common time (T):
        ///      T = TotalCapacity / Sum(ConsumptionRates)
        ///  - Then each resource amount is:
        ///      Amount_i = ConsumptionRate_i * T
        /// 
        /// Example Visualization:
        /// 
        /// Total Capacity = 10000 units
        /// Consumption Rates: [ 0.002, 0.005, 0.000188, 0.02 ]
        /// 
        /// Storage split across 4 resources:
        /// 
        ///  [ Resource 1 ] ▓▓▓▓▓▓▓▓░░░░░░░░░░░░
        ///
        ///  [ Resource 2 ] ▓▓▓▓▓▓▓▓▓▓▓▓░░░░░░░░
        ///
        ///  [ Resource 3 ] ▓▓░░░░░░░░░░░░░░░░░░
        ///
        ///  [ Resource 4 ] ▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓
        /// 
        /// Resources with higher consumption rates (Resource 4) get more storage.
        /// Resources with slower consumption (Resource 3) get less storage.
        /// 
        /// All resources deplete at the same time.
        /// </summary>
        /// <param name="consumptionRates">Array of resource consumption rates (units/second)</param>
        /// <param name="totalCapacity">Total storage capacity (units)</param>
        /// <returns>Array of resource amounts corresponding to each consumption rate</returns>
        /// <exception cref="ArgumentException">Thrown if inputs are invalid (null, empty, or zero rates)</exception>
        public static Dictionary<string, double> BalanceResources(Dictionary<string, double> consumptionRates, double totalCapacity)
        {
            if (consumptionRates == null || consumptionRates.Keys.Count == 0)
                throw new ArgumentException("Consumption rates cannot be null or empty.");

            if (totalCapacity <= 0)
                throw new ArgumentException("Total capacity must be positive.");

            double sumOfRates = consumptionRates.Values.Sum();

            if (sumOfRates == 0)
                throw new ArgumentException("Sum of consumption rates must be greater than zero.");

            double commonBurnTime = totalCapacity / sumOfRates;

            Dictionary<string, double> resourceAmounts = new Dictionary<string, double>();
            string[] rateKeys = consumptionRates.Keys.ToArray();
            double consumptionRate = 0;
            for (int index = 0; index < rateKeys.Length; index++)
            {
                consumptionRate = consumptionRates[rateKeys[index]];
                resourceAmounts.Add(rateKeys[index], commonBurnTime * consumptionRate);
            }

            return resourceAmounts;
        }

        public static Dictionary<string, double> GetBalancedResources(ShipConstruct ship)
        {
            // Find all the gravimetric generators that output Gravity Waves.
            List<WBIModuleGeneratorFX> generators = getGravimetricGenerators(ship);
            int generatorCount = generators.Count;
            if (generatorCount <= 0)
            {
                return null;
            }

            // For each gravimetric generator, get their resource inputs and their resource input rates to obtain all the input resources and rates needed to run all the converters.
            Dictionary<string, double> consumptionRates = new Dictionary<string, double>();
            WBIModuleGeneratorFX generator;
            int inputResourceCount = 0;
            ResourceRatio inputResource;
            for (int index = 0; index < generatorCount; index++)
            {
                generator = generators[index];
                inputResourceCount = generator.inputList.Count;
                for (int resourceIndex = 0; resourceIndex < inputResourceCount; resourceIndex++)
                {
                    inputResource = generator.inputList[resourceIndex];
                    if (inputResource.ResourceName == "ElectricCharge")
                        continue;

                    if (consumptionRates.ContainsKey(inputResource.ResourceName) == false)
                        consumptionRates.Add(inputResource.ResourceName, 0);

                    consumptionRates[inputResource.ResourceName] += inputResource.Ratio;
                }
            }

            // Now we need the total number of units of all the resources.
            // Each part has a tank with the consumed resource.
            // Have a dictionary with a list of parts that contain the resource.
            // As we build the dictionary, sum the total storage units.
            // NOTE: We need to ensure that all units are in the same volume. We assume 5-liter unit volumes.
            Dictionary<string, List<Part>> partResources = new Dictionary<string, List<Part>>();
            double totalUnitCapacity = 0;
            int partCount = ship.parts.Count;
            Part part;
            string[] consumptionRatesKeys = consumptionRates.Keys.ToArray();
            string resourceName;
            PartResourceDefinitionList definitions = PartResourceLibrary.Instance.resourceDefinitions;
            PartResourceDefinition resourceDef;
            double maxAmount;
            for (int index = 0; index < partCount; index++)
            {
                part = ship.parts[index];

                // Check the part for any consumed resources
                for (int keyIndex = 0; keyIndex < consumptionRatesKeys.Length; keyIndex++)
                {
                    resourceName = consumptionRatesKeys[keyIndex];

                    if (part.Resources.Contains(resourceName))
                    {
                        maxAmount = part.Resources[resourceName].maxAmount;

                        // Account for resource volume
                        resourceDef = definitions[resourceName];

                        // Update total unit capacity
                        totalUnitCapacity += maxAmount;

                        // Add part to the fuel tank list
                        if (partResources.ContainsKey(resourceName) == false)
                        {
                            partResources.Add(resourceName, new List<Part>());
                        }
                        partResources[resourceName].Add(part);
                    }
                }
            }

            // Now balance the amounts
            Dictionary<string, double> resourceAmounts = BalanceResources(consumptionRates, totalUnitCapacity);
            return resourceAmounts;
        }

        public static Dictionary<string, double> GetBalancedResources(Vessel ship)
        {
            // Find all the gravimetric generators that output Gravity Waves.
            List<WBIModuleGeneratorFX> generators = getGravimetricGenerators(ship);
            int generatorCount = generators.Count;
            if (generatorCount <= 0)
            {
                return null;
            }

            // For each gravimetric generator, get their resource inputs and their resource input rates to obtain all the input resources and rates needed to run all the converters.
            Dictionary<string, double> consumptionRates = new Dictionary<string, double>();
            WBIModuleGeneratorFX generator;
            int inputResourceCount = 0;
            ResourceRatio inputResource;
            for (int index = 0; index < generatorCount; index++)
            {
                generator = generators[index];
                inputResourceCount = generator.inputList.Count;
                for (int resourceIndex = 0; resourceIndex < inputResourceCount; resourceIndex++)
                {
                    inputResource = generator.inputList[resourceIndex];
                    if (inputResource.ResourceName == "ElectricCharge")
                        continue;

                    if (consumptionRates.ContainsKey(inputResource.ResourceName) == false)
                        consumptionRates.Add(inputResource.ResourceName, 0);

                    consumptionRates[inputResource.ResourceName] += inputResource.Ratio;
                }
            }

            // Now we need the total number of units of all the resources.
            // Each part has a tank with the consumed resource.
            // Have a dictionary with a list of parts that contain the resource.
            // As we build the dictionary, sum the total storage units.
            // NOTE: We need to ensure that all units are in the same volume. We assume 5-liter unit volumes.
            Dictionary<string, List<Part>> partResources = new Dictionary<string, List<Part>>();
            double totalUnitCapacity = 0;
            int partCount = ship.parts.Count;
            Part part;
            string[] consumptionRatesKeys = consumptionRates.Keys.ToArray();
            string resourceName;
            PartResourceDefinitionList definitions = PartResourceLibrary.Instance.resourceDefinitions;
            PartResourceDefinition resourceDef;
            double maxAmount;
            for (int index = 0; index < partCount; index++)
            {
                part = ship.parts[index];

                // Check the part for any consumed resources
                for (int keyIndex = 0; keyIndex < consumptionRatesKeys.Length; keyIndex++)
                {
                    resourceName = consumptionRatesKeys[keyIndex];

                    if (part.Resources.Contains(resourceName))
                    {
                        maxAmount = part.Resources[resourceName].maxAmount;

                        // Account for resource volume
                        resourceDef = definitions[resourceName];

                        // Update total unit capacity
                        totalUnitCapacity += maxAmount;

                        // Add part to the fuel tank list
                        if (partResources.ContainsKey(resourceName) == false)
                        {
                            partResources.Add(resourceName, new List<Part>());
                        }
                        partResources[resourceName].Add(part);
                    }
                }
            }

            // Now balance the amounts
            Dictionary<string, double> resourceAmounts = BalanceResources(consumptionRates, totalUnitCapacity);
            return resourceAmounts;
        }

        /// <summary>
        /// Computes the burn time for the ship's gravimetric generators given the ship's current resource amounts and the rate of consumption of the resources required to run the generators.
        /// 
        /// The idea:
        ///  - For each gravimetric generator, get their resource inputs and their resource input rates to obtain all the input resources and rates needed to run all the converters.
        ///  - For each input resource, determine how much of the resource the ship has.
        ///  - Divide resource amount by input ratio to get the burn time for that ratio.
        ///  - Determine the lowest burn time from the list of burn times.
        ///  - The lowest burn time is returned as the burn time for all the converters.
        /// </summary>
        /// <param name="ship">A ShipConstruct to compute the burn time for.</param>
        /// <param name="status">A string containing the result of the computation.</param>
        /// <returns>a double containing the burn time in seconds.</returns>
        public static double ComputeBurnTime(ShipConstruct ship, out string status)
        {
            status = "";

            // Find all the gravimetric generators that output Gravity Waves.
            List<WBIModuleGeneratorFX> generators = getGravimetricGenerators(ship);
            int generatorCount = generators.Count;
            if (generatorCount <= 0)
            {
                status = Localizer.Format("#LOC_BLUESHIFT_noGeneratorsFound");
                return 0;
            }

            // For each gravimetric generator, get their resource inputs and their resource input rates to obtain all the input resources and rates needed to run all the converters.
            Dictionary<string, double> consumptionRates = new Dictionary<string, double>();
            WBIModuleGeneratorFX generator;
            int inputResourceCount = 0;
            ResourceRatio inputResource;
            for (int index = 0; index < generatorCount; index++)
            {
                generator = generators[index];
                inputResourceCount = generator.inputList.Count;
                for (int resourceIndex = 0; resourceIndex < inputResourceCount; resourceIndex++)
                {
                    inputResource = generator.inputList[resourceIndex];
                    if (inputResource.ResourceName == "ElectricCharge")
                        continue;

                    if (consumptionRates.ContainsKey(inputResource.ResourceName) == false)
                        consumptionRates.Add(inputResource.ResourceName, 0);

                    consumptionRates[inputResource.ResourceName] += inputResource.Ratio;
                }
            }

            // For each input resource, determine how much of the resource the ship has.
            Dictionary<string, double> resourceAmounts = GetResourceAmounts(ship, consumptionRates.Keys.ToArray());

            // Divide resource amounts by input ratios to get the burn time for that ratio.
            Dictionary<string, double> burnTimes = new Dictionary<string, double>();
            string[] keys = consumptionRates.Keys.ToArray();
            double inputRate;
            double amount;
            for (int index = 0; index < keys.Length; index++)
            {
                amount = resourceAmounts[keys[index]];
                inputRate = consumptionRates[keys[index]];

                if (inputRate <= 0)
                {
                    status = Localizer.Format("#LOC_BLUESHIFT_badInputRate") + keys[index];
                    return 0;
                }

                burnTimes.Add(keys[index], amount / inputRate);
            }

            // Determine the lowest burn time from the list of burn times.
            keys = burnTimes.Keys.ToArray();
            double lowestBurnTime = double.MaxValue;
            for (int index = 0; index < keys.Length; index++)
            {
                if (burnTimes[keys[index]] <= lowestBurnTime)
                    lowestBurnTime = burnTimes[keys[index]];
            }

            return lowestBurnTime;
        }

        /// <summary>
        /// Computes the burn time for the ship's gravimetric generators given the ship's current resource amounts and the rate of consumption of the resources required to run the generators.
        /// 
        /// The idea:
        ///  - For each gravimetric generator, get their resource inputs and their resource input rates to obtain all the input resources and rates needed to run all the converters.
        ///  - For each input resource, determine how much of the resource the ship has.
        ///  - Divide resource amount by input ratio to get the burn time for that ratio.
        ///  - Determine the lowest burn time from the list of burn times.
        ///  - The lowest burn time is returned as the burn time for all the converters.
        /// </summary>
        /// <param name="ship">A ShipConstruct to compute the burn time for.</param>
        /// <param name="status">A string containing the result of the computation.</param>
        /// <returns>a double containing the burn time in seconds.</returns>
        public static double ComputeBurnTime(Vessel ship, out string status)
        {
            status = "";

            // Find all the gravimetric generators that output Gravity Waves.
            List<WBIModuleGeneratorFX> generators = getGravimetricGenerators(ship);
            int generatorCount = generators.Count;
            if (generatorCount <= 0)
            {
                status = Localizer.Format("#LOC_BLUESHIFT_noGeneratorsFound");

                return 0;
            }

            // For each gravimetric generator, get their resource inputs and their resource input rates to obtain all the input resources and rates needed to run all the converters.
            Dictionary<string, double> consumptionRates = new Dictionary<string, double>();
            WBIModuleGeneratorFX generator;
            int inputResourceCount = 0;
            ResourceRatio inputResource;
            for (int index = 0; index < generatorCount; index++)
            {
                generator = generators[index];
                inputResourceCount = generator.inputList.Count;
                for (int resourceIndex = 0; resourceIndex < inputResourceCount; resourceIndex++)
                {
                    inputResource = generator.inputList[resourceIndex];
                    if (inputResource.ResourceName == "ElectricCharge")
                        continue;

                    if (consumptionRates.ContainsKey(inputResource.ResourceName) == false)
                        consumptionRates.Add(inputResource.ResourceName, 0);

                    consumptionRates[inputResource.ResourceName] += inputResource.Ratio;
                }
            }

            // For each input resource, determine how much of the resource the ship has.
            Dictionary<string, double> resourceAmounts = GetResourceAmounts(ship, consumptionRates.Keys.ToArray());

            // Divide resource amounts by input ratios to get the burn time for that ratio.
            Dictionary<string, double> burnTimes = new Dictionary<string, double>();
            string[] keys = consumptionRates.Keys.ToArray();
            double inputRate;
            double amount;
            for (int index = 0; index < keys.Length; index++)
            {
                amount = resourceAmounts[keys[index]];
                inputRate = consumptionRates[keys[index]];

                if (inputRate <= 0)
                {
                    status = Localizer.Format("#LOC_BLUESHIFT_badInputRate") + keys[index];
                    return 0;
                }

                burnTimes.Add(keys[index], amount / inputRate);
            }

            // Determine the lowest burn time from the list of burn times.
            keys = burnTimes.Keys.ToArray();
            double lowestBurnTime = double.MaxValue;
            for (int index = 0; index < keys.Length; index++)
            {
                if (burnTimes[keys[index]] <= lowestBurnTime)
                    lowestBurnTime = burnTimes[keys[index]];
            }

            return lowestBurnTime;
        }

        internal static Dictionary<string, double> GetResourceAmounts(ShipConstruct ship, string[] resourceNames)
        {
            // For each input resource, determine how much of the resource the ship has.
            Dictionary<string, double> resourceAmounts = new Dictionary<string, double>();

            Part part;
            int partCount = ship.parts.Count;
            int resourceCount;
            PartResource partResource;
            for (int partIndex = 0; partIndex < partCount; partIndex++)
            {
                part = ship.parts[partIndex];

                resourceCount = part.Resources.Count;
                for (int resourceIndex = 0; resourceIndex < resourceCount; resourceIndex++)
                {
                    partResource = part.Resources[resourceIndex];
                    if (resourceNames.Contains(partResource.resourceName))
                    {
                        if (resourceAmounts.ContainsKey(partResource.resourceName) == false)
                            resourceAmounts.Add(partResource.resourceName, 0);

                        resourceAmounts[partResource.resourceName] += partResource.amount;
                    }
                }
            }

            return resourceAmounts;
        }

        internal static Dictionary<string, double> GetResourceAmounts(Vessel ship, string[] resourceNames)
        {
            // For each input resource, determine how much of the resource the ship has.
            Dictionary<string, double> resourceAmounts = new Dictionary<string, double>();

            Part part;
            int partCount = ship.parts.Count;
            int resourceCount;
            PartResource partResource;
            for (int partIndex = 0; partIndex < partCount; partIndex++)
            {
                part = ship.parts[partIndex];

                resourceCount = part.Resources.Count;
                for (int resourceIndex = 0; resourceIndex < resourceCount; resourceIndex++)
                {
                    partResource = part.Resources[resourceIndex];
                    if (resourceNames.Contains(partResource.resourceName))
                    {
                        if (resourceAmounts.ContainsKey(partResource.resourceName) == false)
                            resourceAmounts.Add(partResource.resourceName, 0);

                        resourceAmounts[partResource.resourceName] += partResource.amount;
                    }
                }
            }

            return resourceAmounts;
        }

        internal static List<WBIModuleGeneratorFX> getGravimetricGenerators(ShipConstruct ship)
        {
            List<WBIModuleGeneratorFX> generators = new List<WBIModuleGeneratorFX>();
            List<WBIModuleGeneratorFX> gravimetricGenerators = new List<WBIModuleGeneratorFX>();

            int partCount = ship.parts.Count;
            Part part;
            int generatorCount = 0;
            WBIModuleGeneratorFX generator;
            int resourceCount = 0;
            for (int index = 0; index < partCount; index++)
            {
                // Find generators in the part
                part = ship.parts[index];
                generators = part.FindModulesImplementing<WBIModuleGeneratorFX>();
                generatorCount = generators.Count;
                if (generatorCount <= 0)
                    continue;

                // Find generators that produce Gravity Waves
                for (int generatorIndex = 0; generatorIndex < generatorCount; generatorIndex++)
                {
                    generator = generators[generatorIndex];
                    resourceCount = generator.outputList.Count;
                    for (int resourceIndex = 0; resourceIndex < resourceCount; resourceIndex++)
                    {
                        if (generator.outputList[resourceIndex].ResourceName == "GravityWaves")
                        {
                            gravimetricGenerators.Add(generator);
                        }
                    }
                }
            }

            return gravimetricGenerators;
        }

        internal static List<WBIModuleGeneratorFX> getGravimetricGenerators(Vessel ship)
        {
            List<WBIModuleGeneratorFX> generators = new List<WBIModuleGeneratorFX>();
            List<WBIModuleGeneratorFX> gravimetricGenerators = new List<WBIModuleGeneratorFX>();

            int partCount = ship.parts.Count;
            Part part;
            int generatorCount = 0;
            WBIModuleGeneratorFX generator;
            int resourceCount = 0;
            for (int index = 0; index < partCount; index++)
            {
                // Find generators in the part
                part = ship.parts[index];
                generators = part.FindModulesImplementing<WBIModuleGeneratorFX>();
                generatorCount = generators.Count;
                if (generatorCount <= 0)
                    continue;

                // Find generators that produce Gravity Waves
                for (int generatorIndex = 0; generatorIndex < generatorCount; generatorIndex++)
                {
                    generator = generators[generatorIndex];
                    resourceCount = generator.outputList.Count;
                    for (int resourceIndex = 0; resourceIndex < resourceCount; resourceIndex++)
                    {
                        if (generator.outputList[resourceIndex].ResourceName == "GravityWaves")
                        {
                            gravimetricGenerators.Add(generator);
                        }
                    }
                }
            }

            return gravimetricGenerators;
        }

        internal static List<WBIWarpEngine> getWarpEngines(ShipConstruct ship)
        {
            List<WBIWarpEngine> warpEngines = new List<WBIWarpEngine>();
            int partCount = ship.parts.Count;
            Part part;
            WBIWarpEngine engine;

            for (int index = 0; index < partCount; index++)
            {
                part = ship.parts[index];
                engine = part.FindModuleImplementing<WBIWarpEngine>();
                if (engine != null)
                    warpEngines.Add(engine);
            }

            return warpEngines;
        }

        internal static List<WBIWarpEngine> getWarpEngines(Vessel ship)
        {
            List<WBIWarpEngine> warpEngines = new List<WBIWarpEngine>();
            int partCount = ship.parts.Count;
            Part part;
            WBIWarpEngine engine;

            for (int index = 0; index < partCount; index++)
            {
                part = ship.parts[index];
                engine = part.FindModuleImplementing<WBIWarpEngine>();
                if (engine != null)
                    warpEngines.Add(engine);
            }

            return warpEngines;
        }

        /// <summary>
        /// Computes the range in light-years based on burn time, warp factor, and pre-defined light-years.
        /// </summary>
        /// <param name="burnTime">Number of seconds of operation time.</param>
        /// <param name="warpFactor">Warp velocity in multiples of C.</param>
        /// <param name="distanceTraveledMeters">Out parameter that returns the distance traveled in meters.</param>
        /// <returns>The distance traveled in light-years.</returns>
        public static double CalculateRange(double burnTime, double warpFactor, out double distanceTraveledMeters)
        {
            double warpVelocityMetersPerSecond = warpFactor * BlueshiftScenario.shared.kLightSpeed;

            distanceTraveledMeters = warpVelocityMetersPerSecond * burnTime;

            double rangeLightYears = distanceTraveledMeters / BlueshiftScenario.shared.kLightYear;

            return rangeLightYears;
        }

        /// <summary>
        /// Formats the time
        /// </summary>
        /// <param name="timeSeconds">Amount of seconds to format</param>
        /// <returns>A string containing the time</returns>
        public static string FormatTime(double timeSeconds)
        {
            string timeString;
            double seconds = Math.Abs(timeSeconds);

            //Find homeworld
            double secondsPerYear = 0;
            double secondsPerDay = 0;
            int count = FlightGlobals.Bodies.Count;
            CelestialBody body = null;
            for (int index = 0; index < count; index++)
            {
                body = FlightGlobals.Bodies[index];
                if (body.isHomeWorld)
                {
                    secondsPerYear = body.orbit.period;
                    secondsPerDay = body.solarDayLength;
                    break;
                }
            }

            if (secondsPerDay <= 0)
                secondsPerDay = 21600;

            double days = Math.Floor(seconds / secondsPerDay);
            seconds -= days * secondsPerDay;

            double secondsPerHour = 3600;
            double hours = Math.Floor(seconds / secondsPerHour);
            seconds -= hours * secondsPerHour;

            double secondsPerMinute = 60;
            double minutes = Math.Floor(seconds / secondsPerMinute);
            seconds -= minutes * secondsPerMinute;

            timeString = string.Format("{0:f0}d {1:f0}h {2:f0}m {3:f2}s", days, hours, minutes, seconds);
            if (timeSeconds < 0f)
                timeString = "-" + timeString;

            return timeString;
        }

        /// <summary>
        /// Courtesy of MechJeb by Sarbian
        /// Licensed under GPLV3
        /// This Vessel extension computes the Bounds of the supplied vessel.
        /// This works both in the editor and in flight.
        /// EX: Bounds vesselBounds = FlightGlobals.ActiveVessel.GetBounds();
        /// </summary>
        /// <param name="vessel">A Vessel object to compute the bounds for.</param>
        /// <returns>A Bounds object containing the vessel's bounds.</returns>
        internal static Bounds GetBounds(this Vessel vessel)
        {
            Bounds vesselBounds = new Bounds();
            bool boundsInitialized = false;

            for (int i = 0; i < vessel.parts.Count; i++)
            {
                Part p = vessel.parts[i];
                Bounds partBounds = p.GetBounds();

                if (!boundsInitialized)
                {
                    vesselBounds = partBounds;
                    boundsInitialized = true;
                }
                else
                {
                    vesselBounds.Encapsulate(partBounds);
                }
            }

            return vesselBounds;
        }

        /// <summary>
        /// Courtesy of MechJeb by Sarbian
        /// Licensed under GPLV3
        /// This Part extension computes the Bounds of the supplied part.
        /// This works both in the editor and flight.
        /// EX: Bounds partBounds = FlightGlobals.ActiveVessel.rootPart.GetBounds();
        /// </summary>
        /// <param name="part">A Part object to compute the Bounds for.</param>
        /// <returns>A Bounds object containing the part's bounds.</returns>
        internal static Bounds GetBounds(this Part part)
        {
            Bounds partBounds = new Bounds();
            bool boundsInitialized = false;

            foreach (Transform t in part.FindModelComponents<Transform>())
            {
                // Check for inactive object. If inactive it's likely a part variant that's not active, so skip it.
                if (t.gameObject.activeSelf == false)
                    continue;

                // Check for disabled or non-existent colliders
                Collider collider = t.GetComponent<Collider>();
                if (collider == null || collider.enabled == false)
                    continue;

                // Check for disabled mesh renderers. Accounts for part variants.
                MeshRenderer renderer = t.GetComponent<MeshRenderer>();
                if (renderer != null && renderer.enabled == false)
                    continue;

                MeshFilter mf = t.GetComponent<MeshFilter>();
                if (mf == null)
                    continue;

                Mesh m = mf.mesh;
                if (m == null)
                    continue;

                Matrix4x4 matrix = part.vessel.transform.worldToLocalMatrix * t.localToWorldMatrix;

                foreach (Vector3 vertex in m.vertices)
                {
                    Vector3 worldSpaceVertex = matrix.MultiplyPoint3x4(vertex);

                    if (!boundsInitialized)
                    {
                        partBounds = new Bounds(worldSpaceVertex, Vector3.zero);
                        boundsInitialized = true;
                    }
                    else
                    {
                        partBounds.Encapsulate(worldSpaceVertex);
                    }
                }
            }

            return partBounds;
        }
    }
}
Blueshift: Kerbal FTL

---INSTALLATION---

Simply copy all the files into your GameData folder. When done, it should look like:

GameData
	WildBlueIndustries
		Blueshift

New Parts

- S1 Multi-Segment Warp Coil: This part combines multiple S1 warp coil segments into one part variant to reduce warp coil part spamming. It offers 2, 4, 6, 8, and 10 segment variants, with appropriate increases in mass, cost, and performance. There are also "Midsection" versions to combine with other coils for truly long warp nacelles.


Bug Fixes

- Fixed issue with WBIModuleGeneratorFX that prevented it from setting Action Groups.

- Adjusted part costs for the Bussard Collectors and Plasma Vents.

- Fixed negative Funds cost of the Mini Jumpgate Platform.

- Fixed issue where space anomalies all had the same vessel name.

- Fixed issue where generator resource consumption dropped to zero when flying in interstellar space.

- Fixed issue with wonky resource generator display in the VAB/SPH.

Changes

- SPACE_ANOMALY entries can now specify "everyPlanet" as a spawnMode. Fair warning: This will spam a lot of anomalies, so use sparringly.

- Added Debug Mode menu item to the Blueshift Settings menu. This supersedes the "debugMode" field found in settings.cfg and various part module files.

- Added Sphere of Influence support for JNSQ's planet Nara.

- WBIWarpCoil: Now supports the ability to multiply the warp capacity (and the corresponding resource requirements) based on the selected part variant. This is done by adding an EXTRA_INFO node to the appropriate variant. Example:

	VARIANT
	{
		name = Short
		displayName =  #autoLOC_8005067 //#autoLOC_8005067 = Short
		primaryColor = #3a562a
		secondaryColor = #9e7100
		GAMEOBJECTS
		{
			yadaYada = true
		}

		EXTRA_INFO
		{
			// Capacity Multiplier specifies how much to multiply the base capacity
			capacityMultiplier = 1
		}
	}

- WBIAnimatedTexture: Texture & emission animation now supports multiple transformed with the same name specified by the textureTransformName field. Additionally, you can now specify a TEXTURE_TRANSFORMS config node with one or more textureTransformName fields that will also be animated. Example:
	MODULE
	{
		name = WBIAnimatedTexture

		// ID of the module so we can distinguish between several animated textures in one part.
		// This one is controlled by the warp coil.
		moduleID = WarpCoil

		// The 3D model can have multiple transforms with this name.
		textureTransformName = plasmaCoil

		// These will also be animated
		TEXTURE_TRANSFORMS
		{
			textureTransformName = plasmaCoil2
			textureTransformName = plasmaCoil3
			textureTransformName = plasmaCoil4
			textureTransformName = plasmaCoil5
		}

		animatedEmissiveTexture = WildBlueIndustries/Blueshift/Parts/Engine/WarpTech/WarpPlasma
		minFramesPerSecond = 15
		maxFramesPerSecond = 50
		fadesAtMinThrottle = true
		emissiveFadeTime = 0.5
	}

---LICENSE---
Art Assets, including .mu, .png, and .dds files are copyright 2021-2022 by Michael Billard, All Rights Reserved.

Wild Blue Industries is trademarked by Michael Billard. All rights reserved.
Note that Wild Blue Industries is a ficticious entity 
created for entertainment purposes. It is in no way meant to represent a real entity.
Any similarity to a real entity is purely coincidental.

Source code copyright 2021-2022 by Michael Billard (Angel-125)

    This source code is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <http://www.gnu.org/licenses/>.
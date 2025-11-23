Blueshift: Kerbal FTL

---INSTALLATION---

Simply copy all the files into your GameData folder. When done, it should look like:

GameData
	WildBlueIndustries
		Blueshift
	FireflyAPI

Changes

SPACE_ANOMALY:
- Added "fixedBody = any" option, which will allow space anomalies with spawnMode = fixedOrbit to appear around a random celestial body (yes, I know, it's weird...)
- Added spawnMode = homeworld option, which will allow space anomalies to spawn in orbit of the homeworld. SMA will automatically be set to the edge of the homeworld's Sphere of Influence. The rest of the parameters are taken from the Fixed Orbit parameters.
- Added forceAddToNetwork flag. If set to true, the jumpgate anomaly will be added to the jumpgate network even if isKnown = false. This allows gates to be discovered and pre-populated with destinations.

Patches

- Removed ModuleManager patch for defunct Galaxies Unlimited and repurposed it.

Extras

- Added UnstableWormhole.txt - This config file provides an example of defining a wormhole that is fixed at one end (Kerbin), and unstable at the other end (it will jump around to various other celestial bodies).

Bug Fixes

- Fixes issue where Waterfall Effects weren't being shown for the warp rings or warp sustainers.
- Fixes issue where vessels are teleported underneath statics when the jump gate is placed on a static.
- Fixes issue where all the lights on a vessel would be activated upon activating a jump gate.
- Fixes NullRefException in WBIModuleGeneratorFX.
- Fixed an issue in space anomalies where the anomaly wouldn't spawn when protoVesselFilePath was null.

---LICENSE---

Sounds effects courtesy of Pond5 and may NOT be redistributed.

Art Assets, including .mu, .png, and .dds files are copyright 2021-2025 by Michael Billard, All Rights Reserved.

Wild Blue Industries is trademarked by Michael Billard. All rights reserved.
Note that Wild Blue Industries is a fictitious entity 
created for entertainment purposes. It is in no way meant to represent a real entity.
Any similarity to a real entity is purely coincidental.

Source code copyright 2021-2025 by Michael Billard (Angel-125)

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
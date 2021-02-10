Blueshift: Kerbal FTL

---INSTALLATION---

Simply copy all the files into your GameData folder. When done, it should look like:

GameData
	WildBlueIndustries
		Blueshift

Changes

Anomalies
- Reduced time between anomaly checks.
- Improved chances for a space anomaly to spawn.

Spheres of Influence
- Updated how stellar spheres of influences are calculated to improve interstellar space detection.
- Changed homeSOIMultiplier in Settings.cfg soiMultiplier. It is now used for all star systems.
- Added new soiNoPlanetsMultiplier to artificially create an SOI for star systems without planets.

Warp Tech
- New navigation assistance- warp engines have some additional fields in their Part Action Window:
  Spatial Situation: Planetary/Interplanetary/Interstellar/Unknown
  Course: <name of the selected target, if any>
  Distance: <Ly, Gm, Mm Km>

CKAN
- Added Blueshift to CKAN (pending PR approval).

Config Nodes
New format for LAST_PLANET
LAST_PLANET
{
	// Name of the last planet.
	// This is the name of the celestial body, NOT the display name!
	name = Cernunnos

	// Name of the star that the planet orbits.
	// This is the name of the celestial body, NOT the display name!
	starName = Grannus
}

---LICENSE---
Art Assets, including .mu, .png, and .dds files are copyright 2021 by Michael Billard, All Rights Reserved.

Wild Blue Industries is trademarked by Michael Billard. All rights reserved.
Note that Wild Blue Industries is a ficticious entity 
created for entertainment purposes. It is in no way meant to represent a real entity.
Any similarity to a real entity is purely coincidental.

Source code copyright 2021 by Michael Billard (Angel-125)

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
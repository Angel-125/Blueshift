Blueshift: Kerbal FTL

---INSTALLATION---

Simply copy all the files into your GameData folder. When done, it should look like:

GameData
	WildBlueIndustries
		Blueshift
	FireflyAPI

---CHANGES---

- Improved spatial-location detection for warp vessels. Star-system boundaries are now calculated from the maximum reach of local planetary branches using apoapsis and sphere of influence.
- Added support for nested planetary systems and barycenters while excluding hierarchy branches that contain other stars.
- Fixed vessels in the outer Kerbol system and modded planetary systems being incorrectly classified as in interstellar space.
- Spatial-boundary checks now compare center-relative orbital distances consistently.
- Removed the bundled LAST_PLANET overrides. Blueshift now detects outer planetary boundaries automatically; an optional override example remains in settings.cfg for unusual system hierarchies.

--END CHANGES--

---LICENSE---

Sounds effects courtesy of Pond5 and may NOT be redistributed.

Art Assets, including .mu, .png, and .dds files are copyright 2021-2026 by Michael Billard, All Rights Reserved.

Wild Blue Industries is trademarked by Michael Billard. All rights reserved.
Note that Wild Blue Industries is a fictitious entity 
created for entertainment purposes. It is in no way meant to represent a real entity.
Any similarity to a real entity is purely coincidental.

Source code copyright 2021-2026 by Michael Billard (Angel-125)

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

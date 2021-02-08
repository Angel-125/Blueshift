Blueshift: Kerbal FTL

---INSTALLATION---

Simply copy all the files into your GameData folder. When done, it should look like:

GameData
	WildBlueIndustries
		Blueshift

New Features

- Space Anomalies now exist! Like asteroids and comets, space anomalies can randomly spawn and de-spawn in solar orbit. If visited, then they'll stick around- and might help out your quest for knowledge... You can disable Space Anomalies in the Settings menu.
- Added new WBITechUnlock part module. It is designed for anomalies and has the ability to randomly unlock a tech tree node in Career or Science Sandbox mode.
- Added celestialBlacklist value to the Settings.cfg file. This value lets you blacklist celestial bodies from being counted as stars and planets. This is to help ignore things like barycenters.
- Added LAST_PLANET node that helps Blueshift determine what is the last planet for the specified star. This is to help in situations where Blueshift can't figure out what is the last planet for a given star system. You can see an example in Settings.cfg.
- Added Graviolium support for SpaceDust and Far Future Technologies. These will need testing...

Changes

- Fixed issue where the warp multiplier couldn't figure out where the edge of the SOI is for the home system.
- Fixed NRE in WBIWarpEngine that occurs during starup.

---LICENSE---
Art Assets, including .mu, .png, and .dds files are copyright 2021 by Michael Billard, All Rights Reserved.

Wild Blue Industries is trademarked by Michael Billard. All rights reserved.
Note that Wild Blue Industries is a ficticious entity 
created for entertainment purposes. It is in no way meant to represent a real entity.
Any similarity to a real entity is purely coincidental.

Source code copyright 2020 by Michael Billard (Angel-125)

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
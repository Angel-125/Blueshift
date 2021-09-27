Blueshift: Kerbal FTL

---INSTALLATION---

Simply copy all the files into your GameData folder. When done, it should look like:

GameData
	WildBlueIndustries
		Blueshift

Changes

Warp Engines

- The planetary SOI warp curve won't be applied if the vessel's max warp speed is below 0.001c. This will help prevent underpowered vessels from crawling at a snail's pace
when in planetary space.
- For lazy players: If a warp engine has power converters built in, then when you start the engine, the converters will also be automatically started as well.
- The vessel course (a.k.a. selected target) and distance to target displays will be hidden when there is no target selected.
- The Max Warp Speed and FTL Check displays will be hidden when the warp engine is shut down.
- The Auto-circularize orbit button will be hidden when the warp engine is shut down.
- The maximum warp speed display will be updated even when the throttle is at 0.
- The FTL Check display now handles the case where the throttle is at 0.
- Fixed issue where the warp engine flames out if the throttle is at 0.
- Fixed "double-dipping" issue where warp capacity was affected by throttle, and speed was affected by throttle as well. Now, just warp speed is affected by the throttle.

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
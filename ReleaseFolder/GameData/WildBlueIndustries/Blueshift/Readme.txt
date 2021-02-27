Blueshift: Kerbal FTL

---INSTALLATION---

Simply copy all the files into your GameData folder. When done, it should look like:

GameData
	WildBlueIndustries
		Blueshift

New Parts

- Astria Porta Arbitrium: This is the control segment of an Astria Porta, a jumpgate that can be built.
- Astria Porta Auxilium: This is a ring segment for an Astria Porta. You'll need 9 of these along with the control segment to complete a gate. The stock Clamp-O-Tron Sr is ideally suited for this part and the Astria Porta Arbitrium.
- Astria Porta Accommodare: This is a small adapter part for putting ring segments in cargo bays.

Changes

- Added an alignment helper to the stock Clamp-O-Tron Sr. docking port to help with docking Astria Porta parts together.
- Added new Jump Tech research node. The Astria Porta parts are located in the new node.
- Added new Wormhole Space Anomaly. You can see it in use if you have Galaxies Unbound installed, but it can support other mods as well if config files are written for them.
- fixedOrbit now accounts for planetary radius and, if it has one, atmospheric depth.
- Removed pairedGateAddress and gateAddress from SPACE_ANOMALY. These fields proved unnecessary. To make a paired jumpgate, simply make sure that both gates use the same network ID and ensure that the network ID is unique.
- Moved the GR series of graviolium tanks to the Advanced Science Tech node, where you also get the stock Drill-O-Matic' Mining Excavator.
- Fixed issue where vessels are teleported to their destination gate during a jumpgate's startup sequence.
- Fixed issue where anomalies weren't being removed after their expiration date.
- Fixed missing lift on mk2 parts.
- Fixed issue where the warp engine wasn't respecting the vessel's control point direction.
- Fixed issue where several part modules were attempting to send controller value updates to Waterfall controllers that didn't exist.

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
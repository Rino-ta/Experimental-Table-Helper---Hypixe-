# Hypixel Experimental-Table Helper
░ WHAT IS THIS?

A lightweight automation mod that integrates directly with SkyHanni's Experimental Table detection system. Once activated, it reads the color highlighted by SkyHanni, moves your cursor to that exact position, and clicks - looping indefinitely until you tell it to stop.
No guessing. No manual input. Just open the table, press X, and watch it work.

░ HOW IT WORKS
1. Open Experimental Table in Hypixel SkyBlock
2. Press [ X ] to activate the solver
3. SkyHanni detects & highlights the target color
4. Mod reads the highlighted color automatically
5. Cursor moves → clicks → loops indefinitely
6. Press [ X ] again to stop
The solver runs on a continuous loop — every tick it re-reads SkyHanni's output, recalculates the target, and fires the click. This means it adapts in real time if the board changes state.

░ REQUIREMENTS

| Dependency | Version | Notes |
| :--- | :--- | :--- |
| Minecraft | `any version` | Recommended for Hypixel |
| Fabric | `latest` | Mod loader |
| SkyHanni | `latest` | **Required** — color detection source |

⚠️ SkyHanni must be installed and the Experimental Table feature must be enabled in its config for this mod to function.


░ INSTALLATION
1. Download the latest .jar from Releases
2. Drop it into your /mods folder
3. Make sure SkyHanni is also in /mods
4. Launch Minecraft → join Hypixel
5. Head to your Experimental Table

░ USAGE

<p align="center">
  <code>[ Open Experimental Table ]</code><br>
  ↓<br>
  <code>[ Press X ]</code><br>
  ↓<br>
  <code>[ Mod activates - reads SkyHanni color → clicks → loops ]</code><br>
  ↓<br>
  <code>[ Press X again to stop ]</code>
</p>

░ NOTES


The mod only activates inside the Experimental Table GUI - it won't interfere with anything else
If SkyHanni doesn't highlight a color (e.g. puzzle already solved), the loop idles safely
Works best with a stable framerate - lower FPS may affect click timing


░ DISCLAIMER

This mod is intended for personal use. Use of automation tools may violate Hypixel's Terms of Service. The author holds no responsibility for any action taken against your account. Use at your own risk.

<div align="center">
made with obsession, not affection
</div>
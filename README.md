# Camel Cavalry

A Mount & Blade II: Bannerlord mod that adds camel cavalry branches to the Aserai and Khuzait troop trees, next to their vanilla cavalry. Nothing in the vanilla troop trees is replaced.

## Why this mod exists

Camels and war camels are in the base game: Aserai horse farms breed them and markets sell them. Yet only a few minor-faction troops ride them, and neither the Aserai nor the Khuzait troop tree has any camel cavalry.

Camel Cavalry adds that cavalry as extra upgrade choices. Each camel troop is balanced against the vanilla troop it stands next to, uses vanilla equipment for its rider, and needs a real camel or war camel to train.

## Troop trees

```
Aserai Mamluke Regular
├── Aserai Mamluke Cavalry      vanilla, unchanged
└── Aserai Camel Rider          new, tier 4, needs a camel
    ├── Aserai Camel Lancer     new, tier 5 melee, needs a war camel
    └── Aserai Camel Archer     new, tier 5 horse archer, needs a war camel

Khuzait Qanqli (noble line)
├── Khuzait Torguud             vanilla, unchanged
└── Khuzait Camel Torguud       new, tier 4 horse archer, needs a camel
    └── Khuzait Camel Kheshig   new, tier 5 horse archer, needs a war camel
        └── Khuzait Heavy Camel Archer   new, tier 6 horse archer, no item needed
```

- Upgrading to Camel Rider or Camel Torguud uses up one ordinary camel (`camel`).
- Upgrading to Camel Lancer, Camel Archer or Camel Kheshig uses up one war camel (`war_camel`).
- Upgrading to Heavy Camel Archer needs no item, like the vanilla Kheshig to Khan's Guard upgrade, because the Camel Kheshig already rides a war camel.
- Other horses, war horses and pack camels do not count for these upgrades. Vanilla upgrades keep their usual rules.
- The Aserai camel troops are balanced against the vanilla Mamluke and Faris troops. Like the vanilla Faris, the Camel Rider and Camel Lancer carry javelins; like the vanilla Heavy Mamluke, the Camel Archer carries a sword and a large adarga shield with one quiver.
- The Khuzait Camel Torguud and Camel Kheshig have the skills and gear of the vanilla Torguud and Kheshig (bow, arrows, glaive and sword), on camels with camel armor. The Heavy Camel Archer has the skills and gear of the vanilla Khan's Guard (bow, two quivers and glaive) on a war camel in lamellar camel armor, the heaviest of the four.
- AI parties upgrade into the camel branches the same way they upgrade into any vanilla branch; like vanilla, they do not need mounts in their inventory. Each lord tends to favour one branch, so the split is not even.

## Camels and camel armor in the world

Khuzait horse farms (`steppe_horse_ranch`) also breed camels: `camel` 0.3 and `war_camel` 0.08 a day, the same as the vanilla Aserai horse farm, and `pack_camel` 0.1, a third of the Aserai rate. Their horse, sumpter horse and mule production is unchanged, and horses stay their main product. The camels are sold in those villages and carried to their trade towns, as other village goods are. Aserai horse farms are left as in vanilla. The change is reapplied on every new or loaded campaign and is not stored in the save.

The four camel armors come from [Camel Armor BM168][bm168], which this mod requires. At load time Camel Cavalry gives them a horse harness market category, a price (3,200 to 4,800) and the merchandise flag, and gives its camel lance a price and the merchandise flag, so markets handle them like other harness and weapons. It also removes a duplicate empty `<Flags />` element from the four armors. Their definitions are not copied or replaced.

The camel armors are Aserai items, and vanilla town workshops only make harness of their own town's culture. Khuzait town workshops are also allowed to make the four camel armors, alongside their own Khuzait harness; towns of every other culture are unchanged.

## How the exact camel rule works

In the troop XML the game can only ask for an item category: a camel counts as a horse and a war camel counts as a war horse. The DLL narrows that to the exact item for the player's party screen and the upgrade pop-up: the available count, the upgrade tooltip, the requirement icon tooltip, the upgrade check and the mount that is taken from the inventory. Camels with any item modifier count. Locked inventory items are used last, as in vanilla, and cancelling or resetting the party screen gives back the exact camels used.

The patches are applied all together or not at all. If a game update changes the code they adjust, Camel Cavalry removes them, writes the reason to the game log and shows a red message when a campaign loads, instead of the game crashing. The troops still work then, but they accept any horse or war horse.

Every log line starts with `[CamelCavalry]`. When a campaign loads, the log lists the Mamluke Regular's and Qanqli's upgrades, the camel troops with their requirements, the category, price and flags of the camel armor items, what the Khuzait and Aserai horse farms produce, and whether Khuzait workshops can make camel armor. At load and once a week it counts the camel troops in AI parties and garrisons.

## Requirements and versions

| Dependency | Tested version | Expected to work with |
|---|---|---|
| Mount & Blade II: Bannerlord | 1.4.8 | 1.4.x |
| War Sails (NavalDLC), optional | 1.2.8 | 1.2.x |
| [Harmony][harmony] | 2.4.2 | 2.x |
| [Camel Armor BM168][bm168] | 1.0.1 | 1.0.x |

[Camel Armor BM168][bm168] supplies the four camel armor meshes, textures, cloth data and item definitions, and the camel lance. Install it separately from its own page; none of its files are included in this mod.

[Harmony][harmony] is needed for the exact camel and war camel upgrade rules, camel breeding on Khuzait horse farms, camel armor in Khuzait town workshops and the removal mode's access to rosters the game keeps out of reach.

[BLSE][blse] is optional. As [Harmony][harmony]'s page notes, it is not required on Steam, GOG or Epic. This mod does not use it either. BLSE improves mod load-order sorting and crash reporting, and the Xbox / PC Game Pass version of the game needs it.

## Compatibility

- **[Castle Market Towns][cmt]:** compatible. Castles whose villages are horse farms also stock the camels those farms breed.
- **Not compatible** with [Camel Cataphracts BM168][cataphracts] (`Camel_Cataphracts_BM168`), which contains an old override of the vanilla Mamluke Regular. Camel Cavalry declares it incompatible, so the launcher warns about the conflict. The armor-only [Camel Armor BM168][bm168] is the one this mod needs.
- **Troop tree overhauls** that replace the Aserai Mamluke Regular or the Khuzait Qanqli: if such a mod loads after Camel Cavalry, the camel branch disappears and the game log warns about it. If it loads before, the branch is added to its version of the troop. Total conversions without the Aserai or Khuzait cultures leave the branches nothing to attach to.
- **Possible conflicts:** mods that overhaul the party screen or troop upgrades may bypass the exact camel rule, so any horse counts. Mods that rebuild village production or workshop production may remove the camels from Khuzait horse farms or the camel armor from Khuzait towns. Mods that edit or replace the [Camel Armor BM168][bm168] items may undo their market category and prices. The chance is low, and such conflicts disable a feature rather than crash the game. Please report them on [GitHub][issues].

## Installation

1. Install [Harmony][harmony] and [Camel Armor BM168][bm168]. Keep Camel Armor BM168 in its own `Camel_Armors_BM168` module folder. [BLSE][blse] is optional on Steam, GOG and Epic.
2. Install Camel Cavalry: extract the `CamelCavalry` folder into `Mount & Blade II Bannerlord\Modules`.
3. Start the game launcher, or the [BLSE][blse] launcher if you use it. Enable Camel Cavalry and place it after [Harmony][harmony] and [Camel Armor BM168][bm168].

The mod can be added to an existing campaign.

## Removing the mod from a campaign

Camel Cavalry adds new troops to a campaign and uses items supplied by [Camel Armor BM168][bm168]. Do not simply disable either module in a save that already contains those troops or items: the game would meet object IDs it can no longer find. Use the removal mode instead:

1. Create an empty file named `prepare_removal.txt` in `Modules\CamelCavalry`.
2. Start the game with Camel Cavalry still enabled and load your save. A message reports how many troops and items were converted:
   - Camel Rider becomes Mamluke Cavalry, Camel Lancer and Camel Archer become Heavy Mamluke, Camel Torguud becomes Torguud, Camel Kheshig becomes Kheshig and Heavy Camel Archer becomes Khan's Guard. Counts, wounded troops and experience are kept. This covers parties, garrisons, prisoner rosters, settlement dungeons, notables' volunteers, troops a companion took to solve an issue, troops back from an issue waiting to rejoin you, troops guarding your alleys and siege break-in casualties.
   - Camel armors become camel saddles, the camel lance becomes a vanilla Aserai lance, and the four Camel Cataphract mount sizes from [Camel Armor BM168][bm168] become vanilla war camels. This covers party, settlement and stash inventories, workshop warehouses and hero equipment.
3. If the message says some parties are in a battle or siege, let a day pass; the cleanup repeats every day and right before every save. If the message is red and names places it could not reach (a game update can rename them), empty those by hand: take the items out of your workshop warehouses, finish or cancel companion-led issue solutions, collect returning companions and withdraw alley guards.
4. Save to a new slot and exit. Because the cleanup converts every [Camel Armor BM168][bm168] item to a vanilla equivalent, the save is ready for disabling both modules together.
5. Disable or uninstall Camel Cavalry and [Camel Armor BM168][bm168].

While `prepare_removal.txt` exists the cleanup keeps running, so delete the file if you decide to keep the mod. Rosters added by other mods are outside its reach.

## Building from source

The repository contains only source code and XML. The TaleWorlds and [Harmony][harmony] DLLs are not included; the build references them from your game installation. No [Camel Armor BM168][bm168] file is part of the repository.

Requirements:

- Windows with .NET Framework 4.7.2 or later.
- Visual Studio 2019 or later, or the Visual Studio Build Tools. The .NET Framework's own MSBuild also works.
- Bannerlord with [Harmony][harmony] installed.

The code is limited to C# 5 so it builds with the .NET Framework compiler too.

### Point the build at your game

Use one of the following:

- Copy `CamelCavalry.user.props.example` to `CamelCavalry.user.props` and set `BannerlordDir`. Git ignores this file.
- Set the `BANNERLORD_DIR` environment variable.
- Pass `/p:BannerlordDir="..."` to MSBuild.

If none is set, a project inside `<game>\Modules\CamelCavalry` uses that game; otherwise the default Steam folder `C:\Program Files (x86)\Steam\steamapps\common\Mount & Blade II Bannerlord` is used.

### Build

```bat
build.cmd
```

The DLL is written to `bin\Win64_Shipping_Client\CamelCavalry.dll`, so the repository folder has the same layout as the installed module.

To also copy `SubModule.xml`, `ModuleData` and the DLL into the game's `Modules\CamelCavalry` folder after each build, set `DeployToGame` to `true` in your `CamelCavalry.user.props`, or run:

```bat
build.cmd /p:DeployToGame=true
```

Close the game first, otherwise the DLL is locked.

## Repository layout

```
CamelCavalry.csproj               Project file
CamelCavalry.user.props.example   Template for local build settings
build.cmd                         Finds MSBuild and builds the project
SubModule.xml                     Module definition loaded by the game
ModuleData/                       Camel troops, and XSLT patches for the vanilla troops and the Camel Armor BM168 items
src/                              C# source
```

## License and credit

Copyright (C) 2026 AtanuDebbarma.

Camel Cavalry is free software under the [GNU General Public License v3.0](LICENSE), with an additional attribution term described in [NOTICE](NOTICE):

- **Using or modifying this code:** if you publish a mod or other work that uses or is based on this code, you must credit **Camel Cavalry by AtanuDebbarma** with a link to [this repository][repo], in your source code and in your mod's documentation. Your work must also be released under the GPL v3.
- **Using it as inspiration:** if this project inspired your work without you copying its code, a credit and link are kindly requested.

The license covers Camel Cavalry's own code and XML only. Credits:

- **[BestMods168][bestmods168]**, [Camel Armor BM168][bm168]: the camel armor meshes, textures, cloth data and items this mod requires. Its author allows it to be used as a dependency; its files are not redistributed here and must be downloaded from its page.
- **tannesxam**, [Camel Riders][camelriders]: troop ideas.
- **TaleWorlds Entertainment:** the vanilla camels, war camels and troop equipment.

Mount & Blade II: Bannerlord is a trademark of TaleWorlds Entertainment. This project is not affiliated with or endorsed by TaleWorlds.

[bm168]: https://www.nexusmods.com/mountandblade2bannerlord/mods/4852
[bestmods168]: https://www.nexusmods.com/profile/bestmods168?gameId=3174
[cataphracts]: https://www.nexusmods.com/mountandblade2bannerlord/mods/4853
[cmt]: https://www.nexusmods.com/mountandblade2bannerlord/mods/14182
[camelriders]: https://www.nexusmods.com/mountandblade2bannerlord/mods/2802
[harmony]: https://www.nexusmods.com/mountandblade2bannerlord/mods/2006?tab=description
[blse]: https://www.nexusmods.com/mountandblade2bannerlord/mods/1?tab=description
[repo]: https://github.com/AtanuDebbarma/CamelCavalry
[issues]: https://github.com/AtanuDebbarma/CamelCavalry/issues

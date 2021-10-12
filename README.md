![Header](https://i.imgur.com/Adr9uVh.jpg)
### [Nexus Link](https://www.nexusmods.com/skyrimspecialedition/mods/56881)
# True Unleveled Skyrim
A collection of patchers meant to unlevel the game and provide a Requiem and/or Morrowloot-style experience while being highly compatible and taking into account every little piece of your modlist, be that NPC overhauls, enemy overhauls, additional enemy or creature mods, new armors and weapons or perk overhauls.

## What it Does
First, it changes Encounter Zones and sets their levels to a static number picked from a range or to a user-defined range, makes them always adhere to their minimum and maximum defined levels without matching the player and changes the spawn level multipliers for different enemy difficulties to provide a more consistent challenge to what the Encounter Zone is supposed to offer.

Second, it changes NPCs to never scale in level depending on the player (unless they are followers, by default) and to have static levels assigned to them. Based on their assigned levels and their class (one handed warrior, mage, etc) they also get their skill levels re-calculated (which in vanilla are usually laughably low) resulting in high level enemies having their main skills maxed out.
NPCs also get perks distributed by default, also based on their class, with their main skills being prioritized the most. They also get their old perks removed, but only the perks that originate from the vanilla Skyrim data files, leaving any mod-added perks safely in place.
A high level NPC who is a one handed warrior will have a high one handed skill and plenty of perks in one handed, proving to be more of a challenge, but never getting perks that they are not eligible for with their current skill level. Extra damage perks which just straight up boosted the damage of NPCs regardless of skill level are also disabled by default.

Third, leveled item lists get unleveled and many of them modified to not contain items that should be exceedingly rare like glass, ebony and daedric equipment. The remaining items also get set to occur from level 1, meaning you can find anything on enemies or in shops from the start, ranging from iron to elven or orcish equipment.
Artifact lists also get unleveled by default, rendering mods that unlevel artifacts useless since you will only ever get the strongest variant of Chillrend or Dragonbane to begin with.

Fourth, there is an option to "Morrowlootify" equipment. This means that all equipment in the current load order will receive stats they would have in Morrowloot, except without the compatibility and balancing nightmare it provides.
The way it works is that the patcher takes the base stats of all rare equipment like Glass, Ebony, Daedric, etc from the supplied plugin list under the Options tab in Synthesis and then goes through every item. For every item the stats are checked and a ratio is calculated based on the difference between the vanilla armor value and the modded armor value.
Say someone might have an armor variant mod that adds a stronger but heavier set of ebony armor that has 10% higher armor rating and weight, the patcher applies the Morrowloot stats to the armor set and increases those stats by 10%, keeping the relative balance but within the Morrowloot vision.
The patcher doesn't fix badly balanced equipment, it merely brings them up to their Morrowloot counterparts while keeping their relative (good or bad) balance with the vanilla values.

## The Result
With the default settings (and the recommended mods further down in the description) the experience is synonymous with a mod like Requiem or Morrowloot, giving a mostly unmoving world that doesn't revolve around the player, with all kinds of powerful items, enemies and riches being available from the very start of the game for the most daring players.
The experience of course is highly customizable, possibly giving something more in-line with vanilla but still a lot more varied or something entirely different. It can be made easier, harder or somewhere in-between.

## Recommended Mods
- **[Encounter Zones Unlocked SE](https://www.nexusmods.com/skyrimspecialedition/mods/19608):** Prevents the game from freezing the level of encounter zones when you first visit them. Not needed with the default settings, but if you have level ranges instead of static levels it's practically essential.
- **[MEZF - Missing Encounter Zones Fixed](https://www.nexusmods.com/skyrimspecialedition/mods/23609?):** Recommend using the full exteriors version. Adds a bunch of unused and/or missing encounter zones that TUS can edit.
- **[NPC Stat Rescaler](https://github.com/theSkyseS/NPCStatRescaler):** Another patcher available through Synthesis, goes great with TUS by helping high level enemies to not be damage sponges. Also deeply customizable.
- **[Morrowloot Miscellania](https://www.nexusmods.com/skyrimspecialedition/mods/27094):** A mod on the Nexus that splits up Morrowloot into some different, smaller modules. I highly recommend using the Daedric Crafting Requirements, Dremora Bound Weapons, Hybrid Loot and Item Distribution modules to have that nice bit of handcrafted feel to your game with hand-placed rare equipment while the others provide some flair and balance.
- **[Enemy overhaul/addition mods](https://www.nexusmods.com/skyrimspecialedition/mods/2969):** Very highly recommended to use mods like these otherwise very high level areas won't have as much variety between each other. Can be something simple like High Level Enemies or something more involved like *Revenge of the Enemies 2016 Modified, Revenge of the Enemies Rescaled Immunities and OBIS* that I personally use. Skyrim Immersive Creatures and similar mods also work well for this.
- **[A good perk overhaul](https://www.nexusmods.com/skyrimspecialedition/mods/1137):** It can be anything you like as long as perks NPCs shouldn't use are marked with NoASIS in their editor ID. Enai's and Simon's perk mods are all compatible, others you'll have to see for yourself. I like Ordinator myself.
- **[Spell mods](https://www.nexusmods.com/skyrimspecialedition/mods/23488):** Whatever you like is best, but they are recommended here for the next suggestion.
- **[Spell Perk Item Distributor configurations](https://www.nexusmods.com/skyrimspecialedition/mods/36869):** Configs for SPID that distribute spells and potions (especially if based on NPC skill level and class) work beautifully here and along with the distributed perks from your favorite perk mod can make for some intense fights with high level NPCs while also making them clearly distinct from low level ones. 
- **[A good combat mod](https://www.nexusmods.com/skyrimspecialedition/mods/53869):** Again, it can be any combat mod you like that makes enemies smarter in combat and pose more of a threat. I use Valravn personally.
- **[Mortal Enemies - Rival Remix](https://github.com/Synthesis-Collective/MortalEnemies-Patcher):** Another Synthesis patcher to remove the excessive tracking NPCs have in combat. Make sure to use the Rival Remix option to not have it feel too stiff.

These are what I would consider a good foundation to use TUS with (at least with the default settings, otherwise it may vary). The stat rescales and small morrowloot plugins I would consider essential, personally.

# How to Use
In this section I'll explain what the patcher settings do first and then how to modify and set up the config files for your own load order if the default ones don't sit well with it.

## The Settings
### Unleveling
  ### Zones
- **Unlevel Zones:** Encounter zones get unleveled if it's checked.
- **Use Morrowloot Zone Balance:** There are 2 sets of config files for zones, the default ones that were included with TUS
and another one that was based loosely off of Morrowloot. This just changes which set of files is used.
- **Static Zone Levels:** Normally in the config file a minimum and maximum level and a level range (variance between min and max) can be set for areas. If this option is turned on, the level ranges are not used and it just randomly assigns a level from the min-max range for the encounter zone in question with no variance.
- **Spawn Level Mult:** The level multiplier for enemies marked as "Easy", "Normal", etc. They will be around the current level of the encounter zone times the multiplier provided.

  ### NPCs
- **Unlevel NPCs:** NPCs get unleveled if it's checked.
- **Remove Old Perks:** NPCs will have all of their already existing perks removed, but only perks that come from the vanilla Skyrim data files like Skyrim.esm, Dawnguard.esm, Dragonborn.esm and Update.esm. This means if a mod like Revenge of the Enemies adds perks, those will stay.
- **Disable Extra Damage Perks:** There are several perks in the game that simply multiply NPC damage for no real reason except them being high level. This is something I believe to be bad design and should be done with higher skill levels, perks and better gear, not to mention that one handed weapons get more of a boost than two handed so high level melee enemies had barely any difference between 1/2 handed weapon damage. This option makes those perks do nothing, but doesn't delete them.
- **Scaling Followers:** If enabled, followers will still keep to the old formula of scaling along with the player in level, so their health, stamina and magicka will slowly improve as you level up alongside them. If turned off, followers will have a static level assigned. Even if this option is turned on, **followers still get perks and skills based on what their static level would be** so no worries there.
- **NPC Perks Per Level:** The number of perks NPCs have to assign per level they have. It can be a decimal value so they can get 1.35 perks per every level they have, resulting in a level 25 enemy having around 33 perks assigned.
- **NPC Skills Per Level:** The number of skillpoints they get to assign to their class skills per level. The points are distributed based on their classes which have different weights for what skills get more points and what skills get less. These also technically control the kinds of perks they can get because they have to meet the requirements just like the player.
- **NPC Max Skill Level:** What it says, really. If you set it to above 100 NPCs can have skills go above 100 (if they have enough skill points for that, of course).

  ### Items
- **Unlevel Items:** Items get unleveled if it's checked.
- **Max Item Level:** The level starting from which items are removed in the leveled item lists. The tooltip gives you a rough level range for each item tier. Those tiers can be different of course if you have mods installed that change the leveled lists. The default should be fine for most people.
- **Min Item Level:** The level below which items are removed in the leveled item lists. Recommended to be set to zero but if you have some weird setup where you only play some large quest mods you could remove low-tier weapons with it.
- **Unlevel Artifacts:** What it says, but done differently from mods on the Nexus. What this does is if a list only contains item variants (or if it corresponds to a key in the config, but more on the configs later) then everything except the highest level one will be deleted. This means that weapons like Chillrend or Dragonbane will only ever have their strongest variants pop up in-game without having to change all 6 of their different leveled variants.
- **Allow Empty Lists:** If turned on, leveled lists that meet the removal level requirement as a whole (like leveled lists that have only ebony weapons) will be completely emptied. This is a bad idea (in most cases) because some mods might use those lists to give a unique NPC a strong enchanted weapon, which in this case would leave them unarmed because the list would be empty. The lists with rare items only don't really have to be emptied because they are usually part of other lists that have a variety of every item listed. In those cases the references to the strong lists would be scrubbed from them while still leaving their data intact.
- **Allow Mid Tier:** If turned on, NPCs will have mid-tier leveled lists entries, not just the newly generated weak and strong versions. What this means is that you will start seeing higher quality gear on lower level enemies, but it will not be quite as widespread among them as on higher level ones. Recommended to leave off for better early game progression. 

### Item Adjustments
- **Morrowlootify Items:** Main toggle for patching weapons and armor to be similar to their Morrowloot counterparts.
  ### Options
- **Base Stat Plugins:** This plugin list allows you to define which plugins the patcher takes the base item stats from for comparison and modification. For example, if someone has *Weapon Armor Clothing and Clutter Fixes* installed which increases the weight and armor value of rare equipment relative to vanilla, the patcher would then apply that ratio to the Morrowloot stats. If this is not something that person would want, they can add WACCF to the base stat plugins list which would make the patcher take those stats as the base value and not apply the ratio to the Morrowloot stats.
- **Skip Artifacts:** Makes the stat rescaler skip adjusting everything that is marked with the "DaedricArtifact" tag. Useful if you have some mods that might make weird changes resulting in weird stats. Alternatively you could just correct those in a little xEdit patch.
- **Skip Uniques:** Makes the stat rescaler skip adjusting everything that is marked with the "MagicDisallowEnchanting" tag. It basically skips every item that you would not be able to disenchant which includes the artifacts from the previous option. Again, just in case you have some specific weird setup where the numbers come out badly.

## The Configs
### artifactKeys.json
A list of editor IDs or editor ID snippets that if matched with a leveled item list record will have it be treated as an artifact list, meaning everything except the highest level item or items in it will be removed. Should be added to if you have a mod that adds artifacts and they don't each have their unique leveled item list or if you just want the functionality described above. The keys are case insensitive.

### customFollowers.json
A list of entries comprised of single keys that are entire editor IDs or snippets and an array of forbidden keys. Any NPC whose editor ID contains one of the listed keys but none of the possible forbidden keys associated with it will be treated as a follower for the NPC unleveling. The keys are case insensitive.

### excludedLVLI.json
A list of keys and forbidden keys, each being an editor ID or editor ID snippet. When a key matches a leveled item list's editor ID and none of the forbidden keys do, they will not have any entries removed from them but they will still be de-leveled. By default used to make sure enchanted item sublists remain whole. The keys are case insensitive.

### excludedNPCs.json
A list of keys and forbidden keys, each being an editor ID or editor ID snippet. When a key matches an NPC's editor ID and none of the forbidden keys do, they will not be unleveled. The keys are case insensitive.

### excludedPerks.json
A list of keys and forbidden keys, each being an editor ID or editor ID snippet. When a key matches a perk's editor ID and none of the forbidden keys, it will be excluded from being given to NPCs. The keys are case insensitive.

### NPCsByEDID.json
A list of NPC entries, each being comprised by an array of keys, an array of forbidden keys and a level entry. When an NPC's editor ID matches one of the keys in an entry and none of the forbidden keys in it, they will be assigned the specific level in the entry. The keys are case insensitive.

### NPCsByFaction.json
A list of NPC entries, each being comprised by an array of keys, an array of forbidden keys and either a level entry or a minimum and maximum level entry. If any of a faction's editor IDs that an NPC belongs to contain one of the keys in an entry and none of the forbidden keys, that NPC will either get the flat level or a random level between the given minimum and maximum, depending on which were defined. The keys are case insensitive.

### raceLevelModifiers.json
Possibly the most versatile config file, containing entries each comprised of a set of keys, a set of forbidden keys and a LevelModifierAdd and LevelModifierMult field. If the editor ID of an NPC's race contains a key from an entry but none of the forbidden keys from it, they will have their level ranges modified. If an entry for "Draugr" is listed with a LevelModifierAdd value of 10 and a LevelModifierMult value of 1.2, then every draugr will have their level increased by 20% and then get a flat boost of 10. This would make a basic level 1 draugr variant have a level of 11 and a level 20 draugr variant have a level of 34. Similarly an entry for "Dragon" with an Add value of 50 and a Mult level of 0.2 would mean a level 1 dragon variant would have a level of 50 and a level 60 dragon variant would now have a level of 62. The keys are case insensitive.

### zoneTypesByEDID.json
A list of entries each comprised of an array of keys, forbidden keys and a minimum level, maximum level and level range value. If an encounter zone's editor ID matches a key in one of the entries but none of the forbidden keys in it, it will get a random level between the minimum and maximum value defined with a level distance of "range" between the minimum and maximum. The keys are case insensitive.

### zoneTypesByKeyword.json
A list of entries each comprised of an array of keys and forbidden keys and a minimum level, maximum level and level range value. If an encounter zone's location has any of the keywords listed in an entry and none of the ones in the forbidden keys array, it will get a random level between the minimum and maximum value defined with a level distance of "range" between the minimum and maximum. The keys are case insensitive.

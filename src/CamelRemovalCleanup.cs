using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.Issues;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;

namespace CamelCavalry
{
    // Opt-in removal mode: while prepare_removal.txt sits in the module folder, every Camel Cavalry troop and every item
    // supplied by Camel Armor BM168 is swapped for a vanilla equivalent. The resulting save can stop loading both modules.
    internal static class CamelRemovalCleanup
    {
        internal const string FlagFileName = "prepare_removal.txt";

        private static readonly Dictionary<string, string> TroopReplacements = new Dictionary<string, string>
        {
            { CamelMountRules.RiderId, "aserai_mameluke_cavalry" },
            { CamelMountRules.LancerId, "aserai_mameluke_heavy_cavalry" },
            { CamelMountRules.ArcherId, "aserai_mameluke_heavy_cavalry" },
            { CamelMountRules.TorguudId, "khuzait_torguud" },
            { CamelMountRules.KheshigId, "khuzait_kheshig" },
            { CamelMountRules.HeavyArcherId, "khuzait_khans_guard" }
        };

        private static readonly Dictionary<string, string> ItemReplacements = new Dictionary<string, string>
        {
            { "camel_scale_armor_1", "camel_saddle" },
            { "camel_lamellar_armor_1", "camel_saddle" },
            { "camel_lamellar_armor_1b", "camel_saddle" },
            { "camel_chainmail_armor_1", "camel_saddle" },
            { "aserai_camel_lance_1", "aserai_lance_1_t5" },
            { "camel_cataphract", CamelMountRules.WarCamelId },
            { "camel_cataphract_m", CamelMountRules.WarCamelId },
            { "camel_cataphract_l", CamelMountRules.WarCamelId },
            { "camel_cataphract_xl", CamelMountRules.WarCamelId }
        };

        private class Result
        {
            public int Troops;
            public int Volunteers;
            public int Items;
            public int HeroSlots;
            public int PartiesInBattle;
            public readonly List<string> Unreachable = new List<string>();
        }

        internal static bool Requested
        {
            get
            {
                string flagPath = FlagPath();
                return flagPath != null && File.Exists(flagPath);
            }
        }

        private static string FlagPath()
        {
            string dllFolder = Path.GetDirectoryName(typeof(CamelRemovalCleanup).Assembly.Location);
            if (string.IsNullOrEmpty(dllFolder))
            {
                return null;
            }

            // The DLL lives in Modules\CamelCavalry\bin\Win64_Shipping_Client.
            return Path.GetFullPath(Path.Combine(dllFolder, "..", "..", FlagFileName));
        }

        internal static void Run(bool announce)
        {
            Dictionary<CharacterObject, CharacterObject> troopMap = ResolveTroops();
            Dictionary<ItemObject, ItemObject> itemMap = ResolveItems();
            if (troopMap == null || itemMap == null)
            {
                if (announce)
                {
                    InformationManager.DisplayMessage(new InformationMessage(new TextObject("{=cmlcav_removal_failed}Camel Cavalry: removal cleanup could not find its vanilla replacements. Nothing was changed; see the game log.").ToString(), Colors.Red));
                }
                return;
            }

            Result result = new Result();

            foreach (MobileParty party in MobileParty.All)
            {
                if (party != null)
                {
                    ConvertParty(party.Party, troopMap, itemMap, result);
                }
            }

            foreach (Settlement settlement in Settlement.All)
            {
                if (settlement == null)
                {
                    continue;
                }

                ConvertParty(settlement.Party, troopMap, itemMap, result);
                if (settlement.Stash != null)
                {
                    result.Items += ConvertItems(settlement.Stash, itemMap);
                }
            }

            ConvertHeroes(Hero.AllAliveHeroes, troopMap, itemMap, result);
            ConvertHeroes(Hero.DeadOrDisabledHeroes, troopMap, itemMap, result);
            ConvertWorkshopWarehouses(itemMap, result);
            ConvertIssueTroops(troopMap, result);
            ConvertAlleyTroops(troopMap, result);
            ConvertBreakInCasualties(troopMap, result);

            bool changed = result.Troops + result.Volunteers + result.Items + result.HeroSlots > 0;
            if (changed || announce)
            {
                CamelLog.Print("removal cleanup: converted " + result.Troops + " troops, " + result.Volunteers + " volunteer slots, "
                    + result.Items + " inventory items and " + result.HeroSlots + " hero equipment slots; "
                    + result.PartiesInBattle + " parties skipped because they are in a battle or siege.");
                if (result.Unreachable.Count > 0)
                {
                    CamelLog.Print("removal cleanup could NOT reach: " + string.Join(", ", result.Unreachable.ToArray()) + ".");
                }
            }

            if (!announce && !changed)
            {
                return;
            }

            TextObject message;
            Color color;
            if (result.Unreachable.Count > 0)
            {
                message = new TextObject("{=cmlcav_removal_partial}Camel Cavalry removal: converted {TROOPS} troops and {ITEMS} items, but could not reach {PLACES}. Empty those by hand before disabling Camel Cavalry and Camel Armor BM168; see the game log.");
                message.SetTextVariable("PLACES", string.Join(", ", result.Unreachable.ToArray()));
                color = Colors.Red;
            }
            else if (result.PartiesInBattle > 0)
            {
                message = new TextObject("{=cmlcav_removal_pending}Camel Cavalry removal: converted {TROOPS} troops and {ITEMS} items. {SKIPPED} parties are in a battle or siege; let a day pass, then save and disable Camel Cavalry and Camel Armor BM168.");
                message.SetTextVariable("SKIPPED", result.PartiesInBattle);
                color = Colors.Yellow;
            }
            else
            {
                message = new TextObject("{=cmlcav_removal_done}Camel Cavalry removal: converted {TROOPS} troops and {ITEMS} items to vanilla ones. Save the game now, then disable Camel Cavalry and Camel Armor BM168.");
                color = Colors.Green;
            }

            message.SetTextVariable("TROOPS", result.Troops + result.Volunteers);
            message.SetTextVariable("ITEMS", result.Items + result.HeroSlots);
            InformationManager.DisplayMessage(new InformationMessage(message.ToString(), color));
        }

        private static Dictionary<CharacterObject, CharacterObject> ResolveTroops()
        {
            Dictionary<CharacterObject, CharacterObject> map = new Dictionary<CharacterObject, CharacterObject>();
            foreach (KeyValuePair<string, string> pair in TroopReplacements)
            {
                CharacterObject replacement = MBObjectManager.Instance.GetObject<CharacterObject>(pair.Value);
                if (replacement == null)
                {
                    CamelLog.Print("removal cleanup: vanilla troop '" + pair.Value + "' is missing.");
                    return null;
                }

                CharacterObject camelTroop = MBObjectManager.Instance.GetObject<CharacterObject>(pair.Key);
                if (camelTroop != null)
                {
                    map[camelTroop] = replacement;
                }
            }

            return map;
        }

        private static Dictionary<ItemObject, ItemObject> ResolveItems()
        {
            Dictionary<ItemObject, ItemObject> map = new Dictionary<ItemObject, ItemObject>();
            foreach (KeyValuePair<string, string> pair in ItemReplacements)
            {
                ItemObject replacement = MBObjectManager.Instance.GetObject<ItemObject>(pair.Value);
                if (replacement == null)
                {
                    CamelLog.Print("removal cleanup: vanilla item '" + pair.Value + "' is missing.");
                    return null;
                }

                ItemObject camelItem = MBObjectManager.Instance.GetObject<ItemObject>(pair.Key);
                if (camelItem != null)
                {
                    map[camelItem] = replacement;
                }
            }

            return map;
        }

        // Player workshop warehouses are saved inside the workshop behaviour, not on any settlement or party.
        private static void ConvertWorkshopWarehouses(Dictionary<ItemObject, ItemObject> itemMap, Result result)
        {
            const string place = "workshop warehouses";
            WorkshopsCampaignBehavior workshops = Campaign.Current.GetCampaignBehavior<WorkshopsCampaignBehavior>();
            if (workshops == null)
            {
                return;
            }

            object value;
            if (!TryReadField(typeof(WorkshopsCampaignBehavior), workshops, "_warehouseRosterPerSettlement", typeof(KeyValuePair<Settlement, ItemRoster>[]), place, result, out value))
            {
                return;
            }

            KeyValuePair<Settlement, ItemRoster>[] warehouses = value as KeyValuePair<Settlement, ItemRoster>[];
            if (warehouses == null)
            {
                return;
            }

            foreach (KeyValuePair<Settlement, ItemRoster> warehouse in warehouses)
            {
                if (warehouse.Value != null)
                {
                    result.Items += ConvertItems(warehouse.Value, itemMap);
                }
            }
        }

        // Troops a companion took to solve an issue, and troops back from one that wait to rejoin, live outside every party.
        private static void ConvertIssueTroops(Dictionary<CharacterObject, CharacterObject> troopMap, Result result)
        {
            IssueManager issueManager = Campaign.Current.IssueManager;
            if (issueManager == null)
            {
                return;
            }

            if (issueManager.Issues != null)
            {
                foreach (IssueBase issue in issueManager.Issues.Values)
                {
                    if (issue != null)
                    {
                        result.Troops += ConvertTroops(issue.AlternativeSolutionSentTroops, troopMap);
                    }
                }
            }

            object returning;
            if (TryReadField(typeof(IssueManager), issueManager, "_awaitingAlternativeSolutionTroops", typeof(TroopRoster), "troops returning from issues", result, out returning))
            {
                result.Troops += ConvertTroops(returning as TroopRoster, troopMap);
            }
        }

        // Troops guarding the player's alleys are saved in SandBox's alley behaviour, one roster per alley.
        private static void ConvertAlleyTroops(Dictionary<CharacterObject, CharacterObject> troopMap, Result result)
        {
            const string place = "alley guards";
            Type alleyBehaviorType = AccessTools.TypeByName("SandBox.CampaignBehaviors.AlleyCampaignBehavior");
            MethodInfo getBehavior = AccessTools.DeclaredMethod(typeof(Campaign), "GetCampaignBehavior", Type.EmptyTypes);
            if (alleyBehaviorType == null || getBehavior == null || !getBehavior.IsGenericMethodDefinition)
            {
                result.Unreachable.Add(place);
                return;
            }

            object alleyBehavior = getBehavior.MakeGenericMethod(alleyBehaviorType).Invoke(Campaign.Current, null);
            if (alleyBehavior == null)
            {
                return;
            }

            object value;
            if (!TryReadField(alleyBehaviorType, alleyBehavior, "_playerOwnedCommonAreaData", null, place, result, out value))
            {
                return;
            }

            IEnumerable alleys = value as IEnumerable;
            if (alleys == null)
            {
                return;
            }

            foreach (object alley in alleys)
            {
                if (alley == null)
                {
                    continue;
                }

                object roster;
                if (!TryReadField(alley.GetType(), alley, "TroopRoster", typeof(TroopRoster), place, result, out roster))
                {
                    return;
                }

                result.Troops += ConvertTroops(roster as TroopRoster, troopMap);
            }
        }

        // Only filled while the player breaks into or out of a siege, but it is saved, so a save made then keeps it.
        private static void ConvertBreakInCasualties(Dictionary<CharacterObject, CharacterObject> troopMap, Result result)
        {
            EncounterGameMenuBehavior encounterMenus = Campaign.Current.GetCampaignBehavior<EncounterGameMenuBehavior>();
            object casualties;
            if (encounterMenus != null && TryReadField(typeof(EncounterGameMenuBehavior), encounterMenus, "_breakInOutCasualties", typeof(TroopRoster), "siege break-in casualties", result, out casualties))
            {
                result.Troops += ConvertTroops(casualties as TroopRoster, troopMap);
            }
        }

        private static bool TryReadField(Type owner, object instance, string fieldName, Type expectedType, string place, Result result, out object value)
        {
            value = null;
            FieldInfo field = AccessTools.DeclaredField(owner, fieldName);
            if (field == null || (expectedType != null && field.FieldType != expectedType))
            {
                if (!result.Unreachable.Contains(place))
                {
                    result.Unreachable.Add(place);
                }

                return false;
            }

            value = field.GetValue(instance);
            return true;
        }

        private static void ConvertParty(PartyBase party, Dictionary<CharacterObject, CharacterObject> troopMap, Dictionary<ItemObject, ItemObject> itemMap, Result result)
        {
            if (party == null)
            {
                return;
            }

            // Rosters of parties fighting on the map are owned by the running MapEvent; they are retried on the next daily tick.
            if (party.MapEvent != null)
            {
                if (HasCamelTroops(party.MemberRoster, troopMap) || HasCamelTroops(party.PrisonRoster, troopMap) || HasCamelItems(party.ItemRoster, itemMap))
                {
                    result.PartiesInBattle++;
                }

                return;
            }

            result.Troops += ConvertTroops(party.MemberRoster, troopMap);
            result.Troops += ConvertTroops(party.PrisonRoster, troopMap);
            if (party.ItemRoster != null)
            {
                result.Items += ConvertItems(party.ItemRoster, itemMap);
            }
        }

        private static bool HasCamelItems(ItemRoster roster, Dictionary<ItemObject, ItemObject> itemMap)
        {
            if (roster == null)
            {
                return false;
            }

            for (int i = 0; i < roster.Count; i++)
            {
                ItemObject item = roster.GetElementCopyAtIndex(i).EquipmentElement.Item;
                if (item != null && itemMap.ContainsKey(item))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool HasCamelTroops(TroopRoster roster, Dictionary<CharacterObject, CharacterObject> troopMap)
        {
            if (roster == null)
            {
                return false;
            }

            for (int i = 0; i < roster.Count; i++)
            {
                CharacterObject character = roster.GetElementCopyAtIndex(i).Character;
                if (character != null && troopMap.ContainsKey(character))
                {
                    return true;
                }
            }

            return false;
        }

        private static int ConvertTroops(TroopRoster roster, Dictionary<CharacterObject, CharacterObject> troopMap)
        {
            if (roster == null)
            {
                return 0;
            }

            int converted = 0;

            // Walk backwards: removing a stack shifts later entries, and replacements are appended past the cursor.
            for (int i = roster.Count - 1; i >= 0; i--)
            {
                TroopRosterElement element = roster.GetElementCopyAtIndex(i);
                CharacterObject replacement;
                if (element.Character == null || element.Number <= 0 || !troopMap.TryGetValue(element.Character, out replacement))
                {
                    continue;
                }

                roster.AddToCounts(element.Character, -element.Number, false, -element.WoundedNumber, -element.Xp);
                roster.AddToCounts(replacement, element.Number, false, element.WoundedNumber, element.Xp);
                converted += element.Number;
            }

            return converted;
        }

        private static int ConvertItems(ItemRoster roster, Dictionary<ItemObject, ItemObject> itemMap)
        {
            int converted = 0;
            for (int i = roster.Count - 1; i >= 0; i--)
            {
                ItemRosterElement element = roster.GetElementCopyAtIndex(i);
                ItemObject item = element.EquipmentElement.Item;
                ItemObject replacement;
                if (item == null || element.Amount <= 0 || !itemMap.TryGetValue(item, out replacement))
                {
                    continue;
                }

                roster.AddToCounts(element.EquipmentElement, -element.Amount);
                roster.AddToCounts(replacement, element.Amount);
                converted += element.Amount;
            }

            return converted;
        }

        private static void ConvertHeroes(IEnumerable<Hero> heroes, Dictionary<CharacterObject, CharacterObject> troopMap, Dictionary<ItemObject, ItemObject> itemMap, Result result)
        {
            if (heroes == null)
            {
                return;
            }

            foreach (Hero hero in heroes)
            {
                if (hero == null)
                {
                    continue;
                }

                result.HeroSlots += ConvertEquipment(hero.BattleEquipment, itemMap);
                result.HeroSlots += ConvertEquipment(hero.CivilianEquipment, itemMap);
                result.HeroSlots += ConvertEquipment(hero.StealthEquipment, itemMap);

                // Notables upgrade their waiting volunteers along the troop tree, so camel troops can sit here too.
                CharacterObject[] volunteers = hero.VolunteerTypes;
                if (volunteers == null)
                {
                    continue;
                }

                for (int i = 0; i < volunteers.Length; i++)
                {
                    CharacterObject replacement;
                    if (volunteers[i] != null && troopMap.TryGetValue(volunteers[i], out replacement))
                    {
                        volunteers[i] = replacement;
                        result.Volunteers++;
                    }
                }
            }
        }

        private static int ConvertEquipment(Equipment equipment, Dictionary<ItemObject, ItemObject> itemMap)
        {
            if (equipment == null)
            {
                return 0;
            }

            int converted = 0;
            for (int i = 0; i < (int)EquipmentIndex.NumEquipmentSetSlots; i++)
            {
                ItemObject item = equipment[i].Item;
                ItemObject replacement;
                if (item != null && itemMap.TryGetValue(item, out replacement))
                {
                    equipment[i] = new EquipmentElement(replacement);
                    converted++;
                }
            }

            return converted;
        }
    }
}

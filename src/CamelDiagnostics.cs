using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;

namespace CamelCavalry
{
    // Writes what the XML and the patches actually produced to the game log, so a test run can be checked without guessing.
    internal static class CamelDiagnostics
    {
        // Vanilla troop, the vanilla upgrade it must keep, and the camel upgrade added next to it.
        private static readonly string[][] Branches =
        {
            new[] { "aserai_mameluke_regular", "aserai_mameluke_cavalry", CamelMountRules.RiderId },
            new[] { "khuzait_qanqli", "khuzait_torguud", CamelMountRules.TorguudId }
        };

        private static readonly string[] ItemIds = { "camel_scale_armor_1", "camel_lamellar_armor_1", "camel_lamellar_armor_1b", "camel_chainmail_armor_1", "aserai_camel_lance_1" };
        private static readonly string[] RanchIds = { CamelVillageProduction.SteppeRanchId, "desert_horse_ranch" };

        internal static void Report()
        {
            MBObjectManager objects = MBObjectManager.Instance;
            if (objects == null)
            {
                return;
            }

            CamelLog.Print("exact camel upgrade rules " + (CamelMountRules.Active ? "active." : "INACTIVE; camel troops accept any item of their category."));

            foreach (string[] branch in Branches)
            {
                ReportBranch(objects, branch[0], branch[1], branch[2]);
            }

            foreach (string troopId in CamelMountRules.TroopIds)
            {
                ReportTroop(objects, troopId, CamelMountRules.MountIdFor(troopId));
            }

            foreach (string itemId in ItemIds)
            {
                ReportItem(objects, itemId);
            }

            foreach (string ranchId in RanchIds)
            {
                ReportRanch(objects, ranchId);
            }

            CamelLog.Print("Khuzait town workshops " + (CamelKhuzaitMarket.Active ? "can make camel armor." : "will NOT make camel armor."));

            ReportAiTroops();

            if (!CamelMountRules.Active)
            {
                InformationManager.DisplayMessage(new InformationMessage(new TextObject("{=cmlcav_rules_off}Camel Cavalry: exact camel upgrade rules are off, so camel troops accept any horse or war horse. See the game log.").ToString(), Colors.Red));
            }
        }

        // AI parties and garrisons upgrade without mounts, so these counts show whether lords really train the camel troops.
        internal static void ReportAiTroops()
        {
            MBObjectManager objects = MBObjectManager.Instance;
            if (objects == null || MobileParty.All == null)
            {
                return;
            }

            List<string> parts = new List<string>();
            foreach (string troopId in CamelMountRules.TroopIds)
            {
                CharacterObject troop = objects.GetObject<CharacterObject>(troopId);
                if (troop == null)
                {
                    continue;
                }

                int soldiers = 0;
                int parties = 0;
                foreach (MobileParty party in MobileParty.All)
                {
                    if (party == null || party == MobileParty.MainParty || party.MemberRoster == null)
                    {
                        continue;
                    }

                    int count = party.MemberRoster.GetTroopRoster().Where(e => e.Character == troop).Sum(e => e.Number);
                    if (count > 0)
                    {
                        soldiers += count;
                        parties++;
                    }
                }

                parts.Add(troopId + " " + soldiers + " in " + parties + " parties");
            }

            CamelLog.Print("camel troops in AI parties and garrisons: " + string.Join("; ", parts));
        }

        private static void ReportBranch(MBObjectManager objects, string sourceId, string vanillaTargetId, string camelTargetId)
        {
            CharacterObject source = objects.GetObject<CharacterObject>(sourceId);
            if (source == null)
            {
                CamelLog.Print("WARNING " + sourceId + " is missing.");
                return;
            }

            string[] targets = TargetIds(source);
            CamelLog.Print(sourceId + " upgrades to: " + string.Join(", ", targets));
            if (!targets.Contains(camelTargetId) || !targets.Contains(vanillaTargetId))
            {
                CamelLog.Print("WARNING the camel branch was not added next to " + vanillaTargetId + "; another mod may have replaced " + sourceId + " after Camel Cavalry loaded.");
            }
        }

        private static void ReportTroop(MBObjectManager objects, string troopId, string mountId)
        {
            CharacterObject troop = objects.GetObject<CharacterObject>(troopId);
            if (troop == null)
            {
                CamelLog.Print("WARNING troop " + troopId + " is missing.");
                return;
            }

            ItemCategory category = troop.UpgradeRequiresItemFromCategory;
            CamelLog.Print(troopId + ": tier " + troop.Tier + ", level " + troop.Level + ", requires " + (mountId ?? "no item") + " (category " + (category != null ? category.StringId : "none") + "), upgrades to: " + string.Join(", ", TargetIds(troop)));
            if (mountId == null)
            {
                if (category != null)
                {
                    CamelLog.Print("WARNING " + troopId + " should need no upgrade item but requires category " + category.StringId + ".");
                }

                return;
            }

            ItemObject mount = objects.GetObject<ItemObject>(mountId);
            if (mount == null)
            {
                CamelLog.Print("WARNING mount item " + mountId + " is missing.");
            }
            else if (mount.ItemCategory != category)
            {
                CamelLog.Print("WARNING " + mountId + " is in category " + (mount.ItemCategory != null ? mount.ItemCategory.StringId : "none") + ", not the category " + troopId + " requires, so the party screen would never offer this upgrade.");
            }
        }

        private static void ReportItem(MBObjectManager objects, string itemId)
        {
            ItemObject item = objects.GetObject<ItemObject>(itemId);
            if (item == null)
            {
                CamelLog.Print("WARNING dependency item " + itemId + " is missing; check that Camel Armor BM168 is installed, enabled, and loaded before Camel Cavalry.");
                return;
            }

            CamelLog.Print(itemId + ": category " + (item.ItemCategory != null ? item.ItemCategory.StringId : "none") + ", value " + item.Value + ", tier " + item.Tier + ", merchandise " + !item.NotMerchandise + ", civilian " + item.IsCivilian);
        }

        private static void ReportRanch(MBObjectManager objects, string ranchId)
        {
            // Productions returns the private _productions list itself, the one CamelVillageProduction appends to.
            VillageType ranch = objects.GetObject<VillageType>(ranchId);
            if (ranch == null || ranch.Productions == null)
            {
                CamelLog.Print("WARNING village type " + ranchId + " is missing.");
                return;
            }

            List<string> parts = new List<string>();
            foreach (ValueTuple<ItemObject, float> production in ranch.Productions)
            {
                parts.Add((production.Item1 != null ? production.Item1.StringId : "?") + " " + production.Item2);
            }

            ItemObject primary = ranch.PrimaryProduction;
            CamelLog.Print(ranchId + " produces: " + string.Join(", ", parts) + "; main product " + (primary != null ? primary.StringId : "none") + ".");
        }

        private static string[] TargetIds(CharacterObject troop)
        {
            return troop.UpgradeTargets == null ? new string[0] : troop.UpgradeTargets.Where(t => t != null).Select(t => t.StringId).ToArray();
        }
    }
}

using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;

namespace CamelCavalry
{
    // The XML only knows item categories (camel is a "horse", war_camel a "war_horse"); these rules narrow the
    // requirement of each camel troop to one exact mount. The requirement belongs to the upgrade target, as in vanilla.
    internal static class CamelMountRules
    {
        internal const string RiderId = "camel_cavalry_aserai_camel_rider";
        internal const string LancerId = "camel_cavalry_aserai_camel_lancer";
        internal const string ArcherId = "camel_cavalry_aserai_camel_archer";
        internal const string TorguudId = "camel_cavalry_khuzait_camel_torguud";
        internal const string KheshigId = "camel_cavalry_khuzait_camel_kheshig";
        internal const string HeavyArcherId = "camel_cavalry_khuzait_heavy_camel_archer";
        internal const string CamelId = "camel";
        internal const string WarCamelId = "war_camel";

        private static readonly Dictionary<string, string> RequiredMountByTarget = new Dictionary<string, string>
        {
            { RiderId, CamelId },
            { LancerId, WarCamelId },
            { ArcherId, WarCamelId },
            { TorguudId, CamelId },
            { KheshigId, WarCamelId }
        };

        // The Heavy Camel Archer needs no upgrade item, like the vanilla Khan's Guard: the Camel Kheshig already rides a war camel.
        internal static readonly string[] TroopIds = { RiderId, LancerId, ArcherId, TorguudId, KheshigId, HeavyArcherId };

        // False until every patch is in place, so a partial failure never leaves the UI and the logic disagreeing.
        internal static bool Active;

        internal static string RequiredMountId(CharacterObject upgradeTarget)
        {
            if (!Active || upgradeTarget == null)
            {
                return null;
            }

            return MountIdFor(upgradeTarget.StringId);
        }

        internal static string MountIdFor(string troopId)
        {
            string mountId;
            return troopId != null && RequiredMountByTarget.TryGetValue(troopId, out mountId) ? mountId : null;
        }

        internal static int CountExact(ItemRoster roster, string mountId)
        {
            if (roster == null)
            {
                return 0;
            }

            int count = 0;
            foreach (ItemRosterElement element in roster)
            {
                ItemObject item = element.EquipmentElement.Item;
                if (item != null && item.StringId == mountId && element.Amount > 0)
                {
                    count += element.Amount;
                }
            }

            return count;
        }

        internal static TextObject MountName(string mountId)
        {
            ItemObject item = MBObjectManager.Instance != null ? MBObjectManager.Instance.GetObject<ItemObject>(mountId) : null;
            return item != null && item.Name != null ? item.Name : new TextObject("{=!}" + mountId);
        }
    }
}

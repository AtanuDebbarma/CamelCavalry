using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;

namespace CamelCavalry
{
    // Town workshops only make items of the town's culture, and the camel armors are Aserai, so Khuzait towns would never
    // stock harness for their camel troops. Khuzait towns may also make them; every other town keeps vanilla behaviour.
    internal static class CamelKhuzaitMarket
    {
        private const string KhuzaitCultureId = "khuzait";

        private static readonly HashSet<string> CamelArmorIds = new HashSet<string>
        {
            "camel_scale_armor_1",
            "camel_lamellar_armor_1",
            "camel_lamellar_armor_1b",
            "camel_chainmail_armor_1"
        };

        internal static bool Active;

        internal static bool Apply(Harmony harmony)
        {
            MethodInfo preferred = AccessTools.DeclaredMethod(typeof(WorkshopsCampaignBehavior), "IsItemPreferredForTown", new[] { typeof(ItemObject), typeof(Town) });
            if (preferred == null || preferred.ReturnType != typeof(bool))
            {
                CamelLog.Print("could not find the workshop culture check; Khuzait towns will not make camel armor.");
                return false;
            }

            harmony.Patch(preferred, postfix: new HarmonyMethod(AccessTools.DeclaredMethod(typeof(CamelKhuzaitMarket), "IsItemPreferredForTownPostfix")));
            return true;
        }

        private static void IsItemPreferredForTownPostfix(ItemObject item, Town townComponent, ref bool __result)
        {
            if (__result || item == null || townComponent == null || townComponent.Culture == null)
            {
                return;
            }

            if (townComponent.Culture.StringId == KhuzaitCultureId && CamelArmorIds.Contains(item.StringId))
            {
                __result = true;
            }
        }
    }
}

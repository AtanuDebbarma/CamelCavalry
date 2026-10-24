using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.ObjectSystem;

namespace CamelCavalry
{
    // Khuzait horse ranches also breed camels so Khuzait players can upgrade the camel troops. Camel and war camel match the
    // vanilla Aserai horse ranch; pack camels are rarer. Aserai ranches are not touched.
    internal static class CamelVillageProduction
    {
        internal const string SteppeRanchId = "steppe_horse_ranch";

        private static readonly KeyValuePair<string, float>[] SteppeRanchAdditions =
        {
            new KeyValuePair<string, float>(CamelMountRules.CamelId, 0.3f),
            new KeyValuePair<string, float>(CamelMountRules.WarCamelId, 0.08f),
            new KeyValuePair<string, float>("pack_camel", 0.1f)
        };

        private static MethodInfo _steppeRanchGetter;
        private static FieldInfo _productionsField;

        internal static bool Active;

        internal static bool Apply(Harmony harmony)
        {
            ConstructorInfo constructor = AccessTools.DeclaredConstructor(typeof(DefaultVillageTypes), Type.EmptyTypes);
            _steppeRanchGetter = AccessTools.PropertyGetter(typeof(DefaultVillageTypes), "VillageTypeSteppeHorseRanch");
            _productionsField = AccessTools.DeclaredField(typeof(VillageType), "_productions");
            if (constructor == null || _steppeRanchGetter == null || _steppeRanchGetter.ReturnType != typeof(VillageType)
                || _productionsField == null || _productionsField.FieldType != typeof(MBList<ValueTuple<ItemObject, float>>))
            {
                CamelLog.Print("could not find the village type code; Khuzait horse ranches will not breed camels.");
                return false;
            }

            try
            {
                harmony.Patch(constructor, postfix: new HarmonyMethod(AccessTools.DeclaredMethod(typeof(CamelVillageProduction), "DefaultVillageTypesPostfix")));
            }
            catch (Exception exception)
            {
                CamelLog.Print("could not patch village production; Khuzait horse ranches will not breed camels: " + exception.Message);
                return false;
            }

            return true;
        }

        // The game rebuilds village types for every new and loaded campaign, so this runs each time. Additions go to the end of
        // the list: vanilla's starting town stock shrinks for each later entry, so putting them first would cut the horses.
        private static void DefaultVillageTypesPostfix(DefaultVillageTypes __instance)
        {
            try
            {
                VillageType ranch = _steppeRanchGetter.Invoke(__instance, null) as VillageType;
                MBList<ValueTuple<ItemObject, float>> productions = ranch != null ? _productionsField.GetValue(ranch) as MBList<ValueTuple<ItemObject, float>> : null;
                if (productions == null)
                {
                    CamelLog.Print("the Khuzait horse ranch has no production list; it will not breed camels.");
                    return;
                }

                foreach (KeyValuePair<string, float> addition in SteppeRanchAdditions)
                {
                    ItemObject item = MBObjectManager.Instance.GetObject<ItemObject>(addition.Key);
                    if (item == null)
                    {
                        CamelLog.Print("item " + addition.Key + " is missing; the Khuzait horse ranch will not produce it.");
                        continue;
                    }

                    bool present = false;
                    foreach (ValueTuple<ItemObject, float> production in productions)
                    {
                        if (production.Item1 == item)
                        {
                            present = true;
                            break;
                        }
                    }

                    if (!present)
                    {
                        productions.Add(new ValueTuple<ItemObject, float>(item, addition.Value));
                    }
                }
            }
            catch (Exception exception)
            {
                CamelLog.Print("could not add camels to the Khuzait horse ranch: " + exception.Message);
            }
        }
    }
}

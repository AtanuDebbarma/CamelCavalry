using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.ViewModelCollection;
using TaleWorlds.CampaignSystem.ViewModelCollection.Party;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;

namespace CamelCavalry
{
    // Player-side exact mount rules. AI parties never check or consume upgrade items, so they are left alone.
    internal static class CamelUpgradePatches
    {
        private static MethodInfo _upgradeRequiresGetter;
        private static FieldInfo _upgradeTargetField;

        [ThreadStatic]
        private static string _consumingMountId;

        internal static bool Apply(Harmony harmony)
        {
            List<string> missing = new List<string>();
            Type[] upgradeTargetCtorArgs = { typeof(int), typeof(CharacterObject), typeof(CharacterCode), typeof(Action<int, int>), typeof(Action<UpgradeTargetVM>) };
            Type[] upgradeHintArgs = { typeof(int), typeof(int), typeof(int), typeof(int), typeof(bool), typeof(PerkObject), typeof(CharacterObject), typeof(TroopRosterElement), typeof(int), typeof(bool) };

            MethodInfo initializeUpgrades = Require(AccessTools.DeclaredMethod(typeof(PartyCharacterVM), "InitializeUpgrades", Type.EmptyTypes), "PartyCharacterVM.InitializeUpgrades", missing);
            MethodInfo countCategory = Require(AccessTools.DeclaredMethod(typeof(PartyCharacterVM), "GetNumOfCategoryItemPartyHas", new[] { typeof(ItemRoster), typeof(ItemCategory) }), "PartyCharacterVM.GetNumOfCategoryItemPartyHas", missing);
            MethodInfo upgradeHint = Require(AccessTools.DeclaredMethod(typeof(CampaignUIHelper), "GetUpgradeHint", upgradeHintArgs), "CampaignUIHelper.GetUpgradeHint", missing);
            ConstructorInfo upgradeTargetCtor = AccessTools.DeclaredConstructor(typeof(UpgradeTargetVM), upgradeTargetCtorArgs);
            if (upgradeTargetCtor == null)
            {
                missing.Add("UpgradeTargetVM constructor");
            }

            MethodInfo upgradeTargetRefresh = Require(AccessTools.DeclaredMethod(typeof(UpgradeTargetVM), "RefreshValues", Type.EmptyTypes), "UpgradeTargetVM.RefreshValues", missing);
            MethodInfo validateCommand = Require(AccessTools.DeclaredMethod(typeof(PartyScreenLogic), "ValidateCommand", new[] { typeof(PartyScreenLogic.PartyCommand) }), "PartyScreenLogic.ValidateCommand", missing);
            MethodInfo upgradeTroop = Require(AccessTools.DeclaredMethod(typeof(PartyScreenLogic), "UpgradeTroop", new[] { typeof(PartyScreenLogic.PartyCommand) }), "PartyScreenLogic.UpgradeTroop", missing);
            MethodInfo removeItems = Require(AccessTools.DeclaredMethod(typeof(PartyScreenLogic), "RemoveItemFromItemRoster", new[] { typeof(ItemCategory), typeof(int) }), "PartyScreenLogic.RemoveItemFromItemRoster", missing);
            MethodInfo hasRequiredItems = Require(AccessTools.DeclaredMethod(typeof(DefaultPartyTroopUpgradeModel), "DoesPartyHaveRequiredItemsForUpgrade", new[] { typeof(PartyBase), typeof(CharacterObject) }), "DefaultPartyTroopUpgradeModel.DoesPartyHaveRequiredItemsForUpgrade", missing);
            _upgradeRequiresGetter = Require(AccessTools.PropertyGetter(typeof(CharacterObject), "UpgradeRequiresItemFromCategory"), "CharacterObject.UpgradeRequiresItemFromCategory", missing);
            _upgradeTargetField = AccessTools.DeclaredField(typeof(UpgradeTargetVM), "_upgradeTarget");
            if (_upgradeTargetField == null || _upgradeTargetField.FieldType != typeof(CharacterObject))
            {
                missing.Add("UpgradeTargetVM._upgradeTarget");
            }

            if (removeItems != null && removeItems.ReturnType != typeof(List<ValueTuple<EquipmentElement, int>>))
            {
                missing.Add("PartyScreenLogic.RemoveItemFromItemRoster return type");
            }

            if (countCategory != null && countCategory.ReturnType != typeof(int))
            {
                missing.Add("PartyCharacterVM.GetNumOfCategoryItemPartyHas return type");
            }

            if (upgradeHint != null && upgradeHint.ReturnType != typeof(string))
            {
                missing.Add("CampaignUIHelper.GetUpgradeHint return type");
            }

            PropertyInfo requirements = AccessTools.DeclaredProperty(typeof(UpgradeTargetVM), "Requirements");
            if (requirements == null || requirements.PropertyType != typeof(UpgradeRequirementsVM) || requirements.GetGetMethod() == null)
            {
                missing.Add("UpgradeTargetVM.Requirements");
            }

            PropertyInfo itemRequirementHint = AccessTools.DeclaredProperty(typeof(UpgradeRequirementsVM), "ItemRequirementHint");
            if (itemRequirementHint == null || itemRequirementHint.PropertyType != typeof(HintViewModel) || itemRequirementHint.GetSetMethod() == null)
            {
                missing.Add("UpgradeRequirementsVM.ItemRequirementHint");
            }

            if (missing.Count > 0)
            {
                CamelLog.Print("could not find these game methods: " + string.Join(", ", missing.ToArray()) + ". No patches were applied.");
                return false;
            }

            try
            {
                harmony.Patch(initializeUpgrades, transpiler: Hook("InitializeUpgradesTranspiler"));
                harmony.Patch(upgradeHint, transpiler: Hook("UpgradeHintTranspiler"));
                harmony.Patch(upgradeTargetCtor, postfix: Hook("UpgradeTargetPostfix"));
                harmony.Patch(upgradeTargetRefresh, postfix: Hook("UpgradeTargetPostfix"));
                harmony.Patch(validateCommand, transpiler: Hook("ValidateCommandTranspiler"), postfix: Hook("ValidateCommandPostfix"));
                harmony.Patch(upgradeTroop, prefix: Hook("UpgradeTroopPrefix"), finalizer: Hook("UpgradeTroopFinalizer"));
                harmony.Patch(removeItems, prefix: Hook("RemoveItemsPrefix"));
                harmony.Patch(hasRequiredItems, postfix: Hook("HasRequiredItemsPostfix"));
            }
            catch (Exception exception)
            {
                CamelLog.Print("patching failed, so every Camel Cavalry patch was removed again: " + exception);
                try
                {
                    harmony.UnpatchAll(harmony.Id);
                }
                catch (Exception unpatchException)
                {
                    CamelLog.Print("removing the partial patches also failed: " + unpatchException.Message);
                }

                return false;
            }

            return true;
        }

        private static T Require<T>(T member, string name, List<string> missing) where T : class
        {
            if (member == null)
            {
                missing.Add(name);
            }

            return member;
        }

        private static HarmonyMethod Hook(string name)
        {
            MethodInfo method = AccessTools.DeclaredMethod(typeof(CamelUpgradePatches), name);
            if (method == null)
            {
                throw new MissingMethodException(typeof(CamelUpgradePatches).Name, name);
            }

            return new HarmonyMethod(method);
        }

        // Inserts the upgrade target in front of a call whose stack is "..., ldloc target, get_UpgradeRequiresItemFromCategory"
        // and swaps the call for a helper. Exactly one match is required; anything else means the game code changed.
        private static IEnumerable<CodeInstruction> ReplaceCategoryCall(IEnumerable<CodeInstruction> instructions, Func<MethodInfo, bool> isCall, MethodInfo helper, string where)
        {
            if (helper == null)
            {
                throw new InvalidOperationException(where + " has no replacement helper");
            }

            List<CodeInstruction> codes = instructions.ToList();
            int matches = 0;
            for (int i = 2; i < codes.Count; i++)
            {
                MethodInfo called = codes[i].operand as MethodInfo;
                if (called == null || called.ReturnType != helper.ReturnType || !isCall(called) || !codes[i - 1].Calls(_upgradeRequiresGetter) || !codes[i - 2].IsLdloc())
                {
                    continue;
                }

                CodeInstruction loadTarget = new CodeInstruction(codes[i - 2].opcode, codes[i - 2].operand);
                loadTarget.labels.AddRange(codes[i].labels);
                loadTarget.blocks.AddRange(codes[i].blocks);
                codes[i] = new CodeInstruction(OpCodes.Call, helper);
                codes.Insert(i, loadTarget);
                matches++;
                i++;
            }

            if (matches != 1)
            {
                throw new InvalidOperationException(where + " matched " + matches + " places instead of 1");
            }

            return codes;
        }

        private static IEnumerable<CodeInstruction> InitializeUpgradesTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            return ReplaceCategoryCall(instructions,
                called => called.Name == "GetNumOfCategoryItemPartyHas" && called.DeclaringType == typeof(PartyCharacterVM),
                AccessTools.DeclaredMethod(typeof(CamelUpgradePatches), "CountForUpgrade"),
                "PartyCharacterVM.InitializeUpgrades");
        }

        private static IEnumerable<CodeInstruction> UpgradeHintTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            return ReplaceCategoryCall(instructions,
                called => called.Name == "GetName" && called.GetParameters().Length == 0,
                AccessTools.DeclaredMethod(typeof(CamelUpgradePatches), "RequirementName"),
                "CampaignUIHelper.GetUpgradeHint");
        }

        // Vanilla's own "item needed" message, shown when the party has no item of the category at all, names the camel too.
        private static IEnumerable<CodeInstruction> ValidateCommandTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            return ReplaceCategoryCall(instructions,
                called => called.Name == "GetName" && called.GetParameters().Length == 0,
                AccessTools.DeclaredMethod(typeof(CamelUpgradePatches), "RequirementName"),
                "PartyScreenLogic.ValidateCommand");
        }

        public static int CountForUpgrade(PartyCharacterVM troop, ItemRoster roster, ItemCategory category, CharacterObject upgradeTarget)
        {
            string mountId = CamelMountRules.RequiredMountId(upgradeTarget);
            return mountId != null ? CamelMountRules.CountExact(roster, mountId) : troop.GetNumOfCategoryItemPartyHas(roster, category);
        }

        public static TextObject RequirementName(ItemCategory category, CharacterObject upgradeTarget)
        {
            string mountId = CamelMountRules.RequiredMountId(upgradeTarget);
            return mountId != null ? CamelMountRules.MountName(mountId) : category.GetName();
        }

        private static void UpgradeTargetPostfix(UpgradeTargetVM __instance)
        {
            try
            {
                string mountId = CamelMountRules.RequiredMountId(_upgradeTargetField.GetValue(__instance) as CharacterObject);
                if (mountId == null || __instance.Requirements == null)
                {
                    return;
                }

                TextObject text = new TextObject("{=Q0j1umAt}Requirement: {REQUIREMENT_NAME}");
                text.SetTextVariable("REQUIREMENT_NAME", CamelMountRules.MountName(mountId).ToString());
                __instance.Requirements.ItemRequirementHint = new HintViewModel(text);
            }
            catch (Exception exception)
            {
                CamelLog.Print("could not show the camel requirement: " + exception.Message);
            }
        }

        private static void ValidateCommandPostfix(PartyScreenLogic __instance, PartyScreenLogic.PartyCommand command, ref bool __result)
        {
            if (!__result || command == null || command.Code != PartyScreenLogic.PartyCommandCode.UpgradeTroop)
            {
                return;
            }

            string mountId = CamelMountRules.RequiredMountId(TargetOf(command));
            if (mountId == null)
            {
                return;
            }

            ItemRoster roster = __instance.RightOwnerParty != null ? __instance.RightOwnerParty.ItemRoster : null;
            if (CamelMountRules.CountExact(roster, mountId) >= command.TotalNumber)
            {
                return;
            }

            __result = false;
            MBTextManager.SetTextVariable("REQUIRED_ITEM", CamelMountRules.MountName(mountId));
            MBInformationManager.AddQuickInformation(GameTexts.FindText("str_item_needed_for_upgrade"));
        }

        private static void UpgradeTroopPrefix(PartyScreenLogic.PartyCommand command)
        {
            _consumingMountId = command != null ? CamelMountRules.RequiredMountId(TargetOf(command)) : null;
        }

        private static Exception UpgradeTroopFinalizer(Exception __exception)
        {
            _consumingMountId = null;
            return __exception;
        }

        // Same order as vanilla (unlocked before locked, then cheapest first), restricted to the exact mount.
        private static bool RemoveItemsPrefix(PartyScreenLogic __instance, ItemCategory itemCategory, int numOfItemsLeftToRemove, ref List<ValueTuple<EquipmentElement, int>> __result)
        {
            string mountId = _consumingMountId;
            if (mountId == null || __instance.RightOwnerParty == null)
            {
                return true;
            }

            ItemRoster roster = __instance.RightOwnerParty.ItemRoster;
            HashSet<string> locked = new HashSet<string>();
            IViewDataTracker tracker = Campaign.Current != null ? Campaign.Current.GetCampaignBehavior<IViewDataTracker>() : null;
            IEnumerable<string> locks = tracker != null ? tracker.GetInventoryLocks() : null;
            if (locks != null)
            {
                locked.UnionWith(locks);
            }

            List<ItemRosterElement> candidates = roster
                .Where(x => x.EquipmentElement.Item != null && x.EquipmentElement.Item.StringId == mountId && x.Amount > 0)
                .OrderBy(x => locked.Contains(LockId(x.EquipmentElement)))
                .ThenBy(x => x.EquipmentElement.Item.Value)
                .ToList();

            List<ValueTuple<EquipmentElement, int>> used = new List<ValueTuple<EquipmentElement, int>>();
            foreach (ItemRosterElement element in candidates)
            {
                int take = Math.Min(numOfItemsLeftToRemove, element.Amount);
                roster.AddToCounts(element.EquipmentElement, -take);
                numOfItemsLeftToRemove -= take;
                used.Add(new ValueTuple<EquipmentElement, int>(element.EquipmentElement, take));
                if (numOfItemsLeftToRemove <= 0)
                {
                    break;
                }
            }

            if (numOfItemsLeftToRemove > 0)
            {
                CamelLog.Print("an upgrade needed " + numOfItemsLeftToRemove + " more " + mountId + " than the party had.");
            }

            __result = used;
            return false;
        }

        private static void HasRequiredItemsPostfix(PartyBase party, CharacterObject upgradeTarget, ref bool __result)
        {
            string mountId = CamelMountRules.RequiredMountId(upgradeTarget);
            if (mountId != null)
            {
                __result = party != null && CamelMountRules.CountExact(party.ItemRoster, mountId) > 0;
            }
        }

        private static CharacterObject TargetOf(PartyScreenLogic.PartyCommand command)
        {
            CharacterObject source = command.Character;
            if (source == null || source.UpgradeTargets == null || command.UpgradeTarget < 0 || command.UpgradeTarget >= source.UpgradeTargets.Length)
            {
                return null;
            }

            return source.UpgradeTargets[command.UpgradeTarget];
        }

        private static string LockId(EquipmentElement element)
        {
            return element.Item.StringId + (element.ItemModifier != null ? element.ItemModifier.StringId : "");
        }
    }
}

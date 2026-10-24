using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace CamelCavalry
{
    public class CamelCavalrySubModule : MBSubModuleBase
    {
        private readonly Harmony _harmony = new Harmony("camel.cavalry");
        private readonly Harmony _villageHarmony = new Harmony("camel.cavalry.villages");
        private readonly Harmony _marketHarmony = new Harmony("camel.cavalry.markets");

        protected override void OnSubModuleLoad()
        {
            try
            {
                CamelMountRules.Active = CamelUpgradePatches.Apply(_harmony);
            }
            catch (System.Exception exception)
            {
                CamelMountRules.Active = false;
                CamelLog.Print("could not start: " + exception);
                Unpatch(_harmony);
            }

            CamelLog.Print(CamelMountRules.Active ? "exact camel and war camel upgrade rules are active." : "exact camel rules are OFF; camel troops will accept any horse or war horse.");

            try
            {
                CamelVillageProduction.Active = CamelVillageProduction.Apply(_villageHarmony);
            }
            catch (System.Exception exception)
            {
                CamelVillageProduction.Active = false;
                CamelLog.Print("could not set up Khuzait camel breeding: " + exception.Message);
                Unpatch(_villageHarmony);
            }

            CamelLog.Print(CamelVillageProduction.Active ? "Khuzait horse ranches also breed camels." : "Khuzait horse ranches will NOT breed camels.");

            try
            {
                CamelKhuzaitMarket.Active = CamelKhuzaitMarket.Apply(_marketHarmony);
            }
            catch (System.Exception exception)
            {
                CamelKhuzaitMarket.Active = false;
                CamelLog.Print("could not let Khuzait towns make camel armor: " + exception.Message);
                Unpatch(_marketHarmony);
            }

            CamelLog.Print(CamelKhuzaitMarket.Active ? "Khuzait town workshops can also make camel armor." : "Khuzait town workshops will NOT make camel armor.");
        }

        private static void Unpatch(Harmony harmony)
        {
            try
            {
                harmony.UnpatchAll(harmony.Id);
            }
            catch (System.Exception unpatchException)
            {
                CamelLog.Print("removing the partial patches also failed: " + unpatchException.Message);
            }
        }

        protected override void OnGameStart(Game game, IGameStarter gameStarter)
        {
            CampaignGameStarter starter = gameStarter as CampaignGameStarter;
            if (game.GameType is Campaign && starter != null)
            {
                starter.AddBehavior(new CamelCavalryBehavior());
            }
        }

        public override void OnGameInitializationFinished(Game game)
        {
            if (!(game.GameType is Campaign))
            {
                return;
            }

            try
            {
                CamelDiagnostics.Report();
            }
            catch (System.Exception exception)
            {
                CamelLog.Print("startup check failed: " + exception.Message);
            }
        }
    }
}

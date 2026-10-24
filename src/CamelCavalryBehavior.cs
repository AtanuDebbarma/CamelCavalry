using TaleWorlds.CampaignSystem;

namespace CamelCavalry
{
    // Logs the AI camel troop counts once a week and runs the opt-in removal cleanup; it stores nothing in the save.
    internal class CamelCavalryBehavior : CampaignBehaviorBase
    {
        public override void RegisterEvents()
        {
            CampaignEvents.WeeklyTickEvent.AddNonSerializedListener(this, OnWeeklyTick);
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
            CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, OnDailyTick);
            CampaignEvents.OnBeforeSaveEvent.AddNonSerializedListener(this, OnBeforeSave);
        }

        public override void SyncData(IDataStore dataStore)
        {
        }

        private void OnWeeklyTick()
        {
            try
            {
                CamelDiagnostics.ReportAiTroops();
            }
            catch (System.Exception exception)
            {
                CamelLog.Print("weekly troop count failed: " + exception.Message);
            }
        }

        private void OnSessionLaunched(CampaignGameStarter starter)
        {
            RunRemovalCleanup(true);
        }

        // AI parties keep upgrading into camel troops while the mod is loaded, so the cleanup repeats until the save is made.
        private void OnDailyTick()
        {
            RunRemovalCleanup(false);
        }

        private void OnBeforeSave()
        {
            RunRemovalCleanup(false);
        }

        private static void RunRemovalCleanup(bool announce)
        {
            try
            {
                if (CamelRemovalCleanup.Requested)
                {
                    CamelRemovalCleanup.Run(announce);
                }
            }
            catch (System.Exception exception)
            {
                CamelLog.Print("removal cleanup failed: " + exception);
            }
        }
    }
}

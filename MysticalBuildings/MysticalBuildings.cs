using MysticalBuildings.Framework.Interfaces;
using MysticalBuildings.Framework.Managers;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using System;

namespace MysticalBuildings
{
    public class MysticalBuildings : Mod
    {
        // Shared static helpers
        internal static IMonitor monitor;
        internal static IModHelper modHelper;

        // Managers
        internal static ApiManager apiManager;

        public override void Entry(IModHelper helper)
        {
            // Set up the monitor, helper and multiplayer
            monitor = Monitor;
            modHelper = helper;

            // Set up the managers
            apiManager = new ApiManager(monitor);

            // Hook into required events
            helper.Events.GameLoop.GameLaunched += OnGameLaunched;
        }

        private void OnGameLaunched(object sender, GameLaunchedEventArgs e)
        {
            // Hook into the APIs we utilize
            if (Helper.ModRegistry.IsLoaded("PeacefulEnd.SolidFoundations") && apiManager.HookIntoSolidFoundations(Helper))
            {
                var solidFoundationsApi = apiManager.GetSolidFoundationsApi();
                solidFoundationsApi.BroadcastSpecialActionTriggered += OnBroadcastSpecialActionTriggered;
            }
        }

        private void OnBroadcastSpecialActionTriggered(object sender, ISolidFoundationsApi.BroadcastEventArgs e)
        {
            Monitor.Log("HERE", LogLevel.Debug);
        }
    }
}

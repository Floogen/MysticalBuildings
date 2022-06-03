using MysticalBuildings.Framework.Managers;
using SolidFoundations.Framework.Interfaces.Internal;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.Menus;
using System;
using System.Collections.Generic;

namespace MysticalBuildings
{
    public class MysticalBuildings : Mod
    {
        // Shared static helpers
        internal static IMonitor monitor;
        internal static IModHelper modHelper;
        internal static ITranslationHelper i18n;

        // Managers
        internal static ApiManager apiManager;

        public override void Entry(IModHelper helper)
        {
            // Set up the monitor, helper and multiplayer
            monitor = Monitor;
            modHelper = helper;
            i18n = helper.Translation;

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

        private void OnBroadcastSpecialActionTriggered(object sender, IApi.BroadcastEventArgs e)
        {
            if (e.BuildingId != "PeacefulEnd.SolidFoundations.MysticalBuildings_StatueofGreed" || e.Farmer.ActiveObject is null)
            {
                return;
            }
            var item = e.Farmer.ActiveObject;
            e.Farmer.currentLocation.createQuestionDialogue(String.Format(i18n.Get("Greed.Question.Confirmation"), item.Name), e.Farmer.currentLocation.createYesNoResponses(), new GameLocation.afterQuestionBehavior((who, whichAnswer) => HandleStatueOfGreed(e.Building, who, whichAnswer, item)));
        }

        private void HandleStatueOfGreed(Building building, Farmer who, string whichAnswer, Item item)
        {
            if (whichAnswer == "No")
            {
                return;
            }
            var solidFoundationsApi = apiManager.GetSolidFoundationsApi();

            double targetChance = new Random((int)((long)Game1.uniqueIDForThisGame + who.DailyLuck + Game1.stats.DaysPlayed * 500 + Game1.ticks)).NextDouble();
            double modifier = 1.0 + who.DailyLuck * 2.0 + who.LuckLevel * 0.08;

            who.removeItemFromInventory(item);
            if (targetChance < 0.4 * modifier)
            {
                // Double the item
                int stackSize = item.Stack * 2;
                List<Item> itemsToAdd = new List<Item>() { item };
                if (stackSize > item.maximumStackSize())
                {
                    var secondaryItem = item.getOne();
                    secondaryItem.Stack = item.maximumStackSize() - stackSize;
                    itemsToAdd.Add(secondaryItem);

                    stackSize = item.maximumStackSize();
                }
                item.Stack = stackSize;

                who.addItemsByMenuIfNecessary(itemsToAdd);

                if (Game1.activeClickableMenu is null)
                {
                    Game1.activeClickableMenu = new DialogueBox(String.Format(i18n.Get("Greed.Response.Reward"), item.Name));
                }
            }
            else
            {
                solidFoundationsApi.AddBuildingFlags(building, new List<string>() { "IsEating" }, true);
                Game1.activeClickableMenu = new DialogueBox(i18n.Get("Greed.Response.Hungry"));
            }
        }
    }
}

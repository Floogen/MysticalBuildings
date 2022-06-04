using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MysticalBuildings.Framework.GameLocations;
using MysticalBuildings.Framework.Managers;
using SolidFoundations.Framework.Interfaces.Internal;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.BellsAndWhistles;
using StardewValley.Buildings;
using StardewValley.Locations;
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

        // Functional variables
        internal static int shakeTimer = 0;
        internal static int cavernTimer = 0;

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
            helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
            helper.Events.Player.Warped += OnWarped;
            helper.Events.Display.RenderingWorld += OnRenderingWorld;
            helper.Events.Display.RenderedHud += OnRenderedHud;
        }

        private void OnWarped(object sender, WarpedEventArgs e)
        {
            if (e.OldLocation is UnstableCavern unstableCavern && unstableCavern is not null)
            {
                Game1.locations.Remove(e.OldLocation);
            }
        }

        internal static Random GenerateRandom(Farmer who = null)
        {
            if (who is not null)
            {
                return new Random((int)((long)Game1.uniqueIDForThisGame + who.DailyLuck + Game1.stats.DaysPlayed * 500 + Game1.ticks));
            }
            return new Random((int)((long)Game1.uniqueIDForThisGame + Game1.stats.DaysPlayed * 500 + Game1.ticks));
        }

        private void OnRenderedHud(object sender, RenderedHudEventArgs e)
        {
            if (Context.IsWorldReady is false || Game1.activeClickableMenu is not null)
            {
                return;
            }

            if (cavernTimer >= 0 && Game1.player.currentLocation is UnstableCavern)
            {
                int color = 2;
                if (cavernTimer >= 40)
                {
                    color = 7;
                }
                else if (cavernTimer >= 30)
                {
                    color = 6;
                }
                else if (cavernTimer >= 20)
                {
                    color = 4;
                }
                else if (cavernTimer >= 10)
                {
                    color = 3;
                }

                Rectangle tsarea = Game1.game1.GraphicsDevice.Viewport.GetTitleSafeArea();
                SpriteText.drawString(e.SpriteBatch, cavernTimer.ToString(), tsarea.Left + 16, tsarea.Top + 16, 999999, -1, 999999, 1f, 1f, junimoText: false, 2, "", color);
            }
        }

        private void OnRenderingWorld(object sender, RenderingWorldEventArgs e)
        {
            if (Context.IsWorldReady is false || Game1.activeClickableMenu is not null)
            {
                return;
            }

            if (shakeTimer > 0)
            {
                var offset = new Vector2(Game1.random.Next(-2, 2), Game1.random.Next(-2, 2));
                Game1.viewport.X += (int)offset.X;
                Game1.viewport.Y += (int)offset.Y;
            }
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

        private void OnSaveLoaded(object sender, SaveLoadedEventArgs e)
        {
            shakeTimer = 0;
            cavernTimer = 0;
        }

        private void OnBroadcastSpecialActionTriggered(object sender, IApi.BroadcastEventArgs e)
        {
            if (e.BuildingId == "PeacefulEnd.SolidFoundations.MysticalBuildings_StatueofGreed")
            {
                if (e.Farmer.ActiveObject is null)
                {
                    return;
                }

                var item = e.Farmer.ActiveObject;
                e.Farmer.currentLocation.createQuestionDialogue(String.Format(i18n.Get("Greed.Question.Confirmation"), item.Name), e.Farmer.currentLocation.createYesNoResponses(), new GameLocation.afterQuestionBehavior((who, whichAnswer) => HandleStatueOfGreed(e.Building, who, whichAnswer, item)));
            }
            else if (e.BuildingId == "PeacefulEnd.SolidFoundations.MysticalBuildings_CrumblingMineshaft")
            {
                HandleCrumblingMineshaft(e.Building, e.Farmer);
            }
        }

        private void HandleStatueOfGreed(Building building, Farmer who, string whichAnswer, Item item)
        {
            if (whichAnswer == "No")
            {
                return;
            }
            var solidFoundationsApi = apiManager.GetSolidFoundationsApi();

            double targetChance = GenerateRandom(who).NextDouble();
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

        private void HandleCrumblingMineshaft(Building building, Farmer who)
        {
            var exitTile = new Point(building.tileX.Value + 2, building.tileY.Value + 3);
            UnstableCavern unstableCavern = new UnstableCavern(who.currentLocation, exitTile);
            Game1.locations.Add(unstableCavern);

            var warpTile = unstableCavern.tileBeneathLadder;
            Game1.warpFarmer(unstableCavern.NameOrUniqueName, (int)warpTile.X, (int)warpTile.Y, 2);
        }
    }
}

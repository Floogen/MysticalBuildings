﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MysticalBuildings.Framework.GameLocations;
using MysticalBuildings.Framework.Managers;
using MysticalBuildings.Framework.Models;
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
using System.Linq;

namespace MysticalBuildings
{
    public class MysticalBuildings : Mod
    {
        // Shared static helpers
        internal static IMonitor monitor;
        internal static IModHelper modHelper;
        internal static ITranslationHelper i18n;
        internal static ModConfig modConfig;

        // Managers
        internal static ApiManager apiManager;

        // Functional variables
        internal static int shakeTimer = 0;
        internal static int cavernTimer = 0;

        private const string CRUMBLING_MINESHAFT_ID = "PeacefulEnd.SolidFoundations.MysticalBuildings_CrumblingMineshaft";
        private const string STATUE_OF_GREED_ID = "PeacefulEnd.SolidFoundations.MysticalBuildings_StatueofGreed";
        private const string QUIZZICAL_STATUE_ID = "PeacefulEnd.SolidFoundations.MysticalBuildings_QuizzicalStatue";
        private static List<string> _targetBuildingID = new List<string>()
        {
            CRUMBLING_MINESHAFT_ID,
            STATUE_OF_GREED_ID,
            QUIZZICAL_STATUE_ID
        };

        private const string REFRESH_DAYS_REMAINING = "PeacefulEnd.MysticalBuildings.RefreshDaysRemaining";
        private const string HAS_ENTERED_FLAG = "HasEntered";
        private const string ATTEMPTED_TEST_FLAG = "AttemptedTest";
        private const string IS_EATING_FLAG = "IsEating";
        private const string QUERY_COOLDOWN_MESSAGE = "QueryCooldown";


        public override void Entry(IModHelper helper)
        {
            // Set up the monitor, helper and multiplayer
            monitor = Monitor;
            modHelper = helper;
            i18n = helper.Translation;
            modConfig = helper.ReadConfig<ModConfig>();

            // Set up the managers
            apiManager = new ApiManager(monitor);

            // Hook into required events
            helper.Events.GameLoop.GameLaunched += OnGameLaunched;
            helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
            helper.Events.GameLoop.DayStarted += OnDayStarted;
            helper.Events.Player.Warped += OnWarped;
            helper.Events.Display.RenderingWorld += OnRenderingWorld;
            helper.Events.Display.RenderedHud += OnRenderedHud;
        }

        private void OnDayStarted(object sender, DayStartedEventArgs e)
        {
            var solidFoundationsApi = apiManager.GetSolidFoundationsApi();
            foreach (BuildableGameLocation buildableGameLocation in Game1.locations.Where(b => b is BuildableGameLocation))
            {
                foreach (var building in buildableGameLocation.buildings.Where(b => _targetBuildingID.Contains(b.buildingType.Value)))
                {
                    int actualDaysRemaining = 0;
                    var rawDaysRemaining = building.modData.ContainsKey(REFRESH_DAYS_REMAINING) is true ? building.modData[REFRESH_DAYS_REMAINING] : null;
                    if (rawDaysRemaining is not null && int.TryParse(rawDaysRemaining, out actualDaysRemaining) is false)
                    {
                        actualDaysRemaining = 0;
                    }

                    if (String.IsNullOrEmpty(rawDaysRemaining))
                    {
                        actualDaysRemaining = GetActualDaysRemaining(solidFoundationsApi, building);
                    }

                    switch (building.buildingType.Value)
                    {
                        case CRUMBLING_MINESHAFT_ID:
                            if (solidFoundationsApi.DoesBuildingHaveFlag(building, HAS_ENTERED_FLAG) && actualDaysRemaining - 1 <= 0)
                            {
                                solidFoundationsApi.RemoveBuildingFlags(building, new List<string>() { HAS_ENTERED_FLAG });
                                building.modData[REFRESH_DAYS_REMAINING] = null;
                                continue;
                            }
                            break;
                        case STATUE_OF_GREED_ID:
                            if (solidFoundationsApi.DoesBuildingHaveFlag(building, IS_EATING_FLAG) && actualDaysRemaining - 1 <= 0)
                            {
                                solidFoundationsApi.RemoveBuildingFlags(building, new List<string>() { IS_EATING_FLAG });
                                building.modData[REFRESH_DAYS_REMAINING] = null;
                                continue;
                            }
                            break;
                        case QUIZZICAL_STATUE_ID:
                            if (solidFoundationsApi.DoesBuildingHaveFlag(building, ATTEMPTED_TEST_FLAG) && actualDaysRemaining - 1 <= 0)
                            {
                                solidFoundationsApi.RemoveBuildingFlags(building, new List<string>() { ATTEMPTED_TEST_FLAG });
                                building.modData[REFRESH_DAYS_REMAINING] = null;
                                continue;
                            }
                            break;
                    }

                    if (actualDaysRemaining - 1 >= 0)
                    {
                        building.modData[REFRESH_DAYS_REMAINING] = (actualDaysRemaining - 1).ToString();
                    }
                }
            }
        }

        private void OnWarped(object sender, WarpedEventArgs e)
        {
            if (e.OldLocation is UnstableCavern unstableCavern && unstableCavern is not null)
            {
                Game1.locations.Remove(e.OldLocation);
                shakeTimer = 0;
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
            if (Helper.ModRegistry.IsLoaded("spacechase0.GenericModConfigMenu") && apiManager.HookIntoGMCM(Helper))
            {
                var configApi = apiManager.GetGMCMApi();
                configApi.Register(ModManifest, () => modConfig = new ModConfig(), () => Helper.WriteConfig(modConfig));
                configApi.AddNumberOption(this.ModManifest, () => modConfig.CrumblingMineshaftRefreshInDays, value => modConfig.CrumblingMineshaftRefreshInDays = value, () => "Crumbling Mineshaft Refresh (in days)", min: 1, max: 14, interval: 1);
                configApi.AddNumberOption(this.ModManifest, () => modConfig.StatueOfGreedRefreshInDays, value => modConfig.StatueOfGreedRefreshInDays = value, () => "Statue of Greed Refresh (in days)", min: 1, max: 14, interval: 1);
                configApi.AddNumberOption(this.ModManifest, () => modConfig.QuizzicalStatueRefreshInDays, value => modConfig.QuizzicalStatueRefreshInDays = value, () => "Quizzical Statue Refresh (in days)", min: 1, max: 14, interval: 1);
            }
        }

        private void OnSaveLoaded(object sender, SaveLoadedEventArgs e)
        {
            shakeTimer = 0;
            cavernTimer = 0;
        }

        private void OnBroadcastSpecialActionTriggered(object sender, IApi.BroadcastEventArgs e)
        {
            var solidFoundationsApi = apiManager.GetSolidFoundationsApi();
            if (e.Message.Equals(QUERY_COOLDOWN_MESSAGE, StringComparison.OrdinalIgnoreCase))
            {
                var rawDaysRemaining = e.Building.modData.ContainsKey(REFRESH_DAYS_REMAINING) is true ? e.Building.modData[REFRESH_DAYS_REMAINING] : null;
                if (rawDaysRemaining is null)
                {
                    rawDaysRemaining = GetActualDaysRemaining(solidFoundationsApi, e.Building).ToString();
                }

                switch (e.Building.buildingType.Value)
                {
                    case CRUMBLING_MINESHAFT_ID:
                        Game1.activeClickableMenu = new DialogueBox(rawDaysRemaining == "1" ? i18n.Get("Mine.Response.AlreadyEntered") : String.Format(i18n.Get("Mine.Response.AlreadyEntered.DaysLeft"), rawDaysRemaining));
                        break;
                    case STATUE_OF_GREED_ID:
                        Game1.activeClickableMenu = new DialogueBox(rawDaysRemaining == "1" ? i18n.Get("Greed.Dialogue.Full") : String.Format(i18n.Get("Greed.Dialogue.Full.DaysLeft"), rawDaysRemaining));
                        break;
                    case QUIZZICAL_STATUE_ID:
                        Game1.activeClickableMenu = new DialogueBox(rawDaysRemaining == "1" ? i18n.Get("Quiz.Response.AlreadyTested") : String.Format(i18n.Get("Quiz.Response.AlreadyTested.DaysLeft"), rawDaysRemaining));
                        break;
                }
                return;
            }

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
                solidFoundationsApi.AddBuildingFlags(building, new List<string>() { IS_EATING_FLAG }, isTemporary: false);
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

        private int GetActualDaysRemaining(IApi solidFoundationsApi, Building building)
        {
            switch (building.buildingType.Value)
            {
                case CRUMBLING_MINESHAFT_ID:
                    if (solidFoundationsApi.DoesBuildingHaveFlag(building, HAS_ENTERED_FLAG))
                    {
                        return modConfig.CrumblingMineshaftRefreshInDays;
                    }
                    break;
                case STATUE_OF_GREED_ID:
                    if (solidFoundationsApi.DoesBuildingHaveFlag(building, IS_EATING_FLAG))
                    {
                        return modConfig.StatueOfGreedRefreshInDays;
                    }
                    break;
                case QUIZZICAL_STATUE_ID:
                    if (solidFoundationsApi.DoesBuildingHaveFlag(building, ATTEMPTED_TEST_FLAG))
                    {
                        return modConfig.QuizzicalStatueRefreshInDays;
                    }
                    break;
            }

            return 0;
        }
    }
}

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using CaveOfMemories.Framework.GameLocations;
using CaveOfMemories.Framework.Managers;
using CaveOfMemories.Framework.Models;
using SolidFoundations.Framework.Interfaces.Internal;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.BellsAndWhistles;
using StardewValley.Buildings;
using StardewValley.Locations;
using StardewValley.Menus;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CaveOfMemories
{
    public class CaveOfMemories : Mod
    {
        // Shared static helpers
        internal static IMonitor monitor;
        internal static IModHelper modHelper;
        internal static ITranslationHelper i18n;

        // Managers
        internal static ApiManager apiManager;
        internal static AssetManager assetManager;

        private const string CAVE_OF_MEMORIES_ID = "PeacefulEnd.SolidFoundations.MysticalBuildings_CaveOfMemories";

        public override void Entry(IModHelper helper)
        {
            // Set up the monitor, helper and multiplayer
            monitor = Monitor;
            modHelper = helper;
            i18n = helper.Translation;

            // Set up the managers
            apiManager = new ApiManager(monitor);
            assetManager = new AssetManager(monitor, helper);

            // Hook into required events
            helper.Events.GameLoop.GameLaunched += OnGameLaunched;
            helper.Events.GameLoop.DayStarted += OnDayStarted;
            helper.Events.GameLoop.DayEnding += OnDayEnding;
            helper.Events.Player.Warped += OnWarped;
        }

        private void OnDayStarted(object sender, DayStartedEventArgs e)
        {
            var solidFoundationsApi = apiManager.GetSolidFoundationsApi();
            foreach (BuildableGameLocation buildableGameLocation in Game1.locations.Where(b => b is BuildableGameLocation))
            {
                foreach (var building in buildableGameLocation.buildings.Where(b => CAVE_OF_MEMORIES_ID == b.buildingType.Value))
                {
                    if (Game1.locations.Any(l => l.Name == "CaveOfMemories") is false)
                    {
                        var exitTile = new Point(building.tileX.Value + 2, building.tileY.Value + 3);
                        CaveOfMemoriesLocation caveOfMemories = new CaveOfMemoriesLocation(buildableGameLocation, exitTile);
                        Game1.locations.Add(caveOfMemories);
                    }

                    return;
                }
            }
        }

        private void OnDayEnding(object sender, DayEndingEventArgs e)
        {
            if (Game1.locations.Any(l => l.Name == "CaveOfMemories") is false)
            {
                foreach (var location in Game1.locations.Where(l => l is CaveOfMemoriesLocation).ToList())
                {
                    Game1.locations.Remove(location);
                }
            }
        }

        private void OnWarped(object sender, WarpedEventArgs e)
        {
            if (e.NewLocation is CaveOfMemoriesLocation caveOfMemories && caveOfMemories is not null && e.OldLocation is not Farm)
            {
                e.Player.setTileLocation(new Vector2(6, 6));
                e.Player.FacingDirection = 0;
            }
        }

        internal static Random GenerateRandom(Farmer who = null)
        {
            if (who is not null)
            {
                return new Random((int)((long)Game1.uniqueIDForThisGame + who.DailyLuck + Game1.stats.DaysPlayed * 500 + Game1.ticks + DateTime.Now.Ticks));
            }
            return new Random((int)((long)Game1.uniqueIDForThisGame + Game1.stats.DaysPlayed * 500 + Game1.ticks + DateTime.Now.Ticks));
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
            var solidFoundationsApi = apiManager.GetSolidFoundationsApi();

            if (e.BuildingId == "PeacefulEnd.SolidFoundations.MysticalBuildings_CaveOfMemories")
            {
                HandleCaveOfMemories(e.Building, e.Farmer);
            }
        }

        private void HandleCaveOfMemories(Building building, Farmer who)
        {
            Game1.warpFarmer("CaveOfMemories", 6, 14, 0);
        }
    }
}

﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using CaveOfMemories.Framework.Models;
using CaveOfMemories.Framework.UI;
using StardewValley;
using StardewValley.Menus;
using StardewValley.Objects;
using StardewValley.TerrainFeatures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace CaveOfMemories.Framework.GameLocations
{
    public class CaveOfMemoriesLocation : GameLocation
    {
        private Farmer _fakeFarmer;
        private List<EventFragment> _eventFragments;

        // Mirror related
        private int _mirrorAlpha = 200;
        private bool _fadeMirror = false;
        private double _elapsedMillisecondsTime = 0;
        private static readonly Point _mirrorTileBase = new Point(6, 10);
        private static readonly Vector2 _mirrorPosition = new Vector2((_mirrorTileBase.X - 1) * 64, (_mirrorTileBase.Y - 4) * 64);

        // Regex patterns
        private readonly string ID_PATTERN = @"(?<id>[^ \/]+).*";
        private readonly string FRIENDSHIP_PATTERN = @"\/f (?<npc>[^ ]+) (?<requiredHearts>[0-9]+).*";
        private readonly string TIMEFRAME_PATTERN = @"\/t (?<startTime>[0-9]+).*";
        private readonly string WEATHER_PATTERN = @"\/w (?<weather>[^ \/]+).*";

        // Regex group names
        private readonly string GROUP_ID = "id";
        private readonly string GROUP_NPC = "npc";
        private readonly string GROUP_REQUIRED_HEARTS = "requiredHearts";
        private readonly string GROUP_START_TIME = "startTime";
        private readonly string GROUP_WEATHER = "weather";

        public CaveOfMemoriesLocation(GameLocation exitLocation, Point exitTile) : base("Maps\\" + $"cave_of_memories", "CaveOfMemories")
        {
            this.IsOutdoors = false;
            this.LightLevel = 0.1f;

            if (this.warps.FirstOrDefault() is Warp warp && warp is not null)
            {
                warp.TargetName = exitLocation.NameOrUniqueName;
                warp.TargetX = exitTile.X;
                warp.TargetY = exitTile.Y;
            }

            RefreshEventFragments();
        }

        internal void RefreshEventFragments()
        {
            _eventFragments = new List<EventFragment>();

            var idRegex = new Regex(ID_PATTERN);
            var friendshipRegex = new Regex(FRIENDSHIP_PATTERN);
            var timeframeRegex = new Regex(TIMEFRAME_PATTERN);
            var weatherRegex = new Regex(WEATHER_PATTERN);
            foreach (var location in Game1.locations)
            {
                try
                {
                    var events = location.GetLocationEvents();
                    foreach (var eventKey in events.Keys)
                    {
                        string id = null;
                        string npcName = null;
                        string rawRequiredHearts = null;
                        string startTime = null;
                        string weather = null;

                        // Get ID_PATTERN value
                        var idMatch = idRegex.Match(eventKey);
                        if (idMatch.Success && idMatch.Groups.Count > 0)
                        {
                            id = idMatch.Groups[GROUP_ID].Value;
                        }

                        // Get FRIENDSHIP_PATTERN values
                        var friendshipMatch = friendshipRegex.Match(eventKey);
                        if (friendshipMatch.Success && friendshipMatch.Groups.Count > 0)
                        {
                            npcName = friendshipMatch.Groups[GROUP_NPC].Value;
                            rawRequiredHearts = friendshipMatch.Groups[GROUP_REQUIRED_HEARTS].Value;
                        }

                        // Get TIMEFRAME_PATTERN values
                        var timeframeMatch = timeframeRegex.Match(eventKey);
                        if (timeframeMatch.Success && timeframeMatch.Groups.Count > 0)
                        {
                            startTime = timeframeMatch.Groups[GROUP_START_TIME].Value;
                        }

                        // Get TIMEFRAME_PATTERN values
                        var weatherMatch = weatherRegex.Match(eventKey);
                        if (weatherMatch.Success && weatherMatch.Groups.Count > 0)
                        {
                            weather = weatherMatch.Groups[GROUP_WEATHER].Value;
                        }

                        if (String.IsNullOrEmpty(id) is false && String.IsNullOrEmpty(npcName) is false && String.IsNullOrEmpty(rawRequiredHearts) is false)
                        {
                            int hearts = int.Parse(rawRequiredHearts);
                            var eventName = $"{hearts / 250} Heart Event";

                            int eventWithParts = _eventFragments.Count(e => e.AssociatedCharacter == npcName && e.Name == eventName);
                            if (eventWithParts > 0)
                            {
                                eventName += " ";
                                for (int x = 0; x <= eventWithParts; x++)
                                {
                                    eventName += "I";
                                }
                            }

                            // Create the EventFragment
                            var eventFragment = new EventFragment()
                            {
                                AssociatedCharacter = npcName,
                                Key = eventKey,
                                Id = id,
                                Data = events[eventKey],
                                Location = location.NameOrUniqueName,
                                Name = eventName,
                                RequiredHearts = hearts / 250,
                                StartTime = startTime,
                                Weather = weather
                            };

                            _eventFragments.Add(eventFragment);
                        }
                    }
                }
                catch (Exception ex)
                {
                    continue;
                }
            }
        }

        internal List<EventFragment> GetEventsForNPC(NPC npc, bool forceRefresh = false)
        {
            if (npc is null)
            {
                return new List<EventFragment>();
            }

            if (_eventFragments is null || forceRefresh is true)
            {
                RefreshEventFragments();
            }

            var characterEvents = new List<EventFragment>();
            foreach (var fragment in _eventFragments.Where(e => e.AssociatedCharacter == npc.Name))
            {
                if (int.TryParse(fragment.Id, out int actualId) && Game1.player.eventsSeen.Contains(actualId) is false)
                {
                    continue;
                }

                characterEvents.Add(fragment);
            }

            return characterEvents;
        }

        internal void StartEventRemembrance(EventFragment eventFragment)
        {
            if (eventFragment is null)
            {
                return;
            }

            LocationRequest locationRequest = Game1.getLocationRequest(eventFragment.Location);
            locationRequest.OnLoad += delegate
            {
                var generatedEvent = new LimitedEvent(eventFragment.Data);
                generatedEvent.setExitLocation("CaveOfMemories", _mirrorTileBase.X, _mirrorTileBase.Y);

                var oldTime = Game1.timeOfDay;
                var oldOutdoorLight = Game1.outdoorLight;
                var oldWeather = Game1.netWorldState.Value.GetWeatherForLocation(LocationContext.Default);

                // Preserve the friendship data
                Dictionary<string, int> nameToFriendshipPoints = new Dictionary<string, int>();
                foreach (var friend in Game1.player.friendshipData.Keys)
                {
                    nameToFriendshipPoints[friend] = Game1.player.friendshipData[friend].Points;
                }

                generatedEvent.onEventFinished += delegate
                {
                    Game1.timeOfDay = oldTime;
                    Game1.outdoorLight = oldOutdoorLight;
                    foreach (var friend in Game1.player.friendshipData.Keys.Where(f => nameToFriendshipPoints.ContainsKey(f)))
                    {
                        Game1.player.friendshipData[friend].Points = nameToFriendshipPoints[friend];
                    }

                    var locationWeather = Game1.netWorldState.Value.GetWeatherForLocation(LocationContext.Default);
                    locationWeather.isRaining.Value = oldWeather.isRaining.Value;
                    locationWeather.isSnowing.Value = oldWeather.isSnowing.Value;
                    locationWeather.isLightning.Value = oldWeather.isLightning.Value;
                    locationWeather.isDebrisWeather.Value = oldWeather.isDebrisWeather.Value;
                    Game1.updateWeather(Game1.currentGameTime);
                };

                // Set the start time, if given
                if (String.IsNullOrEmpty(eventFragment.StartTime) is false && int.TryParse(eventFragment.StartTime, out int startTime))
                {
                    Game1.timeOfDay = startTime;
                }

                // Set the weather, if given
                if (String.IsNullOrEmpty(eventFragment.Weather) is false)
                {
                    var locationWeather = Game1.netWorldState.Value.GetWeatherForLocation(LocationContext.Default);
                    locationWeather.isRaining.Value = false;
                    locationWeather.isSnowing.Value = false;
                    locationWeather.isLightning.Value = false;
                    locationWeather.isDebrisWeather.Value = false;
                    switch (eventFragment.Weather.ToLower())
                    {
                        case "rainy":
                        case "stormy":
                            locationWeather.isRaining.Value = true;
                            break;
                    }
                }
                Game1.currentLocation.currentEvent = generatedEvent;
            };

            int xTile = 0;
            int yTile = 0;
            Utility.getDefaultWarpLocation(locationRequest.Name, ref xTile, ref yTile);
            Game1.warpFarmer(locationRequest, xTile, yTile, Game1.player.FacingDirection);
        }

        private int GetReflectedDirection(int initialDirection)
        {
            if (initialDirection == 0)
            {
                return 2;
            }
            else if (initialDirection == 2)
            {
                return 0;
            }

            return initialDirection;
        }

        public override void cleanupBeforePlayerExit()
        {
            _fakeFarmer = null;
            base.cleanupBeforePlayerExit();
        }
        public override bool isTileOccupiedForPlacement(Vector2 tileLocation, StardewValley.Object toPlace = null)
        {
            // Preventing player from placing items here
            return true;
        }

        public override void UpdateWhenCurrentLocation(GameTime time)
        {
            if (this.farmers.Contains(Game1.player))
            {
                if (_fakeFarmer is null || _fakeFarmer.Name != Game1.player.Name)
                {
                    int facingDirection = GetReflectedDirection(Game1.player.FacingDirection);
                    _fakeFarmer = Game1.player.CreateFakeEventFarmer();
                    _fakeFarmer.completelyStopAnimatingOrDoingAction();
                    _fakeFarmer.hidden.Value = false;
                    _fakeFarmer.faceDirection(facingDirection);
                    _fakeFarmer.setTileLocation(new Vector2(_mirrorTileBase.X, 0));
                    _fakeFarmer.currentLocation = Game1.currentLocation;
                    foreach (var dataKey in Game1.player.modData.Keys)
                    {
                        _fakeFarmer.modData[dataKey] = Game1.player.modData[dataKey];
                    }
                }

                if (_fakeFarmer is not null)
                {
                    var position = Game1.player.Position;
                    if (Game1.player.getTileY() >= _mirrorTileBase.Y + 3 || Game1.player.getTileY() <= _mirrorTileBase.Y - 1)
                    {
                        position.Y = 0;
                    }
                    else
                    {
                        // Old method: position.Y -= 1 * 64;

                        // Determines how close the clone gets from player's position, using 1216 as the starting point
                        position.Y = (1216 - Game1.player.Position.Y);
                    }
                    _fakeFarmer.Position = position;

                    if (Game1.player.ActiveObject is not null && Game1.player.IsCarrying())
                    {
                        _fakeFarmer.ActiveObject = Game1.player.ActiveObject.getOne() as StardewValley.Object;
                    }
                    else if (_fakeFarmer.ActiveObject is not null)
                    {
                        _fakeFarmer.ActiveObject = null;
                    }
                    _fakeFarmer.running = Game1.player.running;
                    _fakeFarmer.FarmerSprite.PauseForSingleAnimation = Game1.player.FarmerSprite.PauseForSingleAnimation;
                    _fakeFarmer.CanMove = Game1.player.CanMove;

                    _fakeFarmer.movementDirections.Clear();
                    foreach (var direction in Game1.player.movementDirections)
                    {
                        if (direction == 0)
                        {
                            _fakeFarmer.movementDirections.Insert(0, 2);
                        }
                        else if (direction == 2)
                        {
                            _fakeFarmer.movementDirections.Insert(0, 0);
                        }
                        else
                        {
                            _fakeFarmer.movementDirections.Insert(0, direction);
                        }
                    }

                    foreach (var dataKey in Game1.player.modData.Keys)
                    {
                        _fakeFarmer.modData[dataKey] = Game1.player.modData[dataKey];
                    }

                    // Fashion Sense compatibility fix
                    _fakeFarmer.FacingDirection = GetReflectedDirection(Game1.player.FacingDirection);
                    _fakeFarmer.faceDirection(_fakeFarmer.FacingDirection);
                    _fakeFarmer.modData["FashionSense.Animation.FacingDirection"] = _fakeFarmer.FacingDirection.ToString();

                    _fakeFarmer.Update(time, this);
                }
            }

            if (_fadeMirror && _mirrorAlpha > 100)
            {
                if (_elapsedMillisecondsTime > 500)
                {
                    _mirrorAlpha -= 5;
                    _elapsedMillisecondsTime = 0;
                }
                else
                {
                    _elapsedMillisecondsTime += time.TotalGameTime.TotalMilliseconds;
                }
            }

            base.UpdateWhenCurrentLocation(time);
        }

        public override bool checkAction(xTile.Dimensions.Location tileLocation, xTile.Dimensions.Rectangle viewport, Farmer who)
        {
            return base.checkAction(tileLocation, viewport, who);
        }

        public override bool performAction(string action, Farmer who, xTile.Dimensions.Location tileLocation)
        {
            if (action.Equals("mirror", StringComparison.OrdinalIgnoreCase) && who.getTileX() == _mirrorTileBase.X && who.getTileY() == _mirrorTileBase.Y)
            {
                Game1.activeClickableMenu = new DialogueBox("You stare into the mirror, gazing back at your reflection.");
                Game1.afterDialogues = delegate
                {
                    Game1.activeClickableMenu = new CharacterSelectionMenu(who, this);
                };
                _fadeMirror = true;

                return true;
            }

            return base.performAction(action, who, tileLocation);
        }

        public override bool isActionableTile(int xTile, int yTile, Farmer who)
        {
            var isActionable = base.isActionableTile(xTile, yTile, who);

            Game1.mouseCursorTransparency = 0.5f;
            if (who.getTileX() == _mirrorTileBase.X && who.getTileY() == _mirrorTileBase.Y)
            {
                Game1.mouseCursorTransparency = 1f;
            }

            return isActionable;
        }

        public override void drawBackground(SpriteBatch b)
        {
            base.drawBackground(b);

            var mirrorTexture = CaveOfMemories.assetManager.GetMirrorTexture();
            b.Draw(mirrorTexture, Game1.GlobalToLocal(Game1.viewport, _mirrorPosition), new Rectangle(0, 0, mirrorTexture.Width, mirrorTexture.Height), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 1f);

            if (_fakeFarmer is not null)
            {
                var previousSortMode = CaveOfMemories.modHelper.Reflection.GetField<SpriteSortMode>(b, "_sortMode").GetValue();
                b.End();
                b.Begin(SpriteSortMode.FrontToBack, BlendState.AlphaBlend, SamplerState.PointClamp);
                _fakeFarmer.draw(b);
                b.End();
                b.Begin(previousSortMode, BlendState.AlphaBlend, SamplerState.PointClamp);
            }

            b.Draw(mirrorTexture, Game1.GlobalToLocal(Game1.viewport, _mirrorPosition), new Rectangle(0, 0, mirrorTexture.Width, mirrorTexture.Height), new Color(255, 255, 255, _mirrorAlpha), 0f, Vector2.Zero, 4f, SpriteEffects.None, 1f);
        }

        public override void draw(SpriteBatch b)
        {
            // Prevents any temporarySprites from playing to stop terrain dust
            temporarySprites.Clear();

            base.draw(b);
        }
    }
}

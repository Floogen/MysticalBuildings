using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MysticalBuildings.Framework.Models;
using MysticalBuildings.Framework.UI;
using StardewValley;
using StardewValley.Menus;
using StardewValley.Objects;
using StardewValley.TerrainFeatures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MysticalBuildings.Framework.GameLocations
{
    internal class CaveOfMemoriesLocation : GameLocation
    {
        private Farmer _fakeFarmer;
        private List<EventFragment> _eventFragments;

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

            foreach (var location in Game1.locations)
            {
                try
                {
                    var events = location.GetLocationEvents();
                    foreach (var eventKey in events.Keys)
                    {
                        foreach (string segment in eventKey.Split('/'))
                        {
                            if (segment.Length > 0 && segment.ToLower()[0] == 'f')
                            {
                                var fragments = segment.Split(' ');
                                if (fragments.Length == 3)
                                {
                                    var npcName = fragments[1];
                                    int hearts = int.Parse(fragments[2]);
                                    var eventName = $"{hearts / 250} Heart Event";

                                    int eventWithParts = _eventFragments.Count(e => e.AssociatedCharacter == npcName && e.Name == eventName);
                                    if (eventWithParts > 0)
                                    {
                                        eventName += $" (Part {eventWithParts + 1})";
                                    }
                                    _eventFragments.Add(new EventFragment() { AssociatedCharacter = npcName, Id = eventKey, Name = eventName });
                                }
                                break;
                            }
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

            return _eventFragments.Where(e => e.AssociatedCharacter == npc.Name).ToList();
        }

        internal void StartEventRemembrance(EventFragment eventFragment)
        {
            Dictionary<string, string> location_events = null;
            try
            {
                location_events = Game1.content.Load<Dictionary<string, string>>("Data\\Events\\" + "Town");
            }
            catch (Exception ex)
            {
                return;
            }
            if (location_events == null)
            {
                return;
            }

            LocationRequest locationRequest = Game1.getLocationRequest("Town");
            locationRequest.OnLoad += delegate
            {
                var generatedEvent = new Event(location_events["4/f Abigail 1500/t 2100 2400/w sunny"], 4);
                generatedEvent.setExitLocation("CaveOfMemories", 6, 6);
                var test = new DialogueBox("");
                var oldTime = Game1.timeOfDay;
                var oldOutdoorLight = Game1.outdoorLight;
                var oldWeather = Game1.netWorldState.Value.GetWeatherForLocation(LocationContext.Default).isRaining.Value;
                generatedEvent.onEventFinished += delegate
                {
                    Game1.timeOfDay = oldTime;
                    Game1.netWorldState.Value.GetWeatherForLocation(LocationContext.Default).isRaining.Value = oldWeather;
                    Game1.outdoorLight = oldOutdoorLight;
                    //Game1.updateWeather(Game1.currentGameTime);
                };

                Game1.timeOfDay = 2100;
                //Game1.netWorldState.Value.GetWeatherForLocation(LocationContext.Default).isRaining.Value = true;
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
            // Prevents any temporarySprites from playing to stop terrain dust
            temporarySprites.Clear();

            if (this.farmers.Contains(Game1.player))
            {
                if (_fakeFarmer is null || _fakeFarmer.Name != Game1.player.Name)
                {
                    int facingDirection = GetReflectedDirection(Game1.player.FacingDirection);
                    _fakeFarmer = Game1.player.CreateFakeEventFarmer();
                    _fakeFarmer.completelyStopAnimatingOrDoingAction();
                    _fakeFarmer.hidden.Value = false;
                    _fakeFarmer.faceDirection(facingDirection);
                    _fakeFarmer.setTileLocation(new Vector2(6, 0));
                    _fakeFarmer.currentLocation = Game1.currentLocation;
                    foreach (var dataKey in Game1.player.modData.Keys)
                    {
                        _fakeFarmer.modData[dataKey] = Game1.player.modData[dataKey];
                    }
                }

                if (_fakeFarmer is not null)
                {
                    var position = Game1.player.Position;
                    position.Y -= 1 * 64;
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
                    _fakeFarmer.modData["FashionSense.Animation.FacingDirection"] = _fakeFarmer.FacingDirection.ToString();

                    _fakeFarmer.faceDirection(GetReflectedDirection(Game1.player.FacingDirection));
                    _fakeFarmer.Update(time, this);
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
            if (action.Equals("mirror", System.StringComparison.OrdinalIgnoreCase))
            {
                Game1.activeClickableMenu = new DialogueBox(new List<string>() { "You stare into the mirror, gazing back at your reflection.", "A memory of Abigail slowly fills your mind as the world fades around you..." });
                Game1.afterDialogues = delegate
                {
                    //StartEventRemembrance();
                    Game1.activeClickableMenu = new CharacterSelectionMenu(who, this);
                };

                return true;
            }
            return base.performAction(action, who, tileLocation);
        }

        public override bool isActionableTile(int xTile, int yTile, Farmer who)
        {
            var isActionable = base.isActionableTile(xTile, yTile, who);

            if (Game1.mouseCursorTransparency == 0.5f && who.getTileX() == 6 && who.getTileY() == 6)
            {
                Game1.mouseCursorTransparency = 1f;
            }

            return isActionable;
        }

        public override void drawBackground(SpriteBatch b)
        {
            base.drawBackground(b);

            var mirrorTexture = MysticalBuildings.assetManager.GetMirrorTexture();
            b.Draw(mirrorTexture, Game1.GlobalToLocal(Game1.viewport, new Vector2(320, 128)), new Rectangle(0, 0, mirrorTexture.Width, mirrorTexture.Height), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 1f);

            if (_fakeFarmer is not null)
            {
                _fakeFarmer.draw(b);
            }
            b.Draw(mirrorTexture, Game1.GlobalToLocal(Game1.viewport, new Vector2(320, 128)), new Rectangle(0, 0, mirrorTexture.Width, mirrorTexture.Height), new Color(255, 255, 255, 100), 0f, Vector2.Zero, 4f, SpriteEffects.None, 1f);
        }

        public override void draw(SpriteBatch b)
        {
            base.draw(b);
        }
    }
}

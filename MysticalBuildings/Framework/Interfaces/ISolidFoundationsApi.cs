using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Buildings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MysticalBuildings.Framework.Interfaces
{
    internal interface ISolidFoundationsApi
    {
        public class BroadcastEventArgs : EventArgs
        {
            public string BuildingId { get; set; }
            public Building Building { get; set; }
            public Farmer Farmer { get; set; }
            public Point TriggerTile { get; set; }
            public string Message { get; set; }
        }

        public event EventHandler<BroadcastEventArgs> BroadcastSpecialActionTriggered;
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CaveOfMemories.Framework.Models
{
    public class EventFragment
    {
        public string Key { get; set; } // The string key within the event JSON (contains the conditions)
        public string Id { get; set; } // The actual event ID (currently an int in SDV v1.5, but will be string in SDV v1.6)
        public string Data { get; set; } // The event logic and commands
        public string Name { get; set; }
        public string Location { get; set; }
        public string AssociatedCharacter { get; set; }
        public int RequiredHearts { get; set; }
        public string StartTime { get; set; }
        public string Weather { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MysticalBuildings.Framework.Models
{
    public class ModConfig
    {
        public int StatueOfGreedRefreshInDays { get; set; } = 1;
        public int QuizzicalStatueRefreshInDays { get; set; } = 1;
        public int CrumblingMineshaftRefreshInDays { get; set; } = 1;
        public int OrbOfReflectionRefreshInDays { get; set; } = 1;
        public int ObeliskOfWeatherRefreshInDays { get; set; } = 1;
    }
}

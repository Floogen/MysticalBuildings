using MysticalBuildings.Framework.Interfaces;
using StardewModdingAPI;

namespace MysticalBuildings.Framework.Managers
{
    internal class ApiManager
    {
        private IMonitor _monitor;
        private ISolidFoundationsApi _solidFoundationsApi;

        public ApiManager(IMonitor monitor)
        {
            _monitor = monitor;
        }

        internal bool HookIntoSolidFoundations(IModHelper helper)
        {
            _solidFoundationsApi = helper.ModRegistry.GetApi<ISolidFoundationsApi>("PeacefulEnd.SolidFoundations");

            if (_solidFoundationsApi is null)
            {
                _monitor.Log("Failed to hook into PeacefulEnd.SolidFoundations.", LogLevel.Error);
                return false;
            }

            _monitor.Log("Successfully hooked into PeacefulEnd.SolidFoundations.", LogLevel.Debug);
            return true;
        }

        public ISolidFoundationsApi GetSolidFoundationsApi()
        {
            return _solidFoundationsApi;
        }
    }
}

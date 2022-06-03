using SolidFoundations.Framework.Interfaces.Internal;
using StardewModdingAPI;

namespace MysticalBuildings.Framework.Managers
{
    internal class ApiManager
    {
        private IMonitor _monitor;
        private IApi _solidFoundationsApi;

        public ApiManager(IMonitor monitor)
        {
            _monitor = monitor;
        }

        internal bool HookIntoSolidFoundations(IModHelper helper)
        {
            _solidFoundationsApi = helper.ModRegistry.GetApi<IApi>("PeacefulEnd.SolidFoundations");

            if (_solidFoundationsApi is null)
            {
                _monitor.Log("Failed to hook into PeacefulEnd.SolidFoundations.", LogLevel.Error);
                return false;
            }

            _monitor.Log("Successfully hooked into PeacefulEnd.SolidFoundations.", LogLevel.Debug);
            return true;
        }

        public IApi GetSolidFoundationsApi()
        {
            return _solidFoundationsApi;
        }
    }
}

using CaveOfMemories.Framework.Interfaces;
using SolidFoundations.Framework.Interfaces.Internal;
using StardewModdingAPI;

namespace CaveOfMemories.Framework.Managers
{
    internal class ApiManager
    {
        private IMonitor _monitor;
        private IApi _solidFoundationsApi;
        private IGenericModConfigMenuApi _genericModConfigMenuApi;

        public ApiManager(IMonitor monitor)
        {
            _monitor = monitor;
        }

        internal bool HookIntoGMCM(IModHelper helper)
        {
            _genericModConfigMenuApi = helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");

            if (_genericModConfigMenuApi is null)
            {
                _monitor.Log("Failed to hook into spacechase0.GenericModConfigMenu.", LogLevel.Error);
                return false;
            }

            _monitor.Log("Successfully hooked into spacechase0.GenericModConfigMenu.", LogLevel.Debug);
            return true;
        }

        public IGenericModConfigMenuApi GetGMCMApi()
        {
            return _genericModConfigMenuApi;
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

using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CaveOfMemories.Framework.Managers
{
    internal class AssetManager
    {
        private IMonitor _monitor;
        private Texture2D _mirrorTexture;

        public AssetManager(IMonitor monitor, IModHelper helper)
        {
            _monitor = monitor;


            // Get the asset folder path
            var assetFolderPath = helper.ModContent.GetInternalAssetName(Path.Combine("Framework", "Assets")).Name;

            // Load in the assets
            _mirrorTexture = helper.ModContent.Load<Texture2D>(Path.Combine(assetFolderPath, "mirror_background.png"));
        }

        public Texture2D GetMirrorTexture()
        {
            return _mirrorTexture;
        }
    }
}

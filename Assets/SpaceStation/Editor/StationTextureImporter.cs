using UnityEditor;
using UnityEngine;

namespace SecondWind.SpaceStation.Editor
{
    public sealed class StationTextureImporter : AssetPostprocessor
    {
        void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith("Assets/SpaceStation/Resources/Station/Art/")) return;
            var importer = (TextureImporter)assetImporter; importer.textureType = TextureImporterType.Default;
            importer.alphaIsTransparency = true; importer.mipmapEnabled = false; importer.wrapMode = TextureWrapMode.Clamp;
            importer.maxTextureSize = 1024; importer.textureCompression = TextureImporterCompression.Uncompressed;
        }
    }
}

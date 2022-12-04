using System;
using Godot;
namespace Yarn.GodotIntegration
{

    [Serializable]
    /// <summary>
    /// Pairs a language ID with a Resource.
    /// </summary>
    public class LanguageToSourceAsset
    {
        /// <summary>
        /// The locale ID that this translation should create a
        /// Localization for.
        /// </summary>
        [Language]
        public string languageID;

        /// <summary>
        /// The Resource containing CSV data that the Localization
        /// should use.
        /// </summary>
        // Hide this when its value is equal to whatever property is
        // stored in the YarnProjectImporterEditor class's
        // CurrentProjectDefaultLanguageProperty.
        public string stringsFile;

        /// <summary>
        /// The folder containing additional assets for the lines, such
        /// as voiceover audio files.
        /// </summary> TODO: substitute? 
        //public DefaultAsset assetsFolder;
    }
}
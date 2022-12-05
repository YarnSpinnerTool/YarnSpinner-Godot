using System.Collections.Generic;
using System.Linq;
using Godot;
using Godot.Collections;

#if UNITY_EDITOR

using System.Linq;
#endif

namespace Yarn.GodotIntegration{

    /// <summary>
    /// List of valid locale codes
    /// https://docs.godotengine.org/en/stable/tutorials/i18n/locales.html#doc-locales
    /// </summary>
    [Tool]
    public partial class Localization : Resource
    {

        [Export] public string LocaleCode { get => _LocaleCode; set => _LocaleCode = value; }

        private string _LocaleCode;

        [Export] 
        private Dictionary _stringTable = new Dictionary();
        [Export] 
        private Dictionary _assetTable = new Dictionary();

        private System.Collections.Generic.Dictionary<string, string> _runtimeStringTable = new System.Collections.Generic.Dictionary<string, string>();

        /// <summary>
        /// Gets a value indicating whether this <see cref="Localization"/>
        /// contains assets that are linked to strings.
        /// </summary>
        public bool ContainsLocalizedAssets { get => _containsLocalizedAssets; set => _containsLocalizedAssets = value; }

        [Export]
        private bool _containsLocalizedAssets;

        /// <summary>
        /// The Resource containing CSV data that the Localization
        /// should use.
        /// </summary>
        // Hide this when its value is equal to whatever property is
        // stored in the YarnProjectImporterEditor class's
        // CurrentProjectDefaultLanguageProperty.
        [Export]
        public string stringsFile = "";
        
        #region Localized Strings
        public string GetLocalizedString(string key)
        {
            Variant result;
            if (_runtimeStringTable.TryGetValue(key, out var resultStr))
            {
                return resultStr;
            }

            if (_stringTable.TryGetValue(key, out result))
            {
                return result.AsString();
            }

            return null;
        }

        /// <summary>
        /// Returns a boolean value indicating whether this <see
        /// cref="Localization"/> contains a string with the given key.
        /// </summary>
        /// <param name="key">The key to search for.</param>
        /// <returns><see langword="true"/> if this Localization has a string
        /// for the given key; <see langword="false"/> otherwise.</returns>
        public bool ContainsLocalizedString(string key) => _runtimeStringTable.ContainsKey(key) || _stringTable.ContainsKey(key);

        /// <summary>
        /// Adds a new string to the string table.
        /// </summary>
        /// <remarks>
        /// This method updates the localisation asset on disk. It is not
        /// recommended to call this method during play mode, because changes
        /// will persist after you leave and may cause conflicts.
        /// </remarks>
        /// <param name="key">The key for this string (generally, the line
        /// ID.)</param>
        /// <param name="value">The user-facing text for this string, in the
        /// language specified by <see cref="LocaleCode"/>.</param>
        public void AddLocalisedStringToAsset(string key, string value) {
            _stringTable.Add(key, value);
        }

        /// <summary>
        /// Adds a new string to the runtime string table.
        /// </summary>
        /// <remarks>
        /// This method updates the localisation's runtime string table, which
        /// is useful for adding or changing the localisation during gameplay or
        /// in a built player. It doesn't modify the asset on disk, and any
        /// changes made will be lost when gameplay ends.
        /// </remarks>
        /// <param name="key">The key for this string (generally, the line
        /// ID.)</param>
        /// <param name="value">The user-facing text for this string, in the
        /// language specified by <see cref="LocaleCode"/>.</param>
        public void AddLocalizedString(string key, string value)
        {
            _runtimeStringTable.Add(key, value);
        }

        /// <summary>
        /// Adds a collection of strings to the runtime string table.
        /// </summary>
        /// <inheritdoc cref="AddLocalizedString(string, string)"
        /// path="/remarks"/>
        /// <param name="strings">The collection of keys and strings to
        /// add.</param>
        public void AddLocalizedStrings(IEnumerable<KeyValuePair<string, string>> strings)
        {
            foreach (var entry in strings)
            {
                AddLocalizedString(entry.Key, entry.Value);
            }
        }

        /// <summary>
        /// Adds a collection of strings to the runtime string table.
        /// </summary>
        /// <inheritdoc cref="AddLocalizedString(string, string)"
        /// path="/remarks"/>
        /// <param name="stringTableEntries">The collection of <see
        /// cref="StringTableEntry"/> objects to add.</param>
        public void AddLocalizedStrings(IEnumerable<StringTableEntry> stringTableEntries)
        {
            foreach (var entry in stringTableEntries)
            {
                AddLocalizedString(entry.ID, entry.Text);
            }
        }

        #endregion

        #region Localised Objects

        public T GetLocalizedObject<T>(string key) where T : Resource
        {
            _assetTable.TryGetValue(key, out var result);

            if (result is T resultAsTargetObject)
            {
                return resultAsTargetObject;
            }

            return null;
        }

        public void SetLocalizedObject<T>(string key, T value) where T : Resource => _assetTable.Add(key, value);

        public bool ContainsLocalizedObject<T>(string key) where T : Resource => _assetTable.ContainsKey(key) && _assetTable[key] is T;

        public void AddLocalizedObject<T>(string key, T value) where T : Resource => _assetTable.Add(key, value);

        public void AddLocalizedObjects<T>(IEnumerable<KeyValuePair<string, T>> objects) where T : Resource
        {
            foreach (var entry in objects)
            {
                _assetTable.Add(entry.Key, entry.Value);
            }
        }
        #endregion

        public virtual void Clear()
        {
            _stringTable.Clear();
            _assetTable.Clear();
            _runtimeStringTable.Clear();
        }

        /// <summary>
        /// Gets the line IDs present in this localization.
        /// </summary>
        /// <remarks>
        /// The line IDs can be used to access the localized text or asset
        /// associated with a line.
        /// </remarks>
        /// <returns>The line IDs.</returns>
        public IEnumerable<string> GetLineIDs()
        {
            var allKeys = new List<string>();

            var runtimeKeys = _runtimeStringTable.Keys;
            var compileTimeKeys = _stringTable.Keys;

            allKeys.AddRange(runtimeKeys);
            allKeys.AddRange(compileTimeKeys.ToList().ConvertAll(v=>v.AsString()));

            return allKeys;
        }
    }
}

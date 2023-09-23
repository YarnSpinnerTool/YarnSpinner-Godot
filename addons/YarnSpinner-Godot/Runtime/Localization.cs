using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Text.Json;
using Godot;


#if TOOLS
using YarnSpinnerGodot.Editor;
#endif

namespace YarnSpinnerGodot
{
    /// <summary>
    /// List of valid locale codes
    /// https://docs.godotengine.org/en/stable/tutorials/i18n/locales.html#doc-locales
    /// </summary>
    [Tool]
    public partial class Localization : Resource
    {
        [Export]
        public string LocaleCode
        {
            get => _LocaleCode;
            set => _LocaleCode = value;
        }

        private string _LocaleCode;

        private Dictionary<string, StringTableEntry> _stringTable;
        [Export] private string _stringTableJSON;

        public Dictionary<string, StringTableEntry> stringTable
        {
            get
            {
                if (_stringTable == null)
                {
                    if (!string.IsNullOrEmpty(_stringTableJSON))
                    {
                        try
                        {
                            _stringTable =
                                JsonSerializer.Deserialize<Dictionary<string, StringTableEntry>>(_stringTableJSON, YarnProject.JSONOptions);
                        }
                        catch (Exception e)
                        {
                            GD.PushError(
                                $"Error parsing {nameof(stringTable)} from {ResourcePath}. The JSON data may have been corrupted. Error: {e.Message}\n{e.StackTrace}");
                        }
                    }
                    else
                    {
                        stringTable = new Dictionary<string, StringTableEntry>();
                    }
                }

                return _stringTable;
            }
            set
            {
                _stringTable = value;
                _stringTableJSON= JsonSerializer.Serialize(_stringTable, YarnProject.JSONOptions);
                #if TOOLS
                                YarnProjectEditorUtility.ClearJSONCache();
                #endif
            }
        }
        
        private System.Collections.Generic.Dictionary<string, string> _runtimeStringTable =
            new System.Collections.Generic.Dictionary<string, string>();

        /// <summary>
        /// The Resource containing CSV data that the Localization
        /// should use.
        /// </summary>
        // Hide this when its value is equal to whatever property is
        // stored in the YarnProjectImporterEditor class's
        // CurrentProjectDefaultLanguageProperty.
        [Export] public string stringsFile = "";

        #region Localized Strings

        public string GetLocalizedString(string key)
        {
            string result;
            if (_runtimeStringTable.TryGetValue(key, out result))
            {
                return result;
            }

            if (stringTable.ContainsKey(key))
            {
                return ((StringTableEntry) stringTable[key]).Text;
            }

            return null;
        }

        /// <summary>
        /// Get <see cref="stringTable"/> as a list of <see cref="StringTableEntry"/>
        /// </summary>
        /// <returns></returns>
        public List<StringTableEntry> GetStringTableEntries()
        {
            return (from object key in stringTable.Keys select (StringTableEntry) stringTable[key.ToString()]).ToList();
        }

        /// <summary>
        /// Returns a boolean value indicating whether this <see
        /// cref="Localization"/> contains a string with the given key.
        /// </summary>
        /// <param name="key">The key to search for.</param>
        /// <returns><see langword="true"/> if this Localization has a string
        /// for the given key; <see langword="false"/> otherwise.</returns>
        public bool ContainsLocalizedString(string key) =>
            _runtimeStringTable.ContainsKey(key) || stringTable.ContainsKey(key);

        /// <summary>
        /// Adds a new string to the string table.
        /// </summary>
        /// <remarks>
        /// This method updates the localisation asset on disk. 
        /// </remarks>
        /// <param name="key">The key for this string (generally, the line
        /// ID.)</param>
        /// <param name="value">The user-facing text for this string, in the
        /// language specified by <see cref="LocaleCode"/>.</param>
        public void AddLocalisedStringToAsset(string key, StringTableEntry value)
        {
            stringTable.Add(key, value);
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
        public virtual void Clear()
        {
            stringTable.Clear();
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
            var compileTimeKeys = stringTable.Keys.Cast<string>();

            allKeys.AddRange(runtimeKeys);
            allKeys.AddRange(compileTimeKeys);

            return allKeys;
        }
    }
}
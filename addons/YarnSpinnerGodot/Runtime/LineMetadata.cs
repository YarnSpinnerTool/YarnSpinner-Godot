using System;
using System.Collections.Generic;
using Godot;

namespace Yarn.GodotIntegration
{
    [Serializable]
    public partial class LineMetadata: Resource
    {
        [Export] private Dictionary<string, string> _lineMetadata = new Dictionary<string, string>();
        public LineMetadata()
        {
            // empty constructor needed to instantiate from YarnProjectUtility
        }
        public LineMetadata(IEnumerable<LineMetadataTableEntry> lineMetadataTableEntries)
        {
            AddMetadata(lineMetadataTableEntries);
        }

        /// <summary>
        /// Adds any metadata if they are defined for each line. The metadata is internally
        /// stored as a single string with each piece of metadata separated by a single
        /// whitespace.
        /// </summary>
        /// <param name="lineMetadataTableEntries">IEnumerable with metadata entries.</param>
        public void AddMetadata(IEnumerable<LineMetadataTableEntry> lineMetadataTableEntries)
        {
            foreach (var entry in lineMetadataTableEntries)
            {
                if (entry.Metadata.Length == 0)
                {
                    continue;
                }

                _lineMetadata.Add(entry.ID, String.Join(" ", entry.Metadata));
            }
        }

        /// <summary>
        /// Gets the line IDs that contain metadata.
        /// </summary>
        /// <returns>The line IDs.</returns>
        public IEnumerable<string> GetLineIDs()
        {
            // The object returned doesn't allow modifications and is kept in
            // sync with `_lineMetadata`.
            return _lineMetadata.Keys;
        }

        /// <summary>
        /// Returns metadata for a given line ID, if any is defined.
        /// </summary>
        /// <param name="lineID">The line ID.</param>
        /// <returns>An array of each piece of metadata if defined, otherwise returns null.</returns>
        public string[] GetMetadata(string lineID)
        {
            if (_lineMetadata.TryGetValue(lineID, out var result))
            {
                return result.Split(' ');
            }

            return null;
        }
    }
}

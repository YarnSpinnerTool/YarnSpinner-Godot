using System;
using System.Collections.Generic;
using System.Linq;
using Godot;


namespace YarnSpinnerGodot
{

    [Serializable]
    [Tool]
    public class LineMetadata
    {

        public Dictionary<string, LineMetadataTableEntry> lineMetadata =
            new Dictionary<string, LineMetadataTableEntry>();

        /// <summary>
        /// File where a CSV will be written to describing the metadata (optional)
        /// </summary>
        public string stringsFile;

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

                entry.File = ProjectSettings.LocalizePath(entry.File);
                lineMetadata.Add(entry.ID, entry);
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
            return lineMetadata.Keys.Cast<string>();
        }

        /// <summary>
        /// Returns metadata for a given line ID, if any is defined.
        /// </summary>
        /// <param name="lineID">The line ID.</param>
        /// <returns>An array of each piece of metadata if defined, otherwise returns null.</returns>
        public string[] GetMetadata(string lineID)
        {
            if (lineMetadata.ContainsKey(lineID))
            {
                return ((LineMetadataTableEntry) lineMetadata[lineID]).Metadata;
            }

            return null;
        }

        public List<LineMetadataTableEntry> GetAllMetadata()
        {
            var metadataList = new List<LineMetadataTableEntry>();
            foreach (var key in lineMetadata.Keys)
            {
                var meta = (LineMetadataTableEntry) lineMetadata[key];
                metadataList.Add(meta);
            }

            return metadataList;
        }

        public void Clear()
        {
            lineMetadata.Clear();
        }
    }
}
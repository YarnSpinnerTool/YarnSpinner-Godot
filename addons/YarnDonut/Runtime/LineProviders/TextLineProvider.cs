using System.Collections.Generic;
using Godot;

namespace YarnDonut
{
    public partial class TextLineProvider : LineProviderBehaviour
    {
        /// <summary>Specifies the language code to use for text content
        /// for this <see cref="TextLineProvider"/>.
        /// </summary>
        [Language]
        [Export] public string textLanguageCode = System.Globalization.CultureInfo.CurrentCulture.Name;

        public override LocalizedLine GetLocalizedLine(Yarn.Line line)
        {
            string text;
            if (textLanguageCode == YarnProject.baseLocalization.LocaleCode)
            {
                text = YarnProject.baseLocalization.GetLocalizedString(line.ID);
            }
            else
            {
                text = Tr($"{line.ID}");
            }

            return new LocalizedLine()
            {
                TextID = line.ID,
                RawText = text,
                Substitutions = line.Substitutions,
                Metadata = YarnProject.lineMetadata.GetMetadata(line.ID),
            };
        }

        public override void PrepareForLines(IEnumerable<string> lineIDs)
        {
            // No-op; text lines are always available
        }

        public override bool LinesAvailable => true;

        public override string LocaleCode => textLanguageCode;
    }
}
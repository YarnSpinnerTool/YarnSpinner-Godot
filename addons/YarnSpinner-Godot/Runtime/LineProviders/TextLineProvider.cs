using System.Collections.Generic;
using Godot;

namespace YarnSpinnerGodot
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
            // By default this provider will treat "en" as matching "en-UK", "en-US" etc. You can 
            // remap language codes how you like if you don't want this behavior 
            if (textLanguageCode.ToLower().StartsWith(YarnProject.baseLocalization.LocaleCode.ToLower()))
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
                Metadata = YarnProject.LineMetadata.GetMetadata(line.ID),
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
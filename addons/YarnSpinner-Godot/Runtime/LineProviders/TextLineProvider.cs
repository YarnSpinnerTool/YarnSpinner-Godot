#nullable disable

using System.Threading;
using Godot;
using Yarn.Markup;

namespace YarnSpinnerGodot;

[GlobalClass]
public partial class TextLineProvider : LineProviderBehaviour
{
    /// <summary>Specifies the language code to use for text content
    /// for this <see cref="TextLineProvider"/>.
    /// </summary>
    [Language] [Export] public string textLanguageCode = System.Globalization.CultureInfo.CurrentCulture.Name;
    
    private YarnTask? prepareForLinesTask = null;

    public override bool LinesAvailable => prepareForLinesTask?.IsCompletedSuccessfully() ?? false;

    private LineParser lineParser = new LineParser();
    private BuiltInMarkupReplacer builtInReplacer = new BuiltInMarkupReplacer();

    public override void RegisterMarkerProcessor(string attributeName, IAttributeMarkerProcessor markerProcessor)
    {
        lineParser.RegisterMarkerProcessor(attributeName, markerProcessor);
    }

    public override void DeregisterMarkerProcessor(string attributeName)
    {
        lineParser.DeregisterMarkerProcessor(attributeName);
    }

    public override void _EnterTree()
    {
        lineParser.RegisterMarkerProcessor("select", builtInReplacer);
        lineParser.RegisterMarkerProcessor("plural", builtInReplacer);
        lineParser.RegisterMarkerProcessor("ordinal", builtInReplacer);
    }

    public override void _Ready()
    {
        if (!IsInstanceValid(YarnProject))
        {
            GD.PushError(
                $"{nameof(YarnProject)} is not set on {nameof(TextLineProvider)}. You must set the yarn project for this " +
                $"script to work properly. ");
        }

        if (string.IsNullOrWhiteSpace(LocaleCode))
        {
            LocaleCode = System.Globalization.CultureInfo.CurrentCulture.Name;
        }
    }

    public override YarnTask<LocalizedLine> GetLocalizedLineAsync(Yarn.Line line,
        CancellationToken cancellationToken)
    {
        string text;

        string sourceLineID = line.ID;

        string[] metadata = System.Array.Empty<string>();
        // Check to see if this line shadows another. If it does, we'll use
        // that line's text and asset.
        if (YarnProject != null)
        {
            metadata = YarnProject.LineMetadata?.GetMetadata(line.ID) ?? System.Array.Empty<string>();

            var shadowLineSource = YarnProject.LineMetadata?.GetShadowLineSource(line.ID);

            if (shadowLineSource != null)
            {
                sourceLineID = shadowLineSource;
            }
        }

        // By default, this provider will treat "en" as matching "en-UK", "en-US" etc. You can 
        // remap language codes how you like if you don't want this behavior 
        if (textLanguageCode.ToLower().StartsWith(YarnProject.baseLocalization.LocaleCode.ToLower()))
        {
            text = YarnProject.baseLocalization.GetLocalizedString(sourceLineID);
        }
        else
        {
            text = Tr($"{line.ID}");
            // fall back to base locale
            if (text.Equals(line.ID))
            {
                text = YarnProject.baseLocalization.GetLocalizedString(sourceLineID);
            }
        }


        if (text == null)
        {
            // No line available.
            GD.PushWarning($"Can't locate the text for the line: {line.ID}", this);
            return YarnTask.FromResult(LocalizedLine.InvalidLine);
        }

        var parseResult = lineParser.ParseString(LineParser.ExpandSubstitutions(text, line.Substitutions),
            this.LocaleCode);

        return YarnTask.FromResult(new LocalizedLine()
        {
            TextID = line.ID,
            Text = parseResult,
            RawText = text,
            Substitutions = line.Substitutions,
            Metadata = metadata,
        });
    }


    public override string LocaleCode
    {
        get => textLanguageCode;
        set => textLanguageCode = value;
    }
}
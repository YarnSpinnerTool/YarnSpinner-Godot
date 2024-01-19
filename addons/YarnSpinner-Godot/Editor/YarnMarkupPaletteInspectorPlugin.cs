#if TOOLS
using System;
using Godot;
using YarnSpinnerGodot.Editor.UI;


namespace YarnSpinnerGodot.Editor
{
    /// <summary>
    /// Custom inspector for <see cref="MarkupPalette"/> that allows the user
    /// to add / remove markup tags and set their associated colors. 
    /// </summary>
    [Tool]
    public partial class YarnMarkupPaletteInspectorPlugin : EditorInspectorPlugin
    {
        public override bool _CanHandle(GodotObject obj)
        {
            return obj is MarkupPalette;
        }

        public override bool _ParseProperty(GodotObject @object, Variant.Type type,
            string path,
            PropertyHint hint, string hintText, PropertyUsageFlags usage, bool wide)
        {
            if (@object is not MarkupPalette palette)
            {
                return false;
            }

            try
            {
                if (path == nameof(MarkupPalette.ColourMarkers))
                {
                    AddCustomControl(new Label
                        {Text = "Map [markup] tag names to colors"});
                    if (palette.ColourMarkers.Count == 0)
                    {
                        var noColorsLabel = new Label();
                        noColorsLabel.Text = "No colors remapped";
                        AddCustomControl(noColorsLabel);
                    }
                    else
                    {
                        var colorRemapGrid = new GridContainer();
                        colorRemapGrid.Columns = 3;
                        colorRemapGrid.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
                        colorRemapGrid.SizeFlagsHorizontal =
                            Control.SizeFlags.ExpandFill;

                        var originalHeader = new Label();
                        originalHeader.Text = "Markup Tag";
                        colorRemapGrid.AddChild(originalHeader);

                        var replacementHeader = new Label();
                        replacementHeader.Text = "Text Color";
                        colorRemapGrid.AddChild(replacementHeader);

                        var deleteHeader = new Label();
                        deleteHeader.Text = "Delete";
                        colorRemapGrid.AddChild(deleteHeader);
                        const int remapHeight = 4;
                        foreach (var tagName in palette.ColourMarkers.Keys)
                        {
                            colorRemapGrid.AddChild(new Label {Text = tagName});

                            var replacementColorButton = new MarkupPaletteColorButton
                                {palette = palette, tagName = tagName};
                            replacementColorButton.Color =
                                palette.ColourMarkers[tagName];
                            replacementColorButton.Size = new Vector2(0, remapHeight);
                            replacementColorButton.SizeFlagsHorizontal =
                                Control.SizeFlags.ExpandFill;
                            colorRemapGrid.AddChild(replacementColorButton);

                            var deleteArea = new HBoxContainer();
                            var deleteSpacer = new Label {Text = "   "};

                            var deleteButton = new MarkupPaletteDeleteTagButton
                            {
                                Text = "X",
                                tagName = tagName,
                                palette = palette
                            };
                            deleteButton.Text = "x";
                            deleteButton.AddThemeColorOverride("normal", Colors.Red);
                            deleteButton.Size = new Vector2(4, remapHeight);
                            deleteButton.SizeFlagsHorizontal = 0;

                            deleteArea.AddChild(deleteSpacer);
                            deleteArea.AddChild(deleteButton);
                            colorRemapGrid.AddChild(deleteArea);
                        }

                        AddCustomControl(colorRemapGrid);
                    }

                    var newTagRow = new HBoxContainer();
                    var addNewTagButton = new MarkupPaletteAddTagButton
                        {Text = "Add", palette = palette};

                    var newTagNameInput = new LineEditWithSubmit()
                    {
                        PlaceholderText = "tag name, without []",
                        CustomMinimumSize = new Vector2(80, 10),
                        SubmitButton = addNewTagButton
                    };
                    addNewTagButton.newTagNameInput = newTagNameInput;
                    newTagNameInput.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
                    addNewTagButton.Disabled = true;

                    newTagRow.AddChild(newTagNameInput);
                    newTagRow.AddChild(addNewTagButton);
                    newTagRow.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
                    newTagRow.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
                    AddCustomControl(newTagRow);

                    return true;
                }

                return false;
            }
            catch (Exception e)
            {
                GD.PushError(
                    $"Error in {nameof(YarnMarkupPaletteInspectorPlugin)}: {e.Message}\n{e.StackTrace}");
                return false;
            }
        }
    }
}
#endif
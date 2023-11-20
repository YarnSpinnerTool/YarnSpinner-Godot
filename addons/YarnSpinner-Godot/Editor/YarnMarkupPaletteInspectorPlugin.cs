#if TOOLS
using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Godot.Collections;


namespace YarnSpinnerGodot.Editor
{
    [Tool]
    public partial class YarnMarkupPaletteInspectorPlugin : EditorInspectorPlugin
    {
        public EditorInterface editorInterface;
        public override bool _CanHandle(GodotObject obj)
        {
            return obj is MarkupPalette;
        }

        public override bool _ParseProperty(GodotObject @object, Variant.Type type, string path,
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
                    AddCustomControl(new Label {Text = "Map [markup] tag names to colors"});
                    Godot.Collections.Dictionary<string, Color> colorMarkers = palette.ColourMarkers;
                    if (colorMarkers.Count == 0)
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
                        colorRemapGrid.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;

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

                            var replacementColorButton = new ColorPickerButton();
                            replacementColorButton.Color = palette.ColourMarkers[tagName];
                            replacementColorButton.Size = new Vector2(0, remapHeight);
                            replacementColorButton.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
                            replacementColorButton.PopupClosed += () =>
                            {
                                if (!IsInstanceValid(palette))
                                {
                                    return;
                                }

                                palette.ColourMarkers[tagName] = replacementColorButton.Color;
                                ResourceSaver.Save(palette, palette.ResourcePath);
                                palette.NotifyPropertyListChanged();
                            };
                            colorRemapGrid.AddChild(replacementColorButton);

                            var deleteArea = new HBoxContainer();
                            var deleteSpacer = new Label {Text = "   "};

                            var deleteButton = new Button {Text = "X",};
                            deleteButton.Text = "x";
                            deleteButton.AddThemeColorOverride("normal", Colors.Red);
                            deleteButton.Size = new Vector2(4, remapHeight);
                            deleteButton.SizeFlagsHorizontal = 0;
                            deleteButton.Pressed += () =>
                            {
                                palette.ColourMarkers.Remove(tagName);
                                ResourceSaver.Save(palette, palette.ResourcePath);
                                palette.NotifyPropertyListChanged();
                            };
                            deleteArea.AddChild(deleteSpacer);
                            deleteArea.AddChild(deleteButton);
                            colorRemapGrid.AddChild(deleteArea);
                        }

                        AddCustomControl(colorRemapGrid);
                    }

                    var newTagRow = new HBoxContainer();

                    var newTagNameInput = new LineEdit
                    {
                        PlaceholderText = "tag name, without []",
                        CustomMinimumSize = new Vector2(80, 10),
                    };
                    newTagNameInput.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
                    var addNewTagButton = new Button {Text = "Add"};
                    addNewTagButton.Disabled = true;
                    newTagNameInput.TextChanged += (newText) =>
                    {
                        if (!IsInstanceValid(addNewTagButton) || !IsInstanceValid(newTagNameInput))
                        {
                            return;
                        }

                        addNewTagButton.Disabled = string.IsNullOrEmpty(newTagNameInput.Text);
                    };
                    addNewTagButton.Pressed += () =>
                    {
                        if (!IsInstanceValid(palette) || !IsInstanceValid(newTagNameInput))
                        {
                            return;
                        }

                        var newTagName = newTagNameInput.Text?
                            .Replace("[", "").Replace("]", "");
                        if (string.IsNullOrEmpty(newTagName))
                        {
                            // button should be enabled, but just in case
                            GD.Print("Enter a markup tag name in order to add a color mapping.");
                            return;
                        }

                        palette.ColourMarkers.Add(newTagName, Colors.Black);
                        palette.NotifyPropertyListChanged();
                    };
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
                GD.PushError($"Error in {nameof(YarnMarkupPaletteInspectorPlugin)}: {e.Message}\n{e.StackTrace}");
                return false;
            }
        }
    }
}
#endif
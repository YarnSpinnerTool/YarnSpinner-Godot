#if TOOLS
using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Godot.Collections;
using Godot.NativeInterop;
using Microsoft.Extensions.FileSystemGlobbing;
using Yarn.Compiler;


namespace YarnSpinnerGodot.Editor
{
    [Tool]
    public partial class YarnProjectInspectorPlugin : EditorInspectorPlugin
    {
        private YarnCompileErrorsPropertyEditor _compileErrorsPropertyEditor;
        private ScrollContainer _parseErrorControl;
        private YarnProject _project;

        private readonly PackedScene _fileNameLabelScene =
            ResourceLoader.Load<PackedScene>("res://addons/YarnSpinner-Godot/Editor/UI/FilenameLabel.tscn");

        private readonly PackedScene _errorTextLabelScene =
            ResourceLoader.Load<PackedScene>("res://addons/YarnSpinner-Godot/Editor/UI/ErrorTextLabel.tscn");

        private readonly PackedScene _contextLabelScene =
            ResourceLoader.Load<PackedScene>("res://addons/YarnSpinner-Godot/Editor/UI/ContextLabel.tscn");

        private VBoxContainer _errorContainer;
        private RichTextLabel _sourceScriptsListLabel;

        public override bool _CanHandle(GodotObject obj)
        {
            return obj is YarnProject;
        }

        public override bool _ParseProperty(GodotObject @object, Variant.Type type, string path,
            PropertyHint hint, string hintText, PropertyUsageFlags usage, bool wide)
        {
            if (@object is not YarnProject project)
            {
                return false;
            }

            try
            {
                _project = project;
                // hide some properties that are not editable by the user
                var hideProperties = new List<string>
                {
                    nameof(YarnProject.LastImportHadAnyStrings),
                    nameof(YarnProject.LastImportHadImplicitStringIDs),
                    nameof(YarnProject.IsSuccessfullyParsed),
                    nameof(YarnProject.CompiledYarnProgramBase64),
                    nameof(YarnProject.baseLocalization),
                    nameof(YarnProject.ImportPath),
                    nameof(YarnProject.JSONProjectPath),
                    // can't use nameof for private fields here
                    "_baseLocalizationJSON",
                    "_lineMetadataJSON",
                    "_serializedDeclarationsJSON",
                    "_listOfFunctionsJSON",
                };
                if (hideProperties.Contains(path))
                {
                    // hide these properties from inspector
                    return true;
                }

                if (path == nameof(YarnProject.ProjectErrors))
                {
                    _compileErrorsPropertyEditor = new YarnCompileErrorsPropertyEditor();
                    AddPropertyEditor(path, _compileErrorsPropertyEditor);
                    _parseErrorControl = new ScrollContainer
                    {
                        SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                        SizeFlagsVertical = Control.SizeFlags.ExpandFill,
                    };
                    int errorAreaHeight = 40;
                    if (_project.ProjectErrors != null && _project.ProjectErrors.Length > 0)
                    {
                        errorAreaHeight = 200;
                    }

                    _parseErrorControl.CustomMinimumSize = new Vector2(0, errorAreaHeight);

                    _errorContainer = new VBoxContainer
                    {
                        SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                        SizeFlagsVertical = Control.SizeFlags.ExpandFill,
                    };
                    _parseErrorControl.AddChild(_errorContainer);
                    //parseErrorControl.BbcodeEnabled = true;
                    _compileErrorsPropertyEditor.OnErrorsUpdated += RenderCompilationErrors;
                    RenderCompilationErrors(_project);
                    AddCustomControl(_parseErrorControl);
                    return true;
                }

                return false;
            }
            catch (Exception e)
            {
                GD.PushError($"Error in {nameof(YarnProjectInspectorPlugin)}: {e.Message}\n{e.StackTrace}");
                return false;
            }
        }

        private void RemoveLocale(string localeCode)
        {
            GD.Print($"Removed locale code {localeCode}");
            _project.JSONProject.Localisation.Remove(localeCode);
            _project.SaveJSONProject();
            _project.NotifyPropertyListChanged();
        }

        public void SelectLocaleCSVPath(string localeCode)
        {
            var dialog = new FileDialog
            {
                FileMode = FileDialog.FileModeEnum.SaveFile,
                Access = FileDialog.AccessEnum.Filesystem,
                Title = $"Select CSV Path for Locale {localeCode}",
            };
            dialog.AddFilter("*.csv; CSV File");
            dialog.FileSelected += (savePath) => CSVFileSelected(savePath, localeCode);
            YarnSpinnerPlugin.editorInterface.GetBaseControl().AddChild(dialog);
            dialog.PopupCentered(new Vector2I(700, 500));
        }

        public void CSVFileSelected(string savePath, string localeCode)
        {
            savePath = ProjectSettings.LocalizePath(savePath);
            GD.Print($"CSV file selected for locale {localeCode}: {savePath}");
            if (!_project.JSONProject.Localisation.ContainsKey(localeCode))
            {
                _project.JSONProject.Localisation.Add(localeCode, new Project.LocalizationInfo());
            }

            _project.JSONProject.Localisation[localeCode].Strings = savePath;
            _project.NotifyPropertyListChanged();
            _project.SaveJSONProject();
        }

        private string _newLocale = null;
        private bool _addLocaleConnected;

        private void SaveNewLocaleCode(string localeCode, Button addButton)
        {
            _newLocale = localeCode;
            addButton.Disabled = string.IsNullOrEmpty(_newLocale);
        }

        public void LocaleAdded()
        {
            _project.JSONProject.Localisation[_newLocale] = new Project.LocalizationInfo
            {
                Assets = "",
                Strings = "",
            };
            _project.SaveJSONProject();
            YarnSpinnerPlugin.editorInterface.GetResourceFilesystem().ScanSources();
            _project.NotifyPropertyListChanged();
        }

        public override void _ParseBegin(GodotObject @object)
        {
            try
            {
                _project = (YarnProject) @object;
                _project.JSONProject =
                    Yarn.Compiler.Project.LoadFromFile(ProjectSettings.GlobalizePath(_project.JSONProjectPath));


                var recompileButton = new Button();
                recompileButton.Text = "Re-compile Scripts in Project";
                recompileButton.Pressed += () => OnRecompileClicked(_project);
                AddCustomControl(recompileButton);


                var addTagsButton = new Button();
                addTagsButton.Text = "Add Line Tags to Scripts";
                addTagsButton.Pressed += () => OnAddTagsClicked(_project);
                AddCustomControl(addTagsButton);


                var updateLocalizationsButton = new Button();

                var sourceScripts = _project.JSONProject.SourceFiles.ToList();
                updateLocalizationsButton.Text = "Update Localizations";
                updateLocalizationsButton.TooltipText = "Update Localization CSV and Godot .translation Files";
                updateLocalizationsButton.Pressed += OnUpdateLocalizationsClicked;
                AddCustomControl(updateLocalizationsButton);

                var scriptPatternsGrid = new GridContainer
                {
                    SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                    Columns = 3,
                    TooltipText = "YarnSpinner-Godot will search for all .yarn files" +
                                  $" that match the \n list of patterns in sourceFiles in {_project.JSONProjectPath}."
                                  + "\nEach pattern will be used to search the file system for files with names " +
                                  $"that match specified patterns by use of a {typeof(Matcher).FullName}."
                                  + $"\nThese patterns are relative to the location of this {nameof(YarnProject)}"
                                  + "\nA list of .yarn files found this way will be displayed here.",
                };

                foreach (var pattern in _project.JSONProject.SourceFilePatterns)
                {
                    scriptPatternsGrid.AddChild(new Label()); // spacer
                    scriptPatternsGrid.AddChild(new Label {Text = pattern});
                    var patternDeleteButton = new Button {Text = "x"};
                    patternDeleteButton.Pressed += () =>
                    {
                        if (!IsInstanceValid(_project))
                        {
                            return;
                        }

                        _project.JSONProject.SourceFilePatterns =
                            _project.JSONProject.SourceFilePatterns.Where(existingPattern =>
                                !existingPattern.Equals(pattern));
                        _project.SaveJSONProject();
                        YarnSpinnerPlugin.editorInterface.GetResourceFilesystem().ScanSources();
                        _project.NotifyPropertyListChanged();
                    };
                    scriptPatternsGrid.AddChild(patternDeleteButton);
                }

                scriptPatternsGrid.AddChild(new Label {Text = "New Pattern", TooltipText = "TODO"});
                var scriptPatternInput = new LineEdit
                    {PlaceholderText = "**/*.yarn", SizeFlagsHorizontal = Control.SizeFlags.ExpandFill};
                scriptPatternsGrid.AddChild(scriptPatternInput);
                var addPatternButton = new Button {Text = "Add"};
                addPatternButton.Pressed += () =>
                {
                    if (!IsInstanceValid(_project) || !IsInstanceValid(scriptPatternInput))
                    {
                        return;
                    }

                    if (string.IsNullOrEmpty(scriptPatternInput.Text))
                    {
                        return;
                    }

                    if (_project.JSONProject.SourceFilePatterns.Contains(scriptPatternInput.Text))
                    {
                        GD.Print($"Not adding duplicate pattern '{scriptPatternInput.Text}");
                    }
                    else
                    {
                        _project.JSONProject.SourceFilePatterns =
                            _project.JSONProject.SourceFilePatterns.Append(scriptPatternInput.Text);
                        _project.SaveJSONProject();
                        YarnSpinnerPlugin.editorInterface.GetResourceFilesystem().ScanSources();
                        _project.NotifyPropertyListChanged();
                    }
                };
                scriptPatternsGrid.AddChild(addPatternButton);
                AddCustomControl(scriptPatternsGrid);


                var numScriptsText = "None";
                if (sourceScripts.Any())
                {
                    numScriptsText = $"{sourceScripts.Count} .yarn script{(sourceScripts.Count > 1 ? "s" : "")}";
                }

                var matchingScriptsHeader = new HBoxContainer
                {
                    SizeFlagsVertical = Control.SizeFlags.ExpandFill,
                    SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
                };
                matchingScriptsHeader.AddChild(new Label {Text = "Matching Scripts"});
                matchingScriptsHeader.AddChild(new Label
                {
                    Text = numScriptsText, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                    HorizontalAlignment = HorizontalAlignment.Right
                });
                AddCustomControl(matchingScriptsHeader);
                int scriptAreaHeight = 40;
                if (sourceScripts.Any())
                {
                    scriptAreaHeight = 180;
                }

                _sourceScriptsListLabel = new RichTextLabel
                {
                    SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                    SizeFlagsVertical = Control.SizeFlags.ExpandFill,
                };
                _sourceScriptsListLabel.CustomMinimumSize = new Vector2(0, scriptAreaHeight);
                RenderSourceScriptsList(_project);
                AddCustomControl(_sourceScriptsListLabel);

                var localeGrid = new GridContainer {SizeFlagsHorizontal = Control.SizeFlags.ExpandFill};
                localeGrid.Columns = 3;

                var label = new Label {Text = "Localization CSVs"};
                localeGrid.AddChild(label);

                var textEntry = new LineEdit
                {
                    PlaceholderText = "locale code",
                    SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                };
                var addButton = new Button {Text = "Add"};
                SaveNewLocaleCode("", addButton);
                textEntry.TextChanged += (string text) => SaveNewLocaleCode(text, addButton);
                localeGrid.AddChild(textEntry);

                addButton.Pressed += LocaleAdded;
                localeGrid.AddChild(addButton);
                localeGrid.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
                localeGrid.SizeFlagsVertical = Control.SizeFlags.ExpandFill;

                foreach (var locale in _project.JSONProject.Localisation)
                {
                    var localeLabel = new Label();
                    localeLabel.Text = locale.Key;
                    localeGrid.AddChild(localeLabel);
                    var picker = new HBoxContainer();
                    picker.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
                    picker.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
                    var pathLabel = new Label
                    {
                        Text = locale.Value.Strings,
                        SizeFlagsVertical = Control.SizeFlags.ExpandFill,
                        AutowrapMode = TextServer.AutowrapMode.Arbitrary
                    };
                    if (pathLabel.Text == "")
                    {
                        pathLabel.Text = "(none)";
                    }

                    pathLabel.CustomMinimumSize = new Vector2(80, 30);
                    pathLabel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
                    pathLabel.ClipText = true;
                    picker.AddChild(pathLabel);
                    var pickerButton = new Button {Text = "Browse"};
                    pickerButton.Pressed += () => SelectLocaleCSVPath(locale.Key);
                    picker.AddChild(pickerButton);
                    localeGrid.AddChild(picker);
                    var deleteButton = new Button {Text = "X"};
                    deleteButton.Pressed += () => RemoveLocale(locale.Key);
                    localeGrid.AddChild(deleteButton);
                }

                AddCustomControl(localeGrid);

                var baseLocaleRow = new HBoxContainer
                {
                    SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                };
                baseLocaleRow.AddChild(new Label {Text = "Base language"});

                var baseLocaleInput = new LineEdit
                {
                    Text = _project.JSONProject.BaseLanguage,
                    SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
                };
                baseLocaleRow.AddChild(baseLocaleInput);
                var changeBaseLocaleButton = new Button {Text = "Change"};
                baseLocaleInput.TextChanged += (newText) =>
                {
                    changeBaseLocaleButton.Disabled = string.IsNullOrWhiteSpace(newText);
                };
                changeBaseLocaleButton.Pressed += () =>
                {
                    if (!IsInstanceValid(_project))
                    {
                        return;
                    }

                    _project.JSONProject.BaseLanguage = baseLocaleInput.Text.Trim();
                    _project.JSONProject.SaveToFile(_project.JSONProject.Path);
                    YarnSpinnerPlugin.editorInterface.GetResourceFilesystem().ScanSources();
                };
                baseLocaleRow.AddChild(changeBaseLocaleButton);
                AddCustomControl(baseLocaleRow);
                var writeBaseCSVButton = new Button();
                writeBaseCSVButton.Text = "Export Strings and Metadata as CSV";
                writeBaseCSVButton.TooltipText = "Write all of the lines in your Yarn Project to a CSV," +
                                                 " including the metadata, line IDs, and the names of the nodes" +
                                                 " in which each line appears.";
                writeBaseCSVButton.Pressed += OnBaseLanguageCSVClicked;
                AddCustomControl(writeBaseCSVButton);
            }
            catch (Exception e)
            {
                GD.PushError($"Error in {nameof(YarnProjectInspectorPlugin)}: {e.Message}\n{e.StackTrace}");
            }
        }

        private void OnBaseLanguageCSVClicked()
        {
            var dialog = new FileDialog
            {
                FileMode = FileDialog.FileModeEnum.SaveFile,
                Access = FileDialog.AccessEnum.Filesystem,
                Title = $"Select CSV Path for the base locale {_project.JSONProject.BaseLanguage}",
            };
            dialog.AddFilter("*.csv; CSV File");
            dialog.FileSelected += OnBaseLanguageCSVFileSelected;
            YarnSpinnerPlugin.editorInterface.GetBaseControl().AddChild(dialog);
            dialog.PopupCentered(new Vector2I(700, 500));
        }

        private void OnBaseLanguageCSVFileSelected(string savePath)
        {
            YarnProjectEditorUtility.WriteBaseLanguageStringsCSV(_project, savePath);
        }

        private void OnUpdateLocalizationsClicked()
        {
            YarnProjectEditorUtility.UpdateLocalizationCSVs(_project);
        }

        private void OnRecompileClicked(YarnProject project)
        {
            YarnProjectEditorUtility.UpdateYarnProject(project);
            _compileErrorsPropertyEditor.Refresh();
            project.NotifyPropertyListChanged();
        }

        public void RenderCompilationErrors(GodotObject yarnProject)
        {
            _project = (YarnProject) yarnProject;
            var errors = _project.ProjectErrors;
            SetErrors(errors);
            yarnProject.NotifyPropertyListChanged();
        }

        public void RenderSourceScriptsList(GodotObject yarnProject)
        {
            _project = (YarnProject) yarnProject;
            var scripts = _project.JSONProject.SourceFiles;
            SetSourceScripts(scripts);
            yarnProject.NotifyPropertyListChanged();
        }

        private void SetErrors(YarnProjectError[] errors)
        {
            for (var i = _errorContainer.GetChildCount() - 1; i >= 0; i--)
            {
                var child = _errorContainer.GetChild(i);

                child.QueueFree();
            }

            var errorGroups = errors.GroupBy(e => e.FileName);
            foreach (var errorGroup in errorGroups)
            {
                var errorsInGroup = errorGroup.ToList();
                var fileNameLabel = _fileNameLabelScene.Instantiate<Label>();
                var resFileName = ProjectSettings.LocalizePath(errorsInGroup[0].FileName);
                fileNameLabel.Text = $"{resFileName}:";
                _errorContainer.AddChild(fileNameLabel);
                var separator = new HSeparator();
                separator.CustomMinimumSize = new Vector2(0, 4);
                separator.SizeFlagsHorizontal = Control.SizeFlags.Expand;
                _errorContainer.AddChild(separator);
                foreach (var err in errorsInGroup)
                {
                    var errorTextLabel = _errorTextLabelScene.Instantiate<Label>();
                    errorTextLabel.Text = $"    {err.Message}";
                    _errorContainer.AddChild(errorTextLabel);

                    var contextLabel = _contextLabelScene.Instantiate<Label>();
                    contextLabel.Text = $"    {err.Context}";
                    _errorContainer.AddChild(contextLabel);
                }
            }
        }

        private void SetSourceScripts(IEnumerable<string> sourceScripts)
        {
            _sourceScriptsListLabel.Text = "";
            foreach (var script in sourceScripts)
            {
                var resFileName = ProjectSettings.LocalizePath(script.Replace("\\", "/"));
                _sourceScriptsListLabel.Text += resFileName + "\n";
            }
        }

        private void OnAddTagsClicked(YarnProject project)
        {
            YarnProjectEditorUtility.AddLineTagsToFilesInYarnProject(project);
            _compileErrorsPropertyEditor.Refresh();
            project.NotifyPropertyListChanged();
        }
    }
}
#endif
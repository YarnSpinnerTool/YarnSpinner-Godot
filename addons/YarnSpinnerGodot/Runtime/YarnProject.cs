using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Godot;
using Yarn.Compiler;
using Yarn.GodotIntegration;
using Yarn.GodotIntegration.Editor;

namespace Yarn.GodotIntegration
{

    public class YarnProject : Resource, IYarnErrorSource
    {

        /// <summary>
        /// Indicates whether the last time this file was imported, the
        /// file contained lines that did not have a line tag (and
        /// therefore were assigned an automatically-generated, 'implicit'
        /// string tag.) 
        /// </summary>
        [Export] public bool LastImportHadImplicitStringIDs;

        /// <summary>
        /// Indicates whether the last time this file was imported, the
        /// file contained any string tags.
        /// </summary>
        [Export] public bool LastImportHadAnyStrings;
        /// <summary>
        /// Indicates whether the last time this file was imported, the
        /// file was able to be parsed without errors. 
        /// </summary>
        /// <remarks>
        /// This value only represents whether syntactic errors exist or
        /// not. Other errors may exist that prevent this script from being
        /// compiled into a full program.
        /// </remarks>
        [Export] public bool IsSuccessfullyParsed = false;
        [Export(PropertyHint.MultilineText)] public List<string> ParseErrorMessages = new List<string>();
        public byte[] CompiledYarnProgram;

        // TODO: filter scripts by parse errors
        public List<Resource> ScriptsWithParseErrors => new List<Resource>();

        [Serializable]
        public class SerializedDeclaration
        {
            public static List<IType> BuiltInTypesList = new List<IType>
            {
                BuiltinTypes.String,
                BuiltinTypes.Boolean,
                BuiltinTypes.Number,
            };

            public string name = "$variable";


            public string typeName = BuiltinTypes.String.Name;

            public bool defaultValueBool;
            public float defaultValueNumber;
            public string defaultValueString;

            public string description;

            public bool isImplicit;

            public Resource sourceYarnAsset;

            public SerializedDeclaration(Declaration decl)
            {
                name = decl.Name;
                typeName = decl.Type.Name;
                description = decl.Description;
                isImplicit = decl.IsImplicit;

                sourceYarnAsset = ResourceLoader.Load<Resource>(decl.SourceFileName);

                if (typeName == BuiltinTypes.String.Name)
                {
                    defaultValueString = Convert.ToString(decl.DefaultValue);
                }
                else if (typeName == BuiltinTypes.Boolean.Name)
                {
                    defaultValueBool = Convert.ToBoolean(decl.DefaultValue);
                }
                else if (typeName == BuiltinTypes.Number.Name)
                {
                    defaultValueNumber = Convert.ToSingle(decl.DefaultValue);
                }
                else
                {
                    throw new InvalidOperationException($"Invalid declaration type {decl.Type.Name}");
                }
            }
        }

        [Serializable]
        /// <summary>
        /// Pairs a language ID with a Resource.
        /// </summary>
        public class LanguageToSourceAsset
        {
            /// <summary>
            /// The locale ID that this translation should create a
            /// Localization for.
            /// </summary>
            [Language]
            public string languageID;

            /// <summary>
            /// The Resource containing CSV data that the Localization
            /// should use.
            /// </summary>
            // Hide this when its value is equal to whatever property is
            // stored in the YarnProjectImporterEditor class's
            // CurrentProjectDefaultLanguageProperty.
            public Resource stringsFile;

            /// <summary>
            /// The folder containing additional assets for the lines, such
            /// as voiceover audio files.
            /// </summary> TODO: substitute? 
            //public DefaultAsset assetsFolder;
        }

        public List<string> CompileErrors = new List<string>();

        public List<SerializedDeclaration> SerializedDeclarations = new List<SerializedDeclaration>();

        [Language]
        public string defaultLanguage = System.Globalization.CultureInfo.CurrentCulture.Name;

        public List<LanguageToSourceAsset> languagesToSourceAssets;
        [Export]public Godot.Collections.Array<Resource> SourceScripts;
        
        /// <summary>
        /// Gets a value indicating whether this Yarn Project is able to
        /// generate a strings table - that is, it has no compile errors,
        /// it has at least one script, and all scripts are fully tagged.
        /// </summary>
        /// <inheritdoc path="exception"
        /// cref="GetScriptHasLineTags(Resource)"/>
        public bool CanGenerateStringsTable => this.CompileErrors.Count == 0 && SourceScripts.Count > 0 && SourceScripts.All(s => GetScriptHasLineTags(s));

        /// <summary>
        /// Gets a value indicating whether the source script has line
        /// tags.
        /// </summary>
        /// <param name="script">The source script to add. This script must
        /// have been imported by a <see cref="YarnImporter"/>.</param>
        /// <returns>
        /// <see langword="true"/> if the the script is fully tagged, <see
        /// langword="false"/> otherwise.
        /// </returns>
        /// <exception cref="System.ArgumentNullException">
        /// Thrown when <paramref name="script"/> is <see
        /// langword="null"/>.
        /// </exception>
        /// <exception cref="System.ArgumentException">
        /// Thrown when <paramref name="script"/> is not imported by a <see
        /// cref="YarnImporter"/>.
        /// </exception>
        private bool GetScriptHasLineTags(Resource script)
        {
            if (script == null)
            {
                // This might be a 'None' or 'Missing' asset, so return
                // false here.
                return false;
            }

            GD.Print("TODO: accurate check on which  scripts have line tags");
            return false;
        }
        
        IList<string> IYarnErrorSource.CompileErrors => ParseErrorMessages;
        public bool Destroyed => false; // not sure when this is used yet
        public Localization baseLocalization;

        public List<Localization> localizations = new List<Localization>();

        public LineMetadata lineMetadata;

        public LocalizationType localizationType;

        /// <summary>
        /// The cached result of deserializing <see
        /// cref="CompiledYarnProgram"/>.
        /// </summary>
        private Program cachedProgram = null;

        /// <summary>
        /// The names of assemblies that <see cref="ActionManager"/> should look
        /// for commands and functions in when this project is loaded into a
        /// <see cref="DialogueRunner"/>.
        /// </summary>
        public List<string> searchAssembliesForActions = new List<string>();

        public Localization GetLocalization(string localeCode)
        {

            // If localeCode is null, we use the base localization.
            if (localeCode == null)
            {
                return baseLocalization;
            }

            foreach (var loc in localizations)
            {
                if (loc.LocaleCode == localeCode)
                {
                    return loc;
                }
            }

            // We didn't find a localization. Fall back to the Base
            // localization.
            return baseLocalization;
        }

        /// <summary>
        /// Gets the Yarn Program stored in this project.
        /// </summary>
        [System.Obsolete("Use the Program property instead, which caches its return value.")]
        public Program GetProgram()
        {
            return Program.Parser.ParseFrom(CompiledYarnProgram);
        }

        /// <summary>
        /// Gets the Yarn Program stored in this project.
        /// </summary>
        /// <remarks>
        /// The first time this is called, the program stored in <see
        /// cref="CompiledYarnProgram"/> is deserialized and cached. Future
        /// calls to this method will return the cached value.
        /// </remarks>
        public Program Program
        {
            get
            {
                if (cachedProgram == null)
                {
                    cachedProgram = Program.Parser.ParseFrom(CompiledYarnProgram);
                }
                return cachedProgram;
            }
        }
    }

    public enum LocalizationType {
        YarnInternal,
        Unity,
    }
}

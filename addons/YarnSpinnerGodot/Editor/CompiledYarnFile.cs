#if TOOLS
using System.Collections.Generic;
using Godot;
using Yarn;
using Yarn.Compiler;
namespace YarnSpinnerGodot.addons.YarnSpinnerGodot
{
    public class CompiledYarnFile : Resource
    {
        /// <summary>
        /// The string encoded CompilationResult for this script
        /// TODO: how should i serialize, and do I only need the Program object? 
        /// </summary>
        [Export(PropertyHint.MultilineText)] public string Compilation;
        
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
    }
}
#endif
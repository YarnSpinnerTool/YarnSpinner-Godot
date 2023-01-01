#if TOOLS
using System;
using System.Collections.Generic;

namespace Yarn.GodotIntegration.Editor
{
    using TypeRegistrationQuery = ValueTuple<Func<Type, IYarnErrorSource>, string>;

    /// <summary>
    /// Interface to be implemented by any Yarn-specific importer to prevent play
    /// mode if there are any errors.
    /// </summary>
    public interface IYarnErrorSource
    {
        IList<string> CompileErrors { get; }
        bool Destroyed { get; }
    }

    /// <summary>
    /// Prevents play mode if there are any errors in any Yarn projects or scripts.
    /// </summary>
    public partial class YarnPreventPlayMode
    {
        private static YarnPreventPlayMode _instance;
        private static YarnPreventPlayMode Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new YarnPreventPlayMode();
                }
                return _instance;
            }
        }

        /// <summary>
        /// Register an error source type.
        ///
        /// </summary>
        /// <typeparam name="T">An asset importer type that qualifies as
        /// error source.</typeparam>
        /// <param name="filterQuery">Search query (see <see
        /// cref="AssetDatabase.FindAssets(string)"/> documentation for
        /// formatting).</param>
        public static void AddYarnErrorSourceType<T>(string filterQuery) where T : class,  IYarnErrorSource
            => Instance.assetSearchQueries.Add((importer => importer as T, filterQuery));

        public static bool HasCompileErrors() => false; // todo: prevent playing the game? Instance.CompilerErrors().Any();

        private readonly List<TypeRegistrationQuery> assetSearchQueries = new List<TypeRegistrationQuery>();

        // private YarnPreventPlayMode() => EditorApplication.playModeStateChanged += OnPlayModeChanged;

        // TODO: any editor signal for about to play to emulate prevent playmode? 
        // private void OnPlayModeChanged(PlayModeStateChange state)
        // {
        //     if (state != PlayModeStateChange.ExitingEditMode) { return; }
        //
        //     var compilerErrors = CompilerErrors();
        //
        //     if (!compilerErrors.Any())
        //     {
        //         return;
        //     }
        //
        //     foreach (var error in compilerErrors)
        //     {
        //         GD.PrintErr(error);
        //     }
        //
        //     EditorApplication.isPlaying = false;
        //     GD.PrintErr("There were import errors. Please fix them to continue.");
        //
        //     SceneView sceneView = EditorWindow.GetWindow<SceneView>();
        //
        //     // Usually the scene view should be initialized, but if it
        //     // isn't then it isn't a huge deal.
        //     if (sceneView != null)
        //     {
        //         sceneView.ShowNotification(new GUIContent("All Yarn compiler errors must be fixed before entering Play Mode."));
        //     }
        // }
        //
        // private IEnumerable<string> CompilerErrors()
        // {
        //     var allImporters = new List<IYarnErrorSource>();
        //
        //     foreach (var query in assetSearchQueries)
        //     {
        //         allImporters.AddRange(YarnEditorUtility.GetAllAssetsOf(query.Item2, query.Item1));
        //     }
        //
        //     return allImporters
        //         .Where(errorSource => errorSource != null && !errorSource.Destroyed && errorSource.CompileErrors.Count > 0)
        //         .SelectMany(errorSource => errorSource.CompileErrors);
        // }
    }
}
#endif
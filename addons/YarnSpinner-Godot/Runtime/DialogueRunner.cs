/*
Yarn Spinner is licensed to you under the terms found in the file LICENSE.md.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;
using Yarn;
using ArgumentOutOfRangeException = System.ArgumentOutOfRangeException;

#nullable enable

namespace YarnSpinnerGodot;

/// <summary>
/// A Line Cancellation Token stores information about whether a dialogue
/// presenter should stop its delivery.
/// </summary>
/// <remarks>
/// <para>Dialogue presenter receive Line Cancellation Tokens as a parameter to
/// <see cref="DialoguePresenterBase.RunLineAsync"/>. Line Cancellation
/// Tokens indicate whether the user has requested that the line's delivery
/// should be hurried up, and whether the dialogue presenter should stop showing
/// the current line.</para>
/// </remarks>
public struct LineCancellationToken
{
    /// <summary>
    /// A <see cref="CancellationToken"/> that becomes cancelled when a <see
    /// cref="DialogueRunner"/> wishes all dialogue presenters to stop running
    /// the current line. For example, on-screen UI should be dismissed, and
    /// any ongoing audio playback should be stopped.
    /// </summary>
    public CancellationToken NextLineToken;


    // this token will ALWAYS be a dependant token on the above 

    /// <summary>
    /// A <see cref="CancellationToken"/> that becomes cancelled when a <see
    /// cref="DialogueRunner"/> wishes all dialogue presenters to speed up their
    /// delivery of their line, if appropriate. For example, UI animations
    /// should be played faster or skipped.
    /// </summary>
    /// <remarks>This token is linked to <see cref="NextLineToken"/>: if the
    /// next line token is cancelled, then this token will become cancelled
    /// as well.</remarks>
    public CancellationToken HurryUpToken;

    /// <summary>
    /// Gets a value indicating whether the dialogue runner has requested
    /// that the next line be shown.
    /// </summary>
    /// <remarks>
    /// <para>
    /// If this value is <see langword="true"/>, dialogue presenters should
    /// present the current line, so that the next piece of content can
    /// be shown to the user.
    /// </para>
    /// <para>
    /// If this property is <see langword="true"/>, then <see
    /// cref="IsHurryUpRequested"/> will also be true.</para>
    /// </remarks>
    public readonly bool IsNextLineRequested => NextLineToken.IsCancellationRequested;

    /// <summary>
    /// Gets a value indicating whether the user has requested that the line
    /// be hurried up.
    /// </summary>
    /// <remarks><para>If this value is <see langword="true"/>, dialogue
    /// presenters should speed up any ongoing delivery of the line, such as
    /// on-screen animations, but are not required to finish delivering the
    /// line entirely (that is, UI elements may remain on screen).</para>
    /// <para>If <see cref="IsNextLineRequested"/> is <see
    /// langword="true"/>, then this property will also be <see
    /// langword="true"/>.</para>
    /// </remarks>
    ///
    public readonly bool IsHurryUpRequested => HurryUpToken.IsCancellationRequested;
}

[GlobalClass]
public partial class DialogueRunner : Godot.Node
{
    static DialogueRunner()
    {
        // See comments on below method - trigger action registration from code generation. 
        YarnSpinnerGodot.Generated.ActionRegistration.Touch();
    }

    private Dialogue? dialogue;

    /// <summary>
    /// Gets the internal <see cref="Dialogue"/> object that reads and
    /// executes the Yarn script.
    /// </summary>
    public Dialogue Dialogue
    {
        get
        {
            if (dialogue == null)
            {
                dialogue = new Yarn.Dialogue(VariableStorage);

                dialogue.LineHandler = OnLineReceived;
                dialogue.OptionsHandler = OnOptionsReceived;
                dialogue.CommandHandler = OnCommandReceived;
                dialogue.NodeStartHandler = OnNodeStarted;
                dialogue.NodeCompleteHandler = OnNodeCompleted;
                dialogue.DialogueCompleteHandler = OnDialogueCompleted;
                dialogue.PrepareForLinesHandler = OnPrepareForLines;

                if (yarnProject != null)
                {
                    Dialogue.SetProgram(yarnProject.Program);
                }
            }

            return dialogue;
        }
    }

    /// <summary>
    /// The Yarn Project containing the nodes that this Dialogue Runner
    /// runs.
    /// </summary>
    [Export] internal YarnProject? yarnProject;

    /// <summary>
    /// The object that manages the Yarn variables used by this Dialogue Runner.
    /// </summary>
    [Export] private VariableStorageBehaviour? variableStorage;

    /// <summary>
    /// If true, errors will be logged at runtime if the <see cref="yarnProject"/>
    /// on this dialogue runner has compilation errors. 
    /// </summary>
    [Export] public bool PrintProjectErrors = true;

    /// <summary>
    /// Gets the <see cref="YarnProject"/> asset that this dialogue runner uses.
    /// </summary>
    /// <seealso cref="SetProject(YarnProject)"/>
    public YarnProject? YarnProject => yarnProject;

    /// <summary>
    /// Gets the VariableStorage that this dialogue runner uses to store and
    /// access Yarn variables.
    /// 
    /// </summary>
    public VariableStorageBehaviour VariableStorage
    {
        get
        {
            // If we don't  have a variable storage, create an in
            // InMemoryVariableStorage and use that.
            if (variableStorage == null)
            {
                var memoryStorage = new InMemoryVariableStorage();
                AddChild(memoryStorage);
                memoryStorage.Name = nameof(InMemoryVariableStorage);
                variableStorage = memoryStorage;
            }

            return variableStorage;
        }
        set
        {
            variableStorage = value;
            Dialogue.VariableStorage = value;
        }
    }

    [Export] internal LineProviderBehaviour? lineProvider;

    /// <summary>
    ///  Gets the <see cref="ILineProvider"/> that this dialogue runner uses
    ///  to fetch localized line content.
    /// </summary>
    public ILineProvider LineProvider
    {
        get
        {
            if (lineProvider == null)
            {
                // No line provider was created. We'll need to create one.
                var textProvider = new TextLineProvider();
                textProvider.Name = nameof(TextLineProvider);
                lineProvider = textProvider;
                AddChild(textProvider);
                lineProvider.YarnProject = yarnProject;
            }

            return lineProvider;
        }
    }

    /// <summary>
    /// The list of dialogue presenters that the dialogue runner delivers content
    /// to.
    /// </summary>
    [Export] public Array<Godot.Node?> dialoguePresenters = [];

    /// <summary>
    /// Gets a value that indicates if the dialogue is actively
    /// running.
    /// </summary>
    public bool IsDialogueRunning => Dialogue.IsActive;

    /// <summary>
    /// Whether the dialogue runner will immediately start running dialogue
    /// after loading.
    /// </summary>
    [Export] public bool autoStart;

    /// <summary>
    /// The name of the node that will start running immediately after
    /// loading.
    /// </summary>
    /// <remarks>This value must be the name of a node present in <see
    /// cref="YarnProject"/>.</remarks>
    /// <seealso cref="YarnProject"/>
    /// <seealso cref="StartDialogue(string)"/>
    [Export] public string? startNode;

    /// <summary>
    /// If this value is set, when an option is selected, the line contained
    /// in it (<see cref="OptionSet.Option.Line"/>) will be delivered to the
    /// dialogue runner's dialogue presenters as though it had been written as a
    /// separate line.
    /// </summary>
    /// <remarks>
    /// This allows a Yarn script to
    /// </remarks>
    [Export] public bool runSelectedOptionAsLine;

    /// <summary>
    /// An event that is called when a node starts running.
    /// </summary>
    /// <remarks>
    /// This event receives as a parameter the name of the node that is
    /// about to start running.
    /// </remarks>
    /// <seealso cref="NodeStartHandler"/>
    [Signal]
    public delegate void onNodeStartEventHandler(string nodeName);

    /// <summary>
    /// An signal that is emitted when a node is complete.
    /// </summary>
    /// <remarks>
    /// This event receives as a parameter the name of the node that
    /// just finished running.
    /// </remarks>
    /// <seealso cref="NodeCompleteHandler"/>
    [Signal]
    public delegate void onNodeCompleteEventHandler(string nodeName);

    /// <summary>
    /// A signal that is emitted when the dialogue starts running.
    /// </summary>
    [Signal]
    public delegate void onDialogueStartEventHandler();

    /// <summary>
    /// A signal that is emitted once the dialogue has completed.
    /// </summary>
    [Signal]
    public delegate void onDialogueCompleteEventHandler();

    /// <summary>
    /// Clear all event handlers for <see cref="onDialogueComplete"/>
    /// </summary>
    public void ClearAllOnDialogueComplete()
    {
        var connections = GetSignalConnectionList(SignalName.onDialogueComplete);
        foreach (var connection in connections)
        {
            var callable = connection["callable"].AsCallable();
            if (IsConnected(SignalName.onDialogueComplete, callable))
            {
                Disconnect(SignalName.onDialogueComplete, callable);
            }
        }
    }

    /// <summary>
    /// A signal that is emitted when a <see
    /// cref="Command"/> is received and no command handler was able to
    /// handle it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Use this method to dispatch a command to other parts of your game.
    /// This method is only called if the <see cref="Command"/> has not been
    /// handled by a command handler that has been added to the <see
    /// cref="DialogueRunner"/>, or by a method on a <see
    /// cref="MonoBehaviour"/> in the scene with the attribute <see
    /// cref="YarnCommandAttribute"/>.
    /// </para>
    /// <para style="hint">
    /// When a command is delivered in this way, the <see
    /// cref="DialogueRunner"/> will not pause execution. If you want a
    /// command to make the DialogueRunner pause execution, see <see
    /// cref="AddCommandHandler(string, Delegate)"/>.
    /// </para>
    /// <para>
    /// This method receives the full text of the command, as it appears
    /// between the <c>&lt;&lt;</c> and <c>&gt;&gt;</c> markers.
    /// </para>
    /// </remarks>
    /// <seealso cref="AddCommandHandler(string, Delegate)"/>
    /// <seealso cref="YarnCommandAttribute"/>
    [Signal]
    public delegate void onUnhandledCommandEventHandler(string commandText);

    /// <summary>
    /// Gets a completed <see cref="YarnTask{DialogueOption}"/> that
    /// contains a <see langword="null"/> value.
    /// </summary>
    /// <remarks>dialogue presenters can return this value from their <see
    /// cref="DialoguePresenterBase.RunOptionsAsync(DialogueOption[],
    /// CancellationToken)" method to indicate that no option was selected.
    /// />
    public static YarnTask<DialogueOption?> NoOptionSelected
    {
        get { return YarnTask.FromResult<DialogueOption?>(null); }
    }


    private CancellationTokenSource? dialogueCancellationSource;
    private CancellationTokenSource? currentLineCancellationSource;
    private CancellationTokenSource? currentLineHurryUpSource;

    // Will be set in the constructor
    private ICommandDispatcher? CommandDispatcher { get; set; }


    public override void _EnterTree()
    {
        if (CommandDispatcher == null)
        {
            var actions = new Actions(this, Dialogue.Library);
            CommandDispatcher = actions;
            actions.RegisterActions();
        }
    }

    /// <summary>
    /// Called by Godot to start running dialogue if <see cref="autoStart"/>
    /// is enabled.
    /// </summary>
    public override void _Ready()
    {
        if (IsInstanceValid(VariableStorage) && IsInstanceValid(yarnProject))
        {
            this.VariableStorage.Program = this.YarnProject!.Program;
        }

        if (IsInstanceValid(yarnProject))
        {
            this.LineProvider.YarnProject = this.YarnProject;
            CheckCompilationErrors();
        }

        foreach (var presenter in dialoguePresenters)
        {
            if (presenter == null ||
                presenter is not DialoguePresenterBase && presenter.GetScript().Obj is not GDScript)
            {
                GD.PushError(
                    $"Node {presenter?.Name} ({presenter?.GetType()}) added to {nameof(dialoguePresenters)} does not appear to be a dialogue presenter. " +
                    $"Ensure only dialogue presenters are added to {nameof(dialoguePresenters)}.");
            }
        }

        if (autoStart)
        {
            if (string.IsNullOrWhiteSpace(startNode))
            {
                GD.PushError(
                    $"Auto Start was enabled on this {nameof(DialogueRunner)}, but no {nameof(startNode)} was provided");
                return;
            }

            CallDeferred(nameof(StartDialogue), startNode);
        }
    }

    /// <summary>
    /// Stops the dialogue immediately, and cancels any currently running
    /// dialogue presenters.
    /// </summary>
    public void Stop()
    {
        CancelDialogue();
    }

    /// <summary>
    /// Called by Godot to cancel the current dialogue when the Dialogue
    /// Runner is destroyed.
    /// </summary>
    public override void _ExitTree()
    {
        CancelDialogue();
    }

    /// <summary>
    /// Gets a <see cref="YarnTask"/> that completes when the dialogue
    /// runner finishes its dialogue.
    /// </summary>
    /// <remarks>
    /// If the dialogue is not currently running when this property is
    /// accessed, the property returns a task that is already complete.
    /// </remarks>
    public YarnTask DialogueTask
    {
        get
        {
            async YarnTask WaitUntilComplete()
            {
                while (IsDialogueRunning)
                {
                    await YarnTask.NextFrame();
                    if (!IsInstanceValid(this))
                    {
                        return;
                    }
                }
            }

            if (IsDialogueRunning)
            {
                return WaitUntilComplete();
            }
            else
            {
                return YarnTask.CompletedTask;
            }
        }
    }

    private void CancelDialogue()
    {
        if (dialogueCancellationSource == null || Dialogue.IsActive == false)
        {
            // We're not running dialogue. There's nothing to cancel.
            return;
        }

        // Cancel the current line, if any.
        currentLineCancellationSource?.Cancel();

        // Cancel the entire dialogue.
        dialogueCancellationSource?.Cancel();

        // Stop the dialogue. This will cause OnDialogueCompleted to be called.
        Dialogue.Stop();
    }

    private void OnPrepareForLines(IEnumerable<string> lineIDs)
    {
        this.LineProvider.PrepareForLinesAsync(lineIDs, CancellationToken.None).Forget();
    }

    private void OnDialogueCompleted()
    {
        EmitSignal(SignalName.onDialogueComplete);
        OnDialogueCompleteAsync().Forget();
    }

    private async YarnTask OnDialogueCompleteAsync()
    {
        // cleaning up the old cancellation token
        currentLineCancellationSource?.Dispose();
        currentLineCancellationSource = null;
        currentLineHurryUpSource?.Dispose();
        currentLineHurryUpSource = null;

        var pendingTasks = new HashSet<YarnTask>();
        foreach (var presenter in this.dialoguePresenters)
        {
            if (!IsInstanceValid(presenter))
            {
                // The presenter doesn't exist. Skip it.
                continue;
            }

            if (presenter is DialoguePresenterBase asyncPresenter)
            {
                // Tell all of our presenters that the dialogue has finished
                async YarnTask RunCompletion()
                {
                    try
                    {
                        await ((DialoguePresenterBase)presenter).OnDialogueCompleteAsync();
                    }
                    catch (System.Exception e)
                    {
                        GD.PushError(e, presenter);
                    }
                }

                YarnTask task = RunCompletion();

                pendingTasks.Add(task);
            }
            else if (presenter.GetScript().Obj is GDScript)
            {
                const string gdScriptMethodName = "on_dialogue_complete_async";

                async YarnTask RunGDScriptCompletion()
                {
                    if (!presenter.HasMethod(gdScriptMethodName))
                    {
                        return;
                    }

                    var methodReturn = presenter.Call(gdScriptMethodName);

                    if (methodReturn.Obj != null &&
                        methodReturn.As<GodotObject>().GetClass() == "GDScriptFunctionState")
                    {
                        //  GDScript method with await statements - wait for them to finish.
                        await ((SceneTree)Engine.GetMainLoop()).ToSignal(methodReturn.AsGodotObject(), "completed");
                    }
                }

                pendingTasks.Add(RunGDScriptCompletion());
            }
        }

        // Wait for all presenters to finish doing their clean-up
        await YarnTask.WhenAll(pendingTasks);
    }

    private void OnNodeCompleted(string completedNodeName)
    {
        EmitSignal(SignalName.onNodeComplete, completedNodeName);
    }

    private void OnNodeStarted(string startedNodeName)
    {
        EmitSignal(SignalName.onNodeStart, startedNodeName);
    }

    private void OnCommandReceived(Command command)
    {
        OnCommandReceivedAsync(command).Forget();
    }

    private async YarnTask OnCommandReceivedAsync(Command command)
    {
        CommandDispatchResult dispatchResult = this.CommandDispatcher!.DispatchCommand(command.Text, this);

        var parts = SplitCommandText(command.Text);
        string commandName = parts.ElementAtOrDefault(0) ?? string.Empty;

        switch (dispatchResult.Status)
        {
            case CommandDispatchResult.StatusType.Succeeded:
                // The command succeeded. Wait for it to complete. (In the
                // case of commands that complete synchronously, this task
                // will be Task.Completed, so this 'await' will return
                // immediately.)
                await dispatchResult.Task;
                break;
            case CommandDispatchResult.StatusType.NoTargetFound:
                GD.PushError(
                    $"Can't call command {commandName}: failed to find a node named {parts.ElementAtOrDefault(1)}",
                    this);
                break;
            case CommandDispatchResult.StatusType.TargetMissingComponent:
                GD.PushError(
                    $"Can't call command {commandName}, because {parts.ElementAtOrDefault(1)} doesn't have the correct component");
                break;
            case CommandDispatchResult.StatusType.InvalidParameterCount:
                GD.PushError($"Can't call command {commandName}: incorrect number of parameters");
                break;
            case CommandDispatchResult.StatusType.CommandUnknown:
                // Attempt a last-ditch dispatch by emitting our 'onUnhandledCommand'
                // signal. Even with nothing connected, it seems there's a default handler registered as a fallback.
                // ignore that one with a Linq expression.
                List<Dictionary> connections = GetSignalConnectionList(SignalName.onUnhandledCommand).Where(dict =>
                {
                    if (!dict.ContainsKey("callable"))
                    {
                        return false;
                    }

                    var handlerCallable = dict["callable"].AsCallable();
                    return !(handlerCallable.Target == this && handlerCallable.Delegate == null);
                }).ToList();
                if (connections.Count > 0)
                {
                    // We can emit the signal!
                    EmitSignal(SignalName.onUnhandledCommand, command.Text);
                }
                else
                {
                    // We're out of ways to handle this command! Log this as an
                    // error.
                    GD.PushError(
                        $"No Command \"{commandName}\" was found. Did you remember to use the YarnCommand attribute or AddCommandHandler() function in C#?");
                }

                return;
            default:
                throw new ArgumentOutOfRangeException(
                    $"Internal error: Unknown command dispatch result status {dispatchResult}");
        }

        // Continue the Dialogue, unless dialogue cancellation was requested.
        if (dialogueCancellationSource?.IsCancellationRequested ?? false)
        {
            return;
        }

        Dialogue.Continue();
    }

    private void OnLineReceived(Line line)
    {
        OnLineReceivedAsync(line).Forget();
    }

    private async YarnTask OnLineReceivedAsync(Line line)
    {
        var localisedLine =
            await LineProvider.GetLocalizedLineAsync(line,
                dialogueCancellationSource?.Token ?? CancellationToken.None);

        if (localisedLine == LocalizedLine.InvalidLine)
        {
            GD.PushError($"Failed to get a localised line for {line.ID}!");
        }

        await RunLocalisedLine(localisedLine);

        if (dialogueCancellationSource?.IsCancellationRequested == false)
        {
            Dialogue.Continue();
        }
    }

    /// <summary>
    /// Runs a localised line on all dialogue presenters.
    /// </summary>
    /// <remarks>
    /// This method can be called from two places: 1. when a line is being run,
    /// and 2. when an option has been selected and <see
    /// cref="runSelectedOptionAsLine"/> is <see langword="true"/>.
    /// </remarks>
    /// <param name="localisedLine"></param>
    /// <returns></returns>
    private async YarnTask RunLocalisedLine(LocalizedLine localisedLine)
    {
        // Create a new cancellation source for this line, linked to the
        // dialogue cancellation (if we have one). Dispose of the previous one,
        // if we have it.
        currentLineCancellationSource?.Dispose();
        currentLineHurryUpSource?.Dispose();

        if (dialogueCancellationSource != null)
        {
            currentLineCancellationSource =
                CancellationTokenSource.CreateLinkedTokenSource(dialogueCancellationSource.Token);
        }
        else
        {
            currentLineCancellationSource = new CancellationTokenSource();
        }

        // now we make a new dependant hurry up cancellation token
        currentLineHurryUpSource =
            CancellationTokenSource.CreateLinkedTokenSource(currentLineCancellationSource.Token);
        var metaToken = new LineCancellationToken
        {
            NextLineToken = currentLineCancellationSource.Token,
            HurryUpToken = currentLineHurryUpSource.Token,
        };

        var pendingTasks = new HashSet<YarnTask>();

        foreach (var presenter in this.dialoguePresenters)
        {
            if (!IsInstanceValid(presenter))
            {
                // The presenter doesn't exist. Skip it.
                continue;
            }

            // Legacy support: if this presenter is a v2-style DialogueViewBase,
            // then set its requestInterrupt delegate to be one that stops
            // the current line.
#pragma warning disable CS0618 // 'construct' is obsolete
            if (presenter is DialogueViewBase v2View)
            {
                v2View.requestInterrupt = RequestNextLine;
            }
#pragma warning restore CS0618 // 'construct' is obsolete
            if (presenter is DialoguePresenterBase asyncPresenter)
            {
                // Tell all of our presenters to run this line, and give them a
                // cancellation token they can use to interrupt the line if needed.

                async YarnTask RunLineAndInvokeCompletion(LineCancellationToken token)
                {
                    try
                    {
                        // Run the line and wait for it to finish
                        await asyncPresenter.RunLineAsync(localisedLine, token);
                    }
                    catch (Exception e)
                    {
                        GD.PushError(e, presenter);
                    }
                }

                YarnTask task = RunLineAndInvokeCompletion(metaToken);

                pendingTasks.Add(task);
            }
            else if (presenter.GetScript().Obj != null && presenter.GetScript().As<Resource>() is GDScript)
            {
                async YarnTask WaitForGDScriptPresenter(Godot.Node gdscriptPresenter)
                {
                    const string gdscriptMethodName = "run_line_async";
                    if (!gdscriptPresenter.HasMethod(gdscriptMethodName))
                    {
                        return;
                    }

                    var methodReturn = gdscriptPresenter.Call(gdscriptMethodName,
                        GDScriptPresenterAdapter.LocalizedLineToDict(localisedLine));
                    if (methodReturn.Obj != null &&
                        methodReturn.As<GodotObject>().GetClass() == "GDScriptFunctionState")
                    {
                        //  GDScript method with await statements - wait for them to finish.
                        await ((SceneTree)Engine.GetMainLoop()).ToSignal(methodReturn.AsGodotObject(), "completed");
                    }
                }

                pendingTasks.Add(WaitForGDScriptPresenter(presenter));
            }
        }

        // Wait for all line presenter tasks to finish delivering the line.
        await YarnTask.WhenAll(pendingTasks);
        if (!IsInstanceValid(this))
        {
            // dialogue runner may have been deleted while awaiting.
            return;
        }
        // We're done; dispose of the cancellation sources. (Null-check them because if we're leaving play mode, then these references may no longer be valid.)

        currentLineCancellationSource?.Dispose();
        currentLineCancellationSource = null;

        currentLineHurryUpSource?.Dispose();
        currentLineHurryUpSource = null;
    }

    private void OnOptionsReceived(OptionSet options)
    {
        OnOptionsReceivedAsync(options).Forget();
    }

    private async YarnTask OnOptionsReceivedAsync(OptionSet options)
    {
        // Create a cancellation source that represents 'we don't need you to
        // select an option anymore'. Link it to the dialogue cancellation
        // source, so that if dialogue gets cancelled, all options get
        // cancelled.
        CancellationTokenSource optionCancellationSource;
        if (dialogueCancellationSource != null)
        {
            optionCancellationSource =
                CancellationTokenSource.CreateLinkedTokenSource(dialogueCancellationSource.Token);
        }
        else
        {
            optionCancellationSource = new CancellationTokenSource();
        }

        DialogueOption[] localisedOptions = new DialogueOption[options.Options.Length];
        for (int i = 0; i < options.Options.Length; i++)
        {
            var opt = options.Options[i];
            LocalizedLine localizedLine =
                await LineProvider.GetLocalizedLineAsync(opt.Line, optionCancellationSource.Token);

            if (localizedLine == LocalizedLine.InvalidLine)
            {
                GD.PushError($"Failed to get a localised line for line {opt.Line.ID} (option {i + 1})!");
            }

            localisedOptions[i] = new DialogueOption
            {
                DialogueOptionID = opt.ID,
                IsAvailable = opt.IsAvailable,
                Line = localizedLine,
                TextID = opt.Line.ID,
            };
        }

        var dialogueSelectionTCS = new YarnTaskCompletionSource<DialogueOption?>();

        async YarnTask WaitForOptionsPresenter(DialoguePresenterBase? presenter)
        {
            if (presenter == null)
            {
                return;
            }

            var result = await presenter.RunOptionsAsync(localisedOptions, optionCancellationSource.Token);
            if (!IsInstanceValid(this))
            {
                return;
            }

            if (result != null)
            {
                // We no longer need the other presenters, so tell them to stop
                // by cancelling the option selection.
                optionCancellationSource.Cancel();
                dialogueSelectionTCS.TrySetResult(result);
            }
        }

        async YarnTask WaitForGDScriptPresenter(Godot.Node gdScriptPresenter)
        {
            const string gdscriptMethodName = "run_options_async";
            if (!gdScriptPresenter.HasMethod(gdscriptMethodName))
            {
                return;
            }

            const int noOptionSelected = -99;
            int selectedOption = noOptionSelected;
            var methodReturn = gdScriptPresenter.Call(gdscriptMethodName,
                GDScriptPresenterAdapter.DialogueOptionsToDictArray(localisedOptions),
                Callable.From((int gdScriptSetOption) => selectedOption = gdScriptSetOption));


            if (methodReturn.Obj != null && methodReturn.As<GodotObject>().GetClass() == "GDScriptFunctionState")
            {
                // callable is from GDScript with await statements
                await ((SceneTree)Engine.GetMainLoop()).ToSignal(methodReturn.AsGodotObject(), "completed");
            }


            // selectedOption will be set by the Callable sent to the GDScript presenter.
            {
                await YarnTask.NextFrame();
                if (!IsInstanceValid(this))
                {
                    // dialogue runner may have been deleted while awaiting.
                    return;
                }
            }

            // if we got this far, selectedOption is not null anymore
            DialogueOption? result =
                localisedOptions.FirstOrDefault(option => option.DialogueOptionID == selectedOption);

            dialogueSelectionTCS.TrySetResult(result);
        }

        var pendingTasks = new List<YarnTask>();
        foreach (var presenter in this.dialoguePresenters)
        {
            if (!IsInstanceValid(presenter))
            {
                continue;
            }

            if (presenter is DialoguePresenterBase asyncPresenter)
            {
                pendingTasks.Add(WaitForOptionsPresenter(asyncPresenter));
            }
            else if (presenter.GetScript().Obj is GDScript)
            {
                pendingTasks.Add(WaitForGDScriptPresenter(presenter));
            }
        }

        await YarnTask.WhenAll(pendingTasks);
        if (!IsInstanceValid(this))
        {
            // dialogue runner may have been deleted while awaiting.
            return;
        }

        // at this point now every presenter has finished their handling of the options
        // the first one to return a non-null value will be the one that is chosen option
        // or if everyone returned null that's an error
        DialogueOption? selectedOption;

        try
        {
            selectedOption = await dialogueSelectionTCS.Task;
        }
        catch (Exception e)
        {
            // If a presenter threw an exception while getting the option,
            // propagate it
            GD.PushError(e);
            return;
            // throw;
        }

        optionCancellationSource.Dispose();

        if (dialogueCancellationSource?.IsCancellationRequested ?? false)
        {
            // We received a request to cancel dialogue while waiting for a
            // choice. Stop here, and do not provide it to the Dialogue.
            return;
        }

        else if (selectedOption == null)
        {
            // None of our option presenters returned an option, and our dialogue
            // wasn't cancelled. That's not allowed, because we don't know what
            // to do next!
            GD.PushError($"No dialogue presenter returned an option selection! Hanging here!");
            return;
        }

        Dialogue.SetSelectedOption(selectedOption.DialogueOptionID);

        if (runSelectedOptionAsLine)
        {
            // Run the selected option's line content as though we had received
            // it as a line.
            await RunLocalisedLine(selectedOption.Line);
        }

        if (dialogueCancellationSource?.IsCancellationRequested ?? false)
        {
            // Our dialogue has been cancelled. Don't continue the dialogue.
            return;
        }
        else
        {
            // Proceed to the next piece of dialogue content.
            Dialogue.Continue();
        }
    }

    /// <summary>
    /// Sets the dialogue runner's Yarn Project.
    /// </summary>
    /// <remarks>
    /// If the dialogue runner is currently running (that is, <see
    /// cref="IsDialogueRunning"/> is <see langword="true"/>), an <see
    /// cref="InvalidOperationException"/> is thrown.
    /// </remarks>
    /// <param name="project">The new <see cref="YarnProject"/> to be
    /// used.</param>
    /// <exception cref="InvalidOperationException">Thrown when attempting
    /// to set a new project while a dialogue is currently
    /// running.</exception>
    public void SetProject(YarnProject project)
    {
        if (this.IsDialogueRunning)
        {
            // Can't change project if we're already running.
            throw new InvalidOperationException("Can't set project, because dialogue is currently running.");
        }

        this.yarnProject = project;

        Dialogue.SetProgram(project.Program);
    }

    private void CheckCompilationErrors()
    {
        if (!PrintProjectErrors)
        {
            return;
        }

        if (IsInstanceValid(yarnProject) && yarnProject.ProjectErrors?.Length > 0)
        {
            GD.PushError(
                $"The yarn project at '{yarnProject.ResourcePath}' has compilation errors. " +
                "Correct the syntax in your yarn scripts to update your dialogue.");
        }
    }

    /// <summary>
    /// Starts running a node of dialogue.
    /// </summary>
    /// <remarks><paramref name="nodeName"/> must be the name of a node in
    /// <see cref="YarnProject"/>.</remarks>
    /// <param name="nodeName">The name of the node to run.</param>
    public void StartDialogue(string nodeName)
    {
        if (yarnProject == null)
        {
            GD.PushError($"Can't start dialogue: no Yarn Project has been configured.", this);
            return;
        }

        if (yarnProject.Program == null)
        {
            // The Yarn Project asset reference is valid, but it doesn't
            // have a program, likely due to a compiler error.
            GD.PushError(
                $"Can't start dialogue: Yarn Project doesn't contain a valid program (possibly due to errors in the Yarn scripts?)",
                this);
            return;
        }

        dialogueCancellationSource?.Dispose();

        dialogueCancellationSource = new CancellationTokenSource();
        LineProvider.YarnProject = yarnProject;
        Dialogue.SetProgram(yarnProject.Program);
        Dialogue.SetNode(nodeName);

        EmitSignal(SignalName.onDialogueStart);

        StartDialogueAsync().Forget();

        async YarnTask StartDialogueAsync()
        {
            var tasks = new List<YarnTask>();
            foreach (var presenter in dialoguePresenters)
            {
                if (presenter == null || !IsInstanceValid(presenter))
                {
                    continue;
                }

                if (presenter is DialoguePresenterBase asyncPresenter)
                {
                    tasks.Add(asyncPresenter.OnDialogueStartedAsync());
                }

                if (presenter.GetScript().Obj is GDScript)
                {
                    const string gdScriptMethodName = "on_dialogue_start_async";

                    async Task GDScriptDialogueStart()
                    {
                        if (!presenter.HasMethod(gdScriptMethodName))
                        {
                            return;
                        }

                        var returnValue = presenter.Call(gdScriptMethodName);
                        if (returnValue.Obj != null &&
                            returnValue.As<GodotObject>().GetClass() == "GDScriptFunctionState")
                        {
                            // callable is from GDScript with await statements
                            await ((SceneTree)Engine.GetMainLoop()).ToSignal(returnValue.AsGodotObject(), "completed");
                        }
                    }

                    tasks.Add(GDScriptDialogueStart());
                }
            }

            await YarnTask.WhenAll(tasks);
            if (!IsInstanceValid(this))
            {
                // dialogue runner may have been deleted while awaiting.
                return;
            }

            Dialogue.Continue();
        }
    }

    /// <summary>
    /// Requests that all dialogue presenters stop showing the current line, and
    /// prepare to show the next piece of content.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The specific behaviour of what happens when this method is called
    /// depends on the implementation of the Dialogue Runner's current
    /// dialogue presenters.
    /// </para>
    /// <para>
    /// If the dialogue runner is not currently running a line (for example,
    /// if it is running options, or is not running dialogue at all), this
    /// method has no effect.
    /// </para>
    /// </remarks>
    public void RequestNextLine()
    {
        if (currentLineCancellationSource == null)
        {
            // We aren't running a line, so there's nothing to cancel.
            return;
        }

        // Cancel the current line. All currently pending tasks, which received
        // a CancellationToken, will be able to respond to the request to
        // cancel.
        currentLineCancellationSource.Cancel();
    }

    /// <summary>
    /// Requests that all dialogue presenters speed up their delivery of the
    /// current line.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The specific behaviour of what happens when this method is called
    /// depends on the implementation of the Dialogue Runner's current
    /// dialogue presenters.
    /// </para>
    /// <para>
    /// If the dialogue runner is not currently running a line (for example,
    /// if it is running options, or is not running dialogue at all), this
    /// method has no effect.
    /// </para>
    /// </remarks>
    public void RequestHurryUpLine()
    {
        if (currentLineCancellationSource == null)
        {
            // We aren't running a line, so there's nothing to cancel.
            return;
        }

        if (currentLineHurryUpSource == null)
        {
            // we are running a line but don't have a hurry up token
            // is this a bug..?
            return;
        }

        currentLineHurryUpSource.Cancel();
    }

    /// <summary>
    /// Find a node by name in the tree starting with the root.
    /// </summary>
    public static Godot.Node FindChild(string name)
    {
        return ((SceneTree)Engine.GetMainLoop()).Root.FindChild(name, true, false);
    }

    public static Godot.Node FindNodeByPath(string path)
    {
        return ((SceneTree)Engine.GetMainLoop()).Root.GetNode(path);
    }

    /// <summary>
    /// Add a command handler using a Callable rather than a C# delegate.
    /// Mostly useful for integrating with GDScript.
    ///
    /// Note that only object methods can be used, you CANNOT pass a GDScript lambda.
    /// 
    /// If the last argument to your handler is a Callable, your command will be
    /// considered an async blocking command. When the work for your command is done,
    /// call the Callable that the DialogueRunner will pass to your handler. Then
    /// the dialogue will continue.
    ///
    /// Callables are only supported as the last argument to your handler for the
    /// purpose of making your command blocking.
    /// </summary>
    /// <param name="commandName">The name of the command.</param>
    /// <param name="handler">The Callable for the <see cref="CommandHandler"/> that
    /// will be invoked when the command is called.</param>
    public void AddCommandHandlerCallable(string commandName, Callable handler)
    {
        // Custom Callables cannot be passed to C#.
        // See: https://github.com/godotengine/godot/issues/76108#issuecomment-1719304017
        // A lambda is a custom callable, so lacks a method to call or any useful info.
        if (!IsInstanceValid(handler.Target) && handler.Delegate == null)
        {
            GD.PushError(
                $"Callable provided to {nameof(AddCommandHandlerCallable)} has no Target nor Delegate. " +
                "Did you pass a lambda? Lambdas cannot be passed to this method.");
            return;
        }

        if (!IsInstanceValid(handler.Target))
        {
            GD.PushError(
                $"Callable provided to {nameof(AddCommandHandlerCallable)} is invalid. " +
                "Could the Node associated with the callable have been freed?");
            return;
        }

        var methodInfo = handler.Target.GetMethodList().Where(dict =>
            dict["name"].AsString().Equals(handler.Method.ToString())).ToList();

        if (methodInfo.Count == 0)
        {
            GD.PushError();
            return;
        }

        var argsCount = methodInfo[0]["args"].AsGodotArray().Count;
        var argTypes = methodInfo[0]["args"].AsGodotArray().ToList()
            .ConvertAll((argDictionary) =>
                (Variant.Type)argDictionary.AsGodotDictionary()["type"].AsInt32());
        var invalidTargetMsg =
            $"Handler node for {commandName} is invalid. Was it freed?";


        async Task GenerateCommandHandler(params Variant[] handlerArgs)
        {
            if (!IsInstanceValid(handler.Target))
            {
                GD.PushError(invalidTargetMsg);
                return;
            }

            var castArgs = CastToExpectedTypes(argTypes, commandName, handlerArgs);

            var returnValue = handler.Call(castArgs.ToArray());
            if (returnValue.Obj != null && returnValue.As<GodotObject>().GetClass() == "GDScriptFunctionState")
            {
                // callable is from GDScript with await statements
                await ((SceneTree)Engine.GetMainLoop()).ToSignal(returnValue.AsGodotObject(), "completed");
            }
        }

        switch (argsCount)
        {
            case 0:
                AddCommandHandler(commandName,
                    async Task () => await GenerateCommandHandler());
                break;
            case 1:
                AddCommandHandler(commandName,
                    async Task (Variant arg0) =>
                        await GenerateCommandHandler(arg0));
                break;
            case 2:
                AddCommandHandler(commandName,
                    async Task (Variant arg0, Variant arg1) =>
                        await GenerateCommandHandler(arg0, arg1));
                break;
            case 3:
                AddCommandHandler(commandName,
                    async Task (Variant arg0, Variant arg1, Variant arg2) =>
                        await GenerateCommandHandler(arg0, arg1, arg2));
                break;
            case 4:
                AddCommandHandler(commandName,
                    async Task (Variant arg0, Variant arg1, Variant arg2,
                            Variant arg3) =>
                        await GenerateCommandHandler(arg0, arg1, arg2, arg3));
                break;
            case 5:
                AddCommandHandler(commandName,
                    async Task (Variant arg0, Variant arg1, Variant arg2,
                            Variant arg3, Variant arg4) =>
                        await GenerateCommandHandler(arg0, arg1, arg2, arg3, arg4));
                break;
            case 6:
                AddCommandHandler(commandName,
                    async Task (Variant arg0, Variant arg1, Variant arg2,
                            Variant arg3, Variant arg4, Variant arg5) =>
                        await GenerateCommandHandler(arg0, arg1, arg2,
                            arg3, arg4, arg5));
                break;
            default:
                GD.PushError($"You have specified a command handler with too " +
                             $"many arguments at {argsCount}. The maximum supported " +
                             $"number of arguments to a command handler is 6.");
                break;
        }
    }

    /// <summary>
    /// Cast a list of arguments from a .yarn script to the type that the handler
    /// expects based on type hinting. Used to cross back over from C# to GDScript
    /// </summary>
    /// <param name="argTypes">List of Variant.Types in order of the arguments
    /// from the caller's command or function handler</param>
    /// <param name="commandOrFunctionName">The name of the function or command
    /// being registered, for error logging purposes</param>
    /// <param name="args">params array of arguments to cast to their expected types</param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    private static Godot.Collections.Array CastToExpectedTypes(List<Variant.Type> argTypes,
        string commandOrFunctionName,
        params Variant[] args)
    {
        var castArgs = new Godot.Collections.Array();
        var argIndex = 0;
        foreach (var arg in args)
        {
            var argType = argTypes[argIndex];
            Variant castArg = argType switch
            {
                Variant.Type.Bool => arg.AsBool(),
                Variant.Type.Int => arg.AsInt32(),
                Variant.Type.Float => arg.AsSingle(),
                Variant.Type.String => arg.AsString(),
                Variant.Type.Callable => arg.AsCallable(),
                // if no type hint is given, assume string type
                Variant.Type.Nil => arg.AsString(),
                _ => throw new ArgumentException($"Type for {arg} is not supported: {argType}"),
            };
            castArgs.Add(castArg);
            if (castArg.Obj == null)
            {
                GD.PushError(
                    $"Argument for the handler for '{commandOrFunctionName}'" +
                    $" at index {argIndex} has unexpected type {argType}");
            }

            argIndex++;
        }

        return castArgs;
    }
}
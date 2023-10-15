using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;

namespace YarnSpinnerGodot
{
    public partial class OptionsListView : Node, DialogueViewBase
    {
        [Export] PackedScene optionViewPrefab;

        [Export] private NodePath lastLineTextPath;
        RichTextLabel lastLineText;

        /// <summary>
        /// The Control that is the parent of all UI elements in this line view.
        /// Used to modify the transparency/visibility of the UI.
        /// </summary>
        [Export] private NodePath viewControlPath;
        public Control viewControl;

        /// <summary>
        /// NodePath to a BoxContainer (HBoxContainer or VBoxContainer),
        /// which will automatically lay out the options
        /// </summary>
        [Export] private NodePath boxContainerPath;
        public BoxContainer boxContainer;

        [Export] float fadeTime = 0.1f;

        [Export] bool showUnavailableOptions = false;

        // A cached pool of OptionView objects so that we can reuse them
        List<OptionView> optionViews = new List<OptionView>();

        // The method we should call when an option has been selected.
        Action<int> OnOptionSelected;

        // The line we saw most recently.
        LocalizedLine lastSeenLine;

        public override void _Ready()
        {
            if (viewControl == null)
            {
                viewControl = GetNode<Control>(viewControlPath);
            }
            if (lastLineText == null && lastLineTextPath != null)
            {
                lastLineText = GetNode<RichTextLabel>(lastLineTextPath);
                lastLineText.Visible = false;
            }
            if (boxContainer == null)
            {
                boxContainer = GetNode<VBoxContainer>(boxContainerPath);
            }
            viewControl.Visible = false;
        }

        public Action requestInterrupt { get; set; }

        public void RunLine(LocalizedLine dialogueLine, Action onDialogueLineFinished)
        {
            // Don't do anything with this line except note it and
            // immediately indicate that we're finished with it. RunOptions
            // will use it to display the text of the previous line.
            lastSeenLine = dialogueLine;
            onDialogueLineFinished();
        }

        public void RunOptions(DialogueOption[] dialogueOptions, Action<int> onOptionSelected)
        {
            viewControl.Visible = false;
            // Hide all existing option views
            foreach (var optionView in optionViews)
            {
                optionView.Visible = false;
            }

            // If we don't already have enough option views, create more
            while (dialogueOptions.Length > optionViews.Count)
            {
                var optionView = CreateNewOptionView();
                optionView.Visible = false;
            }

            // Set up all of the option views
            int optionViewsCreated = 0;

            for (int i = 0; i < dialogueOptions.Length; i++)
            {
                var optionView = optionViews[i];
                var option = dialogueOptions[i];

                if (option.IsAvailable == false && showUnavailableOptions == false)
                {
                    // Don't show this option.
                    continue;
                }

                optionView.Visible = true;

                optionView.Option = option;

                // The first available option is selected by default
                if (optionViewsCreated == 0)
                {
                    optionView.GrabFocus();
                }

                optionViewsCreated += 1;
            }

            // Update the last line, if one is configured
            if (lastLineText != null)
            {
                if (lastSeenLine != null)
                {
                    lastLineText.Visible = true;
                    lastLineText.Text = lastSeenLine.Text.Text;
                }
                else
                {
                    lastLineText.Visible = false;
                }
            }

            // Note the delegate to call when an option is selected
            OnOptionSelected = onOptionSelected;

            // Fade it all in
            Effects.FadeAlpha(viewControl, 0, 1, fadeTime)
                .ContinueWith(t =>
                {
                    if (t.IsFaulted)
                    {
                        GD.PrintErr($"Error running {nameof(Effects.FadeAlpha)} on {nameof(OptionsListView)}: {t.Exception}");
                    }
                });

            /// <summary>
            /// Creates and configures a new <see cref="OptionView"/>, and adds
            /// it to <see cref="optionViews"/>.
            /// </summary>
            OptionView CreateNewOptionView()
            {
                var optionView = optionViewPrefab.Instantiate<OptionView>();
                boxContainer.AddChild(optionView);

                optionView.OnOptionSelected = OptionViewWasSelected;
                optionViews.Add(optionView);

                return optionView;
            }

            /// <summary>
            /// Called by <see cref="OptionView"/> objects.
            /// </summary>
            void OptionViewWasSelected(DialogueOption option)
            {
                OptionViewWasSelectedInternal(option).ContinueWith(t =>
                {
                    if (t.IsFaulted)
                    {
                        GD.PrintErr($"Error running {nameof(OptionViewWasSelected)} on {nameof(OptionsListView)}: {t.Exception}");
                    }
                });

                async Task OptionViewWasSelectedInternal(DialogueOption selectedOption)
                {
                    await Effects.FadeAlpha(viewControl, 1, 0, fadeTime);
                    viewControl.Visible = false;
                    if (lastLineText != null)
                    {
                        lastLineText.Visible = false;
                    }
                    OnOptionSelected(selectedOption.DialogueOptionID);
                }
            }

            optionViews[0].GrabFocus();
        }
        
        /// <inheritdoc />
        public void DialogueComplete()
        {   
            // do we still have a line lying around?
            if (viewControl.Visible)
            {
                lastSeenLine = null;
                OnOptionSelected = null;
                if (lastLineText != null)
                {
                    lastLineText.Visible = false;
                }
                viewControl.Visible = false;
                Effects.FadeAlpha(viewControl, viewControl.Modulate.A, 0, fadeTime);
            }
        }
    }
    
}
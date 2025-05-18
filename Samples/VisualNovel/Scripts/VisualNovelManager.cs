#nullable disable
using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using YarnSpinnerGodot;
using Array = Godot.Collections.Array;

public partial class VisualNovelManager : Node
{
    [Export] private NodePath _dialogueRunnerPath;
    [Export] private NodePath _backgroundPath;
    [Export] private NodePath _colorOverlayPath;
    [Export] private NodePath _dialogueStartUiPath;
    private Control _dialogueStartUi;
    [Export] private NodePath _englishButtonPath;
    private Button _englishButton;

    [Export] private NodePath _spanishButtonPath;
    private Button _spanishButton;

    [Export] private NodePath _japaneseButtonPath;
    private Button _japaneseButton;

    [Export] private NodePath _dialogueCanvasPath;
    private CanvasLayer _dialogueCanvas;
    private DialogueRunner _dialogueRunner;
    private TextureRect _background;

    private ColorRect _colorOverlay;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        _background = GetNode<TextureRect>(_backgroundPath);
        _dialogueCanvas = GetNode<CanvasLayer>(_dialogueCanvasPath);
        _dialogueCanvas.Visible = false;
        _englishButton = GetNode<Button>(_englishButtonPath);
        _spanishButton = GetNode<Button>(_spanishButtonPath);
        _japaneseButton = GetNode<Button>(_japaneseButtonPath);
        _dialogueStartUi = GetNode<Control>(_dialogueStartUiPath);
        _englishButton.Pressed += () =>
            StartDialogue("en-US");
        _spanishButton.Pressed += () =>
            StartDialogue("es");
        _japaneseButton.Pressed += () =>
            StartDialogue("ja");
        _colorOverlay = GetNode<ColorRect>(_colorOverlayPath);
        _dialogueRunner = GetNode<DialogueRunner>(_dialogueRunnerPath);


        _dialogueRunner.onDialogueComplete += OnDialogueComplete;
        _englishButton.GrabFocus();
    }

    public void StartDialogue(string locale)
    {
        TranslationServer.SetLocale(locale);
        ((TextLineProvider) _dialogueRunner.lineProvider).textLanguageCode = locale;
        _dialogueStartUi.Visible = false;
        _dialogueCanvas.Visible = true;
        _dialogueRunner.StartDialogue("Start");
    }

    private void OnDialogueComplete()
    {
        GD.Print("Visual novel sample has completed!");
    }

    private Dictionary<string, string> _bgShortNameToPath = new()
    {
        {
            "bg_office", "res://Samples/VisualNovel/Sprites/bg_office.png"
        }
    };

    /// <summary>
    /// Example of using YarnCommand attribute instead of AddCommandHandler
    /// </summary>
    /// <param name="backgroundImage">short name of the background to switch to</param>
    [YarnCommand]
    public void Scene(string backgroundImage)
    {
        if (!_bgShortNameToPath.ContainsKey(backgroundImage))
        {
            GD.PrintErr($"The audio stream name {backgroundImage} was not defined in {nameof(_bgShortNameToPath)}");
            return;
        }

        var texture = ResourceLoader.Load<Texture2D>(_bgShortNameToPath[backgroundImage]);
        _background.Texture = texture;
    }

    private Dictionary<string, string> _audioShortNameToUuid = new()
    {
        {
            "music_funny", "res://Samples/VisualNovel/Sounds/music_funny.mp3"
        },
        {
            "music_romantic", "res://Samples/VisualNovel/Sounds/music_romantic.mp3"
        },
        {
            "ambient_birds", "res://Samples/VisualNovel/Sounds/ambient_birds.ogg"
        }
    };

    private List<AudioStreamPlayer2D> _audioPlayers = new();

    [YarnCommand]
    public void PlayAudio(string streamName, float volume = 1.0f, string doLoop = "loop")
    {
        async YarnTask PlayAudioAsync()
        {
            if (!_audioShortNameToUuid.ContainsKey(streamName))
            {
                GD.PrintErr($"The audio stream name {streamName} was not defined in {nameof(_audioShortNameToUuid)}");
                return;
            }

            var stream = ResourceLoader.Load<AudioStream>(_audioShortNameToUuid[streamName]);
            var player = new AudioStreamPlayer2D();
            player.VolumeDb = Mathf.LinearToDb(volume);
            player.Stream = stream;
            _audioPlayers.Add(player);
            AddChild(player);
            player.Play();
            if (doLoop != "loop")
            {
                await DefaultActions.Wait(stream.GetLength());
                player.Stop();
                _audioPlayers.Remove(player);
                player.QueueFree();
            }
        }

        PlayAudioAsync().Forget();
    }

    private class Actor
    {
        public TextureRect Rect;
    }

    private Dictionary<string, Actor> _actors = new();

    private Dictionary<string, string> _spriteShortNameToPath = new()
    {
        {
            "biz-guy", "res://Samples/VisualNovel/Sprites/biz-guy.png"
        },
        {
            "cool-girl", "res://Samples/VisualNovel/Sprites/cool-girl.png"
        }
    };

    private Vector2 GetPosition(string coordinateX, string coordinateY)
    {
        // let's see if they used a position keyword
        var labelCoordinate = coordinateX.ToLower()
            .Replace(" ", "")
            .Replace("_", "")
            .Replace("-", "");
        var windowSize = DisplayServer.WindowGetSize();
        var targetCoordinates = new Vector2(GetCoordinate(coordinateX) * windowSize.X,
            (0.5f - GetCoordinate(coordinateY)) * windowSize.Y);
        return targetCoordinates;
    }

    // utility function to convert words like "left" or "right" into
    // equivalent screen ratios, where 0 for an x coordinate is extreme left
    private static float GetCoordinate(string coordinate)
    {
        switch (coordinate)
        {
            case "leftedge":
            case "bottomedge":
            case "loweredge":
                return 0f;
            case "left":
            case "bottom":
            case "lower":
                return 0.25f;
            case "center":
            case "middle":
                return 0.5f;
            case "right":
            case "top":
            case "upper":
                return 0.75f;
            case "rightedge":
            case "topedge":
            case "upperedge":
                return 1f;
            case "offleft":
                return -0.33f;
            case "offright":
                return 1.33f;
        }

        // if none of those worked, then let's try parsing it as a
        // number
        float position;
        if (float.TryParse(coordinate, out position))
        {
            return position;
        }

        GD.PrintErr(
            $"VN Manager couldn't convert position [{coordinate}]... it must be an alignment (left, center, right, or top, middle, bottom) or a value (like 0.42 as 42%)");
        return -1f;
    }

    // move a sprite usage: <<Move actorOrspriteName, screenPosX=0.5,
    // screenPosY=0.5, moveTime=1.0>> screenPosX and screenPosY are
    // normalized screen coordinates (0.0 - 1.0) moveTime is the time
    // in seconds it will take to reach that position
    [YarnCommand("Move")]
    public async Task MoveSprite(string actorOrSpriteName, string screenPosX = "0.5", string screenPosY = "0.5",
        float moveTime = 1)
    {
        var actor = _actors[actorOrSpriteName];
        var targetPosition = GetPosition(screenPosX, screenPosY);
        double elapsed = 0f;

        var distance = targetPosition - actor.Rect.Position;
        if (moveTime > 0)
        {
            while (elapsed < moveTime)
            {
                var delta = GetProcessDeltaTime();
                // calculate the sprite movement this frame, 
                // trying to normalize it based on framerate
                var timeRatio = delta / moveTime;
                var movement = new Vector2((float) timeRatio * distance.X, (float) timeRatio * distance.Y);
                actor.Rect.Position += movement;
                elapsed += delta;
                await DefaultActions.Wait(delta); // wait a frame
            }
        }

        actor.Rect.Position = targetPosition; // fully snap to the final position
    }

    // shake a sprite
    [YarnCommand("Shake")]
    public async Task ShakeSprite(string actorOrSpriteName, float moveTime)
    {
        var actor = _actors[actorOrSpriteName];
        var initialPos = actor.Rect.Position;
        var moveTimeMilliseconds = moveTime * 1000;
        var delay = 16;
        var elapsed = 0f;
        var iterationsSinceDestinationChange = 0;

        Vector2 GetRandomDestination()
        {
            return initialPos +
                   new Vector2(GD.RandRange(-40, 40),
                       GD.RandRange(-40, 40));
        }

        var destination = GetRandomDestination();
        while (elapsed < moveTimeMilliseconds)
        {
            if (iterationsSinceDestinationChange >= 4)
            {
                iterationsSinceDestinationChange = 0;
                destination = GetRandomDestination();
            }
            else
            {
                iterationsSinceDestinationChange++;
            }

            var distance = (destination - actor.Rect.Position) * (1f / delay) * 16;
            actor.Rect.Position += distance;
            await Task.Delay(delay);
            elapsed += delay;
        }

        actor.Rect.Position = initialPos;
    }

    [YarnCommand("Act")]
    public void SetActor(string actorName, string spriteName, string positionX = "", string positionY = "",
        string colorHex = "")
    {
        var newActor = new Actor();
        var rect = new TextureRect();
        AddChild(rect);
        newActor.Rect = rect;
        var texture = ResourceLoader.Load<Texture2D>(_spriteShortNameToPath[spriteName]);
        rect.Texture = texture;
        var originalSize = texture.GetSize();
        var targetHeight = DisplayServer.WindowGetSize().Y;
        rect.CustomMinimumSize = Vector2.Zero;
        var sizeRatio = originalSize.X / originalSize.Y;
        // clamp the actor sprite size to the screen
        rect.Size = new Vector2(targetHeight * sizeRatio, targetHeight);
        _actors[actorName] = newActor;
        rect.Position = GetPosition(positionX, positionY);
        MoveChild(rect, 1);
    }

    [YarnCommand("Hide")]
    public void HideSprite(string actorOrSpriteName)
    {
        _actors[actorOrSpriteName].Rect.Visible = false;
    }

    /// flip a sprite, or force the sprite to face a direction
    [YarnCommand("Flip")]
    public void FlipSprite(string actorOrSpriteName, string xDirection = null)
    {
        bool newFlip;
        var rect = _actors[actorOrSpriteName].Rect;
        if (string.IsNullOrEmpty(xDirection))
        {
            newFlip = !rect.FlipH;
        }
        else
        {
            switch (xDirection.ToLower())
            {
                case "left":
                    newFlip = false;
                    break;
                case "right":
                    newFlip = true;
                    break;
                default:
                    GD.PrintErr($"Unrecognized direction '{xDirection}' specified.");
                    return;
            }
        }

        rect.FlipH = newFlip;
    }

    [YarnCommand]
    public void StopAudioAll()
    {
        foreach (var player in _audioPlayers)
        {
            if (IsInstanceValid(player))
            {
                player.QueueFree();
            }
        }

        _audioPlayers.Clear();
    }

    /// <summary>typical screen fade effect, good for transitions?
    /// usage: Fade( #hexcolor, startAlpha, endAlpha, fadeTime
    /// )</summary>
    [YarnCommand]
    public async Task Fade(string fadeColorHex, float startAlpha = 0, float endAlpha = 1, float fadeTime = 1)
    {
        var elapsed = 0d;
        var newColor = new Color(fadeColorHex);
        newColor.A = startAlpha;
        _colorOverlay.Color = newColor;
        var colorDifference = endAlpha - startAlpha;
        var delay = 16;
        while (elapsed < fadeTime && Mathf.Abs(endAlpha - newColor.A) > 0.001)
        {
            var timeRatio = elapsed / fadeTime;
            newColor.A = (float) (startAlpha + timeRatio * colorDifference);
            _colorOverlay.Color = newColor;
            elapsed += delay / 1000d;
            await Task.Delay(delay);
        }

        GD.Print($"Finished fading to {fadeColorHex}");
    }
}
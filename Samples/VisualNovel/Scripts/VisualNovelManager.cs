using Godot;
using System;
using System.Collections.Generic;
using System.Net.Mime;
using Yarn.GodotIntegration;

public partial class VisualNovelManager : Node
{

    [Export] private NodePath dialogueRunnerPath;
    [Export] private NodePath backgroundPath;

    private DialogueRunner _dialogueRunner;
    private TextureRect _background;
    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        _background = GetNode<TextureRect>(backgroundPath);
        _dialogueRunner = GetNode<DialogueRunner>(dialogueRunnerPath);
        _dialogueRunner.AddCommandHandler("Scene", Scene);
        _dialogueRunner.AddCommandHandler("PlayAudio", (string streamName, float volume, string doLoop) =>
        {
            GD.Print("PlayAudio command invoked.");
            PlayAudio(streamName, volume, doLoop);
        });
        _dialogueRunner.AddCommandHandler("Act", SetActor);
        _dialogueRunner.AddCommandHandler("Move", MoveSprite);
        _dialogueRunner.AddCommandHandler("Flip", FlipSprite);
        _dialogueRunner.StartDialogue("Start");
    }

    private Dictionary<string, string> bgShortNameToPath = new Dictionary<string, string>
    {
        {
            "bg_office", "res://Samples/VisualNovel/Sprites/bg_office.png"
        }
    };
    private void Scene(string backgroundImage)
    {
        if (!bgShortNameToPath.ContainsKey(backgroundImage))
        {
            GD.PrintErr($"The audio stream name {backgroundImage} was not defined in {nameof(bgShortNameToPath)}");
            return;
        }

        var texture = ResourceLoader.Load<Texture2D>(bgShortNameToPath[backgroundImage]);
        _background.Texture = texture;
    }
    private Dictionary<string, string> audioShortNameToUUID = new Dictionary<string, string>
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
    private async void PlayAudio(string streamName, float volume, string doLoop)
    {
        if (!audioShortNameToUUID.ContainsKey(streamName))
        {
            GD.PrintErr($"The audio stream name {streamName} was not defined in {nameof(audioShortNameToUUID)}");
            return;
        }
        var stream = ResourceLoader.Load<AudioStream>(audioShortNameToUUID[streamName]);
        var player = new AudioStreamPlayer2D();
        player.VolumeDb = GD.LinearToDb(volume);
        player.Stream = stream;
        AddChild(player);
        player.Play();
        if (doLoop != "loop")
        {
            await DefaultActions.Wait(stream.GetLength());
            player.Stop();
            player.QueueFree();
        }
    }
    private class Actor
    {
        public TextureRect Rect;
    }
    private Dictionary<string, Actor> actors = new Dictionary<string, Actor>();
    private Dictionary<string, string> spriteShortNameToPath = new Dictionary<string, string>
    {
        {"biz-guy", "res://Samples/VisualNovel/Sprites/biz-guy.png"},
        {"cool-girl", "res://Samples/VisualNovel/Sprites/cool-girl.png"}
    };
    // utility function to convert words like "left" or "right" into
    // equivalent position numbers
    float ConvertCoordinates(string coordinate)
    {
        // first, is anyone named after this coordinate? we'll use the
        // X position
        if (actors.ContainsKey(coordinate))
        {
            return actors[coordinate].Rect.Position.x / DisplayServer.WindowGetSize().x;
        }

        // next, let's see if they used a position keyword
        var labelCoordinate = coordinate.ToLower().Replace(" ", "").Replace("_", "").Replace("-", "");
        switch (labelCoordinate)
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
        float x;
        if (float.TryParse(coordinate, out x))
        {
            return x;
        }

        GD.PrintErr(this, $"VN Manager couldn't convert position [{coordinate}]... it must be an alignment (left, center, right, or top, middle, bottom) or a value (like 0.42 as 42%)");
        return -1f;
    }
    // move a sprite usage: <<Move actorOrspriteName, screenPosX=0.5,
    // screenPosY=0.5, moveTime=1.0>> screenPosX and screenPosY are
    // normalized screen coordinates (0.0 - 1.0) moveTime is the time
    // in seconds it will take to reach that position
    public void MoveSprite(string actorOrSpriteName, string screenPosX = "0.5", string screenPosY = "0.5", float moveTime = 1)
    {
        var actor = actors[actorOrSpriteName];
        var position = new Vector2(ConvertCoordinates(screenPosX), ConvertCoordinates(screenPosY));
    }

    public void SetActor(string actorName, string spriteName, string positionX, string DispositionTypeNames, string color)
    {
        var newActor = new Actor();
        var rect = new TextureRect();
        AddChild(rect);
        rect.IgnoreTextureSize = true;
        newActor.Rect = rect;
        var texture = ResourceLoader.Load<Texture2D>(spriteShortNameToPath[spriteName]);
        rect.Texture = texture;
        var originalSize = texture.GetSize();
        var targetHeight = DisplayServer.WindowGetSize().y;
        var sizeRatio = originalSize.x / originalSize.y;
        // clamp the actor sprite size to the screen
        rect.Size = new Vector2(targetHeight * sizeRatio, targetHeight);
        actors[actorName] = newActor;
    }

    /// flip a sprite, or force the sprite to face a direction
    public void FlipSprite(string actorOrSpriteName, string xDirection)
    {
        bool newFlip;
        var rect = actors[actorOrSpriteName].Rect;
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
}
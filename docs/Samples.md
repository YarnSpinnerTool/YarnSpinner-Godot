This repository contains a few sample scenes that demonstrate the basic functionality of YarnSpinner-Godot. 

Clone this project and open it as a Godot project. Enabled the Plugin as described on the [Installation](./Installation.md) page. 

You can then open the sample scenes  under `Samples/` and run them to see a demonstration of the functionality. 

# Space 

The space sample is a simple dialogue sample which shows a player sprite moving throughout a small level, pressing the space key to interact with two characters, Sally and Computer. It demonstrates how to have different logic when re-entering a dialogue node, and yarn commands. The .yarn files also contain examples of `<<declare>>` statements which can optionally declare a yarn variable at compile time. 

# Visual Novel

This is an example of a dialogue-focused game such as a visual novel, which animates sprites and plays sound effects and music to reinforce the dramatic thrust of the scene. It does so by use of Yarn Commands.  It also displays a demonstration of how to set up localization for a base language (in this case, English) and two other languages. Please note that this sample uses a very rough machine translation so the Japanese and Spanish text may not be correct.

https://github.com/dogboydog/YarnDonut/assets/9920963/3eecbe38-65e5-4130-a838-8154405df013

# Pausing the Typewriter

This sample demonstrates how to use the pausing markup (`[pause /]`) to pause in the middle of line.
This sample also shows using the `onPauseStarted` and `onPauseEnded` events to respond to the dialogue pause by changing a sprite.

This sample highlights:

- using markup in lines
- using attributes inside of markup
- self-closing markup properties
- pausing the built in effects
- responding to events

To start the sample play the scene and the dialogue will begin by itself.
Click the continue button to advance lines.

https://github.com/YarnSpinnerTool/YarnSpinner-Godot/assets/9920963/07eb143e-26de-4a2d-9aee-f9560abeeec6

# Rounded Views

This sample demonstrates the alternative rounded style for the built in views.
Sliced views use Panels  to create resizeable dialogue views with custom texture backgrounds.
These views are made using the default line view and options list view and have no custom code inside of them and are provided as alternative prefabs for dialogue UI.

This sample highlights:

- using the alternative prefabs to create customisable UI
- using multiple sprites and highlight states to customise UI 

To get started play the scene and the dialogue will start itself.
Click on the small arrow to advance lines and click on option bubbles to select a choice.

https://github.com/YarnSpinnerTool/YarnSpinner-Godot/assets/9920963/8f68a761-f3a5-4bb0-a161-cee57863b9ce

# Markup Palettes

This sample demonstrates how to use the built in Markup Palettes to theme text.
Markup Palettes provide a means of lightly theming your lines without requiring any code.
The code for Markup Palettes inside of Line view present a good starting point for more advanced customisation for your game.

This sample highlights:

- Using markup palettes to colour text
- Using markup in dialogue

To get started play the scene and the dialogue will start itself.
Click on the continue button to advance the lines.

You can create your own MarkupPalette with Project -> Tools -> YarnSpinner -> Create Markup Palette.

https://github.com/YarnSpinnerTool/YarnSpinner-Godot/assets/9920963/cb7573ff-8cf2-4de9-b85c-99647a6c5cf0

## ASSET CREDITS

The assets included in this example project are:

    Visual Novel Tutorial Set (public domain) https://opengameart.org/content/visual-novel-tutorial-set
    Lovely Piano Song by Rafael Krux (public domain) https://freepd.com/
    Comic Game Loop - Mischief by Kevin MacLeod (public domain) https://freepd.com/

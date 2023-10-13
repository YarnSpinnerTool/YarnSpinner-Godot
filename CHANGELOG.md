# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/).

## [0.1.5] 2023-10-06

### Added 

- Added onDialogueStart signal to DialogueRunner

### Changed

- Removed some DLL dependencies which could cause problems with ambiguous references (#20)
- Changed onDialogueComplete, onNodeStart, onNodeComplete to be Godot signals rather than C# events - should be backwards compatible (#19)
 
## [0.1.4] 2023-10-06

### Changed

- Calling `Stop` on the Dialogue Runner will now also dismiss the LineView, OptionListView, and VoiceOverView. (#17)
- Updated YarnSpinner DLL files
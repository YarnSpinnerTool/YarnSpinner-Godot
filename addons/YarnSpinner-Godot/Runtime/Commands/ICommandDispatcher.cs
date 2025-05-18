/*
Yarn Spinner is licensed to you under the terms found in the file LICENSE.md.
*/

using System;
using System.Collections.Generic;
using Godot;
using YarnSpinnerGodot;

namespace YarnSpinnerGodot;

interface ICommandDispatcher : IActionRegistration
{
    CommandDispatchResult DispatchCommand(string command, Node host);

    void SetupForProject(YarnProject yarnProject);

    IEnumerable<ICommand> Commands { get; }
}
using System;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace YarnSpinnerGodot
{
    interface ICommandDispatcher : IActionRegistration
    {
        CommandDispatchResult DispatchCommand(string command, out Task commandTask);

        void SetupForProject(YarnProject yarnProject);

        IEnumerable<ICommand> Commands { get; }
    }
}

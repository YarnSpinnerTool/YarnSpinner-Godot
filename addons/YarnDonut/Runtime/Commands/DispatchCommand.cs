using System;
using System.Reflection;
using Godot;


namespace YarnDonut
{
    using Injector = Func<string, object>;
    using Converter = Func<string, object>;

    public partial class DispatchCommand
    {
        public MethodInfo Method { get; set; }
        public Injector Injector { get; set; }
        public Converter[] Converters { get; set; }

        public bool TryInvoke(string[] args, out object returnValue)
        {
            returnValue = null;

            // if the method isn't static, but doesn't have an object name,
            // then we can't proceed, but it might be caught by a manually
            // registered function.
            if (!Method.IsStatic && args.Length < 2) { return false; }

            try
            {
                var instance = Method.IsStatic ? null : Injector?.Invoke(args[1]);
                var finalArgs = ActionManager.ParseArgs(Method, Converters, args, Method.IsStatic);
                returnValue = Method.Invoke(instance, finalArgs);
                return true;
            }
            catch (Exception e) when (
                e is ArgumentException // when arguments are invalid
                || e is TargetException // when a method is not static, but the instance ended up null
            )
            {
                GD.PrintErr($"Can't run command {args[0]}: {e.Message}");
                return false;
            }
        }
    }
}

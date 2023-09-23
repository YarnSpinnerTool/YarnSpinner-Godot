using System;
using System.Threading.Tasks;
using Godot;


namespace YarnSpinnerGodot
{
    public partial class DefaultActions : Godot.Node
    {
        #region Commands
        /// <summary>
        /// Yarn Spinner defines two built-in commands: "wait", and "stop".
        /// Stop is defined inside the Virtual Machine (the compiler traps it
        /// and makes it a special case.) Wait is defined here in Godot.
        /// </summary>
        /// <param name="duration">How long to wait.</param>
        [YarnCommand("wait")]
        public static async Task Wait(double duration)
        {
            var mainLoop = Engine.GetMainLoop();
            var sceneTree = mainLoop as SceneTree;
            var timer = sceneTree.CreateTimer((float)duration);
            await mainLoop.ToSignal(timer, "timeout");
        }
        #endregion

        #region Functions
        [YarnFunction("random")]
        public static float Random()
        {
            return RandomRange(0, 1);
        }

        [YarnFunction("random_range")]
        public static float RandomRange(float minInclusive, float maxInclusive)
        {
            return (float)GD.RandRange(minInclusive, maxInclusive);
        }

        /// <summary>
        /// Pick an integer in the given range.
        /// </summary>
        /// <param name="sides">Dice range.</param>
        /// <returns>A number between <c>[1, <paramref name="sides"/>]</c>.
        /// </returns>
        [YarnFunction("dice")]
        public static uint Dice(int sides)
        {
            return (uint)((GD.Randi() +1)  % sides);
        }

        [YarnFunction("round")]
        public static int Round(float num)
        {
            return (int)RoundPlaces(num, 0);
        }

        [YarnFunction("round_places")]
        public static float RoundPlaces(float num, int places)
        {
            return (float)Math.Round(num, places);
        }

        [YarnFunction("floor")]
        public static int Floor(float num)
        {
            return Mathf.FloorToInt(num);
        }

        [YarnFunction("ceil")]
        public static int Ceil(float num)
        {
            return Mathf.CeilToInt(num);
        }

        /// <summary>
        /// Increment if integer, otherwise go to next integer.
        /// </summary>
        [YarnFunction("inc")]
        public static int Inc(float num)
        {
            if (Decimal(num) != 0)
            {
                return Mathf.CeilToInt(num);
            }
            return (int)num + 1;
        }

        /// <summary>
        /// Decrement if integer, otherwise go to previous integer.
        /// </summary>
        [YarnFunction("dec")]
        public static int Dec(float num)
        {
            if (Decimal(num) != 0)
            {
                return Mathf.FloorToInt(num);
            }
            return (int)num - 1;
        }

        /// <summary>
        /// The decimal portion of the given number.
        /// </summary>
        /// <param name="num">Number to get the decimal portion of.</param>
        /// <returns><c>[0, 1)</c></returns>
        [YarnFunction("decimal")]
        public static float Decimal(float num)
        {
            return num - Int(num);
        }

        /// <summary>
        /// Truncates the number into an int. This is different to
        /// <see cref="Floor(float)"/> because it rounds to zero rather than
        /// negative <see cref="Mathf.Inf"/>.
        /// </summary>
        /// <param name="num">Number to truncate.</param>
        /// <returns>Truncated float value as int.</returns>
        [YarnFunction("int")]
        public static int Int(float num)
        {
            return (int)Math.Truncate(num);
        }
        #endregion
    }
}

using System;


namespace YarnDonut
{
    [AttributeUsage(AttributeTargets.Method, Inherited = false)]
    public partial class YarnActionAttribute : Attribute
    {
        /// <summary>
        /// The name of the command or function, as it exists in Yarn.
        /// </summary>
        /// <remarks>
        /// This value does not have to be the same as the name of the
        /// method. For example, you could have a method named
        /// "`WalkToPoint`", and expose it to Yarn as a command named
        /// "`walk_to_point`".
        /// </remarks>
        public string Name { get; set; }

        public YarnActionAttribute(string name = null) => Name = name;
    }
}

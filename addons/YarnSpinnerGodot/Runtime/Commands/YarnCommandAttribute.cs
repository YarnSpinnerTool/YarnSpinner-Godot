/*

The MIT License (MIT)

Copyright (c) 2015-2017 Secret Lab Pty. Ltd. and Yarn Spinner contributors.

Permission is hereby granted, free of charge, to any person obtaining a
copy of this software and associated documentation files (the "Software"),
to deal in the Software without restriction, including without limitation
the rights to use, copy, modify, merge, publish, distribute, sublicense,
and/or sell copies of the Software, and to permit persons to whom the
Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
DEALINGS IN THE SOFTWARE.

*/


using System.Collections;
using System;
using System.ComponentModel;

namespace Yarn.GodotIntegration
{
    #region Class/Interface

    /// <summary>
    /// An attribute that marks a method on an object as a command.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When a <see cref="DialogueRunner"/> receives a <see cref="Command"/>,
    /// and no command handler has been installed for the command, it splits it
    /// by spaces, and then checks to see if the second word, if any, is the
    /// name of an object.
    /// </para>
    /// <para>
    /// By default, it checks for any <see cref="GameObject"/>s in the scene. If
    /// one is found, it is checked to see if any of the <see
    /// cref="Godot.Node"/>s attached to the class has a <see
    /// cref="YarnCommandAttribute"/> whose <see
    /// cref="YarnCommandAttribute.CommandString"/> matching the first word of
    /// the command.
    /// </para>
    /// <para>If the method is static, it will not try to inject an
    /// object.</para>
    /// <para>If a method is found, its parameters are checked:</para>
    /// <list type="bullet">
    /// <item>
    /// If the method takes a single <see cref="string"/>[] parameter, the
    /// method is called, and will be passed an array containing all words in
    /// the command after the first two.
    /// </item>
    /// <item>
    /// If the method takes a number of parameters equal to the number of words
    /// in the command after the first two, it will be called with those words
    /// as parameters.
    /// </item>
    /// <item>
    /// If a parameter is a <see cref="GameObject"/>, we look up the object
    /// using <see cref="GameObject.Find(string)"/>. As per the API, the game
    /// object must be active.
    /// </item>
    /// <item>
    /// If a parameter is assignable to <see cref="Component"/>, we will locate
    /// the component based on the name of the object. As per the API of <see
    /// cref="GameObject.Find(string)"/>, the game object must be active. If
    /// you'd like to have a custom injector for a parameter, use the <see
    /// cref="YarnParameterAttribute"/>.
    /// </item>
    /// <item>
    /// If a parameter is a <see cref="bool"/>, the string must be <c>true</c>
    /// or <c>false</c> (as defined by the standard converter for <see
    /// cref="string"/> to <see cref="bool"/>). However, we also allow for the
    /// string to equal the parameter name, case insensitive. This allows us to
    /// write commands with more self-documenting parameters, eg for a certain
    /// <c>Move(bool wait)</c>, you could write <c>&lt;&lt;move wait&gt;&gt;</c>
    /// instead of <c>&lt;&lt;move true&gt;&gt;</c>.
    /// </item>
    /// <item>
    /// For any other type, we will attempt to convert using <see
    /// cref="Convert.ChangeType(object, Type, IFormatProvider)"/> using the
    /// <see cref="System.Globalization.CultureInfo.InvariantCulture"/> culture.
    /// This means that you can implement <see cref="IConvertible"/> to add new
    /// accepted types. (Do be aware that it's a non-CLS compliant interface,
    /// according to its docs. Mono for Unity seems to implement it, but you may
    /// have trouble if you use any other CLS implementation.)
    /// Godot note: untested in Godot.
    /// </item>
    /// <item>Otherwise, it will not be called, and a warning will be
    /// issued.</item>
    /// </list>
    /// <para>This attribute may be attached to a coroutine. </para>
    /// <para style="note">
    /// The <see cref="DialogueRunner"/> determines if the method is a coroutine
    /// if the method returns <see cref="IEnumerator"/>. 
    /// </para>
    /// <para>
    /// If the method is a coroutine, the DialogueRunner will pause execution
    /// until the coroutine ends.
    /// </para>
    /// </remarks>
    public partial class YarnCommandAttribute : YarnActionAttribute
    {
        [Obsolete("Use " + nameof(Name) + " instead.")]
        public string CommandString {
            get => Name;
            set => Name = value;
        }

        /// <summary>
        /// Override the state injector for this command only.
        /// </summary>
        /// <remarks>
        /// <para>
        /// If none of those conditions are true, but the function is not
        /// static, an error will be thrown. However, if the function is indeed
        /// static, this parameter will be ignored.
        /// </para>
        /// </remarks>
        public string Injector { get; set; }

        public YarnCommandAttribute(string name = null) => Name = name;
    }
    #endregion
}

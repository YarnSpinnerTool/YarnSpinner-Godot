using System.Collections.Generic;
using Godot;

namespace YarnSpinnerGodot
{
    /// <summary>
    /// A <see cref="Godot.Node"/> that produces <see
    /// cref="LocalizedLine"/>s, for use in Dialogue Views.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="DialogueRunner"/>s use a <see
    /// cref="LineProviderBehaviour"/> to get <see cref="LocalizedLine"/>s,
    /// which contain the localized information that <see
    /// cref="DialogueViewBase"/> classes use to present content to the
    /// player. 
    /// </para>
    /// <para>
    /// Subclasses of this abstract class may return subclasses of <see
    /// cref="LocalizedLine"/>. 
    /// </para>
    /// </remarks>
    /// <seealso cref="DialogueViewBase"/>
    public abstract partial class LineProviderBehaviour : Godot.Node
    {
        /// <summary>
        /// Prepares and returns a <see cref="LocalizedLine"/> from the
        /// specified <see cref="Yarn.Line"/>.
        /// </summary>
        /// <remarks>
        /// This method should not be called if <see
        /// cref="LinesAvailable"/> returns <see langword="false"/>.
        /// </remarks>
        /// <param name="line">The <see cref="Yarn.Line"/> to produce the
        /// <see cref="LocalizedLine"/> from.</param>
        /// <returns>A localized line, ready to be presented to the
        /// player.</returns>
        public abstract LocalizedLine GetLocalizedLine(Yarn.Line line);

        /// <summary>
        /// The YarnProject that contains the localized data for lines.
        /// </summary>
        /// <remarks>This property is set at run-time by the object that
        /// will be requesting content (typically a <see
        /// cref="DialogueRunner"/>).
        public YarnProject YarnProject { get; set; }

        /// <summary>
        /// Signals to the line provider that lines with the provided line
        /// IDs may be presented shortly.        
        /// </summary>
        /// <remarks>
        /// <para>
        /// Subclasses of <see cref="LineProviderBehaviour"/> can override
        /// this to prepare any neccessary resources needed to present
        /// these lines, like pre-loading voice-over audio. The default
        /// implementation does nothing.
        /// </para>
        /// <para style="info">
        /// Not every line may run; this method serves as a way to give the
        /// line provider advance notice that a line <i>may</i> run, not <i>will</i>
        /// run.
        /// </para>
        /// <para>
        /// When this method is run, the value returned by the <see
        /// cref="LinesAvailable"/> property should change to false until the
        /// necessary resources have loaded.
        /// </para>
        /// </remarks>
        /// <param name="lineIDs">A collection of line IDs that the line
        /// provider should prepare for.</param>
        public virtual void PrepareForLines(IEnumerable<string> lineIDs)
        {
            // No-op by default.
        }

        /// <summary>
        /// Gets a value indicating whether this line provider is ready to
        /// provide <see cref="LocalizedLine"/> objects. The default
        /// implementation returns <see langword="true"/>.
        /// </summary>
        /// <remarks>
        /// Subclasses should return <see langword="false"/> when the
        /// required resources needed to deliver lines are not yet ready,
        /// and <see langword="true"/> when they are.
        /// </remarks>
        public virtual bool LinesAvailable => true;

        /// <summary>
        /// Gets the user's current locale identifier, as a BCP-47 code.
        /// </summary>
        /// <remarks>
        /// This value is used by the <see cref="DialogueRunner"/> to control
        /// how certain replacement markers behave (for example, the
        /// <c>[plural]</c> marker, which behaves differently depending on the
        /// user's locale.)
        /// </remarks>
        public abstract string LocaleCode { get; }
        
    }
    
}

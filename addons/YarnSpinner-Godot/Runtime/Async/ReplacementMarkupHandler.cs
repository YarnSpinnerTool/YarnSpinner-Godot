/*
Yarn Spinner is licensed to you under the terms found in the file LICENSE.md.
*/

using System.Collections.Generic;
using System.Text;
using Godot;
using Yarn.Markup;

namespace YarnSpinnerGodot;

/// <summary>
/// An attribute marker processor receives a marker found in a Yarn line,
/// and optionally rewrites the marker and its children into a new form.
/// </summary>
/// <seealso cref="LineProviderBehaviour"/>
public abstract partial class ReplacementMarkupHandler : Node, IAttributeMarkerProcessor
{


    /// <inheritdoc/>
    public abstract ReplacementMarkerResult ProcessReplacementMarker(MarkupAttribute marker,
         StringBuilder childBuilder, List<MarkupAttribute> childAttributes, string localeCode);
}
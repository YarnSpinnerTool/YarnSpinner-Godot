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
    /// <summary>
    /// An empty collection of diagnostics.
    /// </summary>
    public static readonly List<LineParser.MarkupDiagnostic> NoDiagnostics = [];

    /// <inheritdoc/>
    public abstract List<LineParser.MarkupDiagnostic> ProcessReplacementMarker(MarkupAttribute marker,
        StringBuilder childBuilder, List<MarkupAttribute> childAttributes, string localeCode);
}
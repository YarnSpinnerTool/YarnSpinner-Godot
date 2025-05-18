using System.Collections;
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
public abstract partial class AttributeMarkerProcessor : Node, IAttributeMarkerProcessor
{
    /// <summary>
    /// An empty collection of diagnostics.
    /// </summary>
    public static readonly List<LineParser.MarkupDiagnostic> NoDiagnostics = new List<LineParser.MarkupDiagnostic>();

    // TODO: Check with Tim about specific details of docs here
    public abstract List<LineParser.MarkupDiagnostic> ProcessReplacementMarker(Yarn.Markup.MarkupAttribute  marker, StringBuilder childBuilder, List<Yarn.Markup.MarkupAttribute> childAttributes, string localeCode);
    
}
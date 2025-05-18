namespace YarnSpinnerGodot.Generated;

/// <summary>
/// Partial class so that we can reference the generated ActionRegistration class,
/// without it potentially being generated yet.
///
/// If source generation is disabled, this will do nothing, otherwise it will trigger command
/// and function registrations when this class is referenced. 
/// </summary>
public partial class ActionRegistration
{
    public static void Touch()
    {
        // Just a way to reference this class, so that the generated version of the ActionRegistration class 
        // will have its static constructor called. It's hacky, but there is no [RuntimeInitializeOnLoadMethod] 
        // attribute in Godot like there is in Unity, so we have to have it called somehow. 
        
    }
}
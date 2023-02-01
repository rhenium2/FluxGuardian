using FluxGuardian.Models;

namespace FluxGuardian.Helpers;

public class CommandDescriptionAttribute : Attribute
{
    public string Name { get; set; }
    public string Description { get; set; }
    public KeyValuePair<ContextKind, string> Examples { get; set; }
}

public class CommandExampleAttribute : Attribute
{
    public ContextKind ContextKind { get; set; }
    public string Example { get; set; }
}
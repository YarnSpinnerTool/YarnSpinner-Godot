namespace YarnDonut.Editor;
#if TOOLS
using System.Linq;

public class OrderedContractResolver : Newtonsoft.Json.Serialization.DefaultContractResolver
{
    protected override System.Collections.Generic.IList<Newtonsoft.Json.Serialization.JsonProperty>
        CreateProperties(System.Type type, Newtonsoft.Json.MemberSerialization memberSerialization)
    {
        var @base = base.CreateProperties(type, memberSerialization);
        var ordered = @base
            .OrderBy(p => p.Order ?? int.MaxValue)
            .ThenBy(p => p.PropertyName)
            .ToList();
        return ordered;
    }
}

#endif
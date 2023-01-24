using FluxGuardian.FluxApi.SDK;

namespace FluxGuardian.Helpers;

public class Constants
{
    public const int MaximumNodeCount = 4;

    //public static int[] FluxApiPorts = new[] { 16127, 16137, 16147, 16157, 16167, 16177, 16187, 16197 };

    public static FluxPortSet DefaultPortSet = new FluxPortSet(16126, 16127, 16129);

    public static Dictionary<int, FluxPortSet> FluxPortSets = new Dictionary<int, FluxPortSet>(new[]
    {
        new KeyValuePair<int, FluxPortSet>(16126, DefaultPortSet),
        new KeyValuePair<int, FluxPortSet>(16136, new FluxPortSet(16136, 16137, 16139)),
        new KeyValuePair<int, FluxPortSet>(16146, new FluxPortSet(16146, 16147, 16149)),
        new KeyValuePair<int, FluxPortSet>(16156, new FluxPortSet(16156, 16157, 16159)),
        new KeyValuePair<int, FluxPortSet>(16166, new FluxPortSet(16166, 16167, 16169)),
        new KeyValuePair<int, FluxPortSet>(16176, new FluxPortSet(16176, 16177, 16179)),
        new KeyValuePair<int, FluxPortSet>(16186, new FluxPortSet(16186, 16187, 16189)),
        new KeyValuePair<int, FluxPortSet>(16196, new FluxPortSet(16196, 16197, 16199))
    });
}
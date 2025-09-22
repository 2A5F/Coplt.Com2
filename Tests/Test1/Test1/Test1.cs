using System.Runtime.InteropServices;
using Coplt.Com;

namespace Test1;

[Interface, Guid("c523bd17-e326-446c-8aab-c4e40774531a")]
public unsafe partial struct ITest1
{
    public readonly partial uint Add(uint a, uint b);
}

[Interface(typeof(ITest1)), Guid("e6ea2c14-564f-47f8-9a62-7a55446c1438")]
public unsafe partial struct ITest2
{
    public readonly partial uint Sub(uint a, uint b);
    
    public readonly partial uint Foo { get; set; }
    
    public readonly partial uint Foo2 { get; }
    
    public readonly partial uint Foo3 { set; }

    public partial void Some();
}

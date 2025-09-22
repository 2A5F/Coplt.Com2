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

[Interface(typeof(ITest2)), Guid("e785d2ba-cc37-48c6-b2fb-f253a21d0431")]
public unsafe partial struct ITest3
{
    public partial Struct2<int>* Some1(Struct1 a, Enum1 b, Enum2 c);

    public partial void FnPtr(delegate* unmanaged[Cdecl]<int, int, int> fn);

    // public partial void Error(Action a);
    // public partial void Error(object a);
    // public partial void Error(Span<int> a);
    // public partial void Error(object* a);
    // public partial void Error(Span<int>* a);
    // public partial void Error(delegate* unmanaged[Cdecl]<object, int> fn);
    // public partial void Error(delegate* unmanaged[Cdecl]<Span<int>, int> fn);
}

[StructLayout(LayoutKind.Explicit)]
public struct Struct1
{
    [FieldOffset(0)]
    public int a;
}

public enum Enum1
{
    A,
    B,
    C,
}

[RefOnly]
public struct Struct2<T>
{
    public T a;
}

[Flags]
public enum Enum2
{
    None = 0,
    A = 1 << 0,
    B = 1 << 1,
    C = 1 << 2,
}

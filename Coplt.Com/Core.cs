using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Coplt.Com;

/// Maintains similar compatibility with Microsoft COM
[Interface, Guid("00000000-0000-0000-c000-000000000046")]
public unsafe partial struct IUnknown
{
    public readonly partial HResult QueryInterface([ComType<ConstPtr<Guid>>, In] Guid* guid, [Out] void** obj);
    public readonly partial uint AddRef();
    public readonly partial uint Release();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly HResult QueryInterface(in Guid guid, out void* obj)
    {
        fixed (Guid* p_guid = &guid)
        fixed (void** p_obj = &obj)
        {
            return QueryInterface(p_guid, p_obj);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly HResult QueryInterface<T>(out Rc<T> obj) where T : struct, IComInterface<IUnknown>
    {
        var r = QueryInterface(T.Guid, out var ptr);
        obj = new((T*)ptr);
        return r;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly Rc<T> TryCast<T>() where T : struct, IComInterface<IUnknown>
    {
        var r = QueryInterface(out Rc<T> obj);
        return r ? obj : default;
    }
}

using System.Runtime.CompilerServices;

namespace Coplt.Com;

public static unsafe class ComUtils
{
    public static T* AsPointer<T>(in T self) where T : struct, IComInterface => (T*)Unsafe.AsPointer(ref Unsafe.AsRef(in self));

    public static Guid GuidOf<T>() where T : IComInterface => T.Guid;

    public static Guid* GuidPtrOf<T>() where T : IComInterface => (Guid*)Unsafe.AsPointer(ref Unsafe.AsRef(in T.Guid));
}

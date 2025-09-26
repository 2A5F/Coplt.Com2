using System.Runtime.CompilerServices;
using InlineIL;
using static InlineIL.IL.Emit;

namespace Coplt.Com;

public static unsafe class ComUtils
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    // ReSharper disable once EntityNameCapturedOnly.Global
    public static T* AsPointer<T>(in T self)
    {
        Ldarg(nameof(self));
        Conv_U();
        Ret();
        throw IL.Unreachable();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Guid GuidOf<T>() where T : IComInterface => T.Guid;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Guid* GuidPtrOf<T>() where T : IComInterface => AsPointer(in T.Guid);
}

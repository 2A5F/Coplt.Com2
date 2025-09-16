using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Coplt.Com;

public unsafe struct Rc<T>(T* handle) : IEquatable<Rc<T>>, IDisposable
    where T : struct, IComInterface
{
    public T* Handle = handle;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly HResult QueryInterface(Guid* guid, void** obj) => ((IUnknown*)Handle)->QueryInterface(guid, obj);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly uint AddRef() => ((IUnknown*)Handle)->AddRef();
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly uint Release() => ((IUnknown*)Handle)->Release();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly HResult QueryInterface(in Guid guid, out void* obj) => ((IUnknown*)Handle)->QueryInterface(guid, out obj);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly HResult QueryInterface<U>(out Rc<U> obj) where U : struct, IComInterface<IUnknown>
        => ((IUnknown*)Handle)->QueryInterface<U>(out obj);

    public void Dispose()
    {
        var self = Move();
        if (!self) return;
        self.Release();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator true(Rc<T> rc) => rc.Handle != null;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator false(Rc<T> rc) => rc.Handle == null;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !(Rc<T> rc) => rc.Handle == null;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator bool(Rc<T> rc) => rc.Handle != null;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator T*(Rc<T> rc) => rc.Handle;

    public Rc<T> Move()
    {
        ref var self = ref Unsafe.As<Rc<T>, IntPtr>(ref this);
        var old = Interlocked.Exchange(ref self, 0);
        return Unsafe.BitCast<IntPtr, Rc<T>>(old);
    }

    public readonly Rc<T> Clone()
    {
        if (this) AddRef();
        return this;
    }

    public T* Leak() => Move().Handle;

    [UnscopedRef]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref T* GetPinnableReference() => ref Handle;

    public readonly ref T Ref
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => ref *Handle;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(Rc<T> other) => Handle == other.Handle;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override bool Equals(object? obj) => obj is Rc<T> other && Equals(other);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override int GetHashCode() => ((nuint)Handle).GetHashCode();
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(Rc<T> left, Rc<T> right) => left.Equals(right);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(Rc<T> left, Rc<T> right) => !left.Equals(right);

    public override string ToString() => $"Rc<{typeof(T).Name}>(0x{(nuint)Handle:X})";
}

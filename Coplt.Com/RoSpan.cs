using System.Runtime.CompilerServices;

namespace Coplt.Com;

public unsafe struct NRoSpan<T> : IEquatable<NRoSpan<T>>
{
    public T* Data;
    public nuint Size;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public NRoSpan(T* data, UIntPtr size)
    {
        Data = data;
        Size = size;
    }

    public ReadOnlySpan<T> AsSpan
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new(Data, (int)Size);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(NRoSpan<T> other) => Data == other.Data && Size == other.Size;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override bool Equals(object? obj) => obj is NRoSpan<T> other && Equals(other);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override int GetHashCode() => HashCode.Combine((nuint)Data, Size);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(NRoSpan<T> left, NRoSpan<T> right) => left.Equals(right);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(NRoSpan<T> left, NRoSpan<T> right) => !left.Equals(right);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator true(NRoSpan<T> span) => span.Data != null;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator false(NRoSpan<T> span) => span.Data == null;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !(NRoSpan<T> span) => span.Data == null;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator bool(NRoSpan<T> span) => span.Data != null;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator ReadOnlySpan<T>(NRoSpan<T> span) => span.AsSpan;

    public bool IsEmpty
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Size == 0;
    }

    public int Length
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (int)Size;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T* At(int index) => &Data[index];

    public ref readonly T this[int index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => ref Data[index];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref readonly T GetPinnableReference() => ref Data[0];

    public override string ToString()
    {
        if (typeof(T) == typeof(byte)) return AsSpan.ToString();
        return $"NRoSpan<{typeof(T).Name}>[{Size}]";
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public NRoSpan<T> Slice(int start)
    {
        if ((uint)start > Size) throw new ArgumentOutOfRangeException(nameof(start));
        return new(Data + start, this.Size - (uint)start);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public NRoSpan<T> Slice(int start, int length)
    {
        if ((nuint)(uint)start + (uint)length > Size) throw new ArgumentOutOfRangeException();
        return new(Data + start, (uint)length);
    }

    public ReadOnlySpan<T>.Enumerator GetEnumerator() => AsSpan.GetEnumerator();
}

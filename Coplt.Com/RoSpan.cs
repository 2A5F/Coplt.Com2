using System.Runtime.CompilerServices;

namespace Coplt.Com;

public unsafe struct NRoSpan<T> : IEquatable<NRoSpan<T>>
{
    public T* Data;
    public nuint Size;

    public NRoSpan(T* data, UIntPtr size)
    {
        Data = data;
        Size = size;
    }

    public ReadOnlySpan<T> AsSpan => new(Data, (int)Size);

    public bool Equals(NRoSpan<T> other) => Data == other.Data && Size == other.Size;
    public override bool Equals(object? obj) => obj is NRoSpan<T> other && Equals(other);
    public override int GetHashCode() => HashCode.Combine((nuint)Data, Size);
    public static bool operator ==(NRoSpan<T> left, NRoSpan<T> right) => left.Equals(right);
    public static bool operator !=(NRoSpan<T> left, NRoSpan<T> right) => !left.Equals(right);

    public static bool operator true(NRoSpan<T> span) => span.Data != null;
    public static bool operator false(NRoSpan<T> span) => span.Data == null;
    public static bool operator !(NRoSpan<T> span) => span.Data == null;

    public static implicit operator bool(NRoSpan<T> span) => span.Data != null;
    public static implicit operator ReadOnlySpan<T>(NRoSpan<T> span) => span.AsSpan;

    public bool IsEmpty => Size == 0;

    public int Length => (int)Size;

    public T* At(int index) => &Data[index];

    public ref readonly T this[int index] => ref Data[index];

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

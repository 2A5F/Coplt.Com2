using System.Runtime.CompilerServices;

namespace Coplt.Com;

public unsafe struct NSpan<T> : IEquatable<NSpan<T>>
{
    public T* Data;
    public nuint Size;

    public NSpan(T* data, UIntPtr size)
    {
        Data = data;
        Size = size;
    }

    public Span<T> AsSpan => new(Data, (int)Size);

    public bool Equals(NSpan<T> other) => Data == other.Data && Size == other.Size;
    public override bool Equals(object? obj) => obj is NSpan<T> other && Equals(other);
    public override int GetHashCode() => HashCode.Combine((nuint)Data, Size);
    public static bool operator ==(NSpan<T> left, NSpan<T> right) => left.Equals(right);
    public static bool operator !=(NSpan<T> left, NSpan<T> right) => !left.Equals(right);

    public static bool operator true(NSpan<T> span) => span.Data != null;
    public static bool operator false(NSpan<T> span) => span.Data == null;
    public static bool operator !(NSpan<T> span) => span.Data == null;

    public static implicit operator bool(NSpan<T> span) => span.Data != null;
    public static implicit operator Span<T>(NSpan<T> span) => span.AsSpan;
    public static implicit operator ReadOnlySpan<T>(NSpan<T> span) => span.AsSpan;
    public static implicit operator NRoSpan<T>(NSpan<T> span) => new(span.Data, span.Size);

    public bool IsEmpty => Size == 0;

    public int Length => (int)Size;

    public T* At(int index) => &Data[index];

    public ref T this[int index] => ref Data[index];

    public ref T GetPinnableReference() => ref Data[0];

    public override string ToString()
    {
        if (typeof(T) == typeof(byte)) return AsSpan.ToString();
        return $"NSpan<{typeof(T).Name}>[{Size}]";
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public NSpan<T> Slice(int start)
    {
        if ((uint)start > Size) throw new ArgumentOutOfRangeException(nameof(start));
        return new(Data + start, this.Size - (uint)start);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public NSpan<T> Slice(int start, int length)
    {
        if ((nuint)(uint)start + (uint)length > Size) throw new ArgumentOutOfRangeException();
        return new(Data + start, (uint)length);
    }

    public Span<T>.Enumerator GetEnumerator() => AsSpan.GetEnumerator();
}

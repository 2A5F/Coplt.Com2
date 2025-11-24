using System.Runtime.CompilerServices;
using System.Text;

namespace Coplt.Com;

public unsafe struct Str8 : IEquatable<Str8>
{
    [ComType<ConstPtr<byte>>]
    public byte* Data;
    public uint Size;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Str8(byte* data, uint size)
    {
        Data = data;
        Size = size;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(Str8 other) => Data == other.Data && Size == other.Size;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override bool Equals(object? obj) => obj is Str8 other && Equals(other);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override int GetHashCode() => HashCode.Combine((nuint)Data, Size);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(Str8 left, Str8 right) => left.Equals(right);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(Str8 left, Str8 right) => !left.Equals(right);

    public override string ToString() => Encoding.UTF8.GetString(Data, (int)Size);

    public Span<byte> AsSpan
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new(Data, (int)Size);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator true(Str8 str) => str.Data != null;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator false(Str8 str) => str.Data == null;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !(Str8 str) => str.Data == null;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator bool(Str8 str) => str.Data != null;

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
    public byte* At(int index) => &Data[index];

    public ref byte this[int index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => ref Data[index];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref byte GetPinnableReference() => ref Data[0];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Str8 Slice(int start)
    {
        if ((uint)start > Size) throw new ArgumentOutOfRangeException(nameof(start));
        return new(Data + start, this.Size - (uint)start);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Str8 Slice(int start, int length)
    {
        if ((ulong)(uint)start + (uint)length > Size) throw new ArgumentOutOfRangeException();
        return new(Data + start, (uint)length);
    }

    public Span<byte>.Enumerator GetEnumerator() => AsSpan.GetEnumerator();
}

public unsafe struct Str16 : IEquatable<Str16>
{
    [ComType<ConstPtr<char>>]
    public char* Data;
    public uint Size;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Str16(char* data, uint size)
    {
        Data = data;
        Size = size;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(Str16 other) => Data == other.Data && Size == other.Size;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override bool Equals(object? obj) => obj is Str8 other && Equals(other);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override int GetHashCode() => HashCode.Combine((nuint)Data, Size);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(Str16 left, Str16 right) => left.Equals(right);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(Str16 left, Str16 right) => !left.Equals(right);

    public override string ToString() => new(Data, 0, (int)Size);

    public Span<char> AsSpan
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new(Data, (int)Size);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator true(Str16 str) => str.Data != null;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator false(Str16 str) => str.Data == null;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !(Str16 str) => str.Data == null;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator bool(Str16 str) => str.Data != null;

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
    public char* At(int index) => &Data[index];

    public ref char this[int index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => ref Data[index];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref char GetPinnableReference() => ref Data[0];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Str16 Slice(int start)
    {
        if ((uint)start > Size) throw new ArgumentOutOfRangeException(nameof(start));
        return new(Data + start, this.Size - (uint)start);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Str16 Slice(int start, int length)
    {
        if ((ulong)(uint)start + (uint)length > Size) throw new ArgumentOutOfRangeException();
        return new(Data + start, (uint)length);
    }

    public Span<char>.Enumerator GetEnumerator() => AsSpan.GetEnumerator();
}

public enum StrKind : byte
{
    Str8,
    Str16,
}

public unsafe struct StrAny : IEquatable<StrAny>
{
    public void* Data;
    public uint Size;
    public StrKind Kind;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public StrAny(void* data, uint size, StrKind kind)
    {
        Data = data;
        Size = size;
        Kind = kind;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(StrAny other) => Data == other.Data && Size == other.Size;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override bool Equals(object? obj) => obj is Str8 other && Equals(other);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override int GetHashCode() => HashCode.Combine((nuint)Data, Size);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(StrAny left, StrAny right) => left.Equals(right);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(StrAny left, StrAny right) => !left.Equals(right);

    public override string ToString() => Kind switch
    {
        StrKind.Str8 => Encoding.UTF8.GetString((byte*)Data, (int)Size),
        StrKind.Str16 => new string((char*)Data, 0, (int)Size),
        _ => throw new ArgumentOutOfRangeException()
    };

    public Span<byte> AsSpan
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new(Data, Length);
    }

    public Str8 TryStr8
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Kind is StrKind.Str8 ? new((byte*)Data, Size) : default;
    }
    public Str16 TryStr16
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Kind is StrKind.Str16 ? new((char*)Data, Size) : default;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator true(StrAny str) => str.Data != null;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator false(StrAny str) => str.Data == null;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !(StrAny str) => str.Data == null;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator bool(StrAny str) => str.Data != null;

    public bool IsEmpty
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Size == 0;
    }

    public int Length
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (int)(Kind switch
        {
            StrKind.Str8 => Size,
            StrKind.Str16 => Size * 2,
            _ => throw new ArgumentOutOfRangeException()
        });
    }

    public char this[int index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Kind switch
        {
            StrKind.Str8 => (char)((byte*)Data)[index],
            StrKind.Str16 => ((char*)Data)[index],
            _ => throw new ArgumentOutOfRangeException()
        };
    }
}

public static unsafe class StrExtensions
{
    extension(StrKind kind)
    {
        public string GetString(void* ptr, int len) => kind.GetString(ptr, (uint)len);
        public string GetString(void* ptr, uint len) => kind switch
        {
            StrKind.Str8 => new Str8((byte*)ptr, len).ToString(),
            StrKind.Str16 => new Str16((char*)ptr, len).ToString(),
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
        };
    }
}

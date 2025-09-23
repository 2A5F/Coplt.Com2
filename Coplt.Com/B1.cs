namespace Coplt.Com;

public record struct B1(byte Value)
{
    public static implicit operator bool(B1 b) => b.Value != 0;
    public static implicit operator B1(bool v) => new(v ? (byte)1 : (byte)0);

    public static bool operator true(B1 b) => b.Value != 0;
    public static bool operator false(B1 b) => b.Value == 0;

    public static bool operator !(B1 b) => b.Value != 0;

    public override string ToString() => Value == 0 ? "false" : "true";
}

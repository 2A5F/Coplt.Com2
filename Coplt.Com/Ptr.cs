namespace Coplt.Com;

[Transparent]
public unsafe struct Ptr<T> : IEquatable<Ptr<T>>
{
    public T* _forward;

    public static implicit operator Ptr<T>(T* ptr) => new() { _forward = ptr };
    public static implicit operator T*(Ptr<T> self) => self._forward;
    
    public bool Equals(Ptr<T> other) => _forward == other._forward;
    public override bool Equals(object? obj) => obj is Ptr<T> other && Equals(other);
    public override int GetHashCode() => unchecked((int)(long)_forward);
    public static bool operator ==(Ptr<T> left, Ptr<T> right) => left.Equals(right);
    public static bool operator !=(Ptr<T> left, Ptr<T> right) => !left.Equals(right);
}

[Transparent]
public unsafe struct ConstPtr<T> : IEquatable<ConstPtr<T>>
{
    public T* _forward;
    
    public static implicit operator ConstPtr<T>(T* ptr) => new() { _forward = ptr };
    public static implicit operator T*(ConstPtr<T> self) => self._forward;
    
    public bool Equals(ConstPtr<T> other) => _forward == other._forward;
    public override bool Equals(object? obj) => obj is ConstPtr<T> other && Equals(other);
    public override int GetHashCode() => unchecked((int)(long)_forward);
    public static bool operator ==(ConstPtr<T> left, ConstPtr<T> right) => left.Equals(right);
    public static bool operator !=(ConstPtr<T> left, ConstPtr<T> right) => !left.Equals(right);
}

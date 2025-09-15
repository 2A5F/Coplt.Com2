namespace Coplt.Com;

public unsafe struct NonNull<T>
{
    public T* Ptr;
}

[Transparent]
public struct ConstNonNull<T>
{
    public NonNull<T> _forward;
}

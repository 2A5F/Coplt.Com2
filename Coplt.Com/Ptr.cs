namespace Coplt.Com;

[Transparent]
public unsafe struct Ptr<T>
{
    public T* _forward;
}

[Transparent]
public unsafe struct ConstPtr<T>
{
    public T* _forward;
}

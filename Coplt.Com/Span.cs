namespace Coplt.Com;

public unsafe struct NSpan<T>
{
    public T* data;
    public nuint size;
}

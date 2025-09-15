using System.Runtime.CompilerServices;

namespace Coplt.Com;

public interface IComInterface
{
    public static abstract ref readonly Guid Guid { get; }
}

public interface IComInterface<T> : IComInterface
    where T : IComInterface<T>;

public static unsafe class ComInterfaceExtensions
{
    extension<T>(T) where T : IComInterface
    {
        public static ref readonly Guid Guid => ref T.Guid;

        public static Guid* GuidPtr => ComUtils.GuidPtrOf<T>();
    }
}

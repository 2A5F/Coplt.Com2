using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Coplt.Com;

public enum HRESULT : uint
{
    Ok = 0,
    NotImpl = 0x80004001,
    NoInterface = 0x80004002,
    Pointer = 0x80004003,
    Abort = 0x80004004,
    Fail = 0x80004005,
    Unexpected = 0x8000FFFF,
    AccessDenied = 0x80070005,
    Handle = 0x80070006,
    OutOfMemory = 0x8007000E,
    InvalidArg = 0x80070057,
}

[ComMarshalAs(ComUnmanagedType.I32)]
public readonly record struct HResult(HRESULT Value)
{
    public readonly HRESULT Value = Value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public HResult(int value) : this((HRESULT)value) { }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public HResult(uint value) : this((HRESULT)value) { }

    public Exception ToException() => Marshal.GetExceptionForHR((int)Value) ?? new Win32Exception((int)Value);

    [StackTraceHidden]
    public void TryThrow()
    {
        if (IsSuccess) return;
        throw ToException();
    }

    public override string ToString() => Marshal.GetExceptionForHR((int)Value)?.Message ?? Marshal.GetPInvokeErrorMessage((int)Value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator int(HResult value) => (int)value.Value;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator HResult(int value) => new(value);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator HResult(HRESULT value) => new(value);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator HRESULT(HResult value) => value.Value;

    public bool IsSuccess
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (int)Value >= 0;
    }
    public bool IsFailure
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (int)Value < 0;
    }
    public bool IsError
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => ((uint)Value >> 31) == 1;
    }

    public int Code
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (int)Value & 0xFFFF;
    }
    public int Facility
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => ((int)Value >> 16) & 0x1FFF;
    }
    public int Severity
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => ((int)Value >> 31) & 1;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator true(HResult hr) => hr.IsSuccess;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator false(HResult hr) => hr.IsFailure;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !(HResult hr) => hr.IsFailure;
}

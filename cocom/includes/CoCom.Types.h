#pragma once
#ifndef COPLT_COM_TYPES_H
#define COPLT_COM_TYPES_H

#include <cstdint>

namespace Coplt
{
    namespace Types
    {
        using b8 = int8_t;
        using b32 = int32_t;

        using u8 = uint8_t;
        using u16 = uint16_t;
        using u32 = uint32_t;
        using u64 = uint64_t;
        using usize = size_t;
        using isize = ptrdiff_t;

        using i8 = int8_t;
        using i16 = int16_t;
        using i32 = int32_t;
        using i64 = int64_t;

        using f32 = float;
        using f64 = double;

        using char8 = char;
#ifdef _WINDOWS
        using char16 = wchar_t;
        using char32 = char32_t;
#else
        using char16 = char16_t;
        using char32 = wchar_t;
#endif

        using wchar = wchar_t;

        using Char8 = char8_t;
        using Char16 = char16_t;
        using Char32 = char32_t;

        template <class R, class... Args>
        using Func = R(Args...);
    }

    using namespace Types;
}

#endif //COPLT_COM_TYPES_H

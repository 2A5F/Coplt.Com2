#pragma once
#ifndef Test1_TYPES_H
#define Test1_TYPES_H

#include "CoCom.h"

namespace Test1 {

    enum class Enum1 : ::Coplt::i32
    {
        A = 0,
        B = 1,
        C = 2,
    };

    enum class Enum2 : ::Coplt::i32
    {
        None = 0,
        A = 1,
        B = 2,
        C = 4,
    };

    union Struct1;

    template <class T0 /* T */>
    struct Struct2;

    union Struct1
    {
        ::Coplt::i32 a;
    };

    template <class T0 /* T */>
    struct Struct2
    {
        T0 a;
    };

} // namespace Test1

#endif //Test1_TYPES_H

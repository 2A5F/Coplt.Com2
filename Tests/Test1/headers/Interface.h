#pragma once
#ifndef Test1_INTERFACE_H
#define Test1_INTERFACE_H

#include "CoCom.h"
#include "./Types.h"
#include "./Details.h"

namespace Test1 {

    COPLT_COM_INTERFACE(ITest1, "c523bd17-e326-446c-8aab-c4e40774531a", ::Test1::IUnknown)
    {
        COPLT_COM_INTERFACE_BODY_Test1_ITest1

        COPLT_COM_METHOD(Add, ::Coplt::u32, (::Coplt::u32 a, ::Coplt::u32 b) const, a, b)
    };

    COPLT_COM_INTERFACE(ITest2, "e6ea2c14-564f-47f8-9a62-7a55446c1438", ::Test1::ITest1)
    {
        COPLT_COM_INTERFACE_BODY_Test1_ITest2

        COPLT_COM_METHOD(Sub, ::Coplt::u32, (::Coplt::u32 a, ::Coplt::u32 b) const, a, b)
        COPLT_COM_METHOD(get_Foo, ::Coplt::u32, () const)
        COPLT_COM_METHOD(set_Foo, void, (::Coplt::u32 value) const, value)
        COPLT_COM_METHOD(get_Foo2, ::Coplt::u32, () const)
        COPLT_COM_METHOD(set_Foo3, void, (::Coplt::u32 value) const, value)
        COPLT_COM_METHOD(Some, void, ())
    };

    COPLT_COM_INTERFACE(ITest3, "e785d2ba-cc37-48c6-b2fb-f253a21d0431", ::Test1::ITest2)
    {
        COPLT_COM_INTERFACE_BODY_Test1_ITest3

        COPLT_COM_METHOD(Some1, ::Test1::Struct2<::Coplt::i32>*, (::Test1::Struct1 a, ::Test1::Enum1 b, ::Test1::Enum2 c), a, b, c)
        COPLT_COM_METHOD(FnPtr, void, (::Coplt::Func<::Coplt::i32, ::Coplt::i32, ::Coplt::i32>* fn), fn)
    };

} // namespace Test1

#endif //Test1_INTERFACE_H

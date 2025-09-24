#pragma once
#ifndef Test1_DETAILS_H
#define Test1_DETAILS_H

#include "CoCom.h"
#include "./Types.h"

namespace Test1 {

    using IUnknown = ::Coplt::IUnknown;
    using IWeak = ::Coplt::IWeak;

    struct ITest1;
    struct ITest2;
    struct ITest3;

} // namespace Test1

template <>
struct ::Coplt::Internal::VirtualTable<::Test1::ITest1>
{
    VirtualTable<::Test1::IUnknown> b;
    ::Coplt::u32 (*const f_Add)(const ::Test1::ITest1*, ::Coplt::u32 a, ::Coplt::u32 b);
};

template <>
struct ::Coplt::Internal::ComProxy<::Test1::ITest1>
{
    using VirtualTable = VirtualTable<::Test1::ITest1>;

    static COPLT_FORCE_INLINE constexpr inline const ::Coplt::Guid& get_Guid()
    {
        static ::Coplt::Guid s_guid("c523bd17-e326-446c-8aab-c4e40774531a");
        return s_guid;
    }

    template <class Self>
    COPLT_FORCE_INLINE
    static HResult QueryInterface(const Self* self, const ::Coplt::Guid& guid, COPLT_OUT void*& object)
    {
        if (guid == guid_of<::Test1::ITest1>())
        {
            object = const_cast<void*>(static_cast<const void*>(static_cast<const ::Test1::ITest1*>(self)));
            return ::Coplt::HResultE::Ok;
        }
        return ComProxy<::Test1::IUnknown>::QueryInterface(self, guid, object);
    }

    template <std::derived_from<::Test1::ITest1> Base = ::Test1::ITest1>
    struct Proxy : ComProxy<::Test1::IUnknown>::Proxy<Base>
    {
        using Super = ComProxy<::Test1::IUnknown>::Proxy<Base>;
        using Self = Proxy;

    protected:
        virtual ~Proxy() = default;

        COPLT_FORCE_INLINE
        static const VirtualTable& GetVtb()
        {
            static VirtualTable vtb
            {
                .b = Super::GetVtb(),
                .f_Add = [](const ::Test1::ITest1* self, ::Coplt::u32 p0, ::Coplt::u32 p1)
                {
                    return static_cast<const Self*>(self)->Impl_Add(p0, p1);
                },
            };
            return vtb;
        };

        explicit Proxy(const ::Coplt::Internal::VirtualTable<Base>* vtb) : Base(vtb) {}

        explicit Proxy() : Super(&GetVtb()) {}

        virtual ::Coplt::u32 Impl_Add(::Coplt::u32 a, ::Coplt::u32 b) const = 0;
    };
};

#define COPLT_COM_INTERFACE_BODY_Test1_ITest1\
    using Super = ::Test1::IUnknown;\
    using Self = ::Test1::ITest1;\
\
    explicit ITest1(const ::Coplt::Internal::VirtualTable<Self>* vtbl) : Super(&vtbl->b) {}

template <>
struct ::Coplt::Internal::VirtualTable<::Test1::ITest2>
{
    VirtualTable<::Test1::ITest1> b;
    ::Coplt::u32 (*const f_Sub)(const ::Test1::ITest2*, ::Coplt::u32 a, ::Coplt::u32 b);
    ::Coplt::u32 (*const f_get_Foo)(const ::Test1::ITest2*);
    void (*const f_set_Foo)(const ::Test1::ITest2*, ::Coplt::u32 value);
    ::Coplt::u32 (*const f_get_Foo2)(const ::Test1::ITest2*);
    void (*const f_set_Foo3)(const ::Test1::ITest2*, ::Coplt::u32 value);
    void (*const f_Some)(::Test1::ITest2*);
};

template <>
struct ::Coplt::Internal::ComProxy<::Test1::ITest2>
{
    using VirtualTable = VirtualTable<::Test1::ITest2>;

    static COPLT_FORCE_INLINE constexpr inline const ::Coplt::Guid& get_Guid()
    {
        static ::Coplt::Guid s_guid("e6ea2c14-564f-47f8-9a62-7a55446c1438");
        return s_guid;
    }

    template <class Self>
    COPLT_FORCE_INLINE
    static HResult QueryInterface(const Self* self, const ::Coplt::Guid& guid, COPLT_OUT void*& object)
    {
        if (guid == guid_of<::Test1::ITest2>())
        {
            object = const_cast<void*>(static_cast<const void*>(static_cast<const ::Test1::ITest2*>(self)));
            return ::Coplt::HResultE::Ok;
        }
        return ComProxy<::Test1::ITest1>::QueryInterface(self, guid, object);
    }

    template <std::derived_from<::Test1::ITest2> Base = ::Test1::ITest2>
    struct Proxy : ComProxy<::Test1::ITest1>::Proxy<Base>
    {
        using Super = ComProxy<::Test1::ITest1>::Proxy<Base>;
        using Self = Proxy;

    protected:
        virtual ~Proxy() = default;

        COPLT_FORCE_INLINE
        static const VirtualTable& GetVtb()
        {
            static VirtualTable vtb
            {
                .b = Super::GetVtb(),
                .f_Sub = [](const ::Test1::ITest2* self, ::Coplt::u32 p0, ::Coplt::u32 p1)
                {
                    return static_cast<const Self*>(self)->Impl_Sub(p0, p1);
                },
                .f_get_Foo = [](const ::Test1::ITest2* self)
                {
                    return static_cast<const Self*>(self)->Impl_get_Foo();
                },
                .f_set_Foo = [](const ::Test1::ITest2* self, ::Coplt::u32 p0)
                {
                    return static_cast<const Self*>(self)->Impl_set_Foo(p0);
                },
                .f_get_Foo2 = [](const ::Test1::ITest2* self)
                {
                    return static_cast<const Self*>(self)->Impl_get_Foo2();
                },
                .f_set_Foo3 = [](const ::Test1::ITest2* self, ::Coplt::u32 p0)
                {
                    return static_cast<const Self*>(self)->Impl_set_Foo3(p0);
                },
                .f_Some = [](::Test1::ITest2* self)
                {
                    return static_cast<const Self*>(self)->Impl_Some();
                },
            };
            return vtb;
        };

        explicit Proxy(const ::Coplt::Internal::VirtualTable<Base>* vtb) : Base(vtb) {}

        explicit Proxy() : Super(&GetVtb()) {}

        virtual ::Coplt::u32 Impl_Sub(::Coplt::u32 a, ::Coplt::u32 b) const = 0;
        virtual ::Coplt::u32 Impl_get_Foo() const = 0;
        virtual void Impl_set_Foo(::Coplt::u32 value) const = 0;
        virtual ::Coplt::u32 Impl_get_Foo2() const = 0;
        virtual void Impl_set_Foo3(::Coplt::u32 value) const = 0;
        virtual void Impl_Some() = 0;
    };
};

#define COPLT_COM_INTERFACE_BODY_Test1_ITest2\
    using Super = ::Test1::ITest1;\
    using Self = ::Test1::ITest2;\
\
    explicit ITest2(const ::Coplt::Internal::VirtualTable<Self>* vtbl) : Super(&vtbl->b) {}

template <>
struct ::Coplt::Internal::VirtualTable<::Test1::ITest3>
{
    VirtualTable<::Test1::ITest2> b;
    ::Test1::Struct2<::Coplt::i32>* (*const f_Some1)(::Test1::ITest3*, ::Test1::Struct1 a, ::Test1::Enum1 b, ::Test1::Enum2 c);
    void (*const f_FnPtr)(::Test1::ITest3*, ::Coplt::Func<::Coplt::i32, ::Coplt::i32, ::Coplt::i32>* fn);
};

template <>
struct ::Coplt::Internal::ComProxy<::Test1::ITest3>
{
    using VirtualTable = VirtualTable<::Test1::ITest3>;

    static COPLT_FORCE_INLINE constexpr inline const ::Coplt::Guid& get_Guid()
    {
        static ::Coplt::Guid s_guid("e785d2ba-cc37-48c6-b2fb-f253a21d0431");
        return s_guid;
    }

    template <class Self>
    COPLT_FORCE_INLINE
    static HResult QueryInterface(const Self* self, const ::Coplt::Guid& guid, COPLT_OUT void*& object)
    {
        if (guid == guid_of<::Test1::ITest3>())
        {
            object = const_cast<void*>(static_cast<const void*>(static_cast<const ::Test1::ITest3*>(self)));
            return ::Coplt::HResultE::Ok;
        }
        return ComProxy<::Test1::ITest2>::QueryInterface(self, guid, object);
    }

    template <std::derived_from<::Test1::ITest3> Base = ::Test1::ITest3>
    struct Proxy : ComProxy<::Test1::ITest2>::Proxy<Base>
    {
        using Super = ComProxy<::Test1::ITest2>::Proxy<Base>;
        using Self = Proxy;

    protected:
        virtual ~Proxy() = default;

        COPLT_FORCE_INLINE
        static const VirtualTable& GetVtb()
        {
            static VirtualTable vtb
            {
                .b = Super::GetVtb(),
                .f_Some1 = [](::Test1::ITest3* self, ::Test1::Struct1 p0, ::Test1::Enum1 p1, ::Test1::Enum2 p2)
                {
                    return static_cast<const Self*>(self)->Impl_Some1(p0, p1, p2);
                },
                .f_FnPtr = [](::Test1::ITest3* self, ::Coplt::Func<::Coplt::i32, ::Coplt::i32, ::Coplt::i32>* p0)
                {
                    return static_cast<const Self*>(self)->Impl_FnPtr(p0);
                },
            };
            return vtb;
        };

        explicit Proxy(const ::Coplt::Internal::VirtualTable<Base>* vtb) : Base(vtb) {}

        explicit Proxy() : Super(&GetVtb()) {}

        virtual ::Test1::Struct2<::Coplt::i32>* Impl_Some1(::Test1::Struct1 a, ::Test1::Enum1 b, ::Test1::Enum2 c) = 0;
        virtual void Impl_FnPtr(::Coplt::Func<::Coplt::i32, ::Coplt::i32, ::Coplt::i32>* fn) = 0;
    };
};

#define COPLT_COM_INTERFACE_BODY_Test1_ITest3\
    using Super = ::Test1::ITest2;\
    using Self = ::Test1::ITest3;\
\
    explicit ITest3(const ::Coplt::Internal::VirtualTable<Self>* vtbl) : Super(&vtbl->b) {}

#endif //Test1_DETAILS_H

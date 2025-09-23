#pragma once
#ifndef COPLT_COM_INTERFACE_H
#define COPLT_COM_INTERFACE_H

#include "CoCom.Macros.h"
#include "CoCom.Types.h"
#include "CoCom.Guid.h"

namespace Coplt
{
    template <class T>
    struct GuidOf
    {
    };

    template <class T>
    COPLT_FORCE_INLINE constexpr inline const Guid& guid_of() { return guid_of(GuidOf<T>{}); }
}

#define COPLT_INTERFACE_GUID_OF(NAME, ID) struct NAME;\
COPLT_FORCE_INLINE constexpr inline const ::Coplt::Guid& guid_of(const ::Coplt::GuidOf<NAME>&)\
{\
    static ::Coplt::Guid s_guid(ID);\
    return s_guid;\
}

#define COPLT_INTERFACE_DEFINE(NAME, ID, ...) COPLT_INTERFACE_GUID_OF(NAME, ID)\
struct COPLT_UUID_MARK(ID) COPLT_NOVTABLE NAME : __VA_ARGS__

#define COPLT_PVTB (*(const VirtualTable**)this)

namespace Coplt
{
    template <class T>
    struct ComProxy
    {
        ComProxy() = delete;
    };

    enum class HResult : u32
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
    };

    template <class T>
    concept Interface = requires(T& t, Guid& guid, void*& object)
    {
        { guid_of(GuidOf<T>{}) } -> std::same_as<const Guid&>;
        { t.QueryInterface(guid, object) } -> std::same_as<HResult>;
        { t.AddRef() } -> std::same_as<u32>;
        { t.Release() } -> std::same_as<u32>;
    };

    using VPtr = void const* const* const;

    COPLT_INTERFACE_GUID_OF(IUnknown, "00000000-0000-0000-C000-000000000046")

    struct COPLT_UUID_MARK("00000000-0000-0000-C000-000000000046") COPLT_NOVTABLE IUnknown
    {
        using Self = IUnknown;

        struct VirtualTable
        {
            HResult (*const f_QueryInterface)(const Self*, const Guid& guid, COPLT_OUT void*& object);
            u32 (*const f_AddRef)(const Self*);
            u32 (*const f_Release)(const Self*);
        };

        VPtr m_vtbl{};

        explicit IUnknown(const VirtualTable* vtbl) : m_vtbl(reinterpret_cast<VPtr>(vtbl))
        {
        }

        explicit IUnknown(const IUnknown&) = delete;
        explicit IUnknown(IUnknown&&) = delete;

        COPLT_FORCE_INLINE
        HResult QueryInterface(const Guid& guid, COPLT_OUT void*& object) const
        {
            return COPLT_PVTB->f_QueryInterface(this, guid, object);
        }

        [[nodiscard]] COPLT_FORCE_INLINE
        u32 AddRef() const
        {
            return COPLT_PVTB->f_AddRef(this);
        }

        [[nodiscard]] COPLT_FORCE_INLINE
        u32 Release() const
        {
            return COPLT_PVTB->f_Release(this);
        }

        template <Interface T>
        COPLT_FORCE_INLINE
        HResult QueryInterface(COPLT_OUT T*& object) const
        {
            return QueryInterface(guid_of<T>(), reinterpret_cast<void*&>(object));
        }
    };

    template <>
    struct ComProxy<IUnknown>
    {
        template <std::derived_from<IUnknown> Base = IUnknown>
        struct Proxy : Base
        {
            using Self = Proxy;

        protected:
            virtual ~Proxy() = default;

            COPLT_FORCE_INLINE
            static const IUnknown::VirtualTable& GetVtb()
            {
                static IUnknown::VirtualTable vtb{
                    .f_QueryInterface = [](const IUnknown* self, const Guid& guid, void*& object)
                    {
                        return static_cast<const Self*>(self)->Impl_QueryInterface(guid, object);
                    },
                    .f_AddRef = [](const IUnknown* self)
                    {
                        return static_cast<const Self*>(self)->Impl_AddRef();
                    },
                    .f_Release = [](const IUnknown* self)
                    {
                        return static_cast<const Self*>(self)->Impl_Release();
                    },
                };
                return vtb;
            }

            explicit Proxy(const Base::VirtualTable* vtb) : Base(vtb)
            {
            }

            explicit Proxy() : Base(&GetVtb())
            {
            }

            virtual HResult Impl_QueryInterface(const Guid& guid, COPLT_OUT void*& object) const = 0;
            [[nodiscard]] virtual u32 Impl_AddRef() const = 0;
            [[nodiscard]] virtual u32 Impl_Release() const = 0;
        };
    };

    COPLT_INTERFACE_DEFINE(IWeak, "9d01e165-12b5-4190-bb46-3d78413de9a5", IUnknown)
    {
        using Self = IWeak;

        template <class Impl, std::derived_from<IWeak> Base>
        struct Proxy;

        struct VirtualTable
        {
            IUnknown::VirtualTable b_0;
            u32 (*const f_AddRefWeak)(const Self*);
            u32 (*const f_ReleaseWeak)(const Self*);
            bool (*const f_TryUpgrade)(const Self*);
            bool (*const f_TryDowngrade)(const Self*);
        };

        explicit IWeak(const VirtualTable* vtbl) : IUnknown(&vtbl->b_0)
        {
        }

        [[nodiscard]] COPLT_FORCE_INLINE
        u32 AddRefWeak() const
        {
            return COPLT_PVTB->f_AddRefWeak(this);
        }

        [[nodiscard]] COPLT_FORCE_INLINE
        u32 ReleaseWeak() const
        {
            return COPLT_PVTB->f_ReleaseWeak(this);
        }

        [[nodiscard]] COPLT_FORCE_INLINE
        bool TryUpgrade() const
        {
            return COPLT_PVTB->f_TryUpgrade(this);
        }

        [[nodiscard]] COPLT_FORCE_INLINE
        bool TryDowngrade() const
        {
            return COPLT_PVTB->f_TryDowngrade(this);
        }
    };

    template <>
    struct ComProxy<IWeak>
    {
        template <std::derived_from<IWeak> Base = IWeak>
        struct Proxy : ComProxy<IUnknown>::Proxy<Base>
        {
            using Super = ComProxy<IUnknown>::Proxy<Base>;
            using Self = Proxy;

        protected:
            COPLT_FORCE_INLINE
            static const IWeak::VirtualTable& GetVtb()
            {
                static IWeak::VirtualTable vtb{
                    .b_0 = Super::GetVtb(),
                    .f_AddRefWeak = [](const IWeak* self)
                    {
                        return static_cast<const Self*>(self)->Impl_AddRefWeak();
                    },
                    .f_ReleaseWeak = [](const IWeak* self)
                    {
                        return static_cast<const Self*>(self)->Impl_ReleaseWeak();
                    },
                    .f_TryUpgrade = [](const IWeak* self)
                    {
                        return static_cast<const Self*>(self)->Impl_TryUpgrade();
                    },
                    .f_TryDowngrade = [](const IWeak* self)
                    {
                        return static_cast<const Self*>(self)->Impl_TryDowngrade();
                    },
                };
                return vtb;
            }

            explicit Proxy(const Base::VirtualTable* vtb) : Base(vtb)
            {
            }

            explicit Proxy() : Super(&GetVtb())
            {
            }

            [[nodiscard]] virtual u32 Impl_AddRefWeak() const = 0;
            [[nodiscard]] virtual u32 Impl_ReleaseWeak() const = 0;
            [[nodiscard]] virtual bool Impl_TryUpgrade() const = 0;
            [[nodiscard]] virtual bool Impl_TryDowngrade() const = 0;
        };
    };
}

#endif //COPLT_COM_INTERFACE_H

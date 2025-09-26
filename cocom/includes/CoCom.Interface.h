#pragma once
#ifndef COPLT_COM_INTERFACE_H
#define COPLT_COM_INTERFACE_H

#include "CoCom.Macros.h"
#include "CoCom.Types.h"
#include "CoCom.Guid.h"
#include "CoCom.Interface.h"
#include "CoCom.Rc.h"

#define COPLT_COM_INTERFACE(NAME, ID, ...) struct COPLT_UUID_MARK(ID) COPLT_NOVTABLE NAME : __VA_ARGS__

#define COPLT_COM_PVTB(SELF, THIS) (*(const ::Coplt::Internal::VirtualTable<SELF>**)THIS)
#define COPLT_COM_METHOD(NAME, RET, PARAMS, ...) COPLT_FORCE_INLINE\
    RET NAME PARAMS { return ::Coplt::Internal::CallComMethod<Self>::NAME(this __VA_OPT__(,) __VA_ARGS__); }

namespace Coplt
{
#pragma region details

    namespace Internal
    {
        template <class... T>
        struct ComProxy;

        struct MergedInterface
        {
        };

        template <class... T>
        struct VirtualTable;

        template <class Interface>
        static auto AsImpl(Interface* self)
        {
            return reinterpret_cast<ComProxy<Interface>::Impl*>(reinterpret_cast<size_t*>(self) - 1);
        }

        template <class Interface>
        static auto AsImpl(const Interface* self)
        {
            return reinterpret_cast<const ComProxy<Interface>::Impl*>(reinterpret_cast<const size_t*>(self) - 1);
        }

        template <class Self>
        struct CallComMethod;
    }

    template <class T>
    COPLT_FORCE_INLINE constexpr inline const Guid& guid_of() { return Internal::ComProxy<T>::get_Guid(); }

    enum class HResultE : u32
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

    struct HResult
    {
        i32 Value;

        HResult() : Value(0)
        {
        }

        // ReSharper disable once CppNonExplicitConvertingConstructor
        HResult(HResultE e) : Value(static_cast<i32>(e)) // NOLINT(*-explicit-constructor)
        {
        }

#if defined(_WINDOWS) && defined(_HRESULT_DEFINED)
        // ReSharper disable once CppNonExplicitConvertingConstructor
        HResult(const HRESULT e) : Value(static_cast<i32>(e)) // NOLINT(*-explicit-constructor)
        {
        }
#endif

        // ReSharper disable once CppNonExplicitConversionOperator
        operator bool() const { return IsSuccess(); } // NOLINT(*-explicit-constructor)
        bool operator!() const { return IsFailure(); }

        // ReSharper disable once CppNonExplicitConversionOperator
        operator HResultE() const { return static_cast<HResultE>(Value); } // NOLINT(*-explicit-constructor)

        // ReSharper disable once CppNonExplicitConversionOperator
        operator i32() const { return Value; } // NOLINT(*-explicit-constructor)

        bool IsSuccess() const { return Value >= 0; }
        bool IsFailure() const { return Value < 0; }

        bool IsError() const { return (static_cast<u32>(Value) >> 31) == 1; }

        i32 Code() const { return Value & 0xFFFF; }
        i32 Facility() const { return (Value >> 16) & 0x1FFF; }
        i32 Severity() const { return (Value >> 31) & 1; }
    };

    template <class T>
    concept Interface = requires(T& t, const Guid& guid, void*& object)
    {
        { Internal::ComProxy<T>::get_Guid() } -> std::same_as<const Guid&>;
        { t.QueryInterface(guid, object) } -> std::same_as<HResult>;
        { t.AddRef() } -> std::same_as<u32>;
        { t.Release() } -> std::same_as<u32>;
    };

    using VPtr = void const* const* const;

#pragma endregion

#pragma region IUnknown meta

    struct IUnknown;

    template <>
    struct Internal::VirtualTable<IUnknown>
    {
        HResult (*const COPLT_CDECL f_QueryInterface)(const IUnknown*, const Guid& guid,
                                                      COPLT_OUT void*& object) noexcept;
        u32 (*const COPLT_CDECL f_AddRef)(const IUnknown*) noexcept;
        u32 (*const COPLT_CDECL f_Release)(const IUnknown*) noexcept;
    };

    namespace Internal::VirtualImpl_IUnknown
    {
        extern "C" HResult COPLT_CDECL QueryInterface(
            const IUnknown* self, const Guid& guid, COPLT_OUT void*& object
        ) noexcept;
        extern "C" u32 COPLT_CDECL AddRef(const IUnknown* self) noexcept;
        extern "C" u32 COPLT_CDECL Release(const IUnknown* self) noexcept;
    }

    template <>
    struct Internal::ComProxy<IUnknown>
    {
        using VirtualTable = VirtualTable<IUnknown>;

        static COPLT_FORCE_INLINE constexpr inline const Guid& get_Guid()
        {
            static Guid s_guid("00000000-0000-0000-C000-000000000046");
            return s_guid;
        }

        template <class Self>
        COPLT_FORCE_INLINE
        static HResult QueryInterface(const Self* self, const Guid& guid, COPLT_OUT void*& object)
        {
            if (guid == guid_of<IUnknown>())
            {
                object = const_cast<void*>(static_cast<const void*>(static_cast<const IUnknown*>(self)));
                return HResultE::Ok;
            }
            return HResultE::NoInterface;
        }

        COPLT_FORCE_INLINE
        static const VirtualTable& GetVtb()
        {
            static VirtualTable vtb
            {
                .f_QueryInterface = VirtualImpl_IUnknown::QueryInterface,
                .f_AddRef = VirtualImpl_IUnknown::AddRef,
                .f_Release = VirtualImpl_IUnknown::Release,
            };
            return vtb;
        }

        struct Impl
        {
            virtual ~Impl() = default;

            virtual HResult Impl_QueryInterface(const Guid& guid, COPLT_OUT void*& object) const = 0;
            virtual u32 Impl_AddRef() const = 0;
            virtual u32 Impl_Release() const = 0;
        };

        template <std::derived_from<IUnknown> Base = IUnknown>
        struct Proxy : Impl, Base
        {
            using Self = Proxy;

            explicit Proxy(const Internal::VirtualTable<Base>* vtb) : Base(vtb)
            {
            }

            explicit Proxy() : Base(&GetVtb())
            {
            }
        };
    };

    namespace Internal::VirtualImpl_IUnknown
    {
        extern "C" inline HResult COPLT_CDECL QueryInterface(
            const IUnknown* self, const Guid& guid, COPLT_OUT void*& object
        ) noexcept
        {
            return AsImpl(self)->Impl_QueryInterface(guid, object);
        }

        extern "C" inline u32 COPLT_CDECL AddRef(const IUnknown* self) noexcept
        {
            return AsImpl(self)->Impl_AddRef();
        }

        extern "C" inline u32 COPLT_CDECL Release(const IUnknown* self) noexcept
        {
            return AsImpl(self)->Impl_Release();
        }
    }

    template <>
    struct Internal::CallComMethod<IUnknown>
    {
        static COPLT_FORCE_INLINE HResult QueryInterface(const IUnknown* self, const Guid& guid, COPLT_OUT void*& object)
        {
            return COPLT_COM_PVTB(IUnknown, self)->f_QueryInterface(self, guid, object);
        }
        static COPLT_FORCE_INLINE u32 AddRef(const IUnknown* self)
        {
            return COPLT_COM_PVTB(IUnknown, self)->f_AddRef(self);
        }
        static COPLT_FORCE_INLINE u32 Release(const IUnknown* self)
        {
            return COPLT_COM_PVTB(IUnknown, self)->f_Release(self);
        }
    };

#define COPLT_COM_INTERFACE_BODY_IUnknown\
    using Self = IUnknown;\
\
    VPtr m_vtbl{};\
\
    explicit IUnknown(const Internal::VirtualTable<Self>* vtbl) : m_vtbl(reinterpret_cast<VPtr>(vtbl)) {}\
\
    explicit IUnknown(const IUnknown&) = delete;\
    explicit IUnknown(IUnknown&&) = delete;
#pragma endregion IUnknown meta

    struct COPLT_UUID_MARK("00000000-0000-0000-C000-000000000046") COPLT_NOVTABLE IUnknown
    {
        COPLT_COM_INTERFACE_BODY_IUnknown

        COPLT_COM_METHOD(QueryInterface, HResult, (const Guid& guid, COPLT_OUT void*& object) const, guid, object)
        COPLT_COM_METHOD(AddRef, u32, () const)
        COPLT_COM_METHOD(Release, u32, () const)

        template <Interface T>
        COPLT_FORCE_INLINE
        HResult QueryInterface(COPLT_OUT T*& object) const
        {
            return QueryInterface(guid_of<T>(), reinterpret_cast<void*&>(object));
        }

        template <Interface T>
        COPLT_FORCE_INLINE
        HResult QueryInterface(COPLT_OUT Rc<T>& object) const
        {
            T* ptr;
            const auto r = QueryInterface<T>(ptr);
            if (r) object = Rc(static_cast<T*>(ptr));
            return r;
        }

        template <Interface T>
        COPLT_FORCE_INLINE
        Rc<T> TryCast() const
        {
            T* ptr;
            if (QueryInterface<T>(ptr).IsFailure()) return nullptr;
            return Rc(static_cast<T*>(ptr));
        }
    };

#pragma region IWeak meta

    struct IWeak;

    template <>
    struct Internal::VirtualTable<IWeak>
    {
        VirtualTable<IUnknown> b;
        u32 (*const COPLT_CDECL f_AddRefWeak)(const IWeak*);
        u32 (*const COPLT_CDECL f_ReleaseWeak)(const IWeak*);
        bool (*const COPLT_CDECL f_TryUpgrade)(const IWeak*);
        bool (*const COPLT_CDECL f_TryDowngrade)(const IWeak*);
    };

    namespace Internal::VirtualImpl_IWeak
    {
        extern "C" u32 COPLT_CDECL AddRefWeak(const IWeak* self) noexcept;
        extern "C" u32 COPLT_CDECL ReleaseWeak(const IWeak* self) noexcept;
        extern "C" bool COPLT_CDECL TryUpgrade(const IWeak* self) noexcept;
        extern "C" bool COPLT_CDECL TryDowngrade(const IWeak* self) noexcept;
    }

    template <>
    struct Internal::ComProxy<IWeak>
    {
        using VirtualTable = VirtualTable<IWeak>;

        static COPLT_FORCE_INLINE constexpr inline const Guid& get_Guid()
        {
            static Guid s_guid("9d01e165-12b5-4190-bb46-3d78413de9a5");
            return s_guid;
        }

        template <class Self>
        COPLT_FORCE_INLINE
        static HResult QueryInterface(const Self* self, const Guid& guid, COPLT_OUT void*& object)
        {
            if (guid == guid_of<IWeak>())
            {
                object = const_cast<void*>(static_cast<const void*>(static_cast<const IWeak*>(self)));
                return HResultE::Ok;
            }
            return ComProxy<IUnknown>::QueryInterface(self, guid, object);
        }

        COPLT_FORCE_INLINE
        static const VirtualTable& GetVtb()
        {
            static VirtualTable vtb
            {
                .b = ComProxy<IUnknown>::GetVtb(),
                .f_AddRefWeak = VirtualImpl_IWeak::AddRefWeak,
                .f_ReleaseWeak = VirtualImpl_IWeak::ReleaseWeak,
                .f_TryUpgrade = VirtualImpl_IWeak::TryUpgrade,
                .f_TryDowngrade = VirtualImpl_IWeak::TryDowngrade,
            };
            return vtb;
        }

        struct Impl : ComProxy<IUnknown>::Impl
        {
            virtual u32 Impl_AddRefWeak() const = 0;
            virtual u32 Impl_ReleaseWeak() const = 0;
            virtual bool Impl_TryUpgrade() const = 0;
            virtual bool Impl_TryDowngrade() const = 0;
        };

        template <std::derived_from<IWeak> Base = IWeak>
        struct Proxy : Impl, Base
        {
            explicit Proxy(const Internal::VirtualTable<Base>* vtb) : Base(vtb)
            {
            }

            explicit Proxy() : Base(&GetVtb())
            {
            }
        };
    };

    namespace Internal::VirtualImpl_IWeak
    {
        extern "C" inline u32 COPLT_CDECL AddRefWeak(const IWeak* self) noexcept
        {
            return AsImpl<IWeak>(self)->Impl_AddRefWeak();
        }

        extern "C" inline u32 COPLT_CDECL ReleaseWeak(const IWeak* self) noexcept
        {
            return AsImpl<IWeak>(self)->Impl_ReleaseWeak();
        }

        extern "C" inline bool COPLT_CDECL TryUpgrade(const IWeak* self) noexcept
        {
            return AsImpl<IWeak>(self)->Impl_TryUpgrade();
        }

        extern "C" inline bool COPLT_CDECL TryDowngrade(const IWeak* self) noexcept
        {
            return AsImpl<IWeak>(self)->Impl_TryDowngrade();
        }
    }

    template <>
    struct Internal::CallComMethod<IWeak>
    {
        static COPLT_FORCE_INLINE u32 AddRefWeak(const IWeak* self)
        {
            return COPLT_COM_PVTB(IWeak, self)->f_AddRefWeak(self);
        }
        static COPLT_FORCE_INLINE u32 ReleaseWeak(const IWeak* self)
        {
            return COPLT_COM_PVTB(IWeak, self)->f_ReleaseWeak(self);
        }
        static COPLT_FORCE_INLINE bool TryUpgrade(const IWeak* self)
        {
            return COPLT_COM_PVTB(IWeak, self)->f_TryUpgrade(self);
        }
        static COPLT_FORCE_INLINE bool TryDowngrade(const IWeak* self)
        {
            return COPLT_COM_PVTB(IWeak, self)->f_TryDowngrade(self);
        }
    };

#define COPLT_COM_INTERFACE_BODY_IWeak\
    using Super = IUnknown;\
    using Self = IWeak;\
\
    explicit IWeak(const Internal::VirtualTable<Self>* vtbl) : IUnknown(&vtbl->b) {}
#pragma endregion IWeak meta

    COPLT_COM_INTERFACE(IWeak, "9d01e165-12b5-4190-bb46-3d78413de9a5", IUnknown)
    {
        COPLT_COM_INTERFACE_BODY_IWeak

        COPLT_COM_METHOD(AddRefWeak, u32, () const)
        COPLT_COM_METHOD(ReleaseWeak, u32, () const)
        COPLT_COM_METHOD(TryUpgrade, bool, () const)
        COPLT_COM_METHOD(TryDowngrade, bool, () const)
    };
}

#endif //COPLT_COM_INTERFACE_H

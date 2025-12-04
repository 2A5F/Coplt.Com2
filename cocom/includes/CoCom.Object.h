#pragma once
#ifndef COPLT_COM_OBJECT_H
#define COPLT_COM_OBJECT_H

#include "CoCom.Interface.h"

namespace Coplt
{
    namespace Internal
    {
        template <class T>
        concept LooseInterface = std::derived_from<T, IUnknown>;

        struct ComObjectInstance
        {
        };

        template <class T>
        concept IsComObject = std::derived_from<T, ComObjectInstance>;
    }

    template <class... T>
    struct ComObject;

    template <Interface I>
        requires std::derived_from<I, IWeak>
    struct ComObject<I> : Internal::ComObjectInstance, Internal::ComProxy<I>::template Proxy<>
    {
    private:
        mutable std::atomic_uint64_t m_strong{1};
        mutable std::atomic_uint64_t m_weak{1};

    public:
        HResult Impl_QueryInterface(const Guid& guid, COPLT_OUT void*& object) const override
        {
            return Internal::ComProxy<I>::QueryInterface(this, guid, object);
        }

        u32 Impl_AddRef() const override
        {
            return m_strong.fetch_add(1, std::memory_order_relaxed);
        }

        u32 Impl_Release() const override
        {
            const auto r = m_strong.fetch_sub(1, ::std::memory_order_release);
            if (r != 1) [[likely]] return r;
            DropSlow();
            return r;
        }

        u32 Impl_AddRefWeak() const override
        {
            return m_weak.fetch_add(1, std::memory_order_relaxed);
        }

        u32 Impl_ReleaseWeak() const override
        {
            const size_t r = m_weak.fetch_sub(1, std::memory_order_release);
            if (r != 1) return r;
            OnDelete();
            return r;
        }

        bool Impl_TryUpgrade() const override
        {
            size_t cur = m_strong.load(std::memory_order_relaxed);
        re_try:
            if (cur == 0) return false;
            if (m_strong.compare_exchange_weak(cur, cur + 1, std::memory_order_acquire, std::memory_order_relaxed))
            {
                return true;
            }
            goto re_try;
        }

    private:
        void DropSlow() const
        {
            this->~ComObject();
            // ReSharper disable once CppExpressionWithoutSideEffects
            Impl_ReleaseWeak();
        }

    protected:
        virtual void OnDelete() const
        {
            operator delete(const_cast<void*>(static_cast<const void*>(this)));
        }
    };

    template <Interface I>
    struct ComObject<I> : Internal::ComObjectInstance, Internal::ComProxy<I>::template Proxy<>
    {
    private:
        mutable ::std::atomic_uint32_t m_strong{1};

    public:
        HResult Impl_QueryInterface(const Guid& guid, COPLT_OUT void*& object) const override
        {
            return Internal::ComProxy<I>::QueryInterface(this, guid, object);
        }

        u32 Impl_AddRef() const override
        {
            return m_strong.fetch_add(1, ::std::memory_order_relaxed);
        }

        u32 Impl_Release() const override
        {
            const auto r = m_strong.fetch_sub(1, ::std::memory_order_release);
            if (r != 1) [[likely]] return r;
            OnRelease();
            return r;
        }

    protected:
        virtual void OnRelease() const
        {
            delete this;
        }
    };

    template <class Self>
    struct Impl_RefCount
    {
    private:
        mutable ::std::atomic_uint32_t m_strong{1};

    public:
        COPLT_FORCE_INLINE
        u32 Impl_AddRef() const
        {
            return m_strong.fetch_add(1, ::std::memory_order_relaxed);
        }

        COPLT_FORCE_INLINE
        u32 Impl_Release() const
        {
            const auto r = m_strong.fetch_sub(1, ::std::memory_order_release);
            if (r != 1) [[likely]]
            {
                if constexpr (requires(const Self* ptr) { ptr->OnStrongCountSub(r); })
                {
                    static_cast<const Self*>(this)->OnStrongCountSub(r);
                }
                return r;
            }
            if constexpr (requires(const Self* ptr) { ptr->OnDelete(); })
            {
                static_cast<const Self*>(this)->OnDelete();
            }
            else
            {
                delete static_cast<const Self*>(this);
            }
            return r;
        }

    protected:
        u32 GetStrongCount() const noexcept
        {
            return m_strong.load(::std::memory_order_acquire);
        }
    };

    template <class Self>
    struct Impl_WeakRefCount
    {
    private:
        mutable std::atomic_uint64_t m_strong{1};
        mutable std::atomic_uint64_t m_weak{1};

    public:
        COPLT_FORCE_INLINE
        u32 Impl_AddRef() const
        {
            return m_strong.fetch_add(1, std::memory_order_relaxed);
        }

        COPLT_FORCE_INLINE
        u32 Impl_Release() const
        {
            const auto r = m_strong.fetch_sub(1, ::std::memory_order_release);
            if (r != 1) [[likely]]
            {
                if constexpr (requires(const Self* ptr) { ptr->OnStrongCountSub(r); })
                {
                    static_cast<const Self*>(this)->OnStrongCountSub(r);
                }
                return r;
            }
            DropSlow();
            return r;
        }

        COPLT_FORCE_INLINE
        u32 Impl_AddRefWeak() const
        {
            return m_weak.fetch_add(1, std::memory_order_relaxed);
        }

        COPLT_FORCE_INLINE
        u32 Impl_ReleaseWeak() const
        {
            const size_t r = m_weak.fetch_sub(1, std::memory_order_release);
            if (r != 1) return r;
            if constexpr (requires(const Self* ptr) { ptr->OnDelete(); })
            {
                static_cast<const Self*>(this)->OnDelete();
            }
            else
            {
                delete static_cast<const Self*>(this);
            }
            return r;
        }

        COPLT_FORCE_INLINE
        bool Impl_TryUpgrade() const
        {
            size_t cur = m_strong.load(std::memory_order_relaxed);
        re_try:
            if (cur == 0) return false;
            if (m_strong.compare_exchange_weak(cur, cur + 1, std::memory_order_acquire, std::memory_order_relaxed))
            {
                return true;
            }
            goto re_try;
        }

    private:
        void DropSlow() const
        {
            const_cast<Self*>(static_cast<const Self*>(this))->~Self();
            // ReSharper disable once CppExpressionWithoutSideEffects
            Impl_ReleaseWeak();
        }

    protected:
        u32 GetStrongCount() const noexcept
        {
            return m_strong.load(::std::memory_order_acquire);
        }

        u32 GetWeakCount() const noexcept
        {
            return m_weak.load(::std::memory_order_acquire);
        }
    };

    template <class Self, Interface I>
    struct ComImpl : I, Impl_RefCount<Self>
    {
        ComImpl()
            : I(&Internal::ComProxy<I>::template s_vtb<Self>), Impl_RefCount<Self>()
        {
        }

        COPLT_FORCE_INLINE
        HResult Impl_QueryInterface(const Guid& guid, COPLT_OUT void*& object) const
        {
            if (std::addressof(object) == nullptr) return HResultE::InvalidArg;
            return Internal::ComProxy<I>::QueryInterface(this, guid, object);
        }
    };

    template <class Self, Interface I>
        requires std::derived_from<I, IWeak>
    struct ComImpl<Self, I> : I, Impl_WeakRefCount<Self>
    {
        ComImpl()
            : I(&Internal::ComProxy<I>::template s_vtb<Self>), Impl_WeakRefCount<Self>()
        {
        }

        COPLT_FORCE_INLINE
        HResult Impl_QueryInterface(const Guid& guid, COPLT_OUT void*& object) const
        {
            if (std::addressof(object) == nullptr) return HResultE::InvalidArg;
            return Internal::ComProxy<I>::QueryInterface(this, guid, object);
        }
    };

    template <class Self>
    struct RefCount : Impl_RefCount<Self>
    {
        u32 AddRef() const
        {
            return Impl_RefCount<Self>::Impl_AddRef();
        }

        u32 Release() const
        {
            return Impl_RefCount<Self>::Impl_Release();
        }
    };

    template <class Self>
    struct WeakRefCount : Impl_WeakRefCount<Self>
    {
        u32 AddRef() const
        {
            return Impl_WeakRefCount<Self>::Impl_AddRef();
        }

        u32 Release() const
        {
            return Impl_WeakRefCount<Self>::Impl_Release();
        }

        u32 AddRefWeak() const
        {
            return Impl_WeakRefCount<Self>::Impl_AddRefWeak();
        }

        u32 ReleaseWeak() const
        {
            return Impl_WeakRefCount<Self>::Impl_ReleaseWeak();
        }

        u32 TryUpgrade() const
        {
            return Impl_WeakRefCount<Self>::Impl_TryUpgrade();
        }
    };
}

#endif //COPLT_COM_OBJECT_H

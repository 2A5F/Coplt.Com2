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
            size_t cur = m_weak.load(std::memory_order_relaxed);
        re_try:
            if (cur == 0) return false;
            if (m_weak.compare_exchange_weak(cur, cur + 1, std::memory_order_acquire, std::memory_order_relaxed))
            {
                return true;
            }
            goto re_try;
        }

        bool Impl_TryDowngrade() const override
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

    template<class Self>
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
            if (r != 1) [[likely]] return r;
            if constexpr (requires(const Self* ptr) { ptr->OnRelease(); })
            {
                static_cast<const Self*>(this)->OnRelease();
            }
            else
            {
                delete static_cast<const Self*>(this);
            }
            return r;
        }
    };

    template<class Self, Interface I>
    struct ComImpl : I, Impl_RefCount<Self>
    {
        ComImpl() : I(&Internal::ComProxy<I>::template s_vtb<Self>), Impl_RefCount<Self>() {}

        COPLT_FORCE_INLINE
        HResult Impl_QueryInterface(const Guid& guid, COPLT_OUT void*& object) const
        {
            return Internal::ComProxy<I>::QueryInterface(this, guid, object);
        }
    };

    template<class Self>
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
}

#endif //COPLT_COM_OBJECT_H

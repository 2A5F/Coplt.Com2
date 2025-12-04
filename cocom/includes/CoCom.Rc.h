#pragma once
#ifndef COPLT_COM_RC_H
#define COPLT_COM_RC_H

#include "CoCom.Guid.h"
#include "CoCom.Concepts.h"
#include "CoCom.NullPointerError.h"

namespace Coplt
{
    template <WeakReferenceCounting T>
    class Weak;

    template <class T>
    class Rc final
    {
        T* m_ptr;

        template <class U>
        friend class Rc;

        template <WeakReferenceCounting U>
        friend class Weak;

        struct clone_t
        {
        };

        struct upgrade_t
        {
        };

        // clone
        explicit Rc(T* ptr, clone_t) requires ReferenceCounting<T>
            : m_ptr(ptr)
        {
            if (auto p = m_ptr)
            {
                p->AddRef();
            }
        }

        // upgrade
        explicit Rc(T* ptr, upgrade_t) requires WeakReferenceCounting<T>
            : m_ptr(ptr && ptr->TryUpgrade() ? ptr : nullptr)
        {
        }

    public:
        using DerefType = T;

        // null
        Rc() noexcept
            : m_ptr(nullptr)
        {
            static_assert(ReferenceCounting<T>);
        }

        // null
        // ReSharper disable once CppNonExplicitConvertingConstructor
        Rc(std::nullptr_t) noexcept
            : m_ptr(nullptr) // NOLINT(*-explicit-constructor)
        {
            static_assert(ReferenceCounting<T>);
        }

        // create
        explicit Rc(T* ptr) noexcept
            : m_ptr(ptr)
        {
        }

        // copy
        Rc(const Rc& other) noexcept
            : Rc(other.m_ptr, clone_t{})
        {
        }

        // copy conv
        template <typename U> requires std::convertible_to<U*, T*>
        // ReSharper disable once CppNonExplicitConvertingConstructor
        Rc(const Rc<U>& other) noexcept
            : Rc(other.m_ptr, clone_t{}) // NOLINT(*-explicit-constructor)
        {
        }

        // move
        Rc(Rc&& other) noexcept
            : m_ptr(std::exchange(other.m_ptr, nullptr))
        {
        }

        // move conv
        template <typename U> requires std::convertible_to<U*, T*>
        // ReSharper disable once CppNonExplicitConvertingConstructor
        Rc(Rc<U>&& other) noexcept
            : m_ptr(std::exchange(other.m_ptr, nullptr)) // NOLINT(*-explicit-constructor)
        {
        }

        ~Rc()
        {
            if (auto p = m_ptr)
            {
                p->Release();
            }
        }

        // copy ass
        Rc& operator=(const Rc& r) noexcept
        {
            if (m_ptr != r.m_ptr) Rc(r).swap(*this);
            return *this;
        }

        // move ass
        Rc& operator=(Rc&& r) noexcept
        {
            if (&m_ptr != &r.m_ptr) Rc(std::forward<Rc>(r)).swap(*this);
            return *this;
        }

        // copy ass conv
        template <typename U> requires std::convertible_to<U*, T*>
        Rc& operator=(const Rc<U>& r) noexcept
        {
            Rc(r).swap(*this);
            return *this;
        }

        // move ass conv
        template <typename U> requires std::convertible_to<U*, T*>
        Rc& operator=(Rc<U>&& r) noexcept
        {
            Rc(std::move(r)).swap(*this);
            return *this;
        }

        void reset()
        {
            if (auto p = std::exchange(m_ptr, nullptr))
            {
                p->Release();
            }
        }

        void swap(Rc& r) noexcept
        {
            std::swap(m_ptr, r.m_ptr);
        }

        Rc clone() const
        {
            return Rc(*this);
        }

        explicit operator bool() const noexcept
        {
            return get() != nullptr;
        }

        T* get() const noexcept
        {
            return m_ptr;
        }

        T** put() noexcept
        {
            return &m_ptr;
        }

        template <typename U> requires std::convertible_to<T*, U*>
        U** put() noexcept
        {
            return reinterpret_cast<U**>(&m_ptr);
        }

        T& operator*() const
        {
            #ifdef COPLT_NULL_CHECK
            if (m_ptr == nullptr) [[unlikely]] throw NullPointerError();
            #endif
            return *m_ptr;
        }

        T* operator->() const
        {
            #ifdef COPLT_NULL_CHECK
            if (m_ptr == nullptr) [[unlikely]] throw NullPointerError();
            #endif
            return m_ptr;
        }

        // Direct leakage, out of RAII management
        T* leak() noexcept
        {
            return std::exchange(m_ptr, nullptr);
        }

        bool operator==(std::nullptr_t) const
        {
            return get() == nullptr;
        }

        bool operator==(T* ptr) const
        {
            return get() == ptr;
        }

        bool operator==(const Rc& other) const
        {
            return get() == other.get();
        }

        auto downgrade() const requires WeakReferenceCounting<T>
        {
            return Weak(get(), typename Weak<T>::downgrade_t());
        }

        static Rc UnsafeClone(T* ptr)
        {
            return Rc(ptr, clone_t{});
        }

        template <class U> requires requires(T* ptr) { static_cast<U*>(ptr); }
        Rc<U> StaticCast() const
        {
            return Rc<U>(static_cast<U*>(m_ptr), typename Rc<U>::clone_t{});
        }

        template <class U> requires requires(T* ptr) { static_cast<U*>(ptr); }
        Rc<U> StaticCastMove()
        {
            return Rc<U>(static_cast<U*>(std::exchange(m_ptr, nullptr)));
        }

        template <class U> requires requires(T* ptr) { dynamic_cast<U*>(ptr); }
        Rc<U> DynamicCast() const
        {
            return Rc<U>(dynamic_cast<U*>(m_ptr), typename Rc<U>::clone_t{});
        }

        template <class U> requires requires(T* ptr) { dynamic_cast<U*>(ptr); }
        Rc<U> DynamicCastMove()
        {
            return Rc<U>(dynamic_cast<U*>(std::exchange(m_ptr, nullptr)));
        }
    };

    template <WeakReferenceCounting T>
    class Weak final
    {
        T* m_ptr;

        template <WeakReferenceCounting U>
        friend class Weak;

        template <class U>
        friend class Rc;

        struct clone_t
        {
        };

        struct downgrade_t
        {
        };

        // clone
        explicit Weak(T* ptr, clone_t)
            : m_ptr(ptr)
        {
            if (auto p = m_ptr)
            {
                p->AddRefWeak();
            }
        }

        // downgrade
        explicit Weak(T* ptr, downgrade_t)
            : m_ptr(ptr ? ptr->AddRefWeak(), ptr : nullptr)
        {
        }

    public:
        using DerefType = T;

        // downgrade
        Weak(const Rc<T>& other) noexcept
            : Weak(other.m_ptr, downgrade_t{})
        {
        }

        // null
        Weak() noexcept
            : m_ptr(nullptr)
        {
        }

        // null
        // ReSharper disable once CppNonExplicitConvertingConstructor
        Weak(std::nullptr_t) noexcept
            : m_ptr(nullptr) // NOLINT(*-explicit-constructor)
        {
        }

        // copy
        Weak(const Weak& other) noexcept
            : Weak(other.m_ptr, clone_t{})
        {
        }

        // copy conv
        template <typename U> requires std::convertible_to<U*, T*>
        // ReSharper disable once CppNonExplicitConvertingConstructor
        Weak(const Weak<U>& other) noexcept
            : Weak(other.m_ptr, clone_t{}) // NOLINT(*-explicit-constructor)
        {
        }

        // move
        Weak(Weak&& other) noexcept
            : m_ptr(std::exchange(other.m_ptr, nullptr))
        {
        }

        // move conv
        template <typename U> requires std::convertible_to<U*, T*>
        // ReSharper disable once CppNonExplicitConvertingConstructor
        Weak(Weak<U>&& other) noexcept
            : m_ptr(std::exchange(other.m_ptr, nullptr)) // NOLINT(*-explicit-constructor)
        {
        }

        ~Weak()
        {
            if (auto p = m_ptr)
            {
                p->ReleaseWeak();
            }
        }

        // copy ass
        Weak& operator=(const Weak& r) noexcept
        {
            if (m_ptr != r.m_ptr) Weak(r).swap(*this);
            return *this;
        }

        // move ass
        Weak& operator=(Weak&& r) noexcept
        {
            if (&m_ptr != &r.m_ptr) Weak(std::move(r)).swap(*this);
            return *this;
        }

        // copy ass conv
        template <typename U> requires std::convertible_to<U*, T*>
        Weak& operator=(const Weak<U>& r) noexcept
        {
            Weak(r).swap(*this);
            return *this;
        }

        // move ass conv
        template <typename U> requires std::convertible_to<U*, T*>
        Weak& operator=(Weak<U>&& r) noexcept
        {
            Weak(std::move(r)).swap(*this);
            return *this;
        }

        void reset()
        {
            if (auto p = std::exchange(m_ptr, nullptr))
            {
                p->ReleaseWeak();
            }
        }

        void swap(Weak& r) noexcept
        {
            std::swap(m_ptr, r.m_ptr);
        }

        Weak clone() const
        {
            return Weak(*this);
        }

        explicit operator bool() const noexcept
        {
            return get() != nullptr;
        }

        T* get() const noexcept
        {
            return m_ptr;
        }

        bool operator==(std::nullptr_t) const
        {
            return get() == nullptr;
        }

        bool operator==(T* ptr) const
        {
            return get() == ptr;
        }

        bool operator==(const Weak& other) const
        {
            return get() == other.get();
        }

        Rc<T> upgrade() const
        {
            return Rc(get(), typename Rc<T>::upgrade_t());
        }

        static Weak UnsafeDowngrade(T* ptr)
        {
            return Weak(ptr, downgrade_t{});
        }
    };

    template <class T>
    Rc<T> CloneRc(T* ptr)
    {
        return Rc<T>::UnsafeClone(ptr);
    }

    template <WeakReferenceCounting T>
    Weak<T> MakeWeak(T* ptr)
    {
        return Weak<T>::UnsafeDowngrade(ptr);
    }
}

#define COPLT_COM_IMPL_RC(Self)


#endif //COPLT_COM_RC_H

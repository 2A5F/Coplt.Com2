#pragma once
#ifndef COPLT_COM_RC_H
#define COPLT_COM_RC_H

#include "CoCom.Guid.h"
#include "CoCom.Concepts.h"
#include "CoCom.NullPointerError.h"

namespace Coplt
{
    template <class T>
    class Weak;

    template <class T>
    class Rc final
    {
        T* m_ptr;

        template <class U>
        friend class Rc;

        template <class U>
        friend class Weak;

        struct clone_t
        {
        };

        struct upgrade_t
        {
        };

        // clone
        template <class = void> requires ReferenceCounting<T>
        explicit Rc(T* ptr, clone_t) : m_ptr(ptr)
        {
            if (auto p = m_ptr)
            {
                p->AddRef();
            }
        }

        // upgrade
        template <class = void> requires WeakReferenceCounting<T>
        explicit Rc(T* ptr, upgrade_t) : m_ptr(ptr && ptr->TryUpgrade() ? ptr : nullptr)
        {
        }

    public:
        using DerefType = T;

        // null
        Rc() noexcept : m_ptr(nullptr)
        {
            static_assert(ReferenceCounting<T>);
        }

        // null
        // ReSharper disable once CppNonExplicitConvertingConstructor
        Rc(std::nullptr_t) noexcept : m_ptr(nullptr) // NOLINT(*-explicit-constructor)
        {
            static_assert(ReferenceCounting<T>);
        }

        // create
        explicit Rc(T* ptr) noexcept : m_ptr(ptr)
        {
        }

        // copy
        Rc(const Rc& other) noexcept : Rc(other.m_ptr, clone_t{})
        {
        }

        // copy conv
        template <typename U> requires std::convertible_to<U*, T*>
        // ReSharper disable once CppNonExplicitConvertingConstructor
        Rc(const Rc<U>& other) noexcept : Rc(other.m_ptr, clone_t{}) // NOLINT(*-explicit-constructor)
        {
        }

        // move
        Rc(Rc&& other) noexcept : m_ptr(std::exchange(other.m_ptr, nullptr))
        {
        }

        // move conv
        template <typename U> requires std::convertible_to<U*, T*>
        // ReSharper disable once CppNonExplicitConvertingConstructor
        Rc(Rc<U>&& other) noexcept : m_ptr(std::exchange(other.m_ptr, nullptr)) // NOLINT(*-explicit-constructor)
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
            if (&m_ptr != &r.m_ptr) Rc(std::move(r)).swap(*this);
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

        template <class = void> requires WeakReferenceCounting<T>
        Weak<T> downgrade() const
        {
            return Weak(get(), Weak<T>::downgrade_t());
        }
    };

    template <class T>
    class Weak final
    {
        T* m_ptr;

        template <class U>
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
        template <class = void> requires WeakReferenceCounting<T>
        explicit Weak(T* ptr, clone_t) : m_ptr(ptr)
        {
            if (auto p = m_ptr)
            {
                p->AddRefWeak();
            }
        }

        // downgrade
        template <class = void> requires WeakReferenceCounting<T>
        explicit Weak(T* ptr, downgrade_t) : m_ptr(ptr && ptr->TryDowngrade() ? ptr : nullptr)
        {
        }

    public:
        using DerefType = T;

        // null
        Weak() noexcept : m_ptr(nullptr)
        {
            static_assert(WeakReferenceCounting<T>);
        }

        // null
        // ReSharper disable once CppNonExplicitConvertingConstructor
        Weak(std::nullptr_t) noexcept : m_ptr(nullptr) // NOLINT(*-explicit-constructor)
        {
            static_assert(WeakReferenceCounting<T>);
        }

        // copy
        Weak(const Weak& other) noexcept : Weak(other.m_ptr, clone_t{})
        {
        }

        // copy conv
        template <typename U> requires std::convertible_to<U*, T*>
        // ReSharper disable once CppNonExplicitConvertingConstructor
        Weak(const Weak<U>& other) noexcept : Weak(other.m_ptr, clone_t{}) // NOLINT(*-explicit-constructor)
        {
        }

        // move
        Weak(Weak&& other) noexcept : m_ptr(std::exchange(other.m_ptr, nullptr))
        {
        }

        // move conv
        template <typename U> requires std::convertible_to<U*, T*>
        // ReSharper disable once CppNonExplicitConvertingConstructor
        Weak(Weak<U>&& other) noexcept : m_ptr(std::exchange(other.m_ptr, nullptr)) // NOLINT(*-explicit-constructor)
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
            return Rc(get(), Rc<T>::upgrade_t());
        }
    };
}

#define COPLT_COM_IMPL_RC(Self)


#endif //COPLT_COM_RC_H

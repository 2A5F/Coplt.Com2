#pragma once
#ifndef COPLT_COM_CONCEPTS_H
#define COPLT_COM_CONCEPTS_H

#include <concepts>

#include "CoCom.Types.h"

namespace Coplt
{
    template <class F, class R, class... Args>
    concept Fn = std::invocable<F, Args...> && requires(F&& f, Args&&... args)
    {
        { f(std::forward<Args>(args)...) } -> std::convertible_to<R>;
    };

    template <class T>
    concept ReferenceCounting = requires(T& t)
    {
        { t.AddRef() } -> std::convertible_to<u32>;
        { t.Release() } -> std::convertible_to<u32>;
    };

    template <class T>
    concept WeakReferenceCounting = requires(T& t)
    {
        { t.AddRefWeak() } -> std::convertible_to<u32>;
        { t.ReleaseWeak() } -> std::convertible_to<u32>;
        { t.TryUpgrade() } -> std::convertible_to<bool>;
        { t.TryDowngrade() } -> std::convertible_to<bool>;
    };
}

#endif //COPLT_COM_CONCEPTS_H

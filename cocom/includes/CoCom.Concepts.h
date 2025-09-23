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
        { t.AddRef() } -> std::same_as<u32>;
        { t.Release() } -> std::same_as<u32>;
    };

    template <class T>
    concept WeakReferenceCounting = requires(T& t)
    {
        { t.AddRefWeak() } -> std::same_as<u32>;
        { t.ReleaseWeak() } -> std::same_as<u32>;
        { t.TryUpgrade() } -> std::same_as<u32>;
        { t.TryDowngrade() } -> std::same_as<u32>;
    };
}

#endif //COPLT_COM_CONCEPTS_H

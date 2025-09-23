#include "../includes/CoCom.h"

namespace Coplt
{
    struct Foo : ComProxy<IWeak>::Proxy<>
    {
    protected:
        HResult Impl_QueryInterface(const Guid& guid, void*& object) const override;
        [[nodiscard]] u32 Impl_AddRef() const override;
        [[nodiscard]] u32 Impl_Release() const override;
        [[nodiscard]] u32 Impl_AddRefWeak() const override;
        [[nodiscard]] u32 Impl_ReleaseWeak() const override;
        [[nodiscard]] bool Impl_TryUpgrade() const override;
        [[nodiscard]] bool Impl_TryDowngrade() const override;
    };

    HResult Foo::Impl_QueryInterface(const Guid& guid, void*& object) const
    {
        return HResult::NoInterface;
    }

    u32 Foo::Impl_AddRef() const
    {
        return 0;
    }

    u32 Foo::Impl_Release() const
    {
        return 0;
    }

    u32 Foo::Impl_AddRefWeak() const
    {
        return 0;
    }

    u32 Foo::Impl_ReleaseWeak() const
    {
        return 0;
    }

    bool Foo::Impl_TryUpgrade() const
    {
        return 0;
    }

    bool Foo::Impl_TryDowngrade() const
    {
        return 0;
    }

    __declspec( dllexport ) Foo* func()
    {
        return new Foo;
    }
}

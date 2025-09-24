#include <print>

#include "../headers/Interface.h"

using namespace Coplt;

struct Foo : ComObject<Test1::ITest1>
{
protected:
    u32 Impl_Add(u32 a, u32 b) const override
    {
        return a + b;
    }
};

int main(int argc, char** argv)
{
    Rc a(new Foo);
    std::print("{}", a->Add(1, 2));
    return 0;
}

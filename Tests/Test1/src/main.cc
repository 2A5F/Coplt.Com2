#include "../headers/Types.h"

using namespace Coplt;

struct Foo : ComObject<IWeak>
{
    static Rc<Foo> some()
    {
        return Rc(new Foo());
    }

    void some1(Weak<Foo> a)
    {
        if (Rc<Foo> r = a.upgrade())
        {
        }
    }
};

int main(int argc, char** argv)
{
    Rc a = Foo::some();
    Rc b = a;
    b->some1(a.downgrade());
    return 0;
}

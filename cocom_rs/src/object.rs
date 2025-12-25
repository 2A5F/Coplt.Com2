use core::{
    alloc::Layout,
    hash::Hash,
    mem::ManuallyDrop,
    ptr::{self, NonNull},
    sync::atomic::{AtomicU32, Ordering},
};

use crate::{
    com_ptr::*,
    impls::{self, ObjectBox, WeakRefCount},
    *,
};

pub trait ObjectAllocator {
    #[inline]
    unsafe fn alloc(&self, layout: Layout) -> *mut u8;
    #[inline]
    unsafe fn alloc_zeroed(&self, layout: Layout) -> *mut u8;
    #[inline]
    unsafe fn dealloc(&self, ptr: *mut u8, _layout: Layout);
    #[inline]
    unsafe fn realloc(&self, ptr: *mut u8, layout: Layout, new_size: usize) -> *mut u8;
}

pub type DefaultObjectAllocator = ();

impl ObjectAllocator for DefaultObjectAllocator {
    unsafe fn alloc(&self, layout: Layout) -> *mut u8 {
        unsafe { alloc::alloc::alloc(layout) }
    }

    unsafe fn alloc_zeroed(&self, layout: Layout) -> *mut u8 {
        unsafe { alloc::alloc::alloc_zeroed(layout) }
    }

    unsafe fn dealloc(&self, ptr: *mut u8, layout: Layout) {
        unsafe { alloc::alloc::dealloc(ptr, layout) }
    }

    unsafe fn realloc(&self, ptr: *mut u8, layout: Layout, new_size: usize) -> *mut u8 {
        unsafe { alloc::alloc::realloc(ptr, layout, new_size) }
    }
}

pub trait MakeObject {
    type ComOutput;
    type ObjOutput;

    fn make_com(self) -> Self::ComOutput;
    fn make_object(self) -> Self::ObjOutput;
}

pub trait MakeObjectWeak {
    type ComOutput;
    type ObjOutput;

    fn make_com_weak(self) -> Self::ComOutput;
    fn make_object_weak(self) -> Self::ObjOutput;
}

pub trait MakeObjectWith<A: object::ObjectAllocator> {
    type ComOutput;
    type ObjOutput;

    fn make_com_with(self, allocator: A) -> Self::ComOutput;
    fn make_object_with(self, allocator: A) -> Self::ObjOutput;
}

pub trait MakeObjectWeakWith<A: object::ObjectAllocator> {
    type ComOutput;
    type ObjOutput;

    fn make_com_weak_with(self, allocator: A) -> Self::ComOutput;
    fn make_object_weak_with(self, allocator: A) -> Self::ObjOutput;
}

impl<T: impls::Object<DefaultObjectAllocator>> MakeObject for T
where
    T::Interface: RefCount,
    Object<T, DefaultObjectAllocator>:
        impls::ObjectBox<DefaultObjectAllocator, Object = T> + impls::ObjectBoxNew,
{
    type ComOutput = ComPtr<T::Interface>;
    type ObjOutput = ObjectPtr<T, DefaultObjectAllocator>;

    fn make_object(self) -> Self::ObjOutput {
        unsafe {
            ObjectPtr(ComPtr::new(NonNull::new_unchecked(<Object<
                T,
                DefaultObjectAllocator,
            > as impls::ObjectBoxNew>::make_with(
                self, ()
            ))))
        }
    }

    fn make_com(self) -> Self::ComOutput {
        unsafe {
            ComPtr::new(NonNull::new_unchecked(
                <Object<T, DefaultObjectAllocator> as impls::ObjectBoxNew>::new_with(self, ()),
            ))
        }
    }
}

impl<T: impls::Object<A>, A: object::ObjectAllocator> MakeObjectWith<A> for T
where
    T::Interface: RefCount,
    Object<T, A>: impls::ObjectBox<A, Object = T> + impls::ObjectBoxNew<A>,
{
    type ComOutput = ComPtr<T::Interface>;
    type ObjOutput = ObjectPtr<T, A>;

    fn make_object_with(self, allocator: A) -> Self::ObjOutput {
        unsafe {
            ObjectPtr(ComPtr::new(NonNull::new_unchecked(
                <Object<T, A> as impls::ObjectBoxNew<A>>::make_with(self, allocator),
            )))
        }
    }

    fn make_com_with(self, allocator: A) -> Self::ComOutput {
        unsafe {
            ComPtr::new(NonNull::new_unchecked(
                <Object<T, A> as impls::ObjectBoxNew<A>>::new_with(self, allocator),
            ))
        }
    }
}

impl<T: impls::Object> MakeObjectWeak for T
where
    T::Interface: RefCount + WeakRefCount,
    WeakObject<T, DefaultObjectAllocator>: impls::ObjectBox<Object = T> + impls::ObjectBoxNew,
{
    type ComOutput = ComPtr<T::Interface>;
    type ObjOutput = WeakObjectPtr<T, DefaultObjectAllocator>;

    fn make_com_weak(self) -> Self::ComOutput {
        unsafe {
            ComPtr::new(NonNull::new_unchecked(<WeakObject<
                T,
                DefaultObjectAllocator,
            > as impls::ObjectBoxNew>::new_with(
                self, ()
            )))
        }
    }

    fn make_object_weak(self) -> Self::ObjOutput {
        unsafe {
            WeakObjectPtr(ComPtr::new(NonNull::new_unchecked(<WeakObject<
                T,
                DefaultObjectAllocator,
            > as impls::ObjectBoxNew>::make_with(
                self, ()
            ))))
        }
    }
}

impl<T: impls::Object<A>, A: object::ObjectAllocator> MakeObjectWeakWith<A> for T
where
    T::Interface: RefCount + WeakRefCount,
    WeakObject<T, A>: impls::ObjectBox<A, Object = T> + impls::ObjectBoxNew<A>,
{
    type ComOutput = ComPtr<T::Interface>;
    type ObjOutput = WeakObjectPtr<T, A>;

    fn make_com_weak_with(self, allocator: A) -> Self::ComOutput {
        unsafe {
            ComPtr::new(NonNull::new_unchecked(
                <WeakObject<T, A> as impls::ObjectBoxNew<A>>::new_with(self, allocator),
            ))
        }
    }

    fn make_object_weak_with(self, allocator: A) -> Self::ObjOutput {
        unsafe {
            WeakObjectPtr(ComPtr::new(NonNull::new_unchecked(
                <WeakObject<T, A> as impls::ObjectBoxNew<A>>::make_with(self, allocator),
            )))
        }
    }
}

#[repr(C)]
#[derive(Debug)]
pub struct Object<T: impls::Object<A>, A: object::ObjectAllocator = object::DefaultObjectAllocator>
{
    base: T::Interface,
    allocator: A,
    strong: AtomicU32,
    val: ManuallyDrop<T>,
}

impl<T: impls::Object<A>, A: object::ObjectAllocator> Object<T, A> {
    unsafe fn Drop(this: *mut Self) {
        unsafe {
            ManuallyDrop::drop(&mut (*this).val);
            let allocator = ptr::read(&(*this).allocator);
            allocator.dealloc(this as _, Layout::new::<Self>());
        }
    }
}

impl<T: impls::Object<A>, A: object::ObjectAllocator> Object<T, A> {
    pub unsafe fn GetStrongCount(this: *mut Self) -> u32 {
        unsafe { (*this).strong.load(Ordering::Acquire) }
    }

    pub unsafe fn FromValue(value: *mut T) -> *mut Self {
        unsafe {
            let offset = core::mem::size_of::<T::Interface>() + core::mem::size_of::<AtomicU32>();
            let ptr = value as *mut u8;
            let ptr = ptr.sub(offset);
            ptr as _
        }
    }
}

impl<T: impls::Object<DefaultObjectAllocator>> Object<T, DefaultObjectAllocator>
where
    T::Interface: details::Vtbl<Self, DefaultObjectAllocator>,
{
    pub fn new(val: T) -> *mut T::Interface {
        Self::make(val) as _
    }

    pub fn make(val: T) -> *mut Self {
        unsafe {
            let b = ().alloc(Layout::new::<Self>()) as *mut Self;
            b.write(Self {
                base: T::Interface::new(<T::Interface as details::Vtbl<
                    Self,
                    DefaultObjectAllocator,
                >>::vtbl()),
                allocator: (),
                strong: AtomicU32::new(1),
                val: ManuallyDrop::new(val),
            });
            b as _
        }
    }

    pub unsafe fn inplace(init: impl FnOnce(*mut T)) -> ObjectPtr<T> {
        unsafe {
            let b = ().alloc(Layout::new::<Self>()) as *mut Self;
            pmp!(b; .base).write(T::Interface::new(<T::Interface as details::Vtbl<
                Self,
                DefaultObjectAllocator,
            >>::vtbl()));
            pmp!(b; .strong).write(AtomicU32::new(1));
            init(pmp!(b; .val) as *mut _);
            ObjectPtr(ComPtr::new(NonNull::new_unchecked(b)))
        }
    }
}

impl<T: impls::Object<A>, A: object::ObjectAllocator> Object<T, A>
where
    T::Interface: details::Vtbl<Self, A>,
{
    pub fn new_with(val: T, allocator: A) -> *mut T::Interface {
        Self::make_with(val, allocator) as _
    }

    pub fn make_with(val: T, allocator: A) -> *mut Self {
        unsafe {
            let b = allocator.alloc(Layout::new::<Self>()) as *mut Self;
            b.write(Self {
                base: T::Interface::new(<T::Interface as details::Vtbl<Self, A>>::vtbl()),
                allocator: allocator,
                strong: AtomicU32::new(1),
                val: ManuallyDrop::new(val),
            });
            b as _
        }
    }

    pub unsafe fn inplace_with(allocator: A, init: impl FnOnce(*mut T)) -> ObjectPtr<T, A> {
        unsafe {
            let b = allocator.alloc(Layout::new::<Self>()) as *mut Self;
            pmp!(b; .base).write(T::Interface::new(<T::Interface as details::Vtbl<
                Self,
                A,
            >>::vtbl()));
            if size_of::<A>() > 0 {
                pmp!(b; .allocator).write(allocator);
            }
            pmp!(b; .strong).write(AtomicU32::new(1));
            init(pmp!(b; .val) as *mut _);
            ObjectPtr(ComPtr::new(NonNull::new_unchecked(b)))
        }
    }
}

impl<T: impls::Object<A>, A: object::ObjectAllocator> impls::ObjectBoxNew<A> for Object<T, A>
where
    T::Interface: details::Vtbl<Self, A>,
{
    fn new_with(val: T, allocator: A) -> *mut T::Interface {
        Self::new_with(val, allocator)
    }

    fn make_with(val: Self::Object, allocator: A) -> *mut Self {
        Self::make_with(val, allocator)
    }
}

impl<T: impls::Object<A>, A: object::ObjectAllocator> impls::ObjectBox<A> for Object<T, A> {
    type Object = T;

    unsafe fn GetObject(
        this: *mut <Self::Object as impls::Object<A>>::Interface,
    ) -> *mut Self::Object {
        unsafe {
            let this = this as *mut Self;
            &mut *(*this).val
        }
    }

    unsafe fn AddRef(this: *mut T::Interface) -> u32 {
        unsafe {
            let this = this as *mut Self;
            (*this).strong.fetch_add(1, Ordering::Relaxed)
        }
    }

    unsafe fn Release(this: *mut T::Interface) -> u32 {
        unsafe {
            let this = this as *mut Self;
            let r = (*this).strong.fetch_sub(1, Ordering::Release);
            if r == 1 {
                Self::Drop(this);
            }
            r
        }
    }
}

impl<T: impls::Object<A>, A: object::ObjectAllocator> impls::RefCount for Object<T, A> {
    fn AddRef(this: *const Self) -> u32 {
        unsafe { (*this).strong.fetch_add(1, Ordering::Relaxed) }
    }

    fn Release(this: *const Self) -> u32 {
        unsafe {
            let this = this as *mut Self;
            let r = (*this).strong.fetch_sub(1, Ordering::Release);
            if r == 1 {
                Self::Drop(this);
            }
            r
        }
    }
}

impl<T: impls::Object<A>, A: object::ObjectAllocator> Deref for Object<T, A> {
    type Target = T;

    fn deref(&self) -> &Self::Target {
        &*self.val
    }
}

impl<T: impls::Object<A> + PartialEq, A: object::ObjectAllocator> PartialEq for Object<T, A> {
    fn eq(&self, other: &Self) -> bool {
        (**self).eq(&**other)
    }
}

impl<T: impls::Object<A> + Eq, A: object::ObjectAllocator> Eq for Object<T, A> {}

impl<T: impls::Object<A> + PartialOrd, A: object::ObjectAllocator> PartialOrd for Object<T, A> {
    fn partial_cmp(&self, other: &Self) -> Option<core::cmp::Ordering> {
        (**self).partial_cmp(&**other)
    }
}

impl<T: impls::Object<A> + Ord, A: object::ObjectAllocator> Ord for Object<T, A> {
    fn cmp(&self, other: &Self) -> core::cmp::Ordering {
        (**self).cmp(&**other)
    }
}

impl<T: impls::Object<A> + Hash, A: object::ObjectAllocator> Hash for Object<T, A> {
    fn hash<H: core::hash::Hasher>(&self, state: &mut H) {
        (**self).hash(state);
    }
}

#[derive(Debug, Clone, PartialEq, Eq, PartialOrd, Ord, Hash)]
#[repr(transparent)]
pub struct ObjectPtr<T: impls::Object<A>, A: object::ObjectAllocator = DefaultObjectAllocator>(
    ComPtr<Object<T, A>>,
);

impl<T: impls::Object<A>, A: object::ObjectAllocator> ObjectPtr<T, A>
where
    T::Interface: RefCount,
{
    pub fn leak(self) -> *mut T::Interface {
        unsafe { core::mem::transmute(self) }
    }

    pub fn to_com(self) -> ComPtr<T::Interface> {
        unsafe { core::mem::transmute(self) }
    }

    pub fn as_com(&self) -> &ComPtr<T::Interface> {
        unsafe { core::mem::transmute(self) }
    }
}

impl<T: impls::RefCount> ComPtr<T> {
    pub unsafe fn to_object<O: impls::Object<Interface = T>>(self) -> ObjectPtr<O> {
        unsafe { core::mem::transmute(self) }
    }
    pub unsafe fn as_object<O: impls::Object<Interface = T>>(&self) -> &ObjectPtr<O> {
        unsafe { core::mem::transmute(self) }
    }
}

impl<T: impls::Object<A>, A: object::ObjectAllocator> Deref for ObjectPtr<T, A> {
    type Target = T;

    fn deref(&self) -> &Self::Target {
        &*self.0.val
    }
}

impl<T: impls::Object<A>, A: object::ObjectAllocator> AsRef<T::Interface> for ObjectPtr<T, A> {
    fn as_ref(&self) -> &T::Interface {
        &self.0.base
    }
}

#[repr(C)]
#[derive(Debug)]
pub struct WeakObject<
    T: impls::Object<A>,
    A: object::ObjectAllocator = object::DefaultObjectAllocator,
> {
    base: T::Interface,
    allocator: A,
    strong: AtomicU32,
    weak: AtomicU32,
    val: ManuallyDrop<T>,
}

impl<T: impls::Object<A>, A: object::ObjectAllocator> WeakObject<T, A> {
    #[inline(never)]
    unsafe fn DropSlow(this: *mut Self) {
        unsafe {
            ManuallyDrop::drop(&mut (*this).val);
            Self::ReleaseWeak_(this);
        }
    }

    pub unsafe fn ReleaseWeak_(this: *mut Self) -> u32 {
        unsafe {
            let r = (*this).weak.fetch_sub(1, Ordering::Release);
            if r == 1 {
                Self::Drop(this);
            }
            r
        }
    }

    unsafe fn Drop(this: *mut Self) {
        unsafe {
            let allocator = ptr::read(&(*this).allocator);
            allocator.dealloc(this as _, Layout::new::<Self>());
        }
    }
}
impl<T: impls::Object<A>, A: object::ObjectAllocator> WeakObject<T, A> {
    pub unsafe fn GetStrongCount(this: *mut Self) -> u32 {
        unsafe { (*this).strong.load(Ordering::Acquire) }
    }

    pub unsafe fn GetWeakCount(this: *mut Self) -> u32 {
        unsafe { (*this).weak.load(Ordering::Acquire) }
    }

    pub unsafe fn FromValue(value: *mut T) -> *mut Self {
        unsafe {
            let offset =
                core::mem::size_of::<T::Interface>() + core::mem::size_of::<AtomicU32>() * 2;
            let ptr = value as *mut u8;
            let ptr = ptr.sub(offset);
            ptr as _
        }
    }
}

impl<T: impls::Object<DefaultObjectAllocator>> WeakObject<T, DefaultObjectAllocator>
where
    T::Interface: details::Vtbl<Self, DefaultObjectAllocator>,
{
    pub fn new(val: T) -> *mut T::Interface {
        Self::make(val) as _
    }

    pub fn make(val: T) -> *mut Self {
        unsafe {
            let b = ().alloc(Layout::new::<Self>()) as *mut Self;
            b.write(Self {
                base: T::Interface::new(<T::Interface as details::Vtbl<
                    Self,
                    DefaultObjectAllocator,
                >>::vtbl()),
                allocator: (),
                strong: AtomicU32::new(1),
                weak: AtomicU32::new(1),
                val: ManuallyDrop::new(val),
            });
            b as _
        }
    }

    pub unsafe fn inplace(init: impl FnOnce(*mut T)) -> WeakObjectPtr<T, DefaultObjectAllocator> {
        unsafe {
            let b = ().alloc(Layout::new::<Self>()) as *mut Self;
            pmp!(b; .base).write(T::Interface::new(<T::Interface as details::Vtbl<
                Self,
                DefaultObjectAllocator,
            >>::vtbl()));
            pmp!(b; .strong).write(AtomicU32::new(1));
            pmp!(b; .weak).write(AtomicU32::new(1));
            init(pmp!(b; .val) as *mut _);
            WeakObjectPtr(ComPtr::new(NonNull::new_unchecked(b)))
        }
    }
}

impl<T: impls::Object<A>, A: object::ObjectAllocator> WeakObject<T, A>
where
    T::Interface: details::Vtbl<Self, A>,
{
    pub fn new_with(val: T, allocator: A) -> *mut T::Interface {
        Self::make_with(val, allocator) as _
    }

    pub fn make_with(val: T, allocator: A) -> *mut Self {
        unsafe {
            let b = allocator.alloc(Layout::new::<Self>()) as *mut Self;
            b.write(Self {
                base: T::Interface::new(<T::Interface as details::Vtbl<Self, A>>::vtbl()),
                allocator,
                strong: AtomicU32::new(1),
                weak: AtomicU32::new(1),
                val: ManuallyDrop::new(val),
            });
            b as _
        }
    }

    pub unsafe fn inplace_with(allocator: A, init: impl FnOnce(*mut T)) -> WeakObjectPtr<T, A> {
        unsafe {
            let b = allocator.alloc(Layout::new::<Self>()) as *mut Self;
            pmp!(b; .base).write(T::Interface::new(<T::Interface as details::Vtbl<
                Self,
                A,
            >>::vtbl()));
            if size_of::<A>() > 0 {
                pmp!(b; .allocator).write(allocator);
            }
            pmp!(b; .strong).write(AtomicU32::new(1));
            pmp!(b; .weak).write(AtomicU32::new(1));
            init(pmp!(b; .val) as *mut _);
            WeakObjectPtr(ComPtr::new(NonNull::new_unchecked(b)))
        }
    }
}

impl<T: impls::Object<A>, A: object::ObjectAllocator> impls::ObjectBoxNew<A> for WeakObject<T, A>
where
    T::Interface: details::Vtbl<Self, A>,
{
    fn new_with(val: T, allocator: A) -> *mut T::Interface {
        Self::new_with(val, allocator)
    }

    fn make_with(val: Self::Object, allocator: A) -> *mut Self {
        Self::make_with(val, allocator)
    }
}

impl<T: impls::Object<A>, A: object::ObjectAllocator> impls::ObjectBox<A> for WeakObject<T, A> {
    type Object = T;

    unsafe fn GetObject(
        this: *mut <Self::Object as impls::Object<A>>::Interface,
    ) -> *mut Self::Object {
        unsafe {
            let this = this as *mut Self;
            &mut *(*this).val
        }
    }

    unsafe fn AddRef(this: *mut T::Interface) -> u32 {
        unsafe {
            let this = this as *mut Self;
            (*this).strong.fetch_add(1, Ordering::Relaxed)
        }
    }

    unsafe fn Release(this: *mut T::Interface) -> u32 {
        unsafe {
            let this = this as *mut Self;
            let r = (*this).strong.fetch_sub(1, Ordering::Release);
            if r == 1 {
                Self::DropSlow(this);
            }
            r
        }
    }
}

impl<T: impls::Object<A>, A: object::ObjectAllocator> impls::ObjectBoxWeak<A> for WeakObject<T, A>
where
    T::Interface: details::QuIn<T, Self, A>,
{
    unsafe fn AddRefWeak(this: *mut T::Interface) -> u32 {
        unsafe {
            let this = this as *mut Self;
            (*this).weak.fetch_add(1, Ordering::Relaxed)
        }
    }

    unsafe fn ReleaseWeak(this: *mut T::Interface) -> u32 {
        unsafe { Self::ReleaseWeak_(this as _) }
    }

    unsafe fn TryUpgrade(this: *mut T::Interface) -> bool {
        unsafe {
            let this = this as *mut Self;

            #[inline]
            fn checked_increment(n: u32) -> Option<u32> {
                if n == 0 {
                    return None;
                }
                Some(n + 1)
            }

            (*this)
                .strong
                .fetch_update(Ordering::Acquire, Ordering::Relaxed, checked_increment)
                .is_ok()
        }
    }

    unsafe fn TryDowngrade(this: *mut T::Interface) -> bool {
        unsafe {
            let this = this as *mut Self;

            #[inline]
            fn checked_increment(n: u32) -> Option<u32> {
                if n == 0 {
                    return None;
                }
                Some(n + 1)
            }

            (*this)
                .weak
                .fetch_update(Ordering::Acquire, Ordering::Relaxed, checked_increment)
                .is_ok()
        }
    }
}

impl<T: impls::Object<A>, A: object::ObjectAllocator> impls::RefCount for WeakObject<T, A> {
    fn AddRef(this: *const Self) -> u32 {
        unsafe { (*this).strong.fetch_add(1, Ordering::Relaxed) }
    }

    fn Release(this: *const Self) -> u32 {
        unsafe {
            let r = (*this).strong.fetch_sub(1, Ordering::Release);
            if r == 1 {
                Self::DropSlow(this as _);
            }
            r
        }
    }
}

impl<T: impls::Object<A>, A: object::ObjectAllocator> impls::WeakRefCount for WeakObject<T, A> {
    fn AddRefWeak(this: *const Self) -> u32 {
        unsafe { (*this).weak.fetch_add(1, Ordering::Relaxed) }
    }

    fn ReleaseWeak(this: *const Self) -> u32 {
        unsafe { Self::ReleaseWeak_(this as _) }
    }

    fn TryUpgrade(this: *const Self) -> bool {
        #[inline]
        fn checked_increment(n: u32) -> Option<u32> {
            if n == 0 {
                return None;
            }
            Some(n + 1)
        }

        unsafe {
            (*this)
                .strong
                .fetch_update(Ordering::Acquire, Ordering::Relaxed, checked_increment)
                .is_ok()
        }
    }
}

impl<T: impls::Object<A>, A: object::ObjectAllocator> Deref for WeakObject<T, A> {
    type Target = T;

    fn deref(&self) -> &Self::Target {
        &*self.val
    }
}

impl<T: impls::Object<A> + PartialEq, A: object::ObjectAllocator> PartialEq for WeakObject<T, A> {
    fn eq(&self, other: &Self) -> bool {
        (**self).eq(&**other)
    }
}

impl<T: impls::Object<A> + Eq, A: object::ObjectAllocator> Eq for WeakObject<T, A> {}

impl<T: impls::Object<A> + PartialOrd, A: object::ObjectAllocator> PartialOrd for WeakObject<T, A> {
    fn partial_cmp(&self, other: &Self) -> Option<core::cmp::Ordering> {
        (**self).partial_cmp(&**other)
    }
}

impl<T: impls::Object<A> + Ord, A: object::ObjectAllocator> Ord for WeakObject<T, A> {
    fn cmp(&self, other: &Self) -> core::cmp::Ordering {
        (**self).cmp(&**other)
    }
}

impl<T: impls::Object<A> + Hash, A: object::ObjectAllocator> Hash for WeakObject<T, A> {
    fn hash<H: core::hash::Hasher>(&self, state: &mut H) {
        (**self).hash(state);
    }
}

#[derive(Debug, Clone, PartialEq, Eq, PartialOrd, Ord, Hash)]
#[repr(transparent)]
pub struct WeakObjectPtr<
    T: impls::Object<A>,
    A: object::ObjectAllocator = object::DefaultObjectAllocator,
>(ComPtr<WeakObject<T, A>>);

impl<T: impls::Object<A>, A: object::ObjectAllocator> WeakObjectPtr<T, A>
where
    T::Interface: RefCount,
{
    pub fn leak(self) -> *mut T::Interface {
        unsafe { core::mem::transmute(self) }
    }

    pub fn to_com(self) -> ComPtr<T::Interface> {
        unsafe { core::mem::transmute(self) }
    }
}

impl<T: impls::Object<A>, A: object::ObjectAllocator> Deref for WeakObjectPtr<T, A> {
    type Target = T;

    fn deref(&self) -> &Self::Target {
        &*self.0.val
    }
}

impl<T: impls::Object<A>, A: object::ObjectAllocator> AsRef<T::Interface> for WeakObjectPtr<T, A> {
    fn as_ref(&self) -> &T::Interface {
        &self.0.base
    }
}

#[derive(Debug, Clone)]
#[repr(transparent)]
pub struct WeakObjectWeak<
    T: impls::Object<A>,
    A: object::ObjectAllocator = object::DefaultObjectAllocator,
>(ComWeak<WeakObject<T, A>>);

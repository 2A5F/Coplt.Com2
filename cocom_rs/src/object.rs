use core::{
    alloc::Layout,
    hash::Hash,
    mem::ManuallyDrop,
    ops::DerefMut,
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

pub trait MakeObjectWith {
    type Allocator;
    type ComOutput;
    type ObjOutput;

    fn make_com_with(self, allocator: Self::Allocator) -> Self::ComOutput;
    fn make_object_with(self, allocator: Self::Allocator) -> Self::ObjOutput;
}

pub trait MakeObjectWeakWith {
    type Allocator;
    type ComOutput;
    type ObjOutput;

    fn make_com_weak_with(self, allocator: Self::Allocator) -> Self::ComOutput;
    fn make_object_weak_with(self, allocator: Self::Allocator) -> Self::ObjOutput;
}

impl<T: impls::Object<Allocator = DefaultObjectAllocator>> MakeObject for T
where
    T::Interface: RefCount,
    Object<T>: impls::ObjectBox<Object = T> + impls::ObjectBoxNew,
{
    type ComOutput = ComPtr<T::Interface>;
    type ObjOutput = ObjectPtr<T>;

    fn make_object(self) -> Self::ObjOutput {
        unsafe {
            ObjectPtr(ComPtr::new(NonNull::new_unchecked(
                <Object<T> as impls::ObjectBoxNew>::make_with(self, ()),
            )))
        }
    }

    fn make_com(self) -> Self::ComOutput {
        unsafe {
            ComPtr::new(NonNull::new_unchecked(
                <Object<T> as impls::ObjectBoxNew>::new_with(self, ()),
            ))
        }
    }
}

impl<T: impls::Object> MakeObjectWith for T
where
    T::Interface: RefCount,
    Object<T>: impls::ObjectBox<Object = T> + impls::ObjectBoxNew,
{
    type Allocator = T::Allocator;
    type ComOutput = ComPtr<T::Interface>;
    type ObjOutput = ObjectPtr<T>;

    fn make_object_with(self, allocator: T::Allocator) -> Self::ObjOutput {
        unsafe {
            ObjectPtr(ComPtr::new(NonNull::new_unchecked(
                <Object<T> as impls::ObjectBoxNew>::make_with(self, allocator),
            )))
        }
    }

    fn make_com_with(self, allocator: T::Allocator) -> Self::ComOutput {
        unsafe {
            ComPtr::new(NonNull::new_unchecked(
                <Object<T> as impls::ObjectBoxNew>::new_with(self, allocator),
            ))
        }
    }
}

impl<T: impls::Object<Allocator = DefaultObjectAllocator>> MakeObjectWeak for T
where
    T::Interface: RefCount + WeakRefCount,
    WeakObject<T>: impls::ObjectBox<Object = T> + impls::ObjectBoxNew,
{
    type ComOutput = ComPtr<T::Interface>;
    type ObjOutput = WeakObjectPtr<T>;

    fn make_com_weak(self) -> Self::ComOutput {
        unsafe {
            ComPtr::new(NonNull::new_unchecked(
                <WeakObject<T> as impls::ObjectBoxNew>::new_with(self, ()),
            ))
        }
    }

    fn make_object_weak(self) -> Self::ObjOutput {
        unsafe {
            WeakObjectPtr(ComPtr::new(NonNull::new_unchecked(
                <WeakObject<T> as impls::ObjectBoxNew>::make_with(self, ()),
            )))
        }
    }
}

impl<T: impls::Object> MakeObjectWeakWith for T
where
    T::Interface: RefCount + WeakRefCount,
    WeakObject<T>: impls::ObjectBox<Object = T> + impls::ObjectBoxNew,
{
    type Allocator = T::Allocator;
    type ComOutput = ComPtr<T::Interface>;
    type ObjOutput = WeakObjectPtr<T>;

    fn make_com_weak_with(self, allocator: T::Allocator) -> Self::ComOutput {
        unsafe {
            ComPtr::new(NonNull::new_unchecked(
                <WeakObject<T> as impls::ObjectBoxNew>::new_with(self, allocator),
            ))
        }
    }

    fn make_object_weak_with(self, allocator: T::Allocator) -> Self::ObjOutput {
        unsafe {
            WeakObjectPtr(ComPtr::new(NonNull::new_unchecked(
                <WeakObject<T> as impls::ObjectBoxNew>::make_with(self, allocator),
            )))
        }
    }
}

#[repr(C)]
#[derive(Debug)]
pub struct Object<T: impls::Object> {
    base: T::Interface,
    allocator: T::Allocator,
    strong: AtomicU32,
    val: ManuallyDrop<T>,
}

impl<T: impls::Object> Object<T> {
    unsafe fn Drop(this: *mut Self) {
        unsafe {
            ManuallyDrop::drop(&mut (*this).val);
            let allocator = ptr::read(&(*this).allocator);
            allocator.dealloc(this as _, Layout::new::<Self>());
        }
    }
}

impl<T: impls::Object> Object<T> {
    pub unsafe fn GetStrongCount(this: *mut Self) -> u32 {
        unsafe { (*this).strong.load(Ordering::Acquire) }
    }

    pub unsafe fn FromValue(value: *mut T) -> *mut Self {
        unsafe {
            let offset = {
                let p: *mut Self = value as _;
                let fp: *mut ManuallyDrop<T> = &mut (*p).val;
                fp as usize - p as usize
            };
            let ptr = value as *mut u8;
            let ptr = ptr.sub(offset);
            ptr as _
        }
    }
}

impl<T: impls::Object> Object<T> {
    pub fn allocator(&self) -> &T::Allocator {
        unsafe { &self.allocator }
    }
}

impl<T: impls::Object<Allocator = DefaultObjectAllocator>> Object<T>
where
    T::Interface: details::Vtbl<Self>,
{
    pub fn new(val: T) -> *mut T::Interface {
        Self::make(val) as _
    }

    pub fn make(val: T) -> *mut Self {
        unsafe {
            let b = ().alloc(Layout::new::<Self>()) as *mut Self;
            b.write(Self {
                base: T::Interface::new(<T::Interface as details::Vtbl<Self>>::vtbl()),
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
            pmp!(b; .base).write(T::Interface::new(
                <T::Interface as details::Vtbl<Self>>::vtbl(),
            ));
            pmp!(b; .strong).write(AtomicU32::new(1));
            init(pmp!(b; .val) as *mut _);
            ObjectPtr(ComPtr::new(NonNull::new_unchecked(b)))
        }
    }
}

impl<T: impls::Object> Object<T>
where
    T::Interface: details::Vtbl<Self>,
{
    pub fn new_with(val: T, allocator: T::Allocator) -> *mut T::Interface {
        Self::make_with(val, allocator) as _
    }

    pub fn make_with(val: T, allocator: T::Allocator) -> *mut Self {
        unsafe {
            let b = allocator.alloc(Layout::new::<Self>()) as *mut Self;
            b.write(Self {
                base: T::Interface::new(<T::Interface as details::Vtbl<Self>>::vtbl()),
                allocator: allocator,
                strong: AtomicU32::new(1),
                val: ManuallyDrop::new(val),
            });
            b as _
        }
    }

    pub unsafe fn inplace_with(allocator: T::Allocator, init: impl FnOnce(*mut T)) -> ObjectPtr<T> {
        unsafe {
            let b = allocator.alloc(Layout::new::<Self>()) as *mut Self;
            pmp!(b; .base).write(T::Interface::new(
                <T::Interface as details::Vtbl<Self>>::vtbl(),
            ));
            if size_of::<T::Allocator>() > 0 {
                pmp!(b; .allocator).write(allocator);
            }
            pmp!(b; .strong).write(AtomicU32::new(1));
            init(pmp!(b; .val) as *mut _);
            ObjectPtr(ComPtr::new(NonNull::new_unchecked(b)))
        }
    }
}

impl<T: impls::Object> impls::ObjectBoxNew for Object<T>
where
    T::Interface: details::Vtbl<Self>,
{
    fn new_with(val: T, allocator: T::Allocator) -> *mut T::Interface {
        Self::new_with(val, allocator)
    }

    fn make_with(val: Self::Object, allocator: T::Allocator) -> *mut Self {
        Self::make_with(val, allocator)
    }
}

impl<T: impls::Object> impls::ObjectBox for Object<T> {
    type Object = T;

    #[inline(always)]
    unsafe fn GetObject(
        this: *mut <Self::Object as impls::Object>::Interface,
    ) -> *mut Self::Object {
        unsafe {
            let this = this as *mut Self;
            &mut *(*this).val
        }
    }

    #[inline(always)]
    unsafe fn AddRef(this: *mut T::Interface) -> u32 {
        unsafe {
            let this = this as *mut Self;
            (*this).strong.fetch_add(1, Ordering::Relaxed)
        }
    }

    #[inline(always)]
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

impl<T: impls::Object> impls::RefCount for Object<T> {
    #[inline(always)]
    fn AddRef(this: *const Self) -> u32 {
        unsafe { (*this).strong.fetch_add(1, Ordering::Relaxed) }
    }

    #[inline(always)]
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

impl<T: impls::Object> Deref for Object<T> {
    type Target = T;

    fn deref(&self) -> &Self::Target {
        &*self.val
    }
}

impl<T: impls::Object + PartialEq> PartialEq for Object<T> {
    fn eq(&self, other: &Self) -> bool {
        (**self).eq(&**other)
    }
}

impl<T: impls::Object + Eq> Eq for Object<T> {}

impl<T: impls::Object + PartialOrd> PartialOrd for Object<T> {
    fn partial_cmp(&self, other: &Self) -> Option<core::cmp::Ordering> {
        (**self).partial_cmp(&**other)
    }
}

impl<T: impls::Object + Ord> Ord for Object<T> {
    fn cmp(&self, other: &Self) -> core::cmp::Ordering {
        (**self).cmp(&**other)
    }
}

impl<T: impls::Object + Hash> Hash for Object<T> {
    fn hash<H: core::hash::Hasher>(&self, state: &mut H) {
        (**self).hash(state);
    }
}

#[derive(Debug, PartialEq, Eq, PartialOrd, Ord, Hash)]
#[repr(transparent)]
pub struct ObjectPtr<T: impls::Object>(ComPtr<Object<T>>);

impl<T: impls::Object> ObjectPtr<T> {
    pub unsafe fn clone_this(this: &mut T) -> Self {
        unsafe {
            let obj = Object::FromValue(this);
            ObjectPtr(ComPtr::new_clone(NonNull::new_unchecked(obj)))
        }
    }

    pub unsafe fn clone_from_ptr(ptr: *mut T::Interface) -> Self {
        unsafe { ObjectPtr(ComPtr::new_clone(NonNull::new_unchecked(ptr as _))) }
    }
}

impl<T: impls::Object> Clone for ObjectPtr<T>
where
    T::Interface: RefCount,
{
    fn clone(&self) -> Self {
        Self(self.0.clone())
    }
}

impl<T: impls::Object> ObjectPtr<T>
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

impl<T: impls::Object> Deref for ObjectPtr<T> {
    type Target = T;

    fn deref(&self) -> &Self::Target {
        &*self.0.val
    }
}

impl<T: impls::Object> DerefMut for ObjectPtr<T> {
    fn deref_mut(&mut self) -> &mut Self::Target {
        &mut *self.0.val
    }
}

impl<T: impls::Object> AsRef<T::Interface> for ObjectPtr<T> {
    fn as_ref(&self) -> &T::Interface {
        &self.0.base
    }
}

#[repr(C)]
#[derive(Debug)]
pub struct WeakObject<T: impls::Object> {
    base: T::Interface,
    allocator: T::Allocator,
    strong: AtomicU32,
    weak: AtomicU32,
    val: ManuallyDrop<T>,
}

impl<T: impls::Object> WeakObject<T> {
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
impl<T: impls::Object> WeakObject<T> {
    #[inline(always)]
    pub unsafe fn GetStrongCount(this: *mut Self) -> u32 {
        unsafe { (*this).strong.load(Ordering::Acquire) }
    }

    #[inline(always)]
    pub unsafe fn GetWeakCount(this: *mut Self) -> u32 {
        unsafe { (*this).weak.load(Ordering::Acquire) }
    }

    #[inline(always)]
    pub unsafe fn FromValue(value: *mut T) -> *mut Self {
        unsafe {
            let offset = {
                let p: *mut Self = value as _;
                let fp: *mut ManuallyDrop<T> = &mut (*p).val;
                fp as usize - p as usize
            };
            let ptr = value as *mut u8;
            let ptr = ptr.sub(offset);
            ptr as _
        }
    }
}

impl<T: impls::Object> WeakObject<T> {
    pub fn allocator(&self) -> &T::Allocator {
        unsafe { &self.allocator }
    }
}

impl<T: impls::Object<Allocator = DefaultObjectAllocator>> WeakObject<T>
where
    T::Interface: details::Vtbl<Self>,
{
    pub fn new(val: T) -> *mut T::Interface {
        Self::make(val) as _
    }

    pub fn make(val: T) -> *mut Self {
        unsafe {
            let b = ().alloc(Layout::new::<Self>()) as *mut Self;
            b.write(Self {
                base: T::Interface::new(<T::Interface as details::Vtbl<Self>>::vtbl()),
                allocator: (),
                strong: AtomicU32::new(1),
                weak: AtomicU32::new(1),
                val: ManuallyDrop::new(val),
            });
            b as _
        }
    }

    pub unsafe fn inplace(init: impl FnOnce(*mut T)) -> WeakObjectPtr<T> {
        unsafe {
            let b = ().alloc(Layout::new::<Self>()) as *mut Self;
            pmp!(b; .base).write(T::Interface::new(
                <T::Interface as details::Vtbl<Self>>::vtbl(),
            ));
            pmp!(b; .strong).write(AtomicU32::new(1));
            pmp!(b; .weak).write(AtomicU32::new(1));
            init(pmp!(b; .val) as *mut _);
            WeakObjectPtr(ComPtr::new(NonNull::new_unchecked(b)))
        }
    }
}

impl<T: impls::Object> WeakObject<T>
where
    T::Interface: details::Vtbl<Self>,
{
    pub fn new_with(val: T, allocator: T::Allocator) -> *mut T::Interface {
        Self::make_with(val, allocator) as _
    }

    pub fn make_with(val: T, allocator: T::Allocator) -> *mut Self {
        unsafe {
            let b = allocator.alloc(Layout::new::<Self>()) as *mut Self;
            b.write(Self {
                base: T::Interface::new(<T::Interface as details::Vtbl<Self>>::vtbl()),
                allocator,
                strong: AtomicU32::new(1),
                weak: AtomicU32::new(1),
                val: ManuallyDrop::new(val),
            });
            b as _
        }
    }

    pub unsafe fn inplace_with(
        allocator: T::Allocator,
        init: impl FnOnce(*mut T),
    ) -> WeakObjectPtr<T> {
        unsafe {
            let b = allocator.alloc(Layout::new::<Self>()) as *mut Self;
            pmp!(b; .base).write(T::Interface::new(
                <T::Interface as details::Vtbl<Self>>::vtbl(),
            ));
            if size_of::<T::Allocator>() > 0 {
                pmp!(b; .allocator).write(allocator);
            }
            pmp!(b; .strong).write(AtomicU32::new(1));
            pmp!(b; .weak).write(AtomicU32::new(1));
            init(pmp!(b; .val) as *mut _);
            WeakObjectPtr(ComPtr::new(NonNull::new_unchecked(b)))
        }
    }
}

impl<T: impls::Object> impls::ObjectBoxNew for WeakObject<T>
where
    T::Interface: details::Vtbl<Self>,
{
    fn new_with(val: T, allocator: T::Allocator) -> *mut T::Interface {
        Self::new_with(val, allocator)
    }

    fn make_with(val: Self::Object, allocator: T::Allocator) -> *mut Self {
        Self::make_with(val, allocator)
    }
}

impl<T: impls::Object> impls::ObjectBox for WeakObject<T> {
    type Object = T;

    unsafe fn GetObject(
        this: *mut <Self::Object as impls::Object>::Interface,
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

impl<T: impls::Object> impls::ObjectBoxWeak for WeakObject<T>
where
    T::Interface: details::QuIn<T::Interface, T, Self>,
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

impl<T: impls::Object> impls::RefCount for WeakObject<T> {
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

impl<T: impls::Object> impls::WeakRefCount for WeakObject<T> {
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

impl<T: impls::Object> Deref for WeakObject<T> {
    type Target = T;

    fn deref(&self) -> &Self::Target {
        &*self.val
    }
}

impl<T: impls::Object + PartialEq> PartialEq for WeakObject<T> {
    fn eq(&self, other: &Self) -> bool {
        (**self).eq(&**other)
    }
}

impl<T: impls::Object + Eq> Eq for WeakObject<T> {}

impl<T: impls::Object + PartialOrd> PartialOrd for WeakObject<T> {
    fn partial_cmp(&self, other: &Self) -> Option<core::cmp::Ordering> {
        (**self).partial_cmp(&**other)
    }
}

impl<T: impls::Object + Ord> Ord for WeakObject<T> {
    fn cmp(&self, other: &Self) -> core::cmp::Ordering {
        (**self).cmp(&**other)
    }
}

impl<T: impls::Object + Hash> Hash for WeakObject<T> {
    fn hash<H: core::hash::Hasher>(&self, state: &mut H) {
        (**self).hash(state);
    }
}

#[derive(Debug, Clone, PartialEq, Eq, PartialOrd, Ord, Hash)]
#[repr(transparent)]
pub struct WeakObjectPtr<T: impls::Object>(ComPtr<WeakObject<T>>);

impl<T: impls::Object> WeakObjectPtr<T>
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

impl<T: impls::Object> Deref for WeakObjectPtr<T> {
    type Target = T;

    fn deref(&self) -> &Self::Target {
        &*self.0.val
    }
}

impl<T: impls::Object> AsRef<T::Interface> for WeakObjectPtr<T> {
    fn as_ref(&self) -> &T::Interface {
        &self.0.base
    }
}

#[derive(Debug)]
#[repr(transparent)]
pub struct WeakObjectWeak<T: impls::Object>(ComWeak<WeakObject<T>>);

impl<T: impls::Object> Clone for WeakObjectWeak<T>
where
    T::Interface: RefCount,
{
    fn clone(&self) -> Self {
        Self(self.0.clone())
    }
}

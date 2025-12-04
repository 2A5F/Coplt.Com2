use alloc::{boxed::Box, sync::Arc};
use core::{
    borrow::Borrow,
    cell::UnsafeCell,
    hash::Hash,
    mem::ManuallyDrop,
    ptr::{NonNull, drop_in_place},
    sync::atomic::{AtomicU32, Ordering},
};

use crate::{
    com_ptr::*,
    impls::{self, ObjectBox, QueryInterface, WeakRefCount},
    *,
};

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

impl<T: impls::Object> MakeObject for T
where
    T::Interface: RefCount,
    Object<T>: impls::ObjectBox<Object = T> + impls::ObjectBoxNew,
{
    type ComOutput = ComPtr<T::Interface>;
    type ObjOutput = ObjectPtr<T>;

    fn make_object(self) -> Self::ObjOutput {
        unsafe {
            ObjectPtr(ComPtr::new(NonNull::new_unchecked(
                <Object<T> as impls::ObjectBoxNew>::make(self),
            )))
        }
    }

    fn make_com(self) -> Self::ComOutput {
        unsafe {
            ComPtr::new(NonNull::new_unchecked(
                <Object<T> as impls::ObjectBoxNew>::new(self),
            ))
        }
    }
}

impl<T: impls::Object> MakeObjectWeak for T
where
    T::Interface: RefCount + WeakRefCount,
    WeakObject<T>: impls::ObjectBox<Object = T> + impls::ObjectBoxNew,
{
    type ComOutput = ComPtr<T::Interface>;
    type ObjOutput = WeakObjectPtr<T>;

    fn make_com_weak(self) -> Self::ComOutput {
        unsafe {
            ComPtr::new(NonNull::new_unchecked(
                <WeakObject<T> as impls::ObjectBoxNew>::new(self),
            ))
        }
    }

    fn make_object_weak(self) -> Self::ObjOutput {
        unsafe {
            WeakObjectPtr(ComPtr::new(NonNull::new_unchecked(
                <WeakObject<T> as impls::ObjectBoxNew>::make(self),
            )))
        }
    }
}

#[repr(C)]
#[derive(Debug)]
pub struct Object<T: impls::Object> {
    base: T::Interface,
    strong: AtomicU32,
    val: ManuallyDrop<T>,
}

impl<T: impls::Object> Object<T> {
    unsafe fn Drop(this: *mut Self) {
        unsafe {
            ManuallyDrop::drop(&mut (*this).val);
            drop(Box::from_raw(this));
        }
    }
}

impl<T: impls::Object> Object<T>
where
    T::Interface: details::Vtbl<Self>,
{
    pub fn new(val: T) -> *mut T::Interface {
        Self::make(val) as _
    }

    pub fn make(val: T) -> *mut Self {
        let b = Box::leak(Box::new(Self {
            base: T::Interface::new(<T::Interface as details::Vtbl<Self>>::vtbl()),
            strong: AtomicU32::new(1),
            val: ManuallyDrop::new(val),
        }));
        b as *mut _
    }
}

impl<T: impls::Object> impls::ObjectBoxNew for Object<T>
where
    T::Interface: details::Vtbl<Self>,
{
    fn new(val: T) -> *mut T::Interface {
        Self::new(val)
    }

    fn make(val: Self::Object) -> *mut Self {
        Self::make(val)
    }
}

impl<T: impls::Object> impls::ObjectBox for Object<T> {
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
                Self::Drop(this);
            }
            r
        }
    }
}

impl<T: impls::Object> impls::RefCount for Object<T> {
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

#[derive(Debug, Clone, PartialEq, Eq, PartialOrd, Ord, Hash)]
#[repr(transparent)]
pub struct ObjectPtr<T: impls::Object>(ComPtr<Object<T>>);

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
}

impl<T: impls::Object> Deref for ObjectPtr<T> {
    type Target = T;

    fn deref(&self) -> &Self::Target {
        &*self.0.val
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
            drop(Box::from_raw(this));
        }
    }
}

impl<T: impls::Object> WeakObject<T>
where
    T::Interface: details::Vtbl<Self>,
{
    pub fn new(val: T) -> *mut T::Interface {
        Self::make(val) as _
    }

    pub fn make(val: T) -> *mut Self {
        let b = Box::leak(Box::new(Self {
            base: T::Interface::new(<T::Interface as details::Vtbl<Self>>::vtbl()),
            strong: AtomicU32::new(1),
            weak: AtomicU32::new(1),
            val: ManuallyDrop::new(val),
        }));
        b as *mut _
    }
}

impl<T: impls::Object> impls::ObjectBoxNew for WeakObject<T>
where
    T::Interface: details::Vtbl<Self>,
{
    fn new(val: T) -> *mut T::Interface {
        Self::new(val)
    }

    fn make(val: Self::Object) -> *mut Self {
        Self::make(val)
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
    T::Interface: details::QuIn<T, Self>,
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

    fn TryDowngrade(this: *const Self) -> bool {
        #[inline]
        fn checked_increment(n: u32) -> Option<u32> {
            if n == 0 {
                return None;
            }
            Some(n + 1)
        }
        unsafe {
            (*this)
                .weak
                .fetch_update(Ordering::Acquire, Ordering::Relaxed, checked_increment)
                .is_ok()
        }
    }
    // fn AddRefWeak(&self) -> u32 {
    //     self.weak.fetch_add(1, Ordering::Relaxed)
    // }

    // fn ReleaseWeak(&self) -> u32 {
    //     unsafe { Self::ReleaseWeak_(self as *const _ as _) }
    // }

    // fn TryUpgrade(&self) -> bool {
    //     #[inline]
    //     fn checked_increment(n: u32) -> Option<u32> {
    //         if n == 0 {
    //             return None;
    //         }
    //         Some(n + 1)
    //     }

    //     self.strong
    //         .fetch_update(Ordering::Acquire, Ordering::Relaxed, checked_increment)
    //         .is_ok()
    // }

    // fn TryDowngrade(&self) -> bool {
    //     #[inline]
    //     fn checked_increment(n: u32) -> Option<u32> {
    //         if n == 0 {
    //             return None;
    //         }
    //         Some(n + 1)
    //     }

    //     self.weak
    //         .fetch_update(Ordering::Acquire, Ordering::Relaxed, checked_increment)
    //         .is_ok()
    // }
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

#[derive(Debug, Clone)]
#[repr(transparent)]
pub struct WeakObjectWeak<T: impls::Object>(ComWeak<WeakObject<T>>);

use alloc::{boxed::Box, sync::Arc};
use core::{
    cell::UnsafeCell,
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
    type Output;

    fn make_object(self) -> Self::Output;
}

pub trait MakeObjectWeak {
    type Output;

    fn make_object_weak(self) -> Self::Output;
}

impl<T: impls::Object> MakeObject for T
where
    T::Interface: RefCount,
    Object<T>: impls::ObjectBox<Object = T> + impls::ObjectBoxNew,
{
    type Output = ComPtr<T::Interface>;

    fn make_object(self) -> Self::Output {
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
    type Output = ComPtr<T::Interface>;

    fn make_object_weak(self) -> Self::Output {
        unsafe {
            ComPtr::new(NonNull::new_unchecked(
                <WeakObject<T> as impls::ObjectBoxNew>::new(self),
            ))
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
        let b = Box::leak(Box::new(Self {
            base: T::Interface::new(&<T::Interface as details::Vtbl<Self>>::VTBL),
            strong: AtomicU32::new(1),
            val: ManuallyDrop::new(val),
        }));
        b as *mut _ as _
    }
}

impl<T: impls::Object> impls::ObjectBoxNew for Object<T>
where
    T::Interface: details::Vtbl<Self>,
{
    fn new(val: T) -> *mut T::Interface {
        Self::new(val)
    }
}

impl<T: impls::Object> impls::ObjectBox for Object<T> {
    type Object = T;

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
        let b = Box::leak(Box::new(Self {
            base: T::Interface::new(&<T::Interface as details::Vtbl<Self>>::VTBL),
            strong: AtomicU32::new(1),
            weak: AtomicU32::new(1),
            val: ManuallyDrop::new(val),
        }));
        b as *mut _ as _
    }
}

impl<T: impls::Object> impls::ObjectBoxNew for WeakObject<T>
where
    T::Interface: details::Vtbl<Self>,
{
    fn new(val: T) -> *mut T::Interface {
        Self::new(val)
    }
}

impl<T: impls::Object> impls::ObjectBox for WeakObject<T> {
    type Object = T;

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

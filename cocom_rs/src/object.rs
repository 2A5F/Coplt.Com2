use alloc::boxed::Box;
use core::{
    cell::UnsafeCell,
    mem::ManuallyDrop,
    ptr::NonNull,
    sync::atomic::{AtomicU32, Ordering},
};

use crate::{
    com_ptr::*,
    impls::{self},
    *,
};

#[repr(C)]
#[derive(Debug)]
pub struct ComObject<T> {
    value: T,
    strong: AtomicU32,
}

impl<T: impls::QueryInterface + impls::IUnknown> ComObject<T> {
    pub fn new(value: T) -> ComPtr<Self> {
        unsafe { ComPtr::new(Self::alloc(value)) }
    }
}

impl<T> ComObject<T> {
    pub unsafe fn alloc(value: T) -> NonNull<Self> {
        unsafe {
            NonNull::new_unchecked(Box::leak(Box::new(Self {
                value: value,
                strong: AtomicU32::new(1),
            })))
        }
    }

    pub unsafe fn AddRef(&self) -> u32 {
        self.strong.fetch_add(1, Ordering::Relaxed)
    }

    pub unsafe fn Release(&self) -> u32 {
        let r = self.strong.fetch_sub(1, Ordering::Release);
        if r != 1 {
            return r;
        }
        unsafe { self.do_release() };
        return r;
    }

    unsafe fn do_release(&self) {
        let ptr = self as *const _ as *mut Self;
        drop(unsafe { Box::from_raw(ptr) });
    }
}

impl<T: impls::QueryInterface + impls::IUnknown> impls::QueryInterface for ComObject<T> {
    fn QueryInterface(&self, guid: &Guid, out: &mut *mut ()) -> HResult {
        self.value.QueryInterface(guid, out)
    }
}

impl<T: impls::QueryInterface + impls::IUnknown> impls::RefCount for ComObject<T> {
    fn AddRef(&self) -> u32 {
        unsafe { self.AddRef() }
    }

    fn Release(&self) -> u32 {
        unsafe { self.Release() }
    }
}

impl<T> Deref for ComObject<T> {
    type Target = T;

    fn deref(&self) -> &Self::Target {
        &self.value
    }
}

impl<T> DerefMut for ComObject<T> {
    fn deref_mut(&mut self) -> &mut Self::Target {
        &mut self.value
    }
}

#[repr(C)]
#[derive(Debug)]
pub struct WeakObject<T> {
    value: UnsafeCell<ManuallyDrop<T>>,
    strong: AtomicU32,
    weak: AtomicU32,
}

impl<T: impls::QueryInterface + impls::IWeak> WeakObject<T> {
    pub fn new(value: T) -> ComWeak<Self> {
        unsafe { ComWeak::new(Self::alloc(value)) }
    }
}

impl<T> WeakObject<T> {
    pub unsafe fn alloc(value: T) -> NonNull<Self> {
        unsafe {
            NonNull::new_unchecked(Box::leak(Box::new(Self {
                value: UnsafeCell::new(ManuallyDrop::new(value)),
                strong: AtomicU32::new(1),
                weak: AtomicU32::new(1),
            })))
        }
    }

    pub unsafe fn AddRef(&self) -> u32 {
        self.strong.fetch_add(1, Ordering::Relaxed)
    }

    pub unsafe fn Release(&self) -> u32 {
        let r = self.strong.fetch_sub(1, Ordering::Release);
        if r != 1 {
            return r;
        }
        unsafe { self.drop_slow() };
        return r;
    }

    pub unsafe fn AddRefWeak(&self) -> u32 {
        self.weak.fetch_add(1, Ordering::Relaxed)
    }

    pub unsafe fn ReleaseWeak(&self) -> u32 {
        let r = self.weak.fetch_sub(1, Ordering::Release);
        if r != 1 {
            return r;
        }
        unsafe { self.do_delete() };
        return r;
    }

    pub unsafe fn TryUpgrade(&self) -> bool {
        let cur = self.weak.load(Ordering::Relaxed);
        loop {
            if cur == 0 {
                return false;
            }
            if self
                .weak
                .compare_exchange_weak(cur, cur + 1, Ordering::Acquire, Ordering::Relaxed)
                .is_ok()
            {
                return true;
            }
        }
    }

    pub unsafe fn TryDowngrade(&self) -> bool {
        let cur = self.strong.load(Ordering::Relaxed);
        loop {
            if cur == 0 {
                return false;
            }
            if self
                .strong
                .compare_exchange_weak(cur, cur + 1, Ordering::Acquire, Ordering::Relaxed)
                .is_ok()
            {
                return true;
            }
        }
    }

    unsafe fn drop_slow(&self) {
        unsafe { ManuallyDrop::drop(&mut *self.value.get()) };
        unsafe { self.ReleaseWeak() };
    }

    unsafe fn do_delete(&self) {
        let ptr = self as *const _ as *mut Self;
        drop(unsafe { Box::from_raw(ptr) });
    }
}

impl<T: impls::QueryInterface + impls::IWeak> impls::QueryInterface for WeakObject<T> {
    fn QueryInterface(&self, guid: &Guid, out: &mut *mut ()) -> HResult {
        (**self).QueryInterface(guid, out)
    }
}

impl<T: impls::QueryInterface + impls::IWeak> impls::RefCount for WeakObject<T> {
    fn AddRef(&self) -> u32 {
        unsafe { self.AddRef() }
    }

    fn Release(&self) -> u32 {
        unsafe { self.Release() }
    }
}

impl<T: impls::QueryInterface + impls::IWeak> impls::WeakRefCount for WeakObject<T> {
    fn AddRefWeak(&self) -> u32 {
        unsafe { self.AddRefWeak() }
    }

    fn ReleaseWeak(&self) -> u32 {
        unsafe { self.ReleaseWeak() }
    }

    fn TryUpgrade(&self) -> bool {
        unsafe { self.TryUpgrade() }
    }

    fn TryDowngrade(&self) -> bool {
        unsafe { self.TryDowngrade() }
    }
}

impl<T> Deref for WeakObject<T> {
    type Target = T;

    fn deref(&self) -> &Self::Target {
        unsafe { &*self.value.get() }
    }
}

impl<T> DerefMut for WeakObject<T> {
    fn deref_mut(&mut self) -> &mut Self::Target {
        self.value.get_mut()
    }
}

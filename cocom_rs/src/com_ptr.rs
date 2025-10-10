use crate::{IUnknown, IWeak};
use core::ops::Deref;
use std::ops::DerefMut;

#[derive(Debug, PartialEq, Eq, PartialOrd, Ord, Hash)]
pub struct ComPtr<T: AsRef<IUnknown>> {
    ptr: *mut T,
    _p: core::marker::PhantomData<T>,
}

impl<T: AsRef<IUnknown>> ComPtr<T> {
    pub unsafe fn create(ptr: *mut T) -> Self {
        Self {
            ptr,
            _p: core::marker::PhantomData,
        }
    }

    pub fn ptr(&self) -> *mut T {
        self.ptr
    }
}

impl<T: AsRef<IUnknown>> Default for ComPtr<T> {
    fn default() -> Self {
        Self {
            ptr: core::ptr::null_mut(),
            _p: core::marker::PhantomData,
        }
    }
}

impl<T: AsRef<IUnknown>> Drop for ComPtr<T> {
    fn drop(&mut self) {
        if !self.ptr.is_null() {
            unsafe { (*self.ptr).as_ref().Release() };
        }
    }
}

impl<T: AsRef<IUnknown>> Clone for ComPtr<T> {
    fn clone(&self) -> Self {
        unsafe { (*self.ptr).as_ref().AddRef() };
        Self {
            ptr: self.ptr.clone(),
            _p: self._p.clone(),
        }
    }
}

impl<T: AsRef<IUnknown>> Deref for ComPtr<T> {
    type Target = T;

    fn deref(&self) -> &Self::Target {
        unsafe { &*self.ptr }
    }
}

impl<T: AsRef<IUnknown>> DerefMut for ComPtr<T> {
    fn deref_mut(&mut self) -> &mut Self::Target {
        unsafe { &mut *self.ptr }
    }
}

impl<T: AsRef<IUnknown>> ComPtr<T> {
    pub fn is_null(&self) -> bool {
        self.ptr.is_null()
    }

    pub fn put(&mut self) -> &mut *mut T {
        &mut self.ptr
    }

    pub fn leak(mut self) -> *mut T {
        core::mem::replace(&mut self.ptr, core::ptr::null_mut())
    }
}

impl<T: AsRef<IWeak> + AsRef<IUnknown>> ComPtr<T> {
    pub fn downgrade(&self) -> Option<ComWeak<T>> {
        if self.ptr.is_null() {
            return None;
        }
        unsafe {
            let r: &IWeak = (*self.ptr).as_ref();
            if r.TryDowngrade() {
                Some(ComWeak::create(self.ptr))
            } else {
                None
            }
        }
    }
}

#[derive(Debug, PartialEq, Eq, PartialOrd, Ord, Hash)]
pub struct ComWeak<T: AsRef<IWeak>> {
    ptr: *mut T,
    _p: core::marker::PhantomData<T>,
}

impl<T: AsRef<IWeak>> ComWeak<T> {
    pub unsafe fn create(ptr: *mut T) -> Self {
        Self {
            ptr,
            _p: core::marker::PhantomData,
        }
    }

    pub fn ptr(&self) -> *mut T {
        self.ptr
    }
}

impl<T: AsRef<IWeak>> Default for ComWeak<T> {
    fn default() -> Self {
        Self {
            ptr: core::ptr::null_mut(),
            _p: core::marker::PhantomData,
        }
    }
}

impl<T: AsRef<IWeak>> Drop for ComWeak<T> {
    fn drop(&mut self) {
        if !self.ptr.is_null() {
            unsafe { (*self.ptr).as_ref().ReleaseWeak() };
        }
    }
}

impl<T: AsRef<IWeak>> Clone for ComWeak<T> {
    fn clone(&self) -> Self {
        unsafe { (*self.ptr).as_ref().AddRefWeak() };
        Self {
            ptr: self.ptr.clone(),
            _p: self._p.clone(),
        }
    }
}

impl<T: AsRef<IWeak>> Deref for ComWeak<T> {
    type Target = T;

    fn deref(&self) -> &Self::Target {
        unsafe { &*self.ptr }
    }
}

impl<T: AsRef<IWeak>> DerefMut for ComWeak<T> {
    fn deref_mut(&mut self) -> &mut Self::Target {
        unsafe { &mut *self.ptr }
    }
}

impl<T: AsRef<IWeak>> ComWeak<T> {
    pub fn is_null(&self) -> bool {
        self.ptr.is_null()
    }

    pub fn put(&mut self) -> &mut *mut T {
        &mut self.ptr
    }

    pub fn leak(mut self) -> *mut T {
        core::mem::replace(&mut self.ptr, core::ptr::null_mut())
    }
}

impl<T: AsRef<IWeak> + AsRef<IUnknown>> ComWeak<T> {
    pub fn upgrade(&self) -> Option<ComPtr<T>> {
        if self.ptr.is_null() {
            return None;
        }
        unsafe {
            let r: &IWeak = (*self.ptr).as_ref();
            if r.TryUpgrade() {
                Some(ComPtr::create(self.ptr))
            } else {
                None
            }
        }
    }
}

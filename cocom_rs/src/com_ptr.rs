use crate::{IUnknown, IWeak, impls};
use core::{
    fmt::{Debug, Display},
    mem::ManuallyDrop,
    ops::{Deref, DerefMut},
    ptr::NonNull,
};

#[derive(PartialEq, Eq, PartialOrd, Ord, Hash)]
pub struct ComPtr<T: impls::RefCount> {
    ptr: NonNull<T>,
    _p: core::marker::PhantomData<T>,
}

impl<T: impls::RefCount> ComPtr<T> {
    pub unsafe fn new(ptr: NonNull<T>) -> Self {
        Self {
            ptr,
            _p: core::marker::PhantomData,
        }
    }

    pub unsafe fn create(ptr: *mut T) -> Option<Self> {
        Some(Self {
            ptr: NonNull::new(ptr)?,
            _p: core::marker::PhantomData,
        })
    }

    pub fn ptr(&self) -> NonNull<T> {
        self.ptr
    }
}

impl<T: impls::RefCount> Drop for ComPtr<T> {
    fn drop(&mut self) {
        unsafe { self.ptr.as_ref().Release() };
    }
}

impl<T: impls::RefCount> Clone for ComPtr<T> {
    fn clone(&self) -> Self {
        unsafe { self.ptr.as_ref().AddRef() };
        Self {
            ptr: self.ptr,
            _p: self._p,
        }
    }
}

impl<T: impls::RefCount> Deref for ComPtr<T> {
    type Target = T;

    fn deref(&self) -> &Self::Target {
        unsafe { self.ptr.as_ref() }
    }
}

impl<T: impls::RefCount> DerefMut for ComPtr<T> {
    fn deref_mut(&mut self) -> &mut Self::Target {
        unsafe { self.ptr.as_mut() }
    }
}

impl<T: impls::RefCount> ComPtr<T> {
    pub fn leak(self) -> *mut T {
        let this = ManuallyDrop::new(self);
        this.ptr.as_ptr()
    }
}

impl<T: impls::WeakRefCount> ComPtr<T> {
    pub fn downgrade(&self) -> Option<ComWeak<T>> {
        unsafe {
            if self.ptr.as_ref().TryDowngrade() {
                Some(ComWeak::new(self.ptr))
            } else {
                None
            }
        }
    }
}

impl<T: impls::RefCount + Debug> Debug for ComPtr<T> {
    fn fmt(&self, f: &mut core::fmt::Formatter<'_>) -> core::fmt::Result {
        self.deref().fmt(f)
    }
}

impl<T: impls::RefCount + Display> Display for ComPtr<T> {
    fn fmt(&self, f: &mut core::fmt::Formatter<'_>) -> core::fmt::Result {
        self.deref().fmt(f)
    }
}

#[derive(PartialEq, Eq, PartialOrd, Ord, Hash)]
pub struct ComWeak<T: impls::WeakRefCount> {
    ptr: NonNull<T>,
    _p: core::marker::PhantomData<T>,
}

impl<T: impls::WeakRefCount> ComWeak<T> {
    pub unsafe fn new(ptr: NonNull<T>) -> Self {
        Self {
            ptr,
            _p: core::marker::PhantomData,
        }
    }

    pub unsafe fn create(ptr: *mut T) -> Option<Self> {
        Some(Self {
            ptr: NonNull::new(ptr)?,
            _p: core::marker::PhantomData,
        })
    }

    pub fn ptr(&self) -> NonNull<T> {
        self.ptr
    }
}

impl<T: impls::WeakRefCount> Drop for ComWeak<T> {
    fn drop(&mut self) {
        unsafe { self.ptr.as_ref().ReleaseWeak() };
    }
}

impl<T: impls::WeakRefCount> Clone for ComWeak<T> {
    fn clone(&self) -> Self {
        unsafe { self.ptr.as_ref().AddRefWeak() };
        Self {
            ptr: self.ptr,
            _p: self._p,
        }
    }
}

impl<T: impls::WeakRefCount> Deref for ComWeak<T> {
    type Target = T;

    fn deref(&self) -> &Self::Target {
        unsafe { self.ptr.as_ref() }
    }
}

impl<T: impls::WeakRefCount> DerefMut for ComWeak<T> {
    fn deref_mut(&mut self) -> &mut Self::Target {
        unsafe { self.ptr.as_mut() }
    }
}

impl<T: impls::WeakRefCount> ComWeak<T> {
    pub fn leak(self) -> *mut T {
        let this = ManuallyDrop::new(self);
        this.ptr.as_ptr()
    }
}

impl<T: impls::WeakRefCount> ComWeak<T> {
    pub fn upgrade(&self) -> Option<ComPtr<T>> {
        unsafe {
            if self.ptr.as_ref().TryUpgrade() {
                Some(ComPtr::new(self.ptr))
            } else {
                None
            }
        }
    }
}

impl<T: impls::WeakRefCount + Debug> Debug for ComWeak<T> {
    fn fmt(&self, f: &mut core::fmt::Formatter<'_>) -> core::fmt::Result {
        self.deref().fmt(f)
    }
}

impl<T: impls::WeakRefCount + Display> Display for ComWeak<T> {
    fn fmt(&self, f: &mut core::fmt::Formatter<'_>) -> core::fmt::Result {
        self.deref().fmt(f)
    }
}

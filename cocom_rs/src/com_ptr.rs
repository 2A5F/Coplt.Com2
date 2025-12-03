use crate::{IUnknown, IWeak, impls};
use core::{
    fmt::{Debug, Display},
    mem::ManuallyDrop,
    ops::Deref,
    ptr::NonNull,
};

pub trait Upcast<T, U> {
    type Output;

    fn upcast(self) -> Self::Output;
}

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
        unsafe { T::Release(self.ptr.as_ptr()) };
    }
}

impl<T: impls::RefCount> Clone for ComPtr<T> {
    fn clone(&self) -> Self {
        unsafe { T::AddRef(self.ptr.as_ptr()) };
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

impl<T: impls::RefCount> ComPtr<T> {
    pub fn leak(self) -> *mut T {
        let this = ManuallyDrop::new(self);
        this.ptr.as_ptr()
    }
}

impl<T: impls::WeakRefCount> ComPtr<T> {
    pub fn downgrade(&self) -> Option<ComWeak<T>> {
        unsafe {
            if T::TryDowngrade(self.ptr.as_ptr()) {
                Some(ComWeak::new(self.ptr))
            } else {
                None
            }
        }
    }
}

impl<T: impls::RefCount + Debug> Debug for ComPtr<T> {
    fn fmt(&self, f: &mut core::fmt::Formatter<'_>) -> core::fmt::Result {
        unsafe { self.ptr.as_ref() }.fmt(f)
    }
}

impl<T: impls::RefCount + Display> Display for ComPtr<T> {
    fn fmt(&self, f: &mut core::fmt::Formatter<'_>) -> core::fmt::Result {
        unsafe { self.ptr.as_ref() }.fmt(f)
    }
}

impl<U: impls::RefCount, T: impls::RefCount + impls::Inherit<U>> Upcast<T, U> for ComPtr<T> {
    type Output = ComPtr<U>;

    fn upcast(self) -> Self::Output {
        let value = self.leak();
        Self::Output {
            ptr: unsafe { NonNull::new_unchecked((*value).as_ref() as *const U as *mut U) },
            _p: core::marker::PhantomData,
        }
    }
}

impl<U: impls::RefCount, T: impls::RefCount + impls::Inherit<U>> Upcast<T, U> for &ComPtr<T> {
    type Output = ComPtr<U>;

    fn upcast(self) -> Self::Output {
        self.clone().upcast()
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
        unsafe { T::ReleaseWeak(self.ptr.as_ptr()) };
    }
}

impl<T: impls::WeakRefCount> Clone for ComWeak<T> {
    fn clone(&self) -> Self {
        unsafe { T::AddRefWeak(self.ptr.as_ptr()) };
        Self {
            ptr: self.ptr,
            _p: self._p,
        }
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
            if T::TryUpgrade(self.ptr.as_ptr()) {
                Some(ComPtr::new(self.ptr))
            } else {
                None
            }
        }
    }
}

impl<T: impls::WeakRefCount + Debug> Debug for ComWeak<T> {
    fn fmt(&self, f: &mut core::fmt::Formatter<'_>) -> core::fmt::Result {
        unsafe { self.ptr.as_ref() }.fmt(f)
    }
}

impl<T: impls::WeakRefCount + Display> Display for ComWeak<T> {
    fn fmt(&self, f: &mut core::fmt::Formatter<'_>) -> core::fmt::Result {
        unsafe { self.ptr.as_ref() }.fmt(f)
    }
}

impl<U: impls::WeakRefCount, T: impls::WeakRefCount + impls::Inherit<U>> Upcast<T, U>
    for ComWeak<T>
{
    type Output = ComWeak<U>;

    fn upcast(self) -> Self::Output {
        let value = self.leak();
        Self::Output {
            ptr: unsafe { NonNull::new_unchecked((*value).as_ref() as *const U as *mut U) },
            _p: core::marker::PhantomData,
        }
    }
}

impl<U: impls::WeakRefCount, T: impls::WeakRefCount + impls::Inherit<U>> Upcast<T, U>
    for &ComWeak<T>
{
    type Output = ComWeak<U>;

    fn upcast(self) -> Self::Output {
        self.clone().upcast()
    }
}

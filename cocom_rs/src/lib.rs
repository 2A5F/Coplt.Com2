#![no_std]
#![allow(non_snake_case)]
#![allow(non_camel_case_types)]
#![allow(unused)]

extern crate alloc;

use alloc::ffi;
pub use cocom_proc::*;

use core::fmt::Debug;
use core::ops::{Deref, DerefMut};

pub mod com_ptr;
pub mod guid;
pub mod hresult;
pub mod object;

pub use com_ptr::*;
pub use guid::*;
pub use hresult::*;
pub use object::{MakeObject, MakeObjectWeak};

pub trait Interface: Debug {
    const GUID: Guid;
    type VitualTable;
    type Parent: Interface;

    fn new(v_ptr: *const Self::VitualTable) -> Self;
}

use impls::RefCount;
pub mod details {
    use crate::{
        com_ptr::ComWeak,
        impls::{Object, ObjectBox, ObjectBoxWeak, QueryInterface, RefCount, WeakRefCount},
    };

    use super::*;

    pub trait Vtbl<O>: Interface {
        const VTBL: Self::VitualTable;
    }

    struct VT<T, V, O>(core::marker::PhantomData<(T, V, O)>);

    pub trait QuIn<T, O>: Interface {
        unsafe fn QueryInterface(
            this: *mut T,
            guid: *const Guid,
            out: *mut *mut core::ffi::c_void,
        ) -> HResult;
    }

    #[repr(C)]
    #[derive(Debug)]
    pub struct VitualTable_IUnknown {
        pub f_QueryInterface: unsafe extern "C" fn(
            this: *const IUnknown,
            guid: *const Guid,
            out: *mut *mut ::core::ffi::c_void,
        ) -> HResult,
        pub f_AddRef: unsafe extern "C" fn(this: *const IUnknown) -> u32,
        pub f_Release: unsafe extern "C" fn(this: *const IUnknown) -> u32,
    }

    impl<T: impls::IUnknown + impls::Object, O: ObjectBox<Object = T>> VT<T, IUnknown, O>
    where
        T::Interface: details::QuIn<T, O>,
    {
        pub const VTBL: VitualTable_IUnknown = VitualTable_IUnknown {
            f_QueryInterface: Self::f_QueryInterface,
            f_AddRef: Self::f_AddRef,
            f_Release: Self::f_Release,
        };

        unsafe extern "C" fn f_QueryInterface(
            this: *const IUnknown,
            guid: *const Guid,
            out: *mut *mut ::core::ffi::c_void,
        ) -> HResult {
            unsafe { T::Interface::QueryInterface(this as _, guid, out) }
        }
        unsafe extern "C" fn f_AddRef(this: *const IUnknown) -> u32 {
            unsafe { O::AddRef(this as _) }
        }
        unsafe extern "C" fn f_Release(this: *const IUnknown) -> u32 {
            unsafe { O::Release(this as _) }
        }
    }

    impl<T: impls::IUnknown + impls::Object, O: ObjectBox<Object = T>> Vtbl<O> for IUnknown
    where
        T::Interface: details::QuIn<T, O>,
    {
        const VTBL: <IUnknown as Interface>::VitualTable = VT::<T, IUnknown, O>::VTBL;
    }

    #[repr(C)]
    #[derive(Debug)]
    pub struct VitualTable_IWeak {
        pub b: <IUnknown as Interface>::VitualTable,

        pub f_AddRefWeak: unsafe extern "C" fn(this: *const IWeak) -> u32,
        pub f_ReleaseWeak: unsafe extern "C" fn(this: *const IWeak) -> u32,
        pub f_TryUpgrade: unsafe extern "C" fn(this: *const IWeak) -> bool,
        pub f_TryDowngrade: unsafe extern "C" fn(this: *const IWeak) -> bool,
    }

    impl<T: impls::IWeak + impls::Object, O: ObjectBox<Object = T> + ObjectBoxWeak> VT<T, IWeak, O>
    where
        T::Interface: details::QuIn<T, O>,
    {
        pub const VTBL: VitualTable_IWeak = VitualTable_IWeak {
            b: <IUnknown as Vtbl<O>>::VTBL,
            f_AddRefWeak: Self::f_AddRefWeak,
            f_ReleaseWeak: Self::f_ReleaseWeak,
            f_TryUpgrade: Self::f_TryUpgrade,
            f_TryDowngrade: Self::f_TryDowngrade,
        };

        unsafe extern "C" fn f_AddRefWeak(this: *const IWeak) -> u32 {
            unsafe { O::AddRefWeak(this as _) }
        }
        unsafe extern "C" fn f_ReleaseWeak(this: *const IWeak) -> u32 {
            unsafe { O::ReleaseWeak(this as _) }
        }
        unsafe extern "C" fn f_TryUpgrade(this: *const IWeak) -> bool {
            unsafe { O::TryUpgrade(this as _) }
        }
        unsafe extern "C" fn f_TryDowngrade(this: *const IWeak) -> bool {
            unsafe { O::TryDowngrade(this as _) }
        }
    }

    impl<T: impls::IWeak + impls::Object, O: ObjectBox<Object = T> + ObjectBoxWeak> Vtbl<O> for IWeak
    where
        T::Interface: details::QuIn<T, O>,
    {
        const VTBL: <IWeak as Interface>::VitualTable = VT::<T, IWeak, O>::VTBL;
    }

    impl<T: impls::IUnknown + impls::Object, O: ObjectBox<Object = T>> QuIn<T, O> for IUnknown {
        unsafe fn QueryInterface(
            this: *mut T,
            guid: *const Guid,
            out: *mut *mut core::ffi::c_void,
        ) -> HResult {
            unsafe {
                if *guid == IUnknown::GUID {
                    *out = this as _;
                    O::AddRef(this as _);
                    return HResultE::Ok.into();
                }
                HResultE::NoInterface.into()
            }
        }
    }

    impl<T: impls::IWeak + impls::Object, O: ObjectBox<Object = T>> QuIn<T, O> for IWeak {
        unsafe fn QueryInterface(
            this: *mut T,
            guid: *const Guid,
            out: *mut *mut core::ffi::c_void,
        ) -> HResult {
            unsafe {
                if *guid == IWeak::GUID {
                    *out = this as _;
                    O::AddRef(this as _);
                    return HResultE::Ok.into();
                }
                <IUnknown as QuIn<T, O>>::QueryInterface(this, guid, out)
            }
        }
    }
}

#[repr(C)]
pub struct IUnknown {
    v_ptr: *const (),
}

impl IUnknown {
    pub const fn new(v_ptr: *const details::VitualTable_IUnknown) -> Self {
        Self {
            v_ptr: v_ptr as *const (),
        }
    }
}

impl Debug for IUnknown {
    fn fmt(&self, f: &mut core::fmt::Formatter<'_>) -> core::fmt::Result {
        f.debug_struct("IUnknown").finish()
    }
}

impl Interface for IUnknown {
    const GUID: Guid = Guid::from_str("00000000-0000-0000-C000-000000000046").unwrap();
    type VitualTable = details::VitualTable_IUnknown;

    type Parent = IUnknown;

    fn new(v_ptr: *const Self::VitualTable) -> Self {
        Self::new(v_ptr)
    }
}

impl IUnknown {
    #[inline(always)]
    pub fn v_ptr(&self) -> *const details::VitualTable_IUnknown {
        self.v_ptr as *const _
    }

    pub fn QueryInterface(&self, guid: &Guid, out: &mut *mut ::core::ffi::c_void) -> HResult {
        unsafe { ((*self.v_ptr()).f_QueryInterface)(self as *const _, guid as *const _, out) }
    }

    pub fn AddRef(&self) -> u32 {
        unsafe { ((*self.v_ptr()).f_AddRef)(self as *const _) }
    }

    pub fn Release(&self) -> u32 {
        unsafe { ((*self.v_ptr()).f_Release)(self as *const _) }
    }

    pub fn QueryInterfaceTP<T: Interface>(&self, out: &mut *mut T) -> HResult {
        let mut ptr: *mut ::core::ffi::c_void = core::ptr::null_mut();
        let r = self.QueryInterface(&T::GUID, &mut ptr);
        if r.is_failure() {
            return r;
        }
        *out = unsafe { core::mem::transmute(ptr) };
        return r;
    }

    pub fn QueryInterfaceT<T: Interface + RefCount>(&self, out: &mut ComPtr<T>) -> HResult {
        let mut ptr: *mut ::core::ffi::c_void = core::ptr::null_mut();
        let r = self.QueryInterface(&T::GUID, &mut ptr);
        if r.is_failure() {
            return r;
        }
        *out = unsafe { ComPtr::create(core::mem::transmute(ptr)).unwrap() };
        return r;
    }

    pub fn TryCast<T: Interface + RefCount>(&self) -> Option<ComPtr<T>> {
        let mut ptr: *mut ::core::ffi::c_void = core::ptr::null_mut();
        let r = self.QueryInterface(&T::GUID, &mut ptr);
        if r.is_failure() {
            return None;
        }
        Some(unsafe { ComPtr::create(core::mem::transmute(ptr)).unwrap() })
    }
}

impl impls::QueryInterface for IUnknown {
    fn QueryInterface(&self, guid: &Guid, out: &mut *mut ::core::ffi::c_void) -> HResult {
        self.QueryInterface(guid, out)
    }
}

impl impls::RefCount for IUnknown {
    fn AddRef(&self) -> u32 {
        self.AddRef()
    }

    fn Release(&self) -> u32 {
        self.Release()
    }
}

#[repr(C)]
pub struct IWeak {
    base: IUnknown,
}

impl IWeak {
    pub const fn new(v_ptr: *const details::VitualTable_IWeak) -> Self {
        Self {
            base: IUnknown::new(v_ptr as *const details::VitualTable_IUnknown),
        }
    }
}

impl Debug for IWeak {
    fn fmt(&self, f: &mut core::fmt::Formatter<'_>) -> core::fmt::Result {
        f.debug_struct("IWeak").finish()
    }
}

impl Interface for IWeak {
    const GUID: Guid = Guid::from_str("9d01e165-12b5-4190-bb46-3d78413de9a5").unwrap();
    type VitualTable = details::VitualTable_IWeak;
    type Parent = IUnknown;

    fn new(v_ptr: *const Self::VitualTable) -> Self {
        Self::new(v_ptr)
    }
}

impl Deref for IWeak {
    type Target = IUnknown;

    fn deref(&self) -> &Self::Target {
        &self.base
    }
}

impl DerefMut for IWeak {
    fn deref_mut(&mut self) -> &mut Self::Target {
        &mut self.base
    }
}

impl IWeak {
    #[inline(always)]
    pub fn v_ptr(&self) -> *const details::VitualTable_IWeak {
        self.base.v_ptr() as *const _
    }

    pub fn AddRefWeak(&self) -> u32 {
        unsafe { ((*self.v_ptr()).f_AddRefWeak)(self as _) }
    }

    pub fn ReleaseWeak(&self) -> u32 {
        unsafe { ((*self.v_ptr()).f_ReleaseWeak)(self as *const _) }
    }

    pub fn TryUpgrade(&self) -> bool {
        unsafe { ((*self.v_ptr()).f_TryUpgrade)(self as *const _) }
    }

    pub fn TryDowngrade(&self) -> bool {
        unsafe { ((*self.v_ptr()).f_TryDowngrade)(self as *const _) }
    }
}

impl impls::QueryInterface for IWeak {
    fn QueryInterface(&self, guid: &Guid, out: &mut *mut ::core::ffi::c_void) -> HResult {
        (**self).QueryInterface(guid, out)
    }
}

impl impls::RefCount for IWeak {
    fn AddRef(&self) -> u32 {
        (**self).AddRef()
    }

    fn Release(&self) -> u32 {
        (**self).Release()
    }
}

impl impls::WeakRefCount for IWeak {
    fn AddRefWeak(&self) -> u32 {
        self.AddRefWeak()
    }

    fn ReleaseWeak(&self) -> u32 {
        self.ReleaseWeak()
    }

    fn TryUpgrade(&self) -> bool {
        self.TryUpgrade()
    }

    fn TryDowngrade(&self) -> bool {
        self.TryDowngrade()
    }
}

pub mod impls {
    use super::*;

    pub unsafe trait Inherit<T>: AsRef<T> + AsMut<T> {}

    pub trait Object {
        type Interface: Interface + Sized;
    }

    pub trait ObjectBoxNew: ObjectBox {
        fn new(val: Self::Object) -> *mut <Self::Object as Object>::Interface;
    }

    pub trait ObjectBox {
        type Object: Object;

        unsafe fn AddRef(this: *mut <Self::Object as Object>::Interface) -> u32;
        unsafe fn Release(this: *mut <Self::Object as Object>::Interface) -> u32;
    }

    pub trait ObjectBoxWeak: ObjectBox {
        unsafe fn AddRefWeak(this: *mut <Self::Object as Object>::Interface) -> u32;
        unsafe fn ReleaseWeak(this: *mut <Self::Object as Object>::Interface) -> u32;
        unsafe fn TryUpgrade(this: *mut <Self::Object as Object>::Interface) -> bool;
        unsafe fn TryDowngrade(this: *mut <Self::Object as Object>::Interface) -> bool;
    }

    pub trait ObjectRefCount {
        fn _strong(&self) -> &core::sync::atomic::AtomicU32;
    }

    pub trait ObjectRefCountWeak: ObjectRefCount {
        fn _weak(&self) -> &core::sync::atomic::AtomicU32;
    }

    pub trait QueryInterface {
        fn QueryInterface(&self, guid: &Guid, out: &mut *mut ::core::ffi::c_void) -> HResult;
    }

    pub trait RefCount {
        fn AddRef(&self) -> u32;
        fn Release(&self) -> u32;
    }

    pub trait WeakRefCount: RefCount {
        fn AddRefWeak(&self) -> u32;
        fn ReleaseWeak(&self) -> u32;
        fn TryUpgrade(&self) -> bool;
        fn TryDowngrade(&self) -> bool;
    }

    pub trait IUnknown {}

    pub trait IWeak: IUnknown {}
}

#[cfg(test)]
mod test {
    extern crate std;
    use crate::{
        com_ptr::{ComWeak, Upcast},
        impls::ObjectBox,
        object::Object,
        *,
    };

    #[object(IUnknown)]
    #[derive(Debug)]
    pub struct Foo {}

    #[test]
    fn test1() {
        let a = Foo {}.make_object();
        std::println!("{:?}", a);
    }
}

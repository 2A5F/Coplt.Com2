#![no_std]
#![allow(non_snake_case)]
#![allow(non_camel_case_types)]
#![allow(unused)]

extern crate alloc;

use alloc::ffi;
pub use cocom_proc::*;

use core::fmt::Debug;
use core::ops::Deref;

pub mod com_ptr;
pub mod guid;
pub mod hresult;
pub mod object;

pub use com_ptr::*;
pub use guid::*;
pub use hresult::*;
pub use object::{MakeObject, MakeObjectWeak};

/// field projection for mut ptr
#[macro_export]
macro_rules! pmp {
    { $s:expr; .$name:ident } => {
        {
            let ptr: *mut _ = &mut (*($s)).$name;
            ptr
        }
    };
}

/// field projection for const ptr
#[macro_export]
macro_rules! pcp {
    { $s:expr; .$name:ident } => {
        {
            let ptr: *const _ = &(*($s)).$name;
            ptr
        }
    };
}

pub trait Interface: Debug {
    const GUID: Guid;
    type VitualTable: 'static;
    type Parent: Interface + 'static;

    fn new(v_ptr: &'static Self::VitualTable) -> Self;
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

        fn vtbl() -> &'static Self::VitualTable;
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

        fn vtbl() -> &'static Self::VitualTable {
            &<Self as Vtbl<O>>::VTBL
        }
    }

    #[repr(C)]
    #[derive(Debug)]
    pub struct VitualTable_IWeak {
        pub b: <IUnknown as Interface>::VitualTable,

        pub f_AddRefWeak: unsafe extern "C" fn(this: *const IWeak) -> u32,
        pub f_ReleaseWeak: unsafe extern "C" fn(this: *const IWeak) -> u32,
        pub f_TryUpgrade: unsafe extern "C" fn(this: *const IWeak) -> bool,
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
    }

    impl<T: impls::IWeak + impls::Object, O: ObjectBox<Object = T> + ObjectBoxWeak> Vtbl<O> for IWeak
    where
        T::Interface: details::QuIn<T, O>,
    {
        const VTBL: <IWeak as Interface>::VitualTable = VT::<T, IWeak, O>::VTBL;

        fn vtbl() -> &'static Self::VitualTable {
            &<Self as Vtbl<O>>::VTBL
        }
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
    v_ptr: *const core::ffi::c_void,
}

impl IUnknown {
    pub const fn new(v_ptr: *const details::VitualTable_IUnknown) -> Self {
        Self { v_ptr: v_ptr as _ }
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

    fn new(v_ptr: &'static Self::VitualTable) -> Self {
        Self::new(v_ptr)
    }
}

impl IUnknown {
    pub fn v_ptr(&self) -> *const details::VitualTable_IUnknown {
        unsafe { self.v_ptr as *const _ }
    }

    pub fn QueryInterface(&self, guid: *const Guid, out: *mut *mut ::core::ffi::c_void) -> HResult {
        unsafe { ((*self.v_ptr()).f_QueryInterface)(self, guid, out) }
    }

    pub fn AddRef(&self) -> u32 {
        unsafe { ((*self.v_ptr()).f_AddRef)(self) }
    }

    pub fn Release(&self) -> u32 {
        unsafe { ((*self.v_ptr()).f_Release)(self) }
    }
}

impl impls::QueryInterface for IUnknown {
    fn QueryInterface(
        this: *const Self,
        guid: *const Guid,
        out: *mut *mut ::core::ffi::c_void,
    ) -> HResult {
        Self::QueryInterface(unsafe { &*this }, guid, out)
    }
}

impl impls::RefCount for IUnknown {
    fn AddRef(this: *const Self) -> u32 {
        Self::AddRef(unsafe { &*this })
    }

    fn Release(this: *const Self) -> u32 {
        Self::Release(unsafe { &*this })
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

    fn new(v_ptr: &'static Self::VitualTable) -> Self {
        Self::new(v_ptr)
    }
}

impl Deref for IWeak {
    type Target = IUnknown;

    fn deref(&self) -> &Self::Target {
        &self.base
    }
}

impl IWeak {
    pub fn v_ptr(&self) -> *const details::VitualTable_IWeak {
        self.base.v_ptr() as _
    }

    pub fn AddRefWeak(&self) -> u32 {
        unsafe { ((*self.v_ptr()).f_AddRefWeak)(self) }
    }

    pub fn ReleaseWeak(&self) -> u32 {
        unsafe { ((*self.v_ptr()).f_ReleaseWeak)(self) }
    }

    pub fn TryUpgrade(&self) -> bool {
        unsafe { ((*self.v_ptr()).f_TryUpgrade)(self) }
    }
}

impl impls::RefCount for IWeak {
    fn AddRef(this: *const Self) -> u32 {
        IUnknown::AddRef(unsafe { &*this })
    }

    fn Release(this: *const Self) -> u32 {
        IUnknown::Release(unsafe { &*this })
    }
}

impl impls::WeakRefCount for IWeak {
    fn AddRefWeak(this: *const Self) -> u32 {
        Self::AddRefWeak(unsafe { &*this })
    }

    fn ReleaseWeak(this: *const Self) -> u32 {
        Self::ReleaseWeak(unsafe { &*this })
    }

    fn TryUpgrade(this: *const Self) -> bool {
        Self::TryUpgrade(unsafe { &*this })
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
        fn make(val: Self::Object) -> *mut Self;
    }

    pub trait ObjectBox {
        type Object: Object;

        unsafe fn GetObject(this: *mut <Self::Object as Object>::Interface) -> *mut Self::Object;

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
        fn QueryInterface(
            this: *const Self,
            guid: *const Guid,
            out: *mut *mut ::core::ffi::c_void,
        ) -> HResult;
    }

    pub trait RefCount {
        fn AddRef(this: *const Self) -> u32;
        fn Release(this: *const Self) -> u32;
    }

    pub trait WeakRefCount: RefCount {
        fn AddRefWeak(this: *const Self) -> u32;
        fn ReleaseWeak(this: *const Self) -> u32;
        fn TryUpgrade(this: *const Self) -> bool;
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
        let a = Foo {}.make_com();
        a.AddRef();
        a.Release();
        std::println!("{:?}", a);
    }
}

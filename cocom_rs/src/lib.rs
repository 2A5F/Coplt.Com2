#![allow(non_snake_case)]
#![allow(non_camel_case_types)]

use core::fmt::Debug;
use std::ops::{Deref, DerefMut};

pub mod com_ptr;
pub mod guid;
pub mod hresult;
pub use com_ptr::*;
pub use guid::*;
pub use hresult::*;

pub trait Interface: Debug {
    const GUID: Guid;
    type VitualTable;
}

use details::*;
mod details {
    use super::*;

    #[repr(C)]
    #[derive(Debug)]
    pub struct VitualTable_IUnknown {
        pub f_QueryInterface: unsafe extern "C" fn(
            this: *const IUnknown,
            guid: *const Guid,
            out: *mut *mut (),
        ) -> u32,
        pub f_AddRef: unsafe extern "C" fn(this: *const IUnknown) -> u32,
        pub f_Release: unsafe extern "C" fn(this: *const IUnknown) -> u32,
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
}

#[repr(C)]
#[derive(Debug)]
pub struct IUnknown {
    v_ptr: *const (),
}

impl Interface for IUnknown {
    const GUID: Guid = Guid::from_str("00000000-0000-0000-C000-000000000046").unwrap();
    type VitualTable = VitualTable_IUnknown;
}

impl IUnknown {
    fn v_ptr(&self) -> &VitualTable_IUnknown {
        unsafe { core::mem::transmute(self.v_ptr) }
    }

    pub fn QueryInterface(&self, guid: &Guid, out: &mut *mut ()) -> HResult {
        unsafe {
            core::mem::transmute((self.v_ptr().f_QueryInterface)(
                self as *const _,
                guid as *const _,
                out as *mut *mut (),
            ))
        }
    }

    pub fn AddRef(&self) -> u32 {
        unsafe { (self.v_ptr().f_AddRef)(self as *const _) }
    }

    pub fn Release(&self) -> u32 {
        unsafe { (self.v_ptr().f_Release)(self as *const _) }
    }

    pub fn QueryInterfaceTP<T: Interface>(&self, out: &mut *mut T) -> HResult {
        let mut ptr: *mut () = core::ptr::null_mut();
        let r = self.QueryInterface(&T::GUID, &mut ptr);
        if r.is_failure() {
            return r;
        }
        *out = unsafe { core::mem::transmute(ptr) };
        return r;
    }
}

#[repr(C)]
#[derive(Debug)]
pub struct IWeak {
    base: IUnknown,
}
impl Interface for IWeak {
    const GUID: Guid = Guid::from_str("9d01e165-12b5-4190-bb46-3d78413de9a5").unwrap();
    type VitualTable = VitualTable_IWeak;
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
    fn v_ptr(&self) -> &VitualTable_IWeak {
        unsafe { core::mem::transmute(self.base.v_ptr()) }
    }

    pub fn AddRefWeak(&self) -> u32 {
        unsafe { (self.v_ptr().f_AddRefWeak)(self as *const _) }
    }

    pub fn ReleaseWeak(&self) -> u32 {
        unsafe { (self.v_ptr().f_ReleaseWeak)(self as *const _) }
    }

    pub fn TryUpgrade(&self) -> bool {
        unsafe { (self.v_ptr().f_TryUpgrade)(self as *const _) }
    }

    pub fn TryDowngrade(&self) -> bool {
        unsafe { (self.v_ptr().f_TryDowngrade)(self as *const _) }
    }
}

impl AsRef<IUnknown> for IWeak {
    fn as_ref(&self) -> &IUnknown {
        &*self
    }
}

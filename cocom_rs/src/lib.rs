#![no_std]
#![allow(non_snake_case)]
#![allow(non_camel_case_types)]
#![allow(unused)]

extern crate alloc;

use core::fmt::Debug;
use core::ops::{Deref, DerefMut};

pub mod com_ptr;
pub mod guid;
pub mod hresult;
pub mod object;

pub use guid::*;
pub use hresult::*;

pub trait Interface: Debug {
    const GUID: Guid;
    type VitualTable;
    type Parent: Interface;
}

use crate::com_ptr::ComPtr;
use impls::RefCount;
pub mod details {
    use crate::{
        com_ptr::ComWeak,
        impls::{QueryInterface, RefCount, WeakRefCount},
        object::{ComObject, WeakObject},
    };

    use super::*;

    pub struct VT<T, V>(core::marker::PhantomData<(T, V)>);

    pub struct QI<T, I>(core::marker::PhantomData<(T, I)>);

    #[repr(C)]
    #[derive(Debug)]
    pub struct VitualTable_IUnknown {
        pub f_QueryInterface: unsafe extern "C" fn(
            this: *const IUnknown,
            guid: *const Guid,
            out: *mut *mut (),
        ) -> HResult,
        pub f_AddRef: unsafe extern "C" fn(this: *const IUnknown) -> u32,
        pub f_Release: unsafe extern "C" fn(this: *const IUnknown) -> u32,
    }

    impl<T: impls::IUnknown + impls::ObjectQueryInterface> VT<T, IUnknown> {
        pub const VTBL: VitualTable_IUnknown = VitualTable_IUnknown {
            f_QueryInterface: Self::f_QueryInterface,
            f_AddRef: Self::f_AddRef,
            f_Release: Self::f_Release,
        };

        unsafe extern "C" fn f_QueryInterface(
            this: *const IUnknown,
            guid: *const Guid,
            out: *mut *mut (),
        ) -> HResult {
            unsafe {
                (*(this as *const <T as impls::ObjectQueryInterface>::Object))
                    .QueryInterface(&*guid, &mut *out)
            }
        }
        unsafe extern "C" fn f_AddRef(this: *const IUnknown) -> u32 {
            unsafe { (*(this as *const <T as impls::ObjectQueryInterface>::Object)).AddRef() }
        }
        unsafe extern "C" fn f_Release(this: *const IUnknown) -> u32 {
            unsafe { (*(this as *const <T as impls::ObjectQueryInterface>::Object)).Release() }
        }
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

    impl<T: impls::IWeak + impls::ObjectQueryInterface> VT<T, IWeak>
    where
        <T as impls::ObjectQueryInterface>::Object: impls::WeakRefCount,
    {
        pub const VTBL: VitualTable_IWeak = VitualTable_IWeak {
            b: VT::<T, IUnknown>::VTBL,
            f_AddRefWeak: Self::f_AddRefWeak,
            f_ReleaseWeak: Self::f_ReleaseWeak,
            f_TryUpgrade: Self::f_TryUpgrade,
            f_TryDowngrade: Self::f_TryDowngrade,
        };

        unsafe extern "C" fn f_AddRefWeak(this: *const IWeak) -> u32 {
            unsafe { (*(this as *const <T as impls::ObjectQueryInterface>::Object)).AddRefWeak() }
        }
        unsafe extern "C" fn f_ReleaseWeak(this: *const IWeak) -> u32 {
            unsafe { (*(this as *const <T as impls::ObjectQueryInterface>::Object)).ReleaseWeak() }
        }
        unsafe extern "C" fn f_TryUpgrade(this: *const IWeak) -> bool {
            unsafe { (*(this as *const <T as impls::ObjectQueryInterface>::Object)).TryUpgrade() }
        }
        unsafe extern "C" fn f_TryDowngrade(this: *const IWeak) -> bool {
            unsafe { (*(this as *const <T as impls::ObjectQueryInterface>::Object)).TryDowngrade() }
        }
    }

    impl<T: impls::ObjectQueryInterface + impls::IUnknown> QI<T, IUnknown> {
        pub fn QueryInterface(
            this: &<T as impls::ObjectQueryInterface>::Object,
            guid: &Guid,
            out: &mut *mut (),
        ) -> HResult {
            if *guid == IUnknown::GUID {
                *out = this as *const _ as *mut _;
                unsafe { this.AddRef() };
                return HResultE::Ok.into();
            }
            HResultE::NoInterface.into()
        }
    }

    impl<T: impls::ObjectQueryInterface + impls::IWeak> QI<T, IWeak> {
        pub fn QueryInterface(
            this: &<T as impls::ObjectQueryInterface>::Object,
            guid: &Guid,
            out: &mut *mut (),
        ) -> HResult {
            if *guid == IWeak::GUID {
                *out = this as *const _ as *mut _;
                unsafe { this.AddRef() };
                return HResultE::Ok.into();
            }
            QI::<T, IUnknown>::QueryInterface(this, guid, out)
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
}

impl IUnknown {
    fn v_ptr(&self) -> *const details::VitualTable_IUnknown {
        self.v_ptr as *const _
    }

    pub fn QueryInterface(&self, guid: &Guid, out: &mut *mut ()) -> HResult {
        unsafe {
            ((*self.v_ptr()).f_QueryInterface)(
                self as *const _,
                guid as *const _,
                out as *mut *mut (),
            )
        }
    }

    pub fn AddRef(&self) -> u32 {
        unsafe { ((*self.v_ptr()).f_AddRef)(self as *const _) }
    }

    pub fn Release(&self) -> u32 {
        unsafe { ((*self.v_ptr()).f_Release)(self as *const _) }
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

    pub fn QueryInterfaceT<T: Interface + RefCount>(&self, out: &mut ComPtr<T>) -> HResult {
        let mut ptr: *mut () = core::ptr::null_mut();
        let r = self.QueryInterface(&T::GUID, &mut ptr);
        if r.is_failure() {
            return r;
        }
        *out = unsafe { ComPtr::create(core::mem::transmute(ptr)).unwrap() };
        return r;
    }

    pub fn TryCast<T: Interface + RefCount>(&self) -> Option<ComPtr<T>> {
        let mut ptr: *mut () = core::ptr::null_mut();
        let r = self.QueryInterface(&T::GUID, &mut ptr);
        if r.is_failure() {
            return None;
        }
        Some(unsafe { ComPtr::create(core::mem::transmute(ptr)).unwrap() })
    }
}

impl impls::QueryInterface for IUnknown {
    fn QueryInterface(&self, guid: &Guid, out: &mut *mut ()) -> HResult {
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

impl impls::IUnknown for IUnknown {}

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
    fn v_ptr(&self) -> *const details::VitualTable_IWeak {
        self.base.v_ptr() as *const _
    }

    pub fn AddRefWeak(&self) -> u32 {
        unsafe { ((*self.v_ptr()).f_AddRefWeak)(self as *const _) }
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
    fn QueryInterface(&self, guid: &Guid, out: &mut *mut ()) -> HResult {
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

impl impls::IUnknown for IWeak {}

impl impls::IWeak for IWeak {}

pub mod impls {
    use super::*;

    pub unsafe trait Inherit<T>: AsRef<T> + AsMut<T> {}

    pub trait ObjectQueryInterface {
        type Object: QueryInterface + RefCount;

        fn QueryInterface(this: &Self::Object, guid: &Guid, out: &mut *mut ()) -> HResult;
    }

    pub trait QueryInterface {
        fn QueryInterface(&self, guid: &Guid, out: &mut *mut ()) -> HResult;
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

    #[macro_export]
    macro_rules! impl_object {
        {
            #[weak] $S:ty : $I:ty => $i:ident;
            $(#[ctor] impl {
                $($vis:vis fn $name:ident($ii:ident: $it:ty $(, $ai:ident: $at:ty)* $(,)?) -> Self $b:block)*
            })?
        } => {
            impl impls::ObjectQueryInterface for $S {
                type Object = crate::object::WeakObject<Self>;

                fn QueryInterface(this: &Self::Object, guid: &crate::Guid, out: &mut *mut ()) -> crate::HResult {
                    details::QI::<Self, $I>::QueryInterface(this, guid, out)
                }
            }

            impl_object! {
                ; $S : $I => $i;
            }

            $(
                impl $S {
                    $(
                        concat_idents::concat_idents!(fn_name = _ctor_, $name {
                            $vis fn $name($($ai: $at),*) -> crate::com_ptr::ComWeak<Self>
                            {
                                crate::object::WeakObject::new(
                                    Self::fn_name(
                                        $I::new(&details::VT::<Self, $I>::VTBL)
                                        $(, $ai)*
                                    )
                                ).upcast()
                            }

                            fn fn_name($ii: $it $(, $ai: $at)*) -> Self $b
                        });
                    )*
                }
            )?
        };
        {
            $S:ty : $I:ty => $i:ident;
            $(#[ctor] impl {
                $($vis:vis fn $name:ident($ii:ident: $it:ty $(, $ai:ident: $at:ty)* $(,)?) -> Self $b:block)*
            })?
        } => {
            impl impls::ObjectQueryInterface for $S {
                type Object = crate::object::ComObject<Self>;

                fn QueryInterface(this: &Self::Object, guid: &crate::Guid, out: &mut *mut ()) -> crate::HResult {
                    details::QI::<Self, $I>::QueryInterface(this, guid, out)
                }
            }

            impl_object! {
                ; $S : $I => $i;
            }

            $(
                impl $S {
                    $(
                        concat_idents::concat_idents!(fn_name = _ctor_, $name {
                            $vis fn $name($($ai: $at),*) -> crate::com_ptr::ComPtr<Self>
                            {
                                crate::object::ComObject::new(
                                    Self::fn_name(
                                        $I::new(&details::VT::<Self, $I>::VTBL)
                                        $(, $ai)*
                                    )
                                ).upcast()
                            }

                            fn fn_name($ii: $it $(, $ai: $at)*) -> Self $b
                        });
                    )*
                }
            )?
        };
        {
            ; $S:ty : $I:ty => $i:ident;
        } => {
            concat_idents::concat_idents!(macro_name = impl_, $I {
                macro_name! { $S }
            });

            impl Deref for $S {
                type Target = $I;

                fn deref(&self) -> &Self::Target {
                    &self.$i
                }
            }

            impl DerefMut for $S {
                fn deref_mut(&mut self) -> &mut Self::Target {
                    &mut self.$i
                }
            }
        };
    }

    #[macro_export]
    macro_rules! impl_IUnknown {
        { $S:ty } => {
            impl impls::QueryInterface for $S {
                fn QueryInterface(&self, guid: &crate::Guid, out: &mut *mut ()) -> crate::HResult {
                    (**self).QueryInterface(guid, out)
                }
            }

            impl impls::RefCount for $S {
                fn AddRef(&self) -> u32 {
                    (**self).AddRef()
                }

                fn Release(&self) -> u32 {
                    (**self).Release()
                }
            }

            impl impls::IUnknown for $S {}

            impl AsRef<crate::IUnknown> for $S {
                fn as_ref(&self) -> &crate::IUnknown {
                    self
                }
            }

            impl AsMut<crate::IUnknown> for $S {
                fn as_mut(&mut self) -> &mut crate::IUnknown {
                    self
                }
            }

            unsafe impl impls::Inherit<crate::IUnknown> for $S {}
        };
    }

    #[macro_export]
    macro_rules! impl_IWeak {
        { $S:ty } => {
            crate:: impl_IUnknown! { $S }

            impl impls::WeakRefCount for $S {
                fn AddRefWeak(&self) -> u32 {
                    (**self).AddRefWeak()
                }

                fn ReleaseWeak(&self) -> u32 {
                    (**self).ReleaseWeak()
                }

                fn TryUpgrade(&self) -> bool {
                    (**self).TryUpgrade()
                }

                fn TryDowngrade(&self) -> bool {
                    (**self).TryDowngrade()
                }
            }

            impl impls::IWeak for $S {}

            impl AsRef<crate::IWeak> for $S {
                fn as_ref(&self) -> &crate::IWeak {
                    self
                }
            }

            impl AsMut<crate::IWeak> for $S {
                fn as_mut(&mut self) -> &mut crate::IWeak {
                    self
                }
            }

            unsafe impl impls::Inherit<crate::IWeak> for $S {}
        };
    }
}

#[cfg(test)]
mod test {
    extern crate std;
    use crate::{
        com_ptr::{ComWeak, Upcast},
        object::{ComObject, WeakObject},
        *,
    };

    #[repr(C)]
    #[derive(Debug)]
    pub struct Foo {
        pub i: IWeak,
    }

    impl_object! {
        #[weak] Foo : IWeak => i;
        #[ctor] impl {
            pub fn new(i: IWeak) -> Self {
                Self { i }
            }
        }
    }

    #[test]
    fn test1() {
        let a: ComWeak<Foo> = Foo::new();
        std::println!("{:?}", a);
    }
}

#[repr(u32)]
#[non_exhaustive]
#[derive(Debug, Clone, Copy, PartialEq, Eq, PartialOrd, Ord, Hash)]
pub enum HResultE {
    Ok = 0,
    NotImpl = 0x80004001,
    NoInterface = 0x80004002,
    Pointer = 0x80004003,
    Abort = 0x80004004,
    Fail = 0x80004005,
    Unexpected = 0x8000FFFF,
    AccessDenied = 0x80070005,
    Handle = 0x80070006,
    OutOfMemory = 0x8007000E,
    InvalidArg = 0x80070057,
}

#[repr(transparent)]
#[derive(Debug, Clone, Copy, PartialEq, Eq, PartialOrd, Ord, Hash)]
pub struct HResult {
    pub value: i32,
}

impl HResult {
    pub const fn ok() -> Self {
        Self::new(0)
    }
    pub const fn not_impl() -> Self {
        Self::new(0x80004001u32 as i32)
    }
    pub const fn no_interface() -> Self {
        Self::new(0x80004002u32 as i32)
    }
    pub const fn pointer() -> Self {
        Self::new(0x80004003u32 as i32)
    }
    pub const fn abort() -> Self {
        Self::new(0x80004004u32 as i32)
    }
    pub const fn fail() -> Self {
        Self::new(0x80004005u32 as i32)
    }
    pub const fn unexpected() -> Self {
        Self::new(0x8000FFFFu32 as i32)
    }
    pub const fn access_denied() -> Self {
        Self::new(0x80070005u32 as i32)
    }
    pub const fn handle() -> Self {
        Self::new(0x80070006u32 as i32)
    }
    pub const fn out_of_memory() -> Self {
        Self::new(0x8007000Eu32 as i32)
    }
    pub const fn invalid_arg() -> Self {
        Self::new(0x80070057u32 as i32)
    }
}

impl HResult {
    pub const fn new(value: i32) -> Self {
        Self { value }
    }
    pub const fn from_e(value: HResultE) -> Self {
        Self::new(value as i32)
    }

    pub const fn e(self) -> HResultE {
        unsafe { core::mem::transmute(self) }
    }
}

impl From<HResultE> for HResult {
    fn from(value: HResultE) -> Self {
        unsafe { core::mem::transmute(value) }
    }
}

impl From<HResult> for HResultE {
    fn from(value: HResult) -> Self {
        unsafe { core::mem::transmute(value) }
    }
}

impl HResult {
    pub const fn is_success(self) -> bool {
        self.value >= 0
    }
    pub const fn is_failure(self) -> bool {
        self.value < 0
    }
}

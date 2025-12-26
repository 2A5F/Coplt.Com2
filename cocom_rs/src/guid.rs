use core::hash::Hash;
use core::str::FromStr;

#[repr(C, align(16))]
#[derive(Debug, Clone, Copy, Eq)]
pub struct Guid {
    a: u32,
    b: u16,
    c: u16,
    d: u8,
    e: u8,
    f: u8,
    g: u8,
    h: u8,
    i: u8,
    j: u8,
    k: u8,
}

impl Guid {
    pub const fn new(
        a: u32,
        b: u16,
        c: u16,
        d: u8,
        e: u8,
        f: u8,
        g: u8,
        h: u8,
        i: u8,
        j: u8,
        k: u8,
    ) -> Self {
        Self {
            a,
            b,
            c,
            d,
            e,
            f,
            g,
            h,
            i,
            j,
            k,
        }
    }
}

impl Guid {
    pub const fn from_array(
        [a0, a1, a2, a3, b0, b1, c0, c1, d, e, f, g, h, i, j, k]: [u8; 16],
    ) -> Self {
        Self {
            a: a0 as u32 | ((a1 as u32) << 8) | ((a2 as u32) << 16) | ((a3 as u32) << 24),
            b: b0 as u16 | ((b1 as u16) << 8),
            c: c0 as u16 | ((c1 as u16) << 8),
            d,
            e,
            f,
            g,
            h,
            i,
            j,
            k,
        }
    }
}

impl Guid {
    #[inline(always)]
    pub const fn from_u128(value: u128) -> Self {
        unsafe { core::mem::transmute(value) }
    }

    #[inline(always)]
    pub const fn to_u128(self) -> u128 {
        unsafe { core::mem::transmute(self) }
    }

    #[cfg(nightly)]
    #[inline(always)]
    pub const fn to_v128(self) -> core::simd::u8x16 {
        unsafe { core::mem::transmute(self) }
    }
}

impl Into<u128> for Guid {
    fn into(self) -> u128 {
        self.to_u128()
    }
}

impl From<u128> for Guid {
    fn from(value: u128) -> Self {
        Self::from_u128(value)
    }
}

impl Into<i128> for Guid {
    fn into(self) -> i128 {
        self.to_u128() as i128
    }
}

impl From<i128> for Guid {
    fn from(value: i128) -> Self {
        Self::from_u128(value as u128)
    }
}

#[cfg(not(nightly))]
impl PartialEq for Guid {
    #[inline(always)]
    fn eq(&self, other: &Self) -> bool {
        self.to_u128() == other.to_u128()
    }

    #[inline(always)]
    fn ne(&self, other: &Self) -> bool {
        self.to_u128() != other.to_u128()
    }
}

#[cfg(nightly)]
impl PartialEq for Guid {
    #[inline(always)]
    fn eq(&self, other: &Self) -> bool {
        self.to_v128() == other.to_v128()
    }

    #[inline(always)]
    fn ne(&self, other: &Self) -> bool {
        self.to_v128() != other.to_v128()
    }
}

impl Hash for Guid {
    #[inline(always)]
    fn hash<H: core::hash::Hasher>(&self, state: &mut H) {
        self.to_u128().hash(state)
    }
}

const fn reverse_endianness_u32(value: u32) -> u32 {
    // bswap in x86; rev in arm
    u32::rotate_right(value & 0x00FF00FFu32, 8) // xx zz
    + u32::rotate_left(value & 0xFF00FF00u32, 8) // ww yy
}

const fn reverse_endianness_u16(value: u16) -> u16 {
    (value >> 8) + (value << 8)
}

impl Guid {
    pub const fn reverse_abc_endianness(self) -> Self {
        Self {
            a: reverse_endianness_u32(self.a),
            b: reverse_endianness_u16(self.b),
            c: reverse_endianness_u16(self.c),
            ..self
        }
    }
}

const CHAR_TO_HEX_LOOKUP: [u8; 256] = [
    0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
    0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
    0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
    0x0, 0x1, 0x2, 0x3, 0x4, 0x5, 0x6, 0x7, 0x8, 0x9, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
    0xA, 0xB, 0xC, 0xD, 0xE, 0xF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
    0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xa,
    0xb, 0xc, 0xd, 0xe, 0xf, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
    0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
    0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
    0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
    0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
    0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
    0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
    0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
    0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
    0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
];

const fn decode_byte(c1: u8, c2: u8) -> Option<u8> {
    let upper = CHAR_TO_HEX_LOOKUP[c1 as usize];
    let lower = CHAR_TO_HEX_LOOKUP[c2 as usize];
    let result = (upper << 4) | lower;
    if (c1 as u16 | c2 as u16) >> 8 == 0 {
        Some(result)
    } else {
        None
    }
}

macro_rules! try_decode_byte {
    ($c1:expr, $c2:expr) => {
        match decode_byte($c1, $c2) {
            Some(val) => val,
            None => return None,
        }
    };
}

impl Guid {
    pub const fn from_str(str: &str) -> Option<Guid> {
        let str = str.as_bytes();
        if str.len() != 36 {
            return None;
        }

        if str[8] != b'-' || str[13] != b'-' || str[18] != b'-' || str[23] != b'-' {
            return None;
        }

        let mut bytes = [0u8; 16];
        bytes[0] = try_decode_byte!(str[6], str[7]);
        bytes[1] = try_decode_byte!(str[4], str[5]);
        bytes[2] = try_decode_byte!(str[2], str[3]);
        bytes[3] = try_decode_byte!(str[0], str[1]);

        bytes[4] = try_decode_byte!(str[11], str[12]);
        bytes[5] = try_decode_byte!(str[9], str[10]);

        bytes[6] = try_decode_byte!(str[16], str[17]);
        bytes[7] = try_decode_byte!(str[14], str[15]);

        bytes[8] = try_decode_byte!(str[19], str[20]);
        bytes[9] = try_decode_byte!(str[21], str[22]);
        bytes[10] = try_decode_byte!(str[24], str[25]);
        bytes[11] = try_decode_byte!(str[26], str[27]);
        bytes[12] = try_decode_byte!(str[28], str[29]);
        bytes[13] = try_decode_byte!(str[30], str[31]);
        bytes[14] = try_decode_byte!(str[32], str[33]);
        bytes[15] = try_decode_byte!(str[34], str[35]);

        let r = Guid::from_array(bytes);
        #[cfg(target_endian = "little")]
        {
            return Some(r.reverse_abc_endianness());
        }
        #[cfg(target_endian = "big")]
        {
            return Some(r);
        }
    }
}

impl FromStr for Guid {
    type Err = ();

    fn from_str(s: &str) -> Result<Self, Self::Err> {
        match Self::from_str(s) {
            Some(r) => Ok(r),
            None => Err(()),
        }
    }
}

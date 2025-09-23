#pragma once
#ifndef COPLT_COM_MACROS_H
#define COPLT_COM_MACROS_H

#ifndef COPLT_FORCE_INLINE
#ifdef _MSC_VER
#define COPLT_FORCE_INLINE __forceinline
#else
#define COPLT_FORCE_INLINE inline __attribute__((__always_inline__))
#endif
#endif

#ifndef COPLT_NO_INLINE
#ifdef _MSC_VER
#define COPLT_NO_INLINE __declspec(noinline)
#else
#define COPLT_NO_INLINE __attribute__((__noinline__))
#endif
#endif

#ifndef COPLT_U32_MAX
#define COPLT_U32_MAX 4294967295
#endif
#ifndef COPLT_U64_MAX
#define COPLT_U64_MAX 18446744073709551615
#endif

#if defined(_WIN64) || defined(__x86_64__) || defined(__ppc64__)
#ifndef COPLT_X64
#define COPLT_X64
#endif
#elif defined(__aarch64__) || defined(_M_ARM64)
#ifndef COPLT_ARM64
#define COPLT_ARM64
#endif
#endif

#ifndef COPLT_ENUM_FLAGS
#define COPLT_ENUM_FLAGS(Name, Type) enum class Name : Type;\
inline constexpr bool operator==(const Name a, const Type b)\
{\
return static_cast<Type>(a) == b;\
}\
inline constexpr bool operator!=(const Name a, const Type b)\
{\
return static_cast<Type>(a) != b;\
}\
inline constexpr bool operator&&(const Name a, const Name b)\
{\
return (a != 0) && (b != 0);\
}\
inline constexpr bool operator||(const Name a, const Name b)\
{\
return (a != 0) || (b != 0);\
}\
inline constexpr Name operator~(const Name value)\
{\
return static_cast<Name>(~static_cast<Type>(value));\
}\
inline constexpr Name operator&(const Name a, const Name b)\
{\
return static_cast<Name>(static_cast<Type>(a) & static_cast<Type>(b));\
}\
inline constexpr Name operator|(const Name a, const Name b)\
{\
return static_cast<Name>(static_cast<Type>(a) | static_cast<Type>(b));\
}\
inline constexpr Name operator^(const Name a, const Name b)\
{\
return static_cast<Name>(static_cast<Type>(a) ^ static_cast<Type>(b));\
}\
inline constexpr Name operator&=(Name& a, const Name b)\
{\
return a = a & b;\
}\
inline constexpr Name operator|=(Name& a, const Name b)\
{\
return a = a | b;\
}\
inline constexpr Name operator^=(Name& a, const Name b)\
{\
return a = a ^ b;\
}\
inline constexpr bool HasFlags(Name a, Name b)\
{\
return (a & b) == b;\
}\
inline constexpr bool HasAnyFlags(Name a, Name b)\
{\
return (a & b) != 0;\
}\
inline constexpr bool HasFlagsOnly(Name a, Name b)\
{\
return (a & ~(b)) == 0;\
}\
enum class Name : Type
#endif

#ifndef COPLT_UUID_MARK
#ifdef _MSC_VER
#define COPLT_UUID_MARK(ID) __declspec( uuid(ID) )
#else
#define COPLT_UUID_MARK(ID)
#endif
#endif

#ifndef COPLT_NOVTABLE
#ifdef _MSC_VER
#define COPLT_NOVTABLE __declspec(novtable)
#else
#define COPLT_NOVTABLE
#endif
#endif

#ifndef COPLT_OUT
#define COPLT_OUT
#endif

#endif //COPLT_COM_MACROS_H

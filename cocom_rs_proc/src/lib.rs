#![allow(dead_code)]

use proc_macro::TokenStream;
use quote::{format_ident, quote};
use syn::{
    Attribute, FieldsNamed, Generics, Ident, ItemStruct, LitStr, Token, TraitItemFn, Type,
    Visibility, braced, parse::Parse, parse_macro_input, punctuated::Punctuated, token::Brace,
};

struct InterfaceAttr {
    guid_lit: LitStr,
    guid: uuid::Uuid,
}

impl Parse for InterfaceAttr {
    fn parse(input: syn::parse::ParseStream) -> syn::Result<Self> {
        let guid_lit: LitStr = input.parse()?;
        let guid = uuid::Uuid::parse_str(&guid_lit.value())
            .map_err(|e| syn::Error::new(guid_lit.span(), e.to_string()))?;
        Ok(Self { guid_lit, guid })
    }
}

struct ItemInterface {
    pub attrs: Vec<Attribute>,
    pub vis: Visibility,
    pub trait_token: Token![trait],
    pub ident: Ident,
    pub colon_token: Token![:],
    pub parents: Punctuated<Ident, Token![+]>,
    pub brace_token: Brace,
    pub items: Vec<TraitItemFn>,
}

impl Parse for ItemInterface {
    fn parse(input: syn::parse::ParseStream) -> syn::Result<Self> {
        let outer_attrs = input.call(Attribute::parse_outer)?;
        let vis: Visibility = input.parse()?;
        let trait_token: Token![trait] = input.parse()?;
        let ident: Ident = input.parse()?;
        let colon_token: Token![:] = input.parse()?;
        let mut parents: Punctuated<Ident, Token![+]> = Punctuated::new();
        loop {
            parents.push_value(input.parse()?);
            if input.peek(Brace) {
                break;
            }
            parents.push_punct(input.parse()?);
            if input.peek(Brace) {
                break;
            }
        }
        let content;
        let brace_token = braced!(content in input);
        let mut items: Vec<TraitItemFn> = Vec::new();
        while !content.is_empty() {
            items.push(content.parse()?);
        }
        Ok(ItemInterface {
            attrs: outer_attrs,
            vis,
            trait_token,
            ident,
            colon_token,
            parents,
            brace_token,
            items,
        })
    }
}

#[proc_macro_attribute]
pub fn interface(attr: TokenStream, item: TokenStream) -> TokenStream {
    let attr = parse_macro_input!(attr as InterfaceAttr);
    let item = parse_macro_input!(item as ItemInterface);
    let attrs = &item.attrs;
    let vis = &item.vis;
    let name = &item.ident;
    let parent = &item.parents.first();
    let has_weak = item.parents.iter().any(|a| a.to_string() == "IWeak");
    let guid = attr.guid_lit.value();
    let name_str = name.to_string();
    let vtbl_name = format_ident!("VitualTable_{}", name_str);
    let methods = item.items.iter().map(|item| {
        let attrs = &item.attrs;
        let ident = &item.sig.ident;
        let inputs = &item.sig.inputs;
        let params = inputs.iter().skip(1);
        let args = inputs.iter().skip(1).map(|arg| match arg {
            syn::FnArg::Typed(t) => {
                let pat = &t.pat;
                quote! {#pat}
            }
            _ => quote! {},
        });
        let f_name = format_ident!("f_{}", ident);
        let ret = &item.sig.output;
        quote! {
            #(#attrs)*
            pub fn #ident(&self, #(#params),*) #ret {
                unsafe { ((*self.v_ptr()).#f_name)(self as _, #(#args),*) }
            }
        }
    });
    let weak = if !has_weak {
        quote! {}
    } else {
        quote! {
            impl impls::WeakRefCount for #name {
                fn AddRefWeak(this: *const Self) -> u32 {
                    IWeak::AddRefWeak(unsafe { &*this })
                }

                fn ReleaseWeak(this: *const Self) -> u32 {
                    IWeak::ReleaseWeak(unsafe { &*this })
                }

                fn TryUpgrade(this: *const Self) -> bool {
                    IWeak::TryUpgrade(unsafe { &*this })
                }
            }
        }
    };
    quote! {
        #(#attrs)*
        #[repr(C)]
        #vis struct #name {
            base: #parent,
        }

        impl #name {
            pub const fn new(v_ptr: *const details::#vtbl_name) -> Self {
                Self {
                    base: #parent::new(v_ptr as *const <#parent as Interface>::VitualTable),
                }
            }

            #[inline(always)]
            pub fn v_ptr(&self) -> *const details::#vtbl_name {
                self.base.v_ptr() as *const _
            }
        }

        impl cocom::Interface for #name
        {
            const GUID: Guid = Guid::from_str(#guid).unwrap();
            type VitualTable = details::#vtbl_name;
            type Parent = #parent;

            fn new(v_ptr: &'static details::#vtbl_name) -> Self {
                Self::new(v_ptr)
            }
        }

        impl core::fmt::Debug for #name {
            fn fmt(&self, f: &mut core::fmt::Formatter<'_>) -> core::fmt::Result {
                f.debug_struct(#name_str).finish()
            }
        }

        impl core::ops::Deref for #name {
            type Target = #parent;

            fn deref(&self) -> &Self::Target {
                &self.base
            }
        }

        impl core::ops::DerefMut for #name {
            fn deref_mut(&mut self) -> &mut Self::Target {
                &mut self.base
            }
        }

        impl impls::RefCount for #name {
            fn AddRef(this: *const Self) -> u32 {
                IUnknown::AddRef(unsafe { &*this })
            }

            fn Release(this: *const Self) -> u32 {
                IUnknown::Release(unsafe { &*this })
            }
        }

        #weak

        impl #name {
            #(#methods)*
        }
    }
    .into()
}

struct ObjectAttr {
    parent: Type,
    allocator: Option<Type>,
}

impl Parse for ObjectAttr {
    fn parse(input: syn::parse::ParseStream) -> syn::Result<Self> {
        let parent: Type = input.parse()?;
        let allocator = if let Ok(_) = input.parse::<Token![,]>() {
            Some(input.parse()?)
        } else {
            None
        };

        Ok(Self { parent, allocator })
    }
}

struct ItemObject {
    pub attrs: Vec<Attribute>,
    pub vis: Visibility,
    pub struct_token: Token![struct],
    pub ident: Ident,
    pub generics: Generics,
    pub fields: FieldsNamed,
    pub semi_token: Option<Token![;]>,
}

impl Parse for ItemObject {
    fn parse(input: syn::parse::ParseStream) -> syn::Result<Self> {
        let attrs: Vec<Attribute> = input.call(Attribute::parse_outer)?;
        let vis: Visibility = input.parse()?;
        let struct_token: Token![struct] = input.parse()?;
        let ident: Ident = input.parse::<Ident>()?;
        let generics: Generics = input.parse()?;
        let fields: FieldsNamed = input.parse()?;
        let semi_token: Option<Token![;]> = input.parse()?;
        Ok(Self {
            attrs,
            vis,
            struct_token,
            ident,
            generics,
            fields,
            semi_token,
        })
    }
}

#[proc_macro_attribute]
pub fn object(attr: TokenStream, item: TokenStream) -> TokenStream {
    let attr = parse_macro_input!(attr as ObjectAttr);
    let item = parse_macro_input!(item as ItemStruct);
    let parent = &attr.parent;
    let allocator = &attr.allocator;
    let ident = &item.ident;
    let allocator = allocator.as_ref().map(|allocator| quote! { #allocator }).unwrap_or_else(|| quote! { () });
    quote! {
        #item

        impl impls::Object for #ident {
            type Interface = #parent;
            type Allocator = #allocator;
        }

        impl impls::IUnknown for #ident {}
    }
    .into()
}

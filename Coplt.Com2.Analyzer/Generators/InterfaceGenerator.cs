using System.Collections.Immutable;
using System.Text;
using Coplt.Analyzers.Generators.Templates;
using Coplt.Analyzers.Utilities;
using Coplt.Com2.Analyzer.Generators.Templates;
using Coplt.Com2.Analyzer.Resources;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Coplt.Com2.Analyzer.Generators;

[Generator]
public class InterfaceGenerator : IIncrementalGenerator
{
    private static readonly SymbolDisplayFormat TypeDisplayFormat = new(
        globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Included,
        typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
        genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
        miscellaneousOptions:
        SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers |
        SymbolDisplayMiscellaneousOptions.UseSpecialTypes |
        SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier
    );

    public const string Id = "CoCom";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var sources = context.SyntaxProvider.ForAttributeWithMetadataName(
            "Coplt.Com.InterfaceAttribute",
            static (syntax, _) => syntax is StructDeclarationSyntax,
            Transform
        );
        context.RegisterSourceOutput(sources, Output);
    }

    public record struct Varying()
    {
        public AlwaysEq<List<Diagnostic>> diagnostics = new(new());
        public GenBase GenBase;
        public Guid guid;
        public string name;
        public string? parent;
        public bool isIUnknown;
        public ImmutableArray<Member> members;

        public void AddDiagnostic(Diagnostic diagnostic) => diagnostics.Value.Add(diagnostic);
    }

    public enum MemberKind
    {
        Method,
        Property,
    }

    [Flags]
    public enum MemberFlags
    {
        None = 0,
        Readonly = 1 << 0,
        Get = 1 << 1,
        Set = 1 << 2,
        GetReadOnly = 1 << 3,
        SetReadOnly = 1 << 4,
        GetSet = Get | Set,
        GetSetReadOnly = GetReadOnly | SetReadOnly,
    }

    public record struct Member
    {
        public MemberKind Kind;
        public MemberFlags Flags;
        public int Index;
        public string Name;
        public string ReturnType;
        public RefKind ReturnRefKind;
        public ImmutableArray<Param> Params;
    }

    public record struct Param
    {
        public string Name;
        public string Type;
        public RefKind RefKind;
    }

    private bool IsUnmanaged(ITypeSymbol type)
    {
        for (;;)
        {
            if (type is IPointerTypeSymbol ptr)
            {
                type = ptr.PointedAtType;
                continue;
            }
            else if (type is IFunctionPointerTypeSymbol fn)
            {
                var sig = fn.Signature;
                if (!IsUnmanaged(sig.ReturnType)) return false;
                foreach (var p in sig.Parameters)
                {
                    if (!IsUnmanaged(p.Type)) return false;
                }
                return true;
            }
            return type.IsUnmanagedType;
        }
    }

    private Varying Transform(GeneratorAttributeSyntaxContext ctx, CancellationToken _)
    {
        var varying = new Varying();
        var compilation = ctx.SemanticModel.Compilation;
        var attr = ctx.Attributes.First();

        var syntax = (TypeDeclarationSyntax)ctx.TargetNode;
        var semantic_model = ctx.SemanticModel;
        var symbol = (INamedTypeSymbol)ctx.TargetSymbol;
        varying.GenBase = Utils.BuildGenBase(syntax, symbol, compilation);
        varying.name = syntax.Identifier.ToString();

        varying.isIUnknown = symbol.ToDisplayString(TypeDisplayFormat) == "global::Coplt.Com.IUnknown";
        if (attr.ConstructorArguments is [{ Value: INamedTypeSymbol parent }, ..])
        {
            if (!varying.isIUnknown)
            {
                varying.parent = parent.ToDisplayString(TypeDisplayFormat);
            }
        }

        if (symbol.TypeParameters.Length > 0)
        {
            var desc = Utils.MakeError(Id, Strings.Get("Generator.Interface.Error.GenericNotAllow"));
            varying.AddDiagnostic(Diagnostic.Create(desc, syntax.Identifier.GetLocation()));
        }

        var attrs = symbol.GetAttributes();
        var guid_attr = attrs.FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == "System.Runtime.InteropServices.GuidAttribute");
        if (guid_attr == null)
        {
            var desc = Utils.MakeError(Id, Strings.Get("Generator.Interface.Error.MissingGuid"));
            varying.AddDiagnostic(Diagnostic.Create(desc, syntax.Identifier.GetLocation()));
        }
        else if (!Guid.TryParse($"{guid_attr.ConstructorArguments[0].Value}", out varying.guid))
        {
            var desc = Utils.MakeError(Id, Strings.Get("Generator.Interface.Error.InvalidGuid"));
            var loc = guid_attr.ApplicationSyntaxReference?.GetSyntax().GetLocation() ?? syntax.Identifier.GetLocation();
            varying.AddDiagnostic(Diagnostic.Create(desc, loc));
        }

        var members = new List<Member>();
        var member_inc = 0;
        foreach (var member_syntax in syntax.Members)
        {
            var is_partial = member_syntax.Modifiers.Any(a => a.IsKind(SyntaxKind.PartialKeyword));
            var is_public = member_syntax.Modifiers.Any(a => a.IsKind(SyntaxKind.PublicKeyword));
            if (!(is_partial && is_public)) continue;
            var index = member_inc++;
            if (member_syntax is MethodDeclarationSyntax method)
            {
                if (method.TypeParameterList != null)
                {
                    var desc = Utils.MakeError(Id, Strings.Get("Generator.Interface.Error.GenericNotAllow"));
                    varying.AddDiagnostic(Diagnostic.Create(desc, method.Identifier.GetLocation()));
                    continue;
                }
                var args = new List<Param>();
                var sym = semantic_model.GetDeclaredSymbol(method)!;
                if (!IsUnmanaged(sym.ReturnType))
                {
                    var desc = Utils.MakeError(Id, Strings.Get("Generator.Interface.Error.Managed"));
                    varying.AddDiagnostic(Diagnostic.Create(desc, method.ReturnType.GetLocation()));
                }
                var pi = 0;
                foreach (var p in sym.Parameters)
                {
                    var i = pi++;
                    if (!IsUnmanaged(p.Type))
                    {
                        var desc = Utils.MakeError(Id, Strings.Get("Generator.Interface.Error.Managed"));
                        varying.AddDiagnostic(Diagnostic.Create(desc, method.ParameterList.Parameters[i].GetLocation()));
                    }
                    args.Add(new()
                    {
                        Name = p.Name,
                        Type = p.Type.ToDisplayString(TypeDisplayFormat),
                        RefKind = p.RefKind,
                    });
                }
                members.Add(new()
                {
                    Kind = MemberKind.Method,
                    Flags = sym.IsReadOnly ? MemberFlags.Readonly : MemberFlags.None,
                    Index = index,
                    Name = sym.Name,
                    ReturnType = sym.ReturnType.ToDisplayString(TypeDisplayFormat),
                    ReturnRefKind = sym.RefKind,
                    Params = [..args],
                });
            }
            else if (member_syntax is PropertyDeclarationSyntax property)
            {
                var sym = semantic_model.GetDeclaredSymbol(property)!;
                MemberFlags flags = MemberFlags.None;
                if (sym.GetMethod is { } get)
                {
                    flags |= MemberFlags.Get;
                    if (get.IsReadOnly) flags |= MemberFlags.GetReadOnly;
                }
                if (sym.SetMethod is { } set)
                {
                    flags |= MemberFlags.Set;
                    if (set.IsReadOnly) flags |= MemberFlags.SetReadOnly;
                }
                if ((flags & MemberFlags.Get) != 0 && (flags & MemberFlags.Set) != 0)
                {
                    member_inc++;
                }
                members.Add(new()
                {
                    Kind = MemberKind.Property,
                    Flags = flags,
                    Index = index,
                    Name = sym.Name,
                    ReturnType = sym.Type.ToDisplayString(TypeDisplayFormat),
                    ReturnRefKind = sym.RefKind,
                    Params = [],
                });
            }
        }
        varying.members = [..members];

        return varying;
    }

    private static void Output(SourceProductionContext ctx, Varying varying)
    {
        if (varying.diagnostics.Value.Count > 0)
        {
            foreach (var diagnostic in varying.diagnostics.Value)
            {
                ctx.ReportDiagnostic(diagnostic);
            }
        }
        var code = new TemplateComInterface(varying).Gen();
        var source_text = SourceText.From(code, Encoding.UTF8);
        var raw_source_file_name = varying.GenBase.FileFullName;
        var sourceFileName = $"{raw_source_file_name}.com.interface.g.cs";
        ctx.AddSource(sourceFileName, source_text);
    }
}

using System.Collections.Immutable;

namespace Coplt.Com2.DefineModel;

public record struct ComDefine
{
    public required ImmutableArray<TypeDeclare> Types { get; set; }
    public required ImmutableArray<StructDeclare> Structs { get; set; }
    public required ImmutableArray<EnumDeclare> Enums { get; set; }
    public required ImmutableArray<InterfaceDeclare> Interfaces { get; set; }
}

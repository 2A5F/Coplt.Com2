namespace Coplt.Com2.DefineModel;

public record ComDefine
{
    public required List<TypeDeclare> Types { get; set; }
    public required List<StructDeclare> Structs { get; set; }
    public required List<EnumDeclare> Enums { get; set; }
    public required List<InterfaceDeclare> Interfaces { get; set; }
}

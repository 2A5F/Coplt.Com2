using System.Text.Json.Serialization;

namespace Coplt.Com2.DefineModel;

[JsonDerivedType(typeof(InterfaceDeclare), typeDiscriminator: "interface")]
[JsonPolymorphic(TypeDiscriminatorPropertyName = "kind")]
public abstract record ADeclare
{
    public required string Name { get; set; }
}

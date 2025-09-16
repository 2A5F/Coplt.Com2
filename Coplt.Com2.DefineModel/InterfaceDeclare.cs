namespace Coplt.Com2.DefineModel;

public record InterfaceDeclare : ADeclare
{
    public Guid Guid { get; set; }
    public Guid? Parent { get; set; }
}

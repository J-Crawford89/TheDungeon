public sealed class ItemInstance
{
    public Guid InstanceId { get; set; } = Guid.NewGuid();
    public ItemDefinition Definition { get; set; } = new();
    public int Quantity { get; set; } = 1;
}
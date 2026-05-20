public sealed class PhysicalDieRollSpec
{
	public DieRollVisualKind Kind { get; set; }
	public DieType DieType { get; set; }
	public DieVisualRole Role { get; set; } = DieVisualRole.Standard;
	public int FaceValue { get; set; }
}

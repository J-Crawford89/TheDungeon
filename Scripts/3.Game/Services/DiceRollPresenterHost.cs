public sealed class DiceRollPresenterHost
{
	public IDiceRollPresenter Presenter { get; set; } = NullDiceRollPresenter.Instance;
}

public static class TestDiceRoll
{
	public static readonly DiceRollPresenterHost NullHost = new() { Presenter = NullDiceRollPresenter.Instance };

	public static ResolutionService CreateResolution(IDiceRollRequestExecutor dice, DiceRollPresenterHost? host = null) =>
		new ResolutionService(dice, host ?? NullHost);
}

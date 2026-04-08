#nullable enable
using Godot;
using System;
using System.Collections.Generic;
using System.Text;

public partial class CharacterCreationScreen : Control
{
	[Export] private LineEdit _nameEdit = null!;
	[Export] private OptionButton _sexButton = null!;
	[Export] private OptionButton _classButton = null!;
	[Export] private OptionButton _raceButton = null!;
	[Export] private OptionButton _backgroundButton = null!;

	[Export] private Label _mightLabel = null!;
	[Export] private Label _constitutionLabel = null!;
	[Export] private Label _dexterityLabel = null!;
	[Export] private Label _agilityLabel = null!;
	[Export] private Label _intelligenceLabel = null!;
	[Export] private Label _wisdomLabel = null!;
	[Export] private Label _gravitasLabel = null!;
	[Export] private Label _luckLabel = null!;

	[Export] private RichTextLabel _characterDescriptionLabel = null!;

	[Export] private Button _rerollAbilityScoresButton = null!;
	[Export] private Button _backButton = null!;
	[Export] private Button _startGameButton = null!;

	public event Action? BackButtonPressed;
	public event Action<CharacterCreationState>? StartGameRequested;

	private CharacterCreationDependencies? _deps;
	private bool _captureDebugDiagnostics;

	private readonly CharacterCreationState _state = new();
	private readonly Dictionary<string, CharacterClassDefinition> _classesById = new();
	private readonly Dictionary<string, CharacterRaceDefinition> _racesById = new();
	private readonly Dictionary<string, CharacterBackgroundDefinition> _backgroundsById = new();

	private bool _blockSelectionHandlers;
	private bool _ignoreNameCallback;

	public void Initialize(CharacterCreationDependencies dependencies, bool captureDebugDiagnostics)
	{
		_deps = dependencies;
		_captureDebugDiagnostics = captureDebugDiagnostics;
	}

	public override void _Ready()
	{
		if (_deps == null)
		{
			GD.PushError("CharacterCreationScreen.Initialize must be called before entering the tree.");
			return;
		}

		_nameEdit.Text = _state.Name;
		_nameEdit.TextChanged += OnNameTextChanged;

		_sexButton.ItemSelected += OnSexItemSelected;
		_classButton.ItemSelected += OnClassItemSelected;
		_raceButton.ItemSelected += OnRaceItemSelected;
		_backgroundButton.ItemSelected += OnBackgroundItemSelected;

		_rerollAbilityScoresButton.Pressed += OnRerollPressed;
		_backButton.Pressed += () => BackButtonPressed?.Invoke();
		_startGameButton.Pressed += OnStartGamePressed;

		PopulateDropdowns();
		_deps!.CharacterCreation.RollAndApplyRolledScores(_state);
		SyncInitialSelectionsFromDropdowns();

		RefreshAbilityLabels();
		RefreshDescription();
		RefreshStartButton();

		if (_captureDebugDiagnostics)
			GD.Print("CharacterCreationScreen: debug diagnostics capture enabled (reserved for future use).");
	}

	private void PopulateDropdowns()
	{
		_blockSelectionHandlers = true;

		PopulateSexOptions();
		PopulateDefinitionOptions(_classButton, _deps!.CharacterClasses.All, _classesById, d => d.Id, d => d.Name);
		PopulateDefinitionOptions(_raceButton, _deps.CharacterRaces.All, _racesById, d => d.Id, d => d.Name);
		PopulateDefinitionOptions(_backgroundButton, _deps.CharacterBackgrounds.All, _backgroundsById, d => d.Id, d => d.Name);

		if (_classButton.ItemCount == 0)
			GD.PushWarning("Character creation: no character classes in database; assign CharacterClassDatabase on GameRoot.");
		if (_raceButton.ItemCount == 0)
			GD.PushWarning("Character creation: no races in database; assign CharacterRaceDatabase on GameRoot.");
		if (_backgroundButton.ItemCount == 0)
			GD.PushWarning("Character creation: no backgrounds in database; assign CharacterBackgroundDatabase on GameRoot.");

		_blockSelectionHandlers = false;
	}

	private void SyncInitialSelectionsFromDropdowns()
	{
		if (_sexButton.ItemCount > 0)
			ApplySexIndex(0);
		ApplyFirstDefinitionSelection(_classButton, _classesById, d => _state.SelectedClass = d);
		ApplyFirstDefinitionSelection(_raceButton, _racesById, d => _state.SelectedRace = d);
		ApplyFirstDefinitionSelection(_backgroundButton, _backgroundsById, d => _state.SelectedBackground = d);

		_deps!.CharacterCreation.RecomputeFinalAbilityScores(_state);
	}

	private static void ApplyFirstDefinitionSelection<T>(OptionButton button, Dictionary<string, T> map, Action<T?> assign)
		where T : class
	{
		if (button.ItemCount == 0)
		{
			assign(null);
			return;
		}
		var id = button.GetItemMetadata(0).AsString();
		if (string.IsNullOrEmpty(id) || !map.TryGetValue(id, out var def))
			assign(null);
		else
			assign(def);
	}

	private void PopulateSexOptions()
	{
		_sexButton.Clear();
		var idx = 0;
		foreach (CharacterSex sex in Enum.GetValues<CharacterSex>())
		{
			_sexButton.AddItem(sex.ToString());
			_sexButton.SetItemMetadata(idx, Variant.From(sex.ToString()));
			idx++;
		}
		if (idx > 0)
			_sexButton.Select(0);
	}

	private void PopulateDefinitionOptions<TDef>(
		OptionButton button,
		IReadOnlyList<TDef> items,
		Dictionary<string, TDef> map,
		Func<TDef, string> getId,
		Func<TDef, string> getName) where TDef : class
	{
		button.Clear();
		map.Clear();
		for (var i = 0; i < items.Count; i++)
		{
			var def = items[i];
			var id = getId(def);
			if (string.IsNullOrWhiteSpace(id))
				continue;
			map[id] = def;
			button.AddItem(getName(def));
			button.SetItemMetadata(button.ItemCount - 1, Variant.From(id));
		}
		if (button.ItemCount > 0)
			button.Select(0);
	}

	private void OnNameTextChanged(string newText)
	{
		if (_ignoreNameCallback)
			return;

		var sanitized = CharacterNameValidator.Sanitize(newText);
		if (sanitized != newText)
		{
			_ignoreNameCallback = true;
			_nameEdit.Text = sanitized;
			_nameEdit.CaretColumn = sanitized.Length;
			_ignoreNameCallback = false;
		}

		_state.Name = sanitized;
		RefreshDescription();
		RefreshStartButton();
	}

	private void OnSexItemSelected(long index)
	{
		if (_blockSelectionHandlers)
			return;
		ApplySexIndex((int)index);
		RefreshDescription();
		RefreshStartButton();
	}

	private void ApplySexIndex(int index)
	{
		if (index < 0 || index >= _sexButton.ItemCount)
			return;
		var key = _sexButton.GetItemMetadata(index).AsString();
		if (Enum.TryParse<CharacterSex>(key, ignoreCase: true, out var sex))
			_state.Sex = sex;
	}

	private void OnClassItemSelected(long index) =>
		OnDefinitionSelected(index, _classButton, _classesById, d => _state.SelectedClass = d);

	private void OnRaceItemSelected(long index) =>
		OnDefinitionSelected(index, _raceButton, _racesById, d => _state.SelectedRace = d);

	private void OnBackgroundItemSelected(long index) =>
		OnDefinitionSelected(index, _backgroundButton, _backgroundsById, d => _state.SelectedBackground = d);

	private void OnDefinitionSelected<T>(long index, OptionButton button, Dictionary<string, T> map, Action<T?> assign)
		where T : class
	{
		if (_blockSelectionHandlers)
			return;
		var i = (int)index;
		if (i < 0 || i >= button.ItemCount)
			return;
		var id = button.GetItemMetadata(i).AsString();
		if (string.IsNullOrEmpty(id) || !map.TryGetValue(id, out var def))
			assign(null);
		else
			assign(def);
		_deps!.CharacterCreation.RecomputeFinalAbilityScores(_state);
		RefreshDescription();
		RefreshStartButton();
	}

	private void OnRerollPressed()
	{
		_deps!.CharacterCreation.RollAndApplyRolledScores(_state);
		RefreshAbilityLabels();
		RefreshDescription();
	}

	private void OnStartGamePressed()
	{
		if (!CanStart())
			return;
		StartGameRequested?.Invoke(_state.Clone());
	}

	private bool CanStart() =>
		CharacterNameValidator.IsNonEmptyValid(_state.Name)
		&& _state.SelectedClass != null
		&& _state.SelectedRace != null
		&& _state.SelectedBackground != null;

	private void RefreshStartButton()
	{
		_startGameButton.Disabled = !CanStart();
	}

	private void RefreshAbilityLabels()
	{
		SetScoreLabel(_mightLabel, AbilityScore.Might);
		SetScoreLabel(_constitutionLabel, AbilityScore.Constitution);
		SetScoreLabel(_dexterityLabel, AbilityScore.Dexterity);
		SetScoreLabel(_agilityLabel, AbilityScore.Agility);
		SetScoreLabel(_intelligenceLabel, AbilityScore.Intelligence);
		SetScoreLabel(_wisdomLabel, AbilityScore.Wisdom);
		SetScoreLabel(_gravitasLabel, AbilityScore.Gravitas);
		SetScoreLabel(_luckLabel, AbilityScore.Luck);
	}

	private void SetScoreLabel(Label label, AbilityScore ability)
	{
		var v = _state.FinalAbilityScores.GetScore(ability);
		label.Text = $"{ability}: {v}";
	}

	private void RefreshDescription()
	{
		_characterDescriptionLabel.Text = BuildDescriptionBbcode();
	}

	private string BuildDescriptionBbcode()
	{
		var name = BbcodePlainText.SanitizeForEmbedding(_state.Name.Trim());
		if (string.IsNullOrEmpty(name))
			name = "…";

		var pronoun = _state.Sex == CharacterSex.Male ? "He" : "She";
		var raceName = BbcodePlainText.SanitizeForEmbedding(_state.SelectedRace?.Name ?? "Race");
		var className = BbcodePlainText.SanitizeForEmbedding(_state.SelectedClass?.Name ?? "Class");
		var bgName = BbcodePlainText.SanitizeForEmbedding(_state.SelectedBackground?.Name ?? "Background");

		var raceDesc = BbcodePlainText.SanitizeForEmbedding(_state.SelectedRace?.Description ?? "Race are typically blah blah. Here's a description of whatever.");
		var classDesc = BbcodePlainText.SanitizeForEmbedding(_state.SelectedClass?.Description ?? "Class are typically blah blah. Here's a description of whatever.");
		var bgDesc = BbcodePlainText.SanitizeForEmbedding(_state.SelectedBackground?.Description ?? "Background are typically blah blah. Here's a description of whatever.");

		var classHp = _state.SelectedClass?.BaseHp ?? 0;
		var raceHp = _state.SelectedRace?.BaseHp ?? 0;
		var con = _state.FinalAbilityScores.Constitution;
		var hp = classHp + raceHp + con;

		var gold = _state.SelectedBackground?.StartingGold ?? 0;

		var sb = new StringBuilder();
		sb.Append($"You are {name}. {pronoun} is a {raceName}, {className} who was a {bgName} before becoming an adventurer.\n\n");
		sb.Append("[b]RACE[/b]\n");
		sb.Append(raceDesc);
		sb.Append("\n\n[b]CLASS[/b]\n");
		sb.Append(classDesc);
		sb.Append("\n\n[b]BACKGROUND[/b]\n");
		sb.Append(bgDesc);
		sb.Append("\n\n[b]HIT POINTS[/b]\n");
		sb.Append($"You have {hp} hit points.\n\n");
		sb.Append("[b]ABILITIES[/b]\n");
		sb.Append("You have no special abilities.\n\n");
		// TODO: list definition-granted abilities when the player ability pipeline exists.
		sb.Append("[b]EQUIPMENT[/b]\n");
		sb.Append($"You start with {gold} gold and no other equipment.");
		// TODO: list StartingEquipment from class/race/background when inventory exists.

		return sb.ToString();
	}
}

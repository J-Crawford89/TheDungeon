using Godot;
using System;

public partial class CharacterPage : MarginContainer
{
    [Export] private VBoxContainer _identityList;
    [Export] private VBoxContainer _grantedAbilitiesList;
    [Export] private VBoxContainer _futureAbilitiesList;

    [Export] private PackedScene _collapsibleInfoRowScene;

    public void Populate(PlayerState player)
    {
        //clear
        foreach (Node child in _identityList.GetChildren())
            child.QueueFree();
        foreach (Node child in _grantedAbilitiesList.GetChildren())
            child.QueueFree();
        foreach (Node child in _futureAbilitiesList.GetChildren())
            child.QueueFree();

        //identity
        AddRow(_identityList, $"Race: {player.Race.Name}", player.Race.Description);
        AddRow(_identityList, $"Class: {player.Class.Name}", player.Class.Description);
        AddRow(_identityList, $"Background: {player.Background.Name}", player.Background.Description);

        //abilities
        foreach (var ability in player.GrantedAbilities)
        {
            AddRow(_grantedAbilitiesList, ability.)
        }
    }

    private void AddRow(VBoxContainer parent, string title, string descripition)
    {
        var row = _collapsibleInfoRowScene.Instantiate<CollapsibleInfoRow>();
        row.SetContent(title, descripition);
        parent.AddChild(row);
    }
}

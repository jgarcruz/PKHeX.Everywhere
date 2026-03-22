using PKHeX.CLI.Base;
using PKHeX.Facade;
using PKHeX.Facade.Pokemons;
using PKHeX.Facade.Repositories;
using Spectre.Console;

namespace PKHeX.CLI.Commands.EditPokemonCommand.Attributes;

internal class HeldItemAttribute(Pokemon pokemon, Game game)
    : EditPokemonAttribute.SimpleAttribute(pokemon, "Held item", () => pokemon.HeldItem.Name)
{
    public override Result HandleSelection()
    {
        var categories = game.Trainer.Inventories.InventoryTypes
            .Where(t => t != "KeyItems")
            .OrderBy(t => t);

        var categorySelection = AnsiConsole.Prompt(new SelectionPrompt<OptionOrBack>()
            .Title("Select item category:")
            .PageSize(10)
            .AddChoices(OptionOrBack.WithValues(categories))
            .WrapAround());

        if (categorySelection is not OptionOrBack.Option<string> selectedCategory)
            return Result.Continue;

        var items = game.Trainer.Inventories[selectedCategory.Value]
            .AllSupportedItems
            .OrderBy(i => i.Name);

        var itemSelection = AnsiConsole.Prompt(new SelectionPrompt<OptionOrBack>()
            .Title($"Select item from [bold]{selectedCategory.Value}[/]:")
            .PageSize(10)
            .EnableSearch()
            .AddChoices(OptionOrBack.WithValues(
                options: items,
                display: item => $"(#{item.Id:000}) {item.Name}"))
            .WrapAround());

        if (itemSelection is OptionOrBack.Option<ItemDefinition> selectedItem)
            Pokemon.HeldItem = selectedItem.Value;

        return Result.Continue;
    }
}

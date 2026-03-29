using FluentAssertions;
using PKHeX.Facade.Repositories;
using PKHeX.Facade.Tests.Base;

namespace PKHeX.Facade.Tests;

public class PokemonBoxTests
{
    [Theory]
    [SupportedSaveFiles]
    public void BoxShouldContainPokemon(string saveFile)
    {
        var game = Game.LoadFrom(saveFile);
        var allValid = game.Trainer.PokemonBox.All
            .Where(p => p.Species != SpeciesDefinition.None)
            .ToList();

        allValid.Should().HaveCountGreaterThan(0);
        allValid.Should().AllSatisfy(p =>
        {
            p.ResultStats.Attack.Should().BeGreaterThan(0);
            p.ResultStats.Defense.Should().BeGreaterThan(0);
            p.ResultStats.Health.Should().BeGreaterThan(0);
            p.ResultStats.Speed.Should().BeGreaterThan(0);
            p.ResultStats.Total.Should().BeGreaterThan(0);
            p.ResultStats.SpecialAttack.Should().BeGreaterThan(0);
            p.ResultStats.SpecialDefense.Should().BeGreaterThan(0);
        });
    }
}

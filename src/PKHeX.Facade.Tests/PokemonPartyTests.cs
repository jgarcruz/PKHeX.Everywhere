using FluentAssertions;
using PKHeX.Facade.Tests.Base;

namespace PKHeX.Facade.Tests;

public class PokemonPartyTests
{
    [Theory]
    [SupportedSaveFiles]
    public void PartyShouldContainPokemon(string saveFile)
    {
        var game = Game.LoadFrom(saveFile);
        game.Trainer.Party.Pokemons.Should().HaveCountGreaterThan(0);
        game.Trainer.Party.Pokemons.Should().AllSatisfy(p =>
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

    [Theory]
    [SupportedSaveFiles]
    public void PartyShouldBePersistedAcrossSaves(string saveFile)
    {
        var game = Game.LoadFrom(saveFile);
        var firstPokemon = game.Trainer.Party.Pokemons.First();

        firstPokemon.IsShiny.Should().BeFalse();

        firstPokemon.SetShiny(true);

        firstPokemon.IsShiny.Should().BeTrue();

        game.SaveAndReload(savedGame =>
        {
            var savedPokemon = savedGame.Trainer.Party.Pokemons.First();
            savedPokemon.PID.Should().Be(firstPokemon.PID);
            savedPokemon.IsShiny.Should().Be(firstPokemon.IsShiny);
        });
    }
}

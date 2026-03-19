using PKHeX.CLI.Base;
using PKHeX.CLI.Extensions;
using PKHeX.Facade.Extensions;
using PKHeX.Facade.Pokemons;
using Spectre.Console;
using PKHeX.Core;

namespace PKHeX.CLI.Commands.EditPokemonCommand;

abstract class EditPokemonAttribute(Pokemon pokemon)
{
    protected Pokemon Pokemon { get; } = pokemon;
    protected abstract string Label { get; }
    protected abstract string Value { get; }

    public virtual string Display => $"[yellow]{Label}:[/] {Value}";
    public virtual bool Hidden => false;

    public virtual Result HandleSelection() => Result.Continue;

    internal abstract class SimpleAttribute(Pokemon pokemon, Func<string> label, Func<string> value)
        : EditPokemonAttribute(pokemon)
    {
        protected SimpleAttribute(Pokemon pokemon, string label, string value)
            : this(pokemon, () => label, () => value)
        {
        }

        protected SimpleAttribute(Pokemon pokemon, string label, Func<string> value)
            : this(pokemon, () => label, value)
        {
        }

        protected override string Label => label();
        protected override string Value => value();
    }

    internal class ReadOnlyAttribute(Pokemon pokemon, string label, Func<string> value)
        : SimpleAttribute(pokemon, label, value)
    {
        public ReadOnlyAttribute(Pokemon pokemon, string label, string value)
            : this(pokemon, label, () => value)
        {
        }
    }

    public class NameAttribute(Pokemon pokemon) : SimpleAttribute(pokemon, "Name/Nickname", () => pokemon.Nickname)
    {
        public override Result HandleSelection()
        {
            var newName = AnsiConsole.Ask(Label, Pokemon.Nickname);
            newName = newName == Pokemon.NameDisplay()
                ? Pokemon.NameDisplay()
                : newName;

            Pokemon.ChangeNickname(newName);

            return Result.Continue;
        }
    }

    public class LevelAttribute(Pokemon pokemon) : SimpleAttribute(pokemon, "Level", () => pokemon.Level.ToString())
    {
        public override Result HandleSelection()
        {
            var newLevelString = AnsiConsole.Ask(Label, Pokemon.Level.ToString());
            var parsed = int.TryParse(newLevelString, out int level);
            if (!parsed) return Result.Continue;

            Pokemon.ChangeLevel(level);

            return Result.Continue;
        }
    }

    // upon selecting this we need a dropdown to show with each nature enum and have the user select 1
    // highlight the one the pokemon already is
    public class NatureAttribute(Pokemon pokemon) : SimpleAttribute(pokemon, "Nature", () => pokemon.Natures.Nature.ToString())
    {
        public override Result HandleSelection()
        {
            var newNature = AnsiConsole.Prompt(
                new SelectionPrompt<Nature>()
                    .Title("Select nature:")
                    .AddChoices(Enum.GetValues<Nature>())
                    .UseConverter(n => n.ToString())
            );

            Pokemon.Natures.ChangeNature(newNature);

            return Result.Continue;
        }
    }

    public class FlagsAttribute(Pokemon pokemon) : EditPokemonAttribute(pokemon)
    {
        protected override string Label => string.Empty;
        protected override string Value => string.Empty;

        public override string Display => $"[yellow]Is Egg:[/] {Pokemon.Egg.IsEgg.ToDisplayEmoji(),-3} " +
                                          $"[yellow]Infected:[/] {Pokemon.Flags.IsInfected.ToDisplayEmoji(),-3} " +
                                          $"[yellow]Cured:[/] {Pokemon.Flags.IsCured.ToDisplayEmoji()}";
    }

    public class IsShinyAttribute(Pokemon pokemon)
        : SimpleAttribute(pokemon, "IsShiny", () => YesNoPrompt.LabelFrom(pokemon.IsShiny))
    {
        public override Result HandleSelection()
        {
            var result = YesNoPrompt.AskOrDefault("Is Shiny", Pokemon.IsShiny);
            Pokemon.SetShiny(result);

            return Result.Continue;
        }
    }

    public abstract class PokemonStatsBaseAttribute(Pokemon pokemon, string label, Stats stats)
        : SimpleAttribute(pokemon, label, string.Empty)
    {
        public override string Display => $"[yellow]{Label}:[/]{Environment.NewLine}   " +
                                          $"HP {stats.Health,-3} " +
                                          $"Atk {stats.Attack,-3} " +
                                          $"Def {stats.Defense,-3} " +
                                          $"SpA {stats.SpecialAttack,-3} " +
                                          $"SpD {stats.SpecialDefense,-3} " +
                                          $"Spe {stats.Speed,-3} " +
                                          $"Total {stats.Total,-3} ";
    }

    public class EV(Pokemon pokemon) : PokemonStatsBaseAttribute(pokemon, "EV", pokemon.EVs);

    public class IV(Pokemon pokemon) : PokemonStatsBaseAttribute(pokemon, "IV", pokemon.IVs);

    public class BaseStats(Pokemon pokemon) : PokemonStatsBaseAttribute(pokemon, "Base", pokemon.BaseStats);
}
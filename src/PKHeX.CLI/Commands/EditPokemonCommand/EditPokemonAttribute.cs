using PKHeX.CLI.Base;
using PKHeX.CLI.Extensions;
using PKHeX.Facade.Extensions;
using PKHeX.Facade.Pokemons;
using PKHeX.Facade.Repositories;
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

    public class FriendshipAttribute(Pokemon pokemon) : SimpleAttribute(pokemon, "Friendship", () => pokemon.Friendship.ToString())
    {
        public override Result HandleSelection()
        {
            var newFriendshipString = AnsiConsole.Ask(Label, Pokemon.Friendship.ToString());
            var parsed = int.TryParse(newFriendshipString, out int friendship);
            if (!parsed) return Result.Continue;

            Pokemon.ChangeFriendship(friendship);

            return Result.Continue;
        }
    }

    public class NatureAttribute(Pokemon pokemon) : SimpleAttribute(pokemon, "Nature", () => pokemon.Natures.Nature.ToString())
    {
        public override Result HandleSelection()
        {
            var selection = AnsiConsole.Prompt(
                new SelectionPrompt<OptionOrBack>()
                    .Title("Select nature:")
                    .EnableSearch()
                    .AddChoices(OptionOrBack.WithValues(Enum.GetValues<Nature>()))
            );

            if (selection is OptionOrBack.Option<Nature> option)
                Pokemon.ChangeNature(option.Value);

            return Result.Continue;
        }
    }

    public class AbilityAttribute(Pokemon pokemon) : SimpleAttribute(pokemon, "Ability", () => pokemon.Ability.Ability.ToString())
    {
        public override Result HandleSelection()
        {
            var choices = Pokemon.GetAvailableAbilities();
            if (choices.Count == 0)
                return Result.Continue;

            var selection = AnsiConsole.Prompt(
                new SelectionPrompt<OptionOrBack>()
                    .Title("Select ability:")
                    .EnableSearch()
                    .AddChoices(OptionOrBack.WithValues(choices, a => a.Name))
            );

            if (selection is not OptionOrBack.Option<AbilityDefinition> option)
                return Result.Continue;

            Pokemon.ChangeAbility(option.Value);
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
        private static string StatsLine(Stats s) =>
            $"HP {s.Health,-3} Atk {s.Attack,-3} Def {s.Defense,-3} SpA {s.SpecialAttack,-3} SpD {s.SpecialDefense,-3} Spe {s.Speed,-3} Total {s.Total,-3}";

        public override string Display => $"[yellow]{Label}:[/]{Environment.NewLine}   {StatsLine(stats)}";

        protected string DisplayWithBase =>
            Display + Environment.NewLine +
            $"   [yellow]Base:[/] {StatsLine(Pokemon.BaseStats)}";
    }

    private class StatEditAttribute(
        Pokemon pokemon,
        string name,
        Func<int> getter,
        Action<int> setter,
        int min,
        int max,
        Func<int>? availableBudget = null)
        : SimpleAttribute(pokemon, name, () => getter().ToString())
    {
        public override Result HandleSelection()
        {
            var input = AnsiConsole.Ask(Label, getter().ToString());
            if (!int.TryParse(input, out int value)) return Result.Continue;
            value = Math.Clamp(value, min, max);
            if (availableBudget is not null)
                value = Math.Min(value, availableBudget());
            setter(value);
            return Result.Continue;
        }
    }

    public class EV(Pokemon pokemon) : PokemonStatsBaseAttribute(pokemon, "EV", pokemon.EVs)
    {
        private const int MaxTotal = 510;

        public override Result HandleSelection()
        {
            RepeatUntilExit(() =>
            {
                var evs = Pokemon.EVs;
                IEnumerable<EditPokemonAttribute> options =
                [
                    Stat("HP",  () => evs.Health,         v => evs.Health         = v, () => evs.Health),
                    Stat("Atk", () => evs.Attack,         v => evs.Attack         = v, () => evs.Attack),
                    Stat("Def", () => evs.Defense,        v => evs.Defense        = v, () => evs.Defense),
                    Stat("SpA", () => evs.SpecialAttack,  v => evs.SpecialAttack  = v, () => evs.SpecialAttack),
                    Stat("SpD", () => evs.SpecialDefense, v => evs.SpecialDefense = v, () => evs.SpecialDefense),
                    Stat("Spe", () => evs.Speed,          v => evs.Speed          = v, () => evs.Speed),
                ];
                var selected = AnsiConsole.Prompt(new SelectionPrompt<OptionOrBack>()
                    .Title(DisplayWithBase)
                    .AddChoices(OptionOrBack.WithValues(options, o => o.Display))
                    .WrapAround());
                return selected is OptionOrBack.Option<EditPokemonAttribute> opt
                    ? opt.Value.HandleSelection()
                    : Result.Exit;
            });
            return Result.Continue;
        }

        private StatEditAttribute Stat(string name, Func<int> getter, Action<int> setter, Func<int> current)
            => new(Pokemon, name, getter, setter, 0, 255,
                   availableBudget: () => MaxTotal - Pokemon.EVs.Total + current());
    }

    public class IV(Pokemon pokemon) : PokemonStatsBaseAttribute(pokemon, "IV", pokemon.IVs)
    {
        public override Result HandleSelection()
        {
            RepeatUntilExit(() =>
            {
                var ivs = Pokemon.IVs;
                IEnumerable<EditPokemonAttribute> options =
                [
                    new StatEditAttribute(Pokemon, "HP",  () => ivs.Health,         v => ivs.Health         = v, 0, 31),
                    new StatEditAttribute(Pokemon, "Atk", () => ivs.Attack,         v => ivs.Attack         = v, 0, 31),
                    new StatEditAttribute(Pokemon, "Def", () => ivs.Defense,        v => ivs.Defense        = v, 0, 31),
                    new StatEditAttribute(Pokemon, "SpA", () => ivs.SpecialAttack,  v => ivs.SpecialAttack  = v, 0, 31),
                    new StatEditAttribute(Pokemon, "SpD", () => ivs.SpecialDefense, v => ivs.SpecialDefense = v, 0, 31),
                    new StatEditAttribute(Pokemon, "Spe", () => ivs.Speed,          v => ivs.Speed          = v, 0, 31),
                ];
                var selected = AnsiConsole.Prompt(new SelectionPrompt<OptionOrBack>()
                    .Title(DisplayWithBase)
                    .AddChoices(OptionOrBack.WithValues(options, o => o.Display))
                    .WrapAround());
                return selected is OptionOrBack.Option<EditPokemonAttribute> opt
                    ? opt.Value.HandleSelection()
                    : Result.Exit;
            });
            return Result.Continue;
        }
    }

    public class ResultStats(Pokemon pokemon) : PokemonStatsBaseAttribute(pokemon, "Stats", pokemon.ResultStats);
    public class BaseStats(Pokemon pokemon) : PokemonStatsBaseAttribute(pokemon, "Base", pokemon.BaseStats);
}
using PKHeX.CLI.Base;
using PKHeX.Facade;
using Spectre.Console;

namespace PKHeX.CLI.Commands;

public static class ViewTrainerInfo
{
    private static class Choices
    {
        public const string EditName = "Edit Name";
        public const string EditMoney = "Edit Money";
        public const string Back = "[bold darkgreen]< Back[/]";
    }

    public static Result Handle(Game game)
    {
        RepeatUntilExit(() =>
        {
            PrintInfo(game);

            var selection = AnsiConsole.Prompt(new SelectionPrompt<string>()
                .Title("What would you like to do?")
                .AddChoices(Choices.EditName, Choices.EditMoney, Choices.Back)
                .WrapAround());

            return selection switch
            {
                Choices.EditName => EditName(game),
                Choices.EditMoney => EditMoney(game),
                _ => Result.Exit
            };
        });

        return Result.Continue;
    }

    private static void PrintInfo(Game game)
    {
        AnsiConsole.MarkupLine($"[bold darkgreen]Trainer Info[/]");
        Console.WriteLine();
        AnsiConsole.MarkupLine($"[bold darkgreen]TID/SID:[/] {game.Trainer.Id}");
        AnsiConsole.MarkupLine($"[bold darkgreen]Name:[/] {game.Trainer.Name}");
        AnsiConsole.MarkupLine($"[bold darkgreen]Gender:[/] {game.Trainer.Gender}");
        AnsiConsole.MarkupLine($"[bold darkgreen]Money:[/] [yellow]{game.Trainer.Money}[/]");

        Console.WriteLine();

        AnsiConsole.MarkupLine($"[bold darkgreen]Rival:[/] {game.Trainer.RivalName}");

        Console.WriteLine();
    }

    private static Result EditName(Game game)
    {
        var maxLength = game.Trainer.MaxNameLength;
        var newName = AnsiConsole.Ask($"Enter new trainer name [grey italic](max {maxLength} chars)[/]:", game.Trainer.Name);
        if (string.IsNullOrEmpty(newName))
        {
            AnsiConsole.MarkupLine("[red]Name cannot be empty.[/]");
            return Result.Continue;
        }
        if (newName.Length > maxLength)
        {
            AnsiConsole.MarkupLine($"[red]Name too long (max {maxLength} characters).[/]");
            return Result.Continue;
        }
        game.Trainer.Name = newName;
        AnsiConsole.MarkupLine($"[green]Name updated to {newName}.[/]");
        return Result.Continue;
    }

    private static Result EditMoney(Game game)
    {
        var newAmount = AnsiConsole.Ask($"Enter new money amount [grey italic](current: {game.Trainer.Money.Amount})[/]:", game.Trainer.Money.Amount);
        game.Trainer.Money.Set(newAmount);
        AnsiConsole.MarkupLine($"[green]Money updated to {game.Trainer.Money}.[/]");
        return Result.Continue;
    }
}

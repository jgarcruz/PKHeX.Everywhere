using PKHeX.Core;

namespace PKHeX.Facade.Pokemons;

public record PokemonNature(PKM Pokemon)
{
    public Nature Nature => Pokemon.Nature;
    public Nature StatNature => Pokemon.StatNature;

    public bool ChangeAll(Nature newNature)
    {
        if (newNature == Pokemon.Nature) return true;

        var oldNature = Pokemon.Nature;

        Pokemon.Nature = newNature;
        Pokemon.StatNature = newNature;

        return Pokemon.Nature != oldNature;
    }

    // this works gen 5+
    // before tho, nature is derived from PID
    // https://github.com/kwsch/PKHeX/blob/master/PKHeX.Core/PKM/PK4.cs
    // AHHHHHHHHHHHH
    public bool ChangeNature(Nature newNature)
    {
        if (newNature == Pokemon.Nature) return true;

        var oldNature = Pokemon.Nature;

        Pokemon.Nature = newNature;

        return Pokemon.Nature != oldNature;
    }

    public bool ChangeStatNature(Nature newNature)
    {
        if (newNature == Pokemon.StatNature) return true;

        var oldNature = Pokemon.StatNature;

        Pokemon.StatNature = newNature;

        return Pokemon.StatNature != oldNature;
    }

    public override string ToString() => Nature == StatNature
        ? Nature.ToString()
        : $"{Nature} / {StatNature}";
}
using PKHeX.Core;
using PKHeX.Facade.Repositories;

namespace PKHeX.Facade.Pokemons;

public class Pokemon(PKM pokemon, Game game)
{
    // for some reflection
    public Pokemon() : this(default!, default!)
    {
    }

    public UniqueId UniqueId => UniqueId.From(this);
    public PKM Pkm => pokemon;
    public Game Game => game;
    public byte Generation => pokemon.Generation;
    public byte Format => pokemon.Format;


    public ItemDefinition Ball
    {
        get => ItemRepository.GetItem(pokemon.Ball);
        set => pokemon.Ball = Convert.ToByte(value.Id);
    }

    public EntityId Id => new(Pkm.DisplayTID, Pkm.DisplaySID);
    public Owner Owner => new(pokemon);

    public uint PID => Pkm.PID;

    public GameVersionDefinition Version => GameVersionRepository.Instance.Get(Pkm.Version);

    public SpeciesDefinition Species
    {
        get => Game.SpeciesRepository.Get((Species)Pkm.Species);
        set
        {
            if (Pkm.Species == value.ShortId) return;

            Pkm.Species = value.ShortId;

            if (Pkm is ICombatPower combatPower) combatPower.ResetCP();
            if (!NicknameSet) Pkm.ClearNickname();
        }
    }

    public PokemonTypes Types => new(pokemon);
    public string Nickname => pokemon.Nickname;

    public bool NicknameSet =>
        !pokemon.Nickname.Equals(Species.Name, StringComparison.InvariantCultureIgnoreCase);

    public int Level => pokemon.CurrentLevel;

    public uint Experience
    {
        get => pokemon.EXP;
        set => pokemon.EXP = value;
    }

    public PokemonNature Natures => new(pokemon);

    public PokemonForm Form => new(pokemon);

    public Stats EVs => Stats.EvFrom(pokemon);
    public Stats IVs => Stats.IvFrom(pokemon);
    public Stats BaseStats => Stats.BaseFrom(pokemon);
    public Stats ResultStats => Stats.ResultStatsFrom(pokemon);
    public Stats? AVs => pokemon is IAwakened ? Stats.AvFrom(pokemon) : null;
    public PokemonMove Move1 => new(pokemon, PokemonMove.MoveIndex.Move1);
    public PokemonMove Move2 => new(pokemon, PokemonMove.MoveIndex.Move2);
    public PokemonMove Move3 => new(pokemon, PokemonMove.MoveIndex.Move3);
    public PokemonMove Move4 => new(pokemon, PokemonMove.MoveIndex.Move4);
    public Gender Gender
    {
        get => Gender.FromByte(pokemon.Gender);
        set => pokemon.SetGender(value.ToByte());
    }

    public bool IsShiny => pokemon.IsShiny;

    public ItemDefinition HeldItem
    {
        get => ItemRepository.GetItem(Convert.ToUInt16(pokemon.HeldItem));
        set => pokemon.HeldItem = value.Id;
    }

    public AbilityDefinition Ability
    {
        get => AbilityRepository.Instance.Get(pokemon.Ability);
        set => pokemon.Ability = value.Id;
    }

    public List<AbilityDefinition> GetAvailableAbilities()
    {
        var pi = Pkm.PersonalInfo;
        if (pi is not IPersonalAbility12 a)
            return [];

        var ids = new List<int> { a.Ability1, a.Ability2 };
        if (pi is IPersonalAbility12H h)
            ids.Add(h.AbilityH);

        return [.. ids.Distinct().Select(id => AbilityRepository.Instance.Get(id))];
    }

    public void ChangeAbility(AbilityDefinition ability)
    {
        var index = Pkm.PersonalInfo.GetIndexOfAbility(ability.Id);
        if (index < 0) return;
        Pkm.RefreshAbility(index);
    }

    public int Friendship
    {
        get => pokemon.CurrentFriendship;
        set => pokemon.CurrentFriendship = (byte)Math.Clamp(value, 0, 255);
    }

    public PokemonFlags Flags => new(pokemon);
    public MetConditions MetConditions => new(pokemon);
    public Egg Egg => new(pokemon);

    public Dictionary<PokemonMove.MoveIndex, PokemonMove> Moves => new()
    {
        { PokemonMove.MoveIndex.Move1, Move1 },
        { PokemonMove.MoveIndex.Move2, Move2 },
        { PokemonMove.MoveIndex.Move3, Move3 },
        { PokemonMove.MoveIndex.Move4, Move4 },
    };

    public void ChangeLevel(int level)
    {
        var clamped = Math.Clamp(level, 1, 100);
        pokemon.CurrentLevel = Convert.ToByte(clamped);
    }

    public void ChangeFriendship(int friendship)
    {
        var clamped = Math.Clamp(friendship, 0, 255);
        pokemon.CurrentFriendship = Convert.ToByte(clamped);
    }

    public void ChangeNickname(string nickname)
    {
        pokemon.SetNickname(nickname);
    }

    public void SetShiny(bool shiny)
    {
        pokemon.SetIsShiny(shiny);
    }

    public void ChangeMove(PokemonMove.MoveIndex moveIndex, MoveDefinition newMove)
    {
        var newMoveSet = new Moveset(
            moveIndex == PokemonMove.MoveIndex.Move1 ? newMove.Id : Move1.Move.Id,
            moveIndex == PokemonMove.MoveIndex.Move2 ? newMove.Id : Move2.Move.Id,
            moveIndex == PokemonMove.MoveIndex.Move3 ? newMove.Id : Move3.Move.Id,
            moveIndex == PokemonMove.MoveIndex.Move4 ? newMove.Id : Move4.Move.Id);

        if (newMoveSet.ToArray().All(m => m == MoveDefinition.None.Id))
        {
            return;
        }

        pokemon.SetMoves(newMoveSet);
        pokemon.FixMoves();
    }

    public bool ChangeNature(Nature newNature)
    {
        if (newNature == Pkm.Nature) return true;

        var oldNature = Pkm.Nature;
        if (Format < 5)
        {
            Pkm.SetPIDNature(newNature);
            return Pkm.Nature != oldNature;
        }
        else
        {
            Pkm.Nature = newNature;
            return Pkm.Nature != oldNature;
        }

    }

    public bool ChangeStatNature(Nature newNature)
    {
        if (newNature == Pkm.StatNature) return true;

        var oldNature = Pkm.StatNature;

        Pkm.StatNature = newNature;

        return Pkm.StatNature != oldNature;
    }

    public Pokemon MakeCopy()
    {
        var underlyingPkm = Pkm.Clone();
        underlyingPkm.ClearNickname();

        var isShiny = underlyingPkm.IsShiny;

        // re-roll the pid
        underlyingPkm.PID = EntityPID.GetRandomPID(Random.Shared, underlyingPkm.Species, underlyingPkm.Gender,
            underlyingPkm.Version, underlyingPkm.Nature, underlyingPkm.Form, underlyingPkm.PID);

        if (isShiny)
        {
            // because re-rolling may void the shiny status, we are making it shiny again
            underlyingPkm.SetIsShiny(true);
        }

        return new Pokemon(underlyingPkm, Game);
    }

    public Pokemon Clone() => new(Pkm.Clone(), Game);

    public void ApplyChangesFrom(Pokemon template, bool keepPid = true)
    {
        var pid = PID;
        template.Pkm.TransferPropertiesWithReflection(Pkm);

        if (keepPid)
        {
            Pkm.PID = pid;
        }
    }

    public File ToFile(bool encrypted = false)
    {
        return new File
        {
            Name = Pkm.FileName,
            Bytes = encrypted
                ? Pkm.EncryptedPartyData
                : Pkm.DecryptedPartyData
        };
    }

    public static Pokemon LoadFrom(
        byte[] bytes,
        Game? game = null)
    {
        var format =
            EntityFileExtension.GetContextFromExtension(string.Empty, game?.Generation ?? EntityContext.Gen6);
        var pkm = EntityFormat.GetFromBytes(bytes, prefer: format)
                  ?? throw new InvalidOperationException("The file did not load into a valid pokemon file.");

        var version = GameVersionRepository.Instance.Get(pkm.Version);

        return new Pokemon(pkm, game ?? Game.EmptyOf(version));
    }

    public class File
    {
        public required string Name { get; init; }
        public required byte[] Bytes { get; init; }
    }
}
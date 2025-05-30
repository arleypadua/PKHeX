using System;
using static System.Buffers.Binary.BinaryPrimitives;

namespace PKHeX.Core;

/// <summary>
/// <see cref="PersonalInfo"/> class with values from Generation 4 games.
/// </summary>
public sealed class PersonalInfo4(Memory<byte> Raw) : PersonalInfo, IPersonalAbility12, IPersonalInfoTM, IPersonalInfoTutorType
{
    public const int SIZE = 0x2C;

    private Span<byte> Data => Raw.Span;
    public override byte[] Write() => Raw.ToArray();

    public override int HP { get => Data[0x00]; set => Data[0x00] = (byte)value; }
    public override int ATK { get => Data[0x01]; set => Data[0x01] = (byte)value; }
    public override int DEF { get => Data[0x02]; set => Data[0x02] = (byte)value; }
    public override int SPE { get => Data[0x03]; set => Data[0x03] = (byte)value; }
    public override int SPA { get => Data[0x04]; set => Data[0x04] = (byte)value; }
    public override int SPD { get => Data[0x05]; set => Data[0x05] = (byte)value; }
    public override byte Type1 { get => Data[0x06]; set => Data[0x06] = value; }
    public override byte Type2 { get => Data[0x07]; set => Data[0x07] = value; }
    public override byte CatchRate { get => Data[0x08]; set => Data[0x08] = value; }
    public override int BaseEXP { get => Data[0x09]; set => Data[0x09] = (byte)value; }
    private int EVYield { get => ReadUInt16LittleEndian(Data[0x0A..]); set => WriteUInt16LittleEndian(Data[0x0A..], (ushort)value); }
    public override int EV_HP { get => (EVYield >> 0) & 0x3; set => EVYield = (EVYield & ~(0x3 << 0)) | ((value & 0x3) << 0); }
    public override int EV_ATK { get => (EVYield >> 2) & 0x3; set => EVYield = (EVYield & ~(0x3 << 2)) | ((value & 0x3) << 2); }
    public override int EV_DEF { get => (EVYield >> 4) & 0x3; set => EVYield = (EVYield & ~(0x3 << 4)) | ((value & 0x3) << 4); }
    public override int EV_SPE { get => (EVYield >> 6) & 0x3; set => EVYield = (EVYield & ~(0x3 << 6)) | ((value & 0x3) << 6); }
    public override int EV_SPA { get => (EVYield >> 8) & 0x3; set => EVYield = (EVYield & ~(0x3 << 8)) | ((value & 0x3) << 8); }
    public override int EV_SPD { get => (EVYield >> 10) & 0x3; set => EVYield = (EVYield & ~(0x3 << 10)) | ((value & 0x3) << 10); }
    public int Item1 { get => ReadInt16LittleEndian(Data[0xC..]); set => WriteInt16LittleEndian(Data[0xC..], (short)value); }
    public int Item2 { get => ReadInt16LittleEndian(Data[0xE..]); set => WriteInt16LittleEndian(Data[0xE..], (short)value); }
    public override byte Gender { get => Data[0x10]; set => Data[0x10] = value; }
    public override byte HatchCycles { get => Data[0x11]; set => Data[0x11] = value; }
    public override byte BaseFriendship { get => Data[0x12]; set => Data[0x12] = value; }
    public override byte EXPGrowth { get => Data[0x13]; set => Data[0x13] = value; }
    public override int EggGroup1 { get => Data[0x14]; set => Data[0x14] = (byte)value; }
    public override int EggGroup2 { get => Data[0x15]; set => Data[0x15] = (byte)value; }
    public int Ability1 { get => Data[0x16]; set => Data[0x16] = (byte)value; }
    public int Ability2 { get => Data[0x17]; set => Data[0x17] = (byte)value; }
    public override int EscapeRate { get => Data[0x18]; set => Data[0x18] = (byte)value; }
    public override int Color { get => Data[0x19] & 0x7F; set => Data[0x19] = (byte)((Data[0x19] & 0x80) | value); }
    public bool NoFlip { get => Data[0x19] >> 7 == 1; set => Data[0x19] = (byte)(Color | (value ? 0x80 : 0)); }

    public override int AbilityCount => 2;
    public override int GetIndexOfAbility(int abilityID) => abilityID == Ability1 ? 0 : abilityID == Ability2 ? 1 : -1;
    public override int GetAbilityAtIndex(int abilityIndex) => abilityIndex switch
    {
        0 => Ability1,
        1 => Ability2,
        _ => throw new ArgumentOutOfRangeException(nameof(abilityIndex), abilityIndex, null),
    };
    public int GetAbility(bool second) => second && HasSecondAbility ? Ability2 : Ability1;

    public bool HasSecondAbility => Ability1 != Ability2;

    // Manually added attributes
    public override byte FormCount { get => Data[0x29]; set {} }
    public override int FormStatsIndex { get => ReadUInt16LittleEndian(Data[0x2A..]); set {} }

    public void AddTypeTutors(ReadOnlySpan<byte> data) => TypeTutors = FlagUtil.GetBitFlagArray(data);
    public void CopyTypeTutors(PersonalInfo4 other) => TypeTutors = other.TypeTutors;

    /// <summary>
    /// Grass-Fire-Water-Etc typed learn compatibility flags for individual moves.
    /// </summary>
    public bool[] TypeTutors { get; private set; } = [];

    private const int TMHM = 0x1C;
    private const int CountTM = 92;
    private const int CountHM = 8;
    private const int CountTMHM = CountTM + CountHM;
    private const int ByteCountTM = (CountTMHM + 7) / 8;
    private const int CountTutor = 0x34;

    public bool GetIsLearnTM(int index)
    {
        if ((uint)index >= CountTM)
            return false;
        return (Data[TMHM + (index >> 3)] & (1 << (index & 7))) != 0;
    }

    public void SetIsLearnTM(int index, bool value)
    {
        if ((uint)index >= CountTM)
            return;
        if (value)
            Data[TMHM + (index >> 3)] |= (byte)(1 << (index & 7));
        else
            Data[TMHM + (index >> 3)] &= (byte)~(1 << (index & 7));
    }

    public void SetAllLearnTM(Span<bool> result, ReadOnlySpan<ushort> moves)
    {
        var span = Data.Slice(TMHM, ByteCountTM);
        for (int index = CountTM - 1; index >= 0; index--)
        {
            if ((span[index >> 3] & (1 << (index & 7))) != 0)
                result[moves[index]] = true;
        }
    }

    public bool GetIsLearnTutorType(int index)
    {
        if ((uint)index >= CountTutor)
            return false;
        return TypeTutors[index];
    }

    public void SetIsLearnTutorType(int index, bool value)
    {
        if ((uint)index >= CountTutor)
            throw new ArgumentOutOfRangeException(nameof(index), index, null);
        TypeTutors[index] = value;
    }

    public void SetAllLearnTutorType(Span<bool> result, ReadOnlySpan<ushort> moves)
    {
        for (int index = CountTutor - 1; index >= 0; index--)
        {
            if (TypeTutors[index])
                result[moves[index]] = true;
        }
    }

    public bool GetIsLearnHM(int index)
    {
        if ((uint)index >= CountHM)
            return false;
        index += CountTM;
        return (Data[TMHM + (index >> 3)] & (1 << (index & 7))) != 0;
    }

    public void SetAllLearnHM(Span<bool> result, ReadOnlySpan<ushort> moves)
    {
        var span = Data.Slice(TMHM, ByteCountTM);
        for (int index = CountTM + CountHM - 1; index >= CountTM; index--)
        {
            if ((span[index >> 3] & (1 << (index & 7))) != 0)
                result[moves[index - CountTM]] = true;
        }
    }

    /// <summary>
    /// Gets the preferred list of HM moves to disallow on transfer from <see cref="PK4"/> to <see cref="PK5"/>.
    /// </summary>
    /// <remarks>
    /// If Defog is in the moveset, then we prefer HG/SS (remove Whirlpool) over D/P/Pt.
    /// Defog is a competitively viable move, while Whirlpool is not really useful.
    /// </remarks>
    /// <param name="hasDefog">True if the current moveset has <see cref="Move.Defog"/>.</param>
    public static ReadOnlySpan<ushort> GetPreferredTransferHMs(bool hasDefog) => hasDefog ? MachineMovesHiddenHGSS : MachineMovesHiddenDPPt;

    /// <summary> Species that can learn <see cref="Move.BlastBurn"/> via Move Tutor on Route 210. </summary>
    public static ReadOnlySpan<ushort> SpecialTutorBlastBurn => [006, 157, 257, 392];

    /// <summary> Species that can learn <see cref="Move.HydroCannon"/> via Move Tutor on Route 210. </summary>
    public static ReadOnlySpan<ushort> SpecialTutorHydroCannon => [009, 160, 260, 395];

    /// <summary> Species that can learn <see cref="Move.FrenzyPlant"/> via Move Tutor on Route 210. </summary>
    public static ReadOnlySpan<ushort> SpecialTutorFrenzyPlant => [003, 154, 254, 389];

    /// <summary> Species that can learn <see cref="Move.DracoMeteor"/> via Move Tutor on Route 210. </summary>
    public static ReadOnlySpan<ushort> SpecialTutorDracoMeteor => [147, 148, 149, 230, 329, 330, 334, 371, 372, 373, 380, 381, 384, 443, 444, 445, 483, 484, 487];

    /// <summary>
    /// Special tutor moves available via the Move Tutors added in Pt/HG/SS.
    /// </summary>
    public static ReadOnlySpan<ushort> TutorMoves =>
    [
        291, 189, 210, 196, 205, 009, 007, 276, 008, 442, 401, 466, 380, 173, 180, 314,
        270, 283, 200, 246, 235, 324, 428, 410, 414, 441, 239, 402, 334, 393, 387, 340,
        271, 257, 282, 389, 129, 253, 162, 220, 081, 366, 356, 388, 277, 272, 215, 067,
        143, 335, 450, 029,
    ];

    /// <summary>
    /// Technical Machine moves corresponding to their index within TM bitflag permissions.
    /// </summary>
    public static ReadOnlySpan<ushort> MachineMovesTechnical =>
    [
        264, 337, 352, 347, 046, 092, 258, 339, 331, 237,
        241, 269, 058, 059, 063, 113, 182, 240, 202, 219,
        218, 076, 231, 085, 087, 089, 216, 091, 094, 247,
        280, 104, 115, 351, 053, 188, 201, 126, 317, 332,
        259, 263, 290, 156, 213, 168, 211, 285, 289, 315,
        355, 411, 412, 206, 362, 374, 451, 203, 406, 409,
        261, 318, 373, 153, 421, 371, 278, 416, 397, 148,
        444, 419, 086, 360, 014, 446, 244, 445, 399, 157,
        404, 214, 363, 398, 138, 447, 207, 365, 369, 164,
        430, 433,
    ];

    /// <summary>
    /// Hidden Machines in D/P/Pt.
    /// </summary>
    public static ReadOnlySpan<ushort> MachineMovesHiddenDPPt =>
    [
        (int)Move.Cut,
        (int)Move.Fly,
        (int)Move.Surf,
        (int)Move.Strength,
        (int)Move.Defog,
        (int)Move.RockSmash,
        (int)Move.Waterfall,
        (int)Move.RockClimb,
    ];

    /// <summary>
    /// Hidden Machines in HG/SS.
    /// </summary>
    public static ReadOnlySpan<ushort> MachineMovesHiddenHGSS =>
    [
        (int)Move.Cut,
        (int)Move.Fly,
        (int)Move.Surf,
        (int)Move.Strength,
        (int)Move.Whirlpool,
        (int)Move.RockSmash,
        (int)Move.Waterfall,
        (int)Move.RockClimb,
    ];
}

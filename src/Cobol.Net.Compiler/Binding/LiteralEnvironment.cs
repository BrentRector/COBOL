// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding.Model;

namespace CobolNet.Binding;

/// <summary>
/// The compile-time context a §8.8.3 concatenation expression folds in (kb/Work PB1406): the two things an
/// operand's value depends on besides its own spelling.
/// <list type="bullet">
/// <item><b>The HIGH-VALUE / LOW-VALUE characters.</b> §8.3.3.6.4 GR6/GR7 make them the extremes of the collating
/// sequence in effect, and that sequence is a property of WHERE the figurative is written: inside the SPECIAL-NAMES
/// paragraph it is the NATIVE sequence selected by the clause's NATIONAL phrase (§12.3.7.4 GR10), everywhere else
/// the program collating sequence of the operand's class (§12.3.7.4 GR8/GR9; the native pins when none is declared —
/// the <c>FigurativeConstants.FillChar</c> posture).</item>
/// <item><b>The words that stand for a literal.</b> §13.10.3 SR2 lets a constant-name stand wherever a format
/// specifies a literal of its class and category, and §12.3.7.4 GR11 a) makes a symbolic-character a figurative
/// constant — the §8.8.3.1 format writes literal-1 / literal-2, so both are operands. They resolve through the
/// program's own tables, as those tables stand at the fold (definition precedes reference, §13.10.4 GR1).</item>
/// </list>
/// ⛔ The constructor is private: an environment is obtained from one of the three NAMED contexts below, so a
/// fold site cannot forget the national table or the word tables — the PB1406 defect was six fold sites (two in
/// SPECIAL-NAMES, STOP, INVOKE, the boolean channel, OPTIONS) that passed no national table, so a national
/// HIGH-VALUE operand folded as U+00FF.
/// </summary>
internal sealed class LiteralEnvironment
{
    private readonly DataBinder? _data;
    private readonly AlphabetDef? _collate;
    private readonly NationalAlphabetDef? _natCollate;
    /// <summary>True inside the SPECIAL-NAMES paragraph, where §12.3.7.4 GR10 makes HIGH-VALUE / LOW-VALUE the NATIVE
    /// extremes — "in the native national collating sequence, when the NATIONAL phrase is specified, or in the native
    /// alphanumeric collating sequence otherwise". Both native sequences are the 65,536 UTF-16 code units in code-unit
    /// order, so the phrase selects the same pair (<see cref="CobolNet.Runtime.NativeCollatingSequence"/>, owner
    /// decision kb/Work R52) and is not carried.</summary>
    private readonly bool _inSpecialNames;

    private LiteralEnvironment(DataBinder? data, AlphabetDef? collate, NationalAlphabetDef? natCollate,
        bool inSpecialNames, bool refusesSymbolicCharacters)
    {
        _data = data;
        _collate = collate;
        _natCollate = natCollate;
        _inSpecialNames = inSpecialNames;
        RefusesSymbolicCharacters = refusesSymbolicCharacters;
    }

    /// <summary>A literal position that precedes every table a program declares — the OPTIONS paragraph and the
    /// identification-division AS phrases: no collating sequence is declared yet (the native pins apply) and no
    /// constant-name or symbolic-character is defined yet.</summary>
    public static LiteralEnvironment Unscoped { get; } = new(null, null, null, false, false);

    /// <summary>A literal position outside the SPECIAL-NAMES paragraph (the DATA and PROCEDURE divisions and the
    /// environment-division clauses bound after SPECIAL-NAMES): the program collating sequences and the program's
    /// constant-name and symbolic-character tables.</summary>
    public static LiteralEnvironment Program(DataBinder data) =>
        new(data, data.Collating, data.NationalCollating, false, false);

    /// <summary>A literal written INSIDE the SPECIAL-NAMES paragraph (§12.3.7.4 GR10's native extremes — see
    /// <see cref="_inSpecialNames"/>); <paramref name="refusesSymbolicCharacters"/>
    /// is true for the literals §12.3.7.3 SR11 governs ("Literal-1, literal-2, literal-3, literal-4, literal-5,
    /// literal-6, and literal-9 shall specify neither a symbolic-character figurative constant nor a zero-length
    /// literal").</summary>
    public static LiteralEnvironment SpecialNames(DataBinder data, bool refusesSymbolicCharacters) =>
        new(data, null, null, true, refusesSymbolicCharacters);

    /// <summary>True where §12.3.7.3 SR11 forbids a symbolic-character operand.</summary>
    public bool RefusesSymbolicCharacters { get; }

    /// <summary>The HIGH-VALUE character a figurative of class <paramref name="cat"/> stands for here
    /// (§8.3.3.6.4 GR6).</summary>
    public char HighValue(PicCategory cat) => _inSpecialNames
        ? CobolNet.Runtime.NativeCollatingSequence.HighValue
        : cat is PicCategory.National && _natCollate is { } nat ? nat.HighValue
        : cat is not (PicCategory.National or PicCategory.Boolean) && _collate is { } alnum ? alnum.HighValue
        : CobolNet.Runtime.NativeCollatingSequence.HighValue;

    /// <summary>The LOW-VALUE character a figurative of class <paramref name="cat"/> stands for here
    /// (§8.3.3.6.4 GR7).</summary>
    public char LowValue(PicCategory cat) => _inSpecialNames ? CobolNet.Runtime.NativeCollatingSequence.LowValue
        : cat is PicCategory.National && _natCollate is { } nat ? nat.LowValue
        : cat is not (PicCategory.National or PicCategory.Boolean) && _collate is { } alnum ? alnum.LowValue
        : '\0';

    /// <summary>The constant-name <paramref name="word"/> names here, or null (§13.10.3 SR2).</summary>
    public DataBinder.ConstantDef? Constant(string word) => _data?.FindConstant(word);

    /// <summary>Whether <paramref name="word"/> is a symbolic-character name here — in SPECIAL-NAMES, one the
    /// paragraph declares in any clause, since the clauses are order-free (kb/Work PB226).</summary>
    public bool IsSymbolicCharacter(string word) => _data is not null
        && (_inSpecialNames ? _data.IsSymbolicCharacterName(word) : _data.SymbolicOf(word) is not null);

    /// <summary>The ONE character a bound symbolic-character stands for (§12.3.7.4 GR11 b/c), or null when
    /// <paramref name="word"/> names none that is bound.</summary>
    public string? SymbolicValue(string word) => _data?.SymbolicOf(word)?.Value;
}

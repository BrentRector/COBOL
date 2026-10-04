// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding;
using CobolNet.Binding.Model;
using CobolNet.CodeGen.Emit;

namespace CobolNet.CodeGen;

/// <summary>
/// THE §14.6.2.3.2 action-1 BACKGROUND — the one place that answers "what fills storage that has no VALUE
/// clause" (kb/Work PB152).
///
/// <para><b>The rule, in order.</b> §14.6.2.3.2 numbers the initial-state actions, and the first two settle both
/// scope and sequence: action 1 — "If the INITIALIZE clause is specified in the OPTIONS paragraph, the storage
/// allocated for the implied or associated sections is set to the specified-fill-character"; action 2 — "The
/// internal data described in the working-storage section and local-storage section is initialized as described
/// in 13.18.63, VALUE clause." So the fill is a BACKGROUND laid down FIRST and then OVERWRITTEN by VALUE
/// clauses — never a per-item substitute for a missing VALUE. Nothing here needs sequencing code: a field
/// initializer is one expression, and this changes only what the expression is when no VALUE was written.</para>
///
/// <para><b>Why it exists as its own type.</b> "What fills a VALUE-less item" was composed in THREE places and
/// only one of them had ever been fixed. PB151 wired the ALLOCATE arm (§14.9.3.4 GR8/GR9) and left the other
/// two: <c>PicInfo.DefaultInitializer</c> (the native-field arm) and <c>GroupImageCodec</c>'s fall-through (the
/// Tier-B image arm) each hardcoded <c>' '</c> / <c>'0'</c> / <c>0L</c> and never consulted the OPTIONS model at
/// all. Worse, the landed arm's fill decoder was PRIVATE to <c>PtrEmitter</c>, so the two dead arms could not
/// have reused it even if someone had tried. Measured on 2acbd842: with
/// <c>OPTIONS. INITIALIZE ALL TO X"5A".</c> a VALUE-less <c>PIC X(4)</c> displayed four SPACES.
/// After this, every storage form added later inherits the background by construction, because there is exactly
/// one choke point to route through — and a drift test asserts the two arms agree.</para>
///
/// <para><b>⛔ EVERY STORAGE POSITION TAKES THE FILL, NUMERIC ITEMS INCLUDED</b> (owner decision kb/Work R53,
/// overturning the former "a native-numeric carrier takes its zero" determination; kb/Work PB1134). §14.6.2.3.2
/// action 1 sets "the storage allocated for the implied or associated sections" to the specified-fill-character
/// with no exception for numeric items, and §13.18.63.4 GR4 c)'s "undefined" latitude is the NO-clause baseline
/// (§11.9.10.4 GR6), not a license to ignore a clause the program wrote. So a VALUE-less fixed-point numeric item
/// of a governed section is given IMAGE storage at bind time (<c>StorageFormPass</c>, the same promotion a group
/// move's receiver gets) and its image is the fill: a USAGE DISPLAY item holds the fill CHARACTERS, a BINARY /
/// COMP-5 / PACKED item holds the fill BYTES (the fill character's byte by the storage-byte law — HIGH-VALUES is
/// 0xFF), and a floating-point item's native carrier takes the value its fill bytes encode (it holds every IEEE bit
/// pattern). A USAGE INDEX item's eight bytes take the fill byte, a NATIONAL-usage numeric item is promoted to its
/// image like any other (kb/Work PB1466), and an OO method's LOCAL-STORAGE is walked under the method's own
/// effective OPTIONS clause (<c>StorageFormPass</c> step 5b). Two carve-outs, each from a RULE rather than from
/// convenience:</para>
/// <list type="bullet">
///   <item><b>Class object / message-tag / pointer take NULL, never the fill.</b> §13.18.63.4 GR4 c) states this
///     as a positive requirement — those items "are initialized to null" — in the same sentence that leaves
///     every other item undefined. A rule beats a background.</item>
///   <item><b>USAGE BIT keeps its packed zero seed</b> (D19 / kb/Work PB43). Its storage is ceil(n/8) PACKED
///     bytes laid out by the §8.5.1.6.3 walk, not a run of character positions, so a character fill has no
///     position to occupy. A DISPLAY-form boolean item DOES take the fill: under D-B1 its storage is one
///     alphanumeric character per boolean position, which is exactly what the background sets.</item>
/// </list>
/// <para>Recorded in <c>docs/CONFORMANCE.md</c> and <c>COBOLNET_DATA_MODEL_DESIGN.md</c> D23.</para>
/// </summary>
internal sealed class InitialStateBackground(EmitContext ctx)
{
    private HashSet<DataItem>? _wsRoots;
    private HashSet<DataItem>? _lsRoots;
    private IReadOnlyList<DataItem>? _lsSource;   // the list _lsRoots was built from — rebuilt when the activation changes

    /// <summary>The §11.9.10.4 GR5 specified-fill-character that applies to THIS item's initial state, or null
    /// when no background applies to it — the ONE predicate, so section selectivity, the GR7 exclusion and the
    /// no-clause case are answered once instead of once per storage arm.</summary>
    /// <summary>THE §11.9.10.4 GR5 specified-fill-character of the clause itself, section selection aside — or
    /// null when there is no clause, or when its literal-1 was rejected by §11.9.10.3 SR1 (a rejected clause
    /// behaves as an absent one rather than laying down a byte the program did not conformingly ask for).
    ///
    /// <para>⛔ GR5 b and d resolve through <c>FigurativeConstants.FillChar</c> — the compiler's ONE definition
    /// of the alphanumeric high and low value — and NOT through a second map written here. §8.3.3.6.4 GR6 makes
    /// the high-value format "the character … that has the highest ordinal position in the program collating
    /// sequence", so it is a PCS-dependent fact, not a constant. This was measured wrong: PB151's landed
    /// ALLOCATE arm carried its own map spelling HIGH-VALUES as U+FFFF, while every other HIGH-VALUE in the
    /// compiler was U+00FF under the native sequence — one rule, two places, two answers, and the arm with the
    /// private copy was the one that disagreed with the rest of the compiler.</para>
    ///
    /// <para>⛔ GR5 a is NOT GR5 d. "If BINARY ZEROES is specified, a string of binary zeros is the
    /// specified-fill-character" is the literal zero byte; "If LOW-VALUES is specified, the alphanumeric low
    /// value character" is the collating-sequence minimum. They coincide under the native sequence and DIVERGE
    /// under a PROGRAM COLLATING SEQUENCE whose lowest character is not NUL, so they take separate arms.</para>
    /// </summary>
    private char? ClauseFillChar() => ctx.Data.Options?.Initialize switch
    {
        null => null,
        { Fill: OptionsFill.BinaryZeroes } => '\0',
        { Fill: OptionsFill.HighValues } => FigurativeConstants.FillChar('H', ctx.Data.Collating),
        { Fill: OptionsFill.LowValues } => FigurativeConstants.FillChar('L', ctx.Data.Collating),
        { Fill: OptionsFill.Spaces } => FigurativeConstants.FillChar('S', ctx.Data.Collating),
        { Fill: OptionsFill.Literal, LiteralFillChar: var c } => c,
        // ⛔ EXHAUSTIVE BY CONSTRUCTION over §11.9.10.4 GR5 a–e. A new OptionsFill member is a new GR5 arm and
        // needs a derived fill character, so it fails LOUD here rather than defaulting to a space — a silent
        // default would make the new arm behave as "no clause" and look like it worked.
        { Fill: var unknown } => throw new InvalidOperationException(
            $"OPTIONS INITIALIZE fill kind {unknown} has no ISO §11.9.10.4 GR5 fill character; add its arm"),
    };

    private char? FillFor(DataItem item)
    {
        if (ctx.Data.Options?.Initialize is not { } init) return null;   // GR6: no clause ⇒ implementor's choice
        if (ClauseFillChar() is not { } fill) return null;               // SR1-rejected literal ⇒ as if absent

        // The section predicate is OptionsInitialize.Governs — shared with the bind-time storage decision.
        var root = item;
        while (root.Parent is { } p) root = p;
        _wsRoots ??= [.. ctx.Data.WorkingStorageRoots];
        // The unit's own LOCAL-STORAGE roots — or, inside an OO method's initializers, the METHOD's (kb/Work PB1215).
        IReadOnlyList<DataItem> lsSource = ctx.ActivationLocalRoots ?? ctx.Data.LocalStorageRoots;
        if (!ReferenceEquals(_lsSource, lsSource)) { _lsRoots = [.. lsSource]; _lsSource = lsSource; }
        return init.Governs(root, _wsRoots.Contains(root), _lsRoots!.Contains(root)) ? fill : null;
    }

    /// <summary>The background seed for a VALUE-less elementary item, or null to fall back to the caller's own
    /// no-clause baseline — <see cref="PicInfo.DefaultInitializer"/> on the native-field axis, the category
    /// zero/space expression on the Tier-B image axis. Both baselines stay exactly what they were.
    ///
    /// <para>⛔ ONE METHOD, BOTH AXES — and deliberately not two same-bodied twins named for their callers.
    /// The seed depends on the ITEM and its PICTURE, never on which lane is asking: a character-formed item's
    /// background is the same run of fill characters whether it is read through its own typed field or through
    /// the string backing its REDEFINES class shares. Two entry points would have been two places for the rule
    /// to drift apart, which is the exact defect this type exists to end (kb/Work PB152 — the fill was written
    /// in three places and one of them disagreed).</para></summary>
    public string? Seed(DataItem item, PicInfo pic)
    {
        if (FillFor(item) is not { } fill) return null;
        if (CharacterFormed(pic)) return FillRun(fill, pic.Length);
        if (pic.Category is not PicCategory.Numeric) return null;
        // A USAGE INDEX data item holds EIGHT bytes — its value is the signed big-endian integer of those bytes (the INDEX
        // byte form, PicInfo.StorageWidth) — so every one of them takes the fill character's BYTE (kb/Work PB1134).
        if (pic.Usage is Usage.Index)
        {
            ulong bytes = CobolNet.Runtime.StorageByte.ToByte(fill) * 0x0101010101010101UL;
            return $"unchecked((long)0x{bytes:X16}UL)";
        }
        // A numeric item's storage takes the fill too (owner decision R53): DISPLAY storage holds the fill
        // CHARACTER, a byte form holds the fill character's BYTE (the storage-byte law — HIGH-VALUES is 0xFF).
        char unit = pic.Usage is Usage.Display ? fill : CobolNet.Runtime.StorageByte.ToChar(CobolNet.Runtime.StorageByte.ToByte(fill));
        // Image storage — promoted by StorageFormPass for exactly these items, or a Tier-B REDEFINES window.
        if (item.StoreAsImage || (item.Class is not null && !pic.IsFloat && pic.HasImageByteForm))
            return FillRun(unit, item.ImageWidth);
        // A floating-point item's native carrier holds every IEEE bit pattern: the value its fill bytes encode.
        if (pic.IsFloat)
        {
            string image = FillRun(unit, item.ImageWidth);
            return item.Class is not null ? image : RuntimeApi.NumParseImageFloat(image, item.ProfileName, pic.IsSingle);
        }
        return null;   // a Numeric shape with no image form and no native seed here
    }

    /// <summary>Whether this item's storage is a run of CHARACTER POSITIONS that a fill character can occupy —
    /// the determination in this file's header, as one predicate both arms share. ⛔ A USAGE BIT boolean is
    /// EXCLUDED while a DISPLAY-form boolean is included: only the second stores one alphanumeric character per
    /// position (D-B1); the first is ceil(n/8) packed bytes (D19).</summary>
    private static bool CharacterFormed(PicInfo pic) => pic.Category switch
    {
        PicCategory.Alphanumeric or PicCategory.NumericEdited or PicCategory.National => true,
        PicCategory.Boolean => pic.Usage is not Usage.Bit,
        _ => false,
    };

    /// <summary>The fill run as a C# expression. A zero-width item (a zero-length picture) yields the empty
    /// string rather than a <c>new string(c, 0)</c> allocation.</summary>
    private static string FillRun(char fill, int length) =>
        length <= 0 ? "\"\"" : $"new string({EmitText.CsCharLiteral(fill)}, {length})";

    /// <summary>The §14.9.3.4 GR8/GR9 ALLOCATE background as a C# char literal (kb/Work PB151's arm, now reading
    /// the resolved model instead of re-decoding the literal). ⛔ GR8/GR9 key on the CLAUSE, not on its section
    /// list — allocated storage belongs to no section, so <see cref="FillFor"/>'s section predicate deliberately
    /// does NOT govern here; the CONFORMANCE.md GR8 determination records that. No clause ⇒ the space, which
    /// GR8's "the content is undefined" admits.</summary>
    public string AllocateFillLiteral() => EmitText.CsCharLiteral(ClauseFillChar() ?? ' ');
}

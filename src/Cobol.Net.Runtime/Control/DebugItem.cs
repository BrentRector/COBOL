// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Runtime;

/// <summary>
/// The cause that triggered a debugging declarative for a <c>USE FOR DEBUGGING ON procedure-name / ALL PROCEDURES</c>
/// subject (X3.23-1985 debug module — the DEBUG-CONTENTS taxonomy, corroborated by the CCVS DB101A witness). The
/// facility was deleted by ISO/IEC 1989:2002 and is absent from ISO/IEC 1989:2023, so its authoritative behavior is
/// the 1985 standard; WiseOwl COBOL models it only at <c>--std 85</c> (VCR Table 7 row 7.17). Each enumerand renders one
/// DEBUG-CONTENTS string (<see cref="DebugItem.Populate"/>); the transfer-of-control kind that reaches the subject
/// procedure selects it (the PC dispatcher knows the transfer kind).
/// </summary>
public enum DebugCause
{
    /// <summary>A plain PERFORM / GO TO / altered GO TO transfer to the procedure — DEBUG-CONTENTS is SPACES
    /// (X3.23-1985; DB101A "DBCONT-HOLD EQUAL TO SPACE"). The default (0), so an unset field renders SPACES.</summary>
    Transfer = 0,

    /// <summary>First execution of the FIRST nondeclarative procedure — DEBUG-CONTENTS "START PROGRAM"
    /// (DB101A START-PROGRAM-TEST).</summary>
    StartProgram,

    /// <summary>Sequential fall-through into the procedure — DEBUG-CONTENTS "FALL THROUGH" (DB101A).</summary>
    FallThrough,

    /// <summary>The 2nd..nth iteration of a PERFORM … TIMES/UNTIL/VARYING whose range is the procedure —
    /// DEBUG-CONTENTS "PERFORM LOOP" (DB101A, "PERFORM LOOP-ROUTINE FIVE TIMES").</summary>
    PerformLoop,
}

/// <summary>
/// The X3.23-1985 <c>DEBUG-ITEM</c> special register (the '85 debug module). It is IMPLICITLY described — no DATA
/// DIVISION entry — and referenced only inside debugging declaratives. Its members are string-imaged fixed-width
/// fields; the compiler resolves a DEBUG-* reference to a read-only view over this object (a
/// <c>DebugRegisterPlace</c>), so MOVE / DISPLAY of a member "just work". The debug trigger the PC dispatcher
/// injects at a subject procedure's entry space-fills the whole register then sets the members for the triggering
/// occurrence (<see cref="Populate"/>) before it runs the declarative body.
/// <para>Layout (the X3.23-1985 implicit description — DEBUG-SUB-n is S9(4) SIGN LEADING SEPARATE, five
/// characters). The CCVS DB201A witness (<c>MOVE DEBUG-SUB-1 TO SUB-1-1</c>, <c>SUB-1-1 PIC X(5)</c>, then
/// <c>IF SUB-1-1 = "0004"</c>) is SIGN-AGNOSTIC, not a disproof of the signed form: per ISO MOVE GR6a
/// (§14.9.25.4, specs/ISO_COBOL.md:28921) a signed numeric's SEPARATE sign character is NOT moved to an
/// alphanumeric receiver, so S9(4) SIGN LEADING SEPARATE holding +4 → X(5) yields <c>"0004 "</c> — identical to
/// the unsigned image the witness tests. WiseOwl COBOL pins the authoritative signed 5-char width; DEBUG-SUB is
/// SPACES when the triggering reference is not subscripted.</para>
/// <code>
/// 01  DEBUG-ITEM.
///     02  DEBUG-LINE     PIC X(6).
///     02  FILLER         PIC X   VALUE SPACE.
///     02  DEBUG-NAME     PIC X(30).
///     02  FILLER         PIC X   VALUE SPACE.
///     02  DEBUG-SUB-1    PIC S9(4) SIGN LEADING SEPARATE.   *> spaces when the reference is not subscripted
///     02  FILLER         PIC X   VALUE SPACE.
///     02  DEBUG-SUB-2    PIC S9(4) SIGN LEADING SEPARATE.
///     02  FILLER         PIC X   VALUE SPACE.
///     02  DEBUG-SUB-3    PIC S9(4) SIGN LEADING SEPARATE.
///     02  FILLER         PIC X   VALUE SPACE.
///     02  DEBUG-CONTENTS PIC X(30).  *> implementor-defined width (WiseOwl COBOL pins 30, enough for every
///                                    *> procedure-trigger DEBUG-CONTENTS token; the data/file record-image
///                                    *> legs are staged — see COBOLNET1571).
/// </code>
/// </summary>
public sealed class DebugItem
{
    /// <summary>DEBUG-LINE width — PIC X(6).</summary>
    public const int LineWidth = 6;
    /// <summary>DEBUG-NAME width — PIC X(30).</summary>
    public const int NameWidth = 30;
    /// <summary>DEBUG-SUB-1/2/3 width — S9(4) SIGN LEADING SEPARATE (a sign character + four digits = five).</summary>
    public const int SubWidth = 5;
    /// <summary>DEBUG-CONTENTS width — WiseOwl COBOL's pinned implementor width (§ implementor-defined).</summary>
    public const int ContentsWidth = 30;

    /// <summary>The whole DEBUG-ITEM group image width — the members plus the single-space FILLER between each
    /// (6 + 1 + 30 + 1 + 5 + 1 + 5 + 1 + 5 + 1 + 30 = 86; a DEBUG-SUB-n is S9(4) SIGN LEADING SEPARATE = 5).</summary>
    public const int GroupWidth =
        LineWidth + 1 + NameWidth + 1 + SubWidth + 1 + SubWidth + 1 + SubWidth + 1 + ContentsWidth;

    /// <summary>DEBUG-LINE — the source line of the CAUSING statement (the statement whose execution
    /// triggered the debugging declarative — the Wave F review fix, pinned by DB101A; implementor-defined
    /// format: WiseOwl COBOL right-justifies the decimal image in X(6)).</summary>
    public string DebugLine { get; private set; } = new string(' ', LineWidth);

    /// <summary>DEBUG-NAME — the leftmost 30 characters of the triggering procedure-name.</summary>
    public string DebugName { get; private set; } = new string(' ', NameWidth);

    /// <summary>DEBUG-SUB-1 — SPACES for a procedure trigger (a procedure reference is not subscripted).</summary>
    public string DebugSub1 { get; private set; } = new string(' ', SubWidth);
    /// <summary>DEBUG-SUB-2 — SPACES for a procedure trigger.</summary>
    public string DebugSub2 { get; private set; } = new string(' ', SubWidth);
    /// <summary>DEBUG-SUB-3 — SPACES for a procedure trigger.</summary>
    public string DebugSub3 { get; private set; } = new string(' ', SubWidth);

    /// <summary>DEBUG-CONTENTS — the DEBUG-CONTENTS taxonomy string for the triggering cause (X3.23-1985).</summary>
    public string DebugContents { get; private set; } = new string(' ', ContentsWidth);

    /// <summary>The whole DEBUG-ITEM group image (§13.18 group MOVE reads this): the members concatenated with the
    /// single-space FILLER positions between them.</summary>
    public string Image =>
        DebugLine + " " + DebugName + " " + DebugSub1 + " " + DebugSub2 + " " + DebugSub3 + " " + DebugContents;

    /// <summary>Space-fill the whole register (X3.23-1985: DEBUG-ITEM is space-filled before each execution of a
    /// debugging declarative), then set DEBUG-LINE / DEBUG-NAME / DEBUG-CONTENTS for a procedure trigger. DEBUG-SUB-n
    /// stays SPACES (a procedure reference has no subscripts).</summary>
    /// <param name="line">The subject procedure's source line number.</param>
    /// <param name="name">The triggering procedure-name (left-justified/truncated to 30).</param>
    /// <param name="cause">The transfer-of-control cause selecting the DEBUG-CONTENTS token.</param>
    public void Populate(int line, string name, DebugCause cause)
    {
        DebugLine = Fit(line >= 0 ? line.ToString(System.Globalization.CultureInfo.InvariantCulture) : "", LineWidth, right: true);
        DebugName = Fit(name, NameWidth, right: false);
        DebugSub1 = new string(' ', SubWidth);
        DebugSub2 = new string(' ', SubWidth);
        DebugSub3 = new string(' ', SubWidth);
        DebugContents = Fit(ContentsFor(cause), ContentsWidth, right: false);
    }

    /// <summary>The DEBUG-CONTENTS token for a procedure-trigger cause (X3.23-1985; the DB101A-witnessed taxonomy).
    /// The SORT INPUT/OUTPUT, MERGE OUTPUT and USE PROCEDURE causes are staged — a program combining an active
    /// debugging declarative with those is rejected COBOLNET1571 at bind, so they never reach here.</summary>
    private static string ContentsFor(DebugCause cause) => cause switch
    {
        DebugCause.StartProgram => "START PROGRAM",
        DebugCause.FallThrough => "FALL THROUGH",
        DebugCause.PerformLoop => "PERFORM LOOP",
        _ => "",   // Transfer — SPACES (a plain PERFORM / GO TO transfer)
    };

    /// <summary>Fit a string to an exact width: pad with spaces (left- or right-justified) or truncate.</summary>
    private static string Fit(string s, int width, bool right)
    {
        s ??= "";
        if (s.Length > width) return right ? s[^width..] : s[..width];
        return right ? s.PadLeft(width) : s.PadRight(width);
    }
}

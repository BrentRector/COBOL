// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Common;
using CobolNet.Editions;
using CobolNet.Editions.Diagnostics;
using CobolNet.Runtime;

namespace CobolNet.Binding.Model;

/// <summary>The data category a PICTURE describes (ISO/IEC 1989:2023 §8.4.2).</summary>
public enum PicCategory
{
    /// <summary>A group item (no PIC) — maps to a C# <c>record struct</c>.</summary>
    Group,
    /// <summary>Alphanumeric (<c>X</c>) or alphabetic (<c>A</c>) — maps to <see cref="string"/>.</summary>
    Alphanumeric,
    /// <summary>Numeric (<c>9</c>/<c>S</c>/<c>V</c>/<c>P</c>) — maps to <see cref="long"/> or <see cref="decimal"/>.</summary>
    Numeric,
    /// <summary>Numeric-edited (<c>Z * $ , . + - CR DB B 0</c>) — a formatted display image; later slice.</summary>
    NumericEdited,
    /// <summary>National (<c>PIC N</c> / <c>USAGE NATIONAL</c>, ISO §8.5.2.10 / §13.18.40.4 GR9/GR14) — LIVE
    /// (Phase 4a track (a)): one .NET UTF-16 <see cref="char"/> per national position over the string substrate
    /// (the documented D-N1 implementor choice; §13.18.60.4 GR8 + §8.1.2 NOTE 2 leave the size implementor-
    /// specified). Width machinery is CHARACTER-position based throughout. A REDEFINES class no longer refuses a
    /// national leaf: the D-N2 refusal went when kb/Work PB231 collapsed the byte-window carriage gate into ONE
    /// predicate, and a national leaf on either side of a REDEFINES
    /// class is a MOVE receiver like its DISPLAY twin (§13.18.60.4 GR2 — kb/Work PB572, pinned by
    /// <c>conformance:2002/pb572_national_redefines_receiver</c>).</summary>
    National,
    /// <summary>Boolean (<c>PIC 1</c>, optionally <c>USAGE BIT</c>, ISO §8.5.2.5 / §13.18.40.4 GR8) — LIVE
    /// (Phase 4a track (a)): one alphanumeric character '0'/'1' per boolean position (the §13.18.40.4 GR14 R14
    /// representation license — a permanently conforming choice, D-B1); byte=char holds, so boolean leaves ride
    /// every character surface. True bit-packing stays an optional future representation (residue).</summary>
    Boolean,
    /// <summary>Object reference (ISO §8.5.2.14; USAGE OBJECT REFERENCE [class-name], §13.18.60.4) — LIVE as of
    /// the Phase-3 OO spine: a PICTURE-less elementary item holding a .NET object reference (typed → the class's
    /// C# type, universal → <c>object?</c>; <see cref="PicInfo.ObjectRef"/>). Occupies NO character
    /// positions — it never participates in a group's character image (its size is implementor-defined storage,
    /// not part of the §13.18.60 GR4 image rules; a whole-group image over one is rejected loud).</summary>
    ObjectReference,
    /// <summary>Data pointer (USAGE POINTER, ISO §8.5.2.6 / §13.18.60) — LIVE as of Phase-4b increment 1: a
    /// PICTURE-less elementary item holding a data address, carried by the runtime <c>ManagedPointer</c>
    /// (feedback_one_mechanism_per_job — the ONE managed-ref carrier; never an 8-byte handle). Increment 1 holds
    /// only NULL (SET TO NULL / pointer, equality); ADDRESS OF / BASED / ALLOCATE are increment 2+.</summary>
    Pointer,
    /// <summary>Program pointer (USAGE PROGRAM-POINTER, ISO §8.5.2.15 / §13.18.60 GR24) — LIVE (P10 Step 7): a
    /// PICTURE-less elementary item that may contain the address of a program — for a COBOL program, an
    /// OUTERMOST program's externalized identity — carried by the runtime <c>ProgramPointer</c> and resolved
    /// through the ONE run-unit <c>ProgramTable</c> (SET … TO ENTRY §8.4.3.13; CALL §14.9.4 SR1; relations
    /// §8.8.4.2.16). Occupies NO character positions (the Pointer/ObjectReference image posture).</summary>
    ProgramPointer,
    /// <summary>Function pointer (USAGE FUNCTION-POINTER TO function-prototype-name-1, ISO §8.5.2.7 /
    /// §13.18.60.4 GR26) — LIVE (kb/Work PB452 + PB817): a PICTURE-less elementary item that "may contain the
    /// address of a function", carried by the runtime <c>FunctionPointer</c> and resolved through the ONE
    /// run-unit <c>ProgramTable</c> (<c>ADDRESS OF FUNCTION</c> §8.4.3.12; SET Format 8 §14.9.39.4 GR14;
    /// relations §8.8.4.2.16). ⛔ EVERY function-pointer is RESTRICTED: §13.18.60.2's general format prints
    /// <c>FUNCTION-POINTER TO function-prototype-name-1</c> with no brackets (measured on the printed folio 503),
    /// unlike its bracketed POINTER and PROGRAM-POINTER neighbours — so <see cref="PicInfo.RestrictedPrototypeName"/>
    /// is never null on this category and GR26's signature invariant always has a prototype to compare against.
    /// Occupies NO character positions (the Pointer/ProgramPointer image posture).</summary>
    FunctionPointer,
}

/// <summary>A SIGN clause's content (ISO §13.18.52): position (LEADING/TRAILING) and SEPARATE CHARACTER mode.
/// Captured per data-description entry — including on a GROUP item, whose clause applies to every subordinate
/// signed numeric item with the NEAREST enclosing clause taking precedence (§13.18.52 GR1–3).</summary>
public sealed record SignSpec(bool Leading, bool Separate);

/// <summary>The physical representation a <c>USAGE</c> clause selects.</summary>
/// <summary>The GROUP-USAGE clause (ISO §13.18.29; data-model design D20; kb/Work PB79): None for an ordinary
/// group — "an alphanumeric group item" (GR3) — Bit / National for a bit group / national group, declared by the
/// clause or implied for every group subordinate to one (SR2/SR3).</summary>
public enum GroupUsage
{
    /// <summary>No GROUP-USAGE clause specified or implied — an alphanumeric group item (§13.18.29.4 GR3).</summary>
    None,
    /// <summary>GROUP-USAGE BIT — a bit group: class and category boolean, treated as an elementary item of usage bit
    /// described with PICTURE 1(m) (§13.18.29.4 GR1).</summary>
    Bit,
    /// <summary>GROUP-USAGE NATIONAL — a national group: class and category national, treated as an elementary item
    /// of usage national described with PICTURE N(m) (§13.18.29.4 GR2).</summary>
    National,
}

public enum Usage
{
    /// <summary>USAGE DISPLAY (the default) — character form.</summary>
    Display,
    /// <summary>COMP / COMP-4 / BINARY — binary integer.</summary>
    Binary,
    /// <summary>COMP-3 / PACKED-DECIMAL — packed decimal.</summary>
    Packed,
    /// <summary>COMP-5 — native binary (no PICTURE truncation).</summary>
    Comp5,
    /// <summary>COMP-1 — single-precision float.</summary>
    Float,
    /// <summary>COMP-2 — double-precision float.</summary>
    Double,
    /// <summary>USAGE INDEX — an index data item (class index, ISO §13.18.60): holds an occurrence number, which in
    /// the typed-native model IS the index representation (a <c>long</c>, COBOLNET_DESIGN §3.5). Only SET, SEARCH,
    /// and relation conditions may reference it; SET copies it UNCHANGED (no PICTURE store, §14.9.39 GR2b).</summary>
    Index,
    /// <summary>USAGE OBJECT REFERENCE (ISO §13.18.60.4 / §8.5.2.14) — LIVE (the Phase-3 OO spine): a .NET
    /// reference field (typed or universal — <see cref="PicInfo.ObjectRef"/>); zero character positions.</summary>
    ObjectReference,

    // ── The post-'85 §13.18.60 usage inventory — every member is LIVE (each keyword's ConstructRegistry
    // introduction gate fires COBOLNET0900 below its edition, silent at/above; PictureAnalyzer.ParseUsage maps
    // the full grammar inventory explicitly). The former W2 usage-skeleton scaffolding — the sentinel shapes,
    // IsUnimplementedSkeleton, and the throw guards on the storage-mapping switches — is DELETED (P5.11c):
    // nothing had constructed a skeleton usage since the 14g.1 introduction-gate migration. ──

    /// <summary>USAGE NATIONAL (ISO §13.18.60.3 SR12/SR13/SR20 / §8.5.2.10) — LIVE (Phase 4a track (a)):
    /// the usage of every category-national item (implied by PIC N without a USAGE clause, SR13a); stored as a
    /// .NET string, one UTF-16 char per national position (D-N1). The SR12 national-form NUMERIC/boolean legs
    /// (PIC 9/PIC 1 USAGE NATIONAL) are recognized-but-staged (0899).</summary>
    National,
    /// <summary>USAGE BIT (ISO §13.18.60.3 SR5 / §8.5.2.5 category boolean) — LIVE (Phase 4a track (a)):
    /// maps to the SAME one-'0'/'1'-character-per-position string storage as a display-form boolean item (the
    /// §13.18.40.4 GR14 R14 representation license, D-B1); the member stays distinct for declaration fidelity
    /// (SR5 checking) and a future opt-in packed representation.</summary>
    Bit,
    /// <summary>USAGE POINTER (ISO §13.18.60 / §8.5.2.6 data-pointer) — LIVE (Phase-4b increment 1): the
    /// ManagedPointer carrier (<see cref="PicCategory.Pointer"/>).</summary>
    Pointer,
    /// <summary>USAGE PROGRAM-POINTER (ISO §13.18.60 GR24 / §8.5.2.15) — LIVE (P10 Step 7): the ProgramPointer
    /// carrier (<see cref="PicCategory.ProgramPointer"/>); the restricted TO-prototype form (GR25) stages loud.</summary>
    ProgramPointer,
    /// <summary>USAGE FUNCTION-POINTER (ISO §13.18.60) — recognized, STAGED LOUD (P10 Step 7): function
    /// prototypes are the P13 repository work; ParseUsage rejects with the named 0899-band descriptor and the
    /// member exists so the 2014 introduction gate (UsageConstructId) still fires below 2014.</summary>
    FunctionPointer,
    /// <summary>USAGE MESSAGE-TAG (ISO §13.18.60.4 GR9; COBOL-2023, Annex E.2 item 25) — DECLINED
    /// NON-SUPPORT: the data-item half of the asynchronous messaging facility (Annex A.3 item 4,
    /// docs/CONFORMANCE.md §4 item 1), refused BY NAME at every edition with COBOLNET1943, the
    /// FLOAT-BINARY-128 posture. The member exists for exactly the reason that one does: <c>ParseUsage</c> must
    /// return SOMETHING, and the entry is PICTURE-less (§13.16.3 SR8 exempts message-tag), so without a member
    /// of its own the recovery Pic could not be synthesized and the errored compile would carry a null
    /// <c>Pic</c> into the emitter — which is precisely the unhandled NullReferenceException kb/Work PB487
    /// found. It also makes the §13.18.60.3 SR4 / SR14 / SR21 message-tag arms EXPRESSIBLE where they were
    /// forward obligations held open by a comment.</summary>
    MessageTag,
    /// <summary>USAGE FLOAT-SHORT (ISO §13.18.60; the §13.18.59 D16 split) — LIVE (Phase 6a): the implementor-
    /// defined float trio maps FLOAT-SHORT → <c>float</c> (§13.18.60.4 GR13; <see cref="PicInfo.IsFloat"/> /
    /// <see cref="PicInfo.IsSingle"/>).</summary>
    FloatShort,
    /// <summary>USAGE FLOAT-LONG (ISO §13.18.60) — LIVE (Phase 6a): maps to <c>double</c>.</summary>
    FloatLong,
    /// <summary>USAGE FLOAT-EXTENDED (ISO §13.18.60) — LIVE (Phase 6a): maps to <c>double</c> (no .NET quad;
    /// the §13.18.60.4 GR13 subset nesting).</summary>
    FloatExtended,
    /// <summary>USAGE FLOAT-BINARY-32 (ISO §13.18.60.4 GR14 — ISO/IEC 60559:2020 binary32; COBOL-2014) — LIVE
    /// (P12 wave 3): maps EXACTLY to .NET <c>float</c> (the pinned IEEE interchange format is conforming).</summary>
    FloatBinary32,
    /// <summary>USAGE FLOAT-BINARY-64 (ISO §13.18.60.4 GR15 — binary64; COBOL-2014) — LIVE (P12 wave 3): maps
    /// EXACTLY to .NET <c>double</c> (conforming).</summary>
    FloatBinary64,
    /// <summary>USAGE FLOAT-BINARY-128 (ISO §13.18.60.4 GR16 — binary128; COBOL-2014) — PROCESSOR-DEPENDENT
    /// NON-SUPPORT (Annex A.3 item 17): .NET has no IEEE binary128 type, and GR16 PINS the format (backing it by
    /// <c>double</c> would be non-conforming), so ParseUsage rejects it LOUD (COBOLNET1564). The member exists so
    /// the 2014 introduction gate still fires below 2014.</summary>
    FloatBinary128,
    /// <summary>USAGE FLOAT-DECIMAL-16 (ISO §13.18.60.4 GR17 — ISO/IEC 60559:2020 decimal64; COBOL-2014) —
    /// PROCESSOR-DEPENDENT NON-SUPPORT (Annex A.3 item 19): .NET has no IEEE decimal64 type (System.Decimal is a
    /// different format), and GR17 PINS it, so ParseUsage rejects it LOUD (COBOLNET1564).</summary>
    FloatDecimal16,
    /// <summary>USAGE FLOAT-DECIMAL-34 (ISO §13.18.60.4 GR18 — decimal128; COBOL-2014) — PROCESSOR-DEPENDENT
    /// NON-SUPPORT (Annex A.3 item 19): rejected LOUD (COBOLNET1564), the FLOAT-DECIMAL-16 twin.</summary>
    FloatDecimal34,
    /// <summary>USAGE BINARY-CHAR [SIGNED|UNSIGNED] (ISO §13.18.60.4 GR12) — LIVE (Phase 4 M2-DATA-1): a
    /// PICTURE-less native 1-byte two's-complement integer (SIGNED −128..127, UNSIGNED 0..255), realized on the
    /// COMP-5 BinaryCapacity discipline (<see cref="PicInfo.BinaryItem"/>).</summary>
    BinaryChar,
    /// <summary>USAGE BINARY-SHORT (ISO §13.18.60.4 GR12) — LIVE (Phase 4): a native 2-byte integer
    /// (SIGNED −32768..32767, UNSIGNED 0..65535).</summary>
    BinaryShort,
    /// <summary>USAGE BINARY-LONG (ISO §13.18.60.4 GR12) — LIVE (Phase 4): a native 4-byte integer
    /// (SIGNED −2^31..2^31−1, UNSIGNED 0..2^32−1).</summary>
    BinaryLong,
    /// <summary>USAGE BINARY-DOUBLE (ISO §13.18.60.4 GR12) — LIVE (Phase 4): a native 8-byte integer
    /// (SIGNED −2^63..2^63−1, UNSIGNED 0..2^64−1; stored as <see cref="Int128"/>).</summary>
    BinaryDouble,
}

/// <summary>The two usage FAMILIES the standard names as terms and then legislates over by name — ISO §3.166
/// "standard binary floating-point usages: usages float-binary-32, float-binary-64, and float-binary-128" and
/// §3.167 "standard decimal floating-point usages: usages float-decimal-16 and float-decimal-34". Written down
/// ONCE: at least four rules key on these exact sets (§13.18.60.4 GR19c/GR19d/GR20, §11.9.8.3 SR1-SR3, §11.9.9.3
/// SR1-SR6, §15.x's standard-decimal-arithmetic argument rules), and the alternative is the same or-chain spelled
/// out at each of them — where the NEXT member of a family (a future float-binary-256, say) would be added to some
/// and missed at others. kb/Work PB174.</summary>
public static class UsageFamilies
{
    /// <summary>ISO §3.166 — the standard BINARY floating-point usages.</summary>
    public static bool IsStandardBinaryFloat(Usage u) =>
        u is Usage.FloatBinary32 or Usage.FloatBinary64 or Usage.FloatBinary128;

    /// <summary>ISO §3.167 — the standard DECIMAL floating-point usages.</summary>
    public static bool IsStandardDecimalFloat(Usage u) => u is Usage.FloatDecimal16 or Usage.FloatDecimal34;

    /// <summary>The union — every usage the §13.18.60.2 general format prints an endianness-phrase on (and the
    /// only usages §13.18.60.4 GR19c/d give an implied phrase). The implementor-defined float usages
    /// (COMP-1/COMP-2/FLOAT-SHORT/-LONG/-EXTENDED) are deliberately OUTSIDE it: GR13/GR21 leave their
    /// representation to the implementor, and WiseOwl COBOL pins them big-endian (Annex A.1 item 48).</summary>
    public static bool IsStandardFloat(Usage u) => IsStandardBinaryFloat(u) || IsStandardDecimalFloat(u);

    /// <summary>⛔ THE PICTURE-LESS USAGES — ISO §13.16.3 SR8's named set, written down ONCE. "The PICTURE
    /// clause shall not be specified for the subject of a RENAMES clause or for an item whose usage is
    /// binary-char, binary-short, binary-long, binary-double, float-short, float-long, float-extended, index,
    /// message-tag, object reference, pointer, function-pointer, or program-pointer. For any other entry
    /// describing an elementary item, a PICTURE clause shall be specified except as indicated in Syntax rule 9."
    /// So the predicate is a BICONDITIONAL and both directions are load-bearing: true ⇒ a written PICTURE is a
    /// violation and the usage alone fixes the representation; false ⇒ a PICTURE is REQUIRED (SR9's
    /// VALUE-implied picture aside), and an elementary item without one is nonconforming, not a zero-length cell.
    ///
    /// <para>⛔ ONE table, THREE readers — <c>DataBinder.BindEntry</c> screens an entry's OWN clause against it,
    /// <c>DataBinder.UsageInheritancePass</c> screens the usage an elementary item acquires by §13.18.60.4 GR1
    /// group inheritance, and the drift test <c>UsageInheritanceTests</c> enumerates <see cref="Usage"/> against
    /// it. Before kb/Work PB495 the same fact was written down twice as two disagreeing hand-lists (the shed list
    /// in <c>ResolveIndexItems</c> and the inherited set in <c>InheritUsageClauses</c>), and a usage in neither
    /// produced a ZERO-LENGTH elementary item with no diagnostic.</para>
    ///
    /// <para><b>DETERMINATION — the five COBOL-2014 standard float usages.</b> SR8's list names float-short,
    /// float-long and float-extended but NOT float-binary-32/-64/-128 or float-decimal-16/-34, which §13.18.60.2
    /// acquired later; read literally, SR8's second sentence would then REQUIRE a picture character-string on a
    /// binary32 item. No picture character-string can describe one: §13.18.60.4 GR14/GR15/GR16/GR17/GR18 PIN each
    /// of those usages to a named ISO/IEC 60559:2020 interchange format, and §13.18.40 has no form that denotes
    /// one. They are therefore treated as picture-less, which is also what the compiler has always done on the
    /// written-clause path (COBOLNET1521). COMP-1 / COMP-2 need no determination — they are this implementation's
    /// spellings of float-short / float-long (docs/CONFORMANCE.md item 22), which SR8 names.</para></summary>
    public static bool IsPictureless(Usage u) => u
        // ── §13.16.3 SR8's list, verbatim ──
        is Usage.BinaryChar or Usage.BinaryShort or Usage.BinaryLong or Usage.BinaryDouble
        or Usage.FloatShort or Usage.FloatLong or Usage.FloatExtended
        or Usage.Index or Usage.ObjectReference
        or Usage.Pointer or Usage.FunctionPointer or Usage.ProgramPointer
        // message-tag is SR8's thirteenth entry. The usage itself is DECLINED non-support (COBOLNET1943, Annex
        // A.3 item 4) and refused by name in ParseUsage, so this arm is reached only on the errored-recovery
        // path — but SR8 names it either way, and leaving it out is what
        // PicturelessUsageSetDriftTests.EverySr8NamedUsage_WithAMember_IsPictureless caught the moment kb/Work
        // PB487 gave message-tag a Usage member.
        or Usage.MessageTag
        // ── COMP-1 / COMP-2: the implementor spellings of SR8's float-short / float-long ──
        or Usage.Float or Usage.Double
        // ── the DETERMINATION above: the standard float usages SR8's list predates ──
        || IsStandardFloat(u);

    /// <summary>⛔ THE ONE SET of usages whose subject admits NO VALUE clause at all — ISO §13.16.3 SR10's four
    /// CLASSES, spelled as the usages §8.5.2.1 Table 2 files under them (kb/Work PB557, PB515).
    ///
    /// <para><b>The rule is class-keyed, and it is the data description entry's.</b> §13.16.3 SR10: "The VALUE
    /// clause shall not be specified for data items of class index, message-tag, object, or pointer." Table 2
    /// puts category index in class index (USAGE INDEX), category message-tag in class message-tag, category
    /// object-reference in class object (USAGE OBJECT REFERENCE), and categories data-pointer, function-pointer
    /// and program-pointer in class pointer (USAGE POINTER / FUNCTION-POINTER / PROGRAM-POINTER). So six usages,
    /// one rule. <c>ValueClauseUsageSetDriftTests</c> re-derives the set from SR10 and Table 2 on every run.</para>
    ///
    /// <para>§13.18.63.3 SR9 is the VALUE clause's own restatement of four of them: "The VALUE clause shall not be
    /// specified if a USAGE clause with a phrase of FUNCTION-POINTER, MESSAGE-TAG, OBJECT-REFERENCE, or
    /// PROGRAM-POINTER is also specified." It names the usages a programmer WRITES; SR10 names the classes, and
    /// is the one that reaches USAGE INDEX and the plain data-pointer. ⛔ USAGE INDEX was the arm this set was
    /// missing while it was written from SR9 alone (kb/Work PB515): `77 I USAGE INDEX VALUE 7.` compiled clean
    /// at every edition and the value was LIVE — `SET N TO I` yielded 007, so illegal source silently acquired a
    /// defined initial occurrence number. (No VALUE NULL escape exists for any of them: §8.4.3.10.1 makes NULL "a
    /// predefined address of class pointer or a predefined content of class message-tag", an identifier under
    /// §8.4.3 and not a literal.)</para>
    ///
    /// <para>§13.18.63.4 GR4 is what makes the prohibition harmless rather than a loss of expressiveness: "When
    /// VALUE clauses take effect, data items with a VALUE clause are initialized to the specified value and data
    /// items of class message-tag, class object, and class pointer are initialized to null." The null initial
    /// value is the standard's, with no clause written.</para>
    ///
    /// <para>MESSAGE-TAG's arm IS reached, and reports BESIDE the facility decline rather than instead of it —
    /// MEASURED: `01 M USAGE MESSAGE-TAG VALUE "X".` draws COBOLNET1943 (this implementation does not provide
    /// the Annex A.3 item 4 facility) and COBOLNET2168 (the standard forbids the clause on that usage at all).
    /// The two say different things about the same line and both are true, which is why the decline does not
    /// stand in for the rule. The FUNCTION-POINTER arm is the one that cannot be exercised today: such an entry
    /// needs a prototype the repository work has not landed. A rule written only over its reachable arms is how
    /// the other two went missing for the life of the tree.</para></summary>
    public static bool AdmitsNoValueLiteral(Usage u) => u
        // —— §13.16.3 SR10, class by class through §8.5.2.1 Table 2 ——
        is Usage.Index                                                          // class index
        or Usage.MessageTag                                                     // class message-tag
        or Usage.ObjectReference                                                // class object
        or Usage.Pointer or Usage.FunctionPointer or Usage.ProgramPointer;      // class pointer

    /// <summary>The ISO rule a <see cref="AdmitsNoValueLiteral"/> refusal cites for <paramref name="u"/> — the
    /// VALUE clause's own §13.18.63.3 SR9 where it names the usage, else the data description entry's
    /// §13.16.3 SR10, which names the class. One sentence for both sites that screen the rule (the entry's own
    /// USAGE clause in <c>BindEntry</c>, and the usage an elementary item ACQUIRES from its group under
    /// §13.18.60.4 GR1 in <c>UsageInheritancePass</c>), so the two spellings of one item cannot cite differently.</summary>
    public static string NoValueClauseRule(Usage u) => u switch
    {
        Usage.FunctionPointer or Usage.MessageTag or Usage.ObjectReference or Usage.ProgramPointer
            => "ISO §13.18.63.3 SR9; §13.16.3 SR10",
        Usage.Index => "a data item of class index — ISO §13.16.3 SR10",
        _ => "a data item of class pointer — ISO §13.16.3 SR10",
    };

    /// <summary>The §13.18.60 USAGE keyword for a usage, for the §13.18.63.3 SR14 diagnostic text — always a
    /// spelling the programmer could have WRITTEN.
    ///
    /// <para>⛔ DERIVED, not enumerated. <see cref="Usage"/> has 24 members and the enum name hyphenated on its
    /// case/digit boundaries IS the §13.18.60 keyword for all but five of them, so the default arm covers every
    /// member a future usage adds. The bare <c>ToString().ToUpperInvariant()</c> this replaced rendered them as
    /// 'PROGRAMPOINTER', 'FLOATBINARY32' and 'BINARYCHAR' — words no COBOL program contains. The five whose enum
    /// name is not the COBOL word carry an explicit spelling, and <c>UsageWordDriftTests</c> asserts every
    /// member's rendering is made of RESERVED COBOL words, so "automatic" stays true.</para></summary>
    public static string UsageWord(Usage usage) => usage switch
    {
        Usage.Binary => "BINARY",                   // COMP / COMPUTATIONAL are the §13.18.60.2 synonyms
        Usage.Packed => "PACKED-DECIMAL",           // COMP-3 / COMPUTATIONAL-3 are the dialect synonyms
        Usage.Comp5 => "COMPUTATIONAL-5",           // no §13.18.60.2 spelling — the dialect word IS the name
        Usage.Float => "FLOAT-SHORT",               // §13.18.60.2's word; COMP-1 / COMPUTATIONAL-1 the dialect one
        Usage.Double => "FLOAT-LONG",               // COMP-2 / COMPUTATIONAL-2
        Usage.ObjectReference => "OBJECT REFERENCE",   // TWO words in the general format, not one hyphenated
        _ => HyphenateEnumName(usage.ToString()),
    };

    /// <summary>PascalCase (with trailing digit groups) → the COBOL hyphenated word: a hyphen before each
    /// interior capital and before each digit run that follows a non-digit. <c>FloatBinary32</c> →
    /// <c>FLOAT-BINARY-32</c>, <c>BinaryChar</c> → <c>BINARY-CHAR</c>, <c>Display</c> → <c>DISPLAY</c>.</summary>
    private static string HyphenateEnumName(string name)
    {
        var sb = new System.Text.StringBuilder(name.Length + 4);
        for (int i = 0; i < name.Length; i++)
        {
            if (i > 0 && (char.IsUpper(name[i]) || (char.IsDigit(name[i]) && !char.IsDigit(name[i - 1]))))
                sb.Append('-');
            sb.Append(char.ToUpperInvariant(name[i]));
        }
        return sb.ToString();
    }
}

/// <summary>
/// The analyzed PICTURE + USAGE of an elementary item: its category, the numeric profile (digit count, decimal
/// scale, sign) and the .NET type WiseOwl COBOL represents it with. This is pure spec analysis — no byte storage.
/// </summary>
/// <remarks>
/// <para><b>Numeric profile.</b> <see cref="Digits"/> is the count of <c>9</c> positions, <see cref="Scale"/> the
/// number of them after the implied decimal point (<c>V</c>), <see cref="Signed"/> whether an <c>S</c> is present.
/// These drive every <c>CobolNum</c> operation (PIC truncation / ROUNDED / SIZE ERROR).</para>
/// <para>Slice scope: <c>X/A/9/S/V</c> with <c>(n)</c> repetition, and the common usages. Editing symbols and
/// <c>P</c> scaling are recognized into <see cref="PicCategory.NumericEdited"/> / profile but full formatting is a
/// later slice.</para>
/// </remarks>
public sealed record PicInfo(
    PicCategory Category,
    Usage Usage,
    int Length,
    int Digits,
    int Scale,
    bool Signed)
{
    /// <summary>
    /// The runtime <c>NumericSign</c> member name describing how a signed item presents its sign in its DISPLAY
    /// image (only meaningful when <see cref="Signed"/>): over-punch for USAGE DISPLAY (trailing by default, leading
    /// under SIGN LEADING), a separate <c>+</c>/<c>-</c> under SIGN SEPARATE, or a binary leading minus for
    /// COMP/COMP-3/COMP-5. Emitted verbatim into the item's <c>NumProfile</c> (COBOLNET_DESIGN §6.4).
    /// </summary>
    public string SignKind { get; init; } = "TrailingOverpunch";

    /// <summary>This profile is <see cref="Recovery"/>'s — a placeholder for an entry whose PICTURE has ALREADY
    /// been rejected, kept only so the doomed emit stays crash-free (the DEVLOG-597 discipline). It is not an
    /// analysis of anything, so a LATER screen must not read it as one: the §13.18.60.4 GR1 inheritance pass
    /// skips its usage × picture screen on a recovery profile, because the written-clause path never reached
    /// that screen either (<c>PictureAnalyzer.Analyze</c> returns Recovery before it) and the two arms of one
    /// rule must agree. kb/Work PB495.</summary>
    public bool IsRecovery { get; init; }

    /// <summary>⛔ THE CATEGORY AS AN ANALYSIS OF THE WRITTEN SOURCE — <see langword="null"/> for a
    /// <see cref="IsRecovery"/> profile, whose <see cref="Category"/> is a STORAGE placeholder that only keeps the
    /// doomed emit crash-free (kb/Work PB960). <see cref="Category"/> answers "what shape does the carrier have";
    /// this answers "what category did the program declare", and for an entry whose declaration was already
    /// rejected the honest answer is <i>none known</i>. Every §8.5.2.1 class/category READER that a syntax-rule
    /// screen consults (<c>IntrinsicArgumentRules.ClassOfItem</c>, <c>IntrinsicResultType.OperandCategory</c>,
    /// <c>ExpressionBinder.ScreenResultant</c>) reads THIS, so the nullable type forces it to handle the recovery
    /// case, and each handles it by their existing contract — "not statically decidable" fails OPEN. Reading
    /// <see cref="Category"/> there turned <c>PIC 9V9V9</c> into "item 'W-BAD' of category alphanumeric is not
    /// a numeric operand" beside the real COBOLNET1934, at every operand position the item appeared in.</summary>
    public PicCategory? AnalyzedCategory => IsRecovery ? null : Category;

    /// <summary>The <c>type-name-1</c> of a RESTRICTED data-pointer — <c>USAGE POINTER TO type-name-1</c>
    /// (ISO §13.18.60.2 general format; §13.18.60.4 GR23). Null for an ordinary data-pointer and for every
    /// non-pointer item. It is the guarantee Annex D.9.2.2 describes: "a restricted data-pointer … shall contain
    /// only the predefined address NULL or the address of a data item of the specified type", enforced by the
    /// ALLOCATE / SET ADDRESS OF / CALL screens (§14.9.3.3 SR4/SR5, §14.9.39.3 SR19, §14.8.2.3.2).
    /// <para>The OTHER source of a restriction carries no PicInfo at all: Annex D.9.2.2 item 2 and its normative
    /// §8.4.3.11.4 GR2 make <c>ADDRESS OF identifier-1</c> a restricted data-pointer whenever identifier-1 is a
    /// strongly-typed group item or another restricted data-pointer — see
    /// <c>StrongTypeModel.AddressOfRestriction</c>. kb/Work PB153.</para></summary>
    public string? RestrictedTypeName { get; init; }

    /// <summary>The TYPE DECLARATION <see cref="RestrictedTypeName"/> resolves to — the <c>DataBinder.TypeDecls</c>
    /// entry, attached post-build by <c>DataBinder.ResolveRestrictedTypes</c> (inside the ONE <c>ExpandTypes</c>
    /// pass, so the template's own nested TYPE references are already expanded). Null when the item carries no
    /// restriction, and when the name resolves to no type declaration in this source element.
    /// <para>It exists because §8.5.3.1's "same type" is declaration EQUIVALENCE, of which the type-name is one
    /// conjunct: within a source element §13.18.58 makes the type-name unique, so the name alone decides, but
    /// two source elements may declare non-equivalent types under one name and a restriction compared by name
    /// only would admit the crossing the restriction exists to forbid (kb/Work PB427). Read ONLY through
    /// <c>StrongTypeModel.PointerRestriction</c> / <c>SameRestriction</c>; deliberately excluded from the
    /// same-type profile compare, which already compares <see cref="RestrictedTypeName"/> and would not
    /// terminate on a type whose pointer member is restricted back to it.</para></summary>
    public DataItem? RestrictedTypeDecl { get; init; }

    /// <summary>The <c>{function|program}-prototype-name-1</c> of the RESTRICTED prototype-pointer forms —
    /// <c>USAGE FUNCTION-POINTER TO function-prototype-name-1</c> (§13.18.60.4 GR26) and <c>USAGE PROGRAM-POINTER
    /// TO program-prototype-name-1</c> (GR25). ⛔ ONE FIELD FOR BOTH CARRIERS, because the two general rules are
    /// ONE rule written twice and so are the two syntax rules that consume it — §14.9.39.3 SR20 ("The
    /// function-prototypes associated with identifier-12 and identifier-13 shall have the same signature") and
    /// SR22 ("the program-prototypes associated with identifier-7 and identifier-8 shall have the same
    /// signature"). kb/Work PB817 filed exactly that: "SR20 and SR22 are ONE rule (a same-signature test over the
    /// associated prototypes) over TWO carriers; write it once." The CATEGORY says which namespace the name is
    /// resolved in (a function-prototype-name per §8.4.6.6, a program-prototype-name per §8.4.6.8); the RULE over
    /// it is the same compare either way (<c>PrototypeSignatures.Same</c>).
    /// <para>Distinct from <see cref="RestrictedTypeName"/> on purpose: that one names a TYPE (§13.18.60.4 GR23,
    /// whose declaration-shape rule is SR18's TYPEDEF requirement), not a prototype, and its restriction is a
    /// type-identity test rather than a signature test. Null for an unrestricted program-pointer and for every
    /// non-pointer item; NEVER null on <see cref="PicCategory.FunctionPointer"/>, whose TO phrase is unbracketed
    /// in the general format.</para></summary>
    public string? RestrictedPrototypeName { get; init; }

    /// <summary>USAGE PACKED-DECIMAL WITH NO SIGN (ISO/IEC 1989:2023 §13.18.60.4 GR11, a 2023 addition): the item
    /// reserves NO trailing sign nibble, so its storage is exactly the digit nibbles — <c>ceil(Digits/2)</c> bytes
    /// (vs a plain packed item's <c>Digits/2+1</c>). The VALUE semantics are identical to an unsigned (S-less)
    /// packed item ("always considered to have a zero, or positive value"); SR31 forbids an <c>S</c> in the
    /// picture. Only <see cref="StorageWidth"/> / byte-length / physical layout differ — the store/truncation path
    /// is unchanged. Meaningful only when <see cref="Usage"/> is <see cref="Usage.Packed"/>.</summary>
    public bool PackedNoSign { get; init; }

    /// <summary>The effective FLOAT-BINARY endianness of a STANDARD binary floating-point item (USAGE
    /// FLOAT-BINARY-32/-64): <c>true</c> = HIGH-ORDER-RIGHT, the item's IEEE interchange bytes are
    /// LITTLE-endian (ISO §13.18.60.4 GR19b). Derived ONCE, in <see cref="FloatItem"/>, from a TWO-LEVEL source
    /// the standard itself orders: the ITEM's own endianness-phrase on its USAGE clause (§13.18.60.2 general
    /// format) where it wrote one, else the unit's effective OPTIONS FLOAT-BINARY clause (§11.9.8), whose phrase
    /// is implied only "for … any data item described with a standard binary floating-point usage in which an
    /// endianness-phrase is not specified" (§11.9.8.3 SR2/SR3 — so the per-item phrase wins by the implying
    /// clause's own wording, not by preference); §11.9.8.3 SR1 makes the no-clause default the implementor's
    /// documented choice, ours HIGH-ORDER-LEFT / big-endian, Annex A.1 item 48. <c>DataBinder</c> performs that
    /// merge at the single call site and screens the phrase's applicability (COBOLNET1716/1706/1707). Always
    /// <c>false</c> for the implementor-defined float usages (COMP-1/COMP-2/FLOAT-SHORT/-LONG/-EXTENDED,
    /// GR13/GR21 — pinned big-endian; GR19c scopes the clause AND the phrase to the standard usages). Rides
    /// <see cref="ProfileInitializer"/> into the runtime's <c>NumProfile.FloatLittleEndian</c>, the one switch the
    /// IEEE image lanes read — so §14.9.25.4 GR6 item 2c ("When the usage specifications of the receiving data
    /// item differ from those of the sending operand only in endianness specifications, the data … is transferred
    /// … in the endianness of the receiving operand, but without any other change") is satisfied structurally by
    /// the typed-native model: the value lives as a native float and only its IMAGE is byte-ordered, so the MOVE
    /// is a value copy that lands in the receiver's own order for free.</summary>
    public bool FloatLittleEndian { get; init; }

    /// <summary>The FLOATING-POINT form of a numeric-edited PICTURE (ISO §13.18.40.4 GR13 b — a significand and an
    /// exponent separated by the symbol <c>E</c>; data-model design D21, kb/Work PB66): the item's category is still
    /// numeric-edited (§8.5.2.13) and its <see cref="EditMask"/> is the whole expanded string, but its VALUE is a
    /// significand with its own power of ten (the exact <c>CobolDec</c> lane), it has no fixed <see cref="Scale"/>
    /// (0 here) and no <see cref="DigitPositions"/> (0), and every edited store / read dispatches on THIS flag inside
    /// the runtime entry helpers (<c>RuntimeApi.EditFormat</c> / <c>EditTryFormat</c> / <c>NumericRenderer.FieldNum</c>)
    /// — never at a call site. The mask's structure is <c>CobolEdit.FloatMask.Parse(EditMask, commaMode)</c>, the ONE
    /// parser (the analyzer validated the form; consumers read it through the runtime type).</summary>
    public bool IsFloatEdited { get; init; }

    /// <summary>PICTURE format 2 — the LOCALE phrase (ISO §13.18.40.2; kb/Work PB64 T6): the item is fixed-point
    /// numeric-edited (§13.18.40.4 GR16) whose length is the SIZE phrase's integer-1 (GR17 — <see cref="Length"/>
    /// carries it; the picture contributes only the digit shape), whose editing/de-editing is the named-else-current
    /// locale's LC_MONETARY at each use (§13.18.40.5 r9–r15 — <c>CobolLocaleEdit</c>), and whose
    /// <see cref="EditMask"/> is NULL (there is no fixed mask — the currency string, separators and sign are
    /// runtime locale data). ⛔ Like <see cref="IsFloatEdited"/>, every edited store / read dispatches on THIS
    /// property inside the runtime entry helpers (<c>RuntimeApi.EditFormatFor</c> / <c>EditTryFormatFor</c> /
    /// <c>NumericRenderer.FieldNum</c>) — never at a call site.</summary>
    public LocaleEditSpec? LocaleEdit { get; init; }

    /// <summary>The EXPANDED edited picture (repeats unrolled, uppercased, the implied point <c>V</c> retained,
    /// and the mask's currency symbol CANONICALIZED to <c>$</c> — see <see cref="CurrencyString"/>) — the mask
    /// <c>CobolEdit.Format</c> (numeric-edited) or <c>CobolEdit.FormatSimpleInsertion</c> (the character-edited
    /// categories) renders into.
    /// <para>⛔ IT IS ALSO WHAT MAKES AN ITEM'S CATEGORY THE **EDITED** ONE, because <see cref="PicCategory"/>
    /// carries neither edited character category as a member: an alphanumeric-edited item is
    /// <c>{ Category: Alphanumeric, EditMask: not null }</c> (§8.5.2.4/§13.18.40.4 GR7) and a national-edited item
    /// is <c>{ Category: National, EditMask: not null }</c> (§8.5.2.11/GR10, kb/Work PB492) — the ONE pair every
    /// screen that needs the finer §8.5.2 category matches on. §8.5.2.1 Table 2 is why that is sound: the edited
    /// category and its plain one share a CLASS, and only the rules worded in CATEGORIES need to tell them
    /// apart.</para>
    /// <para>Null on a plain alphanumeric / national / numeric item — ⛔ AND on a format-2 (locale)
    /// NumericEdited item (<see cref="LocaleEdit"/>): since PB64 T6 a NumericEdited category no longer guarantees
    /// a mask, so a <c>{ Category: NumericEdited, EditMask: { } m }</c> pattern must carry a LocaleEdit arm beside
    /// it and a <c>pic.EditMask!</c> deref is reachable-null.</para></summary>
    public string? EditMask { get; init; }

    /// <summary>For a numeric-edited item whose PICTURE uses a currency symbol: the currency STRING that symbol
    /// stands for (ISO §12.3.7.4 GR13 — the unit's CURRENCY SIGN SET, <c>DataBinder.CurrencySigns</c>), when it is
    /// not the single character <c>$</c>. <c>PictureAnalyzer</c> canonicalizes the mask's symbol to <c>$</c> and
    /// records the string here, so every edit/de-edit call site passes ONE per-item argument
    /// (<c>currencyString:</c>) and <c>CobolEdit</c> expands the rendered currency position into it — a
    /// multi-character string widens the item by its extra length (§13.18.40.4 GR14, already in
    /// <see cref="Length"/>). Null when the picture has no currency symbol, or its string is <c>$</c>.</summary>
    public string? CurrencyString { get; init; }

    /// <summary>⛔ THE CLASS OF THE CURRENCY LITERAL BEHIND THE PICTURE'S CURRENCY SYMBOL (ISO §12.3.7.3 SR28; kb/Work
    /// PB1089): <see cref="LiteralClass.Alphanumeric"/> or <see cref="LiteralClass.National"/> — the class of
    /// literal-7 of the CURRENCY SIGN clause that defined the symbol (the implied clause of SR25 is alphanumeric) —
    /// or <see langword="null"/> when the picture uses no currency symbol. SR28 says "If literal-7 is of class
    /// alphanumeric, the associated currency symbol may be used only to define a numeric-edited item with usage
    /// display. If literal-7 is of class national, … only … with usage national", so the screen that asks it needs
    /// the class at the moment the item's usage is final, which for an item whose usage comes from its group
    /// (§13.18.60.4 GR1) is a later pass than the analyzer — it rides the profile exactly as
    /// <see cref="CurrencyString"/> does. Unlike that property it is NOT elided for <c>$</c>: the implied
    /// <c>$</c> has a class too.</summary>
    public LiteralClass? CurrencyClass { get; init; }

    /// <summary>The identity of the PICTURE clause this profile was analyzed from — the expanded character-string
    /// with its currency STRING and, when it has a period or comma symbol, the declaring source element's
    /// DECIMAL-POINT IS COMMA state (<see cref="PictureClauseIdentity"/>; kb/Work PB1166). Set by
    /// <c>PictureAnalyzer.Analyze</c> only; <see langword="null"/> for an item with no PICTURE clause (a pointer,
    /// an index, a PICTURE-less usage such as BINARY-LONG, a synthesized RENAMES/filler view) and for a
    /// <see cref="IsRecovery"/> profile. The one input every "the same PICTURE clause" rule reads.</summary>
    public PictureClauseIdentity? Clause { get; init; }

    /// <summary>⛔ THE DECIMAL-POINT MODE BELONGS TO THE ITEM, NOT TO THE UNIT THAT HAPPENS TO EMIT IT
    /// (kb/Work PB2554). ISO §12.3.7.4 GR14 b) makes the comma the decimal separator in a PICTURE character-string
    /// and in an edited item "for the basic format of the PICTURE clause" — a property of the source element
    /// that DESCRIBES the item (§12.3.4 GR1 hands it to a CONTAINED unit only), so a separately compiled
    /// class under DECIMAL-POINT IS COMMA keeps it for its formals no matter which unit INVOKEs them. Every editing,
    /// de-editing, receiver-scale and edited-image consumer reads THIS, never <c>ctx.Data.DecimalPointIsComma</c>
    /// (<c>DecimalPointModeDriftTests</c>). Read off <see cref="Clause"/>, the one record of the declaring unit's
    /// state: it is <see langword="false"/> for a profile with no analyzed PICTURE and for one with neither a
    /// period nor a comma symbol, where the mode cannot change a single edited character.</summary>
    public bool DecimalPointIsComma => Clause?.DecimalPointIsComma ?? false;

    /// <summary>For a numeric-edited (or alphanumeric-edited) item carrying one or more PICTURE EDITING phrases
    /// (ISO §13.18.40.2 Format 1, COBOL-2023): the resolved single-character render rules keyed on character-1's
    /// position in <see cref="EditMask"/> — the simple-insertion (IS) form and the single-occurrence sign-control
    /// (FOR) form. Threaded to <c>CobolEdit.Format</c>/<c>DeEdit</c>. Null on every non-editing item AND on the
    /// render-staged forms (multi-character literals / floating character-1) which <c>PictureAnalyzer</c> rejects
    /// loud (COBOLNET0899) as a documented P14 render GAP — so a non-null value always renders 1:1.</summary>
    public IReadOnlyList<CobolNet.Runtime.CobolEdit.EditRule>? EditingRules { get; init; }

    /// <summary>⛔ THE ONE "is this an EDITED CHARACTER item?" predicate — category alphanumeric-edited
    /// (ISO §8.5.2.4 / §13.18.40.4 GR7) or category national-edited (§8.5.2.11 / GR10), the two categories
    /// §13.18.40.5 Table 7 gives "Simple insertion" and nothing else. Read by every site that treats the two
    /// ALIKE — the MOVE / ACCEPT / STRING receiver arms, and the two whole-width-fill guards in
    /// <c>MoveEmitter.ConvertSource</c> that must route a figurative or ALL-literal source THROUGH the editor
    /// rather than around it (§14.9.25.4 GR6: "any editing specified for … the receiving data item" applies to
    /// every valid elementary move, so an insertion position keeps its own character — <c>MOVE SPACES TO
    /// PIC XX/XX</c> yields <c>  /  </c>, NIST NC223A INI-TEST-GF-1).
    /// <para>It exists because those sites each spelled the pair out and every one of them named only the
    /// ALPHANUMERIC half, so no national-edited item could be moved into, accepted into, or filled (kb/Work
    /// PB492). A site that must tell the two categories APART — Table 16's separate ROWS, INITIALIZE's
    /// category-name match, the edition gate — reads <see cref="Category"/> beside <see cref="EditMask"/>
    /// instead; this predicate is only for the rules that do not distinguish them.</para></summary>
    public bool IsCharacterEdited =>
        Category is PicCategory.Alphanumeric or PicCategory.National && EditMask is not null;

    /// <summary>True when every PICTURE position is <c>A</c> — category alphabetic (ISO §8.5.2). INITIALIZE
    /// category matching (§14.9.20 GR5c/GR6c) must distinguish alphabetic from alphanumeric receivers; both map
    /// to <see cref="PicCategory.Alphanumeric"/> storage, so the category needs this flag.</summary>
    public bool IsAlphabetic { get; init; }

    /// <summary>True for the WIDE storage tier: a fixed-point picture of 19–38 digits (legal 19–31 at COBOL-2002+,
    /// ISO §8.3.3.3.2 / the composite rules §14.7) stores as <see cref="Int128"/> — the design's graduated substrate
    /// (numeric design D1 / SSOT §18 #4). ≤18 digits stay hardware-native <see cref="long"/>.</summary>
    public bool IsWide => Category is PicCategory.Numeric && !IsFloat && Digits > 18;

    /// <summary>
    /// ⛔ THE ONE "is this the description of an INTEGER DATA ITEM?" primitive (ISO §5.5 2)b)2., kb/Work PB248).
    /// The standard's own definition of the term is a CONJUNCTION of three conditions, and every screen in this
    /// compiler that asks the question reads THIS rather than re-deriving it: "a <b>fixed-point</b> numeric data
    /// item, other than an intrinsic function, whose description does not include any digit positions to the
    /// right of the radix point".
    /// </summary>
    /// <remarks>
    /// <para>
    /// ⚠ <b>THE "FIXED-POINT" CONJUNCT IS THE ONE THAT WAS MISSING, TWICE.</b> A floating-point item
    /// (§14.6.8.3 — "a data item described with the FLOAT-SHORT usage, the FLOAT-LONG usage, the FLOAT-EXTENDED
    /// usage, or any standard floating-point usage", plus the COMP-1/COMP-2 synonyms) is PICTURE-less, so
    /// <see cref="FloatItem"/> gives it <c>Scale: 0</c> and the radix-point conjunct is VACUOUSLY satisfied.
    /// Both the §15.3 type-6 intrinsic-argument screen and <c>IntrinsicResultType.IsIntegerOperand</c> tested
    /// the scale alone; a <c>USAGE COMP-2</c> item therefore read as an integer at both, and
    /// <c>PERFORM … F TIMES</c> over 3.7 iterated three times with no diagnostic (§14.9.28.3 SR2). The two
    /// screens are complements over <see cref="Scale"/> (one asks <c>&gt; 0</c>, the other <c>&lt;= 0</c>), so a
    /// single predicate serves both and neither can grow the arm the other lacks.
    /// </para>
    /// <para>
    /// <see cref="Scale"/> ≤ 0 rather than = 0: a trailing-P picture (<c>PIC 9P</c>, scale −1) has no digit
    /// position to the right of the radix point and IS an integer item (its value is a multiple of ten); a
    /// leading-P (<c>PIC P9</c>, scale 2) is not. USAGE INDEX is excluded because §8.5.2.1 Table 2 makes an
    /// index data item class INDEX, never a numeric data item, though its storage <see cref="PicInfo"/> carries
    /// category Numeric at scale 0 and would otherwise answer true (kb/Work R27).
    /// </para>
    /// </remarks>
    public bool IsIntegerDescription => IsClassNumericFixedPoint && Scale <= 0;

    /// <summary>True for a class-numeric, fixed-point description whose scale is exactly zero — an integer with no
    /// symbol 'P' and no digit position to the right of the radix point, the shape "an elementary numeric integer data
    /// item without the symbol P" asks of a STRING / UNSTRING POINTER, COUNT IN and TALLYING item (§14.9.43.3 SR7,
    /// §14.9.48.3 SR5–SR6). ⛔ NOT the bare "integer" of a syntax rule (§13.18.38.3 SR17, §13.18.43.3 SR6): §5.5 2) b)
    /// 2. admits a trailing-P description there, and that is <see cref="IsIntegerDescription"/>.
    /// ⛔ NEVER WRITTEN AS <c>Pic is { Category: Numeric, IsFloat: false, Scale: 0 }</c>: that pattern admits USAGE INDEX,
    /// whose storage profile is category Numeric at scale 0 (see <see cref="IsClassNumericFixedPoint"/>), so an index data
    /// item was accepted as an OCCURS DEPENDING ON object and as a STRING / UNSTRING pointer or tally (kb/Work PB543).</summary>
    public bool IsUnscaledInteger => IsClassNumericFixedPoint && Scale == 0;

    /// <summary>The count of TRAILING symbol 'P' positions of a numeric PICTURE (ISO §13.18.40.4 GR14: the assumed
    /// decimal point lies to the right of the digits, so the signed <see cref="Scale"/> is NEGATIVE — <c>PIC 99P</c>
    /// is −1). Zero for any other description. <see cref="PictureAnalyzer"/> derives the signed scale from the
    /// symbols; this reads it back, ONCE, for every consumer that needs the P positions themselves
    /// (<c>OperandText.PExpand</c>'s sending image) or only their presence (<see cref="HasPScaling"/>).</summary>
    public int TrailingPScaling => Category is PicCategory.Numeric && Scale < 0 ? -Scale : 0;

    /// <summary>The count of LEADING symbol 'P' positions of a numeric PICTURE (ISO §13.18.40.4 GR14: the assumed
    /// decimal point lies to the left of the digits, so every digit is fractional and the signed
    /// <see cref="Scale"/> EXCEEDS <see cref="Digits"/> by the P count — <c>PIC PP99</c> is scale 4 over 2 digits;
    /// a <c>V</c> alone never does, its scale is at most the digit count). Zero for any other description.</summary>
    public int LeadingPScaling => Category is PicCategory.Numeric && Scale > Digits ? Scale - Digits : 0;

    /// <summary>True when the PICTURE of a numeric item contains the symbol 'P' — the predicate behind every
    /// "without the symbol 'P' in its picture character-string" syntax rule (ISO §14.9.43.3 SR8 and §14.9.48.3 SR4 for
    /// STRING/UNSTRING operands). A <c>V</c> alone is NOT a P: <c>PIC 9V9</c> has a non-zero <see cref="Scale"/> and no
    /// P positions.</summary>
    public bool HasPScaling => TrailingPScaling > 0 || LeadingPScaling > 0;

    /// <summary>True for a description whose item is of CLASS NUMERIC and FIXED-POINT (ISO §8.5.2.1 Table 2).
    /// <para>⛔ THE ONE PLACE THE USAGE INDEX TRAP IS WRITTEN DOWN (kb/Work R27, kb/Work PB640). §8.5.2.1
    /// Table 2 makes an index data item class INDEX, never numeric — but its storage
    /// <see cref="PicInfo"/> is <see cref="IndexItem"/>, which carries category <see cref="PicCategory.Numeric"/>
    /// with <c>Digits = 0</c>, so a bare <c>Category is Numeric &amp;&amp; !IsFloat</c> test admits it. Anything
    /// that then treats the item as a numeric RECEIVER stores through a zero-digit profile, whose capacity
    /// reduction is <c>value % 10^0</c> — ZERO, silently. PB640 hit exactly that: §14.2.3 GR9 splits the
    /// argument crossing three ways ("if the formal parameter is numeric, a COMPUTE statement without the
    /// ROUNDED phrase" / "if the formal parameter is of class index, object, or pointer, a SET statement" /
    /// "otherwise, a MOVE statement"), and an activating-side landing guarded on the bare test sent
    /// <c>BY CONTENT</c> an index of 3 across as 0. <see cref="IsIntegerDescription"/> narrows this by scale; callers
    /// asking "is this a FIXED-POINT numeric receiver" want THIS one. A caller asking "is this a COMPUTE receiving
    /// operand" — which a floating-point item is too — wants <see cref="IsClassNumeric"/>.</para></summary>
    public bool IsClassNumericFixedPoint => IsClassNumeric && !IsFloat;

    /// <summary>True for a description whose item is of CLASS NUMERIC, fixed-point OR floating-point, and never a
    /// USAGE INDEX item (see <see cref="IsClassNumericFixedPoint"/> for the trap). The population §14.2.3 GR9's
    /// numeric alternative and §14.8.2.3.3 2) a) speak of — "if the formal parameter is numeric, a COMPUTE statement
    /// without the ROUNDED phrase" — which has NO floating-point exemption: a FLOAT-SHORT or FLOAT-LONG formal is such a
    /// receiver, landed by <c>FloatResultant</c> rather than <c>CobolNum.Store</c> (kb/Work PB1114).</summary>
    public bool IsClassNumeric => Category is PicCategory.Numeric && Usage is not Usage.Index;

    /// <summary>True for an UNSIGNED BinaryCapacity item whose 16-byte container range [0, 2^128) exceeds every
    /// signed carrier — its CLR carrier is <see cref="UInt128"/> (kb/Work R10, owner decision 2026-08-07: the item
    /// owns its full container range, ISO §13.18.60.4 GR12). Reads of such an item enter the numeric renderer on
    /// the unsigned-wide lane (<c>NumX.U</c>), whose value paths keep the full range and whose arithmetic funnel
    /// is <c>CobolNum.Widen</c>.</summary>
    public bool IsUnsignedWideBinary => Category is PicCategory.Numeric && !Signed
        && Truncation == NumericTruncation.BinaryCapacity && StorageWidth == 16;

    /// <summary>True for an UNSIGNED BinaryCapacity item whose 8-byte container range [0, 2^64) exceeds
    /// <c>long</c> but fits <see cref="Int128"/> — its CLR carrier is <c>ulong</c> (the F75 tier: the fold said
    /// 2^64−1 while the <c>long</c> field topped out at 2^63−1). Its reads ride the ordinary Int128 engine
    /// (every <c>ulong</c> value fits), cast at the read site.</summary>
    public bool IsUnsignedLongBinary => Category is PicCategory.Numeric && !Signed
        && Truncation == NumericTruncation.BinaryCapacity && StorageWidth == 8;

    /// <summary>⛔ AN UPPER BOUND ON THE DECIMAL DIGITS OF THIS ITEM'S UNSCALED VALUE — the fact the native renderer's
    /// carrier decisions are made on (<c>NumX.Digits</c>; kb/Work PB1143 / PB621). A DISPLAY / PACKED / plain
    /// COMPUTATIONAL item holds at most <see cref="Digits"/>; a BinaryCapacity (COMP-5, BINARY-CHAR … -DOUBLE) item
    /// owns its whole container range (§13.18.60.4 GR12) — values past the PICTURE's digits are legal — so its bound
    /// is the container's: 2^(8w−1) &lt; 10^3 / 10^5 / 10^10 / 10^19 for the signed 1/2/4/8-byte widths, one more
    /// digit unsigned at 8 bytes (2^64 &lt; 1.85·10^19), and the 16-byte container 39 digits (2^127 ≈ 1.7·10^38 — the
    /// Int128 engine's own magnitude; its unsigned-wide lane funnels through <c>CobolNum.Widen</c>, loud beyond
    /// that). Never smaller than the PICTURE and never smaller than the container: a bound that undershoots would
    /// let an overflowing operation skip its check. (A bound over the carrier's 38 digits simply routes every
    /// consumer to the checked or SDIDI form.)</summary>
    public int UnscaledDigitBound
    {
        get
        {
            int pictured = Math.Max(Digits, DigitPositions);
            if (Truncation != NumericTruncation.BinaryCapacity) return Math.Min(pictured, 39);
            int container = StorageWidth switch { 1 => 3, 2 => 5, 4 => 10, 8 => Signed ? 19 : 20, _ => 39 };
            return Math.Min(Math.Max(pictured, container), 39);
        }
    }

    private readonly int? _digitPositions;
    /// <summary>The ISO §13.18.40.3 SR14 DIGIT-POSITION count that the 1–31 (18 pre-2002) capacity cap is measured
    /// against: the '9'/Z/* positions, each 'P' (§13.18.40.4 — counted in the maximum digit positions though it stores
    /// no digit), and the floating '+'/'-'/currency digit positions (a floating string of a symbol appearing k≥2 times
    /// contributes k−1 — the leftmost is the sign/currency position). For a numeric-EDITED item this can EXCEED
    /// <see cref="Digits"/> (which counts only '9'); for a pure-numeric or any non-analyzer PicInfo it defaults to
    /// <see cref="Digits"/> (the two coincide). (CA33.)</summary>
    public int DigitPositions { get => _digitPositions ?? Digits; init => _digitPositions = value; }

    /// <summary>The ONE PicInfo-keyed receiver-scale rule (kb/Work PB64 T6 — it was written twice, in
    /// <c>MoveEmitter.SenderContext</c> and <c>ArithmeticEmitter.ScaleOf</c>, and both copies silently fell to
    /// <c>Scale</c> = 0 for a locale item, truncating a fractional sender; kb/Work PB1685 moved it HERE so the
    /// report-writer SUM binder asks the same question the emitters do): a floating-point edited receiver has no
    /// fixed scale (0 — the caller's form dispatch never uses it); a format-2 (LOCALE) receiver's scale is the
    /// picture's digits right of '.' (<see cref="Scale"/> — the analyzer set it; there is no mask); a masked
    /// numeric-edited receiver's is the MASK's (<c>CobolEdit.MaskScale</c> — an edited PicInfo's own
    /// <see cref="Scale"/> is 0); everything else <see cref="Scale"/>. A float-edited receiver rides the mask arm
    /// too — its significand scale drives the working scale of an intermediate landing.</summary>
    public int ReceiverScale() =>
        LocaleEdit is not null ? Scale
        : this is { Category: PicCategory.NumericEdited, EditMask: { } mask }
            ? CobolNet.Runtime.CobolEdit.MaskScale(mask, EditingRules as CobolNet.Runtime.CobolEdit.EditRule[], '$', DecimalPointIsComma)
        : Scale;

    /// <summary>For a <see cref="PicCategory.ObjectReference"/> item: the FULL §13.18.60.2 description —
    /// kind × FACTORY × ONLY × name (<see cref="ObjectRefDescriptor"/>). Null for every other category.
    /// <para>⛔ It replaced a single <c>string? ObjectClassName</c> (kb/Work PB389): the general format prints
    /// four INDEPENDENT axes and the scalar carried one, so ACTIVE-CLASS and ONLY were rejected outright and
    /// every consumer re-derived the KIND by interrogating the class table. Ask the descriptor.</para></summary>
    public ObjectRefDescriptor? ObjectRef { get; init; }

    /// <summary>An object-reference item's representation (the Phase-3 OO spine; PICTURE-less per
    /// §13.18.60.4 — the <see cref="PicInfo.IndexItem"/> synthesis pattern). <paramref name="desc"/> is the
    /// whole §13.18.60.2 description; <see cref="ObjectRefDescriptor.Universal"/> is the bare form.</summary>
    public static PicInfo ObjectReferenceItem(ObjectRefDescriptor desc) =>
        new(PicCategory.ObjectReference, Usage.ObjectReference, Length: 0, Digits: 0, Scale: 0, Signed: false)
        { ObjectRef = desc };

    /// <summary>A USAGE POINTER item's representation (Phase-4b; PICTURE-less per §13.18.60 — the IndexItem
    /// synthesis pattern). Occupies NO character positions (never part of a group's §13.18.60 GR4 image).
    /// <para><paramref name="restrictedTypeName"/> is the <c>TO type-name-1</c> of the RESTRICTED data-pointer
    /// form (§13.18.60.2 general format; §13.18.60.4 GR23 — "If type-name-1 is specified, this data item is a
    /// restricted data-pointer. A restricted data-pointer shall contain only the predefined address NULL or the
    /// address of a data item of the specified type."). Null = an ordinary, unrestricted data-pointer. It is a
    /// type NAME rather than a resolved <see cref="DataItem"/> because a restriction is to a TYPE, not to a
    /// member position, and because the naming entry may be bound before the type declaration it references
    /// (the TYPE-clause forward-reference discipline). kb/Work PB153.</para></summary>
    public static PicInfo PointerItem(string? restrictedTypeName = null) =>
        new(PicCategory.Pointer, Usage.Pointer, Length: 0, Digits: 0, Scale: 0, Signed: false)
        { RestrictedTypeName = restrictedTypeName };

    /// <summary>A USAGE PROGRAM-POINTER item's representation (P10 Step 7; PICTURE-less per §13.16.3 SR8 —
    /// the PointerItem synthesis pattern). Occupies NO character positions (§13.18.60 GR24 leaves alignment/
    /// size/representation implementor-defined — this implementation's representation is the managed
    /// <c>ProgramPointer</c> identity carrier, never storage bytes).</summary>
    public static PicInfo ProgramPointerItem(string? restrictedPrototypeName = null) =>
        new(PicCategory.ProgramPointer, Usage.ProgramPointer, Length: 0, Digits: 0, Scale: 0, Signed: false)
        { RestrictedPrototypeName = restrictedPrototypeName };

    /// <summary>A USAGE FUNCTION-POINTER item's representation (kb/Work PB452/PB817; PICTURE-less per
    /// §13.16.3 SR8 — the ProgramPointerItem synthesis pattern). Occupies NO character positions
    /// (§13.18.60.4 GR26 leaves alignment/size/representation implementor-defined — this implementation's
    /// representation is the managed <c>FunctionPointer</c> identity carrier, never storage bytes; the
    /// determination is published as <c>docs/CONFORMANCE.md</c> §7 row DOC-A.1-210).
    /// <para><paramref name="restrictedPrototypeName"/> is the MANDATORY <c>TO function-prototype-name-1</c>:
    /// §13.18.60.2's general format prints it unbracketed, so every function-pointer is restricted and GR26's
    /// "shall contain only the predefined address NULL or the address of a function with the same signature as
    /// that identified by the specified function-prototype-name-1" always has a prototype to name.</para></summary>
    public static PicInfo FunctionPointerItem(string restrictedPrototypeName) =>
        new(PicCategory.FunctionPointer, Usage.FunctionPointer, Length: 0, Digits: 0, Scale: 0, Signed: false)
        { RestrictedPrototypeName = restrictedPrototypeName };

    /// <summary>The synthesized profile of a PICTURE-less fixed-width binary item (USAGE BINARY-CHAR/-SHORT/
    /// -LONG/-DOUBLE, ISO §13.18.60.4 GR12; PICTURE prohibited per §13.16.3 SR8). Category numeric, realized as
    /// a native two's-complement integer of the fixed byte width (1/2/4/8) under the COMP-5 BinaryCapacity
    /// truncation discipline (numeric design D6). SIGNED is the default (GR12); UNSIGNED clears the operational
    /// sign and widens the positive range (same storage width, GR21). The spec gives no implied PICTURE, so the
    /// DISPLAY digit count is WiseOwl COBOL's documented implementor choice: the decimal width of the range's
    /// maximum magnitude — CHAR 3 / SHORT 5 / LONG 10 / DOUBLE 19 (signed) · 20 (unsigned).</summary>
    public static PicInfo BinaryItem(Usage usage, bool signed)
    {
        int digits = usage switch
        {
            Usage.BinaryChar => 3,
            Usage.BinaryShort => 5,
            Usage.BinaryLong => 10,
            Usage.BinaryDouble => signed ? 19 : 20,
            _ => throw new ArgumentException($"not a fixed-width binary usage: {usage}", nameof(usage)),
        };
        return new PicInfo(PicCategory.Numeric, usage, Length: digits, Digits: digits, Scale: 0, Signed: signed)
            { SignKind = SignKindFor(usage, signed, sign: null) };
    }

    /// <summary>The synthesized profile of a REPORT COUNTER — PAGE-COUNTER or LINE-COUNTER (ISO §8.4.3.15.4
    /// GR1): "PAGE-COUNTER and LINE-COUNTER reference temporary unsigned integer data items of class and
    /// category numeric, which are maintained for each report." UNSIGNED and scale 0, exactly as GR1 words it,
    /// so every receiving screen and conversion the compiler already owns treats an assignment to PAGE-COUNTER
    /// (§8.4.3.15.3 SR1) like an assignment to any other unsigned integer item. USAGE BINARY over the engine's
    /// native <c>long</c> carrier, capped at the 18 digits that carrier holds — the standard fixes no digit
    /// count for a counter, and the engine's own range is this implementation's answer (kb/Work PB429).</summary>
    public static PicInfo ReportCounterItem() =>
        new(PicCategory.Numeric, Usage.Binary, Length: 18, Digits: 18, Scale: 0, Signed: false)
        { SignKind = SignKindFor(Usage.Binary, signed: false, sign: null) };

    /// <summary>The synthesized profile of a REPORT SECTION <b>sum counter</b> (ISO §13.18.54.4 GR1): "The sum
    /// counter is a conceptual data item that behaves as a data item of the category numeric. The number of
    /// decimal digits in the sum counter, both integral and fractional, is derived from the corresponding number
    /// of digits, excluding insertion editing characters, in the PICTURE clause of the entry containing the SUM
    /// clause. The sum counter is signed, whether or not the corresponding PICTURE clause has an operational
    /// sign." The counter is engine state, never storage, so its VALUE is the native integer the engine
    /// accumulates in — <paramref name="digits"/>/<paramref name="scale"/> taken from the entry's own analyzed
    /// PICTURE, and SIGNED unconditionally per GR1's last sentence.
    /// <para>⛔ ITS USAGE IS DISPLAY — implementor latitude, recorded in docs/CONFORMANCE.md A.4.11 (kb/Work PB1943,
    /// PB2520). GR1 gives the counter a category (numeric), a digit count and a sign, and NO usage; the standard
    /// states a usage wherever it makes a conceptual item one (§14.9.1.4 GR7: DATE "behaves as if it had been
    /// described as an unsigned elementary integer data item of usage display"), so the choice is the implementor's. DISPLAY is the one the neighbours
    /// make (Micro Focus sizes the counter "by the PICTURE clause" in character positions) and the only one under
    /// which the counter has CHARACTER positions at all: §8.4.3.3.3 SR1 admits reference modification of "a numeric
    /// data item of usage display or national", and FUNCTION LENGTH, LENGTH OF and every other size consumer then
    /// read the digit count GR1 derives (a PIC 9999 counter is 4 positions, never the binary size 2). It is the
    /// profile of <c>PIC S9(digits)V9(scale)</c> USAGE DISPLAY with the default operational sign — trailing, not
    /// separate, so the last digit carries the sign in the processor's overpunch convention — and the capacity is
    /// the PICTURE's digit count (<see cref="NumericTruncation.DigitCount"/>), as it was under BINARY.</para>
    /// <para>⛔ THE DIGIT COUNT IS THE PICTURE'S, NOT A CARRIER'S (kb/Work PB1666). The engine accumulates in an
    /// <c>Int128</c> (<c>CobolReport.SumEntry</c>), the carrier <see cref="ClrType"/> gives any 19–38-digit
    /// numeric item, so every counter a numeric or numeric-edited PICTURE can describe (≤ 31 digit positions,
    /// §13.18.40.3 SR14) keeps its full GR1 digit count. This used to clamp at 18 — the old <c>long</c> carrier's
    /// ceiling — so a PIC 9(20) SUM entry reported a false size error past 9.2·10^18 and printed spaces. The only
    /// counter still bounded is an alphanumeric or national PICTURE of more than 38 positions (one digit per
    /// character position, <c>DataBinder.SumCounterDigits</c>), capped at the 38 digits the carrier holds.</para></summary>
    public static PicInfo SumCounterItem(int digits, int scale)
    {
        int d = System.Math.Clamp(digits, 1, 38);
        return new PicInfo(PicCategory.Numeric, Usage.Display, Length: d, Digits: d, Scale: scale, Signed: true)
            { SignKind = SignKindFor(Usage.Display, signed: true, sign: null) };
    }

    /// <summary>The C# type used to store this item's value.</summary>
    public string ClrType => Category switch
    {
        // A typed object reference is the class's emitted C# type (nullable — COBOL initial state is NULL,
        // §13.18.63); universal → CobolObject? (D-U1: CobolObject IS the runtime universal type — every
        // emitted class derives from it (D2), GR2b defers non-COBOL interop, dispatch sites need no cast,
        // and a non-CobolObject can never leak in). The name mapping matches the ClassUnit emission convention
        // (Sanitize + uppercase — COBOL class names are case-insensitive, §8.3.2.2).
        PicCategory.ObjectReference => (ObjectRef ?? ObjectRefDescriptor.Universal).ClrTypeName + "?",
        // A data pointer is the runtime ManagedPointer carrier; its COBOL initial state is NULL (the Null
        // singleton), so the field is non-nullable and always at least Null.
        PicCategory.Pointer => "ManagedPointer",
        // A program pointer is the runtime ProgramPointer carrier (§13.18.60 GR24 — the address of an
        // outermost program; a readonly identity struct whose default IS the NULL program address).
        PicCategory.ProgramPointer => "ProgramPointer",
        // A function pointer is the runtime FunctionPointer carrier (§13.18.60.4 GR26 — the address of a
        // function; the ProgramPointer twin, a SEPARATE readonly identity struct so the generated C# cannot
        // make the two categories assignment-compatible behind the binder's screens).
        PicCategory.FunctionPointer => "FunctionPointer",
        // National (one UTF-16 char per national position, D-N1) and boolean (one '0'/'1' char per boolean
        // position, D-B1 — §13.18.40.4 GR14 R14) ride the same fixed-width string substrate as alphanumeric.
        PicCategory.Alphanumeric or PicCategory.NumericEdited
            or PicCategory.National or PicCategory.Boolean => "string",
        // Fixed-point numerics (DISPLAY/COMP/COMP-3/COMP-5) are stored as a native integer holding the UNSCALED
        // value (all digits; the decimal point is implied by Scale, compile-time metadata) — long up to 18 digits,
        // Int128 for the 19–31-digit 2002+ tier. COMP-1/COMP-2 are hardware floats. (No decimal/BigInteger.)
        // ⛔ An UNSIGNED BinaryCapacity item owns its full container range (ISO §13.18.60.4 GR12; kb/Work R10,
        // owner decision 2026-08-07), so its carrier is the container's own unsigned CLR type wherever a signed
        // carrier cannot hold that range: an 8-byte container holds [0, 2^64) → ulong (long tops out at 2^63−1 —
        // the F75 fold/storage disagreement), a 16-byte container holds [0, 2^128) → UInt128. Containers of
        // 4 bytes and below fit long exactly and keep the common carrier.
        PicCategory.Numeric => Usage switch
        {
            Usage.Float or Usage.FloatShort or Usage.FloatBinary32 => "float",   // COMP-1 / FLOAT-SHORT / IEEE binary32
            Usage.Double or Usage.FloatLong or Usage.FloatExtended => "double",
            // FLOAT-BINARY-64 = IEEE binary64 (double). FLOAT-BINARY-128 / FLOAT-DECIMAL-16/34 are processor-
            // dependent NON-support (rejected at ParseUsage, COBOLNET1564) — these fallback maps are unreached in a
            // valid compile (never emit a non-conforming double/decimal for a pinned IEEE format).
            Usage.FloatBinary64 or Usage.FloatBinary128 => "double",
            Usage.FloatDecimal16 or Usage.FloatDecimal34 => "decimal",
            _ when IsUnsignedWideBinary => "UInt128",
            _ when IsUnsignedLongBinary => "ulong",
            _ => Digits > 18 ? "Int128" : "long",
        },
        _ => "object", // Group: never stored as a scalar (emitted as a record struct).
    };

    /// <summary>True for a floating-point usage (COMP-1/COMP-2/FLOAT-SHORT/-LONG/-EXTENDED and the 2014
    /// FLOAT-BINARY-*/FLOAT-DECIMAL-* family); its value is IEEE floating-point, not a scaled integer (D16).
    /// FLOAT-EXTENDED maps to double — no .NET quad (§13.18.60.4 GR13 subset nesting).</summary>
    public bool IsFloat => Usage is Usage.Float or Usage.Double
        or Usage.FloatShort or Usage.FloatLong or Usage.FloatExtended
        or Usage.FloatBinary32 or Usage.FloatBinary64 or Usage.FloatBinary128
        or Usage.FloatDecimal16 or Usage.FloatDecimal34;

    /// <summary>True for a SINGLE-precision float usage (COMP-1 / FLOAT-SHORT / FLOAT-BINARY-32 = IEEE binary32) —
    /// drives the <c>f</c> literal suffix and the <c>(float)</c> store cast; every other float usage is double.</summary>
    public bool IsSingle => Usage is Usage.Float or Usage.FloatShort or Usage.FloatBinary32;

    /// <summary>The default C# initializer for an item with no VALUE clause (COBOL initial state, ISO §13.18.63).</summary>
    public string DefaultInitializer => Category switch
    {
        // An object reference's COBOL initial state IS null (§13.18.60.4 — the predefined NULL reference);
        // .NET reference-default null matches exactly, no init needed beyond the explicit form.
        PicCategory.ObjectReference => "null",
        PicCategory.Pointer => "ManagedPointer.Null",   // the predefined NULL data pointer (§8.4.3.10)
        PicCategory.ProgramPointer => "ProgramPointer.Null",   // the NULL program address (§8.4.3.10 GR3)
        PicCategory.FunctionPointer => "FunctionPointer.Null",   // the NULL function address (§8.4.3.10.4 GR2)
        // Alphanumeric AND national default to spaces (the national space is U+0020 under the D-N4 Latin-1
        // repertoire); boolean to boolean zeros (§13.18.63 — the category fill values); numeric to zero (unscaled).
        PicCategory.Alphanumeric or PicCategory.NumericEdited or PicCategory.National
            => $"new string(' ', {Length})",
        PicCategory.Boolean => $"new string('0', {Length})",
        PicCategory.Numeric => Usage switch
        {
            Usage.Float or Usage.FloatShort or Usage.FloatBinary32 => "0f",
            Usage.Double or Usage.FloatLong or Usage.FloatExtended or Usage.FloatBinary64 or Usage.FloatBinary128 => "0d",
            Usage.FloatDecimal16 or Usage.FloatDecimal34 => "0m",   // unreached (rejected at ParseUsage, COBOLNET1564)
            _ when IsUnsignedWideBinary => "(UInt128)0",
            _ when IsUnsignedLongBinary => "0UL",
            _ => Digits > 18 ? "(Int128)0" : "0L",
        },
        _ => "default",
    };

    /// <summary>
    /// The BYTE REPRESENTATION this item's storage takes (<see cref="NumericByteForm"/>, which carries the
    /// pinned form of each) — the one fact that decides what the item occupies at EVERY byte boundary: the
    /// record/group character image, a file record, a SORT key window and the REDEFINES backing
    /// (COBOLNET_DESIGN §14.4). ISO §13.18.60.4 GR4 (BINARY, "a radix of 2 is used") and GR11 (PACKED-DECIMAL,
    /// "a radix of 10 … the minimum possible configuration") leave the representation to the implementor and
    /// §4.2.16 obliges us to document the choice; <see cref="StorageWidth"/> is the width that representation
    /// implies, and <c>DataItem.ByteWidth</c> / <c>DataItem.ImageWidth</c> are two views of it, never two answers.
    /// <para>The truncation discipline CANNOT stand in for this: USAGE DISPLAY and USAGE BINARY are both
    /// <see cref="NumericTruncation.DigitCount"/> yet occupy entirely different bytes.</para>
    /// <para>Consulted only where a <c>NumProfile</c> is emitted (every numeric item, floats included since
    /// kb/Work PB164 wave 2 and USAGE INDEX since the R40 owner decision — see
    /// <c>RecordStructEmitter.EmitProfiles</c>); <see cref="NumericByteForm.None"/> remains only for the
    /// usages that carry no profile at all. <c>NumericByteFormDriftTests</c> pins the whole table.</para>
    /// </summary>
    public NumericByteForm ByteForm => Usage switch
    {
        // Radix 2 (GR4 BINARY / GR6 COMPUTATIONAL / GR12 the fixed-width binary usages) — two's complement,
        // big-endian, StorageWidth bytes. COMP-5 and BINARY-CHAR..DOUBLE share the form: their
        // BinaryCapacity discipline is a CAPACITY fact (values beyond the PICTURE digit count), never a
        // representation fact — which is exactly what let kb/Work PB164 admit them to the image on THIS
        // form (the width is StorageWidth; a full-container value rides FormatImage's Int128 bits
        // contract). Stating the form here made admitting them a width question, and the width was
        // already pinned.
        Usage.Binary or Usage.Comp5 or Usage.BinaryChar or Usage.BinaryShort or Usage.BinaryLong
            or Usage.BinaryDouble => NumericByteForm.Binary,
        // Radix 10 BCD (GR11), with or without the trailing sign nibble the 2023 WITH NO SIGN phrase drops.
        Usage.Packed => PackedNoSign ? NumericByteForm.PackedNoSign : NumericByteForm.Packed,
        // The CHARACTER image — one character per digit position (GR7 for DISPLAY, GR8 for NATIONAL).
        // ⛔ NATIONAL SHARES THE ZONED FORM, and that is the whole of design D-N7 (kb/Work PB646): a
        // national-form numeric item's image is the SAME digit run its DISPLAY twin holds — §13.18.40.4 GR1
        // makes each of those digits a NATIONAL character position rather than an alphanumeric one, and
        // §13.18.60.4 GR8 pins what a national character position occupies (two bytes, D-N1). The doubling is
        // therefore a property of the CHARACTER→BYTE transform, applied once by the one national coding
        // (NationalWindow / CobolBits.NatBytes) at every byte boundary, never a second numeric byte form: a
        // "NationalZoned" form would have to re-derive every digit, sign and de-edit rule Zoned already owns.
        // USAGE BIT and the carrier usages never reach this property.
        Usage.Display or Usage.National => NumericByteForm.Zoned,
        // IEEE 754 interchange, big-endian (kb/Work PB164 wave 2): GR14/GR15 PIN the FLOAT-BINARY forms and
        // GR13 leaves COMP-1/COMP-2/FLOAT-* to the implementor — one encoding serves both. FLOAT-BINARY-128
        // and the decimal floats never reach here (rejected at ParseUsage).
        Usage.Float or Usage.FloatShort or Usage.FloatBinary32 => NumericByteForm.Ieee32,
        Usage.Double or Usage.FloatLong or Usage.FloatExtended or Usage.FloatBinary64 => NumericByteForm.Ieee64,
        // The R40 owner decision (2026-08-30; kb/Work PB164's last leaf-kind exclusion): an index data
        // item's image is its occurrence number as an 8-byte BIG-ENDIAN two's-complement binary — the
        // documented 64-bit carrier's bytes, in the byte order every other pinned form uses (A.1 item 211,
        // §4.2.16; the representation determination is §13.18.60.4 GR10's implementor latitude). The
        // REFERENCE restriction is §13.18.60.3 SR10 — SEARCH/SET, relation conditions, intrinsic and inline
        // method-invocation arguments, and the USING phrases of a PD header / CALL / INVOKE — a
        // statement-surface syntax rule that stands; this row opens the GROUP's byte crossing.
        Usage.Index => NumericByteForm.Binary,
        _ => NumericByteForm.None,
    };

    /// <summary>True when this picture is a numeric with a PINNED byte representation — a
    /// <see cref="ByteForm"/> other than <see cref="NumericByteForm.None"/> — i.e. it can occupy positions in
    /// a record/group image, cross CALL/MOVE as group bytes, and back a REDEFINES window. ⛔ THE ONE image
    /// predicate (kb/Work PB164): it is DERIVED from the ByteForm table, never a hand-rolled usage union —
    /// four copies of the union had drifted to {Display, Binary, Packed} (wave 1 admitted COMP-5/
    /// BINARY-CHAR..DOUBLE; wave 2 the IEEE float forms, which retired the interim !IsFloat conjunct exactly
    /// as its doc promised; the R40 decision pinned USAGE INDEX's 8-byte binary occurrence-number image,
    /// closing the last leaf-kind exclusion). EVERY numeric usage that survives ParseUsage now answers true.
    /// A usage added to ByteForm widens every consumer HERE.</summary>
    public bool HasImageByteForm => Category is PicCategory.Numeric && ByteForm is not NumericByteForm.None;

    /// <summary>⛔ THE ONE "is this a fixed-point numeric item whose IMAGE is a run of digit CHARACTERS?"
    /// predicate — category numeric, not floating-point, and one of the two CHARACTER usages: DISPLAY (ISO
    /// §13.18.60.4 GR7 — "an alphanumeric coded character set shall be used to represent a data item in the
    /// storage of the computer") or NATIONAL (GR8 — "a national coded character set shall be used",
    /// §13.18.60.3 SR12's national-form numeric, LIVE since kb/Work PB646). The two differ only in how one of
    /// those characters is SERIALIZED — design D-N7 pins two UTF-16BE bytes per national position — never in
    /// what the image IS, so a site asking about the IMAGE wants both.
    /// <para>⛔ IT IS THE IMAGE-FORM QUESTION, NOT THE CARRIAGE ONE, and the pair is easy to conflate because
    /// for usage DISPLAY the two answers coincide. Read it where the item is VIEWED through its characters and
    /// keeps its native carrier — a reference modification (§8.4.3.3.4 GR3/GR5a), a RENAMES span's
    /// <c>NumericImagePlace</c> (§13.18.45), a STRING receiver parsing an image. Do NOT read it where the
    /// question is whether the leaf's carrier may be REPLACED by that character string
    /// (<c>DataBinder.MarkImageForced</c> / <c>StorageFormPass</c>'s promotion): promotion pins the carrier at
    /// <c>ImageWidth</c> CHARACTER positions, which for usage DISPLAY is the leaf's whole storage and for usage
    /// NATIONAL is half of it (D-N1), and promoting a national-form numeric leaf was MEASURED to corrupt a plain
    /// group-to-group MOVE between two of them. Each such site says so in its own comment.</para></summary>
    public bool IsCharacterFormNumeric =>
        Category is PicCategory.Numeric && !IsFloat && Usage is Usage.Display or Usage.National;

    /// <summary>The CAPACITY discipline that bounds this item's value — the SIZE ERROR boundary
    /// (<see cref="NumericTruncation"/>). Orthogonal to <see cref="ByteForm"/>, which is the byte
    /// representation: BINARY truncates by PICTURE digit count exactly as DISPLAY does, while COMP-5 and the
    /// fixed-width binary usages hold the native two's-complement range of their byte width (ISO §13.18.60.4
    /// GR12), and PACKED-DECIMAL's capacity follows its BCD nibbles.</summary>
    public NumericTruncation Truncation => Usage switch
    {
        Usage.Packed => NumericTruncation.PackedDecimal,
        Usage.Comp5 or Usage.BinaryChar or Usage.BinaryShort or Usage.BinaryLong or Usage.BinaryDouble
            => NumericTruncation.BinaryCapacity,
        _ => NumericTruncation.DigitCount,
    };

    /// <summary>The item's storage width in BYTES for every usage that has one of its own — the width its
    /// <see cref="ByteForm"/> lays out, the width <c>FUNCTION BYTE-LENGTH</c> reports, and the width it
    /// occupies in a record image (0 for the usages whose bytes ARE their character positions).</summary>
    public int StorageWidth => Usage switch
    {
        // WITH NO SIGN (§13.18.60.4 GR11) drops the trailing sign nibble → exactly the digit nibbles.
        Usage.Packed => PackedNoSign ? (Digits + 1) / 2 : Digits / 2 + 1,
        // ⛔ The ladder must be SUFFICIENT, not merely conventional: §13.18.60.4 GR4 closes with "Sufficient
        // computer storage shall be allocated by the implementor to contain the maximum range of values implied
        // by the associated decimal picture character-string." 1-2-4-8 covers 1..18 digits exactly (a signed
        // 18-digit max 10^18−1 fits 2^63−1; a signed 19-digit 10^19−1 does NOT), so a 19–38-digit picture — legal
        // at COBOL-2002+ and already stored as Int128 (IsWide/ClrType) — takes the 16-byte tier. Before this the
        // table answered 8 for every picture ≥ 10 digits, so PIC S9(31) COMP claimed a width that cannot hold it.
        // The ladder is SIGN-INDEPENDENT — an unsigned 19-digit value would fit 8 bytes, but GR12 pins SIGNED and
        // UNSIGNED to one width for the fixed-width binary usages and every surveyed compiler follows that, so
        // the boundary is set by the signed worst case rather than splitting one picture into two widths.
        Usage.Binary or Usage.Comp5 => Digits <= 2 ? 1 : Digits <= 4 ? 2 : Digits <= 9 ? 4 : Digits <= 18 ? 8 : 16,
        // The fixed-width binary usages own their byte width directly (independent of the implied Digits;
        // ISO §13.18.60.4 GR21 — implementor-defined length, SIGNED and UNSIGNED the same width).
        Usage.BinaryChar => 1,
        Usage.BinaryShort => 2,
        Usage.BinaryLong => 4,
        Usage.BinaryDouble => 8,
        // The R40 pin: 8 bytes — the documented 64-bit occurrence-number carrier (A.1 item 211).
        Usage.Index => 8,
        // The IEEE interchange widths (kb/Work PB164 wave 2 — §13.18.60.4 GR13-GR15).
        Usage.Float or Usage.FloatShort or Usage.FloatBinary32 => 4,
        Usage.Double or Usage.FloatLong or Usage.FloatExtended or Usage.FloatBinary64 => 8,
        _ => 0,
    };

    /// <summary>
    /// The C# initializer text for this item's runtime <c>NumProfile</c> (threaded into every numeric store so
    /// arithmetic obeys the receiver's PICTURE+USAGE). Emitted once per numeric item as a static readonly field.
    /// <para>⛔ A METHOD, NOT A PROPERTY, BECAUSE THE PROFILE CARRIES A COMPILE OPTION (kb/Work PB803): the
    /// over-punch convention (<c>--sign-encoding</c>, Annex A.1 items 177/178) is a property of the COMPILED
    /// PROGRAM, and this is the ONE place a <c>NumProfile</c> is written, so a required parameter here is what
    /// makes every emitted profile state the program's convention — a settable field on <c>PicInfo</c> would have
    /// to be remembered at every one of its construction sites instead.</para>
    /// </summary>
    /// <param name="signEncoding">The compilation's over-punch convention (<c>EmitContext.SignEncoding</c>).</param>
    public string ProfileInitializer(SignEncoding signEncoding) =>
        $"new NumProfile {{ Digits = {Digits}, FractionDigits = {Scale}, " +
        $"Signed = {(Signed ? "true" : "false")}, SignKind = NumericSign.{SignKind}, " +
        $"Truncation = NumericTruncation.{Truncation}, ByteForm = NumericByteForm.{ByteForm}, " +
        // HIGH-ORDER-RIGHT (§13.18.60.4 GR19b via §11.9.8, kb/Work PB164 wave 2) — stated only when set, so
        // the emitted profile of every big-endian item (the documented default) is byte-identical to before.
        $"StorageLength = {StorageWidth}{(FloatLittleEndian ? ", FloatLittleEndian = true" : "")}" +
        // Likewise the over-punch convention: SignEncoding.Ibm is the documented default (kb/Work PB803), so an
        // ordinary compile's emitted profiles are byte-identical to those from before the option existed.
        $"{(signEncoding is SignEncoding.Ibm ? "" : $", SignEncoding = SignEncoding.{signEncoding}")} }}";

    /// <summary>The runtime <c>NumericSign</c> member name for a numeric item (COBOLNET_DESIGN §6.4): binary/packed
    /// usages use a leading minus; the CHARACTER-FORM usages use over-punch (trailing by default, leading under
    /// SIGN LEADING) or a separate <c>+</c>/<c>-</c> character under SIGN SEPARATE (ISO §13.18.52 GR5/GR6). The ONE
    /// computation of SignKind — also called by the binder's group-SIGN inheritance pass with the nearest-ancestor
    /// clause.
    /// <para>⛔ BOTH CHARACTER-FORM USAGES, not display alone (kb/Work PB646). §13.18.52.3 SR2 names exactly the
    /// pair — "The usage of an elementary item for which the SIGN clause is specified shall be display or
    /// national" — and GR6a makes a SEPARATE sign "the leading (or, respectively, trailing) character position of
    /// the data item to which it applies; this character position is not a digit position", which under usage
    /// national is a NATIONAL character position (§13.18.40.4 GR1). The display-only arm sent a national-form
    /// signed item to BinaryMinus, which dropped that position from every width the model derives from
    /// SignKind: <c>PIC S9(3) USAGE NATIONAL SIGN IS LEADING SEPARATE</c> measured FUNCTION LENGTH 3 and
    /// BYTE-LENGTH 6 where the item is 4 national positions and 8 bytes, while its DISPLAY twin measured 4.</para></summary>
    public static string SignKindFor(Usage usage, bool signed, SignSpec? sign)
    {
        if (!signed) return "TrailingOverpunch";                        // unused for an unsigned item
        if (usage is not (Usage.Display or Usage.National)) return "BinaryMinus";   // COMP / COMP-3 / COMP-5
        if (sign is { Separate: true }) return sign.Leading ? "LeadingSeparate" : "TrailingSeparate";
        return sign is { Leading: true } ? "LeadingOverpunch" : "TrailingOverpunch";
    }

    /// <summary>The RECOVERY shape for an entry whose declaration has already FAILED to bind (an invalid
    /// PICTURE, an illegal picture-less USAGE NATIONAL/BIT elementary item, ...): elementary alphanumeric of the
    /// picture's width (1 when nothing usable). The compile has already errored -- this value only keeps the
    /// doomed emit pass crash-free (CompilerDriver reports bind diagnostics after Emit completes; the
    /// binary_usage crash, DEVLOG 597). A plain VALUE, never an identity-tested sentinel: the former
    /// reference-comparable RecoveryItem / NationalUsagePending / BitUsagePending singletons are replaced by the
    /// explicit <c>DataItem.Pending</c> adjudication mark (P5.11c).</summary>
    public static PicInfo Recovery(int length = 1) =>
        new(PicCategory.Alphanumeric, Usage.Display, Length: Math.Max(1, length), Digits: 0, Scale: 0, Signed: false)
        { IsRecovery = true };

    /// <summary><see cref="Recovery"/> for a <c>USAGE MESSAGE-TAG</c> entry — a DECLINED usage, refused by name
    /// at the data description entry (<c>PictureAnalyzer.ParseUsage</c>, COBOLNET1943; Annex A.3 item 4).
    /// <para>⛔ IT CARRIES THE WRITTEN USAGE, AND THAT IS THE WHOLE POINT (kb/Work PB453). The profile is still a
    /// recovery placeholder — <see cref="IsRecovery"/> is set and its category is NOT an analysis of anything,
    /// because §13.18.60.4 GR9 makes the category of a message-tag item message-tag and this implementation
    /// declines to model it — but the USAGE is a FACT the source states, and hiding it made every later screen
    /// answer about a category the item does not have. A Format-17 SET on such an item reached the
    /// object-reference screen KEYED TO §14.9.39.3 SR8 — COBOLNET0867, whose MESSAGE says the receiving operand
    /// "shall be a USAGE OBJECT REFERENCE data item"; ⛔ those are the DIAGNOSTIC's words, not the rule's, and
    /// SR8's own text is "Identifier-3 shall be any item of class object that is permitted as a receiving item" — and the
    /// §8.8.1.1 arithmetic screen (COBOLNET0844, "of category alphanumeric is not a numeric operand", the
    /// diagnostic's words again). Both are false about the program. With the usage visible,
    /// <c>SetFormatSelection</c> selects Format 17 and the statement names the declined FACILITY instead.</para>
    /// <para>⚠ Both arms of <c>ItemCategory.IsMessageTag</c> need it: <c>DataItem.OwnUsage</c> sees an entry
    /// that WROTE the clause, and only the resolved <see cref="PicInfo"/> sees one that acquired the usage by a
    /// TYPE clone or SAME AS copy.</para></summary>
    public static PicInfo MessageTagRecovery() =>
        new(PicCategory.Alphanumeric, Usage.MessageTag, Length: 1, Digits: 0, Scale: 0, Signed: false)
        { IsRecovery = true };

    /// <summary>The synthesized profile of a PICTURE-less <c>USAGE INDEX</c> data item (ISO §13.18.60): an
    /// elementary <c>long</c> holding an occurrence number. Digits/Scale are irrelevant — SET copies an index value
    /// UNCHANGED (§14.9.39 GR2b), never through a PICTURE store. Its IMAGE is the R40 pin — the occurrence
    /// number as 8 big-endian TWO'S-COMPLEMENT bytes (<see cref="ByteForm"/> Binary, <see cref="StorageWidth"/>
    /// 8) — so an INDEX-leaf group crosses images/CALL/MOVE/DISPLAY; §13.18.60.3 SR10 still restricts what may
    /// REFERENCE the item. ⛔ <c>Signed: true</c> IS the two's-complement pin (the R40 review fleet): the
    /// carrier is a signed <c>long</c>, and an UNSIGNED profile routes FormatBinaryImage through its
    /// absolute-value arm and ParseBinaryImage through zero-extension — magnitude out, unsigned in, NOT an
    /// involution, so a group MOVE of arbitrary bytes (HIGH-VALUES) silently rewrote the index window where
    /// §14.9.25.4 GR4 requires a representation copy. Signed makes both lanes the documented form.</summary>
    public static PicInfo IndexItem { get; } = new(PicCategory.Numeric, Usage.Index, Length: 0, Digits: 0, Scale: 0, Signed: true);

    /// <summary>The synthesized profile of a PICTURE-less floating-point item — COMP-1/COMP-2/FLOAT-SHORT/-LONG/
    /// -EXTENDED (ISO §13.18.60.2: floating-point usages are picture-less). Category Numeric, SIGNED (§13.18.60.4
    /// GR13 — "signed numeric data items"); <c>Digits</c>/<c>Scale</c> are inert (no PICTURE truncation, and the
    /// §14.7 composite-digit rule excludes float operands). The value lives in a native <c>float</c>/<c>double</c>
    /// field (D16), not the scaled-integer substrate; <c>IsWide</c> stays false (it already guards <c>!IsFloat</c>).
    /// <para><paramref name="floatBinaryEndianness"/> is the item's EFFECTIVE endianness — its own
    /// endianness-phrase (§13.18.60.2) where it wrote one, else the unit's OPTIONS FLOAT-BINARY model (§11.9.8),
    /// merged by <c>DataBinder</c> at the single call site per §11.9.8.3 SR2/SR3 ("in which an endianness-phrase
    /// is not specified"). THIS is the one application of GR19a/b: HIGH-ORDER-RIGHT flips
    /// <see cref="FloatLittleEndian"/> for the STANDARD binary float usages only (§13.18.60.4 GR19b/GR19c);
    /// <see cref="FloatEndianness.Unspecified"/>/HIGH-ORDER-LEFT and every non-standard float usage stay
    /// big-endian (§11.9.8.3 SR1 — our documented default; GR13/GR21 — our implementor pin). FLOAT-BINARY-128 is
    /// in §3.166's family and so ADMITS the phrase, but the usage itself is documented non-support
    /// (COBOLNET1564) and has no IEEE image lane, so it stays big-endian here too.</para></summary>
    public static PicInfo FloatItem(Usage usage, FloatEndianness floatBinaryEndianness = FloatEndianness.Unspecified) =>
        new(PicCategory.Numeric, usage, Length: 0, Digits: 0, Scale: 0, Signed: true)
        {
            FloatLittleEndian = usage is Usage.FloatBinary32 or Usage.FloatBinary64
                && floatBinaryEndianness is FloatEndianness.HighOrderRight,
        };
}

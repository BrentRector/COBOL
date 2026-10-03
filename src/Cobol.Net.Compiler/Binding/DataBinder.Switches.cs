// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Common;
using CobolNet.Editions;
using CobolNet.Editions.Diagnostics;
using CobolNet.Frontend.Generated;
using CobolNet.Runtime.Collation;

using CobolNet.Binding.Model;

namespace CobolNet.Binding;

using Core = CobolParserCore;

/// <summary>
/// The SPECIAL-NAMES external-switch registry (ISO §12.3.7, version-invariant 85→2023): the switch-name clause
/// associates an implementor-defined external switch with an optional mnemonic-name (Option 1) and/or ON/OFF
/// status condition-names (either Option). The mnemonic is referenced only in SET (SR5); the condition-names are
/// interrogated as switch-status conditions (GR2 / §8.8.4.6); SET alters the status (GR3 / §14.9.39 F3).
/// </summary>
public sealed partial class DataBinder
{
    // ⛔ NO SWITCH-MNEMONIC MAP LIVES HERE (kb/Work PB454). A switch's mnemonic-name is read, like every other
    // SPECIAL-NAMES mnemonic, from the ONE per-unit registry `Procedure.MnemonicRegistry` (BinderContext.Mnemonics),
    // whose value is the ImplementorName ROW, so SET Format 3 (§14.9.39.3 SR5) asks the row's KIND. This file used
    // to keep a second, switch-only map built by a second walk of the same entries — the walk in which an Option-2
    // entry (status phrases, no mnemonic) entered the switch-NAME itself as a mnemonic.

    /// <summary>Switch-status condition-names (case-insensitive) → (implementor switch-name, posited-ON) (ISO
    /// §12.3.7 GR2; §8.4.4.2 Format 1 SR1 — the condition-name shall be associated with a switch-name in
    /// SPECIAL-NAMES). Consulted by the condition binder AFTER level-88 resolution (NC211A defines a name as BOTH;
    /// the level-88 wins).</summary>
    public Dictionary<string, (string ImplementorName, bool IsOn)> SwitchConditions { get; } =
        new(StringComparer.OrdinalIgnoreCase);

    /// <summary>User-defined CLASS names (case-insensitive) → the EXPANDED member-character set (ISO §12.3.7
    /// class-name clause: each literal lists its characters; a THRU pair contributes every character between the
    /// two ordinals in the NATIVE collating sequence, in either order). Consulted by the class-condition binder
    /// (§8.8.4.4 — true when the operand consists entirely of members).
    /// <para>Each entry carries the CLASS of its characters (kb/Work PB976): a <c>FOR NATIONAL</c> class is a set of
    /// national characters, and "the relevant native character set" of §12.3.7.4 GR12 is the national one. Both
    /// native sets are the same UTF-16 repertoire today (implementor item 188), so the members coincide — the flag
    /// is recorded so that a native alphanumeric set that is ever NOT that repertoire cannot silently resolve a
    /// national class's ordinals in the wrong set.</para></summary>
    public Dictionary<string, UserClassDef> UserClasses { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>ALPHANUMERIC alphabet names (case-insensitive) → what the name references (ISO §12.3.7 GR7): the
    /// built collating table of a literal phrase, the LOCALE arm of an <c>IS LOCALE</c> phrase, the identity
    /// (<see cref="AlphabetDef.Native"/>) for NATIVE/STANDARD-1/STANDARD-2 (ISO/IEC 646 order IS the Latin-1 native
    /// order — no table), or an implementor code-name's row (§12.3.7.3 SR15 / §12.3.7.4 GR7 i —
    /// <see cref="ImplementorCodeNames"/>: ASCII, identity like STANDARD-1; EBCDIC, whose 256-entry table the row
    /// carries). An <c>ALPHABET … FOR NATIONAL</c> clause registers in <see cref="NationalAlphabets"/>
    /// instead — the two classes are disjoint reference domains (§12.3.6 SR1/SR2, §14.9.40 GR5).</summary>
    public Dictionary<string, AlphabetDef> Alphabets { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>NATIONAL alphabet names (case-insensitive) → what the name references (ISO §12.3.7 GR7 b/d2/f/g/h/k
    /// + Table 6): a null-table identity for NATIVE and the coded-set names (UCS-4 collates in ISO 10646 order,
    /// which on the D-N1 one-code-unit-per-position substrate IS the native code-unit order — see
    /// <see cref="NationalAlphabetDef"/> for the §8.5.1.4 derivation; UTF-8/UTF-16 name coded character sets ONLY),
    /// or the sparse <see cref="CollatingTable"/> of a literal phrase (the SAME record the alphanumeric arm
    /// builds - one §12.3.7.4 GR7 k model for both classes).</summary>
    public Dictionary<string, NationalAlphabetDef> NationalAlphabets { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Is <paramref name="name"/> an alphabet-name of EITHER class (ISO §12.3.7 ALPHABET clause)? The
    /// question a trailing <c>IN word</c> after a THROUGH range's identifier asks (kb/Work PB843): §8.3.2.2 makes an
    /// alphabet-name a different type of user-defined word from every name a qualifier can be, so a declared
    /// alphabet-name there is §14.9.13.2's <c>IN alphabet-name-1</c> phrase and never a qualifier.</summary>
    public bool IsAlphabetName(string name) => Alphabets.ContainsKey(name) || NationalAlphabets.ContainsKey(name);

    /// <summary>Is <paramref name="name"/> one of the class condition's user-defined-word alternatives — a
    /// class-name-1 or an alphabet-name-1 (ISO §8.8.4.4.2)? The symbol test that separates a bare EVALUATE object
    /// naming a class from identifier-2 (kb/Work PB843).</summary>
    public bool IsClassConditionWord(string name) => UserClasses.ContainsKey(name) || IsAlphabetName(name);

    /// <summary>⛔ THE per-TYPE runtime carriers a THROUGH range's <c>IN alphabet-name-1</c> phrase needs (ISO
    /// §14.7.8 rule 2 — "the collating sequence used for range evaluation is the collating sequence defined by that
    /// alphabet"), keyed by alphabet-name as written. One carrier per NAMED alphabet per runtime module, declared
    /// beside <c>__COLLATE</c> by <c>ObjectComputerEmit</c>.
    /// <para>⛔ A FIELD, NOT AN INLINE CARRIER, and that is a correctness-adjacent performance rule rather than a
    /// preference: a range test sits inside a WHEN pair that is re-analysed on every execution of the EVALUATE, and
    /// rendering <c>new AlphanumericCollation(new ushort[256]{…}, …)</c> there would allocate a fresh 256-entry
    /// table per evaluation. SORT / MERGE / the indexed-file key registrations render their alphabet inline because
    /// each of those runs once per statement; a WHEN does not. The map is populated at BIND, so the field exists
    /// before any body that names it is rendered.</para></summary>
    public Dictionary<string, RangeCollationCarrier> RangeCollations { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Record <paramref name="name"/>'s ALPHANUMERIC alphabet as a range collating sequence — with no
    /// carrier when the alphabet is the native order (an identity sequence needs none: §14.7.8 rule 2's "the
    /// collating sequence defined by that alphabet" IS the native one, and the two-argument
    /// <c>CobolString.Compare</c> overload is it).</summary>
    /// <para>⛔ EVERY ACCEPTED ALPHABET GETS AN ENTRY, the two that need no declaration of their own included — an
    /// identity sequence (entry with a NULL field: the native comparison IS "the collating sequence defined by that
    /// alphabet") and one that IS the program collating sequence (whose <c>__COLLATE</c> is already declared). The
    /// renderer looks the NAME up, so an accepted name with no entry falls back to the category-derived sequence
    /// and silently reinstates the very default the phrase overrides — measured, while these two cases returned
    /// early instead of recording themselves.</para>
    public void RegisterRangeCollation(string name, AlphabetDef def)
        => Register(name, ReferenceEquals(def, Collating) ? "__COLLATE" : null, def.Table, def.Locale, National: false);

    /// <inheritdoc cref="RegisterRangeCollation(string,AlphabetDef)"/>
    public void RegisterRangeCollation(string name, NationalAlphabetDef def)
        => Register(name, ReferenceEquals(def, NationalCollating) ? "__COLLATE_NAT" : null,
            def.Table, def.Locale, National: true);

    /// <summary>⛔ THE ONE resolution of a THROUGH range's <c>IN alphabet-name-1</c> phrase, for EVERY clause that
    /// prints one — §14.9.13.2's range-expression and §13.18.63.2 formats 3 and 5 — because §14.7.8 opens with
    /// "This specification applies to THROUGH phrases specified in the VALUE clause and the EVALUATE statement":
    /// ONE sentence governing both, so one resolver. It enforces both halves of §14.9.13.3 SR3 (§13.18.63.3 SR31 is
    /// the same rule for the VALUE clause) and registers the runtime carrier.
    /// <para>Returns true when the phrase is accepted, in which case <paramref name="alphabetName"/> is what the
    /// bound node carries; false when it was refused, and the range then keeps the implementor default sequence
    /// (§14.7.8 rule 2's no-phrase arm) rather than acquiring a second, invented error mode.</para></summary>
    /// <param name="alphabetName">alphabet-name-1 as written.</param>
    /// <param name="rangeClass">The comparison class of the range's operands, from the ONE comparison-class rule
    /// (<see cref="CollatingSelection.ForComparison"/> over BOTH bounds' categories) — SR3's "of class alphabetic,
    /// alphanumeric, or national" test and its national-vs-alphanumeric split are the same question that rule
    /// answers, so it is asked once and passed in.</param>
    /// <param name="where">The construct, for the diagnostic ("EVALUATE WHEN range", "condition-name 'X'").</param>
    /// <summary>⛔ THE EDITION GATE OF THE <c>[ IN alphabet-name-1 ]</c> RANGE PHRASE — a COBOL-2002 addition
    /// (constructs.json <c>range-in-alphabet-2002</c>; the edge is DERIVED and the derivation is the row's, kb/Work
    /// PB1015). It sits at the one resolver and not in the parse arm because the phrase is decided by SYMBOL: IN is
    /// an optional word (PB983), so `THRU "M" AL` is the phrase only when AL names an alphabet, and an EVALUATE
    /// identifier-4 `IN AL` parses as a qualification until EvaluateBinder peels it (PB843). No recognition-time
    /// arm can see those spellings; every spelling reaches here once it IS the phrase. The VALUE clause's
    /// no-THROUGH refusal (§13.18.63.3 SR31, COBOLNET ValueAlphabetWithoutThrough) calls it too, so a phrase the
    /// binder refuses for another reason still names its edition.</summary>
    internal void GateRangeAlphabetPhrase(string where) =>
        ConstructRegistry.Check(Edition.Edition, Edition.Sink, Constructs.RangeInAlphabet2002, where);

    public bool TryResolveRangeAlphabet(string alphabetName, CollatingClass rangeClass, string where)
    {
        // The TYPE first: a word that names no alphabet at all is not alphabet-name-1, whatever the range's class —
        // and since IN is an optional word (kb/Work PB983), a stray trailing word after a THROUGH range reaches here
        // too, so it must be told it names nothing rather than that the range's class forbids a phrase it never was.
        if (!IsAlphabetName(alphabetName))
        {
            Edition.Error(DiagnosticCatalog.RangeAlphabetUndeclared, $"{where}: '{alphabetName}' after the THROUGH "
                + "phrase is in the [ IN alphabet-name-1 ] position, but it does not name an alphabet declared in "
                + "SPECIAL-NAMES (ISO §12.3.7; §14.9.13.3 SR3)");
            return false;
        }
        GateRangeAlphabetPhrase(where);
        // SR3 sentence 1 — the phrase is admitted only over a range a COLLATING SEQUENCE can order. §14.7.8 rule 1
        // orders a numeric range algebraically and names no sequence at all, and rule 2 (the sequence rule) is
        // written for "alphanumeric or national literals"; a boolean range cannot carry THROUGH in the first place
        // (§14.9.13.3 SR4 / §13.18.63.3 SR29). ⛔ THE SAME PREDICATE THE EC GATES ASK — rule 2 owns the sequence,
        // the IN phrase and the exception together, so the three sites read one antecedent (kb/Work PB401).
        if (!CollatingSelection.IsCollatedThroughRange(rangeClass))
        {
            Edition.Error(DiagnosticCatalog.RangeAlphabetOperandClass, $"{where}: 'IN {alphabetName}' may be "
                + "specified only when the operands of the THROUGH phrase are of class alphabetic, alphanumeric or "
                + "national (ISO §14.9.13.3 SR3 / §13.18.63.3 SR31)");
            return false;
        }
        // SR3 sentence 2 — the alphabet's class shall be the range's. The two domains are disjoint (§12.3.6
        // SR1/SR2), so the lookup itself IS the class test: a national range resolves in NationalAlphabets, an
        // alphanumeric one in Alphabets, and finding the name in the OTHER map is the violation, not an absence.
        bool national = rangeClass is CollatingClass.National;
        if (national)
        {
            if (NationalAlphabets.TryGetValue(alphabetName, out var nd))
            {
                // §12.3.7.4 Table 6 — UTF-8 / UTF-16 name a coded character set only; their collating-sequence
                // column is empty, so they define no sequence this phrase could use.
                if (!nd.HasCollatingSequence)
                {
                    Edition.Error(DiagnosticCatalog.RangeAlphabetClassMismatch, $"{where}: alphabet '{alphabetName}' "
                        + $"({nd.Phrase}) names a national CODED CHARACTER SET only — it defines no national "
                        + "collating sequence (ISO §12.3.7.4 GR7 Table 6; §14.9.13.3 SR3)");
                    return false;
                }
                RegisterRangeCollation(alphabetName, nd);
                return true;
            }
        }
        else if (Alphabets.TryGetValue(alphabetName, out var ad))
        {
            RegisterRangeCollation(alphabetName, ad);
            return true;
        }
        // The name IS an alphabet (the type test above) but not in this class's domain, so it is declared in the
        // other one — SR3 sentence 2's violation, told apart from a word that is no alphabet at all so the message
        // names the rule the program actually broke.
        Edition.Error(DiagnosticCatalog.RangeAlphabetClassMismatch, $"{where}: the THROUGH operands are of class "
            + $"{(national ? "national" : "alphabetic / alphanumeric")}, so alphabet-name-1 shall reference an "
            + $"alphabet that defines {(national ? "a national" : "an alphanumeric")} collating sequence — "
            + $"'{alphabetName}' declares the other class (ISO §14.9.13.3 SR3 / §13.18.63.3 SR31)");
        return false;
    }

    /// <param name="known">The field that ALREADY names this sequence (<c>__COLLATE</c> / <c>__COLLATE_NAT</c>), or
    /// null when a fresh carrier is needed. An identity alphabet takes neither, and is recognised here — from both
    /// components being absent — rather than at each caller.</param>
    private void Register(string name, string? known, CollatingTable? table, LocaleCollatingSpec? locale, bool National)
    {
        if (RangeCollations.ContainsKey(name)) return;
        bool identity = table is null && locale is null;
        // An ORDINAL field name, not one derived from the COBOL word: a user-defined word admits both '-' and '_'
        // (§8.3.2.1), so any sanitizing map could collide two distinct alphabet-names onto one field.
        string? field = identity ? null : known ?? $"__COLLATE_R{RangeCollations.Count}";
        RangeCollations[name] = new RangeCollationCarrier(field, name, table, locale, National,
            Declare: field is not null && known is null);
    }

    /// <summary>Is <paramref name="alphabetName"/> an alphabet "associated with a locale" (ISO §8.8.4.4.3 SR2) /
    /// "specified with the LOCALE phrase" (§12.3.7.3 SR16g / SR17d) — of EITHER class (<c>ALPHABET … IS LOCALE</c>
    /// registers in <see cref="Alphabets"/>, <c>ALPHABET … FOR NATIONAL IS LOCALE</c> in <see cref="NationalAlphabets"/>)?
    /// Such an alphabet defines a collating sequence only (§12.3.7.4 GR7, Table 6: the LOCALE row names no coded
    /// character set), so the ONE question every site that needs a CODED CHARACTER SET asks is this one (kb/Work PB64
    /// T5 — the class condition's alphabet-name-1 today; the CLASS / SYMBOLIC CHARACTERS <c>IN</c> phrases and CODE-SET
    /// when they bind, kb/Work PB110). An undeclared name is not a locale alphabet — its own diagnostic is elsewhere.</summary>
    public bool IsLocaleAlphabet(string alphabetName)
        => (Alphabets.TryGetValue(alphabetName, out var a) && a.Locale is not null)
        || (NationalAlphabets.TryGetValue(alphabetName, out var n) && n.Locale is not null);

    /// <summary>⛔ THE ONE resolver of an alphabet-name referenced where a CODED CHARACTER SET is required (ISO
    /// §12.3.7.4 GR7 Table 6; kb/Work PB110 — the class condition's alphabet-name-1, SYMBOLIC CHARACTERS … IN,
    /// CLASS … IN, CODE-SET): the set of the named alphabet of either class, with the two refusals every site
    /// shares — a LOCALE alphabet defines a collating sequence only (Table 6's blank row → COBOLNET1669, citing
    /// <paramref name="rule"/>), and an undeclared name is COBOLNET0898 naming the site. Null on either refusal.</summary>
    internal CodedCharacterSet? CodedCharacterSetOf(string alphabetName, string site, string rule)
    {
        AlphabetDef? a = Alphabets.TryGetValue(alphabetName, out var av) ? av : null;
        NationalAlphabetDef? n = NationalAlphabets.TryGetValue(alphabetName, out var nv) ? nv : null;
        if (a is null && n is null)
        {
            Edition.Error("COBOLNET0898", $"{site}: '{alphabetName}' is not an alphabet-name declared by a "
                + $"SPECIAL-NAMES ALPHABET clause ({rule}; §12.3.7)");
            return null;
        }
        var set = a is not null ? a.CodedSet : n!.CodedSet;   // a LOCALE alphabet has a null CodedSet — never fall to the OTHER class
        if (set is null)
        {
            Edition.Error(DiagnosticCatalog.LocaleAlphabetNotACharacterSet, $"{site}: the alphabet '{alphabetName}' "
                + $"is associated with a locale — an ALPHABET … IS LOCALE defines a collating sequence, not a coded "
                + $"character set (§12.3.7.4 GR7, Table 6), so it cannot be referenced here ({rule})");
            return null;
        }
        return set;
    }

    /// <summary>SYMBOLIC CHARACTERS figurative constants (ISO §12.3.7.4 GR11 a — "Symbolic-character-1 defines a
    /// figurative constant"; kb/Work PB110): name → the ONE-character value (GR11 b/c — the character at ordinal
    /// integer-1 of the native or IN-alphabet coded character set) and its class. Substituted wherever a figurative
    /// constant may stand — the reference seams consult <see cref="SymbolicOf"/> exactly as they consult the
    /// §13.10 constant table, and the value fills like every figurative (§8.3.3.6.4 GR2 / GR10).</summary>
    public Dictionary<string, (string Value, bool National)> SymbolicCharacters { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>The symbolic character a BARE (unqualified, unsubscripted) data reference names, or null — the
    /// twin of <c>ConstantOf</c> (one shape for "a word that stands for a literal").</summary>
    internal (string Value, bool National)? SymbolicOf(Core.DataReferenceContext dref) =>
        dref.dataReferenceSuffix().Length == 0 && dref.cobolWord() is { } w
        && SymbolicCharacters.TryGetValue(w.GetText(), out var def) ? def : null;

    /// <summary>The symbolic character named by a bare word, or null (the FigurativeOperand ALL-form lookup).</summary>
    internal (string Value, bool National)? SymbolicOf(string word) =>
        SymbolicCharacters.TryGetValue(word, out var def) ? def : null;

    /// <summary>SPECIAL-NAMES <c>ORDER TABLE</c> ordering-names (case-insensitive) → the DECODED literal-9 that
    /// identifies the cultural ordering table (ISO §12.3.7.2's last clause; §12.3.7.4 GR17 — "When ORDER TABLE is
    /// specified, ordering-name-1 shall reference a cultural ordering table that is identified by literal-9 and
    /// constructed in accordance with ISO/IEC 14651:2020, Annex A. The implementor specifies the allowable content
    /// of literal-9"). Read by exactly one consumer, §12.3.7.3 SR9's only legal reference site —
    /// <c>IntrinsicBinder.BindStandardCompare</c> (§15.85.3 r5 / §15.3 argument type 12), which copies the literal
    /// onto the bound call so the backend never has to ask the SPECIAL-NAMES model anything.
    /// <para>⚠ The literal is stored DECODED, not as source text: §12.3.7.3 SR10 admits a national literal
    /// (<c>N"…"</c>) as well as an alphanumeric one, and the runtime resolver matches ordering-table NAMES, not
    /// COBOL literal spellings.</para></summary>
    public Dictionary<string, string> OrderTables { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>SPECIAL-NAMES <c>DYNAMIC LENGTH STRUCTURE</c> declarations (case-insensitive
    /// dynamic-length-structure-name → its layout; ISO §12.3.7.2; kb/Work PB829), read by the DYNAMIC LENGTH clause's
    /// dynamic-length-structure-name-1 (§13.18.19.3 SR2 / SR4). Configuration-section names reach contained source
    /// elements (§8.4.6.1), so the dictionary inherits with its siblings.</summary>
    public Dictionary<string, DynamicLengthStructure> DynamicLengthStructures { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>SPECIAL-NAMES <c>LOCALE locale-name-1 IS {external-locale-name-1 | literal-4}</c> declarations
    /// (case-insensitive locale-name → the symbol; ISO §12.3.7.2; DESIGN-locale-facility seam S1, T1): the locale-name
    /// is a user-defined word (§8.3.2.2) referenced by <c>ALPHABET … IS LOCALE locale-name-2</c> (§12.3.7.3 SR24), SET
    /// LOCALE (§14.9.39.3 SR26), and — in later increments — the LOCALE phrases of PICTURE format 2, LOCALE-COMPARE /
    /// -DATE / -TIME, UPPER-CASE / LOWER-CASE and CHARACTER CLASSIFICATION. The symbol holds the EXTERNAL IDENTIFICATION
    /// only (§8.1.5 — the locale is "determined at runtime"); it inherits into contained units (§12.3.7.4 GR1).</summary>
    public Dictionary<string, LocaleSymbol> Locales { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>The OBJECT-COMPUTER <c>CHARACTER CLASSIFICATION</c> clause (ISO §12.3.6.2; §12.3.6.4 GR5–GR8; kb/Work PB64
    /// T5), or null when none is specified and no containing unit specifies one (GR6 — the coded character set's
    /// classification). Inherited per §12.3.6.4 GR1; consumed by the emitter (resolved at each activation —
    /// <c>__CLASSIFY</c>), by UPPER-CASE / LOWER-CASE without a LOCALE phrase (§15.97.4 r3 / §15.57.4 r3) and by the
    /// ALPHABETIC class tests (§8.8.4.4.4 GR3).</summary>
    public ClassificationSpec? Classification { get; private set; }

    /// <summary>The resolved ALPHANUMERIC PROGRAM COLLATING SEQUENCE (ISO §12.3.6 GR9–GR11) — a literal-phrase table
    /// or a LOCALE sequence — or null when none is specified / the named alphabet is the native order. Drives
    /// relation, condition-name and SORT/MERGE-key comparisons (GR11/GR13), MAX/MIN, ORD/CHAR, and the HIGH-/LOW-VALUE
    /// characters (§8.3.3.6 GR6/GR7). The emitter renders it ONCE as the program's <c>__COLLATE</c> carrier.</summary>
    public AlphabetDef? Collating { get; private set; }

    /// <summary>The resolved NATIONAL PROGRAM COLLATING SEQUENCE (ISO §12.3.6 GR9 — alphabet-name-2 / the FOR
    /// NATIONAL form), or null when none is specified / the named alphabet is an identity sequence (NATIVE, UCS-4 —
    /// the native UTF-16 code-unit order, D-N3). Drives NATIONAL relation and condition-name comparisons (GR11 /
    /// §8.8.4.2.9), CHAR-NATIONAL/ORD-over-national (§15.16.4 / §15.70.4 r2), and the national HIGH-/LOW-VALUE
    /// characters (§12.3.7 GR8/GR9). National SORT/MERGE keys (GR13 / §14.9.40 GR5b) cannot yet EXIST — D-N2
    /// refuses national leaves in FD/SD records and the table-sort national key is staged loud — so no key
    /// consumer reads this yet.</summary>
    public NationalAlphabetDef? NationalCollating { get; private set; }

    /// <summary>DECIMAL-POINT IS COMMA (ISO §12.3.7 GR14 — the decimal and grouping separator characters EXCHANGE
    /// functionality in numeric literals [GR14a] and PICTURE character-strings / edited insertion [GR14b]).
    /// Version-invariant 85→2023 (no VERSION_CHANGE_REFERENCE row). Set BEFORE any literal or PICTURE binds —
    /// SPECIAL-NAMES is walked at the top of <see cref="Bind"/>.</summary>
    public bool DecimalPointIsComma { get; private set; }

    /// <summary>
    /// The unit's CURRENCY SIGN SET (ISO §12.3.7): every currency PICTURE SYMBOL (uppercase-keyed — §12.3.7.3
    /// r20 / §8.1.3 GR3 make the letter cases equivalent) → the currency STRING it stands for (GR13). A bare
    /// <c>CURRENCY SIGN IS literal-7</c> makes literal-7 both string and symbol (r22, one character); the 2002+
    /// <c>WITH PICTURE SYMBOL literal-8</c> form names the one-character symbol (r26/r27) for a string "of any
    /// length" (r23). r25 IMPLIES <c>CURRENCY SIGN '$' PICTURE SYMBOL '$'</c> unless a clause specifies '$' as
    /// literal-7 or literal-8 — so <c>PIC $$9</c> stays legal beside a declared '#', and the set is never empty.
    /// r21: two clauses may not bind equivalent symbols to different strings. Completed by
    /// <see cref="FinalizeCurrencySigns"/> at the end of the SPECIAL-NAMES walk; a contained program inherits the
    /// container's set whole (<see cref="InheritConfiguration"/>, §12.3.4 GR1).
    /// ⛔ THIS REPLACED A SCALAR PAIR (kb/Work PB60 / AR-15.68.3-3): <c>CurrencyPicSymbol</c>/<c>CurrencyString</c>
    /// held ONE symbol and ONE string per unit — a second clause silently overwrote the first, the implied '$'
    /// died the moment any clause bound another symbol (a legal <c>PIC $$,$$9.99</c> under <c>CURRENCY SIGN "#"</c>
    /// was COBOLNET0808), and a multi-character string was refused outright (COBOLNET0896, "not yet supported" —
    /// deferral debt). Every PICTURE consumer now asks this set which symbol a mask uses (<c>PictureAnalyzer</c>
    /// records it on <c>PicInfo.CurrencyString</c> after canonicalizing the mask's symbol to '$'), and NUMVAL-C's
    /// §15.68.3 r3 default is <see cref="SoleCurrencyString"/>.
    /// </summary>
    public IReadOnlyDictionary<char, string> CurrencySigns => _currencySigns;

    private readonly Dictionary<char, string> _currencySigns = new();

    /// <summary>The DISTINCT currency strings the unit's EXPLICIT CURRENCY SIGN clauses specify (r3 of §15.68.3
    /// counts these: "there shall be only one currency string for the compilation unit, either the default
    /// currency sign or a currency string specified in the SPECIAL-NAMES paragraph" — the implied '$' clause of
    /// r25 is the picture-symbol default, not a competing string).</summary>
    private readonly HashSet<string> _explicitCurrencyStrings = new(StringComparer.Ordinal);

    /// <summary>True once a clause named '$' as literal-7 or literal-8 (r25 — the implied clause is then NOT added).</summary>
    private bool _dollarSpecified;

    /// <summary>The ONE currency string NUMVAL-C / TEST-NUMVAL-C use when argument-2 (and LOCALE) is absent
    /// (§15.68.3 r3): the single explicitly specified string, or the default "$" when the unit has no CURRENCY
    /// SIGN clause; NULL when the unit specifies two or more distinct strings — r3's "there shall be only one" is
    /// then violated and the reference is diagnosed at bind (COBOLNET1644).</summary>
    public string? SoleCurrencyString => _explicitCurrencyStrings.Count switch
    {
        0 => "$",
        1 => _explicitCurrencyStrings.First(),
        _ => null,
    };

    /// <summary>The number of distinct explicitly specified currency strings (for the r3 diagnostic's text).</summary>
    public int ExplicitCurrencyStringCount => _explicitCurrencyStrings.Count;

    /// <summary>r25's implied clause: after the SPECIAL-NAMES walk, '$' → "$" joins the set unless a clause
    /// specified '$' as literal-7 or literal-8. Idempotent (a re-walk cannot double it).</summary>
    private void FinalizeCurrencySigns()
    {
        if (!_dollarSpecified) _currencySigns.TryAdd('$', "$");
    }

    /// <summary>Normalize a NUMERIC literal's source text to the canonical dot-decimal form the whole emit-side
    /// decode pipeline consumes (ISO §12.3.7 GR14a: under DECIMAL-POINT IS COMMA "the character written in
    /// numeric literals to represent the decimal separator shall be the comma" — and §8.3.3.3.2 admits ONLY the
    /// decimal point in a fixed-point literal, so the OTHER separator is diagnosed in each mode; the legacy's
    /// unconditional acceptance of both is a version-invariant non-conformance, not ported). The ONE literal
    /// chokepoint: expression paths route through <c>StatementBinder.CheckLiteral</c>, VALUE / level-88 capture
    /// call this directly.</summary>
    public string NormalizeNumericLiteral(string text)
    {
        // The normalization ALGORITHM is the ONE shared CobolNet.Common.NumericLiteral.Normalize (Frontend), so the
        // compile-time expression evaluator applies the identical §12.3.7 GR14a rule; the binder retains ownership
        // of the COBOLNET0895 diagnostic (its own channel/descriptor), routing the returned issue to it verbatim.
        string norm = Common.NumericLiteral.Normalize(text, DecimalPointIsComma, out var issue);
        switch (issue)
        {
            case Common.NumericSeparatorIssue.DecimalPointUnderCommaMode:
                Edition.Error(DiagnosticCatalog.NumericLiteralDecimalSeparator,
                    $"numeric literal '{text}': under DECIMAL-POINT IS COMMA the "
                    + "decimal separator is the comma (ISO §12.3.7.4 GR14a); '.' is not valid in a numeric literal");
                break;
            case Common.NumericSeparatorIssue.CommaWithoutCommaMode:
                Edition.Error(DiagnosticCatalog.NumericLiteralDecimalSeparator,
                    $"numeric literal '{text}': a comma decimal separator requires "
                    + "DECIMAL-POINT IS COMMA (ISO §12.3.7.4 GR14a; §8.3.3.3.2 admits only '.' as the decimal point)");
                break;
        }
        // §8.3.3.3.3 SR2/SR3/SR4 (kb/Work PB99) — the floating-point literal's FORM, checked HERE because both the
        // expression funnel (ExpressionBinder.CheckLiteral) and the VALUE / level-88 funnel normalize through this method.
        if (Common.NumericLiteral.IsFloatingPointForm(norm)
            && Common.NumericLiteral.CheckFloatingPointForm(norm) is var fissue && fissue != Common.NumericLiteral.FloatingLiteralIssue.None)
            Edition.Error(DiagnosticCatalog.FloatingLiteral, $"floating-point numeric literal '{text}': " + fissue switch
            {
                Common.NumericLiteral.FloatingLiteralIssue.SignificandDigits => "the significand shall be from 1 to 36 digits in length (ISO §8.3.3.3.3 SR2)",
                Common.NumericLiteral.FloatingLiteralIssue.ExponentDigits => "the exponent shall have a maximum of four digits (ISO §8.3.3.3.3 SR3)",
                _ => "when all the digits of the significand are zero, all the digits of the exponent shall be zero and neither part shall have a negative sign (ISO §8.3.3.3.3 SR4)",
            });
        return norm;   // canonical dot-decimal; a diagnosed literal is normalized too so downstream decode stays well-formed
    }

    /// <summary>Normalize <paramref name="text"/> when it is a numeric literal (digits with optional sign and
    /// separators); any other operand text (quoted literal, figurative word, data-name) passes through. Used by
    /// the VALUE-clause and level-88 capture paths, whose operand texts are not yet classified.</summary>
    internal string NormalizeIfNumericLiteral(string text)
    {
        bool anyDigit = false, sawE = false;
        foreach (char c in text)
        {
            if (char.IsAsciiDigit(c)) { anyDigit = true; continue; }
            // the floating-point form's ONE exponent marker (§8.3.3.3.3) — its significand normalizes like any literal
            if ((c is 'E' or 'e') && !sawE && anyDigit) { sawE = true; continue; }
            if (c is not ('+' or '-' or '.' or ',')) return text;
        }
        return anyDigit ? NormalizeNumericLiteral(text) : text;
    }

    /// <summary>The OPTIONS baseline a contained program starts from — its container's model (ISO §11.9.4 GR1:
    /// "The clauses in the OPTIONS paragraph apply to the source element in which they are specified and to all
    /// source elements contained in that source element unless overridden by a clause in an OPTIONS paragraph in
    /// a contained source element"). Null for an outermost unit (the all-defaults model).</summary>
    private OptionsModel? _inheritedOptions;

    /// <summary>
    /// ⛔ ISO §12.3.4 GR1 — "The entries explicitly or implicitly specified in the configuration section of a
    /// source unit that contains other source units apply to each directly or indirectly contained source unit"
    /// — and §12.3.3 SR1 forbids the containee a configuration section of its own, so a contained program CANNOT
    /// restate DECIMAL-POINT IS COMMA, CURRENCY SIGN, a CLASS, an ALPHABET, the PROGRAM COLLATING SEQUENCE or a
    /// switch and is entitled to inherit every one of them. Called by <c>BinderDriver.BindUnitData</c> for every
    /// contained unit BEFORE <see cref="Bind"/> (the SPECIAL-NAMES state must be in place before the containee's
    /// first literal or PICTURE binds); the container itself inherited from ITS container first (units bind
    /// container-first), so one level carries the whole ancestry. ONE method, the WHOLE configuration-derived
    /// state (feedback_model_the_rule_shape_not_one_case): before this landed only the REPOSITORY sets were
    /// inherited, and inside a contained program of a DECIMAL-POINT IS COMMA unit NUMVAL("123,45") valued 0
    /// while NUMVAL("123.45") valued 123.45 — the exact inversion of §15.67.3 r5, with no diagnostic
    /// (kb/Work PB60 / AR-15.67.3-5). §11.9.4 GR1's OPTIONS inheritance rides the same call.
    /// <para>⚠ One sibling still lives elsewhere: EVERY SPECIAL-NAMES mnemonic-name — the switch mnemonics SET reads
    /// as much as the device and feature mnemonics DISPLAY UPON / ACCEPT FROM / WRITE ADVANCING read (kb/Work
    /// PB454 moved the switch ones there rather than keep a second copy here) — inherits through
    /// <c>MnemonicRegistry.Of</c>'s enclosing-unit PARSE-TREE walk: correct, but a second mechanism for the same
    /// rule; fold it in here when the environment division gets a bound model.</para>
    /// </summary>
    internal void InheritConfiguration(DataBinder container)
    {
        // SPECIAL-NAMES (§12.3.7)
        DecimalPointIsComma = container.DecimalPointIsComma;
        foreach (var (k, v) in container._currencySigns) _currencySigns.TryAdd(k, v);
        _explicitCurrencyStrings.UnionWith(container._explicitCurrencyStrings);
        _dollarSpecified = container._dollarSpecified;
        foreach (var (k, v) in container.SwitchConditions) SwitchConditions.TryAdd(k, v);
        foreach (var (k, v) in container.UserClasses) UserClasses.TryAdd(k, v);
        foreach (var (k, v) in container.Alphabets) Alphabets.TryAdd(k, v);
        foreach (var (k, v) in container.NationalAlphabets) NationalAlphabets.TryAdd(k, v);
        foreach (var (k, v) in container.OrderTables) OrderTables.TryAdd(k, v);
        foreach (var (k, v) in container.DynamicLengthStructures) DynamicLengthStructures.TryAdd(k, v);   // §8.4.6.1
        foreach (var (k, v) in container.Locales) Locales.TryAdd(k, v);   // §12.3.7.4 GR1 — locale-names reach contained units
        // OBJECT-COMPUTER … PROGRAM COLLATING SEQUENCE (§12.3.6 GR9–GR11) and CHARACTER CLASSIFICATION (GR1 — every
        // OBJECT-COMPUTER clause applies to the contained units)
        Collating = container.Collating;
        Classification = container.Classification;
        NationalCollating = container.NationalCollating;
        // SOURCE-COMPUTER … WITH DEBUGGING MODE (the '85 compile-time switch)
        DebuggingModeDeclared = container.DebuggingModeDeclared;
        // REPOSITORY (§12.3.8) — the M2-UDF-1/-4 inheritance, now here with its siblings
        UserFunctionNames.UnionWith(container.UserFunctionNames);
        // §12.3.8.2's program-specifiers reach contained units on the same rule as their siblings (kb/Work PB237):
        // §12.3.8.4 GR10 scopes program-prototype-name-1 "throughout the scope of the containing environment
        // division", and a contained program with no CONFIGURATION SECTION of its own is inside that scope. The
        // containee's OWN specifier wins on a name clash — TryAdd, the sibling posture, and the one §12.3.8.3 SR15
        // shape that matters here (the containee may re-specify a name the container also declares).
        foreach (var (k, v) in container.ProgramSpecifiers) ProgramSpecifiers.TryAdd(k, v);
        RepositoryIntrinsics.UnionWith(container.RepositoryIntrinsics);
        if (container.RepositoryAllIntrinsic) RepositoryAllIntrinsic = true;
        foreach (var (k, v) in container.OoRepositoryProperties) OoRepositoryProperties.TryAdd(k, v);
        foreach (var (k, v) in container.FunctionSpecifiers) FunctionSpecifiers.TryAdd(k, v);
        // OPTIONS (§11.9.4 GR1) — the containee's own OPTIONS paragraph overrides clause by clause at Bind.
        _inheritedOptions = container.Options;
    }

    /// <summary>Bind <c>DECIMAL-POINT IS COMMA</c> (ISO §12.3.7 — the only word the format admits after IS is
    /// COMMA; the grammar carries it as a generic IDENTIFIER).</summary>
    private void SwitchBindDecimalPoint(Core.DecimalPointClauseContext dp)
    {
        if (dp.IDENTIFIER()?.GetText() is { } word && word.Equals("COMMA", StringComparison.OrdinalIgnoreCase))
            DecimalPointIsComma = true;
        else
            Edition.Error("COBOLNET0894", $"DECIMAL-POINT IS {dp.IDENTIFIER()?.GetText()}: the only form is "
                + "DECIMAL-POINT IS COMMA (ISO §12.3.7)");
    }

    /// <summary>⛔ THE SYNTAX RULES A CURRENCY SIGN LITERAL IS HELD TO, as data — one value per operand, the way
    /// <see cref="LiteralPhraseRules"/> does it for the ALPHABET / CLASS clauses (kb/Work PB791; it used to be a
    /// <c>rule</c> number glued onto one message, which could not say that the two operands carry DIFFERENT
    /// restrictions). §12.3.7.3 SR18 — "<i>Literal-7 shall be an alphanumeric or national literal that is not a
    /// figurative constant</i>" — and SR26 — "<i>Literal-8 shall be an alphanumeric or national literal consisting of
    /// a single character from the computer's compile-time coded character set. It shall be neither a figurative
    /// constant nor a hexadecimal literal</i>" (the one-character half is a LENGTH rule, asked of the decoded
    /// characters by <see cref="SwitchBindCurrency"/>; everything here is about what the operand IS as written).</summary>
    /// <param name="Name">The standard's name for the operand.</param>
    /// <param name="Rule">The syntax rule, "SR18" / "SR26".</param>
    /// <param name="Requirement">The rule's own words, for the diagnostic.</param>
    /// <param name="RefusesHexadecimal">True when a literal in hexadecimal format is refused outright (SR26). A
    /// hexadecimal literal-7 is the opposite: SR19 REQUIRES the PICTURE SYMBOL phrase for it and NOTE 1 exempts its
    /// content from the compile-time currency-string limitations.</param>
    private sealed record CurrencyOperand(string Name, string Rule, string Requirement, bool RefusesHexadecimal)
    {
        public static CurrencyOperand Literal7 { get; } = new("literal-7", "SR18",
            "an alphanumeric or national literal that is not a figurative constant", RefusesHexadecimal: false);

        public static CurrencyOperand Literal8 { get; } = new("literal-8", "SR26",
            "an alphanumeric or national literal that is neither a figurative constant nor a hexadecimal literal",
            RefusesHexadecimal: true);
    }

    /// <summary>The characters of a CURRENCY SIGN clause literal, or null when it is not a literal the operand's
    /// rule admits (<see cref="CurrencyOperand"/>): a NUMERIC literal (⛔ silently ACCEPTED until kb/Work PB770 — the
    /// clause called the CLASS clause's ordinal helper, so <c>CURRENCY SIGN IS 65</c> became the currency symbol 'A'),
    /// a BOOLEAN one, a FIGURATIVE constant (SR18 for literal-7 and SR26 for literal-8 — ⛔ accepted until kb/Work
    /// PB791 because the screen was class-only and <c>LiteralCharsOf</c> decoded a figurative constant as its own
    /// SPELLING: <c>CURRENCY SIGN IS SPACE PICTURE SYMBOL "#"</c> bound the currency string "SPACE"), the predefined
    /// NULL (an identifier, §8.4.3.10.3 SR1 — never a literal), and, for literal-8, a hexadecimal literal (SR26, whatever
    /// it decodes to). The decision is made from the TOKEN as written, never from the decoded characters.</summary>
    private string? CurrencyTextLiteral(Core.LiteralContext lit, CurrencyOperand op)
    {
        var nn = lit.nonNumericLiteral();
        if (nn?.predefinedNull() is not null)
        {
            PredefinedNullRule.Report(Edition, $"the {op.Name} of a CURRENCY SIGN clause — {op.Requirement} "
                + $"(ISO §12.3.7.3 {op.Rule})");
            return null;
        }
        // The literal's CLASS is the grammar's answer (§8.3.3.3.1: "Numeric literals are of the class and category
        // numeric" — the lexer's numeric-literal rule decides it), never a C# parse of its text (kb/Work PB1579):
        // `int.TryParse || decimal.TryParse` answered "not numeric" for a 30- or 31-digit numeric literal (beyond
        // decimal's range), which then fell into the alphanumeric path and was refused under the wrong rule.
        string? what = nn?.figurativeConstant() is not null ? "a figurative constant"
            : lit.numericLiteral() is not null ? "a numeric literal"
            : op.RefusesHexadecimal && nn?.concatenationExpression() is null && CobolLiteral.IsHexadecimalFormat(lit.GetText())
                ? "a hexadecimal literal"
            : OperandLiteralClass(lit) is LiteralClass.Boolean ? "a boolean literal"
            : null;
        if (what is null) return LiteralCharsOf(lit, sr11: false);   // CURRENCY: no NATIONAL phrase; SR11 names no literal-7/-8
        Edition.Error("COBOLNET0892", $"CURRENCY SIGN {ConcatFolder.Spelling(lit)}: {op.Name} shall be {op.Requirement} — "
            + $"this is {what} (ISO §12.3.7.3 {op.Rule})");
        return null;
    }

    /// <summary>Bind <c>CURRENCY [SIGN] [IS] literal-7 [WITH PICTURE SYMBOL literal-8]</c> (ISO §12.3.7.3
    /// r18–r27) into the currency SET. Bare form: literal-7 is both currency string and symbol — a single
    /// character outside the r22 forbidden set. PICTURE SYMBOL form (2002+ — the 85 standard had only the
    /// single-character form): literal-7 is the currency string ("may have any length", r23 — at least one
    /// non-space character, none of <c>0-9 + - , . *</c>), literal-8 the one-character symbol (r26/r27; a
    /// hexadecimal literal-7 requires this form, r19). r21: two clauses may not give equivalent symbols
    /// different strings. r25's implied '$' is added by <see cref="FinalizeCurrencySigns"/>.</summary>
    private void SwitchBindCurrency(Core.CurrencySignClauseContext cur)
    {
        var lits = cur.literal();
        if (lits.Length == 0) return;
        if (CurrencyTextLiteral(lits[0], CurrencyOperand.Literal7) is not { } literal7) return;
        // SR19: a hexadecimal literal-7 (X"…" / NX"…") requires the PICTURE SYMBOL phrase — and NOTE 1 exempts its
        // content from the compile-time currency-string limitations (SR23 b). Detected on the TOKEN's form (the
        // literal's decoded characters cannot tell the forms apart), by the ONE hexadecimal-format question; a
        // concatenation expression is "equivalent to a literal" (§8.8.3.3 GR3), not a literal in hexadecimal format.
        bool literal7Hex = lits[0].nonNumericLiteral()?.concatenationExpression() is null
            && CobolLiteral.IsHexadecimalFormat(lits[0].GetText());

        char symbol;
        string currencyString;
        if (cur.PIC() is not null)
        {
            // currency-picture-symbol-2002 (§12.3.7): the pass owns the edition gate (Exec Step E).
            // PICMODE exploit: the word between PICTURE and literal-8 arrives as a PIC_STRING token — it must be
            // the keyword SYMBOL (the grammar cannot distinguish; semantic check per the grammar's own note).
            if (cur.PIC_STRING()?.GetText() is { } sym && !sym.Equals("SYMBOL", StringComparison.OrdinalIgnoreCase))
                Edition.Error("COBOLNET0892", $"CURRENCY SIGN: expected 'WITH PICTURE SYMBOL', found 'PICTURE {sym}' (ISO §12.3.7)");
            string? literal8 = lits.Length > 1 ? CurrencyTextLiteral(lits[1], CurrencyOperand.Literal8) : "";
            if (literal8 is null) return;
            if (literal8.Length != 1)
            {
                Edition.Error("COBOLNET0892", "CURRENCY SIGN: the PICTURE SYMBOL literal shall be a single "
                    + "character (ISO §12.3.7.3 r26)");
                return;
            }
            // r23a/b — the currency STRING's content (its length is free).
            if (literal7.Trim().Length == 0)
            {
                Edition.Error("COBOLNET0890", "CURRENCY SIGN: the currency string shall contain at least one "
                    + "non-space character (ISO §12.3.7.3 r23a)");
                return;
            }
            // SR23 b is a compile-time limitation on the characters of a currency string, which SR19's NOTE 1 says
            // does not reach "the alphanumeric or national characters represented by hexadecimal literals used as
            // currency strings" — their content is the runtime coded character set's, used exactly as written.
            if (!literal7Hex && literal7.Any(c => char.IsAsciiDigit(c) || c is '+' or '-' or ',' or '.' or '*'))
            {
                Edition.Error("COBOLNET0890", $"CURRENCY SIGN \"{literal7}\": a currency string may not contain the "
                    + "digits 0 through 9 or the characters '+' '-' ',' '.' '*' (ISO §12.3.7.3 r23b)");
                return;
            }
            symbol = literal8[0];
            currencyString = literal7;
            ValidateCurrencyChar(symbol, "PICTURE SYMBOL literal");   // r27 — same forbidden set as r22
        }
        else
        {
            // Bare form (the COBOL-85 surface): r22 — one character, both string and symbol.
            if (literal7Hex)
                Edition.Error("COBOLNET0890", "CURRENCY SIGN: a hexadecimal currency literal requires the PICTURE "
                    + "SYMBOL phrase (ISO §12.3.7.3 r19)");
            if (literal7.Length != 1)
            {
                Edition.Error("COBOLNET0890", $"CURRENCY SIGN \"{literal7}\": without PICTURE SYMBOL the literal "
                    + "shall consist of a single character (ISO §12.3.7.3 r22)");
                return;
            }
            symbol = literal7[0];
            currencyString = literal7;
            ValidateCurrencyChar(symbol, "CURRENCY SIGN literal");
        }

        char key = char.ToUpperInvariant(symbol);
        // r21: no two clauses may bind equivalent symbols to different strings.
        if (_currencySigns.TryGetValue(key, out string? bound) && bound != currencyString)
        {
            Edition.Error("COBOLNET0891", $"CURRENCY SIGN: the symbol '{symbol}' is already bound to "
                + $"\"{bound}\" (ISO §12.3.7.3 r21)");
            return;
        }
        _currencySigns[key] = currencyString;
        _explicitCurrencyStrings.Add(currencyString);
        if (currencyString == "$" || key == '$') _dollarSpecified = true;   // r25 — no implied clause
    }

    /// <summary>The ISO §12.3.7.3 SR22/SR27 forbidden currency-symbol set: digits; A B C D E N P R S V X Z (either
    /// case) or space; and <c>+ - , . * / ; ( ) " =</c>.</summary>
    private void ValidateCurrencyChar(char c, string what)
    {
        bool forbidden = char.IsAsciiDigit(c)
            || char.ToUpperInvariant(c) is 'A' or 'B' or 'C' or 'D' or 'E' or 'N' or 'P' or 'R' or 'S' or 'V' or 'X' or 'Z'
            || c is ' ' or '+' or '-' or ',' or '.' or '*' or '/' or ';' or '(' or ')' or '"' or '=';
        if (forbidden)
            Edition.Error("COBOLNET0891", $"{what} '{c}': not a valid currency symbol — digits, the picture "
                + "letters A B C D E N P R S V X Z, space, and + - , . * / ; ( ) \" = are excluded (ISO §12.3.7.3 SR22/SR27)");
    }

    // ── The §12.3.8.2 program-specifier (kb/Work PB237) ──────────────────────────────────────────────────────
    /// <summary>Bind ONE <c>PROGRAM program-prototype-name-1 [AS literal-3]</c> entry of the REPOSITORY paragraph
    /// (ISO §12.3.8.2's program-specifier, PDF page 334). Two declaration rules apply and both are checked here:
    /// §12.3.8.3 syntax rule 2 (literal-3's class), and syntax rule 1 — "If any … program-prototype-name-1 … is
    /// specified more than once in the REPOSITORY paragraph, all the specifications for that name shall be
    /// identical", which for a program-specifier means the same externalized name.
    /// <para>What the specifier RESOLVES to — §12.3.8.4 GR10's a)/b)/c) search of the compilation group, and
    /// §12.3.8.3 SR15's "this specifier is ignored" for the unit's own or a containing program's name — needs the
    /// unit's identity and its siblings, which this binder does not have. Those run in
    /// <c>BinderDriver.ProgramPrototypesOf</c>, exactly where the function twin's GR11 search runs
    /// (<c>BuildUserFunctionTable</c>). Collecting the SYNTAX here and resolving it there is the same split.</para></summary>
    private void BindProgramSpecifier(Core.RepositoryEntryContext re, string name)
    {
        // A rejected literal-3 registers NO prototype (the PB237 posture — a CALL through it then draws the
        // undeclared-prototype diagnostic rather than silently activating the declared word).
        if (BindSpecifierExternalizedName(re, "PROGRAM", name, "literal-3") is { } externalized)
            ProgramSpecifiers.TryAdd(name, new ProgramSpecifier(name, externalized));
    }

    /// <summary>⛔ THE ONE SCREEN OF A REPOSITORY SPECIFIER'S <c>[ AS literal-n ]</c> — class (literal-1), interface
    /// (literal-2), program (literal-3), property (literal-4) and user-defined function (literal-5), §12.3.8.2
    /// (kb/Work PB974; the program specifier had it alone, PB237). Returns the externalized name — the literal's
    /// value, or the declared name when there is no AS phrase (§12.3.8.4 GR10 NOTE 1 / GR11 NOTE 2) — or null when
    /// the literal was rejected (and reported).
    /// <para>§12.3.8.3 SR2: "Literal-1, literal-2, literal-3, literal-4, and literal-5 shall be alphanumeric literals
    /// or national literals and shall be neither figurative constants nor zero-length literals" — the SAME sentence
    /// the identification paragraphs restate, so the ONE <c>ExternalizedName.Screen</c> (kb/Work PB303). SR1 is asked
    /// of the WHOLE specification by <see cref="CheckRepositorySpecification"/> (kb/Work PB1017).</para></summary>
    private string? BindSpecifierExternalizedName(Core.RepositoryEntryContext re, string kind, string name, string tag)
    {
        using var _ = Edition.At(re);   // the sink stamps every report below with THIS entry's position (PB82)
        string externalized = name;
        if (re.externalizedNamePhrase() is { } asPhrase)
        {
            if (ExternalizedName.Screen(asPhrase.literal(), Edition, DiagnosticCatalog.RepositoryProgramSpecifier,
                    $"REPOSITORY {kind} '{name}' AS {asPhrase.literal().GetText()}", tag, "ISO §12.3.8.3 SR2",
                    LiteralEnv) is not { } literal) return null;
            externalized = literal;
        }
        var spec = new RepositorySpecification(kind, re.externalizedNamePhrase() is null ? null : externalized,
            ExpandsText(re.expandsPhrase()));
        return CheckRepositorySpecification(name, spec, externalized) ?? externalized;
    }

    /// <summary>ONE REPOSITORY specification of a name, as §12.3.8.3 SR1 compares it: the specifier KIND (class,
    /// interface, program, property, user-defined function, or intrinsic function), the value of its
    /// <c>[ AS literal-n ]</c> (null when absent — the name is then externalized by the implementor's mapping of the
    /// user-defined word, §8.3.2.2, so two case spellings of one word are one specification), and the canonical
    /// text of its <c>[ EXPANDS … USING … ]</c> phrase (null when absent). Every part of the written specification
    /// that carries a meaning is a component, so "identical" is record equality.</summary>
    private sealed record RepositorySpecification(string Kind, string? As, string? Expands)
    {
        public override string ToString() =>
            $"{Kind}{(As is null ? "" : $" AS \"{As}\"")}{(Expands is null ? "" : " " + Expands)}";
    }

    /// <summary>The kind an intrinsic-function-name-1 of <c>FUNCTION { intrinsic-function-name-1 } … INTRINSIC</c>
    /// is specified with.</summary>
    private const string IntrinsicKind = "FUNCTION … INTRINSIC";

    /// <summary>⛔ THE ONE §12.3.8.3 SR1 CHECK, OVER THE WHOLE SPECIFICATION (kb/Work PB1017). "If any
    /// object-class-name-1, interface-name-2, program-prototype-name-1, function-prototype-name-1,
    /// intrinsic-function-name-1 or property-name-1 is specified more than once in the REPOSITORY paragraph, all
    /// the specifications for that name shall be identical." The rule is keyed on THE NAME — not on (kind, name) —
    /// and compares everything the specification says: its kind, its externalized name, and its EXPANDS phrase.
    /// It used to compare the externalized name alone, per kind, so `CLASS QC EXPANDS QP USING QA` then
    /// `CLASS QC EXPANDS QP USING QB` (two different classes created under one name, §12.3.8.4 GR5), a name
    /// specified as a CLASS and as an INTERFACE, and `FUNCTION F INTRINSIC` beside `FUNCTION F` were all accepted.
    /// The externalized name is compared ordinally — an operating-environment name, not a COBOL word — and the
    /// declared name case-insensitively (§8.3.2). Returns the FIRST specification's externalized name when this one
    /// repeats a name (after reporting a difference), null when the name is new.</summary>
    private string? CheckRepositorySpecification(string name, RepositorySpecification spec, string externalized)
    {
        string key = name.ToUpperInvariant();
        if (_repositorySpecified.TryGetValue(key, out var first))
        {
            var (prior, priorExternalized) = first;
            if (prior != spec)
                Edition.Error(DiagnosticCatalog.RepositoryProgramSpecifier,
                    $"REPOSITORY '{name}' is specified more than once with different specifications ('{prior}' then "
                    + $"'{spec}'); ISO §12.3.8.3 syntax rule 1 requires all the specifications for one name to be "
                    + "identical");
            return priorExternalized;
        }
        _repositorySpecified[key] = (spec, externalized);
        return null;
    }

    /// <summary>The canonical text of an EXPANDS phrase — <c>EXPANDS target USING actual …</c>, each word
    /// upper-cased (§8.3.2: a user-defined word is case-insensitive), so two spellings of one phrase compare equal.</summary>
    private static string? ExpandsText(Core.ExpandsPhraseContext? ep) => ep is null ? null
        : $"EXPANDS {ep.expandsTarget().GetText().ToUpperInvariant()} USING "
          + string.Join(" ", ep.expandsActual().Select(a => a.GetText().ToUpperInvariant()));

    /// <summary>Upper-cased declared name → its FIRST specification and the externalized name it gave: the
    /// §12.3.8.3 SR1 memory, for THIS unit's REPOSITORY.</summary>
    private readonly Dictionary<string, (RepositorySpecification Spec, string Externalized)> _repositorySpecified = [];


    /// <summary>Populate the switch registry from the SPECIAL-NAMES paragraph's switch-name clauses (ISO §12.3.7
    /// general format: <c>switch-name-1 [IS mnemonic-name-1] [ON [STATUS] [IS] condition-name-1]
    /// [OFF [STATUS] [IS] condition-name-2]</c>; the NIST-85 surface also writes <c>ON IS cond</c> with no STATUS —
    /// both shapes are one grammar rule). Only the switch-names <see cref="ImplementorNames"/> makes available are
    /// accepted (§12.3.7.3 SR8, Annex A.1 item 191 — kb/Work PB862).</summary>
    private void SwitchBindSpecialNames(Core.ProgramUnitContext program)
    {
        Core.ProgramCollatingSequenceClauseContext? pcsClause = null;
        Core.CharacterClassificationClauseContext? ccClause = null;
        var classClauses = new List<Core.ClassDefinitionClauseContext>();
        var symbolicClauses = new List<Core.SymbolicCharactersClauseContext>();
        var currencyClauses = new List<Core.CurrencySignClauseContext>();
        // The names THIS source element declares — a containing element's inherited names may be re-declared
        // (§8.4.6.1: each source element "may use identical user-defined words"), its own may not (§8.4.2.1).
        var ownDls = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var para in EnvDivisions(program)
                     .SelectMany(env => env.configurationSection()?.configurationParagraph() ?? []))
        {
            // OBJECT-COMPUTER … PROGRAM COLLATING SEQUENCE (ISO §12.3.6 — the 85 single-name IS form, the 2002
            // two-name IS form, and the 2002 FOR ALPHANUMERIC/FOR NATIONAL forms; resolved AFTER the walk). The
            // paragraph's clauses are a list since kb/Work PB78 (either clause, either order, with or without a
            // computer-name); §5.2.6.4 admits each at most once — a second is COBOLNET1652.
            if (para.objectComputerParagraph() is { } ocp)
            {
                foreach (var oc in ocp.objectComputerClause())
                {
                    using var _ = Edition.At(oc);
                    if (oc.programCollatingSequenceClause() is { } pcs)
                    {
                        if (pcsClause is not null)
                            Edition.Error(DiagnosticCatalog.ObjectComputerDuplicateClause,
                                "the OBJECT-COMPUTER PROGRAM COLLATING SEQUENCE clause is specified more than once "
                                + "(ISO §12.3.6.2 — the paragraph's clauses may each appear at most once, §5.2.6.4)");
                        pcsClause = pcs;
                    }
                    // §12.3.6 CHARACTER CLASSIFICATION (Annex A.4.9 item 7) — IMPLEMENTED since kb/Work PB64 T5 (it was
                    // refused by name with COBOLNET1518 since PB78, and swallowed by the attribute sink before that).
                    // Saved and resolved AFTER the SPECIAL-NAMES paragraph, whose LOCALE clauses declare the names it
                    // references (§12.3.6.3 SR3) — the PCS takes the same path. A second clause is COBOLNET1652.
                    if (oc.characterClassificationClause() is { } cc)
                    {
                        if (ccClause is not null)
                            Edition.Error(DiagnosticCatalog.ObjectComputerDuplicateClause,
                                "the OBJECT-COMPUTER CHARACTER CLASSIFICATION clause is specified more than once "
                                + "(ISO §12.3.6.2 — the paragraph's clauses may each appear at most once, §5.2.6.4)");
                        ccClause = cc;
                    }
                }
            }
            if (para.specialNamesParagraph() is not { } sn) continue;
            // The paragraph's symbolic-character NAMES, read syntactically before any clause binds: the clauses are
            // order-free, so an ALPHABET clause may precede the SYMBOLIC CHARACTERS clause that declares a name it
            // writes — and §12.3.7.3 SR11 must still recognize it (kb/Work PB226).
            _paragraphSymbolicNames = sn.specialNameEntry()
                .Select(e => e.symbolicCharactersClause()).OfType<Core.SymbolicCharactersClauseContext>()
                .SelectMany(sc => sc.symbolicCharacterEntry()).SelectMany(e => e.cobolWord())
                .Select(w => w.GetText()).ToHashSet(StringComparer.OrdinalIgnoreCase);
            // ⛔ DEPENDENCY ORDER, NEVER SOURCE ORDER (kb/Work PB1558). A clause that REFERENCES a name another clause
            // of this paragraph DECLARES binds after every declaring clause, whatever the source order:
            //   1. the walk below — the clauses that reference no paragraph name (LOCALE declares locale-names,
            //      ORDER TABLE ordering-names, the dynamic-length-structure clause its names, …);
            //   2. ALPHABET — references locale-names (§12.3.7.3 SR24 "Locale-name-2 shall be a locale-name defined
            //      by the LOCALE clause") and, syntactically, symbolic-character names (SR11 — read above);
            //   3. after the whole configuration section, CLASS / SYMBOLIC CHARACTERS (their IN alphabet-name), then
            //      CURRENCY SIGN (a symbolic-character concatenation operand in literal-7 — kb/Work PB1628), and
            //      the OBJECT-COMPUTER clauses (PROGRAM COLLATING SEQUENCE's alphabets, CHARACTER CLASSIFICATION's
            //      locale-names).
            // The general format itself PRINTS alphabet-name-clause BEFORE the LOCALE clause (§12.3.7.2; §5.2.1
            // "shall be written in the compilation group in the sequence given in the general format"), so an
            // ALPHABET … IS LOCALE locale-name-2 is a FORWARD reference in every conforming program — and binding
            // ALPHABET in source order refused every one of them with COBOLNET1664 "no LOCALE clause is in scope".
            var alphabetClauses = new List<Core.AlphabetClauseContext>();
            foreach (var entry in sn.specialNameEntry())
            {
                using var _ = Edition.At(entry);
                // §12.3.7 LOCALE clause (Annex A.4.9 item 10, the clause half) — IMPLEMENTED since kb/Work PB64 T1: the
                // locale-name is DECLARED (DataBinder.LocaleBind). It used to be refused by name with COBOLNET1518
                // (PB25 → T0), and before that a raw parse error at the clause's own literal.
                if (entry.localeClause() is { } loc) { LocaleBind(loc); continue; }
                if (entry.orderTableClause() is { } ot) { OrderTableBind(ot); continue; }
                if (entry.dynamicLengthStructureClause() is { } dls) { DynamicLengthStructureBind(dls, ownDls); continue; }
                if (entry.alphabetClause() is { } alpha) { alphabetClauses.Add(alpha); continue; }
                // CLASS and SYMBOLIC CHARACTERS bind AFTER the walk (kb/Work PB110): their IN alphabet-name may be
                // declared by a LATER ALPHABET clause of the same paragraph (the clauses are order-free — the
                // ResolveProgramCollating shape).
                if (entry.classDefinitionClause() is { } cd) { classClauses.Add(cd); continue; }
                if (entry.symbolicCharactersClause() is { } sc) { symbolicClauses.Add(sc); continue; }
                if (entry.decimalPointClause() is { } dp) { SwitchBindDecimalPoint(dp); continue; }
                // CURRENCY SIGN binds after SYMBOLIC CHARACTERS (kb/Work PB1628): literal-7 may be a concatenation
                // expression (§8.8.3.3 GR3 makes it "equivalent to a literal"; §12.3.7.3 SR18 bars only a figurative
                // constant AS literal-7, and SR11's symbolic-character bar does not name literal-7/8), and a
                // symbolic-character operand's VALUE is declared by that clause, wherever it stands in the source.
                if (entry.currencySignClause() is { } cur) { currencyClauses.Add(cur); continue; }
                // §12.3.7 CURSOR / CRT STATUS (Annex A.4.2 item 25) — the SCREEN module's environment-division
                // surface. ⚠ Both clauses PARSED and were read by no binder at all until kb/Work PB260, so
                // `CRT STATUS IS WS-CRT` compiled clean with ZERO diagnostics: a declined facility that a program
                // could write and get a clean compile out of is not declined, it is undiagnosed. Refused by name
                // (COBOLNET1560), the same posture as the SCREEN SECTION they belong to.
                if (entry.cursorClause() is { } curs) { ScreenFacility.ReportCursorClause(Edition, curs); continue; }
                if (entry.crtStatusClause() is { } crt) { ScreenFacility.ReportCrtStatusClause(Edition, crt); continue; }
                if (entry.implementorSwitchEntry() is { } sw) BindImplementorNameEntry(sw);
            }
            foreach (var alpha in alphabetClauses)                        // phase 2 — every locale-name is declared
            {
                using var _ = Edition.At(alpha);
                AlphabetBind(alpha);
            }
        }
        // The PCS resolves AFTER the walk (OBJECT-COMPUTER precedes SPECIAL-NAMES in source, §12.3.6 GR9); only
        // the NAMED alphabets become the program sequences — other defined alphabets have no effect (NC219A's
        // unreferenced COLLATING-SEQ-2). A native-order alphabet leaves the sequence null (the fast path).
        if (pcsClause is not null) ResolveProgramCollating(pcsClause);
        if (ccClause is not null) ResolveClassification(ccClause);
        foreach (var cd in classClauses) SwitchBindClass(cd);           // after the alphabets exist (PB110)
        foreach (var sc in symbolicClauses) SwitchBindSymbolic(sc);
        foreach (var cur in currencyClauses)                             // after the symbolic characters (PB1628)
        {
            using var _ = Edition.At(cur);
            SwitchBindCurrency(cur);
        }
        FinalizeCurrencySigns();   // §12.3.7.3 r25 — the implied '$' clause, once every explicit clause is in
    }

    /// <summary>One switch-name / feature-name / device-name entry (ISO §12.3.7.2). §12.3.7.3 SR8 — "<i>The implementor
    /// shall specify the names that are available for switch-name-1, feature-name-1, and device-name-1.</i>" — is
    /// decided HERE, once, from the ONE table (<see cref="ImplementorNames"/>): a name that is not a row, or a
    /// non-switch name written with ON/OFF STATUS, is COBOLNET2241 (kb/Work PB862) and registers nothing. A switch
    /// row registers its condition-names (§12.3.7.4 GR2) here; EVERY row's mnemonic — a switch's for SET
    /// (§12.3.7.3 SR5) as much as a device's or feature's — is read by the procedure binders through
    /// <c>BinderContext.Mnemonics</c>, which classifies it by the same <see cref="ImplementorNameEntry"/>
    /// (kb/Work PB454: one walk of the entries for mnemonics, not two).</summary>
    private void BindImplementorNameEntry(Core.ImplementorSwitchEntryContext sw)
    {
        var e = ImplementorNameEntry.Read(sw);
        using (Edition.At(sw))   // §8.3.2.2 — the entry's user-defined words, through the one funnel (kb/Work PB1083)
        {
            if (e.Mnemonic is { } mnemonic) DeclareUserWord(mnemonic, UserWordKind.MnemonicName);
            if (e.OnCondition is { } onName) DeclareUserWord(onName, UserWordKind.ConditionName);
            if (e.OffCondition is { } offName) DeclareUserWord(offName, UserWordKind.ConditionName);
        }
        if (e.Unavailable is { } why)
        {
            Edition.Error(DiagnosticCatalog.UnavailableImplementorName, why);
            return;
        }
        if (e.Row!.Kind != SystemNameKind.Switch) return;
        string switchName = e.Row.Name;
        if (e.OnCondition is { } on) SwitchConditions.TryAdd(on, (switchName, true));
        if (e.OffCondition is { } off) SwitchConditions.TryAdd(off, (switchName, false));
    }

    /// <summary>Resolve the PROGRAM COLLATING SEQUENCE clause (ISO §12.3.6): the IS form's alphabet-name-1
    /// [alphabet-name-2], or the FOR ALPHANUMERIC / FOR NATIONAL forms — each class at most once (§12.3.6.2
    /// braces one of each). SR1: alphabet-name-1 shall reference an alphabet defining an ALPHANUMERIC collating
    /// sequence — a NATIONAL alphabet in the slot is the class-mismatch error and so is a word no ALPHABET clause
    /// declares (kb/Work PB1082; it was inert until then). SR2: alphabet-name-2 shall reference an
    /// alphabet defining a NATIONAL collating sequence — a UTF-8/UTF-16 alphabet references a coded character
    /// set but NO collating sequence (§12.3.7 Table 6), and an alphanumeric-class or undeclared name is the
    /// mismatch; the national slot is a new surface, so it is strict.</summary>
    private void ResolveProgramCollating(Core.ProgramCollatingSequenceClauseContext pcs)
    {
        using var _ = Edition.At(pcs);   // every verdict below is about this clause — report it there
        var (alnumName, natName) = ChoiceIndicators.AlphabetPair(Edition, pcs.collatingForPhrase(),
            f => f.NATIONAL() is not null, f => f.cobolWord().GetText(), pcs.cobolWord(),
            "PROGRAM COLLATING SEQUENCE", "12.3.6.2");

        // ⛔ BOTH SLOTS ASK THE ONE CLASS TEST, CollatingAlphabetFault (kb/Work PB1082): name-1 used to answer only a
        // FOR NATIONAL alphabet and leave every other word inert at every edition — an undeclared word, a data-name, a
        // class-name — so the program silently ran on the native order, while name-2's slot refused the same shapes.
        if (alnumName is not null)
        {
            if (CollatingAlphabetFault(alnumName, national: false) is { } fault)
                Edition.Error("COBOLNET0898", CollatingAlphabetViolation("PROGRAM COLLATING SEQUENCE", alnumName,
                    national: false, fault, "ISO §12.3.6.3 SR1"));
            else if (Alphabets.TryGetValue(alnumName, out var def)) Collating = def.IsIdentity ? null : def;
        }
        if (natName is not null)
        {
            if (CollatingAlphabetFault(natName, national: true) is { } fault)
                Edition.Error("COBOLNET0898", CollatingAlphabetViolation("PROGRAM COLLATING SEQUENCE FOR NATIONAL",
                    natName, national: true, fault, "ISO §12.3.6.3 SR2"));
            else if (NationalAlphabets.TryGetValue(natName, out var def))
                NationalCollating = def.IsIdentity ? null : def;   // null for NATIVE/UCS-4 — the identity fast path (D-N3)
        }
    }

    /// <summary>Bind the OBJECT-COMPUTER <c>CHARACTER CLASSIFICATION {IS locale-phrase-1 [locale-phrase-2] | {FOR ALPHANUMERIC
    /// IS locale-phrase-1 | FOR NATIONAL IS locale-phrase-2}…}</c> clause (ISO §12.3.6.2; kb/Work PB64 T5): each phrase is
    /// LOCALE (the current locale at activation), SYSTEM-DEFAULT, USER-DEFAULT, or a locale-name of the SPECIAL-NAMES
    /// paragraph (§12.3.6.3 SR3 — undeclared → COBOLNET1664, the ONE undeclared-locale-name diagnostic); the two-phrase IS
    /// form gives the alphanumeric then the national classification (GR5 a–j); a class named twice in the FOR form is
    /// a form violation (§5.2.6.4 — each at most once).</summary>
    private void ResolveClassification(Core.CharacterClassificationClauseContext cc)
    {
        using var _ = Edition.At(cc);
        LocalePhrase? alphanumeric = null, national = null;
        // The FOR pair — each class at most once (§5.2.6.4) — through the one reader (kb/Work PB1075).
        var (alnumFor, natFor) = ChoiceIndicators.ForPhrasePair(Edition, cc.classificationForPhrase(),
            f => f.NATIONAL() is not null, "CHARACTER CLASSIFICATION", "12.3.6.2");
        if (alnumFor is not null || natFor is not null)
        {
            if (alnumFor is not null
                && (alphanumeric = ClassificationPhrase(alnumFor.localePhrase().GetText(), national: false)) is null) return;
            if (natFor is not null
                && (national = ClassificationPhrase(natFor.localePhrase().GetText(), national: true)) is null) return;
        }
        else
        {
            var phrases = cc.localePhrase();   // [0] = locale-phrase-1, [1] = locale-phrase-2
            if (phrases.Length < 1) return;    // a malformed shape already drew a parse error
            alphanumeric = ClassificationPhrase(phrases[0].GetText(), national: false);
            if (alphanumeric is null) return;
            if (phrases.Length > 1)
            {
                national = ClassificationPhrase(phrases[1].GetText(), national: true);
                if (national is null) return;
            }
        }
        Classification = new ClassificationSpec(alphanumeric, national);
    }

    private LocalePhrase? ClassificationPhrase(string word, bool national)
    {
        // >>COBOL-WORDS (ISO §7.3.10.4 GR2/GR3/GR4; kb/Work PB250): a §8.9/§8.10 word the lexer does not
        // tokenize is reached ONLY through the map; a raw text test here is inert to the directive both ways.
        if (CobolWords.Is(word, "LOCALE")) return new LocalePhrase(Runtime.Globalization.LocalePhraseKind.Current, null);
        if (CobolWords.Is(word, "SYSTEM-DEFAULT")) return new LocalePhrase(Runtime.Globalization.LocalePhraseKind.SystemDefault, null);
        if (CobolWords.Is(word, "USER-DEFAULT")) return new LocalePhrase(Runtime.Globalization.LocalePhraseKind.UserDefault, null);
        var sym = ResolveLocaleName(word, $"CHARACTER CLASSIFICATION{(national ? " FOR NATIONAL" : "")} {word}",
            $"ISO §12.3.6.3 SR3 — locale-name-{(national ? 2 : 1)} shall be a locale name defined in the SPECIAL-NAMES paragraph");
        return sym is null ? null : new LocalePhrase(Runtime.Globalization.LocalePhraseKind.Named, sym);
    }

    /// <summary>
    /// Bind the SPECIAL-NAMES <c>ORDER TABLE ordering-name-1 IS literal-9</c> clause (ISO §12.3.7.2 — the last item
    /// of the paragraph's general format; kb/Work PB101 / DESIGN-locale-facility §4.9). §12.3.7.4 GR17: "When ORDER
    /// TABLE is specified, ordering-name-1 shall reference a cultural ordering table that is identified by
    /// literal-9 and constructed in accordance with ISO/IEC 14651:2020, Annex A. The implementor specifies the
    /// allowable content of literal-9."
    /// <list type="bullet">
    ///   <item>SR10 — "Literal-4 and literal-9 shall be alphanumeric or national literals": a numeric, boolean or
    ///   figurative operand is COBOLNET0898.</item>
    ///   <item>SR11 — literal-9 "shall specify neither a symbolic-character figurative constant nor a zero-length
    ///   literal".</item>
    ///   <item>The clause is BRACKETED and unrepeated in the general format (unlike LOCALE / CURRENCY / CLASS,
    ///   which carry a printed ellipsis), so a second ORDER TABLE clause — or a repeated ordering-name — is a form
    ///   violation.</item>
    /// </list>
    /// <para>⚖ The ADVISORY (COBOLNET1662, a warning): GR17 leaves literal-9's allowable content to the
    /// implementor, so a spelling this implementation cannot resolve is legal source with a defined RUNTIME
    /// outcome — §15.85.4 r2 sets EC-ORDER-NOT-SUPPORTED at every reference. Rejecting it would refuse a
    /// conforming program; saying nothing would let a program whose every STANDARD-COMPARE is inoperative compile
    /// clean. The resolver consulted here is the SAME one the runtime uses
    /// (<c>CollationEngine.TryGetOrderingTable</c>), so bind-time advice and run-time behaviour cannot drift.</para>
    /// </summary>
    /// <summary>The SPECIAL-NAMES <c>LOCALE locale-name-1 IS {external-locale-name-1 | literal-4}</c> clause (ISO
    /// §12.3.7.2; DESIGN-locale-facility §4.1, T1): declare the locale-name with its external identification. §12.3.7.3
    /// SR10 — literal-4 "shall be alphanumeric or national"; SR11 — "shall not be a symbolic-character figurative
    /// constant" nor a zero-length literal (the grammar admits any literal, so both are checked here); a locale-name
    /// declared twice is COBOLNET1665 (§8.3.2.2 — a user-defined word of one type is unique within its scope). §12.3.7.4
    /// GR5 — the implementor specifies the allowable content: DETERMINATION L1, a locale tag, POSIX spellings normalized
    /// (<see cref="LocaleSymbol.Tag"/>); availability is NOT checked here (§8.1.5 — run time; L1 item 4).</summary>
    private void LocaleBind(Core.LocaleClauseContext loc)
    {
        var words = loc.cobolWord();                  // [0] = locale-name-1, [1] = external-locale-name-1 (word branch); LOCALE is the formatWord
        string name = words[0].GetText();
        DeclareUserWord(name, UserWordKind.LocaleName);   // §8.3.2.2 — the one declaration funnel (kb/Work PB1083)
        string external;
        bool fromLiteral = loc.literal() is not null;
        if (fromLiteral)
        {
            // SR10 / SR11 for literal-4 — the ONE text-literal rule the ORDER TABLE clause's literal-9 shares.
            if (!TryClauseTextLiteral(loc.literal()!, $"LOCALE {name} IS {ConcatFolder.Spelling(loc.literal()!)}", "literal-4", out external)) return;
        }
        else external = words[1].GetText();
        var symbol = new LocaleSymbol(name, external, fromLiteral);
        if (!Locales.TryAdd(name, symbol))
        {
            Edition.Error("COBOLNET1665", $"LOCALE {name}: the locale-name is already declared in this SPECIAL-NAMES paragraph "
                + $"(as {Locales[name]}); a user-defined word is unique within its scope (ISO §8.3.2.2 / §12.3.7.2)");
        }
    }

    /// <summary>Resolve a locale-name reference (ISO §12.3.7.3 SR24 for the ALPHABET clause; §14.9.39.3 SR26 for SET
    /// LOCALE; the later increments' sites) to its symbol, or null after COBOLNET1664 — the ONE undeclared-locale-name
    /// diagnostic, with the citing site named (DESIGN-locale-facility §7 rule a).</summary>
    internal LocaleSymbol? ResolveLocaleName(string name, string site, string rule)
    {
        if (Locales.TryGetValue(name, out var sym)) return sym;
        Edition.Error("COBOLNET1664", $"{site}: '{name}' is not a locale-name declared by a SPECIAL-NAMES LOCALE clause ({rule})"
            + (Locales.Count == 0 ? "; no LOCALE clause is in scope" : $"; declared: {string.Join(", ", Locales.Keys)}"));
        return null;
    }

    /// <summary>The ONE rule for a SPECIAL-NAMES clause's TEXT literal (ISO §12.3.7.3 SR10 — "shall be alphanumeric or
    /// national"; SR11 — "shall specify neither a symbolic-character figurative constant nor a zero-length literal"),
    /// shared by the ORDER TABLE clause's literal-9 and the LOCALE clause's literal-4: decodes the literal (a
    /// concatenation expression folds first — §8.8.3.3 GR3 makes it usable anywhere a literal of its class is),
    /// reporting COBOLNET0898 and returning false on a violation. <paramref name="what"/> names the clause for the
    /// message; <paramref name="operand"/> the literal's role ("literal-9").
    /// <para>⛔ SR11 bars ONE kind of figurative constant — the symbolic-character one — and §8.3.3.6.3 SR1 admits every
    /// other as a literal of its class, so <c>ORDER TABLE T1 IS SPACE</c> and <c>LOCALE LA IS SPACE</c> are legal:
    /// SPACE then names an ordering table / a locale this implementation does not provide, which is the RUN-TIME
    /// answer (the COBOLNET1662 advice and EC-ORDER-NOT-SUPPORTED / EC-LOCALE-MISSING), not a syntax error. This screen
    /// used to refuse every figurative constant under a rule that quotes only the symbolic-character one (kb/Work
    /// PB1537), and the NULL it also refused was not a figurative constant at all (§8.4.3.10.3 SR1).</para></summary>
    private bool TryClauseTextLiteral(Core.LiteralContext lit, string what, string operand, out string text)
    {
        text = "";
        string raw = lit.GetText();
        var nn = lit.nonNumericLiteral();
        if (nn?.predefinedNull() is not null)
        {
            PredefinedNullRule.Report(Edition, $"{what}: {operand} — an alphanumeric or national literal "
                + "(ISO §12.3.7.3 SR10)");
            return false;
        }
        if (nn?.figurativeConstant() is { } fig)
        {
            var slot = new FigurativeSlot(what, message => Edition.Error("COBOLNET0898", message),
                $"{operand} shall be an alphanumeric or national literal", "SR10",
                category => category is PicCategory.Alphanumeric or PicCategory.National,
                $"{operand} shall specify neither a symbolic-character figurative constant nor a zero-length literal");
            if (SpecialNamesFigurative(fig, slot) is not { } figurative) return false;
            text = figurative;
        }
        // §8.8.3.3 GR3 makes a concatenation expression "equivalent to a literal of the same class and value,
        // [usable] anywhere a literal of that class may be used", so `IS "ISO " & "14651_2020_TABLE1"` is legal
        // here and folds to the one literal SR10 then classes. Refusing it would reject conforming source.
        else if (nn?.concatenationExpression() is { } cat)
        {
            // A figurative HIGH-/LOW-VALUE written INSIDE SPECIAL-NAMES takes the NATIVE extremes
            // (§12.3.7.4 GR10 — one pair for both classes, owner R52) and SR11 forbids a
            // symbolic-character operand in literal-9 (the posture this screen gives literal-4 as well).
            var folded = ConcatFolder.Fold(cat, Edition, LiteralEnvironment.SpecialNames(this,
                refusesSymbolicCharacters: true));
            if (folded.Category is not (PicCategory.Alphanumeric or PicCategory.National))
            {
                Edition.Error("COBOLNET0898", $"{what}: {operand} shall be an alphanumeric "
                    + "or national literal (ISO §12.3.7.3 SR10); this concatenation expression is class "
                    + $"{folded.Category} (§8.8.3.3 GR1/GR3)");
                return false;
            }
            text = folded.Value;
        }
        else
        {
            // SR10 — alphanumeric or national. CobolLiteral.ClassOf answers from the PREFIX, so it admits both
            // quoting forms and both hexadecimal spellings (X"…" is one FORM of an alphanumeric literal,
            // §8.3.3.2) and refuses a boolean one; a NUMERIC literal is not a quoted literal and lands here too.
            if (CobolLiteral.ClassOf(raw) is not (LiteralClass.Alphanumeric or LiteralClass.National))
            {
                Edition.Error("COBOLNET0898", $"{what}: {operand} shall be an alphanumeric or "
                    + "national literal (ISO §12.3.7.3 SR10)");
                return false;
            }
            text = CobolLiteral.Decode(raw);
        }
        // SR11's SECOND half.
        if (text.Length == 0)
        {
            Edition.Error("COBOLNET0898", $"{what}: {operand} shall specify neither a "
                + "symbolic-character figurative constant nor a zero-length literal (ISO §12.3.7.3 SR11)");
            return false;
        }
        return true;
    }

    /// <summary>Declare one <c>DYNAMIC LENGTH [STRUCTURE] name [IS] {…}</c> clause (ISO §12.3.7.2; kb/Work PB829):
    /// the layout is read off the choice-indicator group — PREFIXED and DELIMITED, one or more, each at most once
    /// (§5.2.6.4, through the one reader <see cref="ChoiceIndicators.AtMostOnce{T}"/>) — or is the
    /// physical-structure-name-1 alternative, whose allowable names §12.3.7.3 SR32 leaves to the implementor and
    /// this implementor provides NONE of (docs/CONFORMANCE.md §3 D-DL3), so that alternative is refused by name.
    /// A second declaration of one name in the same source element is refused too: every reference to it would
    /// fail §8.4.2.1's "a reference that uniquely identifies that resource".</summary>
    private void DynamicLengthStructureBind(Core.DynamicLengthStructureClauseContext dls, HashSet<string> ownNames)
    {
        if (dls.cobolWord() is not { } nameCtx || dls.dynamicLengthLayout() is not { } layout) return;   // a parse error already
        string name = nameCtx.GetText();
        using (Edition.At(nameCtx)) DeclareUserWord(name, UserWordKind.DynamicLengthStructureName);   // §8.3.2.2 (kb/Work PB1083)
        string where = $"DYNAMIC LENGTH STRUCTURE {name}";
        DynamicLengthStructure declared;
        if (layout.cobolWord() is { } physical)
        {
            Edition.Error(DiagnosticCatalog.DynamicLengthStructureInvalid,
                $"{where} IS {physical.GetText()}: '{physical.GetText()}' is not a physical-structure-name this "
                + "implementation supports — ISO §12.3.7.3 SR32 (\"The implementor shall specify the names supported "
                + "for physical-structure-name-1\") leaves the names to the implementor and WiseOwl COBOL supports none; "
                + "describe the layout with the PREFIXED and/or DELIMITED phrases (docs/CONFORMANCE.md §3 D-DL3)");
            declared = new DynamicLengthStructure(name, DynamicLengthPrefix.None, Delimited: false, physical.GetText());
        }
        else
        {
            var prefixed = ChoiceIndicators.AtMostOnce(Edition, layout.dynamicLengthPrefixedPhrase(), where,
                "the PREFIXED phrase", "12.3.7.2");
            var delimited = ChoiceIndicators.AtMostOnce(Edition, layout.dynamicLengthDelimitedPhrase(), where,
                "the DELIMITED phrase", "12.3.7.2");
            var prefix = prefixed is null ? DynamicLengthPrefix.None
                : (prefixed.SIGNED() is not null, prefixed.dynamicLengthShortWord() is not null) switch
                {
                    (false, false) => DynamicLengthPrefix.Unsigned32,
                    (true, false) => DynamicLengthPrefix.Signed32,
                    (false, true) => DynamicLengthPrefix.Unsigned16,
                    (true, true) => DynamicLengthPrefix.Signed16,
                };
            declared = new DynamicLengthStructure(name, prefix, Delimited: delimited is not null, null);
        }
        if (!ownNames.Add(name))
        {
            using var _ = Edition.At(nameCtx);
            Edition.Error(DiagnosticCatalog.DynamicLengthStructureInvalid,
                $"{where}: the dynamic-length-structure-name '{name}' is already declared in this SPECIAL-NAMES "
                + "paragraph, so no reference to it could uniquely identify one layout (ISO §8.4.2.1)");
            return;
        }
        DynamicLengthStructures[name] = declared;   // replaces an INHERITED same-named structure (§8.4.6.1)
    }

    private void OrderTableBind(Core.OrderTableClauseContext ot)
    {
        // ONE cobolWord: ordering-name-1. The clause's own keywords ORDER and TABLE are both lexer tokens since
        // kb/Work PB704 — ORDER used to ride cobolWord as slot [0], which is exactly the shape the §8.9 funnel
        // mistook for a user-defined word.
        if (ot.cobolWord() is not { } name1 || ot.literal() is not { } lit) return;   // a malformed shape already drew a parse error
        string name = name1.GetText();
        DeclareUserWord(name, UserWordKind.OrderingName);   // §8.3.2.2 — the one declaration funnel (kb/Work PB1083)
        string raw = ConcatFolder.Spelling(lit);
        // SR10 / SR11 for literal-9 — the ONE text-literal rule the LOCALE clause's literal-4 shares.
        if (!TryClauseTextLiteral(lit, $"ORDER TABLE {name} IS {raw}", "literal-9", out string text)) return;
        // ⚠ ONE CLAUSE PER PARAGRAPH, measured off the PRINTED general format rather than inferred from GR17's
        // singular wording: §12.3.7.2 brackets `ORDER TABLE ordering-name-1 IS literal-9` with NO trailing
        // ellipsis, where the repeatable clauses beside it (CLASS, CURRENCY, LOCALE, the switch entry, SYMBOLIC
        // CHARACTERS) each print one. A second clause is therefore a form violation, and so — as a consequence
        // rather than as a second rule — is a second ordering-name.
        if (OrderTables.Count > 0)
        {
            Edition.Error("COBOLNET0898", $"ORDER TABLE {name}: the ORDER TABLE clause may be specified only once "
                + "in a SPECIAL-NAMES paragraph — its general format brackets it without an ellipsis, unlike the "
                + $"repeatable clauses (ISO §12.3.7.2); '{OrderTables.Keys.First()}' is already declared");
            return;
        }
        OrderTables[name] = text;
        // GR17's implementor half, checked against the ONE resolver the runtime uses (§15.85.4 r2 owns the outcome).
        if (!CollationEngine.TryGetOrderingTable(text, out _))
            Edition.Warning(DiagnosticCatalog.OrderTableUnresolved, $"ORDER TABLE {name} IS {raw}: "
                + $"'{text}' does not name a cultural ordering table this implementation provides (ISO §12.3.7.4 "
                + "GR17 leaves literal-9's allowable content to the implementor: the default table "
                + $"'{CollationEngine.DefaultOrderingTableName}' — case-insensitive, space and underscore "
                + "interchangeable — or a CLDR locale tag naming a tailored collation). Every FUNCTION "
                + $"STANDARD-COMPARE reference to {name} will set EC-ORDER-NOT-SUPPORTED at run time (§15.85.4 r2)");
    }

    /// <summary>Build one <c>ALPHABET name IS …</c> clause (ISO §12.3.7 GR7): NATIVE / STANDARD-1 / STANDARD-2 are
    /// the native (ISO/IEC 646) order — no table; a literal phrase assigns successive ascending positions per
    /// k)1–k)6 — a numeric literal is the 1-based NATIVE ordinal (k1a), a multi-character literal positions each
    /// character leftmost-first (k1b), THRU expands the native run in EITHER direction (k5), ALSO members share
    /// ONE position (k6), and every unspecified character takes a DISTINCT ascending position above the highest
    /// specified, in native relative order (k3). HIGH-/LOW-VALUE written INSIDE the clause are the NATIVE extremes
    /// (GR10 — the PCS re-derivation applies only outside SPECIAL-NAMES).</summary>
    private void AlphabetBind(Core.AlphabetClauseContext alpha)
    {
        // ALPHABET … FOR ALPHANUMERIC/NATIONAL — the FOR phrase edition gate is now VersionConformancePass
        // ParseArm.VisitAlphabetClause (14g.4, recognition), as is the UCS-4/UTF-8/UTF-16 phrase gate
        // (alphabet-national-2002). The ISO position for the FOR phrase is between the name and IS
        // (§12.3.7.2); a FOR phrase after the definition is refused by name in ClosedFormatPass (kb/Work PB977)
        // and read here only to recover.
        string name = alpha.cobolWord().GetText();
        DeclareUserWord(name, UserWordKind.AlphabetName);   // §8.3.2.2 — the one declaration funnel (kb/Work PB1083)
        var def = alpha.alphabetDefinition();
        bool national = ForPhraseIsNational(alpha.specialNamesForPhrase(), alpha.misplacedSpecialNamesForPhrase());
        // `IS LOCALE [locale-name-2]` — either branch (§12.3.7.2): Annex A.4.9 item 10 ("LOCALE phrases in the
        // ALPHABET clause"). The grammar recognizes it as its own rule — LOCALE is the phrase's KEYWORD, a formatWord
        // (kb/Work PB764; PB100 fixed the false "reserved word used as a user-defined word" it used to draw) — at every
        // edition, and below 2002 the ONE construct gate (VersionConformancePass, alphabet-locale-2002) refuses it, so
        // there is nothing to bind there.
        if (def.alphabetLocalePhrase() is { } localePhrase)
        {
            if (Edition.DialectLevel < 2002) return;
            // `IS LOCALE` (no locale-name-2) — the LOCALE-based collating sequence of the locale CURRENT at each use
            // (§12.3.7.4 GR7e; §12.3.6.4 GR11/GR12): the runtime LocaleCollation over the derived CLDR/UCA engine (kb/Work
            // PB101 — the T3 arm; determination L5 makes one locale sequence serve both classes). Table 6 row LOCALE: a
            // collating sequence, NOT a coded character set — CODE-SET / SYMBOLIC … IN / CLASS … IN may not name it
            // (§12.3.7.3 SR16g/SR17d); the ONE coded-set resolver (CodedCharacterSetOf) refuses it for all three with
            // COBOLNET1669, because AlphabetDef.CodedSet is null for this arm.
            // `IS LOCALE locale-name-2` (§12.3.7.3 SR24: "locale-name-2 shall be a locale-name defined by the LOCALE clause")
            // — the NAMED form (T1): the sequence of THAT locale, whose external identification the symbol carries and
            // whose availability is decided at use (EC-LOCALE-MISSING). An undeclared name is COBOLNET1664 and the
            // alphabet falls back to the current-locale form so the rest of the program still binds.
            var locale = LocaleCollatingSpec.CurrentLocale;
            if (localePhrase.cobolWord() is { } localeName)
            {
                var sym = ResolveLocaleName(localeName.GetText(), $"ALPHABET {name}{(national ? " FOR NATIONAL" : "")} IS LOCALE {localeName.GetText()}",
                    "ISO §12.3.7.3 SR24 — locale-name-2 shall be a locale-name defined by the LOCALE clause");
                if (sym is not null) locale = new LocaleCollatingSpec(new LocaleRef(sym));
            }
            if (national) NationalAlphabets.TryAdd(name, new NationalAlphabetDef(null, locale, HasCollatingSequence: true, "LOCALE"));
            else Alphabets.TryAdd(name, new AlphabetDef(null, locale, "LOCALE"));
            return;
        }
        if (national)
        {
            AlphabetBindNational(name, def);
            return;
        }

        // The ALPHANUMERIC branch (explicit FOR ALPHANUMERIC, or implied — §12.3.7.3 SR13). The national
        // coded-set names are NOT in this branch's format (§12.3.7.2 admits them only after FOR NATIONAL) —
        // intercept them rather than mis-binding their letters as literal characters.
        if (CodeSetNameOf(def, CobolWords) is { } wrongBranch)
        {
            Edition.Error("COBOLNET0898", $"ALPHABET {name} IS {wrongBranch}: the {wrongBranch} coded character "
                + "set may be referenced only in the FOR NATIONAL branch of the ALPHABET clause "
                + "(ISO §12.3.7.2 general format)");
            return;
        }
        if (def.NATIVE() is not null || def.STANDARD_1() is not null || def.STANDARD_2() is not null)
        {
            // One identity COLLATING SEQUENCE, three distinct CODED CHARACTER SETS (Table 6 + GR7 c/d: STANDARD-1/2
            // are the 128 ISO/IEC 646 IRV characters; NATIVE is the whole native set) — the Phrase carries which
            // (kb/Work PB110); IsIdentity stays true, so no runtime carrier is emitted either way.
            Alphabets.TryAdd(name, def.NATIVE() is not null ? AlphabetDef.Native
                : new AlphabetDef(null, null, def.STANDARD_1() is not null ? "STANDARD-1" : "STANDARD-2"));
            return;
        }

        // A bare word that is not NATIVE / STANDARD-1 / STANDARD-2 / LOCALE and is not a figurative constant is
        // code-name-1 (§12.3.7.2 general format), resolved against the implementor code-name table (§12.3.7.3
        // SR15) — the ONE lookup, shared with the national arm through AlphabetCodeName. A supported name carries
        // its collating sequence in the row's Table (null = the native order, GR7 i's positions being the native
        // ones) and its coded character set through the row itself (§12.3.7.4 GR7 i, Table 6's two Y columns).
        switch (AlphabetCodeName(name, def, national: false, out var codeName))
        {
            case CodeNameOutcome.Refused: return;
            case CodeNameOutcome.Bound:
                Alphabets.TryAdd(name, new AlphabetDef(codeName!.Table, null, codeName.Name, codeName));
                return;
        }

        if (AlphabetLiteralPhrase(name, def, national: false) is { } table)
            Alphabets.TryAdd(name, new AlphabetDef(table, null, "literal-phrase"));
    }

    /// <summary>What a bare-word alphabet definition turned out to be once <see cref="AlphabetCodeName"/> asked
    /// the implementor code-name table about it (ISO §12.3.7.3 SR15).</summary>
    private enum CodeNameOutcome
    {
        /// <summary>The definition is not the one-bare-word shape code-name-1/-2 has — the caller continues to
        /// the literal-phrase reading.</summary>
        NotACodeName,

        /// <summary>A supported code-name; <c>row</c> carries its set and sequence.</summary>
        Bound,

        /// <summary>A bare word that is no supported code-name — refused, and the caller binds nothing.</summary>
        Refused,
    }

    /// <summary>⛔ THE ONE §12.3.7.3 SR15 RESOLUTION, for code-name-1 (alphanumeric) and code-name-2 (national)
    /// alike: "<i>The implementor shall specify the names supported for code-name-1 and code-name-2 in the ALPHABET
    /// clause, if any.</i>" The supported set is <see cref="ImplementorCodeNames"/> — a TABLE, so this method is a
    /// LOOKUP with a refusal on miss and the next supported code-name is a row there, never an arm here
    /// (owner decision, kb/Work PB793; CLAUDE.md rule 5). The words that may stand alone in an alphabet definition
    /// besides a code-name are the general format's own keywords (NATIVE, STANDARD-1, STANDARD-2, UCS-4, UTF-8,
    /// UTF-16, LOCALE), which the callers have already consumed, and the figurative constants of a one-operand
    /// literal phrase (§12.3.7.4 GR10).
    /// <para>⛔ An UNSUPPORTED word was silently REINTERPRETED as the characters of its own spelling until
    /// kb/Work PB770: <c>ALPHABET A-ASC IS ASCII</c> built an alphabet whose first four positions were A, S, C, I,
    /// and every downstream reference (PROGRAM COLLATING SEQUENCE, SORT, SYMBOLIC CHARACTERS … IN) read that. A
    /// bare word is not a literal at all (SR14 b2/c2 require an alphanumeric / national literal), so the
    /// literal-phrase reading is not even available to fall back on.</para>
    /// <para>The check is deliberately narrow: it fires only on a definition that is ONE bare word, which is the
    /// only shape code-name-1/-2 has in the general format. A word inside a multi-operand phrase is a figurative
    /// constant or an SR14 b2/c2 violation, and <see cref="AlphabetOperands"/> owns that.</para></summary>
    private CodeNameOutcome AlphabetCodeName(string name, Core.AlphabetDefinitionContext def, bool national,
        out ImplementorCodeName? row)
    {
        row = null;
        if (def.alphabetEntry() is not [{ ChildCount: 1 } only] || only.GetChild(0) is not Core.CobolWordContext cw
            || IsAlphabetFigurativeWord(cw.GetText()))
            return CodeNameOutcome.NotACodeName;
        if (ImplementorCodeNames.Find(cw.GetText(), national) is { } hit) { row = hit; return CodeNameOutcome.Bound; }
        Edition.Error(DiagnosticCatalog.AlphabetCodeNameUnsupported, $"ALPHABET {name}{(national ? " FOR NATIONAL" : "")} "
            + $"IS {cw.GetText()}: not a supported code-name — the {(national ? "code-name-2" : "code-name-1")} "
            + $"names this implementation supports are: {ImplementorCodeNames.Spellings(national)} "
            + $"(ISO §12.3.7.3 SR15; the {(national ? "national coded character set keywords are NATIVE, UCS-4, UTF-8, and UTF-16" : "alphanumeric coded character set keywords are NATIVE, STANDARD-1, and STANDARD-2")})");
        return CodeNameOutcome.Refused;
    }

    /// <summary>The figurative constants a literal phrase may name (ISO §12.3.7.4 GR10 — "<i>When specified as
    /// literals in the SPECIAL-NAMES paragraph, the figurative constants HIGH-VALUE and LOW-VALUE are associated
    /// with those characters having the highest and lowest positions … in the native national collating sequence,
    /// when the NATIONAL phrase is specified, or in the native alphanumeric collating sequence otherwise</i>"; the
    /// remaining figurative words are §8.3.3.6 constants of the clause's class). ONE list for both arms — the
    /// national arm used to keep its own copy under a different name.</summary>
    private static bool IsAlphabetFigurativeWord(string word) => AlphabetFigurative(word) is not null;

    /// <summary>⛔ THE ONE literal-phrase alphabet builder (ISO §12.3.7.4 GR7 k), for the ALPHANUMERIC arm and the
    /// <c>FOR NATIONAL</c> arm alike. GR7 k states its six sub-rules once, "<i>where the native coded character set
    /// is the type of coded character set or collating sequence being defined, either alphanumeric or national</i>",
    /// and both native sets are the 65,536 UTF-16 code units here (implementor item 188 / D-N1) — so there is ONE
    /// builder and the <paramref name="national"/> flag reaches only the diagnostics and the literal CLASS rule.</summary>
    /// <remarks>k1a — a numeric literal is the 1-based ordinal in the native set; k1b — a (possibly multi-character)
    /// literal takes successive ascending positions leftmost-first; k2 — the operand order IS the ascending position
    /// order; k3 — every UNSPECIFIED character follows the highest specified one, in native relative order (realized
    /// SPARSELY: the runtime computes it arithmetically); k5 — THROUGH expands the native run in EITHER direction;
    /// k6 — ALSO members share ONE position, of which literal-1 is the first character defined.
    /// <para>⛔ §12.3.7.3 SR14 a is enforced HERE, in <c>Assign</c>: "<i>A given character shall not be specified
    /// more than once in that ALPHABET clause.</i>" Both arms used to carry a <c>// diagnostic later</c> comment at
    /// exactly this line and silently keep the first occurrence (kb/Work PB770 leg a). "First wins" survives as the
    /// RECOVERY posture so the rest of the program still binds; it is no longer the answer.</para>
    /// <para>⛔ SR14 b4/c4 — "<i>The number of characters specified shall not exceed the number of characters in the
    /// native … character set</i>" — needs no separate check: SR14 a makes every specified character distinct, and
    /// distinct characters of a set cannot outnumber the set. Enforcing a) enforces it.</para></remarks>
    /// <returns>The table, or null when the phrase specified nothing (every operand failed its syntax rule and the
    /// diagnostics are already reported).</returns>
    private CollatingTable? AlphabetLiteralPhrase(string name, Core.AlphabetDefinitionContext def, bool national)
    {
        var rules = LiteralPhraseRules.Alphabet(name, national);
        string what = rules.What;
        var pos = new Dictionary<char, int>();
        var specOrder = new List<char>();       // every specified character in source order (the GR8/GR9 tie rules)
        var repByPos = new List<char>();        // per position: the FIRST character DEFINED there (§15.15.4 r2 / GR7 k6)
        // ⛔ An int, never a ushort (kb/Work PB1557): the position COUNT reaches 65,536 when the phrase specifies
        // every native character (SR14 b4/c4's bound is "shall not exceed", so equal is legal), one past what a
        // 16-bit counter holds — it wrapped to 0 and the whole table lost its specified block.
        int next = 0;
        void Assign(char c, bool advance)
        {
            if (pos.ContainsKey(c))
            {
                // SR14 a. The recovery posture is the historical one — the FIRST specification wins — so the
                // sequence stays well-formed and the rest of the source still binds.
                Edition.Error(DiagnosticCatalog.AlphabetClauseViolation, $"{what}: the character {DescribeChar(c)} is "
                    + "specified more than once — a given character shall not be specified more than once in that "
                    + "ALPHABET clause (ISO §12.3.7.3 SR14 a)");
                return;
            }
            pos[c] = next;
            specOrder.Add(c);
            if (repByPos.Count == next) repByPos.Add(c);   // the first occupant of the position wins (ALSO literal-1)
            if (advance) next++;
        }

        foreach (var entry in def.alphabetEntry())
        {
            bool thru = entry.THRU() is not null || entry.THROUGH() is not null;
            if (thru && entry.ALSO().Length > 0)
            {
                // ⛔ ONE ENTRY, ONE PHRASE (kb/Work PB790; RENDERED — PDF p321 / folio 291). The literal-phrase
                // figure stacks `{THROUGH|THRU} literal-2` and `{ALSO literal-3}…` inside ONE pair of square brackets
                // with no choice indicators, so §5.2.6.2 admits "one of the alternatives contained within the
                // brackets" — never both. The grammar parses the union (superset-parse / bind-narrow) so the refusal
                // can name its rule; it used to reach the k5 branch below, which `continue`d past the ALSO operands
                // and left them at the GR7 k3 unspecified positions with no diagnostic. There is no merged reading
                // to recover to — k5 and k6 place operands by incompatible rules — so the entry contributes nothing.
                Edition.Error(DiagnosticCatalog.AlphabetThroughWithAlso, $"{what}: the entry for {entry.GetChild(0).GetText()} "
                    + "specifies both a THROUGH phrase and an ALSO phrase — the literal-phrase general format offers "
                    + "them as alternatives within one pair of brackets (ISO §12.3.7.2; §5.2.6.2)");
                continue;
            }
            var operands = AlphabetOperands(entry, rules);
            if (operands.Count == 0) continue;
            // SR14 b3/c3: "Each … literal, when a THROUGH or ALSO phrase is specified, shall be one character in
            // length." A multi-character operand used to be silently DROPPED on the alphanumeric arm — the whole
            // entry vanished from the table with no diagnostic (kb/Work PB770 leg b).
            if ((thru || entry.ALSO().Length > 0) && !OneCharacterOperands(operands, rules, through: thru)) continue;
            if (thru)
            {
                // k5: the native run from operand-1 to operand-2, in EITHER direction, ascending positions.
                if (operands.Count < 2) continue;          // a malformed shape already drew a parse error
                int a = operands[0].Characters[0], b = operands[1].Characters[0], step = a <= b ? 1 : -1;
                for (int c = a; ; c += step) { Assign((char)c, advance: true); if (c == b) break; }
                continue;
            }
            if (entry.ALSO().Length > 0)
            {
                // k6: operand-1 and every ALSO operand share ONE ordinal position; operand-1 is the position's
                // first character (the CHAR() pick and the LOW-VALUE tie winner). ⛔ The advance is GUARDED
                // (PB59): an all-duplicate ALSO group must not advance past an unoccupied position — GR7 k3
                // admits no hole, and RepByPos would acquire one.
                int before = repByPos.Count;
                foreach (var op in operands) Assign(op.Characters[0], advance: false);
                if (repByPos.Count > before) next++;
                continue;
            }
            // k1b: a (possibly multi-character) literal — each character, leftmost first, ascending positions.
            foreach (char c in operands[0].Characters) Assign(c, advance: true);
        }
        if (specOrder.Count == 0) return null;      // nothing was specified — the operand diagnostics stand alone

        // The sparse arrays and the §12.3.7.4 GR8/GR9 extremes — CollatingTable.Build, the ONE place either is
        // computed, shared with the implementor code-name arm (GR7 i/j) which walks a code page instead of a
        // clause. This method used to carry its own copy of the extremes rule (kb/Work PB793).
        return CollatingTable.Build(pos, specOrder, repByPos, next);
    }

    /// <summary>A character named in a diagnostic: its literal form when it is printable, else its U+ code point.</summary>
    private static string DescribeChar(char c) => c > ' ' && c < (char)0x7F ? $"'{c}'" : $"U+{(int)c:X4}";

    /// <summary>The national coded-character-set name a definition consists of ("UCS-4" / "UTF-8" / "UTF-16"),
    /// or null. These are §8.9 CONTEXT-SENSITIVE words scoped to the ALPHABET clause — they arrive as a single
    /// plain <c>cobolWord</c> alphabet entry (never lexer keywords; they stay user-definable elsewhere), so the
    /// shape is: exactly one entry, no THROUGH/ALSO, one cobolWord, whose text is one of the three names.</summary>
    private static string? CodeSetNameOf(Core.AlphabetDefinitionContext def, Editions.CobolWordsMap cobolWords)
    {
        if (def.alphabetEntry() is not [{ } entry]) return null;
        if (entry.THRU() is not null || entry.THROUGH() is not null || entry.ALSO().Length > 0) return null;
        if (entry.ChildCount != 1 || entry.GetChild(0) is not Core.CobolWordContext w) return null;
        // >>COBOL-WORDS (ISO §7.3.10.4 GR2/GR3/GR4; kb/Work PB250): a §8.9/§8.10 word the lexer does not
        // tokenize (UCS-4 / UTF-8 / UTF-16 are §8.10 context-sensitive) is reached ONLY through the map. The
        // CANONICAL name is what is returned, so the downstream Phrase tag never carries a user synonym.
        string? t = cobolWords.Resolve(w.GetText().ToUpperInvariant());
        return t is "UCS-4" or "UTF-8" or "UTF-16" ? t : null;
    }

    /// <summary>Bind an <c>ALPHABET … FOR NATIONAL IS …</c> clause (ISO §12.3.7.2 second branch): NATIVE — the
    /// native national coded set and collating sequence (GR7 d2, identity); UCS-4 — the ISO/IEC 10646 coded set
    /// AND its appearance-order collating sequence (GR7 f), which on the D-N1 one-UTF-16-code-unit-per-position
    /// substrate IS the native code-unit order (see <see cref="NationalAlphabetDef"/> — §8.5.1.4 denies surrogate
    /// -pair recognition, so the supplementary-plane codepoint/code-unit divergence is unreachable; the
    /// correspondence is the BMP identity, implementor item 188); UTF-8/UTF-16 — coded character sets ONLY
    /// (GR7 g/h + Table 6: no collating sequence); a literal phrase — the sparse national collating table
    /// (GR7 k over the native national set). STANDARD-1/STANDARD-2 are not in the national branch's format
    /// (alphanumeric-branch-only); the LOCALE phrase is, and <see cref="AlphabetBind"/> binds it for both branches
    /// before reaching here. Unknown words are code-name-2 — the implementor supports none (§12.3.7.3 SR15).</summary>
    private void AlphabetBindNational(string name, Core.AlphabetDefinitionContext def)
    {
        if (def.NATIVE() is not null)
        {
            NationalAlphabets.TryAdd(name, new NationalAlphabetDef(null, HasCollatingSequence: true, "NATIVE"));
            return;
        }
        if (def.STANDARD_1() is not null || def.STANDARD_2() is not null)
        {
            Edition.Error("COBOLNET0898", $"ALPHABET {name} FOR NATIONAL: STANDARD-1/STANDARD-2 name the "
                + "ISO/IEC 646 ALPHANUMERIC coded character set and may be referenced only in the FOR "
                + "ALPHANUMERIC branch (ISO §12.3.7.2 general format)");
            return;
        }
        if (CodeSetNameOf(def, CobolWords) is { } cs)
        {
            // UCS-4 references BOTH a coded set and a collating sequence; UTF-8/UTF-16 a coded set ONLY
            // (§12.3.7 GR7 f/g/h + Table 6). All three collapse to the identity on this substrate — the
            // difference is only WHERE the name may legally be referenced.
            NationalAlphabets.TryAdd(name, new NationalAlphabetDef(null, HasCollatingSequence: cs == "UCS-4", cs));
            return;
        }
        // A single non-figurative bare word that is not a coded-set name is code-name-2 (§12.3.7.3 SR15 —
        // implementor-defined). Figurative words are literal-phrase operands (GR10). ONE lookup, shared with the
        // alphanumeric arm since kb/Work PB770 — that arm had no SR15 check at all. ⚠ The table holds no
        // code-name-2 row today (kb/Work PB793 decided the ALPHANUMERIC spellings only), so this arm always
        // refuses; it is a LOOKUP and not a refusal so that a national row would bind with no code change.
        switch (AlphabetCodeName(name, def, national: true, out var codeName))
        {
            case CodeNameOutcome.Refused: return;
            case CodeNameOutcome.Bound:
                NationalAlphabets.TryAdd(name, new NationalAlphabetDef(codeName!.Table, null,
                    HasCollatingSequence: true, codeName.Name, codeName));
                return;
        }
        if (AlphabetLiteralPhrase(name, def, national: true) is { } table)
            NationalAlphabets.TryAdd(name, new NationalAlphabetDef(table, HasCollatingSequence: true, "literal-phrase"));
    }

    /// <summary>The ALPHABET clause's view of <see cref="LiteralPhraseOperand"/>: the operands of one alphabet
    /// entry (literal-1 and the THROUGH / ALSO operands), each held to §12.3.7.3 SR14 b (ALPHANUMERIC) or SR14 c
    /// (NATIONAL). Both arms had their own copy until kb/Work PB770, and the alphanumeric copy implemented NONE of
    /// the rules (<c>feedback_two_arm_dispatch</c>, fifth instance); the CLASS clause had a third copy until kb/Work
    /// PB976, which is why the decoder is now shared by clause rather than by arm.</summary>
    private List<PhraseOperand> AlphabetOperands(Core.AlphabetEntryContext entry, LiteralPhraseRules rules)
    {
        var result = new List<PhraseOperand>();
        for (int i = 0; i < entry.ChildCount; i++)
            if (entry.GetChild(i) is Core.LiteralContext or Core.CobolWordContext
                && LiteralPhraseOperand(entry.GetChild(i), rules) is { } operand)
                result.Add(operand);
        return result;
    }

    /// <summary>One decoded operand of a literal phrase: the characters it stands for AND the parse node it was
    /// WRITTEN as. The SYNTAX RULES that speak of "each alphanumeric / national LITERAL" (§12.3.7.3 SR14 b3/c3,
    /// SR17 b4/c4 — one character under THROUGH) are asked of the operand as written, never of the decoded characters:
    /// a NUMERIC literal is an ordinal (SR14 b1/c1, SR17 b2/c2), names one character of its set by construction, and
    /// is no national literal (kb/Work PB1091's L7.4 measurement — a numeric ordinal naming a supplementary
    /// character decodes to a two-code-unit surrogate pair and was refused under c4).</summary>
    /// <param name="Members">How many CHARACTERS of a coded set the operand names — more than one only for a numeric
    /// ordinal under a CLASS clause's IN phrase that lands on an ALSO group (§12.3.7.4 GR7 k6; kb/Work PB1095). It is a
    /// count of characters, never of code units, so a supplementary character (two code units) is still one member.</param>
    private readonly record struct PhraseOperand(Antlr4.Runtime.Tree.IParseTree Node, string Characters, int Members = 1)
    {
        /// <summary>True when the operand is written as a numeric literal that is an integer — an ORDINAL.</summary>
        public bool IsOrdinal => Node is Core.LiteralContext { } lit && lit.numericLiteral() is { } numeric
            && IntegerLiteralDigits(numeric) is not null;

        /// <summary>The operand as the source spelled it, for a diagnostic.</summary>
        public string Spelling => Node.GetText();
    }

    /// <summary>⛔ THE SYNTAX RULES A SPECIAL-NAMES CLAUSE HOLDS ITS LITERAL OPERANDS TO — one value per clause, so
    /// <see cref="LiteralPhraseOperand"/> can never report one clause's rule under another's name (the PB770 leg d
    /// misfiling: a general helper with OPTIONAL arguments defaulted every caller to the CLASS clause's identity).
    /// §12.3.7.3 states the same four operand rules twice, once per clause, under different item numbers:
    /// <list type="bullet">
    /// <item>SR14 (ALPHABET literal-phrase) — b1/c1 the numeric-literal ordinal, b2/c2 the noninteger literal's
    /// class, b3/c3 one character "when a THROUGH or ALSO phrase is specified", b4/c4 the count;</item>
    /// <item>SR17 (CLASS) — b2/c2 the ordinal (in the native set "or, when the IN phrase is specified, … the
    /// character set referenced by alphabet-name-4"), b3/c3 the class, b4/c4 one character "when a THROUGH phrase
    /// is specified", b5/c5 the count.</item>
    /// </list>
    /// The b-arm applies when the ALPHANUMERIC phrase is specified or implied, the c-arm under NATIONAL.</summary>
    private sealed record LiteralPhraseRules(
        string What, bool National, DiagnosticDescriptor Code, string RuleNumber,
        char OrdinalItem, char ClassItem, char LengthItem, string LengthPhrases,
        CodedCharacterSet? InSet)
    {
        /// <summary>"SR14 b1" / "SR17 c3" — the sub-rule of THIS clause, in THIS class.</summary>
        public string Rule(char item) => $"{RuleNumber} {(National ? 'c' : 'b')}{item}";

        /// <summary>The class every noninteger operand shall be (SR14 b2/c2, SR17 b3/c3).</summary>
        public LiteralClass LiteralClass => National ? LiteralClass.National : LiteralClass.Alphanumeric;

        public static LiteralPhraseRules Alphabet(string name, bool national) => new(
            $"ALPHABET {name}{(national ? " FOR NATIONAL" : "")}", national, DiagnosticCatalog.AlphabetClauseViolation,
            "SR14", '1', '2', '3', "a THROUGH or ALSO phrase", InSet: null);

        public static LiteralPhraseRules Class(string name, bool national, CodedCharacterSet? inSet) => new(
            $"CLASS {name}{(national ? " FOR NATIONAL" : "")}", national, DiagnosticCatalog.ClassClauseViolation,
            "SR17", '2', '3', '4', "a THROUGH phrase", inSet);
    }

    /// <summary>The symbolic-character names the SPECIAL-NAMES paragraph being bound declares (see
    /// <c>SwitchBindSpecialNames</c>) — read by the §12.3.7.3 SR11 arm of <see cref="LiteralPhraseOperand"/>.</summary>
    private HashSet<string> _paragraphSymbolicNames = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>⛔ THE ONE SPECIAL-NAMES literal-operand decoder (ISO §12.3.7.3 SR14 b/c for the ALPHABET clause,
    /// SR17 b/c for the CLASS clause — see <see cref="LiteralPhraseRules"/>): the characters one operand stands
    /// for, or null when it violates its clause's rule (the diagnostic is already reported).</summary>
    /// <remarks>The ordinal rule — "<i>Each numeric literal shall be an unsigned integer and shall have a value
    /// within the range of one through the maximum number of characters in the native … character set</i>" — is
    /// 1-based, so it names code unit <c>ordinal − 1</c> of the 65,536-character repertoire (§12.3.7.4 GR7 k1a),
    /// or, for a CLASS clause with an IN phrase, the character at that ordinal of alphabet-name-4's set (GR12 a).
    /// The class rule — "<i>Each noninteger literal shall be an alphanumeric / a national literal</i>" — is decided
    /// by the literal's own prefix. GR10 — the figurative words written inside SPECIAL-NAMES are the NATIVE
    /// extremes/values of the clause's class.
    /// <para>⛔ A numeric ordinal under <c>IN</c> names EVERY character at that ordinal of alphabet-name-4's set —
    /// an ALSO group has several (GR7 k6; <see cref="CodedCharacterSet.CharactersAt"/>, kb/Work PB1095) — and the
    /// operand carries how many, because a THROUGH phrase needs exactly one (<see cref="OneCharacterOperands"/>).</para></remarks>
    private PhraseOperand? LiteralPhraseOperand(Antlr4.Runtime.Tree.IParseTree operand, LiteralPhraseRules r)
    {
        PhraseOperand? Of(string? characters, int members = 1) =>
            characters is null ? null : new PhraseOperand(operand, characters, members);
        switch (operand)
        {
            // ⛔ A FIGURATIVE CONSTANT IS A LITERAL, not a word: `nonNumericLiteral : figurativeConstant | …`, so
            // SPACE / LOW-VALUE / QUOTE reach here through the literal arm whenever the spelling is a lexer token —
            // which is every ordinary spelling. The cobolWord arm below is the non-tokenized route (a >>COBOL-WORDS
            // synonym). Both call the ONE GR10 mapping. (The CLASS clause used to decode a figurative constant as
            // its own SPELLING — `CLASS C IS SPACE` was the class {S, P, A, C, E} — kb/Work PB976's sweep.)
            case Core.LiteralContext figLit when figLit.nonNumericLiteral()?.figurativeConstant() is { } fig:
                return Of(SpecialNamesFigurative(fig, FigurativeSlotOf(r)));
            case Core.LiteralContext lit:
                string text = lit.GetText();
                // The predefined NULL is no literal and no figurative constant (§8.4.3.10.3 SR1): the one refusal.
                if (lit.nonNumericLiteral()?.predefinedNull() is not null)
                {
                    PredefinedNullRule.Report(Edition, $"{r.What}: an operand of the literal phrase — each operand is a "
                        + $"numeric literal, {(r.National ? "a NATIONAL" : "an alphanumeric")} literal or a figurative "
                        + $"constant (ISO §12.3.7.3 {r.Rule(r.ClassItem)})");
                    return null;
                }
                // §12.3.7.3 SR11 — literal-1 … literal-6 "shall specify neither a symbolic-character figurative
                // constant nor a zero-length literal" (a symbolic character is no literal token here, so only the
                // second half can be written).
                if (CobolLiteral.IsZeroLength(text))
                {
                    Edition.Error(r.Code, $"{r.What}: {text} — an operand shall not be a zero-length literal "
                        + "(ISO §12.3.7.3 SR11)");
                    return null;
                }
                if (lit.numericLiteral() is { } numeric && IntegerLiteralDigits(numeric) is ({ } digits, var signed))
                {
                    // The ordinal rule, BOTH halves of one sentence: "shall be an UNSIGNED INTEGER **and** shall
                    // have a value within the range …". ⛔ WHICH RULE is decided by the literal's FORM — an integer
                    // literal is held to the ordinal rule whatever its value — never by whether its text fits an
                    // `int` (kb/Work PB1557's sibling): `int.TryParse` failed on an 11-digit ordinal and sent it to
                    // the CLASS rule below, which reported "each noninteger literal shall be an alphanumeric
                    // literal" for a literal that is an integer. A NONINTEGER numeric literal (1.5) is not an
                    // integer, so it is the class rule's noninteger literal and falls through to it (kb/Work PB770).
                    if (signed)
                    {
                        Edition.Error(r.Code, $"{r.What}: {text} — each numeric literal shall be an UNSIGNED integer "
                            + $"(ISO §12.3.7.3 {r.Rule(r.OrdinalItem)})");
                        return null;
                    }
                    int ordinal = CobolNet.Validation.IntegerOperandRules.HostValue(digits);
                    if (r.InSet is { } inSet)
                    {
                        // GR12 a — the ordinal names every character at that position of the IN set (an ALSO group's
                        // members, GR7 k6); the SYMBOLIC CHARACTERS clause alone takes literal-1 (SwitchBindSymbolic).
                        if (inSet.CharactersAt(ordinal) is { } named) return Of(string.Concat(named), named.Count);
                        Edition.Error(r.Code, $"{r.What}: the ordinal {text} does not exist in the character set "
                            + $"referenced by the IN alphabet ({inSet.Phrase}, {inSet.OrdinalCount} characters) — ISO "
                            + $"§12.3.7.3 {r.Rule(r.OrdinalItem)}");
                        return null;
                    }
                    if (ordinal is >= 1 and <= CollatingTable.Repertoire) return Of(((char)(ordinal - 1)).ToString());
                    Edition.Error(r.Code, $"{r.What}: the ordinal {text} does not exist in the native "
                        + $"{(r.National ? "national" : "alphanumeric")} character set ({CollatingTable.Repertoire} "
                        + "characters) — each numeric literal shall be an unsigned integer with a value from one "
                        + $"through the maximum number of characters in that set (ISO §12.3.7.3 {r.Rule(r.OrdinalItem)})");
                    return null;
                }
                if (OperandLiteralClass(lit) == r.LiteralClass)
                    return Of(LiteralCharsOf(lit, sr11: true));
                // The class rule. A noninteger literal of the wrong class: name the rule, then RECOVER with the
                // literal's characters when it is a string at all, so one bad operand does not cascade.
                Edition.Error(r.Code, $"{r.What}: {text} — each noninteger literal shall be "
                    + $"{(r.National ? "a NATIONAL literal (N\"…\")" : "an alphanumeric literal")} "
                    + $"(ISO §12.3.7.3 {r.Rule(r.ClassItem)})");
                return Of(CobolLiteral.IsStringLiteral(text) ? CobolLiteral.Decode(text) : null);
            case Core.CobolWordContext w:
                // GR10: the NATIVE extremes/values of the clause's class. ⛔ There is NO `_ => t` fallback: an
                // unrecognized word is code-name-1/-2, which AlphabetCodeName has already resolved or refused for
                // the only shape it can legally take, and inside a multi-operand phrase it is a class-rule
                // violation — never the characters of its own spelling (kb/Work PB770 leg e).
                if (AlphabetFigurative(w.GetText()) is { } wordValue) return Of(wordValue);
                // §12.3.7.3 SR11 — literal-1 … literal-6 "shall specify neither a symbolic-character figurative
                // constant nor a zero-length literal": a symbolic-character name reaches here as a word, and it is
                // THAT rule it breaks, not the class rule below (kb/Work PB226).
                if (IsSymbolicCharacterName(w.GetText()))
                {
                    Edition.Error(r.Code, $"{r.What}: {w.GetText()} — an operand shall not be a symbolic-character "
                        + "figurative constant (ISO §12.3.7.3 SR11)");
                    return null;
                }
                Edition.Error(r.Code, $"{r.What}: {w.GetText()} is not a literal — each operand shall be a numeric "
                    + $"literal, {(r.National ? "a NATIONAL" : "an alphanumeric")} literal or a figurative constant "
                    + $"(ISO §12.3.7.3 {r.Rule(r.ClassItem)}; §12.3.7.4 GR10)");
                return null;
        }
        return null;
    }

    /// <summary>The digits and sign of a numeric literal that is an INTEGER — a fixed-point integer core with an
    /// optional sign (§8.3.3.3.2) — or null for a noninteger one (decimal, floating-point). The SPECIAL-NAMES
    /// ordinal rule (ISO §12.3.7.3 SR14 b1/c1, SR17 b2/c2) is asked of the PARSE, not of the text:
    /// <c>int.TryParse</c> accepts a sign and FAILS on an integer too long for an <c>int</c>, so it answered
    /// "is this an integer?" and "what is its value?" as one question and got the first wrong whenever the second
    /// overflowed. The VALUE is read by THE one ordinal reader, <c>IntegerOperandRules.HostValue</c> — the same one
    /// SYMBOLIC CHARACTERS integer-1 (SR16 e/f) is read by, so the three ordinal sites of this paragraph (ALPHABET,
    /// CLASS, SYMBOLIC CHARACTERS) cannot disagree about an integer too long for an <c>int</c> (kb/Work PB1091,
    /// PB1557's sibling): it saturates, and a saturated ordinal is out of range of every character set exactly when
    /// the literal is.</summary>
    private static (string Digits, bool Signed)? IntegerLiteralDigits(Core.NumericLiteralContext numeric)
    {
        if (numeric.signedNumericLiteral() is not { } s || s.numericLiteralCore() is not { ChildCount: 1 } core)
            return null;
        bool separateSign = s.PLUS() is not null || s.MINUS() is not null;
        if (core.INTEGERLIT() is [{ } unsigned]) return (unsigned.GetText(), separateSign);
        // A sign-adjacent literal (a FUNCTION-argument lexer region) is one token whose first character is the sign.
        if (core.SIGNED_INTEGERLIT() is { } adjacent) return (adjacent.GetText()[1..], true);
        return null;
    }

    /// <summary>⛔ WHERE A FIGURATIVE CONSTANT STANDS IN A SPECIAL-NAMES LITERAL SLOT, as data — what the slot is
    /// called, how it reports a refusal, what class it demands of an <c>ALL literal-1</c> operand and how it phrases
    /// the §12.3.7.3 SR11 symbolic-character bar. One value per CLAUSE KIND (kb/Work PB1537): the ALPHABET / CLASS
    /// operands (<see cref="FigurativeSlotOf"/>) and the ORDER TABLE / LOCALE text literals
    /// (<see cref="TryClauseTextLiteral"/>) read their figurative constants through the ONE
    /// <see cref="SpecialNamesFigurative"/>, which had been written only for the first two — the third screen refused
    /// EVERY figurative constant under a rule that bars only one kind of them.</summary>
    /// <param name="What">The clause, for the message ("ALPHABET AL", "ORDER TABLE T1 IS SPACE").</param>
    /// <param name="Refuse">Reports one refusal under the slot's own diagnostic.</param>
    /// <param name="ClassClause">The slot's class rule in its own words (appended to the operand).</param>
    /// <param name="ClassRule">The syntax rule it is, e.g. "SR14 b2".</param>
    /// <param name="Admits">Whether an <c>ALL literal-1</c> of that category is admitted.</param>
    /// <param name="SymbolicClause">The SR11 sentence for a symbolic-character operand.</param>
    private readonly record struct FigurativeSlot(string What, Action<string> Refuse, string ClassClause,
        string ClassRule, Func<PicCategory, bool> Admits, string SymbolicClause);

    /// <summary>The ALPHABET / CLASS clause's figurative slot (§12.3.7.3 SR14 b2/c2, SR17 b3/c3, SR11).</summary>
    private FigurativeSlot FigurativeSlotOf(LiteralPhraseRules r) => new(r.What,
        message => Edition.Error(r.Code, message),
        $"each noninteger literal shall be {(r.National ? "a NATIONAL literal (N\"…\")" : "an alphanumeric literal")}",
        r.Rule(r.ClassItem),
        category => category == (r.National ? PicCategory.National : PicCategory.Alphanumeric),
        "an operand shall not be a symbolic-character figurative constant");

    /// <summary>⛔ THE ONE DECODING of a figurative constant written in a SPECIAL-NAMES literal slot (ISO §12.3.7.4
    /// GR10; §8.3.3.6.3 SR1 — "<i>A figurative constant may be used whenever 'literal' appears in a format</i>"): the
    /// characters it stands for, or null after a refusal. SPACE, QUOTE and ZERO are their §8.3.3.6.4 GR1 characters;
    /// HIGH-VALUE and LOW-VALUE are the NATIVE extremes (GR10); <c>ALL literal-1</c> is literal-1 itself (§8.3.3.6.4
    /// GR3 c, "<i>the length of the string is the length of literal-1</i>"), <c>ALL SPACE</c> and its kin one
    /// character (GR3 b). The ONE refusal the slots share is SR11's "<i>neither a symbolic-character figurative
    /// constant nor a zero-length literal</i>": <c>ALL symbolic-character-1</c> is the symbolic-character figurative
    /// constant a literal-slot can still spell (Format 7), and was reported as "not a character" before — a real
    /// rule answering a different question. ⛔ It used to be REFUSED outright in an ALPHABET literal phrase under that
    /// same SR11 (kb/Work PB976's sweep) and, in the ORDER TABLE / LOCALE screen, STILL was (kb/Work PB1537).</summary>
    private string? SpecialNamesFigurative(Core.FigurativeConstantContext fig, FigurativeSlot slot)
    {
        if (fig.ALL() is null) return AlphabetFigurative(fig.GetText()) ?? FigurativeNotACharacter(fig, slot);
        if (fig.cobolWord() is { } word && IsSymbolicCharacterName(word.GetText()))
        {
            slot.Refuse($"{slot.What}: {ConcatFolder.Spelling(fig)} — {slot.SymbolicClause} (ISO §12.3.7.3 SR11)");
            return null;
        }
        return ConcatFolder.FoldAllLiteral(fig, LiteralEnvironment.SpecialNames(this, refusesSymbolicCharacters: true),
                Edition) is { } literal1 ? AllLiteralCharacters(fig, literal1, slot)
            : AlphabetFigurative(fig.GetChild(1).GetText()) is { } oneChar ? oneChar
            : FigurativeNotACharacter(fig, slot);
    }

    /// <summary>A figurative constant that names no character (<c>ALL</c> over a word that is neither a literal nor
    /// a constant) — never an operand of these clauses: §12.3.7.4 GR10 maps HIGH-VALUE and LOW-VALUE, §8.3.3.6.4 GR1
    /// SPACE, QUOTE and ZERO, and nothing maps the rest. (The predefined NULL is no figurative constant at all —
    /// §8.4.3.10.3 SR1, <see cref="PredefinedNullRule"/>.)</summary>
    private static string? FigurativeNotACharacter(Core.FigurativeConstantContext fig, FigurativeSlot slot)
    {
        slot.Refuse($"{slot.What}: {ConcatFolder.Spelling(fig)} — the figurative constant is not a character of the native "
            + "character set, so it cannot be an operand of this clause (ISO §12.3.7.4 GR10 names HIGH-VALUE and "
            + "LOW-VALUE; §8.3.3.6.4 GR1 SPACE, QUOTE and ZERO)");
        return null;
    }

    /// <summary>The characters of an <c>ALL literal-1</c> operand: literal-1 itself (§8.3.3.6.4 GR3 c, "the length
    /// of the string is the length of literal-1"), folded by the ONE Format 6 reader
    /// (<see cref="ConcatFolder.FoldAllLiteral"/> — a quoted literal, a concatenation expression classed by the
    /// pairwise §8.8.3.3 GR1 fold, or a constant-name) and held to the slot's class rule as ONE literal: a
    /// concatenation expression is "equivalent to a literal of the same class and value" (§8.8.3.3 GR3).</summary>
    private static string? AllLiteralCharacters(Core.FigurativeConstantContext fig, ConcatFolder.Folded literal1,
        FigurativeSlot slot)
    {
        if (slot.Admits(literal1.Category)) return literal1.Value;
        slot.Refuse($"{slot.What}: {ConcatFolder.Spelling(fig)} — {slot.ClassClause} (ISO §12.3.7.3 {slot.ClassRule})");
        return null;
    }

    /// <summary>The CLASS of a noninteger literal operand — alphanumeric (§8.3.3.2, incl. the hexadecimal format
    /// X"hh…"), national (§8.3.3.5 — N"…" / NX"…") or boolean — as §12.3.7.3 SR14 b2/c2 and SR17 b3/c3 ask it; null for a
    /// word that is no quoted literal. ⛔ ONE classifier, <see cref="CobolLiteral.ClassOf"/>, keyed on the parsed
    /// PREFIX: the former alphanumeric test asked <c>CobolLiteral.IsStringLiteral</c>, which answers true for EVERY
    /// prefixed quoted literal, so N"A" and B"1" passed as alphanumeric and <c>ALPHABET A IS N"A"</c> /
    /// <c>CLASS C IS N"0" THRU N"9"</c> compiled clean (kb/Work PB976). A §8.8.3.3 concatenation expression is of
    /// the class §8.8.3.3 GR1 folds pairwise — THE ConcatFolder answer, never the leading operand's prefix, which
    /// called <c>SPACE &amp; N"A"</c> alphanumeric (kb/Work PB1406).</summary>
    private LiteralClass? OperandLiteralClass(Core.LiteralContext lit)
    {
        if (lit.nonNumericLiteral()?.concatenationExpression() is { } ce)
            return ConcatFolder.ClassOf(ce, LiteralEnvironment.SpecialNames(this, refusesSymbolicCharacters: true)) switch
            {
                PicCategory.National => LiteralClass.National,
                PicCategory.Boolean => LiteralClass.Boolean,
                _ => LiteralClass.Alphanumeric,
            };
        return CobolLiteral.ClassOf(lit.GetText());
    }

    /// <summary>⛔ THE ONE §12.3.7.4 GR10 mapping of a figurative constant written INSIDE the SPECIAL-NAMES
    /// paragraph to the character it names: "<i>When specified as literals in the SPECIAL-NAMES paragraph, the
    /// figurative constants HIGH-VALUE and LOW-VALUE are associated with those characters having the highest and
    /// lowest positions, respectively, in the native national collating sequence, when the NATIONAL phrase is
    /// specified, or in the native alphanumeric collating sequence otherwise</i>" — so they are the NATIVE extremes
    /// here, never the sequence being defined. SPACE, QUOTE and ZERO are their §8.3.3.6 characters.
    /// <para>Null for any other word, which is then not an operand at all. Both native sequences answer HIGH-VALUE
    /// U+FFFF (owner decision kb/Work R52 — <see cref="CobolNet.Runtime.NativeCollatingSequence"/>).</para></summary>
    private static string? AlphabetFigurative(string word) => word.ToUpperInvariant() switch
    {
        "HIGH-VALUE" or "HIGH-VALUES" => CobolNet.Runtime.NativeCollatingSequence.HighValue.ToString(),
        "LOW-VALUE" or "LOW-VALUES" => CobolNet.Runtime.NativeCollatingSequence.LowValue.ToString(),
        "SPACE" or "SPACES" => " ",
        "QUOTE" or "QUOTES" => "\"",
        "ZERO" or "ZEROS" or "ZEROES" => "0",
        _ => null,
    };

    /// <summary>Whether <paramref name="word"/> is a symbolic-character name of this paragraph — declared by any
    /// SYMBOLIC CHARACTERS clause, read syntactically because the clauses are order-free (kb/Work PB226), or already
    /// bound. The ONE recognition §12.3.7.3 SR11 asks, of a bare operand and of a concatenation operand alike.</summary>
    internal bool IsSymbolicCharacterName(string word) =>
        _paragraphSymbolicNames.Contains(word) || SymbolicCharacters.ContainsKey(word);

    /// <summary>The literal environment of a position OUTSIDE the SPECIAL-NAMES paragraph (kb/Work PB1406).</summary>
    internal LiteralEnvironment LiteralEnv => LiteralEnvironment.Program(this);

    /// <summary>The characters of an alphabet-entry string literal: a §8.8.3.3 GR3 concatenation folded first — in
    /// the SPECIAL-NAMES environment (§12.3.7.4 GR10's native extremes), with §12.3.7.3 SR11's
    /// symbolic-character prohibition when <paramref name="sr11"/> —, the §8.3.3.2 hexadecimal format decoded
    /// pairwise, otherwise the literal's own characters. ⛔ It never resolves an ORDINAL — that is SR14 b1/c1's job
    /// and it lives in <see cref="AlphabetOperands"/>, so the ALPHABET path can no longer inherit the CLASS clause's
    /// descriptor, message and rule number (kb/Work PB770 leg d).
    /// <para>⛔ A word that is not a literal is not a literal (kb/Work PB791): this used to END with <c>return text</c>,
    /// the SPELLING of whatever it was handed, which is how <c>ALPHABET X IS SPACE</c> positioned S, P, A, C, E
    /// (PB770 fixed that arm) and <c>CURRENCY SIGN IS SPACE PICTURE SYMBOL "#"</c> bound the currency string "SPACE"
    /// (the other caller). Both callers screen the operand's class first (<see cref="LiteralPhraseOperand"/>,
    /// <see cref="CurrencyTextLiteral"/>), so what reaches here is a quoted literal — hexadecimal included, which
    /// <see cref="CobolLiteral.Decode"/> decodes itself — or a concatenation expression, and anything else is a caller
    /// that skipped its screen: a defect to find at once, never a value to invent.</para></summary>
    private string LiteralCharsOf(Core.LiteralContext lit, bool sr11)
    {
        if (lit.nonNumericLiteral()?.concatenationExpression() is { } ce)
            return ConcatFolder.Fold(ce, Edition, LiteralEnvironment.SpecialNames(this, sr11)).Value;
        string text = lit.GetText();
        if (CobolLiteral.IsStringLiteral(text)) return CobolLiteral.Decode(text);
        throw new InvalidOperationException($"'{text}' is neither a quoted literal nor a concatenation expression — "
            + "the caller shall refuse an operand of any other form before decoding it (kb/Work PB791)");
    }
    /// <summary>One <c>CLASS class-name-1 [FOR {ALPHANUMERIC | NATIONAL}] IS {literal-5 [THROUGH literal-6]}…
    /// [IN alphabet-name-4]</c> clause (ISO §12.3.7.2): expand each value item to its member characters — a
    /// multi-character literal lists each character; a THRU pair contributes the contiguous native-collating range
    /// between the two single characters, ASCENDING OR DESCENDING (§12.3.7.4 GR12 — NC174A's <c>"D" THROUGH "A"</c>
    /// equals <c>"A" THRU "D"</c>).
    /// <para>⛔ THE FOR PHRASE IS READ ONCE, HERE, AND EVERY §12.3.7.3 SR17 RULE KEYS ON THAT ONE READING (kb/Work
    /// PB976). It used to be parsed and IGNORED, so the whole class-dependent half of SR17 — b1/c1 the IN alphabet's
    /// class, b3/c3 each noninteger literal's class, b4/c4 one character under THROUGH — went unenforced:
    /// <c>CLASS C IS N"0" THRU N"9"</c> and <c>CLASS C FOR NATIONAL IS 1 THRU 5 IN an-alphanumeric-alphabet</c>
    /// compiled clean. SR17 a — "<i>When neither the ALPHANUMERIC phrase nor the NATIONAL phrase is specified, the
    /// ALPHANUMERIC phrase is implied</i>" — is the default below. The operand rules are the ONE decoder the ALPHABET
    /// clause uses (<see cref="LiteralPhraseOperand"/>), under the CLASS clause's own rule numbers
    /// (<see cref="LiteralPhraseRules.Class"/>).</para></summary>
    private void SwitchBindClass(Core.ClassDefinitionClauseContext cd)
    {
        // CLASS … FOR ALPHANUMERIC/NATIONAL — the FOR phrase edition gate is VersionConformancePass
        // ParseArm.VisitClassDefinitionClause (14g.4, recognition).
        using var _ = Edition.At(cd);
        string name = cd.cobolWord(0).GetText();
        DeclareUserWord(name, UserWordKind.ClassName);   // §8.3.2.2 — the one declaration funnel (kb/Work PB1083)
        bool national = ForPhraseIsNational(cd.specialNamesForPhrase(), cd.misplacedSpecialNamesForPhrase());
        // The IN phrase (ISO §12.3.7.4 GR12 a; kb/Work PB110): a NUMERIC literal is the ordinal of a character
        // within the character set referenced by alphabet-name-4 — not the native set. SR17 d (a LOCALE alphabet)
        // and an undeclared name refuse through the ONE resolver; the clause then binds no class (its references
        // stay loud) rather than silently reverting to native ordinals.
        CodedCharacterSet? inSet = null;
        if (cd.cobolWord().Length > 1)
        {
            string inName = cd.cobolWord(1).GetText();
            inSet = CodedCharacterSetOf(inName, $"CLASS {name} … IN {inName}",
                "ISO §12.3.7.3 SR17 d — alphabet-name-4 shall not reference an alphabet specified with the LOCALE phrase");
            if (inSet is null) return;
            // SR17 b1/c1 — "When the IN phrase is specified, alphabet-name-4 shall reference an alphabet that defines
            // an alphanumeric / a national character set". The SYMBOLIC CHARACTERS clause's SR16 e1/f1 twin is the
            // same test in SwitchBindSymbolic.
            if (inSet.National != national)
            {
                Edition.Error(DiagnosticCatalog.ClassClauseViolation, $"CLASS {name}{(national ? " FOR NATIONAL" : "")} "
                    + $"IN {inName}: alphabet-name-4 shall reference an alphabet that defines "
                    + $"{(national ? "a NATIONAL" : "an ALPHANUMERIC")} character set — this alphabet is "
                    + $"{(inSet.National ? "FOR NATIONAL" : "alphanumeric")} (ISO §12.3.7.3 SR17 {(national ? "c1" : "b1")})");
                return;
            }
        }
        var rules = LiteralPhraseRules.Class(name, national, inSet);
        var members = new System.Text.StringBuilder();
        foreach (var item in cd.classValueSet().classValueItem())
        {
            var lits = item.literal();
            if (LiteralPhraseOperand(lits[0], rules) is not { } lo) continue;
            if (lits.Length < 2) { members.Append(lo.Characters); continue; }
            // SR17 b4/c4 — "Each alphanumeric / national literal, when a THROUGH phrase is specified, shall be one
            // character in length." A multi-character operand used to be taken as a plain literal-5 with literal-6
            // silently dropped (`"AB" THRU "C"` was the class {A, B}).
            if (LiteralPhraseOperand(lits[1], rules) is not { } hi
                || !OneCharacterOperands([lo, hi], rules, through: true)) continue;
            // GR12: "the contiguous characters in the NATIVE character set beginning with … literal-5, and ending
            // with … literal-6" — native, even under IN (GR12 a/b place each END in alphabet-name-4's set).
            char a = lo.Characters[0], b = hi.Characters[0];
            if (a > b) (a, b) = (b, a);
            for (char c = a; ; c++) { members.Append(c); if (c == b) break; }
        }
        string set = members.ToString();
        // SR17 b5/c5 — "The number of characters specified shall not exceed the number of characters in the native
        // … character set or, when the IN phrase is specified, the number of characters in the character set
        // referenced by alphabet-name-4." Without IN it cannot fire (distinct members of a 65,536-character
        // repertoire cannot outnumber it); under IN a smaller set (STANDARD-1's 128) can be exceeded.
        if (inSet is not null)
        {
            int distinct = set.Distinct().Count();
            if (distinct > inSet.OrdinalCount)
                Edition.Error(DiagnosticCatalog.ClassClauseViolation, $"{rules.What} … IN {cd.cobolWord(1).GetText()}: "
                    + $"{distinct} characters are specified — more than the {inSet.OrdinalCount} characters of the "
                    + $"character set referenced by alphabet-name-4 ({inSet.Phrase}) (ISO §12.3.7.3 {rules.Rule('5')})");
        }
        UserClasses.TryAdd(name, new UserClassDef(set, national));
    }

    /// <summary>⛔ THE ONE READING OF A SPECIAL-NAMES FOR PHRASE, for all three clauses that print it (ALPHABET,
    /// CLASS, SYMBOLIC CHARACTERS — ISO §12.3.7.2): NATIONAL when the phrase says so, ALPHANUMERIC otherwise
    /// (§12.3.7.3 SR13, SR16 d, SR17 a — "the ALPHANUMERIC phrase is implied"). The phrase belongs between the
    /// clause's name and IS; one written after the definition (<paramref name="misplaced"/>) has already been
    /// refused by name in ClosedFormatPass (kb/Work PB977), and is read here only so the rest of the clause binds
    /// in the class the user evidently meant instead of cascading class-rule diagnostics.</summary>
    private static bool ForPhraseIsNational(Core.SpecialNamesForPhraseContext? iso,
        Core.MisplacedSpecialNamesForPhraseContext? misplaced)
        => (iso ?? misplaced?.specialNamesForPhrase())?.NATIONAL() is not null;

    /// <summary>The one-character rule a THROUGH (or, in an alphabet, ALSO) phrase imposes on its operands —
    /// §12.3.7.3 SR14 b3/c3 for the ALPHABET clause, SR17 b4/c4 for the CLASS clause, one check for both. False,
    /// with each offending operand reported, when any is not exactly one character.
    /// <para>⛔ The syntax rule governs "each alphanumeric / national LITERAL", so it is asked of the operand AS WRITTEN
    /// (<see cref="PhraseOperand.IsOrdinal"/>): a NUMERIC literal is an ordinal, names exactly one character of its set,
    /// and is held to the ordinal rule instead (SR14 b1/c1, SR17 b2/c2). What a THROUGH phrase then needs of that
    /// character is a POSITION in the native set: §12.3.7.4 GR12 (CLASS) expands "the contiguous characters in the native
    /// character set" between the bounds and GR7 k5 (ALPHABET) "the set of consecutive characters in the native character
    /// set", and the native national set is the 65,536 UTF-16 code units (DOC-A.1-188; docs/CONFORMANCE.md) — so a
    /// numeric ordinal under <c>IN</c> UCS-4 / UTF-8 that names a SUPPLEMENTARY character (two code units) is a character
    /// with no position in that run, and is refused under the rule that says so, not under c4 (kb/Work PB1091). An ALSO
    /// phrase (ALPHABET only, GR7 k6: the operands share ONE ordinal position) asks the same of each operand.</para></summary>
    /// <param name="through">True for a THROUGH phrase, false for an ALSO phrase — the rule the supplementary-character
    /// refusal names.</param>
    private bool OneCharacterOperands(IReadOnlyList<PhraseOperand> operands, LiteralPhraseRules r, bool through)
    {
        bool ok = true;
        foreach (var op in operands)
        {
            if (op.IsOrdinal)
            {
                if (op.Characters.Length == 1) continue;
                if (op.Members > 1)
                {
                    // An ordinal that lands on an ALSO group (GR7 k6) names SEVERAL characters, and a THROUGH bound
                    // is ONE character: the specification defines no run that begins at, or ends at, a group.
                    Edition.Error(r.Code, $"{r.What}: the ordinal {op.Spelling} names {op.Members} characters of the "
                        + "character set referenced by the IN alphabet (an ALSO group shares one ordinal position, "
                        + "ISO §12.3.7.4 GR7 k6), so it is no single character to begin or end the run of native "
                        + "characters a THROUGH phrase expands (ISO §12.3.7.4 GR12)");
                    ok = false;
                    continue;
                }
                Edition.Error(r.Code, $"{r.What}: the ordinal {op.Spelling} names a character outside the native "
                    + $"{(r.National ? "national" : "alphanumeric")} character set (it needs {op.Characters.Length} of its "
                    + "16-bit code units), so it has no position in "
                    + (through
                        ? "the run of native characters a THROUGH phrase expands "
                          + $"(ISO §12.3.7.4 {(r.RuleNumber == "SR17" ? "GR12" : "GR7 k5")}"
                        : "the native character set whose characters an ALSO phrase gives one shared ordinal position "
                          + "(ISO §12.3.7.4 GR7 k6")
                    + "; the native set is the 65,536 UTF-16 code units — docs/CONFORMANCE.md DOC-A.1-188)");
                ok = false;
                continue;
            }
            string chars = op.Characters;
            if (chars.Length != 1)
            {
                Edition.Error(r.Code, $"{r.What}: the operand '{chars}' is {chars.Length} characters — each "
                    + $"{(r.National ? "national" : "alphanumeric")} literal, when {r.LengthPhrases} is specified, shall "
                    + $"be one character in length (ISO §12.3.7.3 {r.Rule(r.LengthItem)})");
                ok = false;
            }
        }
        return ok;
    }

    /// <summary>Bind one SYMBOLIC CHARACTERS clause (ISO §12.3.7.2; §12.3.7.3 SR16; §12.3.7.4 GR11 — kb/Work
    /// PB110: the clause was accepted-inert): each symbolic-character-1 defines a FIGURATIVE CONSTANT whose value
    /// is the character at ordinal integer-1 in the native character set of the clause's class (ALPHANUMERIC
    /// implied — SR16 d) or, under IN, in the coded character set of alphabet-name-3 (GR11 b/c; SR16 e1/f1 the
    /// class of that alphabet; SR16 g / the ONE resolver refuse a LOCALE alphabet). The pairing is positional
    /// per ENTRY (SR16 b), names then integers, with a one-to-one correspondence (SR16 c); a name may be defined
    /// once across ALL the paragraph's SYMBOLIC CHARACTERS clauses (SR16 a).</summary>
    private void SwitchBindSymbolic(Core.SymbolicCharactersClauseContext sc)
    {
        using var _ = Edition.At(sc);
        bool national = ForPhraseIsNational(sc.specialNamesForPhrase(), sc.misplacedSpecialNamesForPhrase());
        CodedCharacterSet? inSet = null;
        if (sc.cobolWord() is { } inWord && inWord is not null)
        {
            inSet = CodedCharacterSetOf(inWord.GetText(), $"SYMBOLIC CHARACTERS … IN {inWord.GetText()}",
                "ISO §12.3.7.3 SR16 g — alphabet-name-3 shall not reference an alphabet specified with the LOCALE phrase");
            if (inSet is null) return;
            if (inSet.National != national)
            {
                Edition.Error(DiagnosticCatalog.SymbolicCharactersViolation, $"SYMBOLIC CHARACTERS{(national ? " FOR NATIONAL" : "")} "
                    + $"IN {inWord.GetText()}: alphabet-name-3 shall reference an alphabet that defines "
                    + $"{(national ? "a NATIONAL" : "an ALPHANUMERIC")} character set — this alphabet is "
                    + $"{(inSet.National ? "FOR NATIONAL" : "alphanumeric")} (ISO §12.3.7.3 SR16 {(national ? "f1" : "e1")})");
                return;
            }
        }
        foreach (var entry in sc.symbolicCharacterEntry())
        {
            var names = entry.cobolWord();
            var ords = entry.integerLiteral();
            if (names.Length != ords.Length)
            {
                Edition.Error(DiagnosticCatalog.SymbolicCharactersViolation, $"SYMBOLIC CHARACTERS: {names.Length} "
                    + $"symbolic-character name(s) against {ords.Length} integer(s) — there shall be a one-to-one "
                    + "correspondence, paired by position (ISO §12.3.7.3 SR16 b/c)");
                continue;
            }
            for (int i = 0; i < names.Length; i++)
            {
                string symName = names[i].GetText();
                DeclareUserWord(symName, UserWordKind.SymbolicCharacter);   // §8.3.2.2 (kb/Work PB1083)
                // integer-1 is an INTEGERLIT — unsigned digits by the grammar (§5.5 1)) — so only its magnitude can
                // be wrong. ⛔ It used to be read with `int.TryParse` and a failure `continue`d: an ordinal too long
                // for an int bound NOTHING and drew NO diagnostic, and the name then surfaced as "not defined" at
                // its first use (kb/Work PB1557's sibling). It is an integer-n, so it is read by THE ONE integer-n
                // reader — the very one the ALPHABET and CLASS literal phrases read their ordinals through
                // (LiteralPhraseOperand; kb/Work PB1091) — which saturates at int.MaxValue: a saturated value is out
                // of range of every character set, so the SR16 check below reports it.
                string ordText = ords[i].GetText();
                int ordinal = CobolNet.Validation.IntegerOperandRules.HostValue(ords[i]);
                string? value = inSet is not null ? inSet.CharAt(ordinal)
                    : ordinal is >= 1 and <= CollatingTable.Repertoire ? ((char)(ordinal - 1)).ToString() : null;
                if (value is null)
                {
                    Edition.Error(DiagnosticCatalog.SymbolicCharactersViolation, $"SYMBOLIC CHARACTERS {symName} IS "
                        + $"{ordText}: the ordinal position does not exist in the "
                        + $"{(inSet is not null ? $"character set referenced by the IN alphabet ({inSet.Phrase}, {inSet.OrdinalCount} characters)" : $"native character set ({CollatingTable.Repertoire} characters)")}"
                        + $" — ISO §12.3.7.3 SR16 {(national ? "f" : "e")}{(inSet is not null ? "1" : "2")}");
                    // RECOVERY, so one bad ordinal does not cascade: the name is DECLARED (DeclareUserWord above), and a
                    // statement that uses it must find it defined rather than draw a second, false diagnostic ("'BIG' is
                    // not defined") for a name the program did write. The placeholder value is never observed — the
                    // clause already failed, so the compilation produces no program (the posture LiteralPhraseOperand's
                    // class-rule recovery takes for an operand of the wrong class). kb/Work PB1091.
                    SymbolicCharacters.TryAdd(symName, ("\0", national));
                    continue;
                }
                if (!SymbolicCharacters.TryAdd(symName, (value, national)))
                    Edition.Error(DiagnosticCatalog.SymbolicCharactersViolation, $"SYMBOLIC CHARACTERS: '{symName}' is "
                        + "already defined — a given symbolic-character-1 may be specified only once within the SYMBOLIC "
                        + "CHARACTER clauses of this SPECIAL-NAMES paragraph (ISO §12.3.7.3 SR16 a)");
            }
        }
    }
}

/// <summary>A SPECIAL-NAMES user-defined class (ISO §12.3.7.2 CLASS clause): its EXPANDED member characters and
/// whether it was declared <c>FOR NATIONAL</c> (§12.3.7.3 SR17 a — ALPHANUMERIC is implied otherwise).</summary>
public sealed record UserClassDef(string Members, bool National);

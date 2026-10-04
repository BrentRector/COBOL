// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Antlr4.Runtime;
using CobolNet.Binding.Bound;
using CobolNet.Binding.Model;
using CobolNet.Editions.Diagnostics;
using CobolNet.Common;
using CobolNet.Frontend.Generated;
using CobolNet.Runtime;

namespace CobolNet.Binding.Procedure;

using Core = CobolParserCore;

/// <summary>The SET verb binder (P7 Step 10m — a real collaborator over <see cref="BinderContext"/>): the
/// seventeen-format dispatch of ISO §14.9.39.2. A format a RESERVED WORD identifies is chosen by its own
/// grammar alternative in <see cref="BindSet"/> (3 switch, 4 condition, 6 attribute, 11/12 locale, 13
/// last-exception, 15 content, and the ENTRY / ADDRESS OF / SIZE OF phrases); the formats that SHARE a token
/// shape — 1, 2, 5, 7, 8, 9, 10, 14, 16 — are selected SEMANTICALLY, and that selection is
/// <see cref="SetFormatSelection"/>'s alone.
/// <para>⛔ THE CHAIN OF PER-FORMAT PEEKS IS GONE (kb/Work PB449 + PB456). Each candidate format used to try in
/// contract order — the F10 pointer peek first, then the F14 CAPACITY-register peek, then the F5 object
/// re-route — and each read <c>receivers[0]</c> (and, for the TO direction, the sender) and returned null
/// otherwise. Three consequences, all measured: the same operands gave different verdicts in different orders,
/// a receiving list no printed format admits fell through to Format 1/2 arithmetic, and a re-route that
/// declined left NOTHING behind it. Now the format is decided once, from the whole receiving list, and each
/// format's own binder is reached having already been chosen — so what those binders screen is the RULE
/// (SR8/SR17/SR20/SR21/SR23), uniformly over every operand.</para>
/// <para>Switches go via the .AlterSwitches host edge (10n), objects via the OO host edge (10s).
/// <see cref="SetTargetOf"/> lives HERE (the host keeps a forwarder for ControlFlowBinder until 10t).</para></summary>
internal sealed class SetBinder(BinderContext ctx, StatementBinder host)
{
    /// <summary>⛔ THE ONE FORMAT SELECTOR (kb/Work PB449 + PB456). Every shape-sharing SET format is chosen
    /// HERE, from the WHOLE receiving list, before any format's own binder runs — see
    /// <see cref="SetFormatSelection"/> for why a per-format peek at <c>receivers[0]</c> could not work.</summary>
    private readonly SetFormatSelection _fmt = new(ctx, host);

    /// <summary>⛔ THE ONE CLASSIFIER OF A `SET … TO` SENDER (kb/Work PB1399 + PB1929): a data reference, a
    /// function-identifier, an inline method invocation, or none of them — read once, so every format asks its own
    /// syntax rule of the same <see cref="SetSender"/>.</summary>
    private readonly SetSenders _senders = new(host);

    /// <summary>Bind a SET statement, dispatching by format (ISO §14.9.39; COBOLNET_DESIGN §12.3). The COBOL-85
    /// surface — Format 1 index/value assignment, Format 2 UP/DOWN BY, Format 4 condition-name TO TRUE — binds here;
    /// the later-edition formats (switches need SPECIAL-NAMES, pointers/objects their 2002 subsystems, TO FALSE the
    /// 2002 FALSE phrase) fail loud by NAME until their subsystem lands.</summary>
    public BoundStatement BindSet(Core.SetStatementContext set)
    {
        // F11 set-locale / F12 save-locale (§14.9.39; Annex A.4.9 item 9) — IMPLEMENTED since kb/Work PB64 T1 (it
        // used to be refused by name with COBOLNET1518 — kb/Work PB92 before that: "'LOCALE' is not defined").
        if (set.setLocaleStatement() is { } sl)
        {
            bool save = sl.setLocaleSource() is null;   // F12 has no Format-11 TO operand (the grammar splits the two formats)
            return save ? BindSaveLocale(sl) : BindSetLocale(sl);
        }
        // F6 attribute (§14.9.39; Annex A.4.2 item 24) — DECLINED: the screen module. It had NO grammar
        // alternative at all before kb/Work PB260, so `SET SG ATTRIBUTE HIGHLIGHT ON` was a generic COBOL0001
        // parse error pointing at ATTRIBUTE rather than a named refusal naming the facility.
        if (set.setScreenAttributeStatement() is { } sat)
        {
            ScreenFacility.ReportSetAttribute(ctx.Edition, sat.dataReference().GetText());
            return BoundRejected.Reported(ctx.Edition);
        }
        if (set.setLastExceptionStatement() is not null) return host.Ec.BindSetLastException();   // F13 (ISO §14.9.39; 2002+)
        if (set.setContentStatement() is { } sc) return BindSetContent(sc);   // F15 numeric-content (2014; kb/Work PB452)
        if (set.setEntryStatement() is { } se) return BindSetEntry(se);   // F9 + §8.4.3.13 ENTRY sender (P10 Step 7)
        if (set.setSizeStatement() is { } ss)
            return BindSetSize(ss.dataReference(), ss.arithmeticExpression());   // F16 explicit SIZE OF (2023)
        if (set.setToValueStatement() is { } tv) return BindSetTo(tv);
        if (set.setIndexStatement() is { } ud) return BindSetUpDown(ud);
        if (set.setBooleanStatement() is { } b) return BindSetCondition(b);
        if (set.setSwitchStatement() is { } sw) return host.Alter.SwitchBindSet(sw);   // Format 3 — external switches (ISO §14.9.39)
        if (set.setFunctionAddressStatement() is { } sfa) return BindSetFunctionAddress(sfa);   // F8 + §8.4.3.12 sender (kb/Work PB452)
        if (set.setProgramAddressStatement() is { } spa) return BindSetProgramAddress(spa);     // F9 + §8.4.3.13 sender (kb/Work PB549)
        if (set.setAddressStatement() is { } sa)
            return host.Ptr.BindSetAddress(sa);   // F7 both directions + ADDRESS OF senders (Phase-4b inc 2)
        if (set.setObjectReferenceStatement() is { } sor)
        {
            // `SET receivers… TO {NULL | SELF | SUPER | reference}` is the shape of Formats 5, 7, 8 and 9 at
            // once, so the format is SELECTED from the whole receiving list (§14.9.39.2; kb/Work PB449) and the
            // chosen format's own syntax rule then refuses any operand it does not admit — SR17 (Format 7),
            // SR20 (Format 8), SR21 (Format 9), SR8 (Format 5). This used to sniff `dataReference(0)` alone,
            // which made `SET P1 X TO NULL` and `SET X P1 TO NULL` two different verdicts.
            // ⛔ WITHOUT THE FUNCTION-POINTER ARM a declarable function-pointer falls through into the OO
            // Format-5 bind and, through BindSetTo, into the Format-1 arithmetic store — the silent wrong
            // answer PB817 filed as "the other end, which a fixer will otherwise miss".
            var sorRefs = sor.dataReference();
            var objRef = sor.objectReference();
            // The grammar spells NULL / SELF / SUPER here (a data-reference sender parses as setToValueStatement), so
            // a data reference is the rare spelling and a function-identifier cannot reach this arm at all.
            var sorSender = objRef.dataReference() is { } sorRef ? SetSender.OfReference(sorRef) : null;
            bool sorNull = objRef.predefinedNull() is not null;
            bool sorSelf = objRef.SELF() is not null;
            bool sorSuper = objRef.SUPER() is not null;
            return SetFormatSelection.Select(_fmt.KindsOf(sorRefs), SetDirections.To, out _) switch
            {
                // ⛔ THE SENDER'S OWN WORD TRAVELS WITH THE FLAG (kb/Work PB388). `sorSelf || sorSuper` collapses
                // two different statements into one bool, and the three arms behind it then had nothing to name
                // but the pair — so a program that wrote SUPER was told about "SELF/SUPER" beside a receiver
                // list rendered as `…`. `objRef.GetText()` is the word as written; the flag still selects the arm.
                SetFormat.F7 => BindSetPointer(sorRefs, sorSender, sorNull, sorSelf || sorSuper,
                    objRef.GetText()),
                SetFormat.F9 => BindSetProgramPointer(sorRefs, sorSender, sorNull, sorSelf || sorSuper,
                    objRef.GetText()),
                SetFormat.F8 => BindSetFunctionPointer(sorRefs, sorSender, sorNull, sorSelf || sorSuper,
                    objRef.GetText()),
                // ⛔ THE SECOND ARM OF THE SAME DISPATCH (kb/Work PB453; feedback_two_arm_dispatch). `SET MT TO
                // NULL` arrives HERE rather than through BindSetTo — NULL is not an arithmeticExpression — and
                // it was this arm that produced the §14.9.39.3 SR8 object-reference diagnostic the note measured.
                SetFormat.F17 => BindMessageTagSet(sorRefs, objRef.GetText()),
                // Format 5, and the residual: NULL/SELF/SUPER are senders of no other format, so a receiving
                // list that selects Format 1/14/16 here is refused by SR8 inside — the diagnostic that names
                // what the statement was trying to be.
                _ => host.Oo.OoBindSetObjectRef(sorRefs, sorSender,
                        senderNull: sorNull, senderSelf: sorSelf, senderSuper: sorSuper),
            };
        }
        return BoundRejected.Report(ctx.Edition, DiagnosticCatalog.StatementFormatShape, $"SET '{set.GetText()}': the statement matches no SET format (ISO §14.9.39.2)");
    }

    /// <summary><c>SET CONTENT OF { identifier-14 } … TO { FARTHEST-FROM-ZERO [IN-ARITHMETIC-RANGE] |
    /// FLOAT-INFINITY | FLOAT-NOT-A-NUMBER | FLOAT-NOT-A-NUMBER-SIGNALING | NEAREST-TO-ZERO
    /// [IN-ARITHMETIC-RANGE] } [SIGN {NEGATIVE|POSITIVE}]</c> — ISO §14.9.39.2 Format 15 (numeric-content),
    /// COBOL-2014; kb/Work PB452.
    /// <para>⛔ MANDATORY BASE LANGUAGE, not an optional module: Annex A.3's processor-dependent list does not
    /// contain it and Annex A.4's ENTIRE SET inventory is three items — format 6 (A.4.2 #24), format 14
    /// (A.4.4 #3) and formats 11/12 (A.4.9 #9) — so §4.2.7 cannot decline it either.</para>
    /// <para>The value is a compile-time property of EACH receiver's own data description (and, under
    /// IN-ARITHMETIC-RANGE, of the arithmetic mode), so the statement resolves here to one store per receiver —
    /// which is also why several receivers of different descriptions each get a DIFFERENT value. The extremes
    /// come from <see cref="AlgebraicRanges"/>, the same evaluator the §15.43/§15.58/§15.83 intrinsics read:
    /// Annex D.32 states the equivalence outright ("<c>SET CONTENT OF numeric-item TO FARTHEST-FROM-ZERO</c> …
    /// the same as … <c>MOVE HIGHEST-ALGEBRAIC (numeric-item) TO numeric-item</c>"), so a second computation
    /// here would be one rule written down twice.</para>
    /// <para>SCREENS: SR31 — FARTHEST-FROM-ZERO / NEAREST-TO-ZERO take a NUMERIC data item (§8.5.2.12), which a
    /// numeric-EDITED item is not (§8.5.2.13), so this is NARROWER than the intrinsics' §15.x.3 r1 →
    /// COBOLNET1938; SR31 a) — the SIGN phrase is required when the receiver's two farthest-from-zero magnitudes
    /// differ → COBOLNET1939; SR32 — the three float words take a STANDARD floating-point usage (§3.166/§3.167,
    /// NOT COMP-1/COMP-2/FLOAT-SHORT/-LONG/-EXTENDED) → COBOLNET1940.</para></summary>
    private BoundStatement BindSetContent(Core.SetContentStatementContext sc)
    {
        var v = sc.setContentValue();
        bool farthest = v.FARTHEST_FROM_ZERO() is not null;
        bool nearest = v.NEAREST_TO_ZERO() is not null;
        bool inArithmeticRange = v.IN_ARITHMETIC_RANGE() is not null;
        // GR32 c / GR36 c / GR33-GR35 all end the same way: "If the SIGN phrase is specified, the sign … is set
        // according to the SIGN specification; otherwise … positive." One reading, applied to every leg.
        bool negative = v.setContentSign()?.NEGATIVE() is not null;
        bool signWritten = v.setContentSign() is not null;

        var stores = new List<SetContentStore>();
        foreach (var dref in sc.dataReference())
        {
            if (host.Expr.ResolveReceiving(dref) is not { } place)
                return BoundRejected.Reported(ctx.Edition);   // the receiving chokepoint reported it — not a deferral (kb/Work PB236, PB881)
            string name = place.Item.CobolName ?? dref.GetText();
            var pic = place.Item.Pic;

            if (farthest || nearest)
            {
                // SR31: "identifier-14 shall reference a numeric data item" — §8.5.2.12's category, so an
                // EDITED item, a group, an index item and a reference-modified reference are all refused.
                if (place is RefModPlace || place.Item.IsGroup
                    || pic is not { Category: PicCategory.Numeric } || pic.Usage is Usage.Index
                    || AlgebraicRanges.Of(pic, ctx.Data.DecimalPointIsComma) is not { } range)
                {
                    return BoundRejected.Report(ctx.Edition, DiagnosticCatalog.SetContentNotNumeric,
                        $"SET CONTENT OF '{name}' TO {(farthest ? "FARTHEST-FROM-ZERO" : "NEAREST-TO-ZERO")}: "
                        + "identifier-14 shall reference a numeric data item (ISO §14.9.39.3 syntax rule 31)");
                }
                // SR31 a): the receiver's positive and negative extremes differ in magnitude — the two's-
                // complement containers of §13.18.60.4 GR12 — so "the value farthest away from zero permitted
                // by the specifications of identifier-14" (GR32 a) does not name one value, and the SIGN phrase
                // resolves it. Measured on the range itself, never on a usage list, so a future capacity
                // discipline with the same asymmetry inherits the rule.
                // ⚠ SR31 b) (the NEAREST-TO-ZERO twin) has NO violating input in this implementation and is
                // deliberately not written as a second test: the nonzero value nearest to zero is 10^(−scale)
                // for every fixed-point description and the carrier's subnormal minimum for every float one,
                // both of them MAGNITUDES that are identical in the two directions. It would become writable
                // only if AlgebraicRange gained a directional Nearest, and inventing one now would be a lookup
                // nothing reads (feedback_a_dead_lookup_is_also_unverified).
                if (farthest && !signWritten && range.FarthestNegative is { } fneg
                    && AlgebraicRanges.CompareMagnitude(range.Farthest, fneg) != 0)
                {
                    return BoundRejected.Report(ctx.Edition, DiagnosticCatalog.SetContentSignRequired,
                        $"SET CONTENT OF '{name}' TO FARTHEST-FROM-ZERO: this item's positive and negative "
                        + $"values farthest from zero are {range.Farthest} and {fneg}, whose absolute values "
                        + "differ, so the SIGN phrase shall be specified (ISO §14.9.39.3 syntax rule 31 a)");
                }
                // GR32 a / GR36 a — the extreme the receiver's own description permits, in the direction the
                // SIGN phrase selects. An UNSIGNED receiver has no negative extreme: GR32 a still yields its
                // farthest-from-zero value and GR32 c then asks for a negative sign the item cannot hold, so
                // the literal is negated and the ORDINARY store rule drops the sign — no special case here.
                string? magnitude = farthest
                    ? (negative ? range.FarthestNegative ?? "-" + range.Farthest : range.Farthest)
                    : (negative ? (range.Nearest is { } n ? "-" + n : null) : range.Nearest);
                // Null only for a FLOATING-POINT numeric-edited description, whose nearest-to-zero value is
                // deliberately unmodelled — and SR31 has already refused every edited receiver above (category
                // numeric-edited is not category numeric). CHECKED rather than asserted, so this arm can never
                // become a silent null dereference if that screen ever moves.
                if (magnitude is null)
                {
                    return BoundRejected.Report(ctx.Edition, DiagnosticCatalog.SetContentNotNumeric,
                        $"SET CONTENT OF '{name}' TO NEAREST-TO-ZERO: identifier-14 shall reference a numeric "
                        + "data item (ISO §14.9.39.3 syntax rule 31)");
                }
                string value = inArithmeticRange ? ClampToArithmeticRange(magnitude, farthest) : magnitude;
                // The store is BOUND here, not synthesized at emission: BoundMove classifies its per-target
                // dispatch at construction and the binder's storage-fact passes only see nodes that exist by
                // the end of binding (kb/Work PB348 — an emitter-built move escaped both and aborted a run).
                stores.Add(new SetContentStore(place,
                    new BoundMove(new BoundComputedOperand(new BoundNumLiteral(value)), [place]), Ieee: null));
                continue;
            }

            // SR32: FLOAT-INFINITY / FLOAT-NOT-A-NUMBER / FLOAT-NOT-A-NUMBER-SIGNALING take "a data item
            // described with a STANDARD floating-point usage" — §3.166's float-binary-32/-64/-128 and §3.167's
            // float-decimal-16/-34 and nothing else. The two families come from the ONE place they are written
            // down (UsageFamilies), so a future member is admitted here automatically (kb/Work PB174).
            if (place is RefModPlace || pic is not { } fpic
                || !(UsageFamilies.IsStandardBinaryFloat(fpic.Usage) || UsageFamilies.IsStandardDecimalFloat(fpic.Usage)))
            {
                return BoundRejected.Report(ctx.Edition, DiagnosticCatalog.SetContentNotStandardFloat,
                    $"SET CONTENT OF '{name}' TO {FloatWordOf(v)}: identifier-14 shall reference a data item "
                    + "described with a standard floating-point usage — FLOAT-BINARY-32/-64/-128 (ISO §3.166) or "
                    + "FLOAT-DECIMAL-16/-34 (§3.167). FLOAT-SHORT, FLOAT-LONG, FLOAT-EXTENDED, COMP-1 and COMP-2 "
                    + "are floating-point but not STANDARD floating-point, and carry no ISO/IEC 60559:2020 basic "
                    + "interchange format for the canonical representation to be taken from "
                    + "(ISO §14.9.39.3 syntax rule 32)");
            }
            stores.Add(new SetContentStore(place, Store: null,
                Ieee: IeeeSpecialOf(v), NegativeSign: negative));
        }
        return new BoundSetContent(stores);
    }

    /// <summary>§14.9.39.4 GR32 b) / GR36 b) — the IN-ARITHMETIC-RANGE phrase: the content is set either to the
    /// receiver's own extreme or to "the value … permitted by the specifications appropriate to the mode of
    /// arithmetic", whichever is CLOSER to zero for FARTHEST-FROM-ZERO and FARTHER from zero for NEAREST-TO-ZERO.
    /// The mode's extremes are <see cref="ArithmeticModes.IntermediateExtremes"/>; the comparison is exact
    /// scaled-BigInteger, because these magnitudes reach 10^±6176 and no CLR numeric type spans that.
    /// <para>⚠ LATENT BY MEASUREMENT, NOT BY ASSUMPTION: for every data description WiseOwl COBOL can declare today
    /// the receiver's bound wins or ties (the widest carrier is binary64, whose extremes ARE the native
    /// intermediate's, and the standard modes' SDIDI is wider still), so this method currently always returns
    /// its argument. It is written as the real comparison anyway — the clamp starts biting the moment a wider
    /// carrier lands (a true IEEE binary128 FLOAT-BINARY-128 reaches 1.19E+4932, past the native intermediate),
    /// and then it bites without anyone having to remember it.</para></summary>
    private string ClampToArithmeticRange(string value, bool farthest)
    {
        var (modeFarthest, modeNearest) = ArithmeticModes.IntermediateExtremes(ctx.Data.Options.Arithmetic);
        string bound = farthest ? modeFarthest : modeNearest;
        int cmp = AlgebraicRanges.CompareMagnitude(value, bound);
        // FARTHEST: take the smaller magnitude. NEAREST: take the larger.
        bool takeBound = farthest ? cmp > 0 : cmp < 0;
        if (!takeBound) return value;
        return value.StartsWith('-') ? "-" + bound : bound;
    }

    /// <summary>Which of the three float value words §14.9.39.2 Format 15 wrote — for the SR32 diagnostic.</summary>
    private static string FloatWordOf(Core.SetContentValueContext v) =>
        v.FLOAT_INFINITY() is not null ? "FLOAT-INFINITY"
        : v.FLOAT_NOT_A_NUMBER() is not null ? "FLOAT-NOT-A-NUMBER"
        : "FLOAT-NOT-A-NUMBER-SIGNALING";

    /// <summary>§14.9.39.4 GR33/GR34/GR35 — WHICH canonical value Format 15 wrote. The parse tree is read
    /// exactly here and nowhere further in; the ENCODINGS and the Annex A.1 item 176 payload determination they
    /// stand for belong to <see cref="IeeeSpecials"/> (docs/CONFORMANCE.md §7, DOC-A.1-176), and the spelling
    /// belongs to the backend.</summary>
    private static IeeeSpecial IeeeSpecialOf(Core.SetContentValueContext v) =>
        v.FLOAT_INFINITY() is not null ? IeeeSpecial.Infinity
        : v.FLOAT_NOT_A_NUMBER() is not null ? IeeeSpecial.QuietNaN
        : IeeeSpecial.SignalingNaN;

    /// <summary><c>SET LOCALE {category… | USER-DEFAULT} TO {identifier-10 | locale-name-1 | USER-DEFAULT | SYSTEM-DEFAULT}</c>
    /// (ISO §14.9.39 Format 11; DESIGN-locale-facility §4.3; kb/Work PB64 T1). The first operand is USER-DEFAULT (GR22 —
    /// the user default is set) or a SET of categories (the printed brace carries choice indicators — §5.2.6.4: one or
    /// more, each at most once, any order; a duplicate is COBOLNET1666). The TO operand is one dataReference split here:
    /// USER-DEFAULT / SYSTEM-DEFAULT (GR23b/c; SR25 — not with a USER-DEFAULT first operand → COBOLNET1667), a
    /// locale-name of the SPECIAL-NAMES LOCALE clause (SR26 — undeclared → COBOLNET1664), else identifier-10, which
    /// shall be an elementary data item of category data-pointer (SR27 → COBOLNET1668).</summary>
    private BoundStatement BindSetLocale(Core.SetLocaleStatementContext sl)
    {
        var words = sl.formatWord();                // [0] = LOCALE, [1..] = categories | USER-DEFAULT (formatWords, kb/Work PB764)
        bool setsUserDefault = false;
        var categories = LocaleCategorySet.None;
        // >>COBOL-WORDS (ISO §7.3.10.4 GR2/GR3/GR4; kb/Work PB250): USER-DEFAULT and the LC_ categories are
        // §8.9/§8.10 words the lexer does not tokenize, so the map reaches them only here.
        if (ctx.CobolWords.Is(words[1].GetText(), "USER-DEFAULT"))
        {
            setsUserDefault = true;
            if (words.Length > 2)
            {
                return BoundRejected.Report(ctx.Edition, DiagnosticCatalog.SetLocaleCategories, $"SET LOCALE USER-DEFAULT {words[2].GetText()}: USER-DEFAULT as the first operand stands alone — "
                    + "the general format's outer brace is a plain alternation of the category list OR USER-DEFAULT (ISO §14.9.39.2 format 11)");
            }
        }
        else
        {
            for (int i = 1; i < words.Length; i++)
            {
                string written = words[i].GetText().ToUpperInvariant();
                // The CANONICAL word decides the category (GR2/GR4); a de-reserved word resolves to null and
                // is not a category at all (GR3) - the diagnostic below then names it, as it should.
                string w = ctx.CobolWords.Resolve(written) ?? "";
                LocaleCategorySet cat = w switch
                {
                    "LC_ALL" => LocaleCategorySet.All,
                    "LC_COLLATE" => LocaleCategorySet.Collate,
                    "LC_CTYPE" => LocaleCategorySet.Ctype,
                    "LC_MESSAGES" => LocaleCategorySet.Messages,
                    "LC_MONETARY" => LocaleCategorySet.Monetary,
                    "LC_NUMERIC" => LocaleCategorySet.Numeric,
                    "LC_TIME" => LocaleCategorySet.Time,
                    _ => LocaleCategorySet.None,
                };
                if (cat == LocaleCategorySet.None)
                {
                    return BoundRejected.Report(ctx.Edition, DiagnosticCatalog.SetLocaleCategories, $"SET LOCALE {words[i].GetText()}: '{words[i].GetText()}' is not a locale category — "
                        + "the first operand is one or more of LC_ALL, LC_COLLATE, LC_CTYPE, LC_MESSAGES, LC_MONETARY, LC_NUMERIC, LC_TIME, "
                        + "or USER-DEFAULT (ISO §14.9.39.2 format 11)");
                }
                // §5.2.6.4 — "any single alternative shall be specified only once": the SAME word twice is the violation
                // (LC_ALL beside LC_TIME is two different alternatives — redundant, legal).
                bool duplicate = false;
                for (int j = 1; j < i; j++) if (ctx.CobolWords.Is(words[j].GetText(), w)) duplicate = true;
                if (duplicate)
                {
                    return BoundRejected.Report(ctx.Edition, DiagnosticCatalog.SetLocaleCategories, $"SET LOCALE … {w}: the category {w} is specified more than once — each alternative "
                        + "of the category brace shall be specified at most once (ISO §14.9.39.2 format 11 / §5.2.6.4)");
                }
                categories |= cat;
            }
        }
        // The TO operand: USER-DEFAULT / SYSTEM-DEFAULT / locale-name-1 / identifier-10.
        // The grammar split the keywords from the names (setLocaleSource, kb/Work PB764): USER-DEFAULT / SYSTEM-DEFAULT
        // arrive as a formatWord, identifier-10 / locale-name-1 as the dataReference.
        var source = sl.setLocaleSource();
        string first = setsUserDefault ? "SET LOCALE USER-DEFAULT" : $"SET LOCALE {string.Join(' ', words.Skip(1).Select(x => x.GetText()))}";
        if (source.formatWord() is { } keyword)
        {
            string kw = keyword.GetText();
            bool user = ctx.CobolWords.Is(kw, "USER-DEFAULT");
            if (setsUserDefault)
            {
                return BoundRejected.Report(ctx.Edition, DiagnosticCatalog.SetLocaleUserDefaultSource, $"{first} TO {kw.ToUpperInvariant()}: if USER-DEFAULT is specified as the first operand, "
                    + "identifier-10 or locale-name-1 shall be specified in the TO phrase (ISO §14.9.39.3 SR25)");
            }
            return new BoundSetLocale(categories, false, user ? LocaleSetSource.UserDefault : LocaleSetSource.SystemDefault, null, null);
        }
        var to = source.dataReference();
        string toText = to.GetText();
        // A bare word that is a declared locale-name (SR26) — before any data item of the same spelling: the
        // locale-name is the format's FIRST listed meaning for a user word here, and §8.3.2.2 keeps the two name
        // types apart by context.
        if (to.ChildCount == 1 && ctx.Data.Locales.TryGetValue(toText, out var symbol))
            return new BoundSetLocale(categories, setsUserDefault, LocaleSetSource.LocaleName, symbol, null);
        // identifier-10: an elementary data item of category data-pointer (SR27).
        if (ctx.Refs.Probe(to) is { } probe)
        {
            if (probe.Item.Pic?.Category is not PicCategory.Pointer)
            {
                return BoundRejected.Report(ctx.Edition, DiagnosticCatalog.SetLocalePointerCategory, $"{first} TO {toText}: identifier-10 shall reference an elementary data item of category "
                    + $"data-pointer (ISO §14.9.39.3 SR27) — '{toText}' is {probe.Item.Pic?.Category.ToString() ?? "a group"}");
            }
            if (host.Expr.ResolveSending(to) is var toAnswer && toAnswer.Place is not { } place)
                return toAnswer.Refusal(ctx.Edition);   // the resolver's answer (kb/Work PB1030)
            return new BoundSetLocale(categories, setsUserDefault, LocaleSetSource.SavedPointer, null, place);
        }
        // Neither: the ONE undeclared-locale-name diagnostic, with this site named (SR26).
        ctx.Data.ResolveLocaleName(toText, $"{first} TO {toText}",
            "ISO §14.9.39.3 SR26 — locale-name-1 shall be specified in the LOCALE clause of the SPECIAL-NAMES paragraph; "
            + "or SR27 — identifier-10 shall reference an elementary data item of category data-pointer, and no such item is declared");
        return BoundRejected.Reported(ctx.Edition);
    }

    /// <summary><c>SET identifier-11 TO LOCALE {LC_ALL | USER-DEFAULT}</c> (ISO §14.9.39 Format 12; kb/Work PB64 T1):
    /// identifier-11 shall be an elementary data item of category data-pointer (SR28 → COBOLNET1668); the words after TO
    /// are LOCALE and LC_ALL (GR26) or USER-DEFAULT (GR27) — the predicate admitted exactly those.</summary>
    private BoundStatement BindSaveLocale(Core.SetLocaleStatementContext sl)
    {
        bool userDefault = ctx.CobolWords.Is(sl.formatWord(1).GetText(), "USER-DEFAULT");
        var target = sl.dataReference();
        if (host.Expr.ResolveReceiving(target) is not { } place || place.Item.Pic?.Category is not PicCategory.Pointer)
        {
            return BoundRejected.Report(ctx.Edition, DiagnosticCatalog.SetLocalePointerCategory, $"SET {target.GetText()} TO LOCALE {(userDefault ? "USER-DEFAULT" : "LC_ALL")}: identifier-11 shall "
                + "reference an elementary data item of category data-pointer (ISO §14.9.39.3 SR28)");
        }
        return new BoundSaveLocale(place, userDefault);
    }

    /// <summary><c>SET receivers… TO value</c> (ISO §14.9.39 Format 1). Receivers may mix index-names and data
    /// items; the sender is any integer-valued operand (an index-name sender reads its occurrence number, §3.5).</summary>

    /// <summary>⛔ AN INDEX-NAME OPERAND IS A CATEGORY ERROR, NEVER AN UNDEFINED NAME (kb/Work PB388), and it
    /// reaches SIX operand positions, not one. The carrier re-routes — data-pointer (§14.9.39 Format 7),
    /// function-pointer (Format 8), program-pointer (Format 9) — are selected by whichever operand HAS a
    /// carrier category, so an index-name can stand on the other side of any of them. Each position then
    /// resolved through <c>ctx.Refs.Resolve</c>, which answers for a DATA ITEM only, and the FIRST thing the
    /// user read was COBOLNET1639: "'IX' is not defined — no declaration in this source element gives the name
    /// 'IX'" — false about a name INDEXED BY declared, and it sends the reader hunting a typo. §13.18.38.3 r7
    /// closes the list of contexts that may reference an index-name and the SET statement IS one of them, as
    /// Format 1 (§14.9.39.3 SR1). So the statement is still refused — for the category, which is the real
    /// reason. ONE helper, asked FIRST at every one of the six positions.</summary>
    private bool SetIndexNameOperand(Core.DataReferenceContext? dref, string position, string carrier,
                                     string cite)
    {
        if (dref is null || host.Expr.IndexFieldOf(dref) is null) return false;
        ctx.Edition.Error(DiagnosticCatalog.PointerOperandShape,
            $"SET '{DataBinder.WrittenText(dref)}': an index-name cannot be the {position} of a {carrier} SET — that operand "
            + $"shall be of category {carrier} ({cite}). An index-name operand belongs to Format 1, whose "
            + "receiver is a data item of class index or an integer data item (ISO §14.9.39.3 SR1)");
        return true;
    }

    /// <summary>⛔ THE ONE RESOLUTION OF A CARRIER FORMAT'S SENDER TO THE DATA ITEM IT REFERENCES (Formats 7, 8, 9 —
    /// kb/Work PB1929). A data reference resolves through the sending chokepoint (an index-name is refused by
    /// <see cref="SetIndexNameOperand"/> first); a function-identifier or inline method invocation already IS the
    /// data item §8.4.3.2.1 / §8.4.3.4.4 GR1 say it references, so its returned temporary is the answer with no
    /// second bind. A null place with a null <paramref name="refusal"/> is an identifier with NO item of its own — an
    /// intrinsic's computed value — which the caller's own category rule then refuses by name.</summary>
    private Place? CarrierSenderPlace(SetSender sender, string carrierWord, string cite, out BoundStatement? refusal)
    {
        refusal = null;
        if (sender.Ref is not { } senderRef) return sender.TemporaryItem;
        if (SetIndexNameOperand(senderRef, "sending operand", carrierWord, cite))
        {
            refusal = BoundRejected.Reported(ctx.Edition);
            return null;
        }
        var answer = host.Expr.ResolveSending(senderRef);
        if (answer.Place is { } place) return place;
        refusal = answer.Refusal(ctx.Edition);   // the resolver's answer (kb/Work PB1030)
        return null;
    }

    /// <summary>SET data-pointer assignment (§14.9.39 Format 7; Phase-4b increment 1): every target shall
    /// be USAGE POINTER (COBOLNET0869 otherwise); the sender is the NULL figurative or another data pointer
    /// (SELF/SUPER are object-only — 0869). A pointer-item sender is screened per receiver for §14.9.39.3 SR19's
    /// restricted-data-pointer rule through PtrBinder's one screen (kb/Work PB548); the <c>ADDRESS OF</c>
    /// spellings reach Format 7 through setAddressStatement / <c>PtrBinder.BindSetAddress</c>.</summary>
    private BoundStatement BindSetPointer(
        IReadOnlyList<Core.DataReferenceContext> targetRefs, SetSender? sender,
        bool toNull, bool senderIsSelfSuper, string? senderText = null)
    {
        if (senderIsSelfSuper)
        {
            // ⛔ NAME THE RECEIVERS AND THE WORD THE PROGRAM WROTE (kb/Work PB388's elision sweep). This read
            // `SET … TO SELF/SUPER`, and the diagnostic renderer transliterates U+2026 to ASCII, so the user
            // was shown `SET . TO SELF/SUPER` — neither the receivers they wrote nor the sender they wrote.
            return BoundRejected.Report(ctx.Edition, DiagnosticCatalog.PointerOperandShape,
                $"SET {SetFormatSelection.Written(targetRefs)} TO {senderText ?? "SELF/SUPER"}: SELF and SUPER "
                + "are object references, not data pointers "
                + "(ISO §14.9.39.2 Format 7 — the sender of a data-pointer SET is NULL or another pointer; "
                + "SELF and SUPER belong to Format 5, the object-reference assignment)");
        }
        var targets = new List<Place>(targetRefs.Count);
        foreach (var t in targetRefs)
        {
            if (SetIndexNameOperand(t, "receiving operand", "data-pointer", "ISO §14.9.39 Format 7, §14.9.39.3 SR17")) return BoundRejected.Reported(ctx.Edition);
            if (host.Expr.ResolveReceiving(t) is not { } tp || tp.Item.Pic?.Category is not PicCategory.Pointer)
            {
                return BoundRejected.Report(ctx.Edition, DiagnosticCatalog.PointerOperandShape,
                    $"SET '{t.GetText()}': the receiving operand of a data-pointer SET shall be of category "
                    + "data-pointer (ISO §14.9.39 Format 7, §14.9.39.3 SR17)");
            }
            targets.Add(tp);
        }
        Place? source = null;
        if (!toNull)
        {
            // ⛔ A SENDER THAT IS NOT A REFERENCE IS SR17's BUSINESS, NOT A FEATURE GAP (kb/Work PB456). The
            // format is chosen by the RECEIVERS, so a literal or an expression sender now ARRIVES here with
            // sender null — and identifier-6 "shall be of category data-pointer", which a literal is not.
            // This used to return BoundUnsupported, unreachably: the caller's re-route declined for exactly
            // this shape and dropped the statement into the Format-1 arithmetic store instead.
            if (sender is not { IsIdentifierOperand: true })
            {
                return BoundRejected.Report(ctx.Edition, DiagnosticCatalog.PointerOperandShape,
                    $"SET {SetFormatSelection.Written(targetRefs)} TO {senderText}: "
                    + "identifier-6 shall be of category data-pointer — a data-pointer sender is the predefined "
                    + "address NULL, another USAGE POINTER item, a function-identifier or inline method invocation "
                    + "of category data-pointer, or ADDRESS OF an identifier, never a literal or "
                    + "an arithmetic expression (ISO §14.9.39.2 Format 7, §14.9.39.3 SR17)");
            }
            var sp = CarrierSenderPlace(sender, "data-pointer", "ISO §14.9.39 Format 7, §14.9.39.3 SR17", out var senderRefusal);
            if (senderRefusal is not null) return senderRefusal;
            if (sp?.Item.Pic?.Category is not PicCategory.Pointer)
            {
                // ⛔ NAME THE RECEIVERS (kb/Work PB388). The message opened `SET … TO 'x'` and the diagnostic
                // renderer transliterates U+2026 to ASCII, so what the user actually read was `SET . TO 'WS-N'`
                // — a statement nobody wrote. The receivers are in hand. The "ADDRESS OF senders are a later
                // increment" tail went with it: `SET p TO ADDRESS OF x` has bound through PtrBinder's sender
                // form since Phase-4b increment 2, so the message named a non-support that no longer exists.
                return BoundRejected.Report(ctx.Edition, DiagnosticCatalog.PointerOperandShape,
                    $"SET {SetFormatSelection.Written(targetRefs)} TO "
                    + $"'{sender.Text}': a data-pointer sender shall be the predefined address NULL, "
                    + "another USAGE POINTER item, a function-identifier or inline method invocation of category "
                    + "data-pointer, or ADDRESS OF an identifier (ISO §14.9.39 Format 7)");
            }
            source = sp;
            // ⛔ THE THIRD ARM (kb/Work PB548). §14.9.39.3 SR19 — "If identifier-5 references a restricted
            // data-pointer, identifier-6 shall be the predefined address NULL or shall reference a data-pointer
            // restricted to the same type" — and its unnumbered continuation ("If identifier-6 references a
            // restricted data-pointer, either identifier-5 shall reference a data-pointer restricted to the same
            // type …") are one rule about each (receiver, sender) pair, asked through PtrBinder's ONE screen, the
            // same call BindSetAddress makes for its identifier-5 receivers. This route used to check only the
            // category, so `SET P TO R` stored an unrestricted address into a restricted data-pointer. TO NULL
            // (the branch not taken here) is admitted by SR19's own words.
            var senderRestriction = StrongTypeModel.PointerRestriction(sp.Item);
            for (int i = 0; i < targets.Count; i++)
            {
                if (!host.Ptr.ScreenPointerReceiverRestriction(targets[i], targetRefs[i].GetText(),
                        senderRestriction, sender.Text, addressSender: false))
                    return BoundRejected.Reported(ctx.Edition);
            }
        }
        // Every receiver on THIS route is identifier-5 (§14.9.39.3 SR17) — the `ADDRESS OF data-name-1`
        // spelling of the printed brace reaches Format 7 through setAddressStatement / PtrBinder instead, and
        // both routes build the SAME receiver list so GR12 and GR13 stay one loop in the emitter.
        return new BoundSetPointer([.. targets.Select(t => new BoundPointerReceiver(t, null))], source, toNull);
    }

    /// <summary>ISO §14.9.39.2 Format 17 (message-tag) — refused BY NAME, which is §4.2.6 ¶3's mandatory
    /// compile-time warning mechanism for a processor-dependent element this implementation does not support
    /// (Annex A.3 item 4; docs/CONFORMANCE.md §4 item 1). kb/Work PB453.
    /// <para>⛔ THE POINT IS THE NAME. The statement already failed — its operands' own MESSAGE-TAG entries are
    /// refused with COBOLNET1943 — but before this it went on to draw a rule from ANOTHER format: the screen
    /// keyed to §14.9.39.3 SR8 for a TO NULL sender (COBOLNET0867, "shall be a USAGE OBJECT REFERENCE data
    /// item" — the DIAGNOSTIC's words; SR8's own text is "Identifier-3 shall be any item of class object that is permitted as a receiving item"),
    /// and the §8.8.1.1 screen for a message-tag sender (COBOLNET0844, "of category
    /// alphanumeric is not a numeric operand", the diagnostic's words again). Both describe a statement the
    /// programmer did not write, and both point at a repair that is not one. The SAME facility's SEND/RECEIVE
    /// half has named itself since COBOLNET1578; this is its data half's statement arm.</para>
    /// <para>⚠ Format 17's SEMANTICS are deliberately NOT implemented, and that is an owner decision still
    /// owed rather than an omission: the printed format is <c>SET data-name-4 TO { data-name-5 | NULL }</c>,
    /// making data-name-4 the receiver, while §14.9.39.4 GR40 moves the content of data-name-4 INTO data-name-5
    /// and GR41 sets the content of data-name-5 — the two readings disagree about which operand is written, and
    /// SR35 constrains only the class. kb/Work PB453 §2 records the contradiction.</para></summary>
    private BoundStatement BindMessageTagSet(IReadOnlyList<Core.DataReferenceContext> receivers, string senderText)
    {
        ctx.Edition.Declined(DiagnosticCatalog.McsMessageTagSetUnsupported,
            $"SET {SetFormatSelection.Written(receivers)} TO {senderText}");
        return BoundRejected.Reported(ctx.Edition);
    }

    /// <summary>SET program-pointer assignment (ISO §14.9.39 Format 9; SR21 — every target AND the sender
    /// shall be category program-pointer; P10 Step 7): the data-pointer Format-7 twin over the ProgramPointer
    /// carrier. The sender is NULL or another program-pointer; SELF/SUPER are object references (0869).</summary>
    private BoundStatement BindSetProgramPointer(
        IReadOnlyList<Core.DataReferenceContext> targetRefs, SetSender? sender,
        bool toNull, bool senderIsSelfSuper, string? senderText = null)
    {
        if (senderIsSelfSuper)
        {
            return BoundRejected.Report(ctx.Edition, DiagnosticCatalog.PointerOperandShape,
                $"SET {SetFormatSelection.Written(targetRefs)} TO {senderText ?? "SELF/SUPER"}: SELF and SUPER "
                + "are object references, not program pointers "
                + "(ISO §14.9.39.2 Format 9 — the sender of a program-pointer SET is NULL, another "
                + "program-pointer, or an ENTRY program-address-identifier; SELF and SUPER belong to Format 5, "
                + "the object-reference assignment)");
        }
        var targets = new List<Place>(targetRefs.Count);
        foreach (var t in targetRefs)
        {
            if (SetIndexNameOperand(t, "receiving operand", "program-pointer", "ISO §14.9.39 Format 9, §14.9.39.3 SR21")) return BoundRejected.Reported(ctx.Edition);
            if (host.Expr.ResolveReceiving(t) is not { } tp || tp.Item.Pic?.Category is not PicCategory.ProgramPointer)
            {
                return BoundRejected.Report(ctx.Edition, DiagnosticCatalog.PointerOperandShape,
                    $"SET '{t.GetText()}': the receiving operand of a program-pointer SET shall be USAGE "
                    + "PROGRAM-POINTER (ISO §14.9.39.3 Format 9 SR21)");
            }
            targets.Add(tp);
        }
        Place? source = null;
        if (!toNull)
        {
            if (sender is not { IsIdentifierOperand: true })   // SR21 over a literal / expression sender (kb/Work PB456)
            {
                return BoundRejected.Report(ctx.Edition, DiagnosticCatalog.PointerOperandShape,
                    $"SET {SetFormatSelection.Written(targetRefs)} TO {senderText}: "
                    + "identifier-8 shall be of category program-pointer — a program-pointer sender is NULL, "
                    + "another USAGE PROGRAM-POINTER item, a function-identifier or inline method invocation of "
                    + "category program-pointer, or an ENTRY program-address-identifier, never a "
                    + "literal or an arithmetic expression (ISO §14.9.39.2 Format 9, §14.9.39.3 SR21)");
            }
            var sp = CarrierSenderPlace(sender, "program-pointer", "ISO §14.9.39 Format 9, §14.9.39.3 SR21", out var senderRefusal);
            if (senderRefusal is not null) return senderRefusal;
            if (sp?.Item.Pic?.Category is not PicCategory.ProgramPointer)
            {
                return BoundRejected.Report(ctx.Edition, DiagnosticCatalog.PointerOperandShape,
                    // ⛔ NAME THE RECEIVERS (kb/Work PB388's sweep, finished here): the renderer transliterates
                    // U+2026 to ASCII, so `SET … TO 'x'` reached the user as `SET . TO 'x'` — a statement
                    // nobody wrote. The Format-7 twin was fixed; these two were the arms it missed.
                    $"SET {SetFormatSelection.Written(targetRefs)} TO "
                    + $"'{sender.Text}': a program-pointer sender shall be NULL, another "
                    + "USAGE PROGRAM-POINTER item, a function-identifier or inline method invocation of category "
                    + "program-pointer, or an ENTRY program-address-identifier "
                    + "(ISO §14.9.39.3 Format 9 SR21 / §8.4.3.13)");
            }
            source = sp;
            // §14.9.39.3 SR22 — "If identifier-7 references a RESTRICTED program-pointer, identifier-8 shall be
            // the predefined address NULL or shall reference a program-pointer and the program-prototypes
            // associated with identifier-7 and identifier-8 shall have the same signature." The rule is
            // conditioned on the RECEIVER being restricted (unlike SR20, where every function-pointer is), which
            // is the only asymmetry between the two; the compare itself is the same one, so it is the same
            // helper. kb/Work PB817: "SR20 and SR22 are ONE rule over TWO carriers; write it once."
            foreach (var t in targets)
                if (PointerAssignmentReason(t.Item, sp.Item) is { } why)
                    return BoundRejected.Report(ctx.Edition, DiagnosticCatalog.PrototypePointerSignature,
                        $"SET '{t.Item.CobolName}' TO '{sp.Item.CobolName}': {why}");
        }
        return new BoundSetProgramPointer(targets, source, toNull);
    }

    /// <summary>⛔ THE ONE §14.9.39.3 VERDICT FOR A POINTER IDENTIFIER STORED INTO A POINTER RECEIVER — the SET of one
    /// (receiver, sender) pair whose two items are already of one pointer category. Asked by SET Format 9 itself and by
    /// every BY CONTENT / BY VALUE argument into a pointer formal, because §14.8.2.3.3 makes that crossing "the same as if
    /// a SET statement were performed in the activating runtime element with the argument as the sending operand and the
    /// corresponding formal parameter as the receiving operand" (kb/Work PB1063: the argument lane compared the
    /// CATEGORY alone, so a restricted data-pointer reached an unrestricted pointer formal, which SR19 refuses). Null
    /// when the SET is valid.
    /// <list type="bullet">
    /// <item>data-pointer — SR19: "If identifier-5 references a restricted data-pointer, identifier-6 shall be the
    /// predefined address NULL or shall reference a data-pointer restricted to the same type", and its third sentence
    /// the converse (a restricted sender needs a receiver restricted to the same type) — both directions over the ONE
    /// restriction model (<see cref="StrongTypeModel.PointerRestriction"/>), the pair
    /// <c>PtrBinder.ScreenPointerReceiverRestriction</c> asks of a Format-7 sender of either spelling;</item>
    /// <item>program-pointer — SR22: a RESTRICTED receiver needs a sender "restricted" to a program-prototype of the
    /// same signature (an unrestricted sender is associated with none); an unrestricted receiver takes any
    /// program-pointer (SR21);</item>
    /// <item>function-pointer — SR20: "The function-prototypes associated with identifier-12 and identifier-13 shall
    /// have the same signature".</item>
    /// </list></summary>
    internal string? PointerAssignmentReason(DataItem receiver, DataItem sender)
    {
        switch (receiver.Pic?.Category)
        {
            case PicCategory.Pointer:
                var rr = StrongTypeModel.PointerRestriction(receiver);
                var sr = StrongTypeModel.PointerRestriction(sender);
                return (rr.IsRestricted || sr.IsRestricted) && !StrongTypeModel.SameRestriction(rr, sr)
                    ? (rr.IsRestricted
                        ? $"the receiving data-pointer is restricted to type '{rr}', so the sender shall be NULL or a "
                          + $"data-pointer restricted to the same type, and it is {(sr.IsRestricted ? $"restricted to '{sr}'" : "unrestricted")} "
                          + "(ISO §14.9.39.3 SR19)"
                        : $"the sender is a RESTRICTED data-pointer of type '{sr}' (ISO §13.18.60.4 GR23), so the receiver "
                          + "shall be a data-pointer restricted to the same type, and it is unrestricted (ISO §14.9.39.3 SR19, "
                          + "third sentence)")
                    : null;
            case PicCategory.ProgramPointer:
                string? targetProto = receiver.Pic.RestrictedPrototypeName;
                if (targetProto is null) return null;   // an UNRESTRICTED receiver — SR22's condition is not met
                // An UNRESTRICTED sender is a violation in its own right, not a vacuous pass: SR22 requires the
                // prototypes "associated with identifier-7 and identifier-8" to have the same signature, and an
                // unrestricted program-pointer is associated with none. (§14.8.2.3.2 says the same thing in the
                // argument-passing direction and says it explicitly — "if either is a restricted pointer, both
                // shall be restricted and of the same type".) This is DISTINCT from a prototype that names no
                // compile-time signature, which PrototypeSignatures.Same deliberately lets through.
                string? senderProto = sender.Pic?.RestrictedPrototypeName;
                return senderProto is null
                       || !PrototypeSignatures.Same(ProgramSignatureOf(targetProto), ProgramSignatureOf(senderProto))
                    ? $"the receiving program-pointer is restricted to program-prototype '{targetProto}' and the sender "
                      + $"{(senderProto is null ? "is unrestricted" : $"to '{senderProto}'")}, so the associated "
                      + "program-prototypes do not have the same signature (ISO §14.9.39.3 SR22; §13.18.60.4 GR25)"
                    : null;
            case PicCategory.FunctionPointer:
                string? rp = receiver.Pic.RestrictedPrototypeName, sp = sender.Pic?.RestrictedPrototypeName;
                return SameFunctionPrototypeSignature(rp, sp) ? null
                    : $"the receiving function-pointer is restricted to function-prototype '{rp}' and the sender to "
                      + $"'{sp}', which do not have the same signature — the function-prototypes associated with "
                      + "identifier-12 and identifier-13 shall have the same signature (ISO §14.9.39.3 SR20; §13.18.60.4 GR26)";
            default:
                return null;
        }
    }

    /// <summary>A program-prototype-name's bound signature, through the §8.4.6.8 scope table the declaration was
    /// already screened against (<c>StatementBinder.ProgramPrototypes</c>, kb/Work PB237 — its hit's Signature is
    /// null for a §12.3.8.4 GR10 c) external-repository prototype, which <see cref="PrototypeSignatures.Same"/>
    /// treats as conforming). The function twin is <see cref="FunctionSignatureOf"/>.</summary>
    private CalleeSignature? ProgramSignatureOf(string? prototypeName) =>
        prototypeName is not null && host.ProgramPrototypes?.TryGetValue(prototypeName, out var p) == true
            ? p.Signature
            : null;

    /// <summary>SET function-pointer assignment (ISO §14.9.39.2 Format 8; §14.9.39.3 SR20 — every target AND the
    /// sender shall be category function-pointer, the sender may be the predefined address NULL, and "the
    /// function-prototypes associated with identifier-12 and identifier-13 shall have the same signature"): the
    /// Format-9 program-pointer twin over the FunctionPointer carrier. kb/Work PB452 + PB817.</summary>
    private BoundStatement BindSetFunctionPointer(
        IReadOnlyList<Core.DataReferenceContext> targetRefs, SetSender? sender,
        bool toNull, bool senderIsSelfSuper, string? senderText = null)
    {
        if (senderIsSelfSuper)
        {
            return BoundRejected.Report(ctx.Edition, DiagnosticCatalog.PointerOperandShape,
                $"SET {SetFormatSelection.Written(targetRefs)} TO {senderText ?? "SELF/SUPER"}: SELF and SUPER "
                + "are object references, not function pointers "
                + "(ISO §14.9.39.2 Format 8 — the sender of a function-pointer SET is NULL, another "
                + "function-pointer, or an ADDRESS OF FUNCTION function-address-identifier; SELF and SUPER "
                + "belong to Format 5, the object-reference assignment)");
        }
        var targets = new List<Place>(targetRefs.Count);
        foreach (var t in targetRefs)
        {
            if (SetIndexNameOperand(t, "receiving operand", "function-pointer", "ISO §14.9.39 Format 8, §14.9.39.3 SR20")) return BoundRejected.Reported(ctx.Edition);
            if (host.Expr.ResolveReceiving(t) is not { } tp || tp.Item.Pic?.Category is not PicCategory.FunctionPointer)
            {
                return BoundRejected.Report(ctx.Edition, DiagnosticCatalog.PointerOperandShape,
                    $"SET '{t.GetText()}': the receiving operand of a function-pointer SET shall be USAGE "
                    + "FUNCTION-POINTER (ISO §14.9.39.3 SR20)");
            }
            targets.Add(tp);
        }
        Place? source = null;
        if (!toNull)
        {
            if (sender is not { IsIdentifierOperand: true })   // SR20 over a literal / expression sender (kb/Work PB456)
            {
                return BoundRejected.Report(ctx.Edition, DiagnosticCatalog.PointerOperandShape,
                    $"SET {SetFormatSelection.Written(targetRefs)} TO {senderText}: "
                    + "identifier-13 shall be of category function-pointer — a function-pointer sender is NULL, "
                    + "another USAGE FUNCTION-POINTER item, a function-identifier or inline method invocation of "
                    + "category function-pointer, or an ADDRESS OF FUNCTION function-address-identifier, "
                    + "never a literal or an arithmetic expression (ISO §14.9.39.2 Format 8, §14.9.39.3 SR20)");
            }
            var sp = CarrierSenderPlace(sender, "function-pointer", "ISO §14.9.39 Format 8, §14.9.39.3 SR20", out var senderRefusal);
            if (senderRefusal is not null) return senderRefusal;
            if (sp?.Item.Pic?.Category is not PicCategory.FunctionPointer)
            {
                return BoundRejected.Report(ctx.Edition, DiagnosticCatalog.PointerOperandShape,
                    $"SET {SetFormatSelection.Written(targetRefs)} TO "   // kb/Work PB388
                    + $"'{sender.Text}': a function-pointer sender shall be NULL, another "
                    + "USAGE FUNCTION-POINTER item, a function-identifier or inline method invocation of category "
                    + "function-pointer, or an ADDRESS OF FUNCTION function-address-identifier "
                    + "(ISO §14.9.39.3 SR20 / §8.4.3.12)");
            }
            source = sp;
            // SR20's LAST sentence, over every receiver: the associated function-prototypes shall have the same
            // signature. §13.18.60.4 GR26 is what makes "associated" a compile-time fact — every function-pointer
            // carries the prototype its unbracketed TO phrase names.
            foreach (var t in targets)
                if (PointerAssignmentReason(t.Item, sp.Item) is { } why)
                    return BoundRejected.Report(ctx.Edition, DiagnosticCatalog.PrototypePointerSignature,
                        $"SET '{t.Item.CobolName}' TO '{sp.Item.CobolName}': {why}");
        }
        return new BoundSetFunctionPointer(targets, source, toNull);
    }

    /// <summary><c>SET function-pointer… TO ADDRESS OF FUNCTION {function-prototype-name-1 | identifier-1}</c> —
    /// ISO §14.9.39.2 Format 8 with the §8.4.3.12 function-address-identifier as its sender. Without this the
    /// category has no non-NULL value it can ever hold (kb/Work PB452).
    /// <para>Both braced arms arrive as ONE <c>dataReference</c> and are told apart HERE, where the facts exist:
    /// §8.4.3.12.3 SR2 makes the prototype arm a REPOSITORY function-specifier (plus §8.4.6.6's containing-
    /// function spelling), and SR1 makes the identifier arm "of category alphanumeric or national". The
    /// prototype arm resolves at COMPILE time, so §8.4.3.12.4 GR3 ("the function-address-identifier has the
    /// characteristics of a function-pointer restricted to function-prototype-name-1") puts it straight into
    /// SR20's same-signature compare; the identifier arm names the function at RUN time (GR1 a), which is
    /// exactly the case §14.9.39.4 GR14's EC-FUNCTION-PTR-INVALID screen exists for.</para></summary>
    private BoundStatement BindSetFunctionAddress(Core.SetFunctionAddressStatementContext sfa)
    {
        // ⚠ `drefs` IS the receiving-operand list and nothing else: the function-address-identifier is its own
        // grammar RULE (§8.4.3.12.2 is an identifier format), so its operand lives under `fai`, exactly as the
        // program-address-identifier's does under BindSetProgramAddress.
        var drefs = sfa.dataReference();
        var fai = sfa.functionAddressIdentifier();
        if (drefs.Length < 1)
            return BoundRejected.Report(ctx.Edition, DiagnosticCatalog.StatementFormatShape, "SET … TO ADDRESS OF FUNCTION with no receiving operand (ISO §14.9.39.2)");
        var targets = new List<Place>(drefs.Length);
        string? receiverProto = null;
        for (int i = 0; i < drefs.Length; i++)
        {
            if (host.Expr.ResolveReceiving(drefs[i]) is not { } tp || tp.Item.Pic?.Category is not PicCategory.FunctionPointer)
            {
                return BoundRejected.Report(ctx.Edition, DiagnosticCatalog.PointerOperandShape,
                    $"SET '{drefs[i].GetText()}': the receiving operand of SET … TO ADDRESS OF FUNCTION shall be "
                    + "USAGE FUNCTION-POINTER (ISO §14.9.39.3 SR20 / §8.4.3.12.4 GR1)");
            }
            targets.Add(tp);
            // SR20 applies to the receivers among themselves too — identifier-13 is ONE sender, so two receivers
            // restricted to differently-signed prototypes cannot both conform to it.
            if (i == 0) receiverProto = tp.Item.Pic?.RestrictedPrototypeName;
            else if (!SameFunctionPrototypeSignature(receiverProto, tp.Item.Pic?.RestrictedPrototypeName))
            {
                return BoundRejected.Report(ctx.Edition, DiagnosticCatalog.PrototypePointerSignature,
                    $"SET '{targets[0].Item.CobolName}' '{tp.Item.CobolName}' TO ADDRESS OF FUNCTION: the receiving "
                    + $"function-pointers are restricted to function-prototypes '{receiverProto}' and "
                    + $"'{tp.Item.Pic?.RestrictedPrototypeName}', which do not have the same signature, so one sender "
                    + "cannot satisfy both (ISO §14.9.39.3 SR20)");
            }
        }

        // The receiving operands AS WRITTEN — every echo of the statement below names them (kb/Work PB388).
        string written = SetFormatSelection.Written(drefs);
        // ⛔ identifier-1 AS A FUNCTION-IDENTIFIER OR AN INLINE METHOD INVOCATION (§8.4.3.1.3 SR1 — "that other
        // identifier may be any of the formats for an identifier"; kb/Work PB1452's sibling sweep). Neither can be a
        // function-prototype-name, so the prototype arm below is not asked; SR1's category screen is, through the ONE
        // reader the data-reference arm uses, over the data item the identifier references (§8.4.3.2.1, §8.4.3.4.4 GR1).
        if (((ParserRuleContext?)fai.functionCall() ?? fai.inlineMethodInvocation()) is { } idSyntax)
        {
            if (AddressNameOfIdentifier(idSyntax, $"SET {written} TO ADDRESS OF FUNCTION {DataBinder.WrittenText(idSyntax)}",
                    DiagnosticCatalog.FunctionAddressOperand, "8.4.3.12.3 SR1") is not { } idName)
                return BoundRejected.Reported(ctx.Edition);
            return new BoundSetFunctionAddress(targets, idName.Literal, idName.Place, ExpectedFormalsOf(receiverProto));
        }
        var operand = fai.dataReference();
        string word = operand.GetText();
        // The PROTOTYPE arm (§8.4.3.12.3 SR2 + §8.4.6.6): a REPOSITORY function-specifier, or the containing
        // function definition's own user-function-name. The SAME two legs the USAGE clause's TO phrase resolves.
        bool isPrototypeName = ctx.Data.UserFunctionNames.Contains(word)
            || CobolNames.Same(host.UdfSelfName, word);
        if (isPrototypeName)
        {
            // GR3 makes the sender a function-pointer restricted to this prototype, so SR20's compare is the
            // same one the pointer-to-pointer arm runs — one rule, one helper.
            if (!SameFunctionPrototypeSignature(receiverProto, word))
            {
                return BoundRejected.Report(ctx.Edition, DiagnosticCatalog.PrototypePointerSignature,
                    $"SET '{targets[0].Item.CobolName}' TO ADDRESS OF FUNCTION {word}: the receiving function-pointer "
                    + $"is restricted to function-prototype '{receiverProto}' and the function-address-identifier has "
                    + $"the characteristics of a function-pointer restricted to '{word}' (ISO §8.4.3.12.4 GR3), and "
                    + "the two do not have the same signature (ISO §14.9.39.3 SR20)");
            }
            // §8.4.3.12.4 GR2 names the EXTERNALIZED function-name as the address's identity, and that is what
            // the run-unit registry holds a unit under (§11.5.4 GR1's AS literal-1 when one is written; the
            // word otherwise) — so the bound node carries the externalized spelling, never the source word
            // (kb/Work PB303, the same drift the CALL/ENTRY paths were fixed for).
            string externalized = host.UserFunctions?.TryGetValue(word, out var proto) == true ? proto.Externalized : word;
            return new BoundSetFunctionAddress(targets, externalized, null, ExpectedFormalsOf(receiverProto));
        }
        // The IDENTIFIER arm (§8.4.3.12.3 SR1): "Identifier-1 shall be of category alphanumeric or national."
        // A DEFERRED shape is the resolver's to announce; only a word that identifies nothing earns SR2's reading.
        if (host.Expr.ResolveSending(operand) is var nameAnswer && nameAnswer.Place is not { } namePlace)
        {
            if (nameAnswer.Outcome == RefOutcome.Deferred) return nameAnswer.Refusal(ctx.Edition);   // kb/Work PB1030
            return BoundRejected.Report(ctx.Edition, DiagnosticCatalog.FunctionAddressOperand,
                $"SET {written} TO ADDRESS OF FUNCTION {word}: '{word}' is neither a function-prototype-name declared in "
                + "the REPOSITORY paragraph (ISO §8.4.3.12.3 SR2 / §8.4.6.6) nor a resolvable identifier "
                + "(§8.4.3.12.3 SR1)");
        }
        if (!ScreenAddressName(namePlace, $"SET {written} TO ADDRESS OF FUNCTION {word}", word,
                DiagnosticCatalog.FunctionAddressOperand, "8.4.3.12.3 SR1"))
            return BoundRejected.Reported(ctx.Edition);
        return new BoundSetFunctionAddress(targets, null, namePlace, ExpectedFormalsOf(receiverProto));
    }

    /// <summary>⛔ THE ONE §8.4.3.12.3 SR1 / §8.4.3.13.3 SR1 SCREEN — "Identifier-1 shall be of category alphanumeric
    /// or national" — over the data item the name operand of an address-identifier references (kb/Work PB1417 +
    /// PB1452). The function-address-identifier and the program-address-identifier print the same sentence, so one
    /// reader answers both: <see cref="ItemCategory.IsAlphanumericOrNational(Place)"/>, the compiler's ONE category
    /// predicate for this rule. Both sites used to test the raw <c>Item.Pic?.Category</c>, which is null for a GROUP
    /// (an alphanumeric or national group item has class and category alphanumeric / national, §8.5.2.1, and was
    /// refused as "category (none)") and which reads PIC A — category alphabetic, outside SR1's list — as
    /// alphanumeric, accepting it. A reference-modified operand takes §8.4.3.3.4 GR6's category through the same
    /// predicate. Returns false having reported.</summary>
    /// <param name="prefix">The statement as the message opens it, ending before the colon.</param>
    private bool ScreenAddressName(Place name, string prefix, string word, DiagnosticDescriptor code, string rule)
    {
        if (ItemCategory.IsAlphanumericOrNational(name)) return true;
        ctx.Edition.Error(code,
            $"{prefix}: identifier-1 shall be of category alphanumeric or national (ISO §{rule}) — '{word}' is {AddressNameFace(name)}");
        return false;
    }

    /// <summary>What the operand IS, for the SR1 refusal: its category when it has one (a reference-modified view's
    /// is §8.4.3.3.4 GR6's), else the group description <see cref="ItemCategory.Face"/> gives.</summary>
    private static string AddressNameFace(Place name) =>
        name is RefModPlace rm ? $"of category {rm.Category.ToString().ToLowerInvariant()}"
        : name.Item.Pic is { } pic ? $"of category {(pic.IsAlphabetic ? "alphabetic" : pic.Category.ToString().ToLowerInvariant())}"
        : ItemCategory.Face(name.Item);

    /// <summary>⛔ identifier-1 OF AN ADDRESS-IDENTIFIER WHEN IT IS A FUNCTION-IDENTIFIER OR AN INLINE METHOD
    /// INVOCATION (§8.4.3.1.3 SR1; kb/Work PB1452) — bound ONCE (its activation registers a statement pre-op), then
    /// screened by <see cref="ScreenAddressName"/>'s rule over the data item it references: a user function's or an
    /// invocation's returned temporary is that item already (§8.4.3.2.1, §8.4.3.4.4 GR1); an intrinsic's character
    /// result is materialized into the §15.4 returned-value temporary (<see cref="SendingValueTemp"/>); an intrinsic
    /// folded to its literal value is the compile-time name itself. A numeric (or other non-character) result is SR1's
    /// refusal. Null having reported; <c>Literal</c> or <c>Place</c> is set, never both.</summary>
    private (string? Literal, Place? Place)? AddressNameOfIdentifier(
        ParserRuleContext syntax, string prefix, DiagnosticDescriptor code, string rule)
    {
        var bound = syntax switch
        {
            Core.FunctionCallContext fc => host.Intrinsic.BindIntrinsic(fc),
            Core.InlineMethodInvocationContext imi => host.Oo.OoBindInlineInvocation(imi),
            _ => throw new InvalidOperationException(
                $"AddressNameOfIdentifier serves ISO §8.4.3.1.2 Formats 1 and 4 only, not {syntax.GetType().Name}"),
        };
        if (bound is BoundExprError) return null;   // the identifier's own binder reported
        string text = DataBinder.WrittenText(syntax);
        var operand = IntrinsicBinder.OperandOf(bound);
        if (operand is BoundStringLiteral { Category: PicCategory.Alphanumeric or PicCategory.National } folded)
            return (folded.Value, null);
        Place? place = operand is BoundFieldOperand f ? f.Place
            : IntrinsicResultType.OperandCategory(operand) is PicCategory.Alphanumeric or PicCategory.National
                ? host.SendingValue.Materialize(operand, "addrname")
                : null;
        if (place is not null && ScreenAddressName(place, prefix, text, code, rule)) return (null, place);
        if (place is null)
            ctx.Edition.Error(code,
                $"{prefix}: identifier-1 shall be of category alphanumeric or national (ISO §{rule}) — '{text}' is of "
                + $"category {IntrinsicResultType.OperandCategory(operand)?.ToString().ToLowerInvariant() ?? "(none)"}");
        return null;
    }

    /// <summary>ISO §14.9.39.3 SR20's last sentence over two function-prototype NAMES: resolve each through the
    /// compilation group's user-function table (the §8.4.6.6 scope the declarations were already screened
    /// against) and hand the pair to the ONE same-signature test <see cref="PrototypeSignatures.Same"/>, which
    /// SR22 reads as well. A name with no table entry is a §12.3.8.4 GR11 c) external-repository prototype: no
    /// compile-time signature exists and the compare conforms (the run-time screen covers it).</summary>
    private bool SameFunctionPrototypeSignature(string? a, string? b) =>
        PrototypeSignatures.Same(FunctionSignatureOf(a), FunctionSignatureOf(b));

    private CalleeSignature? FunctionSignatureOf(string? prototypeName) =>
        prototypeName is not null && host.UserFunctions?.TryGetValue(prototypeName, out var s) == true
            ? new CalleeSignature(s.Formals, s.Returning)
            : null;

    /// <summary>The run-time signature granularity of §14.9.39.4 GR14 for a receiver restricted to
    /// <paramref name="prototypeName"/> — the prototype's formal count, or −1 when no compile-time signature
    /// exists (a §12.3.8.4 GR11 c) external-repository prototype). ⛔ −1 SUPPRESSES the emitted GR14 screen
    /// rather than defaulting it to zero: with no declared signature there is nothing for "the same signature as
    /// the function referenced in the definition of identifier-13" to name, and screening against a guessed
    /// arity would REJECT a conforming separately-compiled target at run time. GR4's not-found screen still
    /// runs, because locating the function needs no signature.</summary>
    private int ExpectedFormalsOf(string? prototypeName) =>
        FunctionSignatureOf(prototypeName) is { } s ? s.Formals.Count : -1;

    /// <summary><c>SET program-pointer… TO ENTRY {literal | identifier}</c> — ⛔ THE MICRO FOCUS / IBM VENDOR
    /// SPELLING, NOT AN ISO FORMAT. This doc comment used to call it "ISO §14.9.39 Format 9 with the §8.4.3.13
    /// program-address-identifier sender" and it is neither (kb/Work PB549): the words <c>TO ENTRY</c> occur
    /// nowhere in ISO/IEC 1989:2023. The ISO surface is <see cref="BindSetProgramAddress"/>
    /// (<c>SET pp … TO ADDRESS OF PROGRAM …</c>). What IS shared is the SEMANTICS, and they are the
    /// standard's: every target shall be category program-pointer (§14.9.39.3 SR21), and the operand names the
    /// program (§8.4.3.13.4 GR1 — a literal, or an identifier whose VALUE names it per §8.3.2.2). So the body
    /// is factored into <see cref="BindProgramAddressTargets"/> + <see cref="BoundSetEntry"/> and both
    /// surfaces reach it, rather than each carrying its own copy of SR21.</summary>
    private BoundStatement BindSetEntry(Core.SetEntryStatementContext se)
    {
        // The LAST dataReference is the ENTRY identifier operand when no literal is present — the grammar
        // shape is `SET dataReference+ TO ENTRY (nonNumericLiteral | dataReference)`.
        var drefs = se.dataReference();
        bool identForm = se.nonNumericLiteral() is null;
        int targetCount = identForm ? drefs.Length - 1 : drefs.Length;
        if (targetCount < 1)
            return BoundRejected.Report(ctx.Edition, DiagnosticCatalog.StatementFormatShape, "SET … TO ENTRY with no receiving operand (ISO §14.9.39.2)");
        if (BindProgramAddressTargets(drefs, targetCount, "SET … TO ENTRY") is not { } targets)
            return BoundRejected.Reported(ctx.Edition);
        // The receiving operands AS WRITTEN — a statement ECHO names them (kb/Work PB388); only the FORM
        // reference passed to BindProgramAddressTargets above keeps the general format's own `…`.
        string written = SetFormatSelection.Written(drefs[..targetCount]);
        if (!identForm)
        {
            var nn = se.nonNumericLiteral();
            if (nn?.STRINGLIT() is not { } lit)
            {
                return BoundRejected.Report(ctx.Edition, DiagnosticCatalog.PointerOperandShape,
                    $"SET {written} TO ENTRY {nn?.GetText()}: the ENTRY literal shall be an alphanumeric literal "
                    + "naming a program (ISO §8.4.3.13 / §8.3.2.2)");
            }
            return new BoundSetEntry(targets, CobolLiteral.Decode(lit.GetText()), null);
        }
        // The resolver reported (or defers) the reference; "the ENTRY identifier is unresolvable" on top named
        // no rule the program broke (kb/Work PB1030).
        if (host.Expr.ResolveSending(drefs[^1]) is var entryAnswer && entryAnswer.Place is not { } namePlace)
            return entryAnswer.Refusal(ctx.Edition);
        return new BoundSetEntry(targets, null, namePlace);
    }

    /// <summary>§14.9.39.3 SR21's receiving half — "Identifier-7 shall reference a data item of category
    /// program-pointer" — over the first <paramref name="count"/> of <paramref name="drefs"/>. ONE screen for
    /// the two surfaces that assign a program address (the ISO <c>ADDRESS OF PROGRAM</c> form and the vendor
    /// <c>TO ENTRY</c> form), so the rule cannot be enforced in one and forgotten in the other. Returns null
    /// having reported.</summary>
    private List<Place>? BindProgramAddressTargets(
        Core.DataReferenceContext[] drefs, int count, string where)
    {
        var targets = new List<Place>(count);
        for (int i = 0; i < count; i++)
        {
            if (host.Expr.ResolveReceiving(drefs[i]) is not { } tp || tp.Item.Pic?.Category is not PicCategory.ProgramPointer)
            {
                ctx.Edition.Error(DiagnosticCatalog.PointerOperandShape,
                    $"SET '{drefs[i].GetText()}': the receiving operand of {where} shall be USAGE "
                    + "PROGRAM-POINTER (ISO §14.9.39.2 Format 9 / §14.9.39.3 SR21)");
                return null;
            }
            targets.Add(tp);
        }
        return targets;
    }

    /// <summary><c>SET { identifier-7 } … TO ADDRESS OF PROGRAM { identifier-1 | literal-1 |
    /// program-prototype-name-1 }</c> — ISO §14.9.39.2 Format 9 with the §8.4.3.13 PROGRAM-ADDRESS-IDENTIFIER
    /// as its sender, the ONLY standard syntax that puts a program's address into a program-pointer. It had no
    /// grammar at any edition, so the conforming spelling was a bare parse error and the vendor
    /// <c>TO ENTRY</c> extension was the only route in (kb/Work PB549).
    /// <para>Three braced operand arms, three syntax rules, all on
    /// <see cref="DiagnosticCatalog.ProgramAddressOperand"/>: §8.4.3.13.3 SR1 "Identifier-1 shall be of
    /// category alphanumeric or national", SR2 "Literal-1 shall be an alphanumeric or national literal whose
    /// length is not zero", SR3 "Program-prototype-name-1 shall be a program prototype specified in the
    /// REPOSITORY paragraph". The receiving operand's own category is §14.9.39.3 SR21's and reports through
    /// <see cref="DiagnosticCatalog.PointerOperandShape"/>, as the vendor spelling's does.</para>
    /// <para>§8.4.3.13.4 GR2 fixes what the address IS — "For a COBOL program, the address is that of the
    /// outermost program identified by the EXTERNALIZED program-name in its PROGRAM-ID paragraph" — so the
    /// prototype arm resolves through the §12.3.8.2 program-specifier's externalized name (its
    /// <c>AS literal-3</c> when one is written), never the source word; that is the same drift kb/Work PB303
    /// fixed on the CALL and ENTRY paths. GR3 makes the prototype arm a program-pointer RESTRICTED to
    /// program-prototype-name-1, which is what §14.9.39.3 SR22 then compares against a restricted
    /// receiver — the same compare the pointer-to-pointer arm runs, so it is the same helper.</para></summary>
    private BoundStatement BindSetProgramAddress(Core.SetProgramAddressStatementContext spa)
    {
        var drefs = spa.dataReference();
        var pai = spa.programAddressIdentifier();
        var operandRef = pai.dataReference();
        // ⚠ `drefs` IS the receiving-operand list and nothing else. The program-address-identifier is its own
        // grammar RULE (§8.4.3.13.2 is an identifier format, not a statement phrase), so its operand lives
        // under `pai`, not flattened into this context's dataReference list — unlike the FUNCTION twin, whose
        // sender phrase is written inline in setFunctionAddressStatement and therefore has to drop the last
        // element. Getting that wrong is silent: the last receiver is simply never screened or stored.
        if (drefs.Length < 1)
            return BoundRejected.Report(ctx.Edition, DiagnosticCatalog.StatementFormatShape, "SET … TO ADDRESS OF PROGRAM with no receiving operand (ISO §14.9.39.2)");
        if (BindProgramAddressTargets(drefs, drefs.Length, "SET … TO ADDRESS OF PROGRAM") is not { } targets)
            return BoundRejected.Reported(ctx.Edition);
        // The receiving operands AS WRITTEN — every echo of the statement below names them, and the two
        // helpers this arm delegates to take it as a parameter rather than re-eliding it (kb/Work PB388).
        string written = SetFormatSelection.Written(drefs);

        // §14.9.39.3 SR22's condition is the RECEIVER being restricted; the restriction the SENDER carries is
        // §8.4.3.13.4 GR3's — the prototype arm only. Captured before the arms so both can answer it.
        string? receiverProto = targets[0].Item.Pic?.RestrictedPrototypeName;
        foreach (var t in targets)
            if (!PrototypeSignatures.Same(ProgramSignatureOf(receiverProto),
                                          ProgramSignatureOf(t.Item.Pic?.RestrictedPrototypeName)))
            {
                // SR22 binds the receivers to ONE sender, so two receivers restricted to differently-signed
                // prototypes cannot both conform to it — the SR20 argument on the function twin, verbatim.
                return BoundRejected.Report(ctx.Edition, DiagnosticCatalog.PrototypePointerSignature,
                    $"SET '{targets[0].Item.CobolName}' '{t.Item.CobolName}' TO ADDRESS OF PROGRAM: the "
                    + $"receiving program-pointers are restricted to program-prototypes '{receiverProto}' and "
                    + $"'{t.Item.Pic?.RestrictedPrototypeName}', which do not have the same signature, so one "
                    + "sender cannot satisfy both (ISO §14.9.39.3 SR22)");
            }

        if (BindProgramAddressOperand(pai, $"SET {written} TO") is not { } operand) return BoundRejected.Reported(ctx.Edition);
        if (operand.Prototype is { } word)
        {
            // GR3: this identifier is a program-pointer RESTRICTED to `word`. SR22 then requires the
            // receiver's prototype to have the same signature — an unrestricted receiver meets no condition.
            if (receiverProto is not null
                && !PrototypeSignatures.Same(ProgramSignatureOf(receiverProto), ProgramSignatureOf(word)))
            {
                return BoundRejected.Report(ctx.Edition, DiagnosticCatalog.PrototypePointerSignature,
                    $"SET '{targets[0].Item.CobolName}' TO ADDRESS OF PROGRAM {word}: the receiving "
                    + $"program-pointer is restricted to program-prototype '{receiverProto}' and the "
                    + $"program-address-identifier has the characteristics of a program-pointer restricted to "
                    + $"'{word}' (ISO §8.4.3.13.4 GR3), and the two do not have the same signature "
                    + "(ISO §14.9.39.3 SR22)");
            }
            return new BoundSetEntry(targets, operand.NameLiteral, null);
        }
        string senderWhat = operand.NamePlace is null
            ? $"literal \"{operand.NameLiteral}\""
            : $"identifier '{operandRef?.GetText() ?? DataBinder.WrittenText((ParserRuleContext?)pai.functionCall() ?? pai.inlineMethodInvocation()!)}'";
        return RestrictedReceiverNeedsPrototype(receiverProto, senderWhat, written)
            ? BoundRejected.Reported(ctx.Edition)
            : new BoundSetEntry(targets, operand.NameLiteral, operand.NamePlace);
    }

    /// <summary>⛔ THE ONE BINDER FOR A §8.4.3.13 PROGRAM-ADDRESS-IDENTIFIER OPERAND (kb/Work PB239) — the SET
    /// Format-9 sender and the CALL argument (§14.9.4.3 SR3/SR4, an address-identifier being §8.4.3.1.2
    /// identifier Format 9) both bind through it, so the three braced arms and their three syntax rules are
    /// stated once. <paramref name="site"/> is the statement text that precedes the identifier in a message
    /// (<c>SET P TO</c>, <c>CALL … USING</c>). Null having reported.
    /// <para>Three braced operand arms, three syntax rules, all on
    /// <see cref="DiagnosticCatalog.ProgramAddressOperand"/>: SR1 identifier-1, SR2 literal-1, SR3
    /// program-prototype-name-1 — asked FIRST, like the function twin: the name is a user-defined word in a slot
    /// where the standard lists the prototype meaning, and §8.3.2.2 keeps the two name types apart by
    /// context.</para></summary>
    internal BoundProgramAddress? BindProgramAddressOperand(Core.ProgramAddressIdentifierContext pai, string site)
    {
        // ── ARM 2: literal-1 (§8.4.3.13.3 SR2 / §8.4.3.13.4 GR1b) ──────────────────────────────────────────
        if (pai.nonNumericLiteral() is { } nn)
            return ProgramAddressLiteral(nn, site) is { } value ? new BoundProgramAddress(value, null, null) : null;

        // ── ARM 1b: identifier-1 written as a function-identifier or an inline method invocation (§8.4.3.1.3 SR1;
        //    kb/Work PB1452) — a reference to a data item like any other, so SR1's category screen is the same one. ──
        if (((ParserRuleContext?)pai.functionCall() ?? pai.inlineMethodInvocation()) is { } idSyntax)
            return AddressNameOfIdentifier(idSyntax, $"{site} ADDRESS OF PROGRAM {DataBinder.WrittenText(idSyntax)}",
                    DiagnosticCatalog.ProgramAddressOperand, "8.4.3.13.3 SR1") is { } idName
                ? new BoundProgramAddress(idName.Literal, idName.Place, null)
                : null;

        var operandRef = pai.dataReference();
        string word = operandRef.GetText();
        // ── ARM 3: program-prototype-name-1 (§8.4.3.13.3 SR3) — GR2's EXTERNALIZED program-name, never the
        //    source word (kb/Work PB303). ──
        if (host.ProgramPrototypes?.GetValueOrDefault(word) is { } proto)
            return new BoundProgramAddress(proto.ExternalizedName, null, word);

        // ── ARM 1: identifier-1 (§8.4.3.13.3 SR1 / §8.4.3.13.4 GR1a) ───────────────────────────────────────
        // A DEFERRED shape is the resolver's to announce; only a word that identifies nothing earns SR3's reading.
        if (host.Expr.ResolveSending(operandRef) is var nameAnswer && nameAnswer.Place is not { } namePlace)
        {
            if (nameAnswer.Outcome == RefOutcome.Deferred) { nameAnswer.PlaceOrReported(ctx.Edition); return null; }   // 0899, reported (kb/Work PB1030)
            ctx.Edition.Error(DiagnosticCatalog.ProgramAddressOperand,
                $"{site} ADDRESS OF PROGRAM {word}: '{word}' is neither a program-prototype-name declared in "
                + "the REPOSITORY paragraph (ISO §8.4.3.13.3 SR3) nor a resolvable identifier (SR1)");
            return null;
        }
        if (!ScreenAddressName(namePlace, $"{site} ADDRESS OF PROGRAM {word}", word,
                DiagnosticCatalog.ProgramAddressOperand, "8.4.3.13.3 SR1"))
            return null;
        return new BoundProgramAddress(null, namePlace, null);
    }

    /// <summary>§8.4.3.13.3 SR2 over the literal arm of a program-address-identifier: "Literal-1 shall be an
    /// alphanumeric or national literal whose length is not zero." The figurative constants and the boolean
    /// literal are excluded by the same sentence — none of them is an alphanumeric or national literal — and a
    /// §8.8.3 concatenation expression folds first, because §8.8.3.3 GR3 makes it "equivalent to a literal of
    /// the same class and value". Returns null having reported.</summary>
    private string? ProgramAddressLiteral(Core.NonNumericLiteralContext nn, string site)
    {
        string? value =
            nn.figurativeConstant() is not null ? null
            : nn.STRINGLIT() is { } s ? CobolLiteral.Decode(s.GetText())
            : nn.HEXLIT() is { } x ? CobolLiteral.DecodeHex(x.GetText())
            : nn.NATLIT() is { } nat ? CobolLiteral.Decode(nat.GetText())
            : nn.concatenationExpression() is { } ce
                && ConcatFolder.Fold(ce, ctx.Edition, ctx.Data.LiteralEnv) is { Category: PicCategory.Alphanumeric or PicCategory.National } f
                    ? f.Value
            : null;
        if (value is null)
        {
            ctx.Edition.Error(DiagnosticCatalog.ProgramAddressOperand,
                $"{site} ADDRESS OF PROGRAM {nn.GetText()}: literal-1 shall be an alphanumeric or national "
                + "literal whose length is not zero (ISO §8.4.3.13.3 SR2)");
            return null;
        }
        if (value.Length == 0)
        {
            ctx.Edition.Error(DiagnosticCatalog.ProgramAddressOperand,
                $"{site} ADDRESS OF PROGRAM \"\": literal-1 shall be an alphanumeric or national literal "
                + "whose length is not zero (ISO §8.4.3.13.3 SR2)");
            return null;
        }
        return value;
    }

    /// <summary>§14.9.39.3 SR22 against a program-address-identifier that is NOT the prototype arm. Only
    /// §8.4.3.13.4 GR3's prototype arm gives the identifier a restriction, so an identifier-1 or literal-1
    /// sender is an UNRESTRICTED program-pointer: SR22 requires "the program-prototypes associated with
    /// identifier-7 and identifier-8" to have the same signature, and an unrestricted pointer is associated
    /// with none. Distinct from a prototype with no compile-time signature, which
    /// <c>PrototypeSignatures.Same</c> deliberately lets through — the same reading
    /// <see cref="BindSetProgramPointer"/> enforces. Returns true having reported.
    /// <para>⚠ WHICH ARM IS THIS, AND WHY IS ITS TWIN NOT SCREENED? The vendor <c>SET pp TO ENTRY</c> form
    /// (<see cref="BindSetEntry"/>) is deliberately NOT subject to this, and the asymmetry is the standard's
    /// rather than an oversight: SR22 constrains <i>identifier-8</i>, the sender of §14.9.39.2 <b>Format 9</b>,
    /// and the ENTRY spelling is not that format — its words are in no ISO general format at all, so the rule
    /// has no operand there to name. <c>tests/conformance/2002/pb817_restricted_program_pointer</c> is the
    /// witness that loads a restricted program-pointer through it. §13.18.60.4 GR25's content obligation on a
    /// restricted program-pointer is a separate question about the EXTENSION's own discipline, registered as
    /// its own mechanism.</para></summary>
    private bool RestrictedReceiverNeedsPrototype(string? receiverProto, string senderWhat, string written)
    {
        if (receiverProto is null) return false;
        ctx.Edition.Error(DiagnosticCatalog.PrototypePointerSignature,
            $"SET {written} TO ADDRESS OF PROGRAM {senderWhat}: the receiving program-pointer is restricted to "
            + $"program-prototype '{receiverProto}', and only the program-prototype-name-1 arm of a "
            + "program-address-identifier is itself restricted (ISO §8.4.3.13.4 GR3); an unrestricted sender "
            + "is associated with no program-prototype, so the two cannot have the same signature "
            + "(ISO §14.9.39.3 SR22)");
        return true;
    }

    /// <summary><c>SET receivers… TO value</c>. ⛔ THE FORMAT IS SELECTED ONCE, FROM EVERY RECEIVING OPERAND
    /// (§14.9.39.2; <see cref="SetFormatSelection"/>, kb/Work PB449 + PB456). Formats 1, 5, 7, 8, 9, 14 and 16
    /// all reach this grammar rule, and each of their receiving braces is written <c>{ … } …</c>, so the whole
    /// list decides — never <c>receivers[0]</c>, and never the SENDER.
    /// <para>The sender used to be half the sniff, and that is what let <c>SET U TO 5</c> escape every rule:
    /// the Format-5 re-route's precondition was "the sender is exactly one bare data reference", a literal is
    /// not one, so the re-route declined and an object reference landed in the Format-1 arithmetic store. Now
    /// the receivers select Format 5 whatever the sender looks like, and SR9 refuses the literal by name.</para></summary>
    public BoundStatement BindSetTo(Core.SetToValueStatementContext tv)
    {
        var recvs = tv.dataReference();
        var kinds = _fmt.KindsOf(recvs);
        var amount = tv.arithmeticExpression();
        string senderText = amount.GetText();
        // ⛔ THE FORMATS THAT TAKE THE AMOUNT AS AN EXPRESSION OF THEIR OWN are decided from the receiving list alone,
        // BEFORE the sender is classified: classifying binds a sole function-identifier (and registers its
        // activation), and F14/F16 bind the same expression themselves — binding it here as well would run a user
        // function twice. No sender can change which of these formats the statement is (their rows name no sending
        // kind), so nothing is lost by asking them first.
        switch (SetFormatSelection.Select(kinds, SetDirections.To, out _))
        {
            case SetFormat.F14:   // OCCURS DYNAMIC capacity change (data-model D9)
                return BindSetCapacity(recvs, amount, SetCapacityKind.To);
            case SetFormat.F16:   // the SIZE-OF-absent bare form; the explicit form enters at BindSetSize
                return BindSetSize(recvs, amount);
            case SetFormat.F17:
                return BindMessageTagSet(recvs, senderText);
            case null:
                _fmt.ReportNoFormat(recvs, kinds, SetDirections.To, senderText);
                return BoundRejected.Reported(ctx.Edition);
        }
        // The sender AS AN IDENTIFIER of any of §8.4.3.1.2's formats (§14.9.39.2's identifier-2/-4/-6/-8/-13
        // positions) — a data reference, a function-identifier or an inline method invocation; null of all three
        // for a literal or an expression, which each format's own syntax rule then refuses (kb/Work PB1399/PB1929).
        var sender = _senders.Classify(amount);
        if (SetSenders.IsRefused(sender)) return BoundRejected.Reported(ctx.Edition);
        // ⛔ SelectForTo, not Select: the receiving list decides, EXCEPT against Format 1's `identifier-1`
        // catch-all brace, where a sender of class object or of category data-/program-/function-pointer is
        // admissible in no Format-1 sending position (§8.8.1.1 / §14.9.39.3 SR2) and names exactly one other
        // format. `SET N4 TO U` is that statement: Format 1's brace admits N4, so the sender used to reach the
        // ARITHMETIC screen (COBOLNET0844, "'U' is not a numeric operand") while the rule the program broke was
        // §14.9.39.3 SR8. It now selects Format 5 and SR8 refuses N4 by name.
        switch (_fmt.SelectForTo(kinds, sender, out _))
        {
            case SetFormat.F7:
                return BindSetPointer(recvs, sender, toNull: false, senderIsSelfSuper: false, senderText);
            case SetFormat.F9:
                return BindSetProgramPointer(recvs, sender, toNull: false, senderIsSelfSuper: false, senderText);
            case SetFormat.F8:
                return BindSetFunctionPointer(recvs, sender, toNull: false, senderIsSelfSuper: false, senderText);
            case SetFormat.F5:
                return host.Oo.OoBindSetObjectRef(recvs, sender, senderNull: false, senderSelf: false,
                    senderSuper: false, senderText);
            case SetFormat.F17:   // reached from a message-tag SENDER over Format 1's catch-all receiving brace
                return BindMessageTagSet(recvs, senderText);
            case SetFormat.F1:
                break;
            default:
                _fmt.ReportNoFormat(recvs, kinds, SetDirections.To, senderText);
                return BoundRejected.Reported(ctx.Edition);
        }
        // §14.9.39.3 SR2 — the sending alternative, classified ONCE for every receiver (kb/Work PB212).
        if (IndexAssignmentSenderOf(recvs, sender) is not { } alternative) return BoundRejected.Reported(ctx.Edition);
        var targets = new List<BoundSetTarget>();
        bool admitted = true;
        foreach (var dref in recvs)
        {
            if (SetTargetOf(dref) is not { } t) return new BoundUnsupported($"SET receiver '{DataBinder.WrittenText(dref)}'");
            admitted &= ScreenIndexAssignmentReceiver(t, DataBinder.WrittenText(dref), alternative, sender.Text);
            targets.Add(t);
        }
        if (!admitted) return BoundRejected.Reported(ctx.Edition);
        return new BoundSetTo(targets, FormatOneValueOf(sender, amount));
    }

    /// <summary>The value Format 1 stores, as an expression. A function-identifier or inline invocation sender is
    /// ALREADY bound (classifying it bound it, once), so it must not be bound again: an index function is
    /// identifier-2's value as it stands, and any other identifier is arithmetic-expression-1 and takes the SAME
    /// §8.8.1.1 numeric-class screen the expression spine applies to it — <see cref="ExpressionBinder.ScreenIdentifierOperand"/>,
    /// the half <c>BindIdentifierOperand</c> shares. Everything else (a data reference, a literal, an expression) binds
    /// as an r7 window, as before (kb/Work R29).</summary>
    private BoundExpr FormatOneValueOf(SetSender sender, Core.ArithmeticExpressionContext amount) =>
        sender is { Bound: { } bound, Syntax: { } syntax }
            ? sender.IdentifierClass is CobolClass.Index
                ? bound
                : host.Expr.ScreenIdentifierOperand(bound, syntax, OperandContext.ArithmeticIndexWindow)
            : host.Expr.BindIndexWindowExpr(amount);   // SET is an r7 window (kb/Work R29)

    /// <summary>Format 1's sending brace, <c>{ arithmetic-expression-1 | index-name-2 | identifier-2 }</c> (ISO
    /// §14.9.39.2, the rendered figure: three alternatives, one required).</summary>
    private enum IndexAssignmentSender { ArithmeticExpression, IndexName, IndexDataItem }

    /// <summary>Which of Format 1's three sending alternatives the sender IS — or null, after reporting, when it is
    /// none of them.
    /// <para>⚠ DETERMINATION (kb/Work PB212 / PB388's SR2 question, settled from the rendered §14.9.39.2 Format-1
    /// figure): SR2 — "Identifier-2 shall reference a data item of class index" — governs the identifier-2
    /// ALTERNATIVE, not every bare identifier. A single numeric identifier is an arithmetic expression (§8.8.1.1:
    /// "An arithmetic expression may be an identifier referencing a numeric data item"), so <c>SET IX TO N</c> over an
    /// integer <c>N</c> is arithmetic-expression-1 and legal against an index-name receiver. The strict reading —
    /// that an integer sender is refused because it is not of class index — is rejected: it would make
    /// arithmetic-expression-1 unable to be a single data item. What SR2 DOES refuse is a bare identifier that
    /// is neither of class index nor numeric: it is no alternative of the brace. That refusal is written HERE, in
    /// SR2's own words, rather than left to the §8.8.1.1 arithmetic screen to reach incidentally.</para>
    /// <para>⛔ A SENDER IS CLASSIFIED BY ITS CLASS, NOT BY HOW IT IS WRITTEN (kb/Work PB1399). identifier-2 is "a data
    /// item of class index" (§14.9.39.3 SR2) and §8.4.3.1.3 SR1 lets it be any identifier format, so an INDEX
    /// FUNCTION — §15.2 item 6: "These are of the class and category index" — is identifier-2 exactly as an index data
    /// item is. The classifier used to return <see cref="IndexAssignmentSender.ArithmeticExpression"/> for every
    /// sender that was not a bare data reference, which put an index function through §14.9.39.3 SR3 / the §8.8.1.1
    /// numeric screen. A function-identifier or invocation of any OTHER class stays arithmetic-expression-1, whose
    /// §8.8.1.1 class screen then answers for it (<see cref="FormatOneValueOf"/>).</para></summary>
    private IndexAssignmentSender? IndexAssignmentSenderOf(IReadOnlyList<Core.DataReferenceContext> recvs, SetSender sender)
    {
        if (sender.Ref is not { } senderDref)   // a literal, an expression, or a function-identifier / invocation
            return sender.IdentifierClass is CobolClass.Index
                ? IndexAssignmentSender.IndexDataItem : IndexAssignmentSender.ArithmeticExpression;
        string senderText = sender.Text;
        switch (_fmt.KindOf(senderDref))
        {
            case SetOperandKind.IndexName: return IndexAssignmentSender.IndexName;
            case SetOperandKind.IndexDataItem: return IndexAssignmentSender.IndexDataItem;
            case SetOperandKind.OtherDataItem
                when ctx.Refs.Probe(senderDref) is { } sp && !IntrinsicArgumentRules.IsArithmeticOperandClass(sp.Item):
                ctx.Edition.Error(DiagnosticCatalog.SetIndexAssignmentOperand,
                    $"SET {SetFormatSelection.Written(recvs)} TO '{senderText}': the sender is neither identifier-2 — ISO §14.9.39.3 SR2: \"Identifier-2 "
                    + "shall reference a data item of class index\" — nor a numeric operand of "
                    + "arithmetic-expression-1 (ISO §8.8.1.1)");
                return null;
            default: return IndexAssignmentSender.ArithmeticExpression;
        }
    }

    /// <summary>§14.9.39.3 SR1, SR3 and SR4 over ONE receiving operand of Format 1, as the receiver-class ×
    /// sender-alternative table §14.9.39.4 GR2 implies: it defines the statement for an index-name receiver from
    /// any of the three alternatives (GR2 a)), and for a data-item receiver only from the alternatives its class
    /// admits — so every other pairing is a syntax-rule violation, never a statement the emitter should store.
    /// <list type="bullet">
    ///   <item>SR1 is the RECEIVER's class — the <see cref="OperandPositions.SetIndexAssignmentReceiver"/> row of
    ///         the ONE operand-class screen. Refused in both lanes: every refused class either threw at run time
    ///         or stored a value (PIC 9(4)V99 received 5 as 000500) GR2 defines for no such receiver.</item>
    ///   <item>SR3 — a class-index receiver: arithmetic-expression-1 "shall not be specified".</item>
    ///   <item>SR4 — a numeric receiver: "index-name-2 shall be specified" — so neither arithmetic-expression-1
    ///         nor an index-data-item identifier-2.</item>
    /// </list>
    /// SR3 and SR4 go through the <see cref="EditionContext.Removed"/> seam: an error under strict, and under
    /// <c>--permissive</c> a warning with the value stored as before — the documented leniency for source that
    /// assigns a number to an integer or index data item with SET.</summary>
    private bool ScreenIndexAssignmentReceiver(BoundSetTarget t, string text, IndexAssignmentSender sender, string senderText)
    {
        if (t is not SetPlaceTarget { Place: var p }) return true;   // index-name-1 — GR2 a) admits every sender
        if (!OperandClassScreen.Screen(ctx.Edition, OperandPositions.SetIndexAssignmentReceiver, p, text)) return false;
        bool indexReceiver = (OperandClassScreen.ClassesOf(p) & OperandClasses.IndexDataItem) != 0;
        string? rule = (indexReceiver, sender) switch
        {
            (true, IndexAssignmentSender.ArithmeticExpression) =>
                "SR3: \"If identifier-1 references a data item of class index, arithmetic-expression-1 shall not be "
                + "specified\"",
            (false, IndexAssignmentSender.ArithmeticExpression or IndexAssignmentSender.IndexDataItem) =>
                "SR4: \"If identifier-1 references a numeric data item, index-name-2 shall be specified\"",
            _ => null,
        };
        if (rule is null) return true;
        ctx.Edition.Removed(DiagnosticCatalog.SetIndexAssignmentOperand.Code,
            $"SET '{text}' TO '{senderText}' violates ISO §14.9.39.3 {rule}");
        return ctx.Edition.Permissive;
    }

    /// <summary><c>SET index-name… {UP|DOWN} BY amount</c> (ISO §14.9.39 Format 2), with Formats 10 and 14
    /// sharing the same grammar shape — selected from the WHOLE receiving list (kb/Work PB449).
    /// <para>⛔ FORMAT 2's RECEIVING BRACE IS <c>{ index-name-3 } …</c>, AND THAT IS THE WHOLE LIST: §14.9.39.2
    /// prints no <c>identifier</c> alternative for it and §14.9.39.4 GR4 is written "For each occurrence of
    /// index-name-3". So the three UP/DOWN formats between them admit an index-name, a data-pointer (SR23) and a
    /// dynamic-capacity register (SR29) — and NOTHING else. <c>SET WS-N UP BY 4</c> over a <c>PIC 9(4)</c> used
    /// to compile and answer 5; it now draws COBOLNET2112, because no printed format admits it.</para></summary>
    public BoundStatement BindSetUpDown(Core.SetIndexStatementContext ud)
    {
        var recvs = ud.dataReference();
        var kinds = _fmt.KindsOf(recvs);
        var amount = ud.arithmeticExpression();
        bool down = ud.DOWN() is not null;
        switch (SetFormatSelection.Select(kinds, SetDirections.UpDown, out bool exact))
        {
            case SetFormat.F10:   // data-pointer arithmetic — PtrBinder screens EVERY operand against SR23
                return host.Ptr.BindSetUpDown(ud);
            case SetFormat.F14:
                return BindSetCapacity(recvs, amount, down ? SetCapacityKind.DownBy : SetCapacityKind.UpBy);
            case SetFormat.F2 when !exact:   // an index-name mixed with something Format 2 does not admit
                _fmt.ReportNotAdmitted(recvs, kinds, SetFormat.F2);
                return BoundRejected.Reported(ctx.Edition);
            case SetFormat.F2:
                break;
            default:
                _fmt.ReportNoFormat(recvs, kinds, SetDirections.UpDown, amount.GetText());
                return BoundRejected.Reported(ctx.Edition);
        }
        var targets = new List<BoundSetTarget>();
        foreach (var dref in recvs)
        {
            if (SetTargetOf(dref) is not { } t) return new BoundUnsupported($"SET receiver '{DataBinder.WrittenText(dref)}'");
            targets.Add(t);
        }
        return new BoundSetUpDown(targets, host.Expr.BindIndexWindowExpr(amount), down);
    }

    /// <summary>SET Format 14 (ISO §14.9.39.2; OCCURS DYNAMIC, data-model D9) — <c>SET data-name-2
    /// {TO | UP BY | DOWN BY} {integer-1 | arithmetic-expression-4}</c> changes the table's current capacity.
    /// Reached only when <see cref="SetFormatSelection"/> has already selected Format 14 from the whole receiving
    /// list, so the only receiver rule left is the format's CARDINALITY: <c>SET data-name-2</c> is printed
    /// WITHOUT an ellipsis, so the register is the sole receiver (COBOLNET1524).
    /// <para>⛔ THE LITERAL ALTERNATIVE IS SCREENED HERE (§14.9.39.3 SR30; kb/Work PB458). Only the
    /// arithmetic-expression alternative is left to §14.9.39.4 GR29/GR30's run-time condition and clamp; integer-1
    /// is a SYNTAX rule and its violation is a refusal. SR30's capacity bounds are conditioned on the SET's own
    /// TO alternative — UP BY / DOWN BY write a DELTA, which "not less than the minimum capacity" cannot be
    /// about — so those two take the nonnegative half alone.</para></summary>
    private BoundStatement BindSetCapacity(
        IReadOnlyList<Core.DataReferenceContext> targets, Core.ArithmeticExpressionContext amount, SetCapacityKind kind)
    {
        // A PURE capacity-register peek (NOT refs.Resolve, which would route an OO `prop OF obj` first target through
        // the property hook and enqueue a spurious pending op — OCCURS DYNAMIC review #7).
        if (targets.Count == 0 || ctx.Refs.CapacityRegisterFor(targets[0]) is not { } cap)
            return new BoundUnsupported("SET capacity-register — the register could not be addressed");
        // The target NAMES a register but breaks one of its reference rules (§13.18.38.3 SR30/SR31, §8.4.2.2.3 SR4,
        // §8.4.3.3.3 SR1; kb/Work PB457). This is still SET Format 14 — SetFormatSelection chose the format from the
        // receiving list, and a CAPACITY register is what this receiver IS — so the format is CONSUMED here and the
        // reference's own rule is stated by the ONE screen that owns it (Refs.Resolve → CapacityPlaceOf). Nothing is
        // stacked on top of that diagnostic: the receiver is implemented; the reference is illegal.
        // ⚠ ResolveSending, deliberately: the receiving chokepoint REFUSES a CAPACITY register (COBOLNET1523 —
        // "except in a SET statement Format 14"), and this IS Format 14; the call exists only for the resolver's
        // own reference diagnostic.
        if (cap.Place is not { } place) { host.Expr.ResolveSending(targets[0]); return new BoundUnsupported("SET receiving operand"); }
        if (targets.Count > 1)
        {
            return BoundRejected.Report(ctx.Edition, "COBOLNET1524",
                $"SET '{cap.Register.CobolName}' {SetCapacityKinds.Text(kind)}: a dynamic-table CAPACITY register "
                + "is the sole receiver of a SET Format 14 statement (ISO §14.9.39; §13.18.38 Format 4)");
        }

        if (SetLiteralAmount.Of(amount) is { } integer1)
        {
            // §13.18.38.4 GR16/GR17 name the two capacities SR30 compares against: "Integer-4 is the minimum
            // capacity of the table. If integer-4 is absent, a value of zero is assumed for it" and "Integer-5
            // is the expected capacity".
            var spec = cap.Table.OccursSpec;
            bool to = kind == SetCapacityKind.To;
            var bound = new SetAmountBound(
                Min: to ? spec?.InitialCap ?? 0 : 0,
                Max: to ? spec?.ExpectedMax : null,
                MinWhat: to && spec?.InitialCap is { } min and > 0
                    ? $"nonnegative and not less than the minimum capacity ({min}) defined in the corresponding OCCURS clause"
                    : "nonnegative",
                MaxWhat: $"not greater than the expected capacity ({spec?.ExpectedMax}) defined in the corresponding OCCURS clause",
                Operand: "integer-1", Rule: "ISO §14.9.39.3 SR30");
            if (SetLiteralAmount.Violation(integer1, bound) is { } why)
            {
                return BoundRejected.Report(ctx.Edition, DiagnosticCatalog.SetLiteralAmountOutOfRange,
                    $"SET '{cap.Register.CobolName}' {SetCapacityKinds.Text(kind)} {integer1}: {why} ({bound.Rule})");
            }
        }
        return new BoundSetCapacity(place.Table, host.Expr.BindIndexWindowExpr(amount), kind);
    }

    /// <summary>SET [SIZE OF] data-name-3 TO n (ISO §14.9.39 Format 16, COBOL-2023): set the current length of a
    /// dynamic-length elementary item. data-name-3 shall itself be dynamic-length (SR33 → COBOLNET1568). The 2023
    /// introduction gate is on the <see cref="BoundSetSize"/> node (VersionConformancePass semantic arm), covering
    /// both the explicit SIZE OF form and the bare re-routed form. Whether EC-STORAGE-NOT-AVAIL checking is enabled
    /// at this statement is NOT captured here: §14.9.39.4 GR37/GR38's nonfatal condition rides the ambient
    /// (EC-STORAGE-NOT-AVAIL → StorageNotAvailChecking) pair the EcBinder already adds to every statement in a
    /// checking-on region, which is also what carries it into the §14.6.13.1.4 #3 selection (kb/Work PB367b).</summary>
    private BoundStatement BindSetSize(Core.DataReferenceContext dref, Core.ArithmeticExpressionContext amount) =>
        BindSetSize([dref], amount, explicitSizeOf: true);

    /// <summary>⛔ BOTH ARMS OF FORMAT 16 ARE ONE BIND (kb/Work PB458). <c>[ SIZE OF ]</c> is a bracket, so
    /// <c>SET SIZE OF D TO n</c> and <c>SET D TO n</c> are the SAME format with the same rules — and the
    /// SIZE-OF-absent arm used to be a separate peek that refused to resolve anything carrying a
    /// <c>dataReferenceSuffix</c>. That test was aimed at subscripts, but a suffix ALSO carries QUALIFICATION,
    /// so <c>SET D OF G TO 2</c> — legal source (§8.4.2.2.3 rule 2: "a name may be qualified even though it does
    /// not need qualification"; §13.16.3 SR18 leaves an OCCURS clause impossible here, so the subscript half had
    /// nothing legal to exclude) — fell into the Format-1 numeric store and died at run time. The ONE receiver
    /// resolution is <see cref="ExpressionBinder.ResolveReceiving"/>, the same one the explicit arm always used;
    /// the format is selected before either arm is entered, so no peek is needed to keep a speculative resolve
    /// off the OO property hook.</summary>
    private BoundStatement BindSetSize(IReadOnlyList<Core.DataReferenceContext> targets,
                                       Core.ArithmeticExpressionContext amount, bool explicitSizeOf = false)
    {
        // §14.9.39.2 Format 16 prints ONE data-name-3, with no ellipsis: a second receiver is no Format 16.
        if (targets.Count != 1)
        {
            _fmt.ReportNotAdmittedCardinality(targets, SetFormat.F16);
            return BoundRejected.Reported(ctx.Edition);
        }
        var dref = targets[0];
        if (host.Expr.ResolveReceiving(dref) is not { } p)
            return BoundRejected.Reported(ctx.Edition);   // the receiving chokepoint reported it — not a deferral (kb/Work PB236, PB881)
        if (!p.Item.IsDynamicLength)
        {
            return BoundRejected.Report(ctx.Edition, "COBOLNET1568",
                $"SET SIZE OF '{p.Item.CobolName}': data-name-3 shall be a dynamic-length elementary item "
                + "(ISO §14.9.39.3 Format 16 SR33)");
        }
        // §14.9.39.3 SR34 over the LITERAL alternative — "Integer-2 shall be non-negative, and shall be equal to
        // or less than the maximum size of data-name-3, as specified in 8.5.1.10" — the maximum §8.5.1.10.1
        // defines and DataItem.DynMaxSize carries. §14.9.39.4 GR37/GR38's EC-STORAGE-NOT-AVAIL and clamp are the
        // rules for arithmetic-expression-5 and stay exactly where they are (kb/Work PB458).
        if (SetLiteralAmount.Of(amount) is { } integer2)
        {
            var bound = new SetAmountBound(0, p.Item.DynMaxSize, "non-negative",
                $"equal to or less than the maximum size of '{p.Item.CobolName}' ({p.Item.DynMaxSize} character "
                + "positions, ISO §8.5.1.10.1)", "integer-2", "ISO §14.9.39.3 SR34");
            if (SetLiteralAmount.Violation(integer2, bound) is { } why)
            {
                return BoundRejected.Report(ctx.Edition, DiagnosticCatalog.SetLiteralAmountOutOfRange,
                    $"SET {(explicitSizeOf ? "SIZE OF " : "")}'{p.Item.CobolName}' TO {integer2}: {why} ({bound.Rule})");
            }
        }
        return new BoundSetSize(p, host.Expr.BindIndexWindowExpr(amount), p.Item.DynMaxSize);
    }

    /// <summary>A Format-1 / Format-2 SET receiving operand: an INDEXED BY index-name (its <c>long</c> field) or a
    /// resolvable data item (an index data item or an integer item — the emitter dispatches on its usage).
    /// <para>It applies NO class screen, and that is deliberate: it is shared with PERFORM VARYING's induction
    /// variable, whose class rule is a different one (§14.9.28.3 SR2). Each caller asks the ONE operand-class
    /// screen for its OWN position — <see cref="BindSetTo"/> for §14.9.39.3 SR1 (kb/Work PB212), the varying
    /// phrase for §14.9.28.3 SR2 / SR5 a). The FORMAT question is not carried here either —
    /// <see cref="SetFormatSelection"/> has already established that every receiver belongs to Format 1's or
    /// Format 2's brace.</para></summary>
    public BoundSetTarget? SetTargetOf(Core.DataReferenceContext dref) =>
        host.Expr.IndexFieldOf(dref) is { } ix ? new SetIndexTarget(ix)
        : host.Expr.ResolveReceiving(dref) is { } p ? new SetPlaceTarget(p)   // a SET receiver IS a receiving operand
        : null;

    /// <summary><c>SET condition-name+ TO TRUE | FALSE</c> (ISO §14.9.39 Format 4).
    /// <para>⛔ ONE ARM, NOT TWO. §14.9.39.4 GR6 and GR7 are the SAME sentence with one word changed — "<i>the
    /// literal in the VALUE clause</i>" against "<i>the literal in the FALSE phrase of the VALUE clause</i>",
    /// both "<i>placed in the conditional variable according to the rules for the VALUE clause</i>", both with
    /// the same group-length and zero-length provisos — so the binder resolves the operands once and carries
    /// WHICH literal as a flag; the emitter has one store path. The FALSE arm used to return
    /// <c>BoundUnsupported</c> because <c>Condition88</c> had no literal-4 to store (kb/Work PB555); now that it
    /// does, the only thing left to check is §14.9.39.3 SR7 — the phrase has to be there.</para></summary>
    public BoundStatement BindSetCondition(Core.SetBooleanStatementContext b)
    {
        var sets = new List<(Place, Condition88, bool)>();
        // ⛔ ONE LOOP OVER THE PRINTED GROUPS (kb/Work PB450). §14.9.39.2 Format 4 wraps the whole
        // `{ condition-name-1 } … TO { TRUE | FALSE }` unit in an outer brace with a trailing `…`, so a
        // statement may write several groups and they need not agree on TRUE/FALSE; the grammar's
        // `setConditionPhrase` IS that unit, so the grouping is READ rather than re-derived from token
        // positions. §14.9.39.4 GR8 makes the flattening exact: "If multiple condition-names are specified,
        // the results are the same as if a separate SET statement had been written for each condition-name-1."
        foreach (var phrase in b.setConditionPhrase())
        {
            bool toTrue = phrase.TRUE_() is not null;
            foreach (var dref in phrase.dataReference())
            {
                // ⛔ SR6 IS DECIDED HERE, SO IT IS REPORTED HERE (kb/Work PB390). "Condition-name-1 shall be
                // associated with a conditional variable" (ISO §14.9.39.3 SR6) — and the operand that
                // DISCRIMINATES the rule is a SPECIAL-NAMES switch-status condition-name (§8.4.4.1's second
                // kind), which the old message denied was a condition-name at all while staging the verdict to
                // a run-time abort.
                if (host.Cond.ConditionOf(dref) is not { } cond)
                {
                    // A DECLARED level-88 the qualifiers do not reach is misqualified, not "not a condition-name"
                    // (kb/Work PB567 — §13.16.3 SR23's negative side, one wording for every site).
                    if (!host.Cond.ReportMisqualifiedCondition(dref))
                        ctx.Validation.RejectSetConditionName(DataBinder.WrittenText(dref), host.Alter.SwitchNameOf(dref));
                    return BoundRejected.Reported(ctx.Edition);
                }
                // The reference's subscripts identify the CONDITIONAL VARIABLE's occurrence (§8.4.2.3 Format 2).
                if (ctx.Refs.ResolveForItem(dref, cond.Parent) is var parentAnswer && parentAnswer.Place is not { } parent)
                    return parentAnswer.Refusal(ctx.Edition);   // the resolver's answer (kb/Work PB1030)
                // §14.9.39.3 SR7 — "If the FALSE phrase is specified, the FALSE phrase shall be specified in the
                // VALUE clause of the data description entry for condition-name-1." The TRUE arm needs no twin
                // screen: §13.18.63.3 SR24 already makes a VALUE clause mandatory on a level-88 entry, so
                // `cond.Values` is never empty where a condition-name exists.
                if (!toTrue && cond.FalseValue is null)
                {
                    return BoundRejected.Report(ctx.Edition, DiagnosticCatalog.SetFalseWithoutFalsePhrase, $"SET '{cond.Name}' TO FALSE: "
                        + $"the VALUE clause of condition-name '{cond.Name}' writes no WHEN SET TO FALSE phrase, "
                        + "so there is no literal-4 to place in the conditional variable (ISO §14.9.39.3 SR7)");
                }
                sets.Add((parent, cond, toTrue));
            }
        }
        return new BoundSetConditions(sets);
    }
}

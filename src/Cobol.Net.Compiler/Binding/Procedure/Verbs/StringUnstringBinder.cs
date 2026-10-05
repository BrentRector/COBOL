// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding.Bound;
using CobolNet.Binding.Model;
using CobolNet.Editions.Diagnostics;
using CobolNet.Frontend.Generated;

namespace CobolNet.Binding.Procedure;

using Core = CobolParserCore;

// The entire STRING/UNSTRING surface bound here — including NOT ON OVERFLOW and END-STRING/END-UNSTRING — is
// COBOL-85 (both verbs' phrases were complete by 1985); no edition gate applies. The post-85 deltas (class
// national operands, the zero-length-item rules, dynamic-length SIZE — §14.9.43.4 GR1) are bound here and emitted
// by StringEmitter (kb/Work PB1179, PB1184); the EC-OVERFLOW-STRING / EC-OVERFLOW-UNSTRING names (2002+, GR8b /
// GR16b) are raised through EcEmitter.EmitOverflow; the ON/NOT ON OVERFLOW control flow itself is
// edition-invariant.

/// <summary>The STRING/UNSTRING verb binder (P7 Step 10d — a real collaborator over
/// <see cref="BinderContext"/>, extracted from the <c>StatementBinder.StringUnstring</c> partial; the
/// operand spine is reached through transitional host edges until 10q). The SR rejections here are
/// CONTROL FLOW (each aborts the statement with a placeholder), so none lift to StatementValidation.
/// The five bound records stayed in <c>Binding/Bound/BoundStringUnstring.cs</c>.</summary>
internal sealed class StringUnstringBinder(BinderContext ctx, StatementBinder host)
{
    /// <summary>Bind STRING (ISO §14.9.43). Each <c>stringSendingPhrase</c> carries ONE sender and optionally the
    /// DELIMITED phrase that closes its run; the phrase governs every phraseless sender back to the previous
    /// phrase, so the binder back-propagates each phrase to its run, and a trailing phraseless run — legal only
    /// immediately preceding INTO — is DELIMITED BY SIZE (SR9). The receiver is screened per SR4 (not
    /// reference-modified) and SR5 (not edited, no JUSTIFIED — JUSTIFIED is not yet modeled, so only the edited
    /// arm is checkable); the pointer per SR7 (elementary integer, no P — a non-zero scale covers both V and P).</summary>
    public BoundStatement BindString(Core.StringStatementContext st)
    {
        var phrases = st.stringSendingPhrase();
        int n = phrases.Length;
        var values = new BoundOperand[n];
        var delims = new BoundOperand?[n];
        var bySize = new bool[n];
        var hasPhrase = new bool[n];
        for (int i = 0; i < n; i++)
        {
            values[i] = StrUnstrOperand(phrases[i].strUnstrOperand(), "STRING sending operand");
            if (BeginsWithAllWord(phrases[i].strUnstrOperand(), values[i]))   // SR2 — literal-1 shall not be a figurative beginning with the word ALL (a bare symbolic character is not — PB110)
                values[i] = BoundOperandError.Report(ctx.Edition, DiagnosticCatalog.StatementOperandRule,
                    $"STRING sending operand '{DataBinder.WrittenText(phrases[i].strUnstrOperand())}' is a figurative constant that "
                    + "begins with the word ALL, which literal-1 shall not be (ISO §14.9.43.3 SR2)",
                    "STRING sending ALL literal (ISO §14.9.43.3 SR2)");
            // ⛔ SR1 IS ONE SENTENCE ABOUT EVERY OPERAND OF THE STATEMENT, and it was enforced at ONE of its
            // three identifier positions (kb/Work PB664 — the sweep the MOVE screen's landing asked for).
            // `STRING P DELIMITED BY SIZE INTO A` over a USAGE POINTER sender compiled clean and stored
            // "CobolNet.Runtime.CellPointer" — OperandText's `_ => Read(p).ToString()` arm, reached because
            // nothing upstream refused the operand — and `STRING <COMP> …` / `STRING <INDEX> …` / a NUMERIC
            // literal-1 were accepted the same way. The receiver arm had the screen; the sending arm did not.
            if (Sr1Offence(values[i]) is { } sendOffence)
                return Sr1Reject($"STRING sending operand '{DataBinder.WrittenText(phrases[i].strUnstrOperand())}'", sendOffence);
            if (Sr8Offence(values[i]) is { } sendSr8)
                return Reject($"STRING sending operand '{DataBinder.WrittenText(phrases[i].strUnstrOperand())}' {sendSr8} (ISO §14.9.43.3 SR8)");
            if (phrases[i].delimitedByPhrase() is not { } dp) continue;
            hasPhrase[i] = true;
            if (dp.SIZE() is not null) { bySize[i] = true; continue; }
            var d = StrUnstrOperand(dp.strUnstrOperand(), "STRING delimiter");
            // SR2 — literal-2 shall not be an ALL figurative; the grammar's (ALL)? token is not in the ISO format.
            if (dp.ALL() is not null || BeginsWithAllWord(dp.strUnstrOperand(), d))
                d = BoundOperandError.Report(ctx.Edition, DiagnosticCatalog.StatementOperandRule,
                    $"STRING DELIMITED BY '{(dp.ALL() is not null ? "ALL " : "")}{DataBinder.WrittenText(dp.strUnstrOperand())}': the delimiter is a figurative constant that begins with "
                    + "the word ALL, which literal-2 shall not be (ISO §14.9.43.3 SR2)",
                    "STRING DELIMITED BY ALL literal (ISO §14.9.43.3 SR2)");
            if (Sr1Offence(d) is { } delimOffence)     // identifier-2 / literal-2 — SR1's second named position
                return Sr1Reject($"STRING DELIMITED BY '{DataBinder.WrittenText(dp.strUnstrOperand())}'", delimOffence);
            // SR3 — literal-2 shall not be a zero-length literal (§8.3.3.1: contiguous delimiters). The run-time kernel
            // reads a zero-length delimiter as DELIMITED BY SIZE, which is GR3 b)'s rule for a zero-length IDENTIFIER-2
            // and is no licence for a zero-length literal-2 (kb/Work PB1181).
            if (IsZeroLengthLiteral(d))
                return Reject($"STRING DELIMITED BY {DataBinder.WrittenText(dp.strUnstrOperand())}: literal-2 is a zero-length literal "
                    + "(ISO §14.9.43.3 SR3; §8.3.3.1)");
            if (Sr8Offence(d) is { } delimSr8)
                return Reject($"STRING DELIMITED BY '{DataBinder.WrittenText(dp.strUnstrOperand())}' {delimSr8} (ISO §14.9.43.3 SR8)");
            delims[i] = d;
        }
        // Back-propagate each DELIMITED phrase over its preceding phraseless run (the general format attaches the
        // phrase AFTER the run); senders left with no phrase form the trailing run ⇒ SIZE (SR9).
        for (int i = n - 2; i >= 0; i--)
            if (!hasPhrase[i]) { delims[i] = delims[i + 1]; bySize[i] = bySize[i + 1]; hasPhrase[i] = hasPhrase[i + 1]; }
        var sendings = new List<BoundStringSending>(n);
        for (int i = 0; i < n; i++)
            sendings.Add(new BoundStringSending(values[i], delims[i], bySize[i] || delims[i] is null));

        // ⛔ EVERY RECEIVING OPERAND OF THIS STATEMENT RESOLVES AT THE RECEIVING CHOKEPOINT (kb/Work PB429).
        // identifier-3 (INTO), identifier-4 (WITH POINTER), and UNSTRING's identifier-4/-5/-6/-7/-8 below are all
        // receivers — §14.9.43.4 GR6 moves the characters into identifier-3 and has identifier-4 "increased by
        // one prior to the move of the next character"; §14.9.48.4 GR13 increments identifier-7 "for each character
        // examined" and GR14 identifier-8 by "the number of identifier-4 receiving data items accessed" — so each
        // asks ExpressionBinder.ResolveReceiving, where the
        // receiver-side rules live ONCE: §8.4.3.15.3 SR1 admits PAGE-COUNTER (an integer-data-item context) and SR3
        // refuses LINE-COUNTER, §13.10.4 GR1 refuses a constant-name, §8.4.3.6.3 SR1 refuses EXCEPTION-OBJECT. The
        // sending resolver these sites used knew none of them.
        if (host.Expr.ResolveReceiving(st.stringIntoPhrase().dataReference()) is not { } into)
            return new BoundUnsupported("STRING INTO operand");   // the chokepoint reported it — not a deferral (kb/Work PB236)
        string intoText = DataBinder.WrittenText(st.stringIntoPhrase().dataReference());
        // §14.9.43.3 SR4–SR6, SR11 — bind-time rejections (kb/Work PB88: each was a run-time loud stage on ILLEGAL
        // source, the wrong-stage family; the statement compiled clean and died when control reached it).
        if (into is RefModPlace)
            return Reject($"STRING INTO '{intoText}': identifier-3 shall not be reference-modified (ISO §14.9.43.3 SR4)");
        if (into.Item.Pic is { Category: PicCategory.NumericEdited }
            or { Category: PicCategory.Alphanumeric or PicCategory.National, EditMask: not null })
            // The National arm too (kb/Work PB155's sweep): SR5 excludes ANY edited item, and a national-edited
            // picture is modeled as Category National WITH an EditMask — the two-arm screen had covered only
            // the numeric-edited and alphanumeric-edited forms. (Unreachable until Phase 4a residue #2 lands —
            // a national-edited ITEM is 0899 at declaration — so no fixture pins it; shaped for that landing.)
            return Reject($"STRING INTO '{intoText}': identifier-3 shall not reference an edited data item (ISO §14.9.43.3 SR5)");
        if (into.Item.Justified)
            return Reject($"STRING INTO '{intoText}': identifier-3 shall not be described with the JUSTIFIED clause (ISO §14.9.43.3 SR5)");
        if (StrongTypeModel.IsStrongGroup(into.Item))
            return Reject($"STRING INTO '{intoText}': identifier-3 shall not reference a strongly-typed group item (ISO §14.9.43.3 SR6)");
        if (into.Item.IsGroup && ReferenceResolver.HasVariableLengthSubordinate(into.Item))
            return Reject($"STRING INTO '{intoText}': identifier-3 shall not specify a variable-length group (ISO §14.9.43.3 SR11; §8.5.1.12)");
        // ⛔ DA7 — SR1 at BIND time. This check previously existed ONLY in StringEmitter as a run-time loud stage,
        // so `STRING … INTO <a COMP item>` compiled clean and crashed at the statement. It now asks the SAME
        // reader the two SENDING positions ask (kb/Work PB664): one sentence, one predicate, three positions.
        var intoOperand = new BoundFieldOperand(into);
        if (Sr1Offence(intoOperand) is { } intoOffence)
            return Sr1Reject($"STRING INTO '{intoText}'", intoOffence);
        // ⛔ SR1's SECOND SENTENCE, and it is the same rule (kb/Work PB664): "If any one of literal-1,
        // literal-2, identifier-1, identifier-2, or identifier-3 is of class national, then all shall be of
        // class national." It names FIVE operands and was enforced at none of them — `01 NN PIC N(2). 01 XX
        // PIC X(8). STRING NN DELIMITED BY SIZE INTO XX` compiled clean and transcoded silently. A rule
        // implemented at half of its positions is how this one came to be implemented at one of three.
        // The ONE all-or-nothing predicate (kb/Work PB980 — UNSTRING SR3 and INSPECT SR4 state the same rule).
        CobolClass?[] stringClasses =
            [IntrinsicArgumentRules.ClassOf(intoOperand), .. values.Select(IntrinsicArgumentRules.ClassOf),
             .. delims.Select(d => d is null ? null : IntrinsicArgumentRules.ClassOf(d))];
        if (AllOrNothingClass.Violated(CobolClass.National, stringClasses))
        {
            return BoundRejected.Report(ctx.Edition, DiagnosticCatalog.CharacterOperandClassMix, $"STRING INTO '{intoText}' "
                + AllOrNothingClass.Offence(CobolClass.National, "ISO §14.9.43.3 SR1") + " (ISO §14.9.43.3 SR1)");
        }

        Place? pointer = null;
        if (st.stringWithPointer()?.dataReference() is { } pd)
        {
            if (host.Expr.ResolveReceiving(pd) is not { } pp)
                return new BoundUnsupported("STRING POINTER operand");   // identifier-4 is a receiver — the chokepoint reported it (kb/Work PB429)
            if (!StrUnstrIsInteger(pp))
                return Reject($"STRING WITH POINTER '{DataBinder.WrittenText(pd)}': identifier-4 shall be an elementary numeric integer "
                    + "data item without the symbol P (ISO §14.9.43.3 SR7)");
            pointer = pp;
        }

        List<BoundStatement>? onOvf = null, notOvf = null;
        if (st.stringOnOverflow() is { } ov)
            (onOvf, notOvf) = PhraseBlocks.Split(ov.statementBlock(), PhraseBlocks.StartsWithNot(ov), b => host.BindBlocks([b]));
        return new BoundStringStmt(sendings, into, pointer, onOvf, notOvf)
        {
            // §14.9.43.4 GR2 — a figurative literal-1 / literal-2 takes identifier-3's usage (kb/Work PB1185).
            CharacterCategory = IntrinsicArgumentRules.ClassOf(intoOperand) is CobolClass.National
                ? PicCategory.National : PicCategory.Alphanumeric,
        };
    }

    /// <summary>Bind UNSTRING (ISO §14.9.48). The sender must be category alphanumeric or national (SR2 — a
    /// numeric [INCLUDING usage DISPLAY], numeric-edited, or boolean sender is rejected); each INTO target's data references arrive in
    /// order [receiver, DELIMITER IN?, COUNT IN?] keyed off the DELIMITER/COUNT tokens, both legal only under a
    /// DELIMITED phrase (SR7); COUNT IN / TALLYING are integer items (SR5) and the pointer per SR6. The grammar's
    /// ambiguous <c>DELIMITED ALL "0"</c> may parse as the figurative ALL-literal — SR1 forbids that figurative
    /// here, so the binder reads it as the UNSTRING ALL phrase over the plain literal.</summary>
    public BoundStatement BindUnstring(Core.UnstringStatementContext un)
    {
        // DA4: the sender is an identifier, so it may be a function-identifier (§14.9.48.2 + §8.4.3.1.2 Format 1).
        string senderText = DataBinder.WrittenText(un.strUnstrSender());
        var source = StrUnstrSender(un.strUnstrSender(), $"UNSTRING source '{senderText}'");
        if (source is BoundOperandError) return new BoundUnsupported($"UNSTRING source '{senderText}'");
        // ⛔ identifier-1 IS AN IDENTIFIER, NOT A LITERAL SLOT (kb/Work PB1182). §13.10.3 SR2 lets a constant-name stand
        // "anywhere that a format specifies a literal", and the UNSTRING format specifies identifier-1 only (SR8: "the
        // data item referenced by identifier-1 is the sending operand") — whereas STRING's identifier-1/literal-1 and
        // both verbs' delimiter operands DO specify a literal, so StrUnstrSender, which serves all four positions,
        // substitutes the constant's literal there and is right to. This is the one position it must refuse: the
        // receiving side already does (COBOLNET1548), and without this the two arms disagree.
        if (un.strUnstrSender().dataReference() is { } constRef && ctx.Data.ConstantOf(constRef) is not null)
            return Reject($"UNSTRING sender '{senderText}' is a constant-name, which stands only where a format specifies a "
                + "literal; identifier-1 is a data item (ISO §14.9.48.3 SR8; §13.10.3 SR2)");
        // SR2 — identifier-1 (the sender) shall be category alphanumeric or national (a fixed-length group and a
        // reference-modified slice are alphanumeric-image senders and remain permitted). A numeric item — INCLUDING
        // usage DISPLAY, whose zoned image would otherwise be examined as characters — a numeric-edited item, or a
        // boolean item is not a permitted sender. The screen reads the CATEGORY off whichever shape arrived: a
        // field's PICTURE, or an intrinsic's §15.2 result category.
        if (UnstringSenderCategory(source) is { } badCat)
            return Reject($"UNSTRING sender '{senderText}' is category {badCat} "
                + "(identifier-1 shall reference a data item of category alphanumeric or national, ISO §14.9.48.3 SR2)");
        if (source is BoundFieldOperand { Place.Item: { IsGroup: true } sg } && ReferenceResolver.HasVariableLengthSubordinate(sg))
            return Reject($"UNSTRING sender '{senderText}' shall not reference a variable-length group (ISO §14.9.48.3 SR10; §8.5.1.12)");

        var delims = new List<BoundUnstringDelimiter>();
        if (un.unstringDelimiterPhrase() is { } dp)
            foreach (var item in dp.unstringDelimiterItem())
            {
                bool all = item.ALL() is not null;
                var v = StrUnstrOperand(item.strUnstrOperand(), "UNSTRING delimiter");
                // SR1 — literal-1 and literal-2 shall be neither a figurative constant that begins with the word ALL
                // nor a zero-length literal (kb/Work PB1181). `ALL ALL ","` and `ALL ALL SPACES` are the ALL phrase
                // followed by exactly such a figurative; the fold below would otherwise read both ALLs as one.
                string delimText = DataBinder.WrittenText(item.strUnstrOperand());
                if (all && BeginsWithAllWord(item.strUnstrOperand(), v))
                    return Reject($"UNSTRING DELIMITED BY ALL: the delimiter '{delimText}' is a figurative constant that "
                        + "begins with the word ALL, which literal-1 and literal-2 shall not be (ISO §14.9.48.3 SR1)");
                if (IsZeroLengthLiteral(v))
                    return Reject($"UNSTRING DELIMITED BY {(all ? "ALL " : "")}{delimText}: the delimiter is "
                        + "a zero-length literal, which literal-1 and literal-2 shall not be (ISO §14.9.48.3 SR1; §8.3.3.1)");
                // SR1 names the LITERAL positions (category alphanumeric or national) and SR2 the identifier ones, in
                // the same clause. The sweep that screened only the sender left a numeric or edited DELIMITED BY
                // operand binding clean (kb/Work PB155), and the literal arms were never asked at all (kb/Work PB1181).
                if (UnstringSenderCategory(v) is { } badDelim)
                    return Reject(v is BoundFieldOperand or BoundComputedOperand
                        ? $"UNSTRING delimiter '{delimText}' is category {badDelim} (identifier-2 and identifier-3 shall "
                          + "reference data items of category alphanumeric or national, ISO §14.9.48.3 SR2)"
                        : $"UNSTRING delimiter '{delimText}' is a {badDelim} literal (literal-1 and literal-2 shall be "
                          + "literals of the category alphanumeric or national, ISO §14.9.48.3 SR1)");
                if (v is BoundAllLiteral allLit) { v = new BoundStringLiteral(allLit.Literal) { Category = allLit.Category }; all = true; }
                delims.Add(new BoundUnstringDelimiter(v, all));
            }

        var areas = new List<(Place Target, Place? DelimiterIn, Place? CountIn)>();
        foreach (var t in un.unstringIntoPhrase().unstringIntoTarget())   // ONE INTO, then the receivers (§14.9.48.2; kb/Work PB1183)
        {
            var drefs = t.dataReference();
            if (host.Expr.ResolveReceiving(drefs[0]) is not { } target)
                return new BoundUnsupported("UNSTRING INTO operand");   // identifier-4 is a receiver — the chokepoint reported it (kb/Work PB429)
            // SR4 — identifier-4 shall be (usage display + category alphabetic/alphanumeric/numeric) or (usage
            // national + category national/numeric). A fixed-length group (SR10) and a reference-modified slice
            // are alphanumeric-image receivers and are exempt; edited, COMP/packed/COMP-5, index, and float
            // receivers are not permitted.
            // ⛔ DA7: a COMPILE-TIME diagnostic. The verdict was already correct and already computed HERE,
            // but returning only BoundUnsupported let the illegal program compile clean and throw when control
            // reached the UNSTRING. An alphanumeric or national group stays exempt — §14.9.43.4 GR3a's
            // alphanumeric-MOVE semantics carry a group receiver, including one holding a BINARY/PACKED leaf
            // (V59) — but a BIT group is class and category boolean (§13.18.29.4 GR1 a) and a strongly-typed group's
            // category is its type-name (§8.5.2.1), neither of which SR4's list admits (kb/Work PB1182: the receiver
            // arm exempted every group, while Sr2OffendingCategory's sender arm already knew both).
            if (target.DenotedItem is not null
                && (target.Item.IsGroup
                    ? target.Item.GroupUsage is GroupUsage.Bit || StrongTypeModel.IsStrongGroup(target.Item)
                    : !UnstringReceiverAllowed(target.Item.Pic)))
            {
                return BoundRejected.Report(ctx.Edition, DiagnosticCatalog.CharacterOperandUsage,
                    $"UNSTRING INTO '{DataBinder.WrittenText(drefs[0])}' requires a usage-display alphabetic/alphanumeric/"
                    + "numeric or usage-national national/numeric receiver, a numeric one without the symbol P; "
                    + "edited, COMP, packed, index, float and boolean (bit-group) receivers have no character image, "
                    + "and a strongly-typed group's category is its type-name (§8.5.2.1) (ISO §14.9.48.3 SR4)");
            }
            bool hasDelim = t.DELIMITER() is not null, hasCount = t.COUNT() is not null;
            if ((hasDelim || hasCount) && un.unstringDelimiterPhrase() is null)
                return Reject("UNSTRING DELIMITER IN / COUNT IN: the DELIMITED BY phrase shall be specified when either "
                    + "is written (ISO §14.9.48.3 SR7)");
            if (target.Item.IsGroup && ReferenceResolver.HasVariableLengthSubordinate(target.Item))
                return Reject($"UNSTRING INTO '{DataBinder.WrittenText(drefs[0])}' shall not reference a variable-length group (ISO §14.9.48.3 SR10; §8.5.1.12)");
            int next = 1;
            Place? delimIn = null, countIn = null;
            if (hasDelim)
            {
                if (host.Expr.ResolveReceiving(drefs[next]) is not { } d5)
                    return new BoundUnsupported("UNSTRING DELIMITER IN operand");   // identifier-5 is a receiver — the chokepoint reported it (kb/Work PB429)
                // identifier-5 is SR2's fourth name (kb/Work PB155) — the delimiter RECEIVER shares the
                // category rule, not SR4's receiver list.
                if (Sr2OffendingCategory(d5.Item) is { } badD5)
                    return Reject($"UNSTRING DELIMITER IN '{DataBinder.WrittenText(drefs[next])}' is category {badD5} "
                        + "(identifier-5 shall reference a data item of category alphanumeric or national, "
                        + "ISO §14.9.48.3 SR2)");
                delimIn = d5;
                next++;
            }
            if (hasCount)
            {
                if (host.Expr.ResolveReceiving(drefs[next]) is not { } c6)
                    return new BoundUnsupported("UNSTRING COUNT IN operand");   // identifier-6 is a receiver — the chokepoint reported it (kb/Work PB429)
                if (!StrUnstrIsInteger(c6))
                    return Reject($"UNSTRING COUNT IN '{DataBinder.WrittenText(drefs[next])}': identifier-6 shall reference an integer "
                        + "data item without the symbol P (ISO §14.9.48.3 SR5)");
                countIn = c6;
            }
            areas.Add((target, delimIn, countIn));
        }

        // ⛔ SR3 (kb/Work PB980): "If any of identifier-1, identifier-2, identifier-3, identifier-4, identifier-5,
        // literal-1, or literal-2 are of category national, then all shall be of category national" — the rule shape
        // STRING SR1 and INSPECT SR4 share, asked of the ONE predicate. identifier-6 (COUNT IN) is not named.
        // ⚖ A NUMERIC identifier-4 answers with its USAGE. SR4 admits a numeric receiver of usage display OR usage
        // national — "usage display and category alphabetic, alphanumeric, or numeric; or … usage national and
        // category national or numeric" — and read by CATEGORY alone SR3 would make the second arm unusable (a
        // numeric receiver is never category national, so no national sender could ever reach it), which no reading
        // of the two rules together supports. SR4's own pairing is the answer: a usage-national numeric receiver
        // stands with the national operands, a usage-display one with the others (DETERMINATION D-UN3,
        // docs/CONFORMANCE.md §3).
        CobolClass?[] sr3Classes =
        [
            IntrinsicArgumentRules.ClassOf(source),
            .. delims.Select(d => IntrinsicArgumentRules.ClassOf(d.Value)),
            .. areas.Select(a => a.Target.DenotedItem is not null && a.Target.Item.OperandPic is { Category: PicCategory.Numeric } np
                ? (CobolClass?)(np.Usage is Usage.National ? CobolClass.National : CobolClass.Alphanumeric)
                : IntrinsicArgumentRules.ClassOf(new BoundFieldOperand(a.Target))),
            .. areas.Where(a => a.DelimiterIn is not null).Select(a => IntrinsicArgumentRules.ClassOf(new BoundFieldOperand(a.DelimiterIn!))),
        ];
        if (AllOrNothingClass.Violated(CobolClass.National, sr3Classes))
        {
            return BoundRejected.Report(ctx.Edition, DiagnosticCatalog.CharacterOperandClassMix, $"UNSTRING '{senderText}' "
                + AllOrNothingClass.Offence(CobolClass.National, "ISO §14.9.48.3 SR3") + " (ISO §14.9.48.3 SR3)");
        }

        Place? pointer = null;
        if (un.unstringWithPointer()?.dataReference() is { } pd)
        {
            if (host.Expr.ResolveReceiving(pd) is not { } pp)
                return new BoundUnsupported("UNSTRING POINTER operand");   // identifier-7 is a receiver — the chokepoint reported it (kb/Work PB429)
            if (!StrUnstrIsInteger(pp))
                return Reject($"UNSTRING WITH POINTER '{DataBinder.WrittenText(pd)}': identifier-7 shall be an elementary numeric integer "
                    + "data item without the symbol P (ISO §14.9.48.3 SR6)");
            pointer = pp;
        }
        Place? tallying = null;
        if (un.unstringTallying()?.dataReference() is { } td)
        {
            if (host.Expr.ResolveReceiving(td) is not { } tp)
                return new BoundUnsupported("UNSTRING TALLYING operand");   // identifier-8 is a receiver — the chokepoint reported it (kb/Work PB429)
            if (!StrUnstrIsInteger(tp))
                return Reject($"UNSTRING TALLYING IN '{DataBinder.WrittenText(td)}': identifier-8 shall reference an integer data item "
                    + "without the symbol P (ISO §14.9.48.3 SR5)");
            tallying = tp;
        }

        List<BoundStatement>? onOvf = null, notOvf = null;
        if (un.unstringOnOverflow() is { } ov)
            (onOvf, notOvf) = PhraseBlocks.Split(ov.statementBlock(), PhraseBlocks.StartsWithNot(ov), b => host.BindBlocks([b]));

        // ⛔ GR11 c) / d) ARE MOVES (kb/Work PB979). "The characters examined … shall be treated as an elementary
        // national data item if identifier-1 is of category national, and otherwise as an elementary alphanumeric
        // data item, and shall be moved into the current receiving area according to the rules for the MOVE
        // statement" — so the statement's conceptual item is minted once, and every receiver's store is a MOVE
        // from it bound through the ONE move binder. The emitter used to carry a private copy of the MOVE rules
        // per receiver category, and a copy is where ANY LENGTH, reference-modified and dynamic-length receivers
        // went missing. Bound AFTER every syntax screen above, so a refused statement mints nothing. The same
        // category is GR7's figurative-delimiter context (BoundUnstringStmt.CharacterCategory — kb/Work PB1185).
        var itemCategory =IntrinsicArgumentRules.ClassOf(source) is CobolClass.National
            ? PicCategory.National : PicCategory.Alphanumeric;
        var examined = ConceptualItem(itemCategory, "unstring");
        Place? delimiting = areas.Any(a => a.DelimiterIn is not null) ? ConceptualItem(itemCategory, "unsdelim") : null;
        var receivers = new List<BoundUnstringReceiver>(areas.Count);
        foreach (var (target, delimIn, countIn) in areas)
            receivers.Add(new BoundUnstringReceiver(
                target, delimIn, countIn,
                host.Move.BindMoveOf(new BoundFieldOperand(examined), [target], ImplicitMovePhrase.UnstringInto),
                delimIn is null ? null
                    : host.Move.BindMoveOf(new BoundFieldOperand(delimiting!), [delimIn], ImplicitMovePhrase.UnstringDelimiterIn),
                // GR8 — "zero-filled if it is described as numeric": a receiver DESCRIBED as numeric (a
                // reference-modified slice is the §8.4.3.3.4 GR6 unique item, not the numeric description).
                target.DenotedItem is not null && target.Item.OperandPic is { Category: PicCategory.Numeric }
                    ? host.Move.BindMoveOf(new BoundFigurative('Z'), [target], ImplicitMovePhrase.UnstringInto)
                    : null));
        return new BoundUnstringStmt(source, delims, receivers, pointer, tallying, onOvf, notOvf)
        {
            CharacterCategory = itemCategory, Examined = examined, Delimiting = delimiting,
        };

        Place ConceptualItem(PicCategory category, string tag) =>
            host.SendingValue.ConceptualCharacterItem(category, tag)
            ?? throw new InvalidOperationException($"UNSTRING: the {tag} conceptual item (ISO §14.9.48.4 GR11) did not resolve");
    }

    /// <summary>Bind a STRING/UNSTRING SENDING operand position (exactly one of a function-identifier, a data
    /// reference, a literal, or a figurative constant per the grammar). A figurative word is the implicit
    /// ONE-character item (STRING GR2 / UNSTRING GR7); the callers screen the ALL-literal figurative per their SRs.
    /// <para>
    /// ⛔ DA4 — the function-identifier arm. §14.9.43.2 and §14.9.48.2 write these operands as identifier-N, and
    /// §8.4.3.1.2 Format 1 makes <c>function-identifier-1</c> a FORMAT of an identifier, so a function is
    /// admissible in every one of them; §8.4.3.2.3 SR1 excludes it only from a RECEIVING operand, which is why the
    /// INTO phrases do not route through here. Before this, <c>STRING FUNCTION ORD(C) DELIMITED BY SIZE INTO A</c>
    /// did not even PARSE ("no viable alternative at input 'FUNCTION'"), so DA2's string-context renderer could
    /// never be reached — the rejection happened a whole stage earlier.
    /// </para>
    /// <para>ONE helper serves all four sending positions (STRING identifier-1 and identifier-2, UNSTRING
    /// identifier-1 and the DELIMITED BY … OR … items), so this arm is written once rather than four times. The
    /// grammar names the two shapes — <c>strUnstrOperand</c> (identifier or literal) and its strict subset
    /// <c>strUnstrSender</c> (identifier only) — so this takes ONE context parameter rather than a row of
    /// mutually-exclusive nullable ones that a caller could transpose.</para>
    /// </summary>
    private BoundOperand StrUnstrOperand(Core.StrUnstrOperandContext? op, string role)
        => op is null ? BoundOperandError.Refused(ctx.Edition, role)
        : op.strUnstrSender() is { } snd ? StrUnstrSender(snd, role)
        : op.literal() is { } lit ? host.Expr.LiteralOperand(lit)
        : op.figurativeConstant() is { } fig ? host.Expr.FigurativeOperand(fig)
        : BoundOperandError.Refused(ctx.Edition, role);

    /// <summary>Bind the narrower SENDER shape — an identifier only (a function-identifier or a data reference),
    /// no literal. This is what §14.9.48.2's `UNSTRING identifier-1` admits, and
    /// <see cref="StrUnstrOperand"/> delegates its two identifier arms here so the shapes cannot drift apart.</summary>
    private BoundOperand StrUnstrSender(Core.StrUnstrSenderContext? snd, string role)
        => snd?.inlineMethodInvocation() is { } imi ? host.Oo.OoInlineInvocationOperand(imi)   // §8.4.3.1.2 Format 4; kb/Work PB428
        : snd?.functionCall() is { } fn ? IntrinsicBinder.OperandOf(host.Intrinsic.BindIntrinsic(fn))
        : snd?.dataReference() is { } dref ? ScreenedField(dref, role)
        : BoundOperandError.Refused(ctx.Edition, role);

    /// <summary>A sending data reference, screened for an INDEX-NAME (kb/Work R16): STRING/UNSTRING are none
    /// of §13.18.38.3 r7's five index-name contexts, and this funnel serves all four sending positions — the
    /// one place, so the diagnostic cannot cover three slots and miss the fourth. Before, the reference
    /// compiled clean and aborted at run time in the string channel.</summary>
    private BoundOperand ScreenedField(Core.DataReferenceContext dref, string role)
    {
        var op = host.Expr.FieldOperand(dref);
        return host.Expr.ScreenIndexNameOperand(op, DataBinder.WrittenText(dref), role)
            ? BoundOperandError.Refused(ctx.Edition, $"{role}: the index-name '{DataBinder.WrittenText(dref)}' (ISO §13.18.38.3 r7)")
            : op;
    }

    /// <summary>⛔ ISO §14.9.43.3 SR1 AS ONE PREDICATE — the OFFENCE phrase for a STRING operand, or
    /// <see langword="null"/> when the operand is admitted. The rule is ONE sentence with two halves and it
    /// reaches every operand position of the statement but identifier-4:
    /// <list type="bullet">
    /// <item>"All literals shall be described as alphanumeric, boolean, or national literals" — so a NUMERIC
    /// literal is not a STRING operand, in the sending position or the DELIMITED BY position.</item>
    /// <item>"all identifiers, except identifier-4, shall be described implicitly or explicitly as usage display
    /// or national" — identifier-4 is the POINTER, so identifier-1, identifier-2 AND identifier-3 are covered.
    /// A reference-modified operand answers with identifier-1's own usage, which is what §8.4.3.3.4 GR6 gives
    /// the unique data item ("the same class, category, and usage as that defined for identifier-1").</item>
    /// </list>
    /// <para>An ALPHANUMERIC or NATIONAL GROUP is EXEMPT: §8.5.2.1 treats an alphanumeric group as usage display,
    /// and §14.9.43.4 GR3a defines the transfer "in accordance with the MOVE statement rules for
    /// alphanumeric-to-alphanumeric moves", which admit a group — including one holding a BINARY/PACKED leaf (V59).
    /// A STRONGLY-TYPED group is NOT: its class is its type-name and nothing gives it a usage, so SR1 reads the
    /// usage off its elementary items and refuses one holding a leaf that is neither display nor national
    /// (kb/Work PB244).</para>
    /// <para>A FUNCTION-IDENTIFIER is not screened here and that is deliberate: SR1's antecedent is a
    /// DESCRIPTION ("shall be described … as usage display or national"), and a function result has no data
    /// description entry to read a usage from (§8.4.3.2.1 — "the unique data item that results from the
    /// evaluation of a function"). The screen rejects what the standard names, never what it cannot
    /// classify.</para>
    /// <para>Edition-invariant: SR1 is unchanged at 85/2002/2014/2023.</para></summary>
    private static string? Sr1Offence(BoundOperand op) => op switch
    {
        BoundNumericLiteral => "is a numeric literal; SR1 admits only alphanumeric, boolean and national literals",
        BoundFieldOperand { Place: { } p } when !p.Item.IsGroup && p.DenotedItem is not null
            && !p.Item.StoreAsImage && p.Item.Pic is { } pic
            && pic.Usage is not (Usage.Display or Usage.National) =>
            $"is an elementary item of USAGE {pic.Usage}, which has no character image; SR1 requires every "
            + "identifier except the POINTER to be usage display or national",
        // ⛔ A STRONGLY-TYPED GROUP HAS NO USAGE OF ITS OWN (kb/Work PB244): §8.5.2.1 gives "an alphanumeric group
        // item" the usage of display ("is treated as though it had a usage of display") and a national group its
        // class national, but the class AND category of a strongly-typed group are its type-name, so SR1's
        // "described implicitly or explicitly as usage display or national" can only be read off the elementary
        // items it is made of. A strong group holding a POINTER / OBJECT REFERENCE / BINARY ... leaf is therefore
        // not described as usage display, and was compiled and then aborted at run time in the Tier-C whole-group
        // island ("whole-group image ... with a pointer/object-class leaf"). The strong group whose every leaf is
        // display or national satisfies the sentence and keeps working.
        BoundFieldOperand { Place: { } p } when p.DenotedItem is not null && StrongTypeModel.IsStrongGroup(p.Item)
            && FirstLeafOutsideDisplayOrNational(p.Item) is { } leaf =>
            $"is a strongly-typed group item holding '{leaf.CobolName ?? leaf.CsName}' of USAGE {leaf.Pic!.Usage}, "
            + "which has no character image; SR1 requires every identifier except the POINTER to be usage display "
            + "or national, and a strongly-typed group's usage is that of the elementary items it is made of "
            + "(§8.5.2.1 gives only an alphanumeric group the usage of display)",
        _ => null,
    };

    /// <summary>The first elementary item beneath <paramref name="group"/> (declaration order, REDEFINES views
    /// excluded: they occupy no storage of their own) whose usage is neither display nor national — null when the
    /// group is made wholly of character-form items. A leaf stored as a character image (<c>StoreAsImage</c>) is a
    /// DISPLAY or NATIONAL numeric by construction and passes.</summary>
    private static DataItem? FirstLeafOutsideDisplayOrNational(DataItem group)
    {
        foreach (var c in group.Children.Where(c => c.RedefinesTargetName is null))
        {
            if (c.IsGroup)
            {
                if (FirstLeafOutsideDisplayOrNational(c) is { } inner) return inner;
            }
            else if (!c.StoreAsImage && c.Pic is { } pic && pic.Usage is not (Usage.Display or Usage.National))
                return c;
        }
        return null;
    }

    /// <summary>The ONE report site for SR1's FIRST sentence (ISO §14.9.43.3 — the usage / literal-kind rule) — so
    /// the three identifier positions and the two literal positions cannot drift onto different diagnostics for
    /// one sentence. The SECOND sentence (the all-national rule) is the rule shape UNSTRING SR3 and INSPECT SR4
    /// share, and reports through <see cref="AllOrNothingClass"/> as COBOLNET2306 (kb/Work PB980).</summary>
    private BoundStatement Sr1Reject(string where, string offence)
    {
        return BoundRejected.Report(ctx.Edition, DiagnosticCatalog.CharacterOperandUsage, $"{where} {offence} (ISO §14.9.43.3 SR1)");
    }

    /// <summary>ISO §14.9.48.3 SR2 — the OFFENDING category of an UNSTRING sender (as its printable name), or
    /// <see langword="null"/> when it is a permitted one. Reads the category off whichever operand shape the
    /// sender is: a FIELD's PICTURE (a group has none and is an alphanumeric-image sender, so it passes), or a
    /// numeric/boolean intrinsic's §15.2 RESULT category. A string-class function (UPPER-CASE, TRIM,
    /// SUBSTITUTE …) is a permitted sender; FUNCTION ORD or a boolean function is not, for exactly the reason a
    /// numeric ITEM is not. An alphanumeric-EDITED or national-EDITED item (Category Alphanumeric/National WITH
    /// an EditMask — there is no *Edited enum member for them) is category alphanumeric-edited/national-edited,
    /// NOT the category alphanumeric/national SR2 names (kb/Work PB155's sweep — the old screen passed
    /// them).</summary>
    private static string? UnstringSenderCategory(BoundOperand source) => source switch
    {
        BoundFieldOperand f => Sr2OffendingCategory(f.Place.Item),
        // ⛔ THE LITERAL ARMS OF §14.9.48.3 SR1 (kb/Work PB1181): "Literal-1 and literal-2 shall be literals of the
        // category alphanumeric or national" — a numeric literal (written, or a constant-name's, or a function folded
        // at bind) and a boolean one (plain or ALL) offend. They fell to `_ => null` because this arm was written for
        // the IDENTIFIER positions of SR2 only, so `DELIMITED BY 5` split the sender on the digit.
        BoundNumericLiteral => "numeric",
        BoundStringLiteral { Category: PicCategory.Boolean } or BoundAllLiteral { Category: PicCategory.Boolean } => "boolean",
        // ⛔ THE SAME WHITELIST, ON THE OTHER ARM (kb/Work PB664). This arm listed the categories it REFUSES
        // while its FIELD twin was being turned into the rule's own admit-list — two arms of one sentence, one
        // of them fixed, which is this repository's most reproducible defect shape. It is behaviour-identical
        // TODAY by construction (IntrinsicCatalog.CategoryOf is total over exactly National / Alphanumeric /
        // Boolean / Numeric, so the two spellings partition the same four values) and it stays right if that
        // mapping ever gains a fifth.
        BoundComputedOperand { Expr: BoundIntrinsicCall ic } when ic.ResultCategory is not
            (PicCategory.Alphanumeric or PicCategory.National) => ic.ResultCategory.ToString(),
        _ => null,
    };

    /// <summary>ISO §14.9.48.3 SR2's category test as ONE predicate — the rule names identifier-1, -2, -3 AND
    /// -5 in a single sentence, so the sender, both delimiter operands and the DELIMITER IN receiver all ask
    /// this (kb/Work PB155).
    /// <para>⛔ IT IS A WHITELIST, AND THAT IS THE RULE'S OWN SHAPE (kb/Work PB664,
    /// <c>feedback_model_the_rule_shape_not_one_case</c>): SR2 says the operand "shall reference data items of
    /// category alphanumeric or national", so everything else offends BY CONSTRUCTION. It used to name the
    /// categories it REFUSED — numeric, numeric-edited, boolean and the two edited forms — and the four
    /// PICTURE categories the model gained later (data-pointer, program-pointer, function-pointer and object
    /// reference) therefore passed as if they were admitted. MEASURED at 2f6b38c61: `UNSTRING P DELIMITED BY
    /// " " INTO B` over a USAGE POINTER sender stored "Cobo" — the first four characters of the CLR carrier's
    /// type name — and `UNSTRING A DELIMITED BY P` compared every character against that type name and never
    /// found a delimiter. A blacklist is a list that a new category joins silently.</para>
    /// <para>A GROUP has no PICTURE and answers by its GROUP-USAGE: an ordinary group is "an alphanumeric group
    /// item" (§13.18.29.4 GR3) and a national group is category national (GR2), both admitted; a BIT group is
    /// class and category BOOLEAN (GR1a) and is not.</para>
    /// <para>⚠ Category ALPHABETIC is admitted here, and the reason is a MODEL fold, not an adjudication: the
    /// storage model carries PIC A as <see cref="PicCategory.Alphanumeric"/> (one string carrier), so this
    /// predicate cannot tell them apart. SR2 is category-worded and would separate them — the un-folding lives
    /// in <c>IntrinsicArgumentRules.ClassOfPlace</c>'s <c>PicInfo.IsAlphabetic</c> and reaching it from here is
    /// a change of verdict for existing source, so it is recorded rather than made silently.</para></summary>
    private static string? Sr2OffendingCategory(DataItem item) => item.Pic switch
    {
        // ⛔ A STRONGLY-TYPED GROUP is none of the three (kb/Work PB244): §8.5.2.1 — "Both the class and the category
        // of a strongly-typed group item are the type-name specified in the TYPE clause", and only the groups that
        // are NOT strongly typed are alphanumeric, boolean or national. SR2 admits category alphanumeric or
        // national only, so it offends by construction (it compiled and aborted at run time when it held a
        // pointer leaf, and quietly examined the image of one that did not).
        null when StrongTypeModel.IsStrongGroup(item) =>
            $"'{StrongTypeModel.TypeAnchor(item)?.TypeName ?? item.CobolName}' — the type-name of a strongly-typed group item (§8.5.2.1)",
        null => item.GroupUsage is GroupUsage.Bit ? "boolean (a bit group, §13.18.29.4 GR1 a)" : null,
        { Category: PicCategory.Alphanumeric or PicCategory.National, EditMask: null } => null,
        { Category: PicCategory.Alphanumeric or PicCategory.National } p =>
            $"{p.Category.ToString().ToLowerInvariant()}-edited",
        { Category: var cat } => cat.ToString(),
    };

    /// <summary>A STRING / UNSTRING syntax-rule violation: reported at BIND (COBOLNET1651 — the compile fails) and
    /// staged loud as the backstop the caller returns (kb/Work PB88: the stage alone let illegal source compile
    /// clean and die at run time). ONE helper, so no rule site can forget the diagnostic half again.</summary>
    private BoundStatement Reject(string message)
    {
        return BoundRejected.Report(ctx.Edition, DiagnosticCatalog.StringUnstringOperandRule, message);
    }

    /// <summary>True for an elementary fixed-point INTEGER item with no P scaling — the shape STRING SR7 /
    /// UNSTRING SR5–SR6 require of the POINTER / COUNT IN / TALLYING items (a V or P picture yields a non-zero
    /// scale; P is the signed-scale encoding, §13.18.40).</summary>
    private static bool StrUnstrIsInteger(Place p) =>
        p.Item.Pic is { Category: PicCategory.Numeric, IsFloat: false, Scale: 0 };

    /// <summary>ISO §14.9.43.3 SR2 / §14.9.48.3 SR1 — "a figurative constant that begins with the word ALL", read off the
    /// WRITTEN operand: the ALL-literal node says so (<see cref="BoundAllLiteral.BeginsWithAll"/>, a bare symbolic
    /// character does not), but <c>ALL SPACES</c> / <c>ALL ZEROS</c> bind to a plain <see cref="BoundFigurative"/> that
    /// has lost the word, so the operand's first TOKEN is asked as well (the rule's own words: it "begins with" ALL;
    /// no data-name, function or literal can start with the reserved word). kb/Work PB1181: `STRING ALL SPACES …`
    /// compiled clean.</summary>
    private static bool BeginsWithAllWord(Core.StrUnstrOperandContext? written, BoundOperand bound) =>
        bound is BoundAllLiteral { BeginsWithAll: true } || written?.Start?.Type == Core.ALL;

    /// <summary>ISO §14.9.43.3 SR3 / §14.9.48.3 SR1 — a delimiter LITERAL of zero length (§8.3.3.1: contiguous opening
    /// and closing delimiters), written plain, hexadecimal or national, or as the operand of the ALL phrase. A
    /// figurative constant has no length of its own and is never one. ONE predicate for both verbs, so the rule cannot
    /// be implemented at one of its two statements (kb/Work PB1181).</summary>
    private static bool IsZeroLengthLiteral(BoundOperand op) =>
        op is BoundStringLiteral { Value.Length: 0 } or BoundAllLiteral { Literal.Length: 0 };

    /// <summary>⛔ ISO §14.9.43.3 SR8 — the OFFENCE phrase for a STRING identifier-1 / identifier-2 that is an
    /// ELEMENTARY NUMERIC data item and is not "an integer without the symbol 'P' in its picture character-string",
    /// or <see langword="null"/> (kb/Work PB1182). Only SR7's identifier-4 was ever asked
    /// <see cref="StrUnstrIsInteger"/>, so `STRING N DELIMITED BY SIZE` over <c>PIC 9V9</c> sent "12". A group, a
    /// reference-modified slice (the §8.4.3.3.4 GR6 unique item is not the numeric description) and a literal are not
    /// "an elementary numeric data item" and pass; a non-DISPLAY/NATIONAL usage was already refused by SR1.</summary>
    private static string? Sr8Offence(BoundOperand op) => op switch
    {
        BoundFieldOperand { Place: { } p } when p.DenotedItem is not null && !p.Item.IsGroup
            && p.Item.OperandPic is { Category: PicCategory.Numeric, Scale: not 0 } pic =>
            pic.HasPScaling
                ? "is an elementary numeric item whose picture contains the symbol P; SR8 requires an integer without it"
                : "is an elementary numeric item with digit positions to the right of the assumed decimal point; SR8 requires an integer",
        _ => null,
    };

    /// <summary>ISO §14.9.48.3 SR4 — the permitted UNSTRING INTO (identifier-4) receiver categories: usage display
    /// with category alphabetic/alphanumeric/numeric (alphabetic folds into <see cref="PicCategory.Alphanumeric"/>;
    /// <c>EditMask: null</c> excludes alphanumeric-edited), OR usage national with category national/numeric. A
    /// numeric receiver must be non-float and usage display or national — so a numeric-edited, COMP/packed/COMP-5,
    /// index, boolean, or float receiver is rejected. National is SR4-LEGAL and stays allowed (the emitter defers it
    /// separately). Callers exempt a group (SR10) and a reference-modified slice before consulting this.</summary>
    private static bool UnstringReceiverAllowed(PicInfo? pic) => pic is
        { Category: PicCategory.Alphanumeric, EditMask: null }
        // EditMask: null on the National arm too (kb/Work PB155's sweep): SR4 names CATEGORY national, and a
        // national-edited picture (Category National WITH an EditMask) is category national-edited — the
        // alphanumeric arm screened its edited form while this arm admitted one. (Unreachable until Phase 4a
        // residue #2 lands — a national-edited ITEM is 0899 at declaration; shaped for that landing.)
        or { Category: PicCategory.National, EditMask: null }
        // SR4's last sentence: "Numeric items shall not be specified with the symbol 'P'" (kb/Work PB1182) — a V alone is
        // not a P, so PIC 9V9 stays a legal receiver and PIC 99P / PP99 do not.
        or { Category: PicCategory.Numeric, IsFloat: false, Usage: Usage.Display or Usage.National, HasPScaling: false };
}

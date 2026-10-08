// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding.Model;
using CobolNet.CodeGen.Emit;
using CobolNet.Runtime;
using CobolNet.Runtime.IO;

namespace CobolNet.CodeGen;

/// <summary>
/// The typed, <c>nameof</c>-anchored façade over the emitted runtime surface (P7 Step 4b;
/// DESIGN-codegen-backend §3): every C# fragment that names a runtime member routes through here, so a runtime
/// rename breaks THIS file at compile time instead of silently mis-emitting text. Migration is INCREMENTAL by
/// design (the doc's shrinking-whitelist plan): the ratchet guard test
/// (<c>tests/Cobol.Net.Tests.Characterization/RuntimeApiGuardTests.cs</c>) pins each CodeGen file's bare
/// <c>Cobol*.</c> count and fails on any INCREASE — Step 9's per-verb rewrites drive the counts to zero, at
/// which point the whitelist empties and the guard flips to forbid-all. Type-name anchors land first (a runtime
/// TYPE rename already breaks here); member anchors accrete per migrated file.
/// </summary>
internal static class RuntimeApi
{
    // ── Type-name anchors (each is a compile-time reference to the runtime type). ──
    public static string Bool => nameof(CobolBool);

    // ── The CALL-site exception carrier (ISO §14.9.4.4 GR3h/GR3i; kb/Work PB233). Its two classification
    //    predicates are asked in BOTH directions, and both route here so the runtime member is named once:
    //    at COMPILE time to split the statement's enabled name set into catch arms, and as EMITTED TEXT in the
    //    arm that must test the raised name at run time. ──

    /// <summary>ISO §14.9.4.4 GR3h item 1's family partition — "if the exception condition is any of the
    /// EC-PROGRAM or EC-EXTERNAL exception conditions" — asked at COMPILE time over one enabled name.</summary>
    public static bool CallEcIsProgramOrExternal(string ec) => CobolCallException.IsProgramOrExternal(ec);

    /// <summary>The EMITTED form of the same predicate, over a caught exception's <c>EcName</c> — the arm that
    /// takes item 1's families when checking for the raised name is NOT enabled.</summary>
    public static string CallEcIsProgramOrExternalText(string ecNameExpr) =>
        $"{nameof(CobolCallException)}.{nameof(CobolCallException.IsProgramOrExternal)}({ecNameExpr})";

    /// <summary>Can a <see cref="CobolCallException"/> actually raise <paramref name="ec"/>? A COMPILE-time
    /// question: an enabled name with no raise site on this carrier would contribute a catch-filter disjunct
    /// that can never be true (<see cref="CobolCallException.CarriedNames"/>).</summary>
    public static bool CallEcIsCarried(string ec) => CobolCallException.CanCarry(ec);

    /// <summary>Boolean NOT — ISO §8.8.4.5 boolean expressions (the D-B1 '0'/'1' string substrate).</summary>
    public static string BoolNot(string operand) => $"{nameof(CobolBool)}.{nameof(CobolBool.Not)}({operand})";

    /// <summary>The alphabet-name class condition's membership test (ISO §8.8.4.4.4 GR3 a; kb/Work PB109):
    /// <c>CobolClass.IsInCodedSet(arg, CobolClass.CodedSetKind.&lt;kind&gt;)</c>.</summary>
    public static string ClassInCodedSet(string arg, string kind) =>
        $"{nameof(CobolClass)}.{nameof(CobolClass.IsInCodedSet)}({arg}, {nameof(CobolClass)}.{nameof(CobolClass.CodedSetKind)}.{kind})";

    /// <summary>A boolean dyadic op (AND/OR/XOR/EXCLUSIVE-OR family) — <c>CobolBool.{method}(l, r)</c>.
    /// <paramref name="method"/> is the runtime method NAME (validated by the anchors below at compile time
    /// via <see cref="BoolOpName"/>).</summary>
    public static string BoolOp(string method, string l, string r) => $"{nameof(CobolBool)}.{method}({l}, {r})";

    /// <summary>The literal-optimized variant <c>CobolBool.{method}All(operand, bits)</c>.</summary>
    public static string BoolOpAll(string method, string operand, string bitsLiteral) =>
        $"{nameof(CobolBool)}.{method}All({operand}, {bitsLiteral})";

    /// <summary>Compile-time anchor for the boolean dyadic method names the binder selects: renaming
    /// <c>CobolBool.And/Or/Xor</c> breaks this member, not the emitted text.</summary>
    public static string BoolOpName(char op) => op switch
    {
        '|' => nameof(CobolBool.Or),
        '^' => nameof(CobolBool.Xor),
        _ => nameof(CobolBool.And),   // '&' and the (unreachable) default — the pre-4b table's shape
    };

    /// <summary>A boolean shift/rotate (ISO §8.8.2 rule 8, 2023) — <c>CobolBool.Shift{Left|Right}[Circular](v, k)</c>.</summary>
    public static string BoolShift(CobolNet.Binding.Bound.BoolShiftKind kind, string operand, string count) => kind switch
    {
        CobolNet.Binding.Bound.BoolShiftKind.Left => $"{nameof(CobolBool)}.{nameof(CobolBool.ShiftLeft)}({operand}, {count})",
        CobolNet.Binding.Bound.BoolShiftKind.Right => $"{nameof(CobolBool)}.{nameof(CobolBool.ShiftRight)}({operand}, {count})",
        CobolNet.Binding.Bound.BoolShiftKind.LeftCircular => $"{nameof(CobolBool)}.{nameof(CobolBool.ShiftLeftCircular)}({operand}, {count})",
        _ => $"{nameof(CobolBool)}.{nameof(CobolBool.ShiftRightCircular)}({operand}, {count})",
    };

    // ── Numeric (CobolNum) ──

    /// <summary>Decode a zoned/separate-sign DISPLAY image per the receiver's profile — <c>CobolNum.ParseDisplay</c>.</summary>
    public static string NumParseDisplay(string image, string profile) =>
        $"{nameof(CobolNum)}.{nameof(CobolNum.ParseDisplay)}({image}, {profile})";

    /// <summary>An unsigned integer's DISPLAY image zero-padded to a fixed digit width —
    /// <c>CobolNum.FormatUnsignedDisplay</c> (the ACCEPT temporal conceptual-item image, ISO §14.9.1.4 GR7–GR12).</summary>
    public static string NumFormatUnsignedDisplay(string value, int digits) =>
        $"{nameof(CobolNum)}.{nameof(CobolNum.FormatUnsignedDisplay)}({value}, {digits})";

    /// <summary>The text image of an intrinsic FUNCTION's returned value (ISO §15.4 temporary item;
    /// characteristics implementor-defined under native arithmetic, §15.4.1) — <c>CobolNum.FormatFunctionText</c>.
    /// The literal form, so a folded intrinsic and a computed one are indistinguishable (DA2).</summary>
    /// <paramref name="deSign"/> carries §14.9.25.4 GR6a (the operational sign is not moved to an alphanumeric
    /// receiver / text comparison) — the same flag <c>FieldAsString</c> honours for a signed FIELD operand.
    /// <paramref name="commaMode"/> is the decimal separator the image is written with (kb/Work PB2507): the
    /// referencing unit's DECIMAL-POINT IS COMMA mode for a call, the materialized temporary's own for a place —
    /// REQUIRED, so no emit site can forget it.
    public static string NumFormatFunctionText(string value, int scale, bool deSign, bool commaMode) =>
        $"{nameof(CobolNum)}.{nameof(CobolNum.FormatFunctionText)}({value}, {scale}{FunctionTextFlags(deSign, commaMode, ", ")})";

    /// <summary>The same text image for a STANDARD-DECIMAL intermediate — <c>CobolDec.ToFunctionText</c>.</summary>
    public static string DecFunctionText(string value, bool deSign, bool commaMode) =>
        $"({value}).{nameof(CobolDec.ToFunctionText)}({FunctionTextFlags(deSign, commaMode, "")})";

    /// <summary>The trailing <c>deSign</c> / <c>commaMode</c> arguments of a function-text call, each written only when
    /// set (an unflagged call keeps its byte-stable emission); <paramref name="lead"/> precedes a non-empty list.</summary>
    private static string FunctionTextFlags(bool deSign, bool commaMode, string lead) => (deSign, commaMode) switch
    {
        (false, false) => "",
        (true, false) => lead + "true",
        (false, true) => lead + "commaMode: true",
        (true, true) => lead + "true, commaMode: true",
    };

    /// <summary>The numeric MOVE-rules store (decimal alignment, truncation/zero-fill) — <c>CobolNum.Store</c>,
    /// or <c>CobolNum.StoreU</c> when the VALUE expression is on the unsigned-wide lane (<c>NumX.U</c> — a
    /// 16-byte unsigned COMP-5 read or the HIGHEST-ALGEBRAIC fold literal, kb/Work R10). The lane is picked by
    /// NAME, never by overload: an int constant converts implicitly to both Int128 and UInt128, so a same-name
    /// pair makes every <c>Store(0, …)</c>-shaped emission a CS0121 ambiguity.</summary>
    public static string NumStore(string value, string scale, string profile, bool u = false) =>
        $"{nameof(CobolNum)}.{(u ? nameof(CobolNum.StoreU) : nameof(CobolNum.Store))}({value}, {scale}, {profile})";

    /// <summary>Render an unscaled value as the receiver's DISPLAY image — <c>CobolNum.FormatDisplay</c>
    /// (<c>FormatDisplayU</c> for a UInt128-carrier read — see <see cref="NumStore"/> on lane-by-name).
    /// ⛔ This is the CHARACTER rendering (what a DISPLAY statement shows). For the bytes an item occupies in a
    /// record, a file, a SORT key or a REDEFINES backing, use <see cref="NumFormatImage"/> — for a BINARY or
    /// PACKED item the two differ, and that difference is V59.</summary>
    public static string NumFormatDisplay(string value, string profile, bool u = false) =>
        $"{nameof(CobolNum)}.{(u ? nameof(CobolNum.FormatDisplayU) : nameof(CobolNum.FormatDisplay))}({value}, {profile})";

    /// <summary>Encode an unscaled value as the BYTES the item occupies at a byte boundary — the record/group
    /// image, a file record, a SORT key window, a Tier-B REDEFINES backing (<c>CobolNum.FormatImage</c>,
    /// COBOLNET_DESIGN §14.4). Zoned items render exactly as <see cref="NumFormatDisplay"/>; BINARY and PACKED
    /// render their radix-2 / BCD bytes.</summary>
    public static string NumFormatImage(string value, string profile) =>
        $"{nameof(CobolNum)}.{nameof(CobolNum.FormatImage)}({value}, {profile})";

    /// <summary>Decode an item's record-image bytes back to its unscaled value — <c>CobolNum.ParseImage</c>, the
    /// inverse of <see cref="NumFormatImage"/>.
    /// <para>⛔ <paramref name="sending"/> HAS NO DEFAULT, deliberately (the "caller names the landing" shape
    /// <see cref="FloatToScaled"/> uses): a windowed decode is either a SENDING reference of the item's content,
    /// which ISO §14.6.13.2 rule 2 makes EC-DATA-INCOMPATIBLE-checkable, or it is not — and a new call site that
    /// silently inherited "not" is precisely how rule 2 came to have no raise site at all (kb/Work PB230).
    /// <c>true</c> emits <c>CobolNum.ParseImageSending</c>, which tests the content against the numeric class
    /// condition under checking and is otherwise identical.</para></summary>
    public static string NumParseImage(string image, string profile, bool sending) =>
        $"{nameof(CobolNum)}.{(sending ? nameof(CobolNum.ParseImageSending) : nameof(CobolNum.ParseImage))}({image}, {profile})";

    /// <summary>The table SORT (ISO §14.9.40 Format 2) — <c>CobolTable.SortInPlace(occurrences, comparison)</c>
    /// over the table's CURRENT occurrences (GR20; kb/Work PB1174): the stable <c>OrderBy</c> §14.9.40.4 GR19c/GR3c
    /// want, with the framework array sort's comparer-exception wrapper undone so a key comparison's fatal COBOL
    /// exception condition still reaches the statement guard (kb/Work PB230).</summary>
    public static string TableSortInPlace(string occurrences, string comparison) =>
        $"{nameof(CobolTable)}.{nameof(CobolTable.SortInPlace)}({occurrences}, {comparison})";

    /// <summary>The current occurrences of a table as a span: a fixed or OCCURS DEPENDING table's array prefix of
    /// <paramref name="count"/> elements, or a dynamic-capacity table's <c>CurrentOccurrences</c>.</summary>
    public static string TableCurrentOccurrences(string table, string? count) => count is null
        ? $"{table}.{nameof(CobolDynTable<int>.CurrentOccurrences)}"
        : $"System.MemoryExtensions.AsSpan({table}, 0, {count})";

    /// <summary>The checked read of a BOOLEAN sending operand — <c>CobolBool.Sending(value)</c>: raises the fatal
    /// EC-DATA-INCOMPATIBLE for content that is not all <c>'0'</c>/<c>'1'</c> under checking (ISO §14.6.13.2
    /// rule 1), else returns the value. The boolean twin of <see cref="FloatSending"/>.</summary>
    public static string BoolSending(string value) =>
        $"{nameof(CobolBool)}.{nameof(CobolBool.Sending)}({value})";

    /// <summary>⛔ THE ONE NUMERIC CLASS CONDITION over a numeric item's stored image —
    /// <c>CobolNum.IsNumericImage</c> (ISO §8.8.4.4.4 GR3 n)1, keyed on the item's byte form). Emitted by the class
    /// condition itself, and called from inside <see cref="NumParseImage"/>'s checked lane, because §14.6.13.2
    /// rule 2 defines its own test BY REFERENCE to this one — one rule, one place (kb/Work PB230).</summary>
    public static string NumIsNumericImage(string image, string profile) =>
        $"{nameof(CobolNum)}.{nameof(CobolNum.IsNumericImage)}({image}, {profile})";

    /// <summary>⛔ THE BOOLEAN CLASS CONDITION — <c>CobolClass.IsBoolean</c> (ISO §8.8.4.4.4 GR3 e): "the
    /// condition is true if the content of the data item referenced by identifier-1 consists entirely of the
    /// boolean values '0' and '1'", with GR1's zero-length FALSE already on it. Anchored here rather than
    /// emitted bare because it is the class condition's NEWEST alternative (kb/Work PB590) and the ratchet in
    /// <c>RuntimeApiGuardTests</c> is what keeps the renderer's remaining bare accesses shrinking; its three
    /// ALPHABETIC siblings route at P9 with the rest of <c>ConditionRenderer</c>.</summary>
    public static string ClassIsBoolean(string value) =>
        $"{nameof(CobolClass)}.{nameof(CobolClass.IsBoolean)}({value})";

    /// <summary>⛔ THE FLOATING-POINT CLASS CONDITIONS — <c>CobolFloatClass.Is</c> over a float carrier read (ISO
    /// §8.8.4.4.4 GR3 g)–m) and n) 1. b.; kb/Work PB225). <paramref name="carrier"/> is the item's OWN
    /// <c>float</c>/<c>double</c> field, never a widened value: a binary32 → binary64 widening quiets a signaling
    /// NaN.</summary>
    public static string FloatClass(string carrier, FloatClassTest test) =>
        $"{nameof(CobolFloatClass)}.{nameof(CobolFloatClass.Is)}({carrier}, {nameof(FloatClassTest)}.{test})";

    /// <summary>The <see cref="FloatClass"/> twin for a float item stored as its IEEE window image —
    /// <c>CobolFloatClass.IsImage</c>, which reads the image's raw bits.</summary>
    public static string FloatClassImage(string image, string profile, FloatClassTest test) =>
        $"{nameof(CobolFloatClass)}.{nameof(CobolFloatClass.IsImage)}({image}, {profile}, {nameof(FloatClassTest)}.{test})";

    /// <summary>NUMERIC over a COMPUTED numeric operand's value (ISO §8.8.4.4.4 GR3 n) 1. c.; kb/Work PB1401) —
    /// <c>CobolValueClass.IsNumeric</c>: the operand is evaluated and its content is a valid value by
    /// construction. <paramref name="dec"/> is the operand lifted to its decimal form.</summary>
    public static string ValueClassIsNumeric(string dec) =>
        $"{nameof(CobolValueClass)}.{nameof(CobolValueClass.IsNumeric)}({dec})";

    /// <summary>FARTHEST-FROM-ZERO / NEAREST-TO-ZERO over a COMPUTED numeric operand's value (ISO §8.8.4.4.4 GR3 g)
    /// / m)) — <c>CobolValueClass.IsExtreme</c>, which takes the value ONCE. <paramref name="negative"/> is
    /// <c>null</c> for a description that cannot hold a sign.</summary>
    public static string ValueClassIsExtreme(string dec, string positive, string? negative) =>
        $"{nameof(CobolValueClass)}.{nameof(CobolValueClass.IsExtreme)}({dec}, {positive}, {negative ?? "null"})";

    /// <summary>IN-ARITHMETIC-RANGE over a COMPUTED numeric operand's value (ISO §8.8.4.4.4 GR3 l)) —
    /// <c>CobolValueClass.IsInArithmeticRange</c> against the mode's intermediate extremes.</summary>
    public static string ValueClassIsInArithmeticRange(string dec, string farthest, string nearest) =>
        $"{nameof(CobolValueClass)}.{nameof(CobolValueClass.IsInArithmeticRange)}({dec}, {farthest}, {nearest})";

    /// <summary>The rule-2 checked sending read on the STRING channel — <c>CobolNum.SendingImage</c>: a ZONED
    /// window is handed on VERBATIM (its stored image is its text), having first been tested against the numeric
    /// class condition under checking. <paramref name="sending"/> false is the raw read, for an exempt context
    /// (§14.6.13.2 rule 2's class-condition and VALIDATE dashes — <see cref="Emit.SendingRef"/>).</summary>
    public static string NumSendingImage(string image, string profile, bool sending) =>
        sending ? $"{nameof(CobolNum)}.{nameof(CobolNum.SendingImage)}({image}, {profile})" : image;

    /// <summary>The FLOAT decode lane (kb/Work PB164 wave 2) — the IEEE bit reinterpretation (the Int128 lane would
    /// numerically CONVERT).</summary>
    /// <param name="binary32Carrier">NO DEFAULT, so every site states which value it wants (kb/Work PB961).
    /// <c>true</c> — the item is binary32 (<c>PicInfo.IsSingle</c>) and the read is a STORE, COPY, class test or
    /// DISPLAY of its content: <c>CobolNum.ParseImageSingle</c>, a <c>float</c> with no binary64 between, because
    /// a binary32 → binary64 widening QUIETS a signaling NaN (§14.9.25.4 GR6 c) — a same-usage transfer is
    /// "without change"). <c>false</c> — a binary64 item, or an ARITHMETIC read of either width, where the
    /// widening is the operation: <c>CobolNum.ParseImageFloat</c>, a <c>double</c>.</param>
    public static string NumParseImageFloat(string image, string profile, bool binary32Carrier) =>
        binary32Carrier
            ? $"{nameof(CobolNum)}.{nameof(CobolNum.ParseImageSingle)}({image}, {profile})"
            : $"{nameof(CobolNum)}.{nameof(CobolNum.ParseImageFloat)}({image}, {profile})";

    /// <summary>The UNSIGNED decode twins (the Step D arm-1 dissolution) — a ulong/UInt128-carried window
    /// decodes to its container value, bit-identically through the signed lane.
    /// <para>⚠ The 8-byte (<c>ulong</c>) half has NO emitter caller: <c>ParseBinaryImage</c> already returns an
    /// unsigned item's full width as a non-negative <c>Int128</c>, so <c>NumericRenderer</c>'s generic
    /// StoreAsImage arm decodes a <c>PIC 9(10..18) COMP-5</c> window identically and the extra arm was pure
    /// duplication (kb/Work PB164, the Step D review). Kept as the named half of the runtime's unsigned lane
    /// pair, NOT as live drift — the 16-byte twin below is the one an emitter picks.</para></summary>
    public static string NumParseImageU(string image, string profile) =>
        $"{nameof(CobolNum)}.{nameof(CobolNum.ParseImageU)}({image}, {profile})";

    /// <inheritdoc cref="NumParseImageU"/>
    /// <param name="sending">As <see cref="NumParseImage"/> — no default, so a new site states whether it is a
    /// §14.6.13.2 rule 2 sending reference.</param>
    public static string NumParseImageU128(string image, string profile, bool sending) =>
        $"{nameof(CobolNum)}.{(sending ? nameof(CobolNum.ParseImageU128Sending) : nameof(CobolNum.ParseImageU128))}({image}, {profile})";

    /// <summary>The FLOAT encode lane (kb/Work PB164 wave 2) — distinctly named because FormatImage overloads on a
    /// float would make integer call sites ambiguous.</summary>
    /// <param name="binary32">NO DEFAULT (kb/Work PB961): the RECEIVING item's width, <c>PicInfo.IsSingle</c>. A
    /// binary32 item encodes through <c>CobolNum.FormatImageSingle</c> after an explicit <c>(float)</c> — a no-op on
    /// a <c>float</c> value, the item's own narrowing on a <c>double</c> one — so a binary32 value never widens on
    /// its way to its own bytes (a widening quiets a signaling NaN, which ISO §14.9.39.4 rule 35 and §14.9.25.4
    /// GR6 c) both forbid). The choice is keyed on the ITEM, never on the value's C# type, so a new site cannot
    /// pick the wrong lane by passing the right-looking expression.</param>
    public static string NumFormatImageFloat(string value, string profile, bool binary32) =>
        binary32
            ? $"{nameof(CobolNum)}.{nameof(CobolNum.FormatImageSingle)}((float)({value}), {profile})"
            : $"{nameof(CobolNum)}.{nameof(CobolNum.FormatImageFloat)}({value}, {profile})";

    // ── Strings (CobolString) ──

    /// <summary>The alphanumeric MOVE-rules store (left-justify, right space-fill/truncate) —
    /// <c>CobolString.Store</c>.</summary>
    public static string StrStore(string value, string width) =>
        $"{nameof(CobolString)}.{nameof(CobolString.Store)}({value}, {width})";

    /// <summary>The DYNAMIC LENGTH receiving store (ISO §8.5.1.10.4 — replace, truncate on the right at the
    /// maximum size, NO padding) — <c>CobolDynString.Store</c>. <paramref name="limit"/> is the item's
    /// §8.5.1.10.1 MAXIMUM SIZE (<c>DataItem.DynMaxSize</c>, from <c>CobolDynString.MaxSizeOf</c>) — always a real
    /// character count, never the "-1 = no LIMIT phrase" sentinel it used to be (kb/Work PB463).</summary>
    public static string DynStore(string value, string limit) =>
        $"{nameof(CobolDynString)}.{nameof(CobolDynString.Store)}({value}, {limit})";

    /// <summary>SET [SIZE OF] data-name TO n (ISO §14.9.39 Format 16) — set the current length of a dynamic-length
    /// item, space-filling grown positions (GR39); the negative→0 (GR37) and clamp-to-maximum (GR38) legs raise the
    /// nonfatal EC-STORAGE-NOT-AVAIL through the engine's own (ambient flag, name) pair, so the raise reaches a USE
    /// declarative (§14.6.13.1.4 #3) instead of only setting the status. <paramref name="newLen"/> is
    /// the arithmetic-expression-5 value at FULL precision (a <c>double</c>) so the GR37 sign test precedes the
    /// toward-zero truncation — <c>CobolDynString.SetSize(current, newLen, limit)</c>.</summary>
    public static string DynSetSize(string current, string newLen, string limit) =>
        $"{nameof(CobolDynString)}.{nameof(CobolDynString.SetSize)}({current}, {newLen}, {limit})";

    /// <summary>CONTINUE AFTER n SECONDS (ISO §14.9.9.4 GR1) — the timed pause for an interval carried as ONE value: a
    /// binary64 or a standard-decimal <c>CobolDec</c>. A negative interval sets the nonfatal
    /// EC-CONTINUE-LESS-THAN-ZERO when checking is enabled — <c>CobolTiming.ContinueAfter(seconds, check)</c>.</summary>
    public static string ContinueAfter(string seconds, string checkLessThanZero) =>
        $"{nameof(CobolTiming)}.{nameof(CobolTiming.ContinueAfter)}({seconds}, {checkLessThanZero})";

    /// <summary>The scaled-integer interval (kb/Work PB1529): the <c>Int128</c> or <c>UInt128</c> unscaled value and its
    /// scale, so the runtime takes the sign and the saturated whole seconds from the exact value —
    /// <c>CobolTiming.ContinueAfter(unscaled, scale, check)</c>.</summary>
    public static string ContinueAfterScaled(string unscaledOfCarrierType, string scale, string checkLessThanZero) =>
        $"{nameof(CobolTiming)}.{nameof(CobolTiming.ContinueAfter)}({unscaledOfCarrierType}, {scale}, {checkLessThanZero})";

    /// <summary>Set the run-unit termination status passed to the OS as the process exit code (ISO §14.9.42.4 GR5 /
    /// §14.9.18.4 GR10) — <c>RunUnit.SetExitStatus(status)</c>.</summary>
    public static string SetExitStatus(string status) =>
        $"{nameof(RunUnit)}.{nameof(RunUnit.SetExitStatus)}({status})";

    /// <summary>The CURRENT run unit's factory object of a class (ISO §9.3.14.2 — created before its first
    /// reference by a run unit; kb/Work PB1069) — <c>RunUnit.Current.FactoryObject&lt;F&gt;()</c>, where
    /// <paramref name="factoryCs"/> is the generated factory type.</summary>
    public static string FactoryObject(string factoryCs) =>
        $"{nameof(RunUnit)}.{nameof(RunUnit.Current)}.{nameof(RunUnit.FactoryObject)}<{factoryCs}>()";

    // ── Editing (CobolEdit) ──

    /// <summary>Edit a numeric value into a PICTURE mask — <c>CobolEdit.Format</c>. <paramref name="cfgArgs"/> is
    /// the per-item editing-config suffix (<see cref="Emit.EmitContext.EditCfg"/>), possibly empty.</summary>
    public static string EditFormat(string value, string scale, string maskLiteral, string cfgArgs) =>
        $"{nameof(CobolEdit)}.{nameof(CobolEdit.Format)}({value}, {scale}, {maskLiteral}{cfgArgs})";

    /// <summary>THE edited MOVE-semantics store keyed on the receiver's picture (data-model design D21 / kb/Work PB66 —
    /// the form dispatch lives HERE, never at a call site): a floating-point numeric-edited receiver takes
    /// <c>CobolEdit.FormatFloatMove</c> over the sender's exact form (an unscaled Int128 + scale, a CobolDec, or a
    /// binary64 — §14.9.25.4 GR6 item 4: overflow → EC-DATA-OVERFLOW + the pinned saturated image, underflow → zero);
    /// a fixed-point one the classic <c>CobolEdit.Format</c> over the value ALIGNED at the mask's scale
    /// (<paramref name="alignedFixed"/> — the caller's rescale, which a floating-point form never needs).</summary>
    public static string EditFormatFor(PicInfo pic, Emit.NumX value, string alignedFixed, string alignedScale, string cfgArgs)
    {
        // A format-2 (LOCALE) receiver edits through CobolLocaleEdit (§13.18.40.5 r9–r15; PB64 T6) — the arm
        // sits FIRST because a locale item has NO EditMask and the deref below is reachable-null for it. Its
        // cfgArgs carry at most the blankWhenZero: flag — EmitContext.EditCfg produces "" for a locale item
        // (no currencyString:, no commaMode: — §13.18.40.5 r9 / §12.3.7.4 GR14) and EditsArg "" (no EDITING).
        if (pic.LocaleEdit is { } le)
            return $"{nameof(CobolLocaleEdit)}.{nameof(CobolLocaleEdit.Format)}({alignedFixed}, {alignedScale}, "
                + $"{Emit.EmitText.CsLiteral(le.Picture)}, {LocaleTagArg(le.Locale)}, {le.Size}{cfgArgs})";
        if (!pic.IsFloatEdited) return EditFormat(alignedFixed, alignedScale, Emit.EmitText.CsLiteral(pic.EditMask!), cfgArgs);
        string mask = Emit.EmitText.CsLiteral(pic.EditMask!);
        return value.Real || value.Dec
            ? $"{nameof(CobolEdit)}.{nameof(CobolEdit.FormatFloatMove)}({value.Expr}, {mask}{cfgArgs})"
            : $"{nameof(CobolEdit)}.{nameof(CobolEdit.FormatFloatMove)}({value.Expr}, {value.Scale}, {mask}{cfgArgs})";
    }

    /// <summary>A locale-name reference rendered for a runtime call: the L1-normalized tag as a string literal,
    /// or <c>null</c> for the current-locale form (the runtime resolves the category's current locale at use —
    /// §13.18.40.5 r11 / §14.6.6 r6). The ONE renderer of a <see cref="Binding.Model.LocaleRef"/> argument.</summary>
    public static string LocaleTagArg(Binding.Model.LocaleRef locale) =>
        locale.Tag is { } t ? Emit.EmitText.CsLiteral(t) : "null";

    /// <summary>The format-2 (LOCALE) receiver's ARITHMETIC store (§14.7.5 — false = the size error condition,
    /// receiver unchanged; the capacity is the picture's integer digit positions, DISTINCT from EC-LOCALE-SIZE,
    /// which is §13.18.40.5 r14 b's character-truncation condition inside the edit itself):
    /// <c>CobolLocaleEdit.TryFormat</c>. The EditTryFormatFloat shape.</summary>
    public static string EditTryFormatLocale(PicInfo pic, string alignedFixed, string alignedScale, string imgVar, string cfgArgs)
    {
        var le = pic.LocaleEdit!;
        return $"{nameof(CobolLocaleEdit)}.{nameof(CobolLocaleEdit.TryFormat)}({alignedFixed}, {alignedScale}, "
            + $"{Emit.EmitText.CsLiteral(le.Picture)}, {LocaleTagArg(le.Locale)}, {le.Size}, out var {imgVar}{cfgArgs})";
    }

    /// <summary>A format-2 (LOCALE) sender's DE-EDIT read (§14.9.25.4 GR5/GR6 d over §14.6.13.2 r4) — the
    /// <c>CobolLocaleEdit.DeEdit</c> call under the locale current NOW; the scale is the picture's.</summary>
    public static string LocaleDeEdit(PicInfo pic, string read, bool blankWhenZero)
    {
        var le = pic.LocaleEdit!;
        return $"{nameof(CobolLocaleEdit)}.{nameof(CobolLocaleEdit.DeEdit)}({read}, {Emit.EmitText.CsLiteral(le.Picture)}, "
            + $"{LocaleTagArg(le.Locale)}{(blankWhenZero ? ", blankWhenZero: true" : "")})";
    }

    /// <summary>A format-2 (LOCALE) item's VALUE-clause initializer — a RUNTIME <c>CobolLocaleEdit.Format</c>
    /// call, never a baked image: §13.18.40.5 r11 + §14.6.6 r6 make the locale the one current AT THE TIME of
    /// editing, so no compile-time image exists. The ONE producer for the field initializer, the group-image
    /// composer and the level-88 membership value.</summary>
    public static string LocaleEditCompose(PicInfo pic, Int128 unscaled, int scale, bool blankWhenZero)
    {
        var le = pic.LocaleEdit!;
        return $"{nameof(CobolLocaleEdit)}.{nameof(CobolLocaleEdit.Format)}({Emit.EmitText.IntLiteral(unscaled.ToString())}, {scale}, "
            + $"{Emit.EmitText.CsLiteral(le.Picture)}, {LocaleTagArg(le.Locale)}, {le.Size}{(blankWhenZero ? ", blankWhenZero: true" : "")})";
    }

    /// <summary>The CORRECTLY-ROUNDED scaled-value→double conversion — <c>CobolFloat.ScaledToDouble</c> (kb/Work
    /// PB115; the ONE conversion <c>NumericRenderer.Real</c> emits for a scaled float-lane argument).</summary>
    public static string ScaledToDouble(string unscaled, int scale) =>
        $"{nameof(CobolFloat)}.{nameof(CobolFloat.ScaledToDouble)}({unscaled}, {scale})";

    /// <summary>The CORRECTLY-ROUNDED scaled-value→binary32 conversion, ONE rounding — <c>CobolFloat.ScaledToSingle</c>
    /// (kb/Work PB1110), the twin of <see cref="ScaledToDouble"/> for a binary32 receiver.</summary>
    public static string ScaledToSingle(string unscaled, int scale) =>
        $"{nameof(CobolFloat)}.{nameof(CobolFloat.ScaledToSingle)}({unscaled}, {scale})";

    /// <summary>The checked store of an already single-converted fixed-point MOVE sender —
    /// <c>CobolFloat.StoreScaledSingleChecked</c> (ISO §14.9.25.4 GR6 d)4.a; kb/Work PB1110).</summary>
    public static string FloatStoreScaledSingleChecked(string converted) =>
        $"{nameof(CobolFloat)}.{nameof(CobolFloat.StoreScaledSingleChecked)}({converted})";

    /// <summary>The floating-point form's ARITHMETIC store (§14.7.5 cases 3/4 — false = the size error condition,
    /// receiver unchanged): <c>CobolEdit.TryFormatFloat</c> over the result's exact form.</summary>
    public static string EditTryFormatFloat(PicInfo pic, Emit.NumX value, string imgVar, string cfgArgs)
    {
        string mask = Emit.EmitText.CsLiteral(pic.EditMask!);
        return value.Real || value.Dec
            ? $"{nameof(CobolEdit)}.{nameof(CobolEdit.TryFormatFloat)}({value.Expr}, {mask}, out var {imgVar}{cfgArgs})"
            : $"{nameof(CobolEdit)}.{nameof(CobolEdit.TryFormatFloat)}({value.Expr}, {value.Scale}, {mask}, out var {imgVar}{cfgArgs})";
    }

    /// <summary>A floating-point literal as the EXACT standard-decimal operand (ISO §8.8.1.5.2 r1 — the literal's
    /// value, significand × 10^exponent, lifted through the ONE range-checking funnel <c>CobolDec.FromParsed</c>; a
    /// 35/36-digit significand rounds to decimal128's 34 under the intermediate rounding mode). Under STANDARD-DECIMAL
    /// arithmetic this replaces the binary64 form a floating literal takes natively (D16), so a 20-digit significand or
    /// a 4-digit exponent reaches the intermediate exactly (kb/Work PB99).</summary>
    public static string DecFromParsedLiteral(Int128 sig, int exp10, string modeExpr) =>
        $"CobolDec.FromParsed({Emit.EmitText.IntLiteral(sig.ToString())}, {exp10}, {modeExpr})";

    /// <summary>The compile-time floating-point edited image of a VALUE literal (<c>CobolEdit.FormatFloatMove</c> at
    /// compile time — the same runtime, so the baked initial content is what a MOVE of the literal would store).</summary>
    public static string EditComposeFloat(Int128 sig, int exp10, string picture, bool blankWhenZero, bool commaMode,
        IReadOnlyList<CobolEdit.EditRule>? edits = null) =>
        CobolEdit.FormatFloatMove(new CobolDec(sig, exp10), picture, blankWhenZero, commaMode, edits?.ToArray());

    /// <summary>The trailing <c>edits:</c> named argument for a numeric-edited store carrying PICTURE EDITING
    /// phrases (ISO §13.18.40.2 Format 1) — the resolved render rules serialized as a
    /// <c>CobolEdit.EditRule[]</c>. Empty for every non-editing item, so the generated code of an ordinary program
    /// is byte-identical. ⛔ Edited stores do NOT append it themselves: <see cref="Emit.EmitContext.EditCfg"/>
    /// carries it, so every fixed- AND floating-point edited call site gets it from the one producer (kb/Work PB866).
    /// <para>literal-2 / literal-3 / literal-1 travel as STRING literals (§13.18.40.3 SR9 allows 50 characters)
    /// and the FLOATING flag travels beside them, because §13.18.40.5 rule 6 makes an extended editing sign
    /// control symbol a floating insertion symbol and only the binder saw the character-string that decides it
    /// (kb/Work PB491).</para></summary>
    public static string EditsArg(IReadOnlyList<CobolEdit.EditRule>? rules)
    {
        if (rules is null || rules.Count == 0) return "";
        static string Ch(char c) => Microsoft.CodeAnalysis.CSharp.SymbolDisplay.FormatLiteral(c, quote: true);
        static string Str(string s) => Microsoft.CodeAnalysis.CSharp.SymbolDisplay.FormatLiteral(s, quote: true);
        string items = string.Join(", ", rules.Select(r =>
            $"new {nameof(CobolEdit)}.{nameof(CobolEdit.EditRule)}({Ch(r.Char1)}, {Str(r.Neg)}, {Str(r.Pos)}, "
            + $"{(r.SimpleInsertion ? "true" : "false")}, {(r.Floating ? "true" : "false")})"));
        return $", edits: new {nameof(CobolEdit)}.{nameof(CobolEdit.EditRule)}[] {{ {items} }}";
    }

    /// <summary>Place sending characters into an ALPHANUMERIC-EDITED or NATIONAL-EDITED mask's positions
    /// (ISO §13.18.40.5 Table 7 — both categories' ONLY editing is SIMPLE INSERTION; rule 3 places the insertion
    /// character at the symbol's own position) — <c>CobolEdit.FormatSimpleInsertion</c>.
    /// <para>⛔ It takes the <see cref="PicInfo"/>, never a bare mask: the mask and the item's PICTURE EDITING
    /// rules are rendered TOGETHER from the one item, which is what keeps a new emit site from doing what all
    /// three existing ones did — pass the mask and drop <see cref="PicInfo.EditingRules"/>, so
    /// <c>PIC XXTXX EDITING "T" IS ":"</c> rendered the mask LETTER (kb/Work PB490; §13.18.40.4 GR7 names
    /// character-1 as an alphanumeric-edited constituent, and GR10 names it identically for national-edited).
    /// This is the character-category twin of <see cref="EditFormatFor"/>, which owns the numeric-edited form
    /// dispatch for the same reason. ⛔ ONE entry point for BOTH edited character categories, deliberately: an
    /// <c>…Alphanumeric</c> name is how the national arm came to have no renderer at all (kb/Work PB492).</para></summary>
    public static string EditFormatSimpleInsertion(string value, PicInfo pic) =>
        $"{nameof(CobolEdit)}.{nameof(CobolEdit.FormatSimpleInsertion)}({value}, "
        + $"{Emit.EmitText.CsLiteral(pic.EditMask!)}{EditsArg(pic.EditingRules)})";

    /// <summary>The §14.9.25.4 GR6 d) 3 read of an ALPHANUMERIC or NATIONAL sending operand in a numeric context —
    /// an unsigned integer over the operand's rightmost 31 character positions. <paramref name="sending"/> selects
    /// the MOVE-rules CHECKED form (<c>CobolNum.FromAlphanumericSending</c>, GR6 d) 1's EC-DATA-INCOMPATIBLE); the
    /// answer for a given reference is <c>SendingRef.AlphanumericChecked()</c>'s, never a call-site bool literal
    /// invented on the spot.
    /// <para>⛔ <paramref name="sending"/> HAS NO DEFAULT, deliberately — the same lesson
    /// <c>SendingRefRules.FloatChecked</c> was rewritten for: a default of <c>false</c> would let a call site
    /// added later inherit the UNCHECKED read by omission, which is how a raise the standard requires goes
    /// missing without anyone deciding it should. Every caller answers.</para>
    /// <para>⛔ NOT the decode of a NUMERIC item's own character image — that is <see cref="NumDigitMagnitude"/>,
    /// which applies no size rule (kb/Work PB426).</para></summary>
    public static string NumFromAlphanumeric(string image, bool sending) =>
        $"{nameof(CobolNum)}.{(sending ? nameof(CobolNum.FromAlphanumericSending) : nameof(CobolNum.FromAlphanumeric))}({image})";

    /// <summary>Decode a digit image's magnitude with NO size rule (non-digits contribute no digit, §14.6.13.2) —
    /// <c>CobolNum.DigitMagnitude</c>. For an image whose size its own data description already fixes: a numeric
    /// item's character image, or §14.9.25.4 GR6 d) 3 b)'s figurative replication across the RECEIVER's digit
    /// positions.</summary>
    public static string NumDigitMagnitude(string image) =>
        $"{nameof(CobolNum)}.{nameof(CobolNum.DigitMagnitude)}({image})";

    /// <summary>A signed numeric DISPLAY item's replaced digit image re-signed with the sign the item's CURRENT image
    /// carries, a zero magnitude included — <c>CobolNum.RetainSign</c> (ISO §14.9.22.4 GR4 d; kb/Work PB1128).
    /// <paramref name="digits"/> is the replaced digit run, <paramref name="current"/> the item's image before the
    /// statement.</summary>
    public static string NumRetainSign(string digits, string current, string profile) =>
        $"{nameof(CobolNum)}.{nameof(CobolNum.RetainSign)}({digits}, {current}, {profile})";

    /// <summary>Rescale an unscaled value between fraction scales under a rounding mode — <c>CobolNum.Rescale</c>,
    /// or the size-error-latching <c>CobolNum.RescaleChecked</c> when <paramref name="checkedPath"/>.</summary>
    /// <summary>The §15.3 integer-argument landing (PB22) — <c>CobolIntrinsics.IntegerArg</c>, which RAISES on a
    /// value outside the <c>long</c> range instead of letting an unchecked cast wrap it past the function's own
    /// range guard. <paramref name="real"/> selects the double-typed twin (distinct name, not an overload — an
    /// integer literal converts to both carriers and would be a CS0121 ambiguity).</summary>
    public static string IntegerArg(string value) =>
        $"{nameof(CobolIntrinsics)}.{nameof(CobolIntrinsics.IntegerArg)}({value})";

    /// <summary>The §15.3 type-6 intake for a value on ONE of the operand carriers, keyed by carrier KIND — the ONE
    /// family of landings the renderer's bounded (<c>long</c>) and total (<c>Int128</c>) integer arguments share,
    /// so the integrality rule (kb/Work PB1526) is asked on every carrier and a new carrier is a compile error in
    /// the one caller, not a missed arm. <paramref name="wide"/> is the body's declared <c>Int128</c> carrier
    /// (kb/Work PB254); a scale-0 fixed-point operand is integral by construction, so the bounded lane narrows
    /// it with <c>IntegerArg</c> and the wide lane passes it through raw.</summary>
    public static string IntegerArgOf(IntegerArgCarrier carrier, string value, int scale, bool wide) => carrier switch
    {
        IntegerArgCarrier.Real => $"{nameof(CobolIntrinsics)}.{(wide ? nameof(CobolIntrinsics.IntegerArgWideReal) : nameof(CobolIntrinsics.IntegerArgReal))}({value})",
        IntegerArgCarrier.Dec => $"{nameof(CobolIntrinsics)}.{(wide ? nameof(CobolIntrinsics.IntegerArgWideDec) : nameof(CobolIntrinsics.IntegerArgDec))}({value})",
        IntegerArgCarrier.Scaled when scale != 0 =>
            $"{nameof(CobolIntrinsics)}.{(wide ? nameof(CobolIntrinsics.IntegerArgWideScaled) : nameof(CobolIntrinsics.IntegerArgScaled))}({value}, {scale})",
        IntegerArgCarrier.Scaled => wide ? value : IntegerArg(value),
        _ => throw new ArgumentOutOfRangeException(nameof(carrier)),
    };

    /// <summary>The position intake for a subscript or reference-modifier bound that is an arithmetic EXPRESSION —
    /// <c>CobolTable.OccValue*</c> / <c>CobolString.RefModValue*</c> over the value on its OWN carrier, so
    /// §8.4.2.3.4 GR1b / §8.4.3.3.4 rule 5)c)'s integrality test reads the exact intermediate (kb/Work PB1890).
    /// Keyed by the same carrier kinds as <see cref="IntegerArgOf"/>; the result is the saturated <c>long</c>
    /// position.</summary>
    public static string PositionValueOf(IntegerArgCarrier carrier, string value, int scale, bool refMod) =>
        (carrier, refMod) switch
        {
            (IntegerArgCarrier.Real, false) => $"{nameof(CobolTable)}.{nameof(CobolTable.OccValueReal)}({value})",
            (IntegerArgCarrier.Dec, false) => $"{nameof(CobolTable)}.{nameof(CobolTable.OccValueDec)}({value})",
            (IntegerArgCarrier.Scaled, false) => $"{nameof(CobolTable)}.{nameof(CobolTable.OccValue)}({value}, {scale})",
            (IntegerArgCarrier.Real, true) => $"{nameof(CobolString)}.{nameof(CobolString.RefModValueReal)}({value})",
            (IntegerArgCarrier.Dec, true) => $"{nameof(CobolString)}.{nameof(CobolString.RefModValueDec)}({value})",
            (IntegerArgCarrier.Scaled, true) => $"{nameof(CobolString)}.{nameof(CobolString.RefModValue)}({value}, {scale})",
            _ => throw new ArgumentOutOfRangeException(nameof(carrier)),
        };

    /// <summary>The operand carriers an integer-argument intake is total over (<see cref="IntegerArgOf"/>): the
    /// exact scaled <c>Int128</c>, the SDIDI, and binary64 — the unsigned-wide lane funnels into the first through
    /// <c>NumericRenderer.DeU</c> before it reaches the intake.</summary>
    public enum IntegerArgCarrier { Scaled, Dec, Real }

    /// <summary>The runtime scale-37 codomain-maximum constant for a bounded float-family function (PB65 /
    /// RV-15.75.4-1) — consumed by <c>CobolIntrinsics.FromDoubleBounded</c>'s clamp.</summary>
    public static string CodomainConst(CobolNet.Binding.IntrinsicCodomain c) => c switch
    {
        CobolNet.Binding.IntrinsicCodomain.UnitOpen => $"{nameof(CobolIntrinsics)}.{nameof(CobolIntrinsics.CodomainBelowOne37)}",
        CobolNet.Binding.IntrinsicCodomain.HalfPi => $"{nameof(CobolIntrinsics)}.{nameof(CobolIntrinsics.CodomainHalfPi37)}",
        _ => $"{nameof(CobolIntrinsics)}.{nameof(CobolIntrinsics.CodomainPi37)}",
    };

    /// <summary>⛔ THE argument-1 domain screen (ISO §15.3 rule 14; kb/Work PB952) — <c>CobolIntrinsics.DomainScaled</c>
    /// / <c>DomainDec</c> / <c>DomainReal</c> over the operand on its OWN carrier, returning the binary64 the body
    /// computes on. The carrier picks the overload by NAME (an integer expression converts to Int128 and to double
    /// alike — CS0121): the exact scaled integer at its scale, the SDIDI, or a binary64 that is already the value.
    /// A UInt128 operand (a 16-byte unsigned COMP-5, scale 0) is its own double's integer, which no correctly-rounded
    /// conversion moves across −1, 0 or +1, so it takes the binary64 arm.</summary>
    public static string DomainArg(Emit.NumX x, CobolNet.Binding.IntrinsicDomain domain, string function, string rule)
    {
        string tail = DomainTail(domain, function, rule);
        return x.Dec ? $"{nameof(CobolIntrinsics)}.{nameof(CobolIntrinsics.DomainDec)}({x.Expr}, {tail})"
            : x.Real || x.U ? $"{nameof(CobolIntrinsics)}.{nameof(CobolIntrinsics.DomainReal)}({Emit.NumericRenderer.Real(x)}, {tail})"
            : $"{nameof(CobolIntrinsics)}.{nameof(CobolIntrinsics.DomainScaled)}((Int128)({x.Expr}), {x.Scale}, {tail})";
    }

    /// <summary>The SAME screen over an SDIDI operand whose body takes the carrier UNNARROWED
    /// (<c>IntrinsicRenderer.WholeRangeBodies</c>; kb/Work PB999) — <c>CobolIntrinsics.DomainDecAdmitted</c>, which
    /// shares <c>DomainDec</c>'s predicate and returns the admitted <c>CobolDec?</c> (null = rejected) instead of
    /// its binary64.</summary>
    public static string DomainArgAdmitted(Emit.NumX x, CobolNet.Binding.IntrinsicDomain domain, string function, string rule) =>
        x.Dec
            ? $"{nameof(CobolIntrinsics)}.{nameof(CobolIntrinsics.DomainDecAdmitted)}({x.Expr}, {DomainTail(domain, function, rule)})"
            : throw new InvalidOperationException("DomainArgAdmitted screens an SDIDI operand only");

    private static string DomainTail(CobolNet.Binding.IntrinsicDomain domain, string function, string rule) =>
        $"{nameof(CobolIntrinsics)}.{nameof(CobolIntrinsics.ArgumentDomain)}.{domain}, "
        + $"{Emit.EmitText.CsLiteral(function)}, {Emit.EmitText.CsLiteral(rule)}";

    /// <summary>A VALUE-SEMANTICS rescale — <c>CobolNum.Rescale</c>. ⛔ Every render of this today NARROWS to
    /// scale 0 (an integer intrinsic argument, a LINAGE line number, an unstringing pointer), where the plain
    /// rescale is exact. It is NOT a landing into a receiver: a receiver-bound alignment takes
    /// <see cref="NumRescaleStore"/>, whose two forms are the ones DOC-A.1-70 and §14.7.5 name (kb/Work PB639 —
    /// this helper carried a <c>checkedPath = false</c> DEFAULT, and the arithmetic store's edited landing took
    /// it, so a widening past the carrier wrapped in binary).</summary>
    public static string NumRescale(string value, string fromScale, string toScale, CobolRounding mode) =>
        $"{nameof(CobolNum)}.{nameof(CobolNum.Rescale)}({value}, {fromScale}, {toScale}, {RoundingText(mode)})";

    /// <summary>The receiver-bound STORE alignment, in the landing's own form and with NO default — the caller
    /// says which store it is, exactly as <see cref="FloatToScaled"/> does for the float carrier (kb/Work PB77).
    /// The UNCHECKED landing is <c>CobolNum.RescaleStoreCap</c>: the LOW-ORDER digits of the result aligned at
    /// the receiver's scale, which is the determination <c>CONFORMANCE.md</c> DOC-A.1-70 documents for the
    /// §14.7.5 no-phrase rule-4 size error (§14.6.13.1.3 item 8). The CHECKED landing is
    /// <c>CobolNum.RescaleChecked</c>, which raises the §14.7.5 case-3 size error for the statement's
    /// ON SIZE ERROR / EC-SIZE machinery so storing rule 2 leaves the receiver unchanged.</summary>
    public static string NumRescaleStore(string value, string fromScale, string toScale, CobolRounding mode, bool checkedPath) =>
        $"{nameof(CobolNum)}.{(checkedPath ? nameof(CobolNum.RescaleChecked) : nameof(CobolNum.RescaleStoreCap))}({value}, {fromScale}, {toScale}, {RoundingText(mode)})";

    /// <summary>The §14.9.12 GR6c/GR7 scaled division — <c>CobolNum.Divide</c>, or the size-error-throwing
    /// <c>CobolNum.DivideOrThrow</c> under a checked context.</summary>
    public static string NumDivide(bool orThrow, string a, string aScale, string b, string bScale, string resultScale, CobolRounding mode) =>
        $"{nameof(CobolNum)}.{(orThrow ? nameof(CobolNum.DivideOrThrow) : nameof(CobolNum.Divide))}({a}, {aScale}, {b}, {bScale}, {resultScale}, {RoundingText(mode)})";

    /// <summary>The checked numeric store — <c>CobolNum.TryStore</c> (false = capacity/PROHIBITED failure; the
    /// receiver stays unchanged, §14.7.5). <paramref name="argsFragment"/> is the pre-shaped value/scale/profile
    /// argument run (fixed, Real-landed, or SDIDI overload); <paramref name="u"/> picks the unsigned-wide
    /// <c>TryStoreU</c> lane by NAME (see <see cref="NumStore"/>).</summary>
    public static string NumTryStore(string argsFragment, CobolRounding mode, string outVar, bool u = false) =>
        $"{nameof(CobolNum)}.{(u ? nameof(CobolNum.TryStoreU) : nameof(CobolNum.TryStore))}({argsFragment}, {RoundingText(mode)}, out var {outVar})";

    /// <summary>The unchecked rounded store — the <c>CobolNum.Store</c> overload taking a rounding mode
    /// (<c>StoreU</c> on the unsigned-wide lane — see <see cref="NumStore"/>).</summary>
    public static string NumStoreRounded(string argsFragment, CobolRounding mode, bool u = false) =>
        $"{nameof(CobolNum)}.{(u ? nameof(CobolNum.StoreU) : nameof(CobolNum.Store))}({argsFragment}, {RoundingText(mode)})";

    /// <summary>The RAISING store — <c>CobolNum.StoreOrRaise</c> (<c>StoreUOrRaise</c> on the unsigned-wide
    /// lane, by NAME). The EXPRESSION-position sibling of <see cref="NumTryStore"/>, for a §14.7.5 case-3
    /// overflow at a store that has no ON SIZE ERROR flag to latch because it is not inside an arithmetic
    /// statement: the §14.2.3 GR9/GR10 argument crossings of a CALL and an INVOKE (kb/Work PB640).</summary>
    public static string NumStoreOrRaise(string argsFragment, CobolRounding mode, bool u = false) =>
        $"{nameof(CobolNum)}.{(u ? nameof(CobolNum.StoreUOrRaise) : nameof(CobolNum.StoreOrRaise))}"
        + $"({argsFragment}, {RoundingText(mode)})";

    /// <summary>The unsigned-wide → Int128 funnel — <c>CobolNum.Widen</c> (kb/Work R10: loud beyond the
    /// documented native intermediate, never a silent wrap).</summary>
    public static string NumWiden(string value) =>
        $"{nameof(CobolNum)}.{nameof(CobolNum.Widen)}({value})";

    /// <summary>An algebraic-value comparison with an unsigned-wide side — <c>CobolNum.CompareU</c> (kb/Work
    /// R10). The overload set covers U-vs-U and either mixed order; operands are passed with their own scales.</summary>
    public static string NumCompareU(string a, string aScale, string b, string bScale) =>
        $"{nameof(CobolNum)}.{nameof(CobolNum.CompareU)}({a}, {aScale}, {b}, {bScale})";

    /// <summary>The Int128-lane exact non-widening comparison at each operand's own scale —
    /// <c>CobolNum.Compare</c> (fix-queue PB65: the common-scale alignment wrapped at 39 aligned digits).</summary>
    public static string NumCompareScaled(string a, string aScale, string b, string bScale) =>
        $"{nameof(CobolNum)}.{nameof(CobolNum.Compare)}({a}, {aScale}, {b}, {bScale})";

    /// <summary>The escape-CHECKED widening rescale — <c>CobolNum.RescaleEscape</c> (fix-queue PB65: a
    /// value-semantics alignment past the Int128 intermediate is the size-error condition, never a wrap).</summary>
    public static string NumRescaleEscape(string value, string fromScale, string toScale, CobolRounding mode) =>
        $"{nameof(CobolNum)}.{nameof(CobolNum.RescaleEscape)}({value}, {fromScale}, {toScale}, {RoundingText(mode)})";

    /// <summary>An SDIDI intermediate landed to an unscaled value — the instance <c>CobolDec.ToUnscaled</c> (the
    /// UNCHECKED §14.7 final transfer: MOVE, alignment, an arithmetic store with no size-error checking).</summary>
    public static string DecToUnscaled(string decExpr, string scale, CobolRounding mode) =>
        $"({decExpr}).{nameof(CobolDec.ToUnscaled)}({scale}, {RoundingText(mode)})";

    /// <summary>The SIZE-ERROR-CHECKED sibling — <c>CobolDec.ToUnscaledChecked</c> (kb/Work PB74): a magnitude past
    /// the Int128 carrier raises <c>CobolSizeError</c> EC-SIZE-TRUNCATION for the statement's ON SIZE ERROR /
    /// EC-SIZE machinery instead of returning the low-order digits. The checked numeric-EDITED transfer in
    /// <c>ArithmeticEmitter.StoreArith</c> rides this; the numeric receiver's <c>TryStore(CobolDec)</c> calls it directly.</summary>
    public static string DecToUnscaledChecked(string decExpr, string scale, CobolRounding mode) =>
        $"({decExpr}).{nameof(CobolDec.ToUnscaledChecked)}({scale}, {RoundingText(mode)})";

    /// <summary>The INTERMEDIATE landing — <c>CobolDec.ToUnscaledIntermediate</c> (kb/Work PB69): an SDIDI value
    /// entering the Int128 carrier as an argument, an arithmetic operand, a subscript … A magnitude the carrier
    /// cannot hold at the scale raises EC-SIZE-OVERFLOW (§14.7.5 case 5 — the implementor-defined intermediate
    /// range IS checked, A.1 item 179), never the modular low-order digits.</summary>
    /// <summary>The algebraic sign of an SDIDI intermediate as an <c>int</c> (−1/0/+1) — <c>Int128.Sign</c> over the
    /// significand, which carries the value's sign exactly at every exponent (a sign condition over a
    /// STANDARD-DECIMAL expression, or over a native integer power — kb/Work PB84, NIST NC250A).</summary>
    /// <summary>Is a data-address / pointer expression the NULL pointer? The runtime's null is the
    /// <c>ManagedPointer.Null</c> SINGLETON, never a C# null — a bare <c>is null</c> test is always false on it (kb/Work
    /// PB80: the LENGTH r4a association guard read an unallocated BASED entry as associated).</summary>
    public static string PtrIsNull(string ptrExpr) => $"({ptrExpr} is null || {ptrExpr}.{nameof(ManagedPointer.IsNull)})";

    public static string DecSign(string decExpr) =>
        $"{nameof(Int128)}.{nameof(Int128.Sign)}(({decExpr}).{nameof(CobolDec.Sig)})";

    public static string DecToUnscaledIntermediate(string decExpr, string scale, CobolRounding mode) =>
        $"({decExpr}).{nameof(CobolDec.ToUnscaledIntermediate)}({scale}, {RoundingText(mode)})";

    /// <summary>SDIDI exponentiation (ISO §8.8.1.5.4; P10 Step 12) — <c>CobolDec.Pow</c>. <paramref name="mode"/>
    /// is the pre-rendered INTERMEDIATE ROUNDING fragment (<c>CobolRounding.X</c>).</summary>
    public static string DecPow(string baseOperand, string expOperand, string mode) =>
        $"{nameof(CobolDec)}.{nameof(CobolDec.Pow)}({baseOperand}, {expOperand}, {mode})";

    /// <summary>A NATIVE product past the Int128 carrier formed on the SDIDI — <c>CobolDec.MulToOdd</c> (kb/Work PB1143):
    /// the exact product reduced to 34 digits by round-to-odd, so the receiver's one rounding sees any tail.</summary>
    public static string DecMulToOdd(string leftOperand, string rightOperand) =>
        $"{nameof(CobolDec)}.{nameof(CobolDec.MulToOdd)}({leftOperand}, {rightOperand})";

    /// <summary>The FINAL TRANSFER of a native product past the Int128 carrier — <c>CobolDec.MulAtScale</c> (kb/Work PB1143,
    /// review finding N1): the exact product rounded once to <paramref name="resultScale"/> with <paramref name="mode"/>,
    /// as an unscaled Int128 at that scale. <paramref name="checkedTransfer"/> is the statement's ON SIZE ERROR /
    /// EC-SIZE checking (PROHIBITED raises only when it is on).</summary>
    public static string DecMulAtScale(string left, int leftScale, string right, int rightScale, int resultScale,
                                       CobolRounding mode, bool checkedTransfer) =>
        $"{nameof(CobolDec)}.{nameof(CobolDec.MulAtScale)}({left}, {leftScale}, {right}, {rightScale}, {resultScale}, "
        + $"{RoundingText(mode)}, {(checkedTransfer ? "true" : "false")})";

    /// <summary>A native quotient formed on the SDIDI — <c>CobolDec.DivToOdd</c>: the quotient reduced to 34 digits by
    /// round-to-odd, so the receiver's one rounding sees any tail (kb/Work PB1143's sibling, as <see cref="DecMulToOdd"/>).</summary>
    public static string DecDivToOdd(string leftOperand, string rightOperand) =>
        $"{nameof(CobolDec)}.{nameof(CobolDec.DivToOdd)}({leftOperand}, {rightOperand})";

    // ── The EXACT WIDE intermediate (CobolWide, kb/Work PB1900) — one spelling per operation, so the renderer never
    //    writes the runtime type's member names out for itself.

    /// <summary>The widest magnitude, in decimal digits, the exact wide form holds (<c>CobolWide.MaxDigits</c>): the bound the
    /// renderer's digit analysis selects the form against.</summary>
    public const int WideMaxDigits = CobolWide.MaxDigits;

    /// <summary>An <c>Int128</c>-lane value lifted into the exact wide form (same unscaled magnitude; the caller tracks the scale).</summary>
    public static string WideFrom(string int128Expr) => $"{nameof(CobolWide)}.{nameof(CobolWide.From)}({int128Expr})";

    /// <summary>The exact product of two wide values (the caller adds the scales).</summary>
    public static string WideMul(string left, string right) =>
        $"{nameof(CobolWide)}.{nameof(CobolWide.Mul)}({left}, {right})";

    /// <summary>A wide value aligned UP by <paramref name="digits"/> decimal places (× 10^digits).</summary>
    public static string WideUp(string wide, int digits) =>
        digits == 0 ? wide : $"{nameof(CobolWide)}.{nameof(CobolWide.Up)}({wide}, {digits})";

    /// <summary>The exact sum (<paramref name="subtract"/> false) or difference of two wide values at one scale.</summary>
    public static string WideAdditive(string left, bool subtract, string right) =>
        $"{nameof(CobolWide)}.{(subtract ? nameof(CobolWide.Sub) : nameof(CobolWide.Add))}({left}, {right})";

    public static string WideNegate(string wide) => $"{nameof(CobolWide)}.{nameof(CobolWide.Negate)}({wide})";

    /// <summary>The exact comparison (−1/0/+1) of two scaled wide values.</summary>
    public static string WideCompare(string left, int leftScale, string right, int rightScale) =>
        $"{nameof(CobolWide)}.{nameof(CobolWide.Compare)}({left}, {leftScale}, {right}, {rightScale})";

    /// <summary>The FINAL TRANSFER of an exact wide value: rounded once from <paramref name="scale"/> to
    /// <paramref name="toScale"/> with <paramref name="mode"/>, as an unscaled Int128 at <paramref name="toScale"/>
    /// (<c>CobolWide.ToUnscaled</c>; <paramref name="checkedTransfer"/> as <see cref="DecMulAtScale"/>).</summary>
    public static string WideToUnscaled(string wide, int scale, int toScale, CobolRounding mode, bool checkedTransfer) =>
        $"({wide}).{nameof(CobolWide.ToUnscaled)}({scale}, {toScale}, {RoundingText(mode)}, {(checkedTransfer ? "true" : "false")})";

    /// <summary>A wide value lowered to the SDIDI by round-to-odd (<c>CobolWide.ToDec</c>).</summary>
    public static string WideToDec(string wide, int scale) => $"({wide}).{nameof(CobolWide.ToDec)}({scale})";

    /// <summary>A wide value as binary64 (<c>CobolWide.ToDouble</c>).</summary>
    public static string WideToDouble(string wide, int scale) => $"({wide}).{nameof(CobolWide.ToDouble)}({scale})";

    /// <summary>The exact §15.27.3 r3 FUNCTION E constant under a standard mode — <c>CobolDec.E</c> (kb/Work R18).</summary>
    public static string DecE => $"{nameof(CobolDec)}.{nameof(CobolDec.E)}";

    /// <summary>The exact §15.73.3 r3 FUNCTION PI constant under a standard mode — <c>CobolDec.Pi</c> (kb/Work R18).</summary>
    public static string DecPi => $"{nameof(CobolDec)}.{nameof(CobolDec.Pi)}";

    /// <summary>An exact fixed-point value lifted into SDIDI form — <c>CobolDec.From(unscaled, scale)</c>.</summary>
    public static string DecFrom(string unscaled, string scale) =>
        $"{nameof(CobolDec)}.{nameof(CobolDec.From)}({unscaled}, {scale})";

    /// <summary>The §8.8.1.5.1 implementor-defined float→SDIDI operand conversion — <c>CobolDec.FromDouble</c>
    /// (the shortest round-trip decimal identity of the IEEE value; P10 Step 12).</summary>
    public static string DecFromDouble(string doubleExpr) =>
        $"{nameof(CobolDec)}.{nameof(CobolDec.FromDouble)}({doubleExpr})";

    /// <summary>One SDIDI division of two exactly-lifted fixed-point values — MEAN's §15.60.4 equivalent-expression
    /// division under standard-decimal arithmetic (§15.4.1 r1; P10 Step 12).</summary>
    public static string DecDivLifted(string numerator, string numeratorScale, string denominator, string mode) =>
        $"{nameof(CobolDec)}.{nameof(CobolDec.Div)}({nameof(CobolDec)}.{nameof(CobolDec.From)}({numerator}, "
        + $"{numeratorScale}), {nameof(CobolDec)}.{nameof(CobolDec.From)}({denominator}, 0), {mode})";

    /// <summary>A float value's inexactness probe at a fraction scale (the ROUNDED PROHIBITED gate, §14.7.4.3
    /// item 7) — <c>CobolFloat.InexactAtScale</c>.</summary>
    public static string FloatInexactAtScale(string value, string scale) =>
        $"{nameof(CobolFloat)}.{nameof(CobolFloat.InexactAtScale)}({value}, {scale})";

    /// <summary>The capacity-checked edited format — <c>CobolEdit.TryFormat</c> (false = the aligned value's
    /// significant digits exceed the mask, §14.7.5 case 3).</summary>
    public static string EditTryFormat(string value, string scale, string maskLiteral, string imgVar, string cfgArgs) =>
        $"{nameof(CobolEdit)}.{nameof(CobolEdit.TryFormat)}({value}, {scale}, {maskLiteral}, out var {imgVar}{cfgArgs})";

    /// <summary>A boolean ITEM operand of an item-width-carrying expression (ISO §14.9.8.4 GR3; kb/Work PB589) —
    /// <c>CobolBool.Item(v)</c>: it contributes its own run-time positions to the width.</summary>
    public static string BoolItem(string value) => $"{nameof(CobolBool)}.{nameof(CobolBool.Item)}({value})";

    /// <summary>A boolean LITERAL / figurative operand of an item-width-carrying expression — <c>CobolBool.Literal(v)</c>:
    /// positions in the value, none in the GR3 width.</summary>
    public static string BoolLiteral(string value) => $"{nameof(CobolBool)}.{nameof(CobolBool.Literal)}({value})";

    /// <summary>The §14.9.8.4 GR3 value of an item-width-carrying expression — <c>CobolBool.ToItemWidth(v)</c>.</summary>
    public static string BoolToItemWidth(string sized) => $"{nameof(CobolBool)}.{nameof(CobolBool.ToItemWidth)}({sized})";

    /// <summary>Alphanumeric/national THROUGH-range membership under the effective collating sequence —
    /// <c>CobolString.ThruMember(read, lo, hi{collate})</c>: sets the nonfatal EC-RANGE-INVALID and returns false when
    /// <c>lo</c> collates after <c>hi</c> (§14.7.8 rule 2), else the inclusive bound test. <paramref name="collate"/>
    /// is the trailing collating-arg fragment (empty for the default, <c>, __COLLATE</c> / <c>, __COLLATE_NAT</c>
    /// otherwise) selecting the matching <c>Compare</c> overload.</summary>
    public static string ThruMember(string read, string lo, string hi, string collate) =>
        $"{nameof(CobolString)}.{nameof(CobolString.ThruMember)}({read}, {lo}, {hi}{collate})";

    /// <summary>The same carrier when one or both range ends is a FIGURATIVE SEED —
    /// <c>CobolString.ThruMemberFig(read, lo, hi, loFig:, hiFig:{collate})</c>, which sizes each figurative end to
    /// <c>read</c>'s own runtime character-position count (ISO §8.3.3.6.4 GR2) before §14.7.8 rule 2's inversion
    /// test. A SEPARATE runtime name for the same reason <c>CompareFig</c> is one beside <c>Compare</c> — the
    /// collating overload set stays collapsed to its two sequence channels. The flags are named so the generated
    /// text says which end carries the seed (kb/Work PB401).</summary>
    public static string ThruMemberFig(string read, string lo, string hi, bool loFig, bool hiFig, string collate) =>
        $"{nameof(CobolString)}.{nameof(CobolString.ThruMemberFig)}({read}, {lo}, {hi}, "
        + $"loFig: {(loFig ? "true" : "false")}, hiFig: {(hiFig ? "true" : "false")}{collate})";

    /// <summary>The emitted-text reference to a <see cref="CobolRounding"/> value — <c>nameof</c>-anchored so a
    /// member rename breaks HERE, never the generated text.</summary>
    public static string RoundingText(CobolRounding mode) => $"{nameof(CobolRounding)}.{mode}";

    /// <summary>A floating-point intermediate landed to a scaled integer at a target fraction scale — the CHECKED
    /// <c>CobolFloat.ToScaled</c> (saturates past the carrier so a capacity check raises — an ON SIZE ERROR / EC-SIZE
    /// store, an intermediate consumer) or the UNCHECKED <c>CobolFloat.ToScaledUnchecked</c> (the low-order digits —
    /// a MOVE, §14.6.8.2 r4; the no-phrase store; INVOKE BY CONTENT). The caller names the LANDING (kb/Work PB77) —
    /// there is no default, so a new site has to say which store it is.</summary>
    public static string FloatToScaled(string value, string scale, CobolRounding mode, bool checkedLanding) =>
        $"{nameof(CobolFloat)}.{(checkedLanding ? nameof(CobolFloat.ToScaled) : nameof(CobolFloat.ToScaledUnchecked))}({value}, {scale}, {RoundingText(mode)})";

    /// <summary>An arithmetic value transferred into a FLOATING-POINT resultant identifier under the receiver's
    /// §14.7.4.3 mode — <c>FloatResultant</c>'s entry for the value's carrier (binary64, SDIDI, scaled signed or
    /// unsigned-wide). With <paramref name="tryOut"/> it is the CHECKED form, a <c>bool</c> (false = the size error
    /// condition: past the format's range, or PROHIBITED and inexact) that declares the landed binary64 as
    /// <paramref name="tryOut"/>; without it, the unchecked no-phrase landing's <c>double</c>. A binary32 receiver's
    /// landed value is exactly representable, so the caller's <c>(float)</c> cast is exact (kb/Work PB1196).</summary>
    public static string FloatResultantStore(Emit.NumX value, CobolRounding mode, bool single, string? tryOut = null)
    {
        var (method, args) = FloatResultantEntry(value);
        string tail = $"{RoundingText(mode)}, {(single ? "true" : "false")}";
        return tryOut is null
            ? $"{nameof(Runtime.FloatResultant)}.{method}({args}, {tail})"
            : $"{nameof(Runtime.FloatResultant)}.Try{method}({args}, {tail}, out double {tryOut})";
    }

    /// <summary>The RAISING form of <see cref="FloatResultantStore"/> — an EXPRESSION that lands the value and throws
    /// the EC-SIZE-TRUNCATION size error when it is further from zero than the float format permits (§14.7.5 case 3 +
    /// no-phrase rule 4): the store of an argument crossing that has no SIZE ERROR phrase to offer, compiled when
    /// EC-SIZE-TRUNCATION checking is enabled at the activating statement (kb/Work PB1114).</summary>
    public static string FloatResultantStoreOrRaise(Emit.NumX value, CobolRounding mode, bool single)
    {
        var (method, args) = FloatResultantEntry(value);
        return $"{nameof(Runtime.FloatResultant)}.{method}OrRaise({args}, {RoundingText(mode)}, {(single ? "true" : "false")})";
    }

    /// <summary>The <c>FloatResultant</c> entry (method stem and leading arguments) for a value's carrier — ONE choice
    /// for the checked, unchecked and raising renderings.</summary>
    private static (string Method, string Args) FloatResultantEntry(Emit.NumX value) => value switch
    {
        { Real: true } => (nameof(Runtime.FloatResultant.FromReal), value.Expr),
        { Dec: true } => (nameof(Runtime.FloatResultant.FromDec), value.Expr),
        { U: true } => (nameof(Runtime.FloatResultant.FromUnsignedScaled), $"(UInt128)({value.Expr}), {value.Scale}"),
        _ => (nameof(Runtime.FloatResultant.FromScaled), $"(Int128)({value.Expr}), {value.Scale}"),
    };

    /// <summary>The checked read of a standard-float SENDING operand — <c>CobolFloat.Sending(value)</c>: raises the
    /// fatal EC-DATA-NOT-FINITE for a NaN/±Infinity content under checking (ISO §14.6.13.2 item 3), else returns the
    /// value. Wrapped at both float read chokepoints (the numeric-value read and the string-image read); the exempt
    /// sites (class/sign condition, same-usage MOVE) emit the raw read instead.</summary>
    public static string FloatSending(string value) =>
        $"{nameof(CobolFloat)}.{nameof(CobolFloat.Sending)}({value})";

    /// <summary>A float value's DISPLAY image — <c>CobolFloat.Display(value)</c> (invariant-culture shortest
    /// round-trip, §14.9.11 GR1 implementor-defined).</summary>
    public static string FloatDisplay(string value) =>
        $"{nameof(CobolFloat)}.{nameof(CobolFloat.Display)}({value})";

    /// <summary>A float carrier's value FROM ITS INTERCHANGE BITS — <c>CobolFloat.FromBinary32Bits</c> /
    /// <c>FromBinary64Bits</c> over an unsigned hex literal (binary32 in the low 32 bits of <paramref name="bits"/>).
    /// The SET Format-15 canonical values (ISO §14.9.39.4 rules 33–35, <c>IeeeSpecials.Bits</c>) are spelled ONLY
    /// through this: an opaque runtime call, because an inline <c>BitConverter</c> reinterpretation of a constant
    /// is a JIT floating constant, and the JIT's binary64 constant quiets a binary32 signaling NaN (kb/Work
    /// PB961).</summary>
    public static string FloatFromBits(ulong bits, bool single) =>
        single
            ? $"{nameof(CobolFloat)}.{nameof(CobolFloat.FromBinary32Bits)}(0x{(uint)bits:X8}u)"
            : $"{nameof(CobolFloat)}.{nameof(CobolFloat.FromBinary64Bits)}(0x{bits:X16}UL)";

    /// <summary>The checked store of a MOVE algebraic value into a SINGLE-precision float receiver —
    /// <c>CobolFloat.StoreSingleChecked(src)</c>: raises the fatal EC-DATA-OVERFLOW when a finite source overflows to
    /// ±Infinity under checking (ISO §14.9.25.4 GR6 d)4.a), else returns the cast value.</summary>
    public static string FloatStoreSingleChecked(string src) =>
        $"{nameof(CobolFloat)}.{nameof(CobolFloat.StoreSingleChecked)}({src})";

    /// <summary>The checked store of a STANDARD-DECIMAL MOVE algebraic value into a float receiver —
    /// <c>CobolFloat.StoreChecked(dec, single)</c>. The SDIDI is handed over WHOLE, because the range test is on
    /// the algebraic value and a <c>ToDouble</c> would have already collapsed it to ±Infinity (ISO §14.9.25.4
    /// GR6 d)4.a; kb/Work PB271).</summary>
    public static string FloatStoreDecChecked(string dec, bool single) =>
        $"{nameof(CobolFloat)}.{nameof(CobolFloat.StoreChecked)}({dec}, {(single ? "true" : "false")})";

    /// <summary>The BOOLEAN-receiver store (§14.6.8.6 — boolean-ZERO pad, explicit justification) —
    /// <c>CobolString.Store</c> with <c>pad: '0'</c>.</summary>
    public static string StrStoreBoolean(string value, string width, bool justifiedRight) =>
        $"{nameof(CobolString)}.{nameof(CobolString.Store)}({value}, {width}, " +
        $"justifiedRight: {(justifiedRight ? "true" : "false")}, pad: '0')";

    // ── INSPECT (CobolInspect; ISO §14.9.22) ──

    /// <summary>The tallying pass — <c>CobolInspect.Tally</c>. Array-literal fragments are pre-rendered by the caller.</summary>
    public static string InspectTally(string image, string kinds, string pats, string befs, string afts, string backward) =>
        $"{nameof(CobolInspect)}.{nameof(CobolInspect.Tally)}({image}, new int[] {{ {kinds} }}, " +
        $"new string?[] {{ {pats} }}, new string?[] {{ {befs} }}, new string?[] {{ {afts} }}, {backward})";

    /// <summary>The replacing pass — <c>CobolInspect.Replace</c>. <paramref name="figs"/> is the comma-separated
    /// per-operand figurative-replacement flags, or null when no replacement is figurative (the runtime's default).</summary>
    public static string InspectReplace(string image, string kinds, string pats, string reps, string befs, string afts, string backward,
        string? figs = null) =>
        $"{nameof(CobolInspect)}.{nameof(CobolInspect.Replace)}({image}, new int[] {{ {kinds} }}, new string?[] {{ {pats} }}, " +
        $"new string?[] {{ {reps} }}, new string?[] {{ {befs} }}, new string?[] {{ {afts} }}, {backward}"
        + (figs is null ? ")" : $", new bool[] {{ {figs} }})");

    /// <summary>CONVERTING — <c>CobolInspect.Convert</c>. <paramref name="toFigurative"/> marks a figurative
    /// literal-5, which the runtime repeats to the from-set's size.</summary>
    public static string InspectConvert(string image, string from, string to, string before, string after, string backward,
        bool toFigurative = false) =>
        $"{nameof(CobolInspect)}.{nameof(CobolInspect.Convert)}({image}, {from}, {to}, {before}, {after}, {backward}"
        + (toFigurative ? ", toFigurative: true)" : ")");

    /// <summary>Compile-time anchor for the tally-kind discriminators the emitter selects.</summary>
    public static string InspectTallyKindText(Binding.Bound.InspectTallyKind k) => k switch
    {
        Binding.Bound.InspectTallyKind.All => $"{nameof(CobolInspect)}.{nameof(CobolInspect.TallyAll)}",
        Binding.Bound.InspectTallyKind.Leading => $"{nameof(CobolInspect)}.{nameof(CobolInspect.TallyLeading)}",
        _ => $"{nameof(CobolInspect)}.{nameof(CobolInspect.TallyCharacters)}",
    };

    /// <summary>Compile-time anchor for the replace-kind discriminators the emitter selects.</summary>
    public static string InspectReplaceKindText(Binding.Bound.InspectReplaceKind k) => k switch
    {
        Binding.Bound.InspectReplaceKind.All => $"{nameof(CobolInspect)}.{nameof(CobolInspect.ReplaceAll)}",
        Binding.Bound.InspectReplaceKind.First => $"{nameof(CobolInspect)}.{nameof(CobolInspect.ReplaceFirst)}",
        Binding.Bound.InspectReplaceKind.Leading => $"{nameof(CobolInspect)}.{nameof(CobolInspect.ReplaceLeading)}",
        _ => $"{nameof(CobolInspect)}.{nameof(CobolInspect.ReplaceCharacters)}",
    };

    // ── STRING / UNSTRING (CobolStringOps; ISO §14.9.43 / §14.9.48) ──

    /// <summary>One sending operand's transfer into the STRING working image — <c>CobolStringOps.StringTransfer</c>
    /// (advances the pointer, latches the overflow flag by ref).</summary>
    public static string StrTransfer(string acc, string src, string delim, string ptrVar, string ovfVar) =>
        $"{nameof(CobolStringOps)}.{nameof(CobolStringOps.StringTransfer)}({acc}, {src}, {delim}, ref {ptrVar}, ref {ovfVar})";

    /// <summary>One receiving area's extraction — <c>CobolStringOps.UnstringExtract</c> (out-vars for the examined
    /// field and the matched delimiter; −1 = not acted upon).</summary>
    public static string UnstringExtract(string src, string dels, string alls, string noDelimSize,
        string ptrVar, string fldVar, string dlmVar) =>
        $"{nameof(CobolStringOps)}.{nameof(CobolStringOps.UnstringExtract)}({src}, {dels}, {alls}, {noDelimSize}, " +
        $"ref {ptrVar}, out var {fldVar}, out var {dlmVar})";

    /// <summary>Which listed delimiter an examination matched — <c>CobolStringOps.MatchedDelimiterIndex</c> (−1 at the
    /// end of the sender); asked only where an identifier delimiter can be overwritten mid-statement (D-UNS2).</summary>
    public static string UnstringMatchedDelimiter(string dels, string dlmVar) =>
        $"{nameof(CobolStringOps)}.{nameof(CobolStringOps.MatchedDelimiterIndex)}({dels}, {dlmVar})";

    /// <summary>The JUSTIFIED-right alphanumeric store (§14.9.25.4 GR6c) — <c>CobolString.Store</c> with
    /// <c>justifiedRight: true</c>.</summary>
    public static string StrStoreJustified(string value, string width) =>
        $"{nameof(CobolString)}.{nameof(CobolString.Store)}({value}, {width}, justifiedRight: true)";

    /// <summary>The ONE alignment dispatch for a character store (§14.9.25.4 GR6/GR6c via §13.18.32): a
    /// JUSTIFIED receiver right-justifies (left space-fill / left truncation), otherwise left-justified with
    /// right space-fill / right truncation. Every emitter storing a character image into an
    /// alphanumeric/national receiver routes here — MOVE, STRING, the INVOKE formal copy-back, ACCEPT
    /// temporal (kb/Work PB139's one-rule-one-place extraction of six hand-rolled ternaries).</summary>
    public static string StrStoreAligned(string value, string width, bool justified) =>
        justified ? StrStoreJustified(value, width) : StrStore(value, width);

    // ── Pointers (CobolPtr; ISO §14.9.39 F7/F10, §14.9.3, §14.9.15) ──

    /// <summary>Displace a pointer by n character positions — <c>CobolPtr.UpBy</c> (GR18's null trap and GR20's
    /// implementor-range guard inside).</summary>
    public static string PtrUpBy(string ptr, string amount) =>
        $"{nameof(CobolPtr)}.{nameof(CobolPtr.UpBy)}({ptr}, {amount})";

    /// <summary>THE SET pointer UP/DOWN BY amount landing over an EXACT scaled fixed-point amount —
    /// <c>CobolPtr.UpByAmount</c>. The amount keeps its scale and its FULL magnitude all the way in, so GR19's
    /// integrality test sees the fraction and GR20's range test sees the magnitude that an emitter-side
    /// <c>(long)</c> narrowing wrapped away (kb/Work PB465).</summary>
    public static string PtrUpByAmount(string ptr, string scaled, string scale, bool down) =>
        $"{nameof(CobolPtr)}.{nameof(CobolPtr.UpByAmount)}({ptr}, {scaled}, {scale}, {(down ? "true" : "false")})";

    /// <summary>ALLOCATE — <c>CobolPtr.Allocate</c> over the FULL Int128 size (no emitter-side narrowing —
    /// the PB22 wrap family), with the GR6/GR8 fill character and the GR5 not-available out-flag.
    /// <paramref name="nullImageOffsets"/> are the based item's pointer-member positions, seeded with the NULL
    /// image (GR9; kb/Work PB1071) - empty for the CHARACTERS form.</summary>
    public static string PtrAllocate(string sizeInt128, string fillCharLiteral, string notAvailVar,
                                     IReadOnlyCollection<int>? nullImageOffsets = null) =>
        $"{nameof(CobolPtr)}.{nameof(CobolPtr.Allocate)}({sizeInt128}, {fillCharLiteral}, out {notAvailVar}"
        + (nullImageOffsets is { Count: > 0 } ? $", [{string.Join(", ", nullImageOffsets)}])" : ")");

    /// <summary>ALLOCATE with a native-float expression — <c>CobolPtr.AllocateReal</c> (GR1's round-UP on
    /// the double; kb/Work PB151).</summary>
    public static string PtrAllocateReal(string sizeDouble, string fillCharLiteral, string notAvailVar) =>
        $"{nameof(CobolPtr)}.{nameof(CobolPtr.AllocateReal)}({sizeDouble}, {fillCharLiteral}, out {notAvailVar})";

    /// <summary>The NATIVE-FLOAT lane of <see cref="PtrUpByAmount"/> — <c>CobolPtr.UpByAmountReal</c>, whose
    /// GR19 integrality test runs on the <c>double</c> itself (kb/Work PB151) and whose out-of-carrier leg is
    /// GR20's, not GR19's (kb/Work PB465).</summary>
    public static string PtrUpByAmountReal(string ptr, string amountDouble, bool down) =>
        $"{nameof(CobolPtr)}.{nameof(CobolPtr.UpByAmountReal)}({ptr}, {amountDouble}, {(down ? "true" : "false")})";

    /// <summary>FREE a pointer's cell — <c>CobolPtr.Free</c> (three-way per GR1; not-alloc out-flag).</summary>
    public static string PtrFree(string ptr, string notAllocVar) =>
        $"{nameof(CobolPtr)}.{nameof(CobolPtr.Free)}({ptr}, out {notAllocVar})";

    // ── Indexes (CobolIndex; ISO §14.9.39.4 GR2 a) 1., GR3, GR4 a), GR29 · §13.18.38.4 GR2 — kb/Work PB459) ──

    /// <summary>THE SET-family amount landing over an EXACT scaled fixed-point amount —
    /// <c>CobolIndex.TryAmount</c>. The amount keeps its scale all the way in, so the integrality test
    /// (GR2 a) 1. a / GR3 / GR29) sees the fraction an emitter-side <c>(long)</c> narrowing would have
    /// destroyed. <see langword="false"/> is "the execution of the SET statement is unsuccessful".</summary>
    public static string IndexTryAmount(string scaled, string scale, SetAmountRule rule, string detail, string outVar) =>
        $"{nameof(CobolIndex)}.{nameof(CobolIndex.TryAmount)}({scaled}, {scale}, "
        + $"{nameof(SetAmountRule)}.{rule}, {EmitText.CsLiteral(detail)}, out long {outVar})";

    /// <summary>The NATIVE-FLOAT lane of <see cref="IndexTryAmount"/> — <c>CobolIndex.TryAmountReal</c>, whose
    /// integrality test runs on the <c>double</c> itself (the <c>CobolPtr.UpByReal</c> shape, kb/Work PB151).</summary>
    public static string IndexTryAmountReal(string amountDouble, SetAmountRule rule, string detail, string outVar) =>
        $"{nameof(CobolIndex)}.{nameof(CobolIndex.TryAmountReal)}({amountDouble}, "
        + $"{nameof(SetAmountRule)}.{rule}, {EmitText.CsLiteral(detail)}, out long {outVar})";

    /// <summary>THE guarded index augment — <c>CobolIndex.Augment</c> (§14.9.39.4 GR4 a): a result outside the
    /// implementor index range raises EC-RANGE-INDEX and returns the index UNCHANGED.</summary>
    public static string IndexAugment(string index, string amount, bool down, string detail) =>
        $"{nameof(CobolIndex)}.{nameof(CobolIndex.Augment)}({index}, {amount}, {(down ? "true" : "false")}, "
        + $"{EmitText.CsLiteral(detail)})";

    // ── More strings / tables ──

    /// <summary>A reference-modification slice — <c>CobolString.RefMod</c> (1-based start, length).
    /// <paramref name="allowZeroLength"/> (the REF-MOD-ZERO-LENGTH directive, §7.3.23) emits the named argument only
    /// when true, so every existing site stays byte-identical.</summary>
    public static string StrRefMod(string s, string start, string len, bool allowZeroLength = false) =>
        $"{nameof(CobolString)}.{nameof(CobolString.RefMod)}({s}, {start}, {len}{(allowZeroLength ? ", allowZeroLength: true" : "")})";

    /// <summary>The checked ZERO-BASED offset of a reference modifier's leftmost position — <c>CobolString.RefModStartOffset</c>,
    /// for an operand that needs the position, not the slice (<c>ADDRESS OF identifier-1(leftmost:length)</c>, kb/Work
    /// PB1407). <paramref name="size"/> is the C# <c>int</c> expression of identifier-1's positions: a literal for a
    /// fixed item, the CURRENT extent for an occurs-depending group (kb/Work PB1969), or
    /// <see cref="UnknownRefModSize"/> where only the run-time knows them.</summary>
    public static string StrRefModStartOffset(string start, string len, string size, bool allowZeroLength = false) =>
        $"{nameof(CobolString)}.{nameof(CobolString.RefModStartOffset)}({start}, {len}, {size}"
        + $"{(allowZeroLength ? ", allowZeroLength: true" : "")})";

    /// <summary>The size expression of an item whose positions the bind cannot supply (an ANY LENGTH item): only the
    /// lower bound of its reference modifier is checked.</summary>
    public const string UnknownRefModSize = "int.MaxValue";

    /// <summary>The OMITTED-length ref-mod sentinel (<c>identifier(start:)</c> "to the end") as an emit expression —
    /// routed through the façade (the P7 Step 4b ratchet) so a rename of the runtime const breaks HERE at compile time.
    /// Distinct from −1 so a specified negative length raises EC-BOUND-REF-MOD (review C14).</summary>
    public static string OmittedRefModLength => $"{nameof(CobolString)}.{nameof(CobolString.OmittedRefModLength)}";

    /// <summary>⛔ THE ONE EMIT of a COBOL integer VALUE narrowed to a host <c>int</c> (kb/Work PB1033): the
    /// runtime's saturating <see cref="CobolNum.Position32"/>, never a C# <c>(int)</c> cast, which WRAPS a value
    /// past the carrier back into range. <paramref name="integerExpr"/> is an integer-valued expression — the
    /// integer landing <c>NumericRenderer.Align(…, 0)</c> or a scale-0 field read. An expression that is already
    /// an <c>int</c> literal is emitted as written.</summary>
    public static string HostInt32(string integerExpr) =>
        int.TryParse(integerExpr, System.Globalization.NumberStyles.AllowLeadingSign,
            System.Globalization.CultureInfo.InvariantCulture, out _)
            ? integerExpr
            : $"{nameof(CobolNum)}.{nameof(CobolNum.Position32)}({integerExpr})";

    /// <summary>The <see cref="HostInt32"/> twin for a host <c>long</c> — <see cref="CobolNum.Position(Int128)"/>,
    /// the runtime's ONE saturating narrowing to <c>long</c>.</summary>
    public static string HostInt64(string integerExpr) =>
        $"{nameof(CobolNum)}.{nameof(CobolNum.Position)}((Int128)({integerExpr}))";

    /// <summary>A COBOL integer LITERAL narrowed to a host <c>int</c> at COMPILE time, through the SAME
    /// <see cref="CobolNum.Position32"/> the run-time narrowing uses — a C# <c>(int)(4294967297)</c> is not a
    /// wrap but a backend error (CS0221), and a 20-digit literal is CS1021.</summary>
    public static string HostInt32Literal(string literal) =>
        CobolNum.Position32(CobolNum.IntegerLiteralValue(literal)).ToString(System.Globalization.CultureInfo.InvariantCulture);

    /// <summary>The <see cref="HostInt32Literal"/> twin for a host <c>long</c>.</summary>
    public static string HostInt64Literal(string literal) =>
        CobolNum.Position(CobolNum.IntegerLiteralValue(literal)).ToString(System.Globalization.CultureInfo.InvariantCulture) + "L";

    // The ref-mod positions are integer-valued COBOL expressions and the runtime takes `int`, so each is
    // narrowed at the call site through HostInt32; an omitted length renders the distinct sentinel above rather
    // than −1. Both rules live HERE and nowhere else: a ref-mod attaches to a storage PLACE (PlaceRenderer,
    // readable and writable), to a ref-modified FUNCTION RESULT (IntrinsicRenderer, read-only — §8.4.3.3.3 SR2)
    // and to a ref-modified ACCEPT receiver's transfer width (AcceptDisplayEmitter), and they must agree.

    /// <summary>The runtime <c>int</c> leftmost-position from the typed start position (§8.4.3.3.4 item 5b).
    /// A value past the carrier saturates, so it stays out of range and raises EC-BOUND-REF-MOD (item 5c) rather
    /// than wrapping onto a position inside the item (kb/Work PB1033).</summary>
    public static string RefModStart(Position start) => HostInt32(PositionRenderer.Render(start));

    /// <summary>The runtime <c>int</c> length from the typed length position, or the OMITTED sentinel when the
    /// "to the end" form was written (§8.4.3.3.4 item 5c). A specified length narrows through
    /// <see cref="CobolString.SpecifiedRefModLength"/>, which saturates like <see cref="HostInt32"/> but can never
    /// land on the omitted sentinel.</summary>
    public static string RefModLength(Position? length) =>
        length is null ? OmittedRefModLength
        : length.Int32Literal is >= 0
            ? PositionRenderer.Render(length)
            : $"{nameof(CobolString)}.{nameof(CobolString.SpecifiedRefModLength)}({PositionRenderer.Render(length)})";

    /// <summary>A reference-modification slice over an already-rendered VALUE, from the model's
    /// <see cref="RefModSpec"/>. The value form has no splice counterpart: §8.4.3.2.3 SR1 makes a
    /// function-identifier a non-receiving operand, so a ref-modified function result is read-only.</summary>
    public static string StrRefMod(string s, RefModSpec rm) =>
        StrRefMod(s, RefModStart(rm.Start), RefModLength(rm.Length), rm.AllowZeroLength);

    /// <summary>Splice <paramref name="rhs"/> into <paramref name="s"/> at a 1-based start/length, preserving the
    /// rest of the width — <c>CobolString.SpliceInto</c>. <paramref name="pad"/> is the optional fill-char argument
    /// (a C# <c>char</c> literal, e.g. boolean-zero <c>'0'</c>); null emits the default space fill.</summary>
    public static string StrSpliceInto(string s, string start, string len, string rhs, string? pad = null,
        bool allowZeroLength = false, bool repeat = false) =>
        $"{nameof(CobolString)}.{nameof(CobolString.SpliceInto)}({s}, {start}, {len}, {rhs}"
        + $"{(pad is null ? "" : $", pad: {pad}")}{(allowZeroLength ? ", allowZeroLength: true" : "")}"
        + $"{(repeat ? ", repeat: true" : "")})";

    /// <summary>⛔ A COMPILER-CHOSEN WINDOW over a character image — <c>CobolString.Window</c>; the emit for every
    /// slice the compiler computes (a REDEFINES view, an occurs-depending group's current extent, an INVOKE/CALL
    /// boundary prefix, a READ … INTO current record) as opposed to a reference modification the PROGRAM wrote,
    /// which is <see cref="StrRefMod(string, string, string, bool)"/>. The first is lenient by design (a short
    /// image pads); the second ends the run unit on a range violation (kb/Work PB1707, R60).
    /// <c>RuntimeApiWindowDriftTests</c> holds the two lists apart.</summary>
    public static string StrWindow(string s, string start, string len) =>
        $"{nameof(CobolString)}.{nameof(CobolString.Window)}({s}, {start}, {len})";

    /// <summary>The receiving twin of <see cref="StrWindow"/> — <c>CobolString.WindowInto</c>;
    /// <paramref name="pad"/> is an optional C# <c>char</c> literal (boolean-zero <c>'0'</c>), null = space.</summary>
    public static string StrWindowInto(string s, string start, string len, string rhs, string? pad = null) =>
        $"{nameof(CobolString)}.{nameof(CobolString.WindowInto)}({s}, {start}, {len}, {rhs}"
        + $"{(pad is null ? "" : $", pad: {pad}")})";

    /// <summary>The three-way alphanumeric comparison — <c>CobolString.Compare</c>. <paramref name="weightsArg"/>
    /// is the trailing collation argument (", __COLLATE" — the program's CobolCollation carrier), possibly empty.</summary>
    public static string StrCompare(string a, string b, string weightsArg) =>
        $"{nameof(CobolString)}.{nameof(CobolString.Compare)}({a}, {b}{weightsArg})";

    /// <summary>⚠ NOT a text fragment — the COMPILE-TIME FOLD of ISO §8.3.3.6.4 GR2, evaluated by calling the very
    /// function the emitted code would call (<c>CobolString.FigToWidth</c>). A figurative sized against a VALUE
    /// clause or a fixed-width receiver folds to a constant at compile time while a relation condition sizes it at
    /// runtime (kb/Work PB297); those two answers must be the same answer, and one implementation is how that is
    /// guaranteed rather than asserted. It lives HERE, in the one CodeGen file the P7 Step 4b ratchet lets name the
    /// runtime, for the reason the façade exists at all: a runtime rename breaks exactly this file.</summary>
    public static string FigToWidthFold(string seed, int width) => CobolString.FigToWidth(seed, width);

    /// <summary>The three-way comparison of a relation whose <paramref name="figIsLeft"/> side is a FIGURATIVE
    /// SEED — <c>CobolString.CompareFig</c> sizes it to the OTHER operand's own runtime character-position count
    /// (ISO §8.3.3.6.4 GR2 over §8.4.3.3.4 GR5; kb/Work PB297). <paramref name="weightsArg"/> is the trailing
    /// pad/collation argument (", pad: '0'" or ", __COLLATE"), possibly empty.</summary>
    public static string StrCompareFig(string a, string b, bool figIsLeft, string weightsArg) =>
        $"{nameof(CobolString)}.{nameof(CobolString.CompareFig)}({a}, {b}, "
        + $"figIsLeft: {(figIsLeft ? "true" : "false")}{weightsArg})";

    /// <summary>An integer count or occurrence-number read — <c>CobolTable.Occ</c>, through the item's profile when
    /// it is numeric (the checked §14.6.13.2 rule 2 read): <c>PlaceRenderer.CountRead</c>'s OCCURS DEPENDING count
    /// and <c>PositionRenderer</c>'s subscript read.</summary>
    public static string TableOcc(string expr, string? profile) => profile is null
        ? $"{nameof(CobolTable)}.{nameof(CobolTable.Occ)}({expr})"
        : $"{nameof(CobolTable)}.{nameof(CobolTable.Occ)}({expr}, {profile})";

    /// <summary>A numeric item read as a reference-modifier position through its own profile —
    /// <c>CobolString.RefModPosition(x, profile)</c> (kb/Work PB41: the item's value, never its unscaled storage).</summary>
    public static string StrRefModPosition(string expr, string profile) =>
        $"{nameof(CobolString)}.{nameof(CobolString.RefModPosition)}({expr}, {profile})";

    /// <summary>A FIXED OCCURS element access — the ref-returning <c>CobolTable.At(path, oneBasedIndex)</c>
    /// (ISO §8.4.2.3.4 GR2 — a benign out-of-range occurrence, subscript-checking off in COBOL-85).</summary>
    public static string TableAt(string path, string oneBasedIndex) =>
        $"{nameof(CobolTable)}.{nameof(CobolTable.At)}({path}, {oneBasedIndex})";

    /// <summary>An OCCURS DEPENDING element access that also tests data-name-1 against integer-1..integer-2 at the
    /// reference (ISO §13.18.38.4 GR7, EC-BOUND-ODO; kb/Work PB1268) — <c>CobolTable.At(path, index, count, min,
    /// max)</c>.</summary>
    public static string TableAtOdo(string path, string oneBasedIndex, string count, int minOccurs, int maxOccurs) =>
        $"{nameof(CobolTable)}.{nameof(CobolTable.At)}({path}, {oneBasedIndex}, {count}, {minOccurs}, {maxOccurs})";

    /// <summary>The current POSITION extent of an occurs-depending GROUP operand (ISO §13.18.38 GR8) — the fixed
    /// prefix plus data-name-1's clamped value × the element width — <c>CobolTable.OdoExtent</c>. The unit is the
    /// group's own (bit positions for a subtree holding USAGE BIT leaves, character positions otherwise; kb/Work
    /// PB173) — the CHARACTER channel uses <see cref="TableOdoExtentChars"/>.</summary>
    public static string TableOdoExtent(string occ, int minOccurs, int maxOccurs, int fixedUnits, int elemUnits) =>
        $"{nameof(CobolTable)}.{nameof(CobolTable.OdoExtent)}({occ}, {minOccurs}, {maxOccurs}, {fixedUnits}, {elemUnits})";

    /// <summary>The current CHARACTER extent of an occurs-depending GROUP operand — <see cref="TableOdoExtent"/>
    /// rounded up to whole characters (<c>CobolTable.OdoExtentChars</c>). Identity when the positions ARE characters,
    /// so the image channel has ONE arm rather than a units branch (kb/Work PB173).</summary>
    public static string TableOdoExtentChars(
        string occ, int minOccurs, int maxOccurs, int fixedUnits, int elemUnits, int positionsPerChar) =>
        $"{nameof(CobolTable)}.{nameof(CobolTable.OdoExtentChars)}"
        + $"({occ}, {minOccurs}, {maxOccurs}, {fixedUnits}, {elemUnits}, {positionsPerChar})";

    /// <summary>A table(ALL) intrinsic argument's enumeration (ISO §15.3; kb/Work PB62) — <c>CobolTable.AllArgs&lt;T&gt;</c>
    /// over one range lambda per ALL level (each <c>Func&lt;long[], long&gt;</c>, the index vector in) and the element
    /// lambda; yields the <c>T[]</c> a <c>params T[]</c> body binds to.</summary>
    public static string TableAllArgs(string csType, IEnumerable<string> countLambdas, string elementLambda, string? leadLambda = null) =>
        $"{nameof(CobolTable)}.{nameof(CobolTable.AllArgs)}<{csType}>(new Func<long[], long>[] {{ {string.Join(", ", countLambdas)} }}, {elementLambda}"
        + (leadLambda is null ? ")" : $", {leadLambda})");

    /// <summary>The intrinsic argument list assembled from written operands and enumerations, in source order —
    /// <c>CobolTable.ArgConcat&lt;T&gt;</c>.</summary>
    public static string TableArgConcat(string csType, IEnumerable<string> parts) =>
        $"{nameof(CobolTable)}.{nameof(CobolTable.ArgConcat)}<{csType}>({string.Join(", ", parts)})";

    /// <summary>Bind an evaluated value to a name inside an expression — <c>CobolTable.With(value, name => body)</c>
    /// (an enumerated argument list read twice: MEAN's sum and count, a leading positional argument and its tail).</summary>
    public static string With(string value, string name, string body) =>
        $"{nameof(CobolTable)}.{nameof(CobolTable.With)}({value}, {name} => {body})";

    // ── Keyed file I/O (CobolFile; ISO §14.9.10/.30/.35/.41/.51) ──

    /// <summary>Register a RELATIVE connector — <c>CobolFile.RegisterRelative</c>. <paramref name="varyArgs"/> is
    /// the optional trailing ", min, max" record-bounds fragment (§13.18.43 GR9/GR10), possibly empty;
    /// <paramref name="edition"/> is the compiling program's <c>--std</c> (see <see cref="EditionArg"/>).</summary>
    public static string FileRegisterRelative(string name, string assign, int width, string optional, int access, int keyDigits, string varyArgs, int edition, string? selectName = null) =>
        $"{nameof(CobolFile)}.{nameof(CobolFile.RegisterRelative)}({name}, {assign}, {width}, {optional}, {access}, {keyDigits}{varyArgs}{SelectNameArg(selectName)}{EditionArg(edition)})";

    /// <summary>Bytes per national character position — <c>CobolBits.BytesPerNational</c> (ISO §13.18.60.4 GR8
    /// leaves a national character's storage size to the implementor; D-N1 pins TWO, UTF-16BE). Re-exported
    /// through the façade for the same reason every emitted fragment routes here (the P7 Step 4b ratchet,
    /// <c>RuntimeApiGuardTests</c>): an emitter that needs the SIZE must not name the runtime member itself, or
    /// a rename stops breaking exactly one file. Used where a national POSITION count and a BYTE count meet —
    /// the §14.9.41.4 GR13 START length, the national group's ODO extent, the record codec's per-occurrence
    /// stride (kb/Work PB327).</summary>
    public const int BytesPerNational = CobolBits.BytesPerNational;

    /// <summary>Declare the connector's record area NATIONAL — <c>CobolFile.RegisterNationalArea</c>, emitted
    /// right after the registration (the <see cref="FileRegisterSharing"/> pattern) for exactly the files whose
    /// record area is of category national, so §14.9.30.4 GR15's short-record fill is the NATIONAL space
    /// (kb/Work PB327).</summary>
    public static string FileRegisterNationalArea(string name) =>
        $"{nameof(CobolFile)}.{nameof(CobolFile.RegisterNationalArea)}({name})";

    /// <summary>Declare the connector's RECORD clause as carrying a DEPENDING phrase —
    /// <c>CobolFile.RegisterRecordLengthFromDepending</c>, emitted right after the registration (the
    /// <see cref="FileRegisterNationalArea"/> pattern) for exactly those files (§14.9.51.4 GR21/GR22; kb/Work
    /// PB1191).</summary>
    public static string FileRegisterRecordLengthFromDepending(string name) =>
        $"{nameof(CobolFile)}.{nameof(CobolFile.RegisterRecordLengthFromDepending)}({name})";

    /// <summary>Declare the connector's input-output area count — <c>CobolFile.RegisterReserve</c>, emitted right after
    /// the registration (the <see cref="FileRegisterNationalArea"/> pattern) for exactly the files whose file control
    /// entry writes the RESERVE clause (ISO §12.4.5.14.3 GR1; kb/Work PB643).</summary>
    public static string FileRegisterReserve(string name, int areas) =>
        $"{nameof(CobolFile)}.{nameof(CobolFile.RegisterReserve)}({name}, {areas})";

    /// <summary>Declare the connector's §13.18.13 CODE-SET conversion — <c>CobolFile.RegisterCodeSet</c>,
    /// emitted right after the registration (the <see cref="FileRegisterNationalArea"/> pattern) for exactly the
    /// files whose CODE-SET clause names a coded character set whose correspondence with the native one is NOT
    /// the identity. <paramref name="toNative"/> is the §12.3.7.4 GR7 i correspondence rendered as a char[]
    /// literal: the whole table is a COMPILE-TIME artifact of the named alphabet, exactly as a collating
    /// sequence's weights are, so the runtime holds no code page (kb/Work PB793).</summary>
    public static string FileRegisterCodeSet(string name, string toNative) =>
        $"{nameof(CobolFile)}.{nameof(CobolFile.RegisterCodeSet)}({name}, {toNative})";

    /// <summary>The SELECT-spelled file-name (ISO §15.28.4 r1c/r2b — kb/Work PB63) as the registration's trailing
    /// named argument; empty when the caller has none.</summary>
    private static string SelectNameArg(string? selectName) => selectName is null ? "" : $", selectName: {selectName}";

    /// <summary>⛔ THE COMPILING PROGRAM'S ISO EDITION as the registration's trailing named argument — the
    /// <c>--std</c> year (85 / 2002 / 2014 / 2023) this compilation targets, which becomes
    /// <c>FileConnector.Edition</c> and is the argument every RUNTIME-side per-edition behaviour gate passes to
    /// <c>DialectBehaviors.IsActive</c> (kb/Work PB344). It rides on the REGISTRATION, beside
    /// <c>selectName</c>/<c>optional</c>, because the rule that governs a file's records is the one the source
    /// element that DESCRIBED the file was compiled under — a run unit may link programs from separate
    /// compilations, so it can be neither a process-wide setting nor a property of the registry.
    /// <para>It is ALWAYS rendered, at every edition including the default: a registration whose edition is
    /// invisible in the generated text is exactly the state PB344 found (no edition reached the runtime at
    /// all), and an emitted named argument is what makes a wrong one readable in a <c>--emit-cs</c> dump.</para></summary>
    private static string EditionArg(int edition) => $", edition: {edition}";

    /// <summary>Register an INDEXED connector — <c>CobolFile.RegisterIndexed</c> (prime-key window per §12.4.5.12,
    /// plus the optional §12.4.5.7 prime-key collating sequence — a CobolCollation expression; <paramref name="weights"/>
    /// is "null" for native, emitted as a named argument so a no-clause file's registration is byte-identical to the
    /// pre-clause engine).</summary>
    public static string FileRegisterIndexed(string name, string assign, int width, string optional, int access, string pkOffset, int pkWidth, string varyArgs, int edition, string weights = "null", string? selectName = null, string layout = "null") =>
        $"{nameof(CobolFile)}.{nameof(CobolFile.RegisterIndexed)}({name}, {assign}, {width}, {optional}, {access}, {pkOffset}, {pkWidth}{varyArgs}{(weights == "null" ? "" : $", primeCollation: {weights}")}{SelectNameArg(selectName)}{EditionArg(edition)}{(layout == "null" ? "" : $", primeLayout: {layout}")})";

    /// <summary>Register one ALTERNATE RECORD KEY window (§12.4.5.6) — <c>CobolFile.AddAlternateKey</c>, with its
    /// optional §12.4.5.7 collating weights and §12.4.5.6.4 GR6 SUPPRESS WHEN value ("null" = absent, each emitted
    /// as a named argument so a plain alternate key's registration is unchanged).</summary>
    public static string FileAddAlternateKey(string name, string offset, int width, string dups, string weights = "null", string suppress = "null", string layout = "null") =>
        $"{nameof(CobolFile)}.{nameof(CobolFile.AddAlternateKey)}({name}, {offset}, {width}, {dups}{(weights == "null" ? "" : $", collation: {weights}")}{(suppress == "null" ? "" : $", suppress: {suppress}")}{(layout == "null" ? "" : $", layout: {layout}")})";

    /// <summary>Position a relative connector to the RELATIVE KEY item's RRN — <c>CobolFile.SetRelativeKey</c>.</summary>
    public static string FileSetRelativeKey(string name, string rrn) =>
        $"{nameof(CobolFile)}.{nameof(CobolFile.SetRelativeKey)}({name}, {rrn})";

    // ⛔ THERE IS NO UNGOVERNED RENDERER FOR A RECORD VERB, AND THERE MUST NEVER BE ONE AGAIN (kb/Work PB683).
    // READ, WRITE, REWRITE and DELETE each have EXACTLY ONE renderer here — FileReadShared / FileWriteShared /
    // FileRewriteShared / FileDeleteShared, and FileReadKeyedShared for the Format-2 random read (kb/Work
    // PB338 collapsed its two halves — an ungoverned ReadKeyed plus a post-read status patch — into that ONE
    // governed entry) — because whether a connector is open FOR FILE SHARING is a RUN-TIME fact: ISO §9.1.15,
    // "The SHARING phrase on an OPEN statement overrides the SHARING clause in the file control entry for
    // establishing the sharing mode". No property of the file control entry or of the statement can see it, so
    // an emitter that CHOOSES between a governed and an ungoverned entry is guessing. It guessed wrong for every
    // connector opened `SHARING WITH READ ONLY` / `NO OTHER` by the OPEN's own phrase, which then read a record
    // another connector had locked with '00' where §14.9.30.4 GR9/GR10 b) require '51'. So there is nothing to
    // choose: render the governed one and let the runtime, one layer down, decide where the OPEN is visible.
    // ⛔ AND THE RUNTIME NO LONGER "FALLS THROUGH TO THE PLAIN BODY" ON A `_connectorShares` MISS — that
    // sentence stood here while it was the SAME defect one layer down (kb/Work PB669): a connector with no
    // SHARING and no LOCK MODE clause never consulted the physical file's lock table, so it read, rewrote and
    // DELETED records another connector held locked. `FileRegistry.ShareOf` now gives a miss the
    // §12.4.5.9.4 GR1 b) 2. implementor default and the governed body runs for EVERY connector; only lock
    // ACQUISITION is still gated, by `LocksEffective`.
    // ⛔ The same rule holds for a verb's SHAPE: the ADVANCING phrase rides INSIDE FileWriteShared as a
    // `WriteAdvance` argument. FileWriteAdvancing/FileWriteBeforeAndAfter existed as separate renderers, and
    // because neither entry has a lock or RETRY parameter, `WRITE R AFTER ADVANCING 1 LINE WITH LOCK` — one
    // legal statement of §14.9.51.2 Format 1 — silently lost both phrases. (The descriptor carries ONE amount
    // and the GR25 e)/f) placement: §14.9.51.2 prints one ADVANCING operand — kb/Work PB712.)

    /// <summary>The ONE governed FORMAT-2 (random) keyed READ (§9.1.16 / §14.9.30.4 GR9–GR12) —
    /// <c>CobolFile.ReadKeyedShared</c> (I-O status result, out image). It OWNS the physical retrieval, so
    /// nothing may render a bare <c>ReadKeyed</c> and then patch its status: §14.9.30.4 GR10 a)/d) require the
    /// file position indicator and the key of reference to be UNCHANGED on a record operation conflict, which a
    /// post-read adjustment cannot deliver (kb/Work PB338).</summary>
    public static string FileReadKeyedShared(string name, int keyIndex, string keyImage, string lockRef,
        string ignoringLock, string retryKind, string retryAmount, string imgVar, string? areaExtents = null) =>
        $"{nameof(CobolFile)}.{nameof(CobolFile.ReadKeyedShared)}({name}, {keyIndex}, {keyImage}, {lockRef}, {ignoringLock}, {retryKind}, {retryAmount}, out var {imgVar}{AreaExtentsArg(areaExtents)})";

    /// <summary>⛔ THE ONE SPELLING of the record area's EXTENT TABLE argument (determination D-FRA (v); kb/Work
    /// PB1053) on every <c>CobolFile</c> entry that takes a record-area image: a NAMED trailing argument, so it can
    /// follow whichever optional arguments an entry has; nothing at all when the record is not a variable-length
    /// group (<see cref="Emit.OperandText.RecordAreaExtents"/> answered null).</summary>
    private static string AreaExtentsArg(string? areaExtents) => areaExtents is null ? "" : $", areaExtents: {areaExtents}";

    /// <summary>The WRITE / REWRITE statement's record category argument (§14.9.51.4 GR21 / GR22, §14.9.35.4 GR17):
    /// named, and present only for a NATIONAL record, the alphanumeric one being the parameter's default.</summary>
    private static string NationalRecordArg(bool nationalRecord) => nationalRecord ? ", nationalRecord: true" : "";

    /// <summary>The ONE governed FORMAT-1 READ, every organization (§9.1.16 / §14.9.30.4 GR9–GR12 + the GR22
    /// ADVANCING ON LOCK skip-scan) — <c>CobolFile.ReadShared</c> (I-O status result, out image). Both READ
    /// emitters render this call: the keyed one takes the status straight, the sequential one wraps it in
    /// <see cref="FileReadSharedOk"/> for its bool contract.</summary>
    public static string FileReadShared(string name, string previous, string lockRef, string advancingOnLock,
        string ignoringLock, string retryKind, string retryAmount, string imgVar) =>
        $"{nameof(CobolFile)}.{nameof(CobolFile.ReadShared)}({name}, {previous}, {lockRef}, {advancingOnLock}, {ignoringLock}, {retryKind}, {retryAmount}, out var {imgVar})";

    /// <summary>The governed Format-1 READ as a BOOL — "a record was made available" is the I-O status's first
    /// character being '0' (§9.1.13.2, through <see cref="IoStatusClass"/>), the same contract as the plain <c>FileRead</c>.</summary>
    public static string FileReadSharedOk(string name, string previous, string lockRef, string advancingOnLock,
        string ignoringLock, string retryKind, string retryAmount, string imgVar) =>
        $"{IoStatusClass.Successful(FileReadShared(name, previous, lockRef, advancingOnLock, ignoringLock, retryKind, retryAmount, imgVar))}";

    /// <summary>⛔ THE ONE WRITE, any organization and any print-control shape (§14.9.51 GR10/GR11) —
    /// <c>CobolFile.WriteShared</c>. <paramref name="pageArg"/> is the executing element's LINAGE page
    /// (§13.18.34 GR6 b) — see <see cref="LinagePageExpr"/>; <paramref name="advance"/> is the statement's
    /// ADVANCING phrases as a <c>WriteAdvance</c> descriptor (omitted = none), never a separate entry
    /// (kb/Work PB683). <paramref name="nationalRecord"/> — record-name-1's category (§14.9.51.4 GR21 / GR22) — is
    /// rendered only when true, so every alphanumeric WRITE renders as it always did (kb/Work PB1191).</summary>
    public static string FileWriteShared(string name, string image, string lenArg, string lockRef, string retryKind,
        string retryAmount, string pageArg, string? advance = null, string? areaExtents = null, bool nationalRecord = false) =>
        $"{nameof(CobolFile)}.{nameof(CobolFile.WriteShared)}({name}, {image}, {lenArg}, {lockRef}, {retryKind}, {retryAmount}, {pageArg}{(advance is null ? "" : $", {advance}")}{AreaExtentsArg(areaExtents)}{NationalRecordArg(nationalRecord)})";

    /// <summary>Governed REWRITE for a sharing-active file, any organization (§14.9.35 GR11/GR12) — <c>CobolFile.RewriteShared</c>.</summary>
    public static string FileRewriteShared(string name, string image, string lenArg, string lockRef, string retryKind, string retryAmount,
        string? areaExtents = null, bool nationalRecord = false) =>
        $"{nameof(CobolFile)}.{nameof(CobolFile.RewriteShared)}({name}, {image}, {lenArg}, {lockRef}, {retryKind}, {retryAmount}{AreaExtentsArg(areaExtents)}{NationalRecordArg(nationalRecord)})";

    /// <summary>Governed DELETE RECORD for a sharing-active file (§14.9.10 GR6/GR7) — <c>CobolFile.DeleteShared</c>.</summary>
    public static string FileDeleteShared(string name, string areaImage, string retryKind, string retryAmount,
        string? areaExtents = null) =>
        $"{nameof(CobolFile)}.{nameof(CobolFile.DeleteShared)}({name}, {areaImage}, {retryKind}, {retryAmount}{AreaExtentsArg(areaExtents)})";

    /// <summary>DELETE FILE with a RETRY phrase (§14.9.10 GR15 — the '62' re-attempt) — <c>CobolFile.DeleteFile</c>.</summary>
    public static string FileDeleteFileRetry(string name, string retryKind, string retryAmount, string overridden) =>
        $"{nameof(CobolFile)}.{nameof(CobolFile.DeleteFile)}({name}, {retryKind}, {retryAmount}, {overridden})";

    /// <summary>The connector's current relative slot number — <c>CobolFile.RelativeSlot</c>.</summary>
    public static string FileRelativeSlot(string name) =>
        $"{nameof(CobolFile)}.{nameof(CobolFile.RelativeSlot)}({name})";

    /// <summary>DELETE FILE (Format 2, every organization) — <c>CobolFile.DeleteFile</c>.</summary>
    public static string FileDeleteFile(string name, string overridden) =>
        $"{nameof(CobolFile)}.{nameof(CobolFile.DeleteFile)}({name}, {overridden})";

    /// <summary>START FIRST/LAST — <c>CobolFile.StartFirstLast</c>.</summary>
    public static string FileStartFirstLast(string name, string last) =>
        $"{nameof(CobolFile)}.{nameof(CobolFile.StartFirstLast)}({name}, {last})";

    /// <summary>START on a relative file (§14.9.41 GR9/GR10 — numeric RRN comparison) — <c>CobolFile.StartRelative</c>.</summary>
    public static string FileStartRelative(string name, string opLiteral, string rrn) =>
        $"{nameof(CobolFile)}.{nameof(CobolFile.StartRelative)}({name}, {opLiteral}, {rrn})";

    /// <summary>START on an indexed file (§14.9.41 GR17 — leftmost-LENGTH key comparison) — <c>CobolFile.StartIndexed</c>.</summary>
    /// <param name="recordAreaImage">The FD's RECORD AREA image — §14.9.41.4 GR17 a) sets the search key up "by
    /// moving the relevant parts of the record area into a temporary data area", so the connector slices the key
    /// of reference out of the area exactly as the random READ and DELETE do (kb/Work PB355). NOT data-name-1's
    /// own rendering: that agreed with the rule only until <paramref name="len"/> reached past it.</param>
    /// <param name="len">A <see cref="StartKeyLength"/> expression (<see cref="StartKeyLengthScaled"/> /
    /// <see cref="StartKeyLengthReal"/> / <see cref="StartKeyLengthWidth"/>) — never a pre-narrowed <c>int</c>
    /// (kb/Work PB357).</param>
    public static string FileStartIndexed(string name, int keyIndex, string opLiteral, string recordAreaImage, string len,
        string? areaExtents = null) =>
        $"{nameof(CobolFile)}.{nameof(CobolFile.StartIndexed)}({name}, {keyIndex}, {opLiteral}, {recordAreaImage}, {len}{AreaExtentsArg(areaExtents)})";

    /// <summary>START WITH LENGTH arithmetic-expression-1 on the exact fixed-point lane (§14.9.41.4 GR14 —
    /// <c>StartKeyLength.OfScaled</c>): the scaled <c>Int128</c> and its scale travel intact, so the connector's
    /// integrality and range test sees the value the source computed.</summary>
    public static string StartKeyLengthScaled(string scaled, string scale, int unitWidth) =>
        $"{nameof(StartKeyLength)}.{nameof(StartKeyLength.OfScaled)}((System.Int128)({scaled}), {scale}, {unitWidth})";

    /// <summary>The native-float lane of <see cref="StartKeyLengthScaled"/> — <c>StartKeyLength.OfReal</c>.</summary>
    public static string StartKeyLengthReal(string value, int unitWidth) =>
        $"{nameof(StartKeyLength)}.{nameof(StartKeyLength.OfReal)}((double)({value}), {unitWidth})";

    /// <summary>No WITH LENGTH phrase — GR17 b)'s "or else the length of data-name-1" (<c>StartKeyLength.OfWidth</c>).</summary>
    public static string StartKeyLengthWidth(int storageWidth) =>
        $"{nameof(StartKeyLength)}.{nameof(StartKeyLength.OfWidth)}({storageWidth})";

    // ⛔ NO MODE-SPECIFIC "IMPLICIT OPEN" RENDERER LIVES HERE. `FileOpenInput` / `FileOpenOutput` used to, for
    // the SORT/MERGE USING and GIVING transfers alone, and their whole defect was the parameter they did NOT
    // take: §14.9.40.4 GR12 a)/GR15 a) and §14.9.24.4 GR7 a)/GR12 a) each render the initiation "as if an OPEN
    // statement with the … SHARING WITH READ ONLY / SHARING WITH NO OTHER phrase had been executed", and an
    // entry point with no sharing parameter silently dropped every one of them (kb/Work PB714). The implicit
    // opens now render through the SAME two entries an explicit OPEN does — <see cref="FileOpen"/> for the
    // "without a SHARING phrase" arm and <see cref="FileOpenShared"/> for a phrase-bearing one — so the DRIFT IS
    // COMPILE-ENFORCED: there is no phrase-less implicit-open renderer left for a new call site to reach.

    /// <summary>CLOSE a connector — <c>CobolFile.Close</c>.</summary>
    public static string FileClose(string name) =>
        $"{nameof(CobolFile)}.{nameof(CobolFile.Close)}({name})";

    /// <summary>The IMPLICIT close (§14.9.5 GR9 — only a connector "that is open") — <c>CobolFile.CloseIfOpen</c>.</summary>
    public static string FileCloseIfOpen(string name) =>
        $"{nameof(CobolFile)}.{nameof(CobolFile.CloseIfOpen)}({name})";

    /// <summary>Register a SEQUENTIAL/LINE-SEQUENTIAL connector — <c>CobolFile.Register</c>.
    /// <paramref name="varyArgs"/> is the optional trailing ", min, max" bounds fragment;
    /// <paramref name="edition"/> is the compiling program's <c>--std</c> (see <see cref="EditionArg"/>).</summary>
    public static string FileRegister(string name, string assign, string width, string lineSeq, string optional, int edition, string varyArgs = "", string? selectName = null, int recordMax = 0) =>
        $"{nameof(CobolFile)}.{nameof(CobolFile.Register)}({name}, {assign}, {width}, {lineSeq}, {optional}{varyArgs}{SelectNameArg(selectName)}{EditionArg(edition)}"
        + (recordMax > 0 ? $", recordMax: {recordMax})" : ")");

    /// <summary>Register a REPORT FILE connector — <c>CobolFile.RegisterReport</c>: a file with no record description,
    /// whose <paramref name="lineWidth"/> is the widest hosted RD's line width (kb/Work PB677).</summary>
    public static string FileRegisterReport(string name, string assign, string lineWidth, string optional, int edition, string? selectName = null) =>
        $"{nameof(CobolFile)}.{nameof(CobolFile.RegisterReport)}({name}, {assign}, {lineWidth}, {optional}{SelectNameArg(selectName)}{EditionArg(edition)})";

    /// <summary>⛔ THE PER-STATEMENT OPERANDS OF THE RUNTIME ELEMENT EXECUTING A FILE STATEMENT — the
    /// <c>assign, assignDynamic, page</c> argument triple every OPEN entry takes (kb/Work PB673). ISO
    /// §12.4.5.3 GR3 a)/b) associate the connector using the specification "in the source unit that specifies" /
    /// "in the runtime element that executes" the OPEN, SORT or MERGE, and §13.18.34 GR6 b) 1 reads the LINAGE
    /// operands at the completion of an OPEN OUTPUT — so these are the STATEMENT'S arguments, never state
    /// installed on a connector that a whole run unit may share (§13.18.22.4 GR4 a).</summary>
    public static string ExecutingElementArgs(string assign, bool assignDynamic, string pageArg) =>
        $"{assign}, {(assignDynamic ? "true" : "false")}, {pageArg}";

    /// <summary>The executing element's LINAGE operand values as a <c>LinagePage</c> (§13.18.34 GR6: page size,
    /// footing start, top margin, bottom margin — a literal operand renders a constant per GR6 a), a data-name
    /// operand the element's own field read per GR6 b)). <c>null</c> when the FD carries no LINAGE clause; a
    /// <c>null</c> <paramref name="footing"/> is §13.18.34.4 GR1's ABSENT FOOTING phrase, which is not the value
    /// zero (kb/Work PB525 — see <see cref="LinagePage"/>).</summary>
    public static string LinagePageExpr(string body, string footing, string top, string bottom) =>
        $"new {nameof(LinagePage)}({body}, {footing}, {top}, {bottom})";

    /// <summary>Mark a connector sharing-active (Phase 4d M2-FILE-1) — <c>CobolFile.RegisterSharing</c>.</summary>
    public static string FileRegisterSharing(string name, string argsFragment) =>
        $"{nameof(CobolFile)}.{nameof(CobolFile.RegisterSharing)}({name}, {argsFragment})";

    /// <summary>The sharing-governed OPEN (Table 19 → status 61) — <c>CobolFile.OpenShared</c>.</summary>
    public static string FileOpenShared(string name, string argsFragment) =>
        $"{nameof(CobolFile)}.{nameof(CobolFile.OpenShared)}({name}, {argsFragment})";

    /// <summary>The mode-specific plain OPEN — anchored over <c>CobolFile.Open{Input,Output,Extend,IO}</c>.
    /// <para><paramref name="tape"/> = the file-name's §14.9.27.2 tape phrase, which routes to the ONE
    /// written-form entry <c>CobolFile.OpenTape</c> instead (§14.9.27.4 GR11 for NO REWIND, the COBOL-85
    /// backward retrieval for REVERSED — the runtime owns the medium test and the positioning, exactly as it
    /// owns Table 14's for CLOSE). The mode still travels, so the runtime never has to re-derive it
    /// (kb/Work PB317, kb/Work PB668).</para></summary>
    public static string FileOpen(string name, Binding.Bound.BoundOpenMode mode,
        Binding.Bound.BoundOpenTapePhrase tape, string elementArgs)
    {
        if (tape is not Binding.Bound.BoundOpenTapePhrase.None)
            return $"{nameof(CobolFile)}.{nameof(CobolFile.OpenTape)}({name}, {FileOpenModeExpr(mode)}, {OpenTapePhraseExpr(tape)}, {elementArgs})";
        return $"{nameof(CobolFile)}.{mode switch
        {
            Binding.Bound.BoundOpenMode.Output => nameof(CobolFile.OpenOutput),
            Binding.Bound.BoundOpenMode.Extend => nameof(CobolFile.OpenExtend),
            Binding.Bound.BoundOpenMode.IO => nameof(CobolFile.OpenIO),
            _ => nameof(CobolFile.OpenInput),
        }}({name}, {elementArgs})";
    }

    /// <summary>⛔ THE ONE rendering of a bound tape phrase as the runtime <c>OpenTapePhrase</c> member — the
    /// plain entry and the sharing entry share it, because §14.9.27.2 puts the SHARING/RETRY phrases and the
    /// per-file-name tape phrase in one general format and a statement may write both (kb/Work PB317/PB668).
    /// The two enums are deliberately separate types — the bound tree is backend-neutral — and this is the ONE
    /// seam between them, so a new tape phrase is a member on each side and one arm here.</summary>
    public static string OpenTapePhraseExpr(Binding.Bound.BoundOpenTapePhrase tape) =>
        $"{nameof(OpenTapePhrase)}.{tape switch
        {
            Binding.Bound.BoundOpenTapePhrase.NoRewind => nameof(OpenTapePhrase.NoRewind),
            Binding.Bound.BoundOpenTapePhrase.Reversed => nameof(OpenTapePhrase.Reversed),
            _ => nameof(OpenTapePhrase.None),
        }}";

    /// <summary>⛔ THE ONE rendering of a bound open mode as the runtime <c>FileOpenMode</c> member — the
    /// sharing entry, the tape-phrase entry and any future one share it, so a new mode cannot reach one caller
    /// and miss another (the emitter used to carry its own copy of this switch inline).</summary>
    public static string FileOpenModeExpr(Binding.Bound.BoundOpenMode mode) =>
        $"{nameof(FileOpenMode)}.{mode switch
        {
            Binding.Bound.BoundOpenMode.Output => nameof(FileOpenMode.Output),
            Binding.Bound.BoundOpenMode.Extend => nameof(FileOpenMode.Extend),
            Binding.Bound.BoundOpenMode.IO => nameof(FileOpenMode.IO),
            _ => nameof(FileOpenMode.Input),
        }}";

    /// <summary>The written-form CLOSE entry — one per Table 14 row (§14.9.6.4 GR3) plus WITH LOCK, anchored
    /// over <c>CobolFile.Close{,WithLock,ReelUnit,ReelUnitForRemoval,NoRewind}</c>. The runtime resolves the
    /// row against the file's §14.9.6.4 GR2 category; the emitter names the FORM the program wrote.</summary>
    public static string FileClose(string name, Binding.Bound.BoundCloseKind kind) =>
        $"{nameof(CobolFile)}.{kind switch
        {
            Binding.Bound.BoundCloseKind.WithLock => nameof(CobolFile.CloseWithLock),
            Binding.Bound.BoundCloseKind.ReelUnit => nameof(CobolFile.CloseReelUnit),
            Binding.Bound.BoundCloseKind.ReelUnitForRemoval => nameof(CobolFile.CloseReelUnitForRemoval),
            Binding.Bound.BoundCloseKind.NoRewind => nameof(CobolFile.CloseNoRewind),
            Binding.Bound.BoundCloseKind.Normal => nameof(CobolFile.Close),
            // ⛔ NOT a silent fall-through to the plain CLOSE. A written form with no entry here would emit a
            // Table 14 row the program did not write — the exact shape that let REEL/UNIT FOR REMOVAL ride
            // REEL/UNIT's entry for two waves (kb/Work PB235). Only an out-of-range cast reaches this.
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "no CobolFile CLOSE entry for this written form"),
        }}({name})";

    /// <summary>UNLOCK — <c>CobolFile.Unlock</c> (records flag = UNLOCK RECORDS).</summary>
    public static string FileUnlock(string name, string records) =>
        $"{nameof(CobolFile)}.{nameof(CobolFile.Unlock)}({name}, {records})";

    /// <summary>The LINAGE end-of-page probe (§13.18.34) — <c>CobolFile.EndOfPage</c>.</summary>
    public static string FileEndOfPage(string name) =>
        $"{nameof(CobolFile)}.{nameof(CobolFile.EndOfPage)}({name})";

    /// <summary>The connector's two-character I-O status — <c>CobolFile.Status</c>.</summary>
    public static string FileStatus(string name) =>
        $"{nameof(CobolFile)}.{nameof(CobolFile.Status)}({name})";

    /// <summary>Terminate the run unit on a fatal I-O status that nothing covers (a file with no FILE STATUS clause
    /// and no applicable USE procedure; A.1 item 103) — <c>CobolFile.TerminateOnUncoveredFatalStatus</c>.
    /// <paramref name="verbLiteral"/> is the statement's name as a C# string literal.</summary>
    public static string TerminateOnUncoveredFatalStatus(string name, string verbLiteral) =>
        $"{nameof(CobolFile)}.{nameof(CobolFile.TerminateOnUncoveredFatalStatus)}({name}, {verbLiteral})";

    /// <summary>The EC-I-O level-3 exception-name the connector's last I-O operation set to exist (§9.1.13.1's
    /// status correspondence, or a name the operation's own rule gave) — <c>CobolFile.IoConditionName</c>.</summary>
    public static string FileIoConditionName(string name) =>
        $"{nameof(CobolFile)}.{nameof(CobolFile.IoConditionName)}({name})";

    /// <summary>The open-mode ordinal of a connector (the USE mode-scope switch) — <c>CobolFile.OpenModeOf</c>.</summary>
    public static string FileOpenModeOf(string name) =>
        $"{nameof(CobolFile)}.{nameof(CobolFile.OpenModeOf)}({name})";

    /// <summary>The just-read record's frame length — <c>CobolFile.LastReadLength</c>.</summary>
    public static string FileLastReadLength(string name) =>
        $"{nameof(CobolFile)}.{nameof(CobolFile.LastReadLength)}({name})";

    /// <summary>A READ's ODO-too-long outcome — <c>CobolFile.ReadExceedsRecordMaximum</c> (§9.1.13.6 item 4 b);
    /// kb/Work PB1513): sets '34' on the connector and yields the status.</summary>
    public static string FileReadExceedsRecordMaximum(string name) =>
        $"{nameof(CobolFile)}.{nameof(CobolFile.ReadExceedsRecordMaximum)}({name})";

    /// <summary>The logical record a WRITE / REWRITE / RELEASE released, at its released length —
    /// <c>CobolFile.ReleasedRecord</c> (§14.9.51.4 GR4, §14.9.35.4 GR6, §14.9.32.4 GR3; kb/Work PB1195).</summary>
    public static string FileReleasedRecord(string image, string length) =>
        $"{nameof(CobolFile)}.{nameof(CobolFile.ReleasedRecord)}({image}, {length})";

    /// <summary>The record the last successful READ made available, at its own length —
    /// <c>CobolFile.CurrentRecord</c> (determination D-FRA; kb/Work PB981).</summary>
    public static string FileCurrentRecord(string name) =>
        $"{nameof(CobolFile)}.{nameof(CobolFile.CurrentRecord)}({name})";

    /// <summary>The extent table the current record was sent with — <c>CobolFile.CurrentRecordExtents</c>
    /// (determination D-FRA (v); kb/Work PB1053).</summary>
    public static string FileCurrentRecordExtents(string name) =>
        $"{nameof(CobolFile)}.{nameof(CobolFile.CurrentRecordExtents)}({name})";

    // ── SORT / MERGE (CobolSort; ISO §14.9.40 / §14.9.24) ──

    /// <summary>Initialize the per-SD image store — <c>CobolSort.Init</c> — with the statement's collating sequence,
    /// snapshotted at statement start (ISO §14.6.6 r5: a locale switch during the SORT/MERGE has no effect on it).</summary>
    public static string SortInit(string sd, string weights, string natWeights) =>
        $"{nameof(CobolSort)}.{nameof(CobolSort.Init)}({sd}, {weights}, {natWeights})";

    /// <summary>The implicit USING release of one record image — <c>CobolSort.Release</c>, with the size the
    /// record had when READ and the sort-merge file's record range (the EC-SORT-MERGE-RELEASE test, §14.9.40.4
    /// GR12 b) / §14.9.24.4 GR7 b)), and the record's EXTENT TABLE when a variable-length group record is released
    /// (D-FRA (v); kb/Work PB1053).</summary>
    public static string SortRelease(string sd, string image, string readSize, int min, int max, string? extents = null) =>
        $"{nameof(CobolSort)}.{nameof(CobolSort.Release)}({sd}, {image}, {readSize}, {min}, {max}{(extents is null ? "" : $", extents: {extents}")})";

    /// <summary>The SORT/MERGE statement's association of its own sort-merge file — <c>CobolSort.AssociationMade</c>
    /// (§12.4.5.3 GR3 b); kb/Work PB1097). <paramref name="assign"/> is data-name-1's content.</summary>
    public static string SortAssociationMade(string assign) =>
        $"{nameof(CobolSort)}.{nameof(CobolSort.AssociationMade)}({assign})";

    /// <summary>The EC-SORT-MERGE-FILE-OPEN test for one USING/GIVING file — <c>CobolSort.FileNotOpen</c>
    /// (§14.9.40.4 GR9, §14.9.24.4 GR7 / GR12).</summary>
    public static string SortFileNotOpen(string sd, string file) =>
        $"{nameof(CobolSort)}.{nameof(CobolSort.FileNotOpen)}({sd}, {file})";

    /// <summary>The RELEASE STATEMENT — <c>CobolSort.ReleaseStatement</c> (§14.9.32.4 GR1's phase test, then GR2).
    /// The implicit USING release (§14.9.40.4 GR12 b) renders <see cref="SortRelease"/> instead.
    /// <paramref name="min"/>..<paramref name="max"/> is the record range §13.18.43.4 GR14 / GR19 test,
    /// <paramref name="size"/> the GR13 a) DEPENDING ON value (null: the image's own length, GR13 b)/c)) and
    /// <paramref name="extents"/> the record's extent table (D-FRA (v); kb/Work PB1053).</summary>
    public static string SortReleaseStatement(string sd, string image, int min, int max, string? size = null, string? extents = null) =>
        $"{nameof(CobolSort)}.{nameof(CobolSort.ReleaseStatement)}({sd}, {image}, {min}, {max}{(size is null ? "" : $", size: {size}")}{(extents is null ? "" : $", extents: {extents}")})";

    /// <summary>The RETURN STATEMENT — <c>CobolSort.ReturnStatement</c> (§14.9.34.4 GR1 + GR3's phase and at-end
    /// tests, then GR3's retrieval). The implicit GIVING return renders <see cref="SortReturn"/> instead.</summary>
    public static string SortReturnStatement(string sd, string imgVar) =>
        $"{nameof(CobolSort)}.{nameof(CobolSort.ReturnStatement)}({sd}, out var {imgVar})";

    /// <summary>Enter a SORT's INPUT PROCEDURE (<c>output: false</c>) or a SORT/MERGE's OUTPUT PROCEDURE
    /// (<c>output: true</c>) — <c>CobolSort.EnterProcedure</c>, the phase the two verbs' GR1 test.</summary>
    public static string SortEnterProcedure(string sd, bool output) =>
        $"{nameof(CobolSort)}.{nameof(CobolSort.EnterProcedure)}({sd}, output: {(output ? "true" : "false")})";

    /// <summary>The sequence phase — <c>CobolSort.Sort</c> (stable; GR8).</summary>
    public static string SortSort(string sd, string keys, string dupsInOrder) =>
        $"{nameof(CobolSort)}.{nameof(CobolSort.Sort)}({sd}, {keys}, {dupsInOrder})";

    /// <summary>The k-way merge — <c>CobolSort.Merge</c> (GR4 — file order breaks ties).</summary>
    public static string SortMerge(string sd, string keys) =>
        $"{nameof(CobolSort)}.{nameof(CobolSort.Merge)}({sd}, {keys})";

    /// <summary>Open a new pre-sorted USING stream — <c>CobolSort.NextInput</c>.</summary>
    public static string SortNextInput(string sd) => $"{nameof(CobolSort)}.{nameof(CobolSort.NextInput)}({sd})";

    /// <summary>Close the SD store — <c>CobolSort.Close</c>.</summary>
    public static string SortClose(string sd) => $"{nameof(CobolSort)}.{nameof(CobolSort.Close)}({sd})";

    /// <summary>Rewind the return cursor (each GIVING file gets the FULL result, GR15) — <c>CobolSort.Rewind</c>.</summary>
    public static string SortRewind(string sd) => $"{nameof(CobolSort)}.{nameof(CobolSort.Rewind)}({sd})";

    /// <summary>Pull the next record in key order — <c>CobolSort.Return</c> (bool, out image).</summary>
    public static string SortReturn(string sd, string imgVar) =>
        $"{nameof(CobolSort)}.{nameof(CobolSort.Return)}({sd}, out var {imgVar})";

    /// <summary>The just-returned record's length (§13.18.43 GR15) — <c>CobolSort.LastReturnedLength</c>.</summary>
    public static string SortLastReturnedLength(string sd) =>
        $"{nameof(CobolSort)}.{nameof(CobolSort.LastReturnedLength)}({sd})";

    /// <summary>The just-returned record's extent table — <c>CobolSort.LastReturnedExtents</c> (determination D-FRA
    /// (v); kb/Work PB1053).</summary>
    public static string SortLastReturnedExtents(string sd) =>
        $"{nameof(CobolSort)}.{nameof(CobolSort.LastReturnedExtents)}({sd})";

    /// <summary>The short-record right-fill of a SORT/MERGE transfer (§14.9.40.4 GR7 / GR16, §14.9.24.4 GR2 / GR13;
    /// kb/Work PB1140) — <c>CobolSort.FillTo</c>. <paramref name="national"/> is
    /// <c>FileModel.ShortRecordFillNational</c> of the file the record moves to; <paramref name="extents"/> is the
    /// returned record's extent table on the GIVING side (null on the USING side, whose record has none).</summary>
    public static string SortFillTo(string image, string width, bool national, string? extents = null) =>
        $"{nameof(CobolSort)}.{nameof(CobolSort.FillTo)}({image}, {width}, {(national ? "true" : "false")}{(extents is null ? "" : $", {extents}")})";

    /// <summary>The <c>CobolSort.Key[]</c> array literal over per-key "new(…)" element fragments.</summary>
    public static string SortKeyArray(IEnumerable<string> keyElements) =>
        $"new {nameof(CobolSort)}.{nameof(CobolSort.Key)}[] {{ {string.Join(", ", keyElements)} }}";

    /// <summary>⛔ THE ONE MAPPING between the binder's <see cref="Binding.CollatingClass"/> and the runtime's
    /// <c>CobolSort.KeyClass</c> — the two enumerations state the SAME ISO rule (§14.9.40.4 GR5 / §14.9.24.4 GR5
    /// select a key's comparator by the key's class) on the two sides of the emitted-text boundary, and this
    /// exhaustive switch is what makes a member added to one and not the other a COMPILE error here rather than a
    /// silently mis-emitted comparator.</summary>
    public static string SortKeyClass(Binding.CollatingClass cls)
    {
        string member = cls switch
        {
            Binding.CollatingClass.Alphanumeric => nameof(CobolSort.KeyClass.Alphanumeric),
            Binding.CollatingClass.National => nameof(CobolSort.KeyClass.National),
            Binding.CollatingClass.Boolean => nameof(CobolSort.KeyClass.Boolean),
            Binding.CollatingClass.Numeric => nameof(CobolSort.KeyClass.Numeric),
            _ => throw new ArgumentOutOfRangeException(nameof(cls), cls, "unmapped collating class"),
        };
        return $"{nameof(CobolSort)}.{nameof(CobolSort.KeyClass)}.{member}";
    }

    // ── Report Writer (CobolReport; ISO §13.14–§13.18) ──

    /// <summary>A fresh, empty line of the report at <paramref name="reportIndex"/> — <c>CobolReport.NewLine</c>.
    /// Emitted inside the declaring program's line compose, so the engine is this program's own (depth 0).</summary>
    public static string ReportNewLine(int reportIndex) =>
        $"{ReportEngine(reportIndex)}.{nameof(CobolReport.NewLine)}()";

    /// <summary>Place a printable item's image at its COLUMN (§13.18.14) — <c>CobolReport.Place</c>, which tests the
    /// §13.18.14.4 GR4 column overlap and GR5 page width as it places (kb/Work PB1188).</summary>
    public static string ReportPlace(int reportIndex, string lineVar, int column, string image) =>
        ReportPlace(reportIndex, lineVar, column.ToString(), image);

    /// <summary>The variable-column form of <see cref="ReportPlace(int,string,int,string)"/> — a relative (PLUS)
    /// COLUMN operand places against the line's horizontal counter (§13.18.14.4 GR8).</summary>
    public static string ReportPlace(int reportIndex, string lineVar, string columnExpr, string image) =>
        $"{ReportEngine(reportIndex)}.{nameof(CobolReport.Place)}({lineVar}, {columnExpr}, {image})";

    /// <summary>Is slot <paramref name="slot"/> of report group <paramref name="groupIndex"/>'s presence snapshot
    /// present — <c>CobolReport.IsPresent</c> (§13.18.41.4 GR2; kb/Work PB1272). Emitted inside the declaring
    /// program's line compose (depth 0).</summary>
    public static string ReportIsPresent(int reportIndex, int groupIndex, int slot) =>
        $"{ReportEngine(reportIndex)}.{nameof(CobolReport.IsPresent)}({groupIndex}, {slot})";

    /// <summary>The per-program-instance engine field of the report at <paramref name="reportIndex"/>
    /// (<c>ReportModel.CsIndex</c>) — the ONE spelling of that field, so the emitter and the place renderer
    /// cannot drift apart.
    /// <para><paramref name="depth"/> is the containment distance to the DECLARING program (0 = this one): a GLOBAL
    /// report (ISO §13.18.27.4 GR2; kb/Work PB369) is one engine, owned by the program that declares it and reached
    /// from a contained program through the <c>__outer</c> chain.</para></summary>
    public static string ReportEngine(int reportIndex, int depth = 0) => $"{OuterChain(depth)}__RPT_{reportIndex}";

    /// <summary>Ask the engine of the report at <paramref name="reportIndex"/> whether sum counter
    /// <paramref name="counterId"/> may be moved to its printable item — false (and EC-REPORT-SUM-SIZE raised) while
    /// its size error indicator is set (ISO §13.18.54.4 GR4; kb/Work PB1130). Emitted inside the declaring program's
    /// line compose, so the engine is this program's own (depth 0).</summary>
    public static string ReportSumPresentable(int reportIndex, int counterId) =>
        $"{ReportEngine(reportIndex)}.{nameof(CobolReport.SumPresentable)}({counterId})";

    /// <summary>The instance-chain prefix from a contained program's class to its <paramref name="depth"/>-th
    /// container (<c>__outer.</c> repeated; empty for 0) — the ONE spelling of the walk the generated contained
    /// classes expose (ProgramEmitter: <c>private readonly Outer __outer</c>).</summary>
    public static string OuterChain(int depth) => depth == 0 ? "" : string.Concat(Enumerable.Repeat("__outer.", depth));

    /// <summary>Read a report counter (ISO §8.4.3.15.4 GR1 — "PAGE-COUNTER and LINE-COUNTER reference temporary
    /// unsigned integer data items of class and category numeric, which are maintained for each report"): the
    /// engine's <c>PageCounter</c> / <c>LineCounter</c>. THE ONE SPELLING — the numeric renderer's sending
    /// <c>BoundReportCounterRef</c> and the place renderer's receiving <c>ReportPageCounterPlace</c> both read
    /// through here, so the two directions cannot name different members (kb/Work PB429).</summary>
    public static string ReportCounterRead(int reportIndex, int depth, bool isPage) =>
        $"{ReportEngine(reportIndex, depth)}.{(isPage ? nameof(CobolReport.PageCounter) : nameof(CobolReport.LineCounter))}";

    /// <summary>Assign PAGE-COUNTER from the procedure division (ISO §8.4.3.15.3 SR1; SR3 bars LINE-COUNTER and
    /// ONLY LINE-COUNTER from the receiving side) — <c>CobolReport.SetPageCounter</c>.</summary>
    public static string ReportPageCounterWrite(int reportIndex, int depth, string valueExpr) =>
        $"{ReportEngine(reportIndex, depth)}.{nameof(CobolReport.SetPageCounter)}((long)({valueExpr}));";

    /// <summary>The argument list that ADDRESSES one SUM counter for <c>CobolReport.SumValue</c> /
    /// <c>SetSumValue</c> — THE ONE SPELLING, so a read and a write of the same reference cannot select different
    /// counters. With no <paramref name="subscripts"/> it is the counter's id (GR1's identity: an entry occurrence,
    /// kb/Work PB882). With subscripts it is a REPEATING entry's family (kb/Work PB1271): the first id of the family's
    /// block, the extent of each OCCURS level (outermost first) and the one-based subscript values, which the engine
    /// turns into the occurrence's id with §8.4.2.3.4 GR2's EC-BOUND-SUBSCRIPT test. The two lists are collection
    /// expressions over <c>ReadOnlySpan</c> parameters, so the access allocates nothing.</summary>
    public static string ReportSumAddress(int counterId, IReadOnlyList<int> extents, IReadOnlyList<string> subscripts) =>
        subscripts.Count == 0
            ? counterId.ToString(System.Globalization.CultureInfo.InvariantCulture)
            : $"{counterId}, [{string.Join(", ", extents.Select(e => e.ToString(System.Globalization.CultureInfo.InvariantCulture)))}], "
              + $"[{string.Join(", ", subscripts.Select(s => $"(long)({s})"))}]";

    /// <summary>Read a SUM counter's content, unscaled at the counter's own scale (ISO §13.18.54.4 GR1/GR4) —
    /// <c>CobolReport.SumValue</c>. <paramref name="counterAddress"/> is <see cref="ReportSumAddress"/>'s.
    /// <para>The engine carries every counter in an <see cref="Int128"/> (kb/Work PB1509/PB1560/PB1666); the read
    /// lands it in <paramref name="clrType"/>, the counter's OWN carrier (<c>PicInfo.ClrType</c> of its GR1
    /// profile — <c>long</c> up to 18 digits, <c>Int128</c> beyond), so every consumer sees the type any other
    /// numeric item of that profile has. The narrowing is exact: the engine never holds a value past the
    /// counter's digits (its GR3 size-error test), and a procedure-division write arrives stored through the
    /// same profile.</para></summary>
    public static string ReportSumRead(int reportIndex, int depth, string counterAddress, string clrType)
    {
        string read = $"{ReportEngine(reportIndex, depth)}.{nameof(CobolReport.SumValue)}({counterAddress})";
        return clrType == "Int128" ? read : $"(({clrType}){read})";
    }

    /// <summary>A report writer OCCURS … DEPENDING repetition count (ISO §13.18.38.4 GR13) —
    /// <c>CobolReport.DependingCount</c> over data-name-1's integer value, evaluated once.</summary>
    public static string ReportDependingCount(string valueExpr, int minOccurs, int maxOccurs) =>
        $"{nameof(CobolReport)}.{nameof(CobolReport.DependingCount)}({valueExpr}, {minOccurs}, {maxOccurs})";

    /// <summary>The prior-control key of a floating-point CONTROL item (ISO §13.18.16.4 GR3; kb/Work PB1234) —
    /// <c>CobolReport.FloatControlKey</c>, the item's exact bit pattern, over its OWN carrier value
    /// (<paramref name="carrier"/> is a <c>float</c> or a <c>double</c> expression; the overload follows it).</summary>
    public static string ReportFloatControlKey(string carrier) =>
        $"{nameof(CobolReport)}.{nameof(CobolReport.FloatControlKey)}({carrier})";

    /// <summary>The value a floating-point CONTROL item's key holds, on the item's own carrier —
    /// <c>CobolReport.FloatControlSingle</c> / <c>FloatControlDouble</c> (the GR4 a) restore half).</summary>
    public static string ReportFloatControlValue(string key, bool single) =>
        $"{nameof(CobolReport)}.{(single ? nameof(CobolReport.FloatControlSingle) : nameof(CobolReport.FloatControlDouble))}({key})";

    /// <summary>The break test of a floating-point CONTROL item — <c>CobolReport.FloatControlEqual</c> as a method
    /// group for the engine's equality delegate.</summary>
    public static string ReportFloatControlEqual => $"{nameof(CobolReport)}.{nameof(CobolReport.FloatControlEqual)}";

    /// <summary>A report VARYING FROM/BY value landed as the Int128 integer ISO §13.18.64.4 GR1 makes the counter,
    /// raising EC-REPORT-VARYING for a noninteger value (GR5) — <c>CobolReport.VaryingInteger</c>. The argument
    /// list is the value's own lane: <c>(unscaled, scale, detail)</c> for fixed point, <c>(value, detail)</c> for
    /// a float or a standard-decimal intermediate (kb/Work PB1305).</summary>
    public static string ReportVaryingInteger(string args) =>
        $"{nameof(CobolReport)}.{nameof(CobolReport.VaryingInteger)}({args})";

    /// <summary>Alter a SUM counter's content from the procedure division (ISO §13.18.54.4 GR12) —
    /// <c>CobolReport.SetSumValue</c>, at the counter's own scale, widened to the engine's Int128 carrier.
    /// <paramref name="counterAddress"/> is <see cref="ReportSumAddress"/>'s.</summary>
    public static string ReportSumWrite(int reportIndex, int depth, string counterAddress, string valueExpr) =>
        $"{ReportEngine(reportIndex, depth)}.{nameof(CobolReport.SetSumValue)}({counterAddress}, (Int128)({valueExpr}));";

    /// <summary>Decode a DISPLAY image back into a native numeric leaf, preserving unset positions from the
    /// current value — <c>CobolNum.StoreDisplay</c>.</summary>
    public static string NumStoreDisplay(string image, string profile, string current) =>
        $"{nameof(CobolNum)}.{nameof(CobolNum.StoreDisplay)}({image}, {profile}, {current})";

    /// <summary>Decode a spliced RECORD IMAGE back into a numeric leaf — <c>CobolNum.StoreImage</c>, the write
    /// half of <see cref="NumFormatImage"/> (the <paramref name="current"/> dummy selects the storage form's
    /// conversion, exactly as <see cref="NumStoreDisplay"/> does).</summary>
    public static string NumStoreImage(string image, string profile, string current) =>
        $"{nameof(CobolNum)}.{nameof(CobolNum.StoreImage)}({image}, {profile}, {current})";

    // ── Inter-program ABI (CobolArgAdapt / CobolPassMode; interprogram design D1/D2) — Step 9-final sweep ──

    /// <summary>A LINKAGE formal's numeric carrier adoption — <c>CobolArgAdapt.Num&lt;T&gt;</c> over the
    /// formal's OWN carrier type (kb/Work R12 — the cell type is the field type, so a wide or unsigned formal's
    /// carrier-typed reads compile and carry the full container range).</summary>
    public static string ArgAdaptNum(string args, int position, string profile, string scale, string carrier = "long") =>
        $"{nameof(CobolArgAdapt)}.{nameof(CobolArgAdapt.Num)}<{carrier}>({args}, {position}, {profile}, {scale})";

    /// <summary>A LINKAGE formal's text carrier adoption — <c>CobolArgAdapt.Text</c>. <paramref name="groupAtoms"/>
    /// is a GROUP formal's §8.5.1.12 atoms expression (kb/Work PB965, PB2280), null for any other formal;
    /// <paramref name="unbound"/> is the factory of the formal's unbound value, which an OMITTED argument's carrier
    /// reads (kb/Work PB992, PB2671).</summary>
    public static string ArgAdaptText(string args, int position, string width, string? groupAtoms, string unbound) =>
        $"{nameof(CobolArgAdapt)}.{nameof(CobolArgAdapt.Text)}({args}, {position}, {width}, {groupAtoms ?? "null"}, {unbound})";

    /// <summary>A DYNAMIC LENGTH formal's text carrier adoption — <c>CobolArgAdapt.DynText</c> (ISO §13.18.19;
    /// §14.2.3 GR9's second-regime dynamic-length record — kb/Work PB165).</summary>
    public static string ArgAdaptDynText(string args, int position, string limit) =>
        $"{nameof(CobolArgAdapt)}.{nameof(CobolArgAdapt.DynText)}({args}, {position}, {limit})";

    // ── The VARIABLE-LENGTH GROUP boundary carrier (ISO §8.5.1.12; kb/Work PB204). A third crossing form
    //    beside the native cell and the character image, because a variable-length group has neither a fixed
    //    record window nor an invertible flat image — see CobolVarGroup.

    /// <summary>The C# type of the variable-length-group boundary carrier.</summary>
    public static string VarGroupType => nameof(CobolVarGroup);

    /// <summary>A variable-length record type's contiguous-image layout — <c>new CobolContiguousLayout(…)</c>, the
    /// ONE object that both decomposes a record read back (determination D-FRA; kb/Work PB981) and locates a key
    /// a variable-length member precedes (D-KWV; kb/Work PB1025). Emitted once per record type as the static
    /// field <see cref="ContiguousLayoutField"/>.</summary>
    public static string ContiguousLayoutNew(int fixedTotal, IEnumerable<int> fixedAt,
        IEnumerable<int> unit, IEnumerable<long> maxUnits, IEnumerable<int> structure, bool odoTail = false) =>
        $"new {nameof(CobolContiguousLayout)}({fixedTotal}, "
        + $"new int[] {{ {string.Join(", ", fixedAt)} }}, new int[] {{ {string.Join(", ", unit)} }}, "
        + $"new long[] {{ {string.Join(", ", maxUnits.Select(m => $"{m}L"))} }}"
        + (structure.Any(c => c != 0) ? $", new int[] {{ {string.Join(", ", structure)} }}" : "")
        + (odoTail ? ", OdoTail: true" : "") + ")";   // the constructor parameter of that name

    /// <summary>The carrier of a fixed-OCCURS table of variable-length group elements: every occurrence's
    /// <c>AsVarImage()</c> concatenated, fixed runs and components in occurrence order (<c>CobolVarGroup.Concat</c>).</summary>
    public static string VarGroupConcat(string carriersExpr) =>
        $"{nameof(CobolVarGroup)}.{nameof(CobolVarGroup.Concat)}({carriersExpr})";

    /// <summary>The carrier of the first <paramref name="countExpr"/> occurrences of an OCCURS DEPENDING table of
    /// variable-length group elements (<c>CobolTable.ConcatVarImages</c>, kb/Work PB244): every occurrence's
    /// <paramref name="carrierLambda"/> (its <c>AsVarImage</c>) concatenated, fixed runs and components in
    /// occurrence order — <see cref="VarGroupConcat"/> at the count a statement's operand names.</summary>
    public static string TableConcatVarImages(string tableExpr, string countExpr, string carrierLambda) =>
        $"{nameof(CobolTable)}.{nameof(CobolTable.ConcatVarImages)}({tableExpr}, {countExpr}, {carrierLambda})";

    /// <summary>The current-extent image of an OCCURS DEPENDING table whose elements are variable-length groups
    /// (<c>CobolTable.ConcatImages</c>, kb/Work PB244): the first <paramref name="countExpr"/> occurrences of
    /// <paramref name="tableExpr"/>, each rendered by the element lambda <paramref name="imageLambda"/>.</summary>
    public static string TableConcatImages(string tableExpr, string countExpr, string imageLambda) =>
        $"{nameof(CobolTable)}.{nameof(CobolTable.ConcatImages)}({tableExpr}, {countExpr}, {imageLambda})";

    /// <summary>A dynamic-length ELEMENTARY RECORD's image framed by its DYNAMIC LENGTH STRUCTURE — ISO §12.3.7.4 GR18
    /// length field, data, GR19 delimiter (<c>CobolDynStructure.FrameWith</c>; kb/Work PB1094). A group record frames
    /// each member through its layout (<c>RecordExtents.MediumImage</c>) instead.</summary>
    public static string DynStructureFrame(int code, string contentExpr) =>
        $"{nameof(CobolDynStructure)}.{nameof(CobolDynStructure.FrameWith)}({code}, {contentExpr})";

    /// <summary>The inverse of <see cref="DynStructureFrame"/> — the data of the READ record
    /// (<c>CobolDynStructure.UnframeWith</c>).</summary>
    public static string DynStructureUnframe(int code, string recordExpr) =>
        $"{nameof(CobolDynStructure)}.{nameof(CobolDynStructure.UnframeWith)}({code}, {recordExpr})";

    /// <summary>The static field of a variable-length group's record struct that holds its
    /// <see cref="ContiguousLayoutNew"/> layout.</summary>
    public const string ContiguousLayoutField = "__contiguous";

    /// <summary>The instance property that exposes <see cref="ContiguousLayoutField"/> through a record VALUE, so a
    /// key registration can name it from the record's place without knowing the struct's type name.</summary>
    public const string ContiguousLayoutProperty = "__Contiguous";

    /// <summary><paramref name="record"/>'s layout, read through its place — a SORT/MERGE key or an indexed key a
    /// variable-length member precedes (kb/Work PB1025).</summary>
    public static string ContiguousLayoutOf(string record) => $"{record}.{ContiguousLayoutProperty}";

    /// <summary>A carrier's <c>Elements</c> as a list of one entry per component (null for a plain one) — what a group
    /// splices in for a nested variable-length group's carrier (kb/Work PB2496): <c>CobolVarGroup.ElementList</c>.</summary>
    public static string VarGroupElementList(string carrier) => $"{carrier}.{nameof(CobolVarGroup.ElementList)}";

    /// <summary>The empty carrier value (an unbound formal's seed).</summary>
    public static string VarGroupEmpty => $"{nameof(CobolVarGroup)}.{nameof(CobolVarGroup.Empty)}";

    /// <summary>A FIXED-length group's record image as a §8.5.1.12 carrier in its OWN shape — no components, the whole
    /// image its fixed run (kb/Work PB2496); a statement pairs it with a variable-length group by the two shapes
    /// (<see cref="VarGroupReshape"/>, <see cref="VarGroupCompare"/>).</summary>
    public static string VarGroupOfImage(string image) => $"new {nameof(CobolVarGroup)}({image}, [])";

    /// <summary>A FIXED-length group's decomposition into the same carrier (ISO §8.5.1.12.3 sentence 3 /
    /// §14.6.9.1 — its table is treated as a dynamic-capacity table of its fixed or DEPENDING count), so a fixed
    /// group can stand on the other side of a §14.9.25.4 GR9 move (kb/Work PB393). <paramref name="spans"/> is
    /// the flat (offset, width) pair list from <c>VariableLengthCompatibility.CorrespondingSpans</c> (the PAIR's §8.5.1.12.2 correspondence).</summary>
    public static string VarGroupFromFixedImage(string image, string spans) =>
        $"{nameof(CobolVarGroup)}.{nameof(CobolVarGroup.FromFixedImage)}({image}, {spans})";

    /// <summary>The ISO §8.8.4.2.17 comparison of two compatible groups' carriers, each in its own shape —
    /// <c>CobolVarGroup.Compare</c>, &lt;0 / 0 / &gt;0; <paramref name="collateArg"/> is the <c>, __COLLATE</c> suffix or
    /// empty (kb/Work PB1467, PB2496).</summary>
    public static string VarGroupCompare(string left, GroupAtom[] leftShape, string right, GroupAtom[] rightShape,
                                         string collateArg) =>
        $"{nameof(CobolVarGroup)}.{nameof(CobolVarGroup.Compare)}({left}, {GroupAtomsNew(leftShape)}, {right}, "
        + $"{GroupAtomsNew(rightShape)}{collateArg})";

    /// <summary>The same comparison between two variable-length groups whose shapes cannot be stated (a USAGE BIT leaf)
    /// and so share one layout — <c>CobolVarGroup.CompareInLayout</c> at the variable-length side's component offsets.</summary>
    public static string VarGroupCompareInLayout(string left, string right, string componentOffsets, string collateArg) =>
        $"{nameof(CobolVarGroup)}.{nameof(CobolVarGroup.CompareInLayout)}({left}, {right}, {componentOffsets}{collateArg})";

    /// <summary>A variable-length record type's component offsets in its fixed run —
    /// <c>{group}.__Contiguous.ComponentOffsets</c>.</summary>
    public static string VarGroupComponentOffsets(string groupRead) =>
        $"{groupRead}.{ContiguousLayoutProperty}.{nameof(CobolContiguousLayout.ComponentOffsets)}";

    /// <summary>The inverse of <see cref="VarGroupFromFixedImage"/> — rebuild the fixed group's record image.</summary>
    public static string VarGroupToFixedImage(string carrier, int totalWidth, string spans) =>
        $"{nameof(CobolVarGroup)}.{nameof(CobolVarGroup.ToFixedImage)}({carrier}, {totalWidth}, {spans})";

    /// <summary>A fixed-length formal's store back over a variable-length argument's storage (§14.2.3 GR8;
    /// kb/Work PB965) — <c>CobolVarGroup.OverlayFixedImage</c>.</summary>
    public static string VarGroupOverlayFixedImage(string current, string image, string spans) =>
        $"{nameof(CobolVarGroup)}.{nameof(CobolVarGroup.OverlayFixedImage)}({current}, {image}, {spans})";

    /// <summary>A variable-length group's carrier rebuilt in the shape of another, compatible one (ISO §8.5.1.12;
    /// kb/Work PB480) — <c>CobolVarGroup.Reshape</c>.</summary>
    public static string VarGroupReshape(string carrier, GroupAtom[] from, GroupAtom[] to) =>
        $"{nameof(CobolVarGroup)}.{nameof(CobolVarGroup.Reshape)}({carrier}, {GroupAtomsNew(from)}, {GroupAtomsNew(to)})";

    /// <summary>A variable-length view's store back over the variable-length storage it views (ISO §14.2.3 GR8;
    /// kb/Work PB480) — <c>CobolVarGroup.Overlay</c>.</summary>
    public static string VarGroupOverlay(string current, string view, GroupAtom[] currentShape, GroupAtom[] viewShape) =>
        $"{nameof(CobolVarGroup)}.{nameof(CobolVarGroup.Overlay)}({current}, {view}, {GroupAtomsNew(currentShape)}, "
        + $"{GroupAtomsNew(viewShape)})";

    /// <summary>A DETACHED variable-length carrier cell (BY CONTENT / BY VALUE, §14.2.3 GR9/GR10).</summary>
    public static string VarGroupCell(string value) => $"ManagedPointer<{VarGroupType}>.Cell({value})";

    /// <summary>An ALIASING variable-length carrier over the caller's storage (BY REFERENCE, §14.2.3 GR8).</summary>
    public static string VarGroupOverField(string get, string set) =>
        $"ManagedPointer<{VarGroupType}>.OverField(() => {get}, __v => {{ {set} }})";

    /// <summary>A LINKAGE formal's variable-length carrier adoption — <c>CobolArgAdapt.VarGroup</c>.</summary>
    public static string ArgAdaptVarGroup(string args, int position, string formalAtoms) =>
        $"{nameof(CobolArgAdapt)}.{nameof(CobolArgAdapt.VarGroup)}({args}, {position}, {formalAtoms})";

    /// <summary>The BY VALUE / BY CONTENT twin — <c>CobolArgAdapt.VarGroupValue</c> (§14.2.3 GR9/GR10).</summary>
    public static string ArgAdaptVarGroupValue(string args, int position, string formalAtoms) =>
        $"{nameof(CobolArgAdapt)}.{nameof(CobolArgAdapt.VarGroupValue)}({args}, {position}, {formalAtoms})";

    // ── The MANAGED-SLOT boundary carrier (class pointer / class object-reference; kb/Work PB663). The FOURTH
    //    crossing form: its value is a managed reference with no byte image, so neither the native numeric cell
    //    nor the character image can carry it — see CobolArgAdapt.Slot.

    /// <summary>A LINKAGE formal of class pointer or object-reference adopting the caller's carrier —
    /// <c>CobolArgAdapt.Slot&lt;T&gt;</c> over the formal's OWN <c>PicInfo.ClrType</c> (ISO §14.2.3 GR8's
    /// "same storage area"; §14.8.2.3.2 forces the same category/class on both sides, so T matches).</summary>
    public static string ArgAdaptSlot(string args, int position, string carrier) =>
        $"{nameof(CobolArgAdapt)}.{nameof(CobolArgAdapt.Slot)}<{carrier}>({args}, {position})";

    /// <summary>The BY VALUE / BY CONTENT twin — <c>CobolArgAdapt.SlotValue&lt;T&gt;</c> (ISO §14.2.3 GR10's
    /// detached record, filled by "a SET statement" when the formal is of class object or pointer).</summary>
    public static string ArgAdaptSlotValue(string args, int position, string carrier) =>
        $"{nameof(CobolArgAdapt)}.{nameof(CobolArgAdapt.SlotValue)}<{carrier}>({args}, {position})";

    /// <summary>A BY VALUE numeric formal's DETACHED value-copy cell (ISO §14.2.3 GR10 — stores never reach
    /// the caller) — <c>CobolArgAdapt.NumValue&lt;T&gt;</c> over the formal's carrier (see
    /// <see cref="ArgAdaptNum"/>).</summary>
    public static string ArgAdaptNumValue(string args, int position, string profile, string scale, string carrier = "long") =>
        $"{nameof(CobolArgAdapt)}.{nameof(CobolArgAdapt.NumValue)}<{carrier}>({args}, {position}, {profile}, {scale})";

    /// <summary>A BY VALUE image-carried formal's DETACHED value-copy cell (§14.2.3 GR10, image form) —
    /// <c>CobolArgAdapt.TextValue</c>. <paramref name="profile"/> / <paramref name="scale"/> are the FORMAL's
    /// description (the record GR10's COMPUTE fills — kb/Work PB873), <c>"null"</c> / <c>"0"</c> for a formal
    /// with no numeric description; <paramref name="unbound"/> is the factory of the formal's unbound value, which an
    /// OMITTED argument's carrier reads (kb/Work PB2671).</summary>
    public static string ArgAdaptTextValue(string args, int position, string width, string profile, string scale,
        string? groupAtoms, string unbound) =>
        $"{nameof(CobolArgAdapt)}.{nameof(CobolArgAdapt.TextValue)}({args}, {position}, {width}, {profile}, {scale}, {groupAtoms ?? "null"}, {unbound})";

    /// <summary>The §8.5.1.12 atoms argument of a TABLE-LESS group formal — the empty array, which the adapter
    /// reads as <c>CobolVarGroup.FixedRun(width)</c> (kb/Work PB965, PB2280); allocation-free at every activation.</summary>
    public const string NoTableGroupAtoms = "System.Array.Empty<GroupAtom>()";

    /// <summary>The ACTIVATING element's §14.2.3 GR9/GR10 argument crossing — <c>CobolArgAdapt.LandForFormal</c>
    /// wrapped around a built <c>CobolArg</c> (kb/Work PB640). <paramref name="carrier"/> is the FORMAL's
    /// <c>PicInfo.ClrType</c> and <paramref name="profile"/> / <paramref name="scale"/> come from the same
    /// <c>PicInfo</c>, so the landing's rescale target and its capacity discipline cannot disagree;
    /// <paramref name="checking"/> is this statement's <c>&gt;&gt;TURN EC-SIZE</c> state, which selects the
    /// raising kernel exactly as the arithmetic store's <c>checkedLanding</c> does.</summary>
    public static string ArgLandForFormal(string arg, string profile, string scale, string carrier, bool checking) =>
        $"{nameof(CobolArgAdapt)}.{nameof(CobolArgAdapt.LandForFormal)}<{carrier}>({arg}, {profile}, {scale}, "
        + $"checking: {(checking ? "true" : "false")})";

    /// <summary>The argument-present probe (OMITTED handling, §14.2.3) — <c>CobolArgAdapt.Present</c>.</summary>
    public static string ArgAdaptPresent(string args, int position) =>
        $"{nameof(CobolArgAdapt)}.{nameof(CobolArgAdapt.Present)}({args}, {position})";

    /// <summary>The storage of an AREA formal (§14.2.3 GR8; kb/Work PB2087) — <c>CobolArgAdapt.Area</c>: the argument's
    /// own cell area when it can hold <paramref name="areaWidth"/> positions BY REFERENCE, else a fresh cell.</summary>
    public static string ArgAdaptArea(string args, int position, int areaWidth, bool byValueFormal, string formalShape, string fresh) =>
        $"{nameof(CobolArgAdapt)}.{nameof(CobolArgAdapt.Area)}({args}, {position}, {areaWidth}, {(byValueFormal ? "true" : "false")}, {formalShape}, {fresh})";

    /// <summary>The storage of a METHOD's area formal (§14.2.3 GR8; kb/Work PB2087) — <c>CobolArgAdapt.Area</c>'s one
    /// decision over the method ABI's area parameter and presence: the argument's own area when it can hold
    /// <paramref name="areaWidth"/> positions and the formal can be laid over it, else a fresh cell.
    /// <paramref name="formalShape"/> is a variable-length formal's atoms ("null" for every other formal) and
    /// <paramref name="fresh"/> the factory of the category-default cell every area formal takes when it is not laid
    /// over its argument (kb/Work PB2094, PB2671; <c>DataEmitter.AreaFormalShape</c>).</summary>
    public static string ArgAdaptAreaOf(string sharedArea, string present, int areaWidth, string formalShape, string fresh) =>
        $"{nameof(CobolArgAdapt)}.{nameof(CobolArgAdapt.Area)}({sharedArea}, {present}, {areaWidth}, {formalShape}, {fresh})";

    /// <summary>The storage of an AREA formal at the main-program entry (ISO §13.7.4 GR3/GR5; kb/Work PB2671) —
    /// <c>CobolArgAdapt.Unbound</c>: <paramref name="carrier"/> unchanged when an earlier Call set it, else a fresh area
    /// from <paramref name="fresh"/>, the formal's category-default cell.</summary>
    public static string ArgAdaptUnboundArea(string carrier, int areaWidth, string formalShape, string fresh) =>
        $"{nameof(CobolArgAdapt)}.{nameof(CobolArgAdapt.Unbound)}({carrier}, {areaWidth}, {formalShape}, {fresh})";

    /// <summary>The storage area a carrier-resident BY REFERENCE formal occupies (§14.2.3 GR8; kb/Work PB2089) —
    /// <c>CobolArgAdapt.ArgumentArea</c>: its argument's <c>CobolArg.Area</c>, or null.</summary>
    public static string ArgAdaptArgumentArea(string args, int position) =>
        $"{nameof(CobolArgAdapt)}.{nameof(CobolArgAdapt.ArgumentArea)}({args}, {position})";

    /// <summary>Is an area formal's storage its argument's own (kb/Work PB2087)? — <c>CobolArgAdapt.Aliased</c>.</summary>
    public static string ArgAdaptAliased(string args, int position, string area) =>
        $"{nameof(CobolArgAdapt)}.{nameof(CobolArgAdapt.Aliased)}({args}, {position}, {area})";

    /// <summary>The storage area of a cell-backed argument (kb/Work PB2087): a data pointer at <paramref name="offset"/>
    /// characters into <paramref name="cell"/> — the <c>CobolArg.Area</c> an area formal is laid over. A variable-length
    /// group's area also states the component ordinal it begins at and its §8.5.1.12 atoms (<paramref name="dynBase"/>,
    /// <paramref name="shape"/>; kb/Work PB2094), so a formal of the same storage numbers its components from there.</summary>
    public static string ArgArea(string cell, string offset, string? dynBase = null, string? shape = null) =>
        dynBase is null
            ? $"new {nameof(CellPointer)}({cell}, {offset})"
            : $"new {nameof(CellPointer)}({cell}, {offset}) {{ {nameof(CellPointer.DynBase)} = (int)({dynBase}), {nameof(CellPointer.Shape)} = {shape} }}";

    /// <summary>The BY CONTENT record of a group whose values ride managed slots (§14.2.3 GR9; kb/Work PB1940) —
    /// <c>CobolArgAdapt.ContentRecord</c>: a detached copy of <paramref name="width"/> positions of <paramref name="area"/>.</summary>
    public static string ArgAdaptContentRecord(string area, int width) =>
        $"{nameof(CobolArgAdapt)}.{nameof(CobolArgAdapt.ContentRecord)}({area}, {width})";

    /// <summary>The RETURNING delivery of a group whose values ride managed slots (§14.6.5; kb/Work PB1940) —
    /// <c>CobolArgAdapt.StoreReturnArea</c>: the returning item's area <paramref name="source"/> into the receiver's.</summary>
    public static string ArgAdaptStoreReturnArea(string ret, string source, int width) =>
        $"{nameof(CobolArgAdapt)}.{nameof(CobolArgAdapt.StoreReturnArea)}({ret}, {source}, {width})";

    /// <summary>RETURNING delivery into the caller's item (§14.6.5) — <c>CobolArgAdapt.StoreReturn</c>.
    /// <paramref name="description"/> is the SENDING item's description when it has one — its
    /// <c>NumProfile</c> field, or a variable-length group's atoms array (kb/Work PB962/PB965, PB2280).</summary>
    public static string ArgAdaptStoreReturn(string ret, string value, string? description = null) =>
        $"{nameof(CobolArgAdapt)}.{nameof(CobolArgAdapt.StoreReturn)}({ret}, {value}{(description is null ? "" : $", {description}")})";

    /// <summary>The initial image of an ANY LENGTH RETURNING item (§13.18.2.4 GR1 b)) — <c>CobolArgAdapt.ReturningSeed</c>:
    /// The initial character is <see cref="AnyLengthFill"/>; the PICTURE length is used when the activation has no
    /// receiver (kb/Work PB1167).</summary>
    public static string ArgAdaptReturningSeed(string ret, PicInfo pic) =>
        $"{nameof(CobolArgAdapt)}.{nameof(CobolArgAdapt.ReturningSeed)}({ret}, {AnyLengthFill(pic)}, {pic.Length})";

    /// <summary>The C# char literal of an ANY LENGTH item's initial character — a clear bit for a boolean item, a
    /// space otherwise (the alphanumeric and national initial state). ONE spelling for the program ABI's
    /// <see cref="ArgAdaptReturningSeed"/> and the method ABI's <c>__retLen</c> seed (kb/Work PB1167).</summary>
    public static string AnyLengthFill(PicInfo pic) => pic.Category is PicCategory.Boolean ? "'0'" : "' '";

    /// <summary>A fixed-length group returning item with a table — <c>CobolArgAdapt.StoreReturnGroup</c>: its
    /// image plus its §8.5.1.12 atoms (kb/Work PB965, PB2280).</summary>
    public static string ArgAdaptStoreReturnGroup(string ret, string image, string atoms) =>
        $"{nameof(CobolArgAdapt)}.{nameof(CobolArgAdapt.StoreReturnGroup)}({ret}, {image}, {atoms})";

    /// <summary>The emitted-text reference to a <see cref="CobolPassMode"/> value — <c>nameof</c>-anchored like
    /// <see cref="RoundingText"/>, so a member rename breaks HERE, never the generated text.</summary>
    public static string PassModeText(CobolPassMode mode) => $"{nameof(CobolPassMode)}.{mode}";

    /// <summary><c>CobolArg.Unstated</c> — a boundary item with no fixed character length (kb/Work PB1040): the ONE
    /// spelling the emitters compare a <c>CallEmitter.BoundaryLength</c> against.</summary>
    public const int UnstatedBoundaryLength = CobolArg.Unstated;

    /// <summary>A registered formal that states nothing (<see cref="BoundaryItem"/>): neither compared nor landed, and present
    /// only so the registered array stays positional.</summary>
    public const string UnstatedBoundaryItem = "new " + nameof(BoundaryItem) + "(null)";

    /// <summary>The registered CARRIER of a numeric formal (<see cref="CobolNet.Runtime.CarrierLanding{T}"/>; kb/Work PB2549):
    /// <paramref name="clrType"/> is the formal's <c>PicInfo.ClrType</c>, the type argument <see cref="ArgLandForFormal"/>
    /// lands with.</summary>
    public static string CarrierLanding(string clrType) =>
        $"{nameof(CobolNet.Runtime.CarrierLanding)}<{clrType}>.{nameof(CobolNet.Runtime.CarrierLanding<long>.Instance)}";

    /// <summary>The carrier of the predefined NULL written as a CALL / function-activation argument (kb/Work PB1630;
    /// <see cref="PredefinedNullArgument"/> states the rule).</summary>
    public static string PredefinedNullArgumentCarrier =>
        $"{nameof(PredefinedNullArgument)}.{nameof(PredefinedNullArgument.Instance)}";

    // ── Run-unit lifecycle (CobolFile) ──

    /// <summary>Run-unit file-subsystem init (the entry wrapper's Main) — <c>CobolFile.Init</c>. (The matching
    /// §14.6.11 run-unit-termination implicit CLOSE is runtime-side — <see cref="Runtime.ProgramTable.RunMain"/>'s
    /// finally — so a separately-compiled module's open files are closed even when this main group declares none.)</summary>
    public static string FileInit() => $"{nameof(CobolFile)}.{nameof(CobolFile.Init)}()";

    /// <summary>Mint a per-object instance-file connector key (§9.1.4) — <c>CobolFile.MintInstanceKey</c>.</summary>
    public static string FileMintInstanceKey(string baseKeyLiteral) =>
        $"{nameof(CobolFile)}.{nameof(CobolFile.MintInstanceKey)}({baseKeyLiteral})";

    // ── Pointers / objects ──

    /// <summary>Dereference a data-address pointer to its storage cell (GR3/GR4 loud) — <c>CobolPtr.Deref</c>.</summary>
    public static string PtrDeref(string ptr, string classWidth) =>
        $"{nameof(CobolPtr)}.{nameof(CobolPtr.Deref)}({ptr}, {classWidth})";

    /// <summary>The character offset a data-address pointer currently points at in its storage cell — the run-time
    /// displacement of a BASED item's window (<c>CobolPtr.OffsetOf</c>; <c>Binding.Model.PositionPointerOffset</c>).</summary>
    public static string PtrOffsetOf(string ptr) => $"{nameof(CobolPtr)}.{nameof(CobolPtr.OffsetOf)}({ptr})";

    /// <summary>The first cell component a data-address pointer currently addresses — the run-time base of a BASED or
    /// area-formal class's component ordinals (<c>CobolPtr.DynBaseOf</c>; <c>Binding.Model.PositionPointerDynBase</c>,
    /// kb/Work PB2094).</summary>
    public static string PtrDynBaseOf(string ptr) => $"{nameof(CobolPtr)}.{nameof(CobolPtr.DynBaseOf)}({ptr})";

    /// <summary>Read a pointer-class member's MANAGED SLOT of a shared storage area — <c>CobolPtr.SlotRead</c>
    /// (kb/Work PB231 — the pointer third). A class-pointer / class-object member holds a managed reference and
    /// has no byte image, so its value rides the cell's slot at the same byte offset its RESERVED bytes occupy;
    /// <paramref name="nullStateExpr"/> is the item's own <c>PicInfo.DefaultInitializer</c>, which makes ISO
    /// §14.9.3.4 GR9's "data items of class object or class pointer in the allocated storage are initialized to
    /// null" the value an unwritten slot simply has. The pointer counterpart of <see cref="NatReadWindow"/>.</summary>
    public static string PtrSlotRead(string cellExpr, string byteOffsetExpr, string carrierType, string nullStateExpr) =>
        $"{nameof(CobolPtr)}.{nameof(CobolPtr.SlotRead)}<{carrierType}>({cellExpr}, {byteOffsetExpr}, {nullStateExpr})";

    /// <summary>Store a pointer-class member's MANAGED SLOT — <c>CobolPtr.SlotWrite</c>, the receiving twin of
    /// <see cref="PtrSlotRead"/> (kb/Work PB231). The runtime stores the slot AND the member's 8 positions' pointer
    /// image (kb/Work PB1071); a class-object member has no image and its reserved positions are left alone, so a write
    /// through one description cannot disturb another's characters. <paramref name="carrierType"/> is the member's own
    /// carrier type, as for <see cref="PtrSlotRead"/>.</summary>
    public static string PtrSlotWrite(string cellExpr, string byteOffsetExpr, string carrierType, string valueExpr) =>
        $"{nameof(CobolPtr)}.{nameof(CobolPtr.SlotWrite)}<{carrierType}>({cellExpr}, {byteOffsetExpr}, {valueExpr})";

    /// <summary>The NULL pointer image — <c>PointerImage.NullImage</c>, eight zero positions (DOC-A.1-216): what a
    /// pointer member's reserved positions hold in a freshly seeded shared area (kb/Work PB1071).</summary>
    public static string PointerNullImage() => $"{nameof(PointerImage)}.{nameof(PointerImage.NullImage)}";

    /// <summary>The storage image of a pointer VALUE expression — <c>PointerImage.Of</c> (data-, program- or
    /// function-pointer by the expression's static type; DOC-A.1-216).</summary>
    public static string PointerImageOf(string valueExpr) => $"{nameof(PointerImage)}.{nameof(PointerImage.Of)}({valueExpr})";

    // ── A CELL-BACKED area's VARIABLE-LENGTH half (kb/Work PB1026, PB1042) — StorageCell's component slots ──────────

    /// <summary>The current content of a dynamic-length member of a cell-backed area — <c>StorageCell.DynAt</c>.
    /// <paramref name="ordinal"/> is the component ordinal expression (<c>Place.CellComponents</c>).</summary>
    public static string CellDynRead(string cellExpr, string ordinal) =>
        $"{cellExpr}.{nameof(StorageCell.DynAt)}((int)({ordinal}))";

    /// <summary>The SENDING read of a dynamic-length item through a description that does not own its storage —
    /// <c>CobolDynString.Agree</c>, ISO §14.6.13.2 rule 5's agreement with THIS description's maximum size (kb/Work
    /// PB1118). <paramref name="valueExpr"/> is the plain read; the result is the same content.</summary>
    public static string DynAgree(string valueExpr, int maxSize) =>
        $"{nameof(CobolDynString)}.{nameof(CobolDynString.Agree)}({valueExpr}, {maxSize})";

    /// <summary>Store a dynamic-length member's new content — <c>StorageCell.SetDynAt</c>, the receiving twin of
    /// <see cref="CellDynRead"/>. <paramref name="valueExpr"/> already carries §8.5.1.10.4's receiving rule.</summary>
    public static string CellDynWrite(string cellExpr, string ordinal, string valueExpr) =>
        $"{cellExpr}.{nameof(StorageCell.SetDynAt)}((int)({ordinal}), {valueExpr})";

    /// <summary>A cell-backed area's dynamic-capacity table, asked to agree with the referencing description's FROM
    /// minimum and element width (ISO §14.6.13.2 rule 6) — <c>StorageCell.DynTableAt</c> (kb/Work PB1042).</summary>
    public static string CellDynTable(string cellExpr, string ordinal, int min, int elementWidth) =>
        $"{cellExpr}.{nameof(StorageCell.DynTableAt)}((int)({ordinal}), {min}, {elementWidth})";

    /// <summary>Seed a cell's dynamic-capacity table component — the chained <c>.SeedDynTable(ordinal, table)</c> of a
    /// cell initializer (kb/Work PB1042). <paramref name="tableExpr"/> is the table's construction from its own OCCURS
    /// clause, <c>ValueInitializer.DynTableNew</c> — the one construction site.</summary>
    public static string CellSeedDynTable(int ordinal, string tableExpr) =>
        $".{nameof(StorageCell.SeedDynTable)}({ordinal}, {tableExpr})";

    /// <summary>A C# collection expression of int constants — the <c>ReadOnlySpan&lt;int&gt;</c> layout arguments
    /// of the cell's variable-length group helpers (constant data, so no allocation at the call).</summary>
    private static string IntSpan(IEnumerable<int> xs) => $"[{string.Join(", ", xs)}]";

    /// <summary>A cell-backed variable-length group's CONTIGUOUS image (§8.5.1.11.2) — <c>StorageCell.ContiguousAt</c>.
    /// <paramref name="dynTable"/> is each component's table element width, 0 for a dynamic-length item.</summary>
    public static string CellVarContiguous(string cellExpr, string fixedAtExpr, int width, string dynBase,
                                           IEnumerable<int> dynFixedAt, IEnumerable<int> dynTable,
                                           CellOdoTail odo, string count, IReadOnlyList<CellGroupShape?> dynElem) =>
        $"{cellExpr}.{nameof(StorageCell.ContiguousAt)}({fixedAtExpr}, {width}, (int)({dynBase}), {IntSpan(dynFixedAt)}, {IntSpan(dynTable)}, {CellOdoTailOf(odo)}, {count}{CellElemShapes(dynElem)})";

    /// <summary>The trailing <c>elems</c> argument of <c>StorageCell.ContiguousAt</c>, <c>VarGroupAt</c> and
    /// <c>StoreVarGroupAt</c>: for each component that is a
    /// dynamic-capacity table of VARIABLE-LENGTH elements, the element's window shape (<c>CellGroupShape</c>,
    /// kb/Work PB244); nothing when no component has one.</summary>
    private static string CellElemShapes(IReadOnlyList<CellGroupShape?> shapes) =>
        shapes.All(s => s is null) ? "" : $", [{string.Join(", ", shapes.Select(CellShapeText))}]";

    private static string CellShapeText(CellGroupShape? s) => s is null ? "null"
        : $"new {nameof(CellGroupShape)}({s.Width}, {IntSpan(s.DynFixedAt)}, {IntSpan(s.DynTable)}, {IntSpan(s.DynMax)}"
          + (s.Elems is null || s.Elems.All(e => e is null) ? ")" : $", [{string.Join(", ", s.Elems.Select(CellShapeText))}])");

    /// <summary>The OCCURS DEPENDING table a cell-backed variable-length group holds as its trailing storage
    /// (<c>CellOdoTail</c>, kb/Work PB244), as the argument of the cell's group helpers; <c>default</c> for a group
    /// that holds none. <paramref name="odo"/> is the table's element width and maximum count (zero when absent).</summary>
    private static string CellOdoTailOf(CellOdoTail odo) =>
        odo.Max > 0 ? $"new {nameof(CellOdoTail)}({odo.Elem}, {odo.Max}, {odo.Comps})" : "default";

    /// <summary>Make a contiguous image a cell-backed variable-length group's content — <c>StorageCell.StoreContiguousAt</c>.</summary>
    public static string CellVarStoreContiguous(string cellExpr, string fixedAtExpr, int width, string dynBase,
                                                IEnumerable<int> dynFixedAt, IEnumerable<int> dynMax,
                                                IEnumerable<int> dynStructure, IEnumerable<int> dynTable,
                                                CellOdoTail odo, string imageExpr,
                                                string? extentsExpr = null, bool fixedForm = false) =>
        $"{cellExpr}.{nameof(StorageCell.StoreContiguousAt)}({fixedAtExpr}, {width}, (int)({dynBase}), "
        + $"{IntSpan(dynFixedAt)}, {IntSpan(dynMax)}, {IntSpan(dynStructure)}, {IntSpan(dynTable)}, {CellOdoTailOf(odo)}, {imageExpr}, {extentsExpr ?? "null"}{(fixedForm ? ", true" : "")})";

    /// <summary>A cell-backed variable-length group's EXTENT TABLE — <c>StorageCell.ContiguousExtentsAt</c>
    /// (determination D-FRA (v); kb/Work PB1053). <paramref name="dynStructure"/> is each dynamic-length member's
    /// DYNAMIC LENGTH STRUCTURE code (<c>CobolDynStructure.Code</c>; kb/Work PB1094), 0 for none.</summary>
    public static string CellVarContiguousExtents(string cellExpr, int width, string dynBase,
                                                  IEnumerable<int> dynFixedAt, IEnumerable<int> dynMax,
                                                  IEnumerable<int> dynStructure, IEnumerable<int> dynTable,
                                                  CellOdoTail odo, string count) =>
        $"{cellExpr}.{nameof(StorageCell.ContiguousExtentsAt)}({width}, (int)({dynBase}), {IntSpan(dynFixedAt)}, {IntSpan(dynMax)}, {IntSpan(dynStructure)}, {IntSpan(dynTable)}, {CellOdoTailOf(odo)}, {count})";

    /// <summary>A cell-backed variable-length group's §8.5.1.12 component carrier — <c>StorageCell.VarGroupAt</c>.</summary>
    public static string CellVarCarrier(string cellExpr, string fixedAtExpr, int width, string dynBase,
                                        IEnumerable<int> dynFixedAt, IEnumerable<int> dynTable,
                                        CellOdoTail odo, string count, IReadOnlyList<CellGroupShape?> dynElem) =>
        $"{cellExpr}.{nameof(StorageCell.VarGroupAt)}({fixedAtExpr}, {width}, (int)({dynBase}), {IntSpan(dynFixedAt)}, {IntSpan(dynTable)}, {CellOdoTailOf(odo)}, {count}{CellElemShapes(dynElem)})";

    /// <summary>Distribute a component carrier into a cell-backed variable-length group — <c>StorageCell.StoreVarGroupAt</c>.</summary>
    public static string CellVarStoreCarrier(string cellExpr, string fixedAtExpr, int width, string dynBase,
                                             IEnumerable<int> dynFixedAt, IEnumerable<int> dynMax,
                                             IEnumerable<int> dynTable, CellOdoTail odo, string count, string carrierExpr,
                                             IReadOnlyList<CellGroupShape?> dynElem, bool storage) =>
        $"{cellExpr}.{nameof(StorageCell.StoreVarGroupAt)}({fixedAtExpr}, {width}, (int)({dynBase}), {IntSpan(dynFixedAt)}, {IntSpan(dynMax)}, {IntSpan(dynTable)}, {CellOdoTailOf(odo)}, {count}, {carrierExpr}"
        // the storage flag reaches only the nested elements' stores (the top level's maximum sizes already say it)
        + (dynElem.All(s => s is null) ? ")" : $"{CellElemShapes(dynElem)}{(storage ? ", true" : "")})");

    /// <summary>The INVOKE null-receiver guard (EC-OO-NULL, §14.9.23.4 GR5) — <c>CobolObject.RequireNonNull</c>.</summary>
    public static string ObjRequireNonNull(string receiver) =>
        $"{nameof(CobolObject)}.{nameof(CobolObject.RequireNonNull)}({receiver})";

    /// <summary>The checked narrowing of an object reference delivered back across a UNIVERSAL invocation into a
    /// receiving item of CLR type <paramref name="clrType"/> (no trailing <c>?</c>) — see
    /// <see cref="CobolObject.NarrowUniversal{T}"/>.</summary>
    public static string ObjNarrowUniversal(string clrType, string box, string what) =>
        $"{nameof(CobolObject)}.{nameof(CobolObject.NarrowUniversal)}<{clrType}>({box}, "
        + $"{Microsoft.CodeAnalysis.CSharp.SymbolDisplay.FormatLiteral(what, quote: true)})";

    /// <summary>An object-view's checked re-description (ISO §8.4.3.5.4 GR2–GR6) of <paramref name="source"/> as CLR type
    /// <paramref name="clrType"/> (no trailing <c>?</c>), exact-class when the ONLY phrase was written — see
    /// <see cref="CobolObject.ObjectView{T}"/>.</summary>
    public static string ObjObjectView(string clrType, string source, bool exactClass, string what) =>
        $"{nameof(CobolObject)}.{nameof(CobolObject.ObjectView)}<{clrType}>({source}, "
        + $"{(exactClass ? "true" : "false")}, {Microsoft.CodeAnalysis.CSharp.SymbolDisplay.FormatLiteral(what, quote: true)})";

    /// <summary>An object-reference value AS AN EXCEPTION OBJECT — the <c>CobolObject?</c> the runtime's
    /// exception-object slots take (<c>ExceptionState.SetObject</c> for RAISE identifier-1, §14.9.29.4;
    /// <c>SetPropagatingObject</c> for GOBACK/EXIT RAISING identifier-1, §14.9.18.4 GR1 b) 2.). An
    /// interface-described reference (§13.18.60.4 GR22 c)) is emitted as its C# INTERFACE type, which has no
    /// implicit conversion to the class root, so the explicit reference conversion is written here once for every
    /// raise site (kb/Work PB814: `GOBACK RAISING` and `RAISE` of an interface-typed identifier were both a
    /// Roslyn CS1503 on conforming source). Every object a COBOL reference can hold is a <c>CobolObject</c>, so
    /// the conversion cannot fail at run time.</summary>
    public static string AsExceptionObject(string objectRefExpr) => $"({nameof(CobolObject)}?)({objectRefExpr})";

    /// <summary>A spelled OMITTED argument through a universal receiver (ISO §14.9.23.2; kb/Work PB757) —
    /// <c>CobolInvokeArg.OmittedArgument()</c>.</summary>
    public static string ObjOmittedArgument => $"{nameof(CobolInvokeArg)}.{nameof(CobolInvokeArg.OmittedArgument)}()";

    /// <summary>A reference-modified argument through a universal receiver, described at its EVALUATED length
    /// (ISO §8.4.3.3.4 GR5 c); kb/Work PB480) and boxed LIVE over the slice (§14.2.3 GR8; kb/Work PB2087) —
    /// <c>CobolInvokeArg.ReferenceModified(description, get, set, area)</c>.</summary>
    public static string ObjReferenceModifiedArgument(string descriptionExpr, string getExpr, string setStatement, string areaExpr) =>
        $"{nameof(CobolInvokeArg)}.{nameof(CobolInvokeArg.ReferenceModified)}({descriptionExpr}, () => {getExpr}, __v => {{ {setStatement} }}, {areaExpr})";

    /// <summary>An identifier argument through a universal receiver, boxed LIVE over the argument's own storage
    /// (§14.2.3 GR8; kb/Work PB2087) — <c>new CobolInvokeArg(description, get, set, area)</c>.</summary>
    public static string ObjLiveArgument(string descriptionExpr, string getExpr, string setStatement, string areaExpr) =>
        $"new {nameof(CobolInvokeArg)}({descriptionExpr}, () => {getExpr}, __v => {{ {setStatement} }}, {areaExpr})";

    /// <summary>⛔ THE ONE C# RENDERING OF AN <see cref="ActivationDescription"/> (kb/Work PB480): an object initializer
    /// naming every member that differs from its default, so the generated code rebuilds exactly the description the
    /// compiler's <c>ActivationDescriptions</c> built. Every member is rendered (reflection over the init properties),
    /// so a member added to the description cannot be dropped on its way to the run-time relations.</summary>
    public static string ActivationDescriptionNew(ActivationDescription d)
    {
        if (d.Shape is ActivationShape.Omitted) return $"{nameof(ActivationDescription)}.{nameof(ActivationDescription.Omitted)}";
        var members = new List<string>();
        foreach (var prop in typeof(ActivationDescription).GetProperties(
                     System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance))
        {
            object? v = prop.GetValue(d);
            string? text = v switch
            {
                null => null,
                string s => s.Length == 0 ? null : Microsoft.CodeAnalysis.CSharp.SymbolDisplay.FormatLiteral(s, quote: true),
                bool b => b ? "true" : null,
                int n => n == 0 ? null : n.ToString(System.Globalization.CultureInfo.InvariantCulture),
                int[] a => $"new int[] {{ {string.Join(", ", a.Select(x => x.ToString(System.Globalization.CultureInfo.InvariantCulture)))} }}",
                GroupAtom[] atoms => GroupAtomsNew(atoms),
                Enum e =>Convert.ToInt32(e, System.Globalization.CultureInfo.InvariantCulture) == 0 ? null : $"{e.GetType().Name}.{e}",
                _ => throw new InvalidOperationException(
                    $"ActivationDescription.{prop.Name} is of a type ({v.GetType().Name}) the renderer does not spell"),
            };
            if (text is not null) members.Add($"{prop.Name} = {text}");
        }
        return $"new {nameof(ActivationDescription)} {{ {string.Join(", ", members)} }}";
    }

    /// <summary>The C# expression constructing a group's ISO §8.5.1.12 atoms (<see cref="GroupAtom"/>, element atoms
    /// included) — the ONE rendering of the layout the run-time compatibility walk and carrier reshaping read (kb/Work
    /// PB480).</summary>
    public static string GroupAtomsNew(GroupAtom[] atoms) =>
        $"new {nameof(GroupAtom)}[] {{ {string.Join(", ", atoms.Select(a =>
            $"new {nameof(GroupAtom)}({nameof(GroupAtomKind)}.{a.Kind}, {a.Bytes}, {a.Chars}, {a.ElementBytes}, "
            + $"{a.ElementChars}, {(a.Element is { } e ? GroupAtomsNew(e) : "null")})"))} }}";

    /// <summary>Normalize a runtime method-name value for universal dispatch (D-U6) —
    /// <c>CobolObject.NormalizeMethodName</c>.</summary>
    public static string ObjNormalizeMethodName(string nameExpr) =>
        $"{nameof(CobolObject)}.{nameof(CobolObject.NormalizeMethodName)}({nameExpr})";

    // ── USAGE BIT record images (ISO §13.18.60.4 GR5 / §8.5.1.6.3; design D19, fix-queue PB43) ──
    // The VALUE carrier stays a '0'/'1' string for both boolean usages; only USAGE BIT's IMAGE is packed.

    /// <summary>Pack a bit run's carrier into its packed record image — <c>CobolBits.Pack</c>.</summary>
    public static string BitsPack(string bitsExpr, string countExpr) =>
        $"{nameof(CobolBits)}.{nameof(CobolBits.Pack)}({bitsExpr}, {countExpr})";

    /// <summary>Pack every boolean position the carrier holds — <c>CobolBits.Pack(bits)</c>. For an operand
    /// whose position count is the carrier's own length (a reference-modified usage-bit slice, §8.4.3.3.4 GR5c;
    /// kb/Work PB173), so the slice expression is evaluated once and no count is re-derived at the call site.</summary>
    public static string BitsPackAll(string bitsExpr) =>
        $"{nameof(CobolBits)}.{nameof(CobolBits.Pack)}({bitsExpr})";

    /// <summary>Unpack a packed image slice back to the run's carrier — <c>CobolBits.Unpack</c>.</summary>
    public static string BitsUnpack(string imageExpr, string countExpr) =>
        $"{nameof(CobolBits)}.{nameof(CobolBits.Unpack)}({imageExpr}, {countExpr})";

    /// <summary>The UTF-16BE byte serialization of a national string — <c>CobolBits.NatBytes</c> (D-N1; the ONE
    /// national→bytes reduction, shared with the runtime CONVERT NAT arm — PB59 family 5b's ANY storage channel).</summary>
    public static string NatBytes(string expr) =>
        $"{nameof(CobolBits)}.{nameof(CobolBits.NatBytes)}({expr})";

    /// <summary>Read a Tier-B REDEFINES member's BIT window out of the class's ONE byte backing —
    /// <c>CobolBits.ReadWindow</c>. The §13.18.44.4 GR1 storage association is stated in BITS, and
    /// §13.18.29.4 GR1c sends a bit group's members through §8.5.1.6.3, so a bit member of a redefines class
    /// is located at a BIT offset in the shared area, not a byte one (kb/Work PB203).</summary>
    public static string BitsReadWindow(string imageExpr, string startBitExpr, string countExpr) =>
        $"{nameof(CobolBits)}.{nameof(CobolBits.ReadWindow)}({imageExpr}, {startBitExpr}, {countExpr})";

    /// <summary>Splice a Tier-B REDEFINES member's BIT window back into the class's ONE byte backing, leaving
    /// every other bit of it untouched — <c>CobolBits.WriteWindow</c>, the receiving twin of
    /// <see cref="BitsReadWindow"/> (kb/Work PB203). Leaving the neighbouring bits alone is what makes two
    /// same-level bit members that SHARE a byte (§8.5.1.6.3) independent receivers.</summary>
    public static string BitsWriteWindow(string imageExpr, string startBitExpr, string bitsExpr) =>
        $"{nameof(CobolBits)}.{nameof(CobolBits.WriteWindow)}({imageExpr}, {startBitExpr}, {bitsExpr})";

    /// <summary>The national read of a Tier-B window over the class's one byte backing —
    /// <c>CobolBits.NatReadWindow</c> (kb/Work PB231). A national character position occupies TWO bytes of the
    /// byte-addressed backing (ISO §13.18.60.4 GR8 leaves the size to the implementor; D-N1 pins two), so the
    /// window's bytes are transcoded back to the member's value carrier — the inverse of
    /// <see cref="NatBytes"/>, and the national counterpart of <see cref="BitsReadWindow"/>. The offset is
    /// 0-BASED (the window helpers' convention, unlike the 1-based <see cref="StrRefMod"/>).</summary>
    public static string NatReadWindow(string imageExpr, string startByteExpr, string positionsExpr) =>
        $"{nameof(CobolBits)}.{nameof(CobolBits.NatReadWindow)}({imageExpr}, {startByteExpr}, {positionsExpr})";

    /// <summary>Splice a value into a national window of the class backing, leaving every other byte untouched —
    /// <c>CobolBits.NatWriteWindow</c>, the receiving twin of <see cref="NatReadWindow"/> (kb/Work PB231). The
    /// fit to exactly the member's position count is the helper's, done in POSITIONS before serialization.</summary>
    public static string NatWriteWindow(string imageExpr, string startByteExpr, string positionsExpr, string valueExpr) =>
        $"{nameof(CobolBits)}.{nameof(CobolBits.NatWriteWindow)}({imageExpr}, {startByteExpr}, {positionsExpr}, {valueExpr})";

    /// <summary>One member's slice of an unpacked run carrier — <c>CobolBits.Slice</c>.</summary>
    public static string BitsSlice(string carrierExpr, string offsetExpr, string countExpr) =>
        $"{nameof(CobolBits)}.{nameof(CobolBits.Slice)}({carrierExpr}, {offsetExpr}, {countExpr})";

    /// <summary>Repeat an element image for a table initializer — <c>CobolString.Repeat</c>.</summary>
    public static string StrRepeat(string s, string n) =>
        $"{nameof(CobolString)}.{nameof(CobolString.Repeat)}({s}, {n})";

    // ── Intrinsic functions (CobolIntrinsics / CobolDate / EcFunctions / CobolModule; ISO §15 — P7 Step 12) ──

    /// <summary>A <c>CobolIntrinsics</c> call. <paramref name="method"/> is normally the catalog row's
    /// <c>RuntimeMethod</c> name — <c>IntrinsicCatalog</c> is the single name source, exercised end-to-end by
    /// the intrinsic conformance suite; the TYPE anchor breaks here on a rename.</summary>
    public static string Intrinsic(string method, string args) =>
        $"{nameof(CobolIntrinsics)}.{method}({args})";

    /// <summary>A <c>CobolLocale</c> call (the §15.51–§15.54 LOCALE functions; kb/Work PB64 T4 — same catalog-name discipline).</summary>
    public static string LocaleFn(string method, string args) =>
        $"{nameof(CobolLocale)}.{method}({args})";

    /// <summary>A <c>CobolDate</c> call (the §15 date/time family — same catalog-name discipline).</summary>
    public static string DateFn(string method, string args) =>
        $"{nameof(CobolDate)}.{method}({args})";

    /// <summary>The membership-preserving landing of a FLOAT or SDIDI seconds argument into the formatted-time
    /// family's (unscaled, scale) pair — <c>CobolDate.SecondsOfReal</c> / <c>SecondsOfDec</c> (kb/Work PB1379): both
    /// round toward negative infinity so the §7.3.17.4 floor screen sees a negative value as negative.</summary>
    public static string SecondsLanding(string value, bool dec) =>
        $"{nameof(CobolDate)}.{(dec ? nameof(CobolDate.SecondsOfDec) : nameof(CobolDate.SecondsOfReal))}({value})";

    /// <summary>A last-exception interrogation read (§15.28–15.33) — <c>EcFunctions.{method}(args)</c>.</summary>
    public static string EcFn(string method, string args = "") =>
        $"{nameof(Runtime.Exceptions.EcFunctions)}.{method}({args})";

    /// <summary>The predefined object reference EXCEPTION-OBJECT (ISO §8.4.3.6.4 GR1/GR2 — "the current
    /// exception object", one instance per run unit), read from the run unit's ONE exception state.
    /// <para>⛔ FULLY QUALIFIED, AND THAT IS THE FIX RATHER THAN THE STYLE (kb/Work PB922). Every other
    /// <c>ExceptionState</c> emission is reachable only when the EC model is active or a class exists, which is
    /// exactly when <c>ProgramEmitter</c> writes <c>using CobolNet.Runtime.Exceptions;</c> — but a reference to
    /// this register needs NEITHER: <c>SET U TO EXCEPTION-OBJECT</c> in a declarative-free, class-free program
    /// emitted a bare <c>ExceptionState.ExceptionObject</c> and the backend failed with CS0103 on generated C#
    /// the user cannot see. Qualifying the name here makes the emission independent of the conditional using,
    /// and keeps the zero-scaffolding invariant (SSOT §18.16) intact for programs that reference nothing.</para></summary>
    public static string ExceptionObjectRead { get; } =
        $"CobolNet.Runtime.Exceptions.{nameof(Runtime.Exceptions.ExceptionState)}"
        + $".{nameof(Runtime.Exceptions.ExceptionState.ExceptionObject)}";

    /// <summary>Push a METHOD activation frame (ISO §15.65.4 r5 — "This may be by a CALL statement, an INVOKE
    /// statement, a function reference, or an inline invocation"; fix-queue PB36). Emitted INSIDE the method body
    /// rather than at the INVOKE site, because a method is reached by several paths — a typed direct call, the
    /// universal <c>__CobolInvoke</c> switch, an inline invocation — and a per-site push would be the same
    /// two-arm dispatch this compiler keeps re-learning.</summary>
    public static string ModulePushMethod(string nameLit, string classLit) =>
        $"{nameof(CobolModule)}.{nameof(CobolModule.Push)}({nameLit}, {classLit}, false)";

    /// <summary>The run unit's module stack, resolved once per activation — <c>CobolModule.Stack</c>.</summary>
    public static string ModuleStack() => $"{nameof(CobolModule)}.{nameof(CobolModule.Stack)}";

    /// <summary>A method activation's resource check (ISO §14.9.23.4 GR7 b); kb/Work PB2659) — the head of every method
    /// body that executes statements: <c>ActivationStack.RequireForMethod("M", "C")</c>, EC-OO-METHOD when the stack the
    /// activation needs is not available. <paramref name="methodLiteral"/> and <paramref name="classLiteral"/> are C#
    /// string literals.</summary>
    public static string MethodActivationResources(string methodLiteral, string classLiteral) =>
        $"{nameof(ActivationStack)}.{nameof(ActivationStack.RequireForMethod)}({methodLiteral}, {classLiteral})";

    /// <summary>Pop the activation frame pushed by <see cref="ModulePushMethod"/> — always in a finally.</summary>
    public static string ModulePop() => $"{nameof(CobolModule)}.{nameof(CobolModule.Pop)}()";

    /// <summary>FUNCTION MODULE-NAME's runtime read (§15.65) — <c>CobolModule.Name(kind)</c>.</summary>
    public static string ModuleNameFn(int kind) =>
        $"{nameof(CobolModule)}.{nameof(CobolModule.Name)}({kind})";

    /// <summary>§14.9.12.4 GR6c's subsidiary-quotient digit cap (kb/Work PB129) — the low-order digits
    /// at the GIVING receiver's digit capacity, the §14.7.5 no-phrase store's own disposition.</summary>
    public static string NumCapDigits(string expr, int digits) =>
        $"{nameof(CobolNum)}.{nameof(CobolNum.CapDigits)}({expr}, {digits})";

    /// <summary>The COMPILE-TIME WHEN-COMPILED stamp format (a typed passthrough like <see cref="MaskScale"/>):
    /// the §15.99.3 r2 compilation timestamp is baked as a constant with the SAME runtime formatter the
    /// generated CURRENT-DATE call uses.</summary>
    public static string DateFormat21(DateTimeOffset t) => CobolDate.Format21(t);

    /// <summary>The COMPILE-TIME fractional-second count of a literal time format (§15.79 — the result scale
    /// is format-derived at compile time), through the ONE runtime format analyzer.</summary>
    public static int DateFormatFractionDigits(string format) => CobolDate.FormatFractionDigits(format);

    /// <summary>The COMPILE-TIME mask-scale computation (a typed passthrough, not a fragment): the emitters
    /// compute a numeric-edited receiver's fraction scale from its edit mask at compile time with the SAME
    /// runtime routine the generated code uses — one definition, anchored here. It takes the <see cref="PicInfo"/>
    /// rather than the bare mask so the item's PICTURE EDITING rules always ride along: a FLOATING extended
    /// editing sign control symbol's repetitions are digit positions (§13.18.40.5 rule 6) and the mask alone
    /// cannot say so (kb/Work PB491).</summary>
    public static int MaskScale(PicInfo pic, string mask, char currency) =>
        CobolEdit.MaskScale(mask, currency, pic.DecimalPointIsComma, pic.EditingRules as CobolEdit.EditRule[]);

    /// <summary>ISO §13.18.8.4 GR3's content test over an operand's image — <c>CobolEdit.IsBlanked</c>.</summary>
    public static string EditIsBlanked(string read) => $"{nameof(CobolEdit)}.{nameof(CobolEdit.IsBlanked)}({read})";

    /// <summary>The COMPILE-TIME edited-image composition (a typed passthrough): a numeric literal VALUE on a
    /// numeric-edited item bakes its edited image as a constant (ISO §13.18.63 GR6) with the SAME runtime
    /// editor the generated code calls.</summary>
    public static string EditCompose(Int128 value, int valueScale, string picture, bool blankWhenZero, string? currencyString,
        bool commaMode, IReadOnlyList<CobolEdit.EditRule>? edits = null) =>
        CobolEdit.Format(value, valueScale, picture, blankWhenZero, '$', commaMode, edits?.ToArray(), currencyString);
}

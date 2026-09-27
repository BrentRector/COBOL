// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Runtime.Exceptions;

namespace CobolNet.Runtime;

/// <summary>
/// The size error condition (ISO/IEC 1989:2023 §14.7.5) raised <i>during expression evaluation or a store</i> — a
/// zero divisor (case 2), an exponentiation-rule violation (case 1), a native or standard-decimal INTERMEDIATE past
/// its range (cases 5/7 — <c>CobolDec</c>'s decimal128 range check, <c>CobolNum.RescaleEscape</c>), a
/// PROHIBITED-inexact transfer or intermediate (§14.7.4.3 r7 / §11.9.11.2 r3d), a checked store past the receiver
/// (case 3). Two dispositions, decided by WHERE it surfaces (kb/Work PB75):
/// <list type="bullet">
///   <item>inside an arithmetic statement's checked shape — an ON SIZE ERROR phrase or EC-SIZE checking — the
///         statement's own <c>try/catch (CobolSizeError)</c> takes it: the phrase runs, or the EC-SIZE handling
///         sets the status and dispatches — the split being on the phrase itself:
///         §14.7.5 rules 1–3 when "the size error condition exists and the SIZE ERROR phrase is specified", its
///         no-phrase rules 1–5 when it is not;</item>
///   <item>ESCAPING — from a condition, a DISPLAY / function argument, a subscript, an INVOKE argument, or a
///         no-phrase arithmetic statement whose checking is off — it IS the fatal exception condition §14.7.5's
///         no-phrase rules name (EC-SIZE-OVERFLOW / -ZERO-DIVIDE / -EXPONENTIATION / -TRUNCATION, "processing
///         proceeds as specified in 14.6.13.1.3"), so it derives from <see cref="CobolFatalException"/>: a
///         statement compiled with that EC's checking enabled dispatches its USE F3 declarative or the enclosing
///         PERFORM's WHEN (§14.6.13.1.3 #4/#5) else terminates the run unit (#7); with checking off it reaches
///         <c>ProgramTable.RunMain</c>'s boundary (#8 — this implementation terminates loudly, "abnormal run-unit
///         termination: EC-SIZE-… (fatal): …", exit 1 — never a raw CLR crash, which is what
///         <c>IF 10 ** 100000 &gt; 5</c> under STANDARD-DECIMAL used to be).</item>
/// </list>
/// <para><paramref name="ecName"/> is the precise Table 13 level-3 EC-SIZE-* name (§14.6.13.1.6): a zero divisor is
/// EC-SIZE-ZERO-DIVIDE; an exponentiation-rule violation EC-SIZE-EXPONENTIATION; the PROHIBITED-inexact
/// EC-SIZE-TRUNCATION; a range overflow EC-SIZE-OVERFLOW ("arithmetic overflow in calculation"). ⛔ It is REQUIRED —
/// there is no default name: a defaulted EC-SIZE-OVERFLOW is how the outermost-quotient PROHIBITED raise
/// (<c>CobolNum.DivideOrThrow</c>) latched the wrong level-3 name, so a USE AFTER EC-SIZE-TRUNCATION declarative
/// never saw it (kb/Work PB1147). Every raise site now says which Table 13 condition it is.</para>
/// </summary>
public sealed class CobolSizeError(string detail, string ecName) : CobolFatalException(ecName, detail)
{
    /// <summary>The zero divisor — §14.7.5 case 2, "if the divisor in a divide operation or in a DIVIDE statement is
    /// zero", EC-SIZE-ZERO-DIVIDE by no-phrase rule 2 (a Fatal condition in Table 13). The ONE raise every value
    /// lane's quotient kernel throws (<c>CobolNum.Divide</c> for the scaled <see cref="Int128"/> carrier,
    /// <c>CobolFloat.Div</c> for binary64, <c>CobolDec.Div</c> for the standard-decimal intermediate), and it throws
    /// it whether or not checking is enabled: the condition exists either way, and only its disposition differs.
    /// With checking off and no SIZE ERROR phrase §14.6.13.1.3 item 8 leaves that disposition to the implementor,
    /// and WiseOwl COBOL's (CONFORMANCE.md DOC-A.1-70) is that a quotient which cannot be formed has no value to store,
    /// so the run unit terminates abnormally naming the condition (kb/Work PB1605). Until then the scaled lane
    /// answered 0 and the binary64 lane ±Infinity (or NaN), and the run went on with that value.</summary>
    public static CobolSizeError ZeroDivide() =>
        new("a zero divisor in a divide operation (ISO §14.7.5 case 2)", "EC-SIZE-ZERO-DIVIDE");
}

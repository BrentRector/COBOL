// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding.Model;
using CobolNet.Common;
using CobolNet.Editions.Diagnostics;
using CobolNet.Frontend.Generated;

namespace CobolNet.Binding;

using Core = CobolParserCore;
using CobolNet.Runtime;

/// <summary>
/// ⛔ THE ONE READER OF THE ASSIGN CLAUSE'S TO-PHRASE LIST — ISO/IEC 1989:2023 §12.4.5.1 (every format writes
/// <c>ASSIGN [TO] {device-name-1 | literal-1} …</c>, the ellipsis on the inner brace pair) under WiseOwl COBOL's
/// §12.4.5.2 SR5 determination, <c>docs/CONFORMANCE.md</c> §7 row <c>DOC-A.1-71</c> (kb/Work PB829).
/// <para><b>What the standard leaves open.</b> SR5: "The meaning and rules for the allowable specification of
/// device-name-1 and the value of literal-1 are defined by the implementor." The general format admits a LIST,
/// and SR5 is the only rule that says which lists are allowable and what one means — so a list the determination
/// does not allow is refused BY NAME, here, citing SR5 and the determination. It used to be refused by the
/// grammar, whose one-operand <c>assignTarget</c> answered the second operand with COBOL0308 "a data-name is
/// expected here" — a parse error for a general format the standard prints.</para>
/// <para><b>The determination</b> (CLAUDE.md rule 1's precedence: ISO leaves it to the implementor → follow
/// GnuCOBOL, whose documented form is a device word followed by ONE file name, e.g. <c>ASSIGN TO DISK
/// TEXTFILE-NAME</c>):</para>
/// <list type="bullet">
///   <item>ONE operand — device-name-1 or literal-1 IS the physical file's name (item 71's mapping,
///   <c>CobolFile.ResolveHostPath</c>).</item>
///   <item>TWO operands — the first shall be a device-name-1 naming a DEVICE CLASS this implementation
///   provides, <see cref="DeviceClasses"/>; the second (device-name-1 or literal-1) is the physical file's name.
///   Both classes denote a file of the host file system, so the class word selects nothing further.</item>
///   <item>Anything else — three or more operands, a literal before another operand, or a leading word that
///   is not a provided device class — is COBOLNET2256.</item>
/// </list>
/// </summary>
internal static class AssignTargetRule
{
    /// <summary>The device classes a two-operand TO phrase may lead with. Each denotes a file of the host file
    /// system named by the operand that follows. KEYBOARD / DISPLAY-style console devices are NOT provided: a
    /// file connector here is always a host file, so such a word would promise a device it cannot deliver.</summary>
    internal static readonly IReadOnlySet<string> DeviceClasses =
        new HashSet<string>(CobolNames.Comparer) { "DISK", "PRINTER" };

    /// <summary>The target the TO phrase identifies — the text <c>FileModel.AssignTarget</c> carries to the
    /// runtime — reporting COBOLNET2256 for a list the determination does not allow. On a refused list the LAST
    /// operand is returned so binding continues on a plausible shape under an already-failed compile.</summary>
    public static string Resolve(EditionContext edition, LiteralEnvironment env, Core.AssignTargetContext[] targets,
        string fileName)
    {
        if (targets.Length == 1) return Text(edition, env, targets[0]);
        var first = targets[0];
        bool deviceLed = first.cobolWord() is not null && DeviceClasses.Contains(first.GetText());
        if (targets.Length == 2 && deviceLed) return Text(edition, env, targets[1]);
        using (edition.At(targets[1]))
            edition.Error(DiagnosticCatalog.AssignTargetListNotAllowed,
                $"SELECT {fileName}: the ASSIGN clause's TO phrase lists {targets.Length} operands ("
                + string.Join(" ", targets.Select(t => t.GetText())) + "); ISO §12.4.5.2 SR5 leaves the "
                + "allowable specification of device-name-1 and literal-1 to the implementor, and this "
                + "implementation allows ONE operand naming the file, or a device class ("
                + string.Join(" or ", DeviceClasses.Order(StringComparer.Ordinal)) + ") followed by ONE operand "
                + "naming the file (docs/CONFORMANCE.md §7 DOC-A.1-71)"
                + (targets.Length == 2 ? (first.cobolWord() is null
                    ? " — here the first operand is a literal, not a device class"
                    : $" — here '{first.GetText()}' is not a device class this implementation provides") : ""));
        return Text(edition, env, targets[^1]);
    }

    /// <summary>The physical name one operand gives: a device-name-1 word is its own spelling; literal-1 is
    /// screened against §12.4.5.2 SR4 — "an alphanumeric literal ... neither a figurative constant nor a
    /// zero-length literal" — and decoded (a §8.8.3 concatenation expression folds to its equivalent literal,
    /// §8.8.3.3 GR3; the hexadecimal-alphanumeric X"..." is an alphanumeric literal, §8.3.3.2.2). A refused literal
    /// yields "" under an already-failed compile.</summary>
    private static string Text(EditionContext edition, LiteralEnvironment env, Core.AssignTargetContext t)
    {
        if (t.nonNumericLiteral() is not { } nn) return t.GetText();
        void Refuse(string why)
        {
            using var _ = edition.At(nn);
            edition.Error(DiagnosticCatalog.AssignLiteralShape,
                $"ASSIGN TO {nn.GetText()}: literal-1 {why} (ISO §12.4.5.2 SR4)");
        }
        string value;
        if (nn.concatenationExpression() is { } ce)
        {
            var folded = ConcatFolder.Fold(ce, edition, env);
            if (folded.Category != PicCategory.Alphanumeric) { Refuse("shall be an alphanumeric literal, and this concatenation expression is not one"); return ""; }
            value = folded.Value;
        }
        else if (nn.predefinedNull() is not null)
        {
            // NULL is an identifier, not a literal (§8.4.3.10.3 SR1) — the ONE refusal, not the literal-shape verdict.
            using var _ = edition.At(nn);
            PredefinedNullRule.Report(edition, "literal-1 of the ASSIGN clause, which shall be an alphanumeric "
                + "literal (ISO §12.4.5.2 SR4),");
            return "";
        }
        else if (nn.figurativeConstant() is not null)
        {
            Refuse("shall not be a figurative constant");
            return "";
        }
        else
        {
            string token = nn.GetText();
            if (nn.STRINGLIT() is null && nn.HEXLIT() is null
                || CobolLiteral.ClassOf(token) != LiteralClass.Alphanumeric)
            {
                Refuse("shall be an alphanumeric literal");
                return "";
            }
            value = CobolLiteral.Decode(token);
        }
        if (value.Length == 0) { Refuse("shall not be a zero-length literal"); return ""; }
        return value;
    }
}

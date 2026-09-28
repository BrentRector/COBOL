// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding.Model;
using CobolNet.Common;
using CobolNet.Editions;
using CobolNet.Editions.Diagnostics;
using CobolNet.Frontend.Generated;

namespace CobolNet.Binding;

using Core = CobolParserCore;

/// <summary>
/// The ONE screen for the literal of an <c>AS</c> externalized-name phrase (grammar rule
/// <c>externalizedNamePhrase</c>).
///
/// <para>ISO §8.3.2.2 2) is the rule the phrase realizes: <i>"For any externalized user-defined words for which
/// the AS phrase is specified, the content of the literal specified in that AS phrase is a name that is
/// externalized to the operating environment. The implementor defines the formation and mapping rules of these
/// names."</i> Every clause that prints the phrase then restates ONE syntax rule about that literal, in the same
/// words: §11.10.3 SR1 (PROGRAM-ID), §11.5.3 SR1 (FUNCTION-ID), §11.6.3 SR1 (INTERFACE-ID), §11.7.3 SR1
/// (METHOD-ID) and §12.3.8.3 SR2 (the REPOSITORY specifiers) all read <i>"Literal-1 shall be an alphanumeric
/// literal or a national literal and shall be neither a figurative constant nor a zero-length literal"</i>.
/// Six clauses, one rule — so one screen, and a new AS site gets it by calling this rather than by growing a
/// seventh copy.</para>
///
/// <para>⚠ §11.3.3 SR1 (CLASS-ID) is the ONE deliberate asymmetry, verified against the PRINTED page (ISO PDF
/// p.264): it reads <i>"Literal-1 shall be an alphanumeric literal or a national literal and shall not be a
/// figurative constant"</i> — the zero-length exclusion the other five carry is ABSENT. It is honoured exactly,
/// through <paramref name="rejectZeroLength"/>; nothing here may "tidy" the standard into symmetry.</para>
///
/// <para>A hexadecimal literal IS §8.3.3.2 Format 2 of an alphanumeric one (the PB130 determination on the CALL
/// twin), and a §8.8.3 concatenation expression folds FIRST because §8.8.3.3 GR3 makes it "equivalent to a
/// literal of the same class and value". The fold is NOT collating-independent: the rule excludes a figurative
/// constant AS literal-1, but a concatenation operand may be one (§8.8.3.2 SR1), and a HIGH-/LOW-VALUE operand
/// takes the collating sequence of the context — so every caller names its <see cref="LiteralEnvironment"/>
/// (the id paragraphs, screened before any table exists, pass <see cref="LiteralEnvironment.Unscoped"/>;
/// kb/Work PB1406).</para>
/// </summary>
internal static class ExternalizedName
{
    /// <summary>Screen one AS-phrase literal against its clause's syntax rule and return its value, or null
    /// (having reported) on violation.</summary>
    /// <param name="lit">The phrase's <c>literal</c> child.</param>
    /// <param name="edition">The diagnostic sink; the caller has already positioned it.</param>
    /// <param name="code">The reporting descriptor — the OWNING clause's, never a shared one, so a message
    /// still names the paragraph the rule belongs to.</param>
    /// <param name="where">The source-shaped prefix of every message, e.g. <c>PROGRAM-ID 'X' AS "Y"</c>.</param>
    /// <param name="tag">The literal's name in its own clause — "literal-1" everywhere but the REPOSITORY
    /// program-specifier, where §12.3.8.2 numbers it literal-3.</param>
    /// <param name="rule">The citation appended to every message, e.g. <c>ISO §11.10.3 SR1</c>.</param>
    /// <param name="env">The literal environment a concatenation-expression literal folds in.</param>
    /// <param name="rejectZeroLength">False ONLY for §11.3.3 SR1 (CLASS-ID), which omits the exclusion.</param>
    /// <summary>The value an AS-phrase literal gives, WITHOUT diagnosing — for a reader that must know the
    /// externalized name but is not the phrase's screen (OoRepositoryScope, kb/Work PB974). Null for any literal
    /// <see cref="Screen"/> would reject, so both agree on which names exist.</summary>
    public static string? Peek(Core.LiteralContext lit) =>
        Screen(lit, new EditionContext(2023), DiagnosticCatalog.ExternalizedNameLiteral, "", "", "",
            LiteralEnvironment.Unscoped, rejectZeroLength: true);

    public static string? Screen(
        Core.LiteralContext lit, EditionContext edition, DiagnosticDescriptor code,
        string where, string tag, string rule, LiteralEnvironment env, bool rejectZeroLength = true)
    {
        void Reject(string why) => edition.Error(code, $"{where}: {why}");

        if (lit.nonNumericLiteral() is not { } nn)
        {
            Reject($"{tag} shall be an alphanumeric or national literal — a numeric literal is not an "
                   + $"externalized name ({rule})");
            return null;
        }
        if (nn.figurativeConstant() is not null)
        {
            Reject($"{tag} shall not be a figurative constant ({rule})");
            return null;
        }
        // The predefined NULL is an identifier, not a literal (kb/Work PB1427) — the ONE §8.4.3.10.3 SR1 refusal, not
        // the boolean-literal verdict the last arm below would have given it.
        if (nn.predefinedNull() is not null)
        {
            PredefinedNullRule.Report(edition, $"{where}: {tag}, which shall be an alphanumeric or national literal "
                + $"({rule}),");
            return null;
        }
        string value;
        if (nn.concatenationExpression() is { } ce)
        {
            var folded = ConcatFolder.Fold(ce, edition, env);
            if (folded.Category is not (PicCategory.Alphanumeric or PicCategory.National))
            {
                Reject($"{tag} folds to a {folded.Category.ToString().ToLowerInvariant()} literal; {rule} "
                       + "admits an alphanumeric or national literal");
                return null;
            }
            value = folded.Value;
        }
        else if (nn.STRINGLIT() is { } s) value = CobolLiteral.Decode(s.GetText());
        else if (nn.HEXLIT() is { } x) value = CobolLiteral.DecodeHex(x.GetText());
        else if (nn.NATLIT() is { } nat) value = CobolLiteral.Decode(nat.GetText());
        else if (nn.BOOLLIT() is not null)
        {
            Reject($"{tag} shall be an alphanumeric or national literal — a boolean literal is not an "
                   + $"externalized name ({rule})");
            return null;
        }
        else
            throw new InvalidOperationException($"{where}: nonNumericLiteral alternative '{nn.GetText()}' has no "
                + "externalized-name decoding — the grammar rule grew an arm this screen does not read");
        if (rejectZeroLength && value.Length == 0)
        {
            Reject($"{tag} shall not be a zero-length literal ({rule})");
            return null;
        }
        return value;
    }
}

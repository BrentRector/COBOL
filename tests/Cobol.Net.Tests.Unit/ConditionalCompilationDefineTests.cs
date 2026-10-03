// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System;
using System.Linq;
using CobolNet.Frontend.Diagnostics;
using CobolNet.Frontend.Preprocessor;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// The <c>&gt;&gt;DEFINE</c> directive (ISO §7.3.11) Wave-D behaviour in <see cref="ConditionalCompilationProcessor"/>:
/// the §7.3.11.3 SR2 no-OVERRIDE redefinition check (COBOLNET1618), the GR3 OVERRIDE phrase, and the GR4
/// <c>AS PARAMETER</c> operating-environment source.
/// </summary>
public sealed class ConditionalCompilationDefineTests
{
    private static (string Text, DiagnosticBag Diags) Run(string src)
    {
        var bag = new DiagnosticBag();
        string outp = ConditionalCompilationProcessor.Process(src, diagnostics: bag, sourcePath: "t.cob");
        return (outp, bag);
    }

    private static bool Has1618(DiagnosticBag b) => b.Diagnostics.Any(d => d.Code == "COBOLNET1618");

    [Fact] // SR2 (§7.3.11.3 #2) — a no-OVERRIDE redefinition to a DIFFERENT value is rejected.
    public void Redefine_DifferentValue_NoOverride_Rejected1618()
    {
        var (_, diags) = Run(">>DEFINE X AS 1\n>>DEFINE X AS 2\n");
        Assert.True(Has1618(diags));
    }

    [Fact] // SR2 third bullet — redefinition to the SAME value is allowed.
    public void Redefine_SameValue_NoOverride_Accepted()
    {
        var (_, diags) = Run(">>DEFINE X AS 1\n>>DEFINE X AS 1\n");
        Assert.False(Has1618(diags));
    }

    [Fact] // GR3 — OVERRIDE unconditionally redefines (SR2 bypassed).
    public void Redefine_WithOverride_Accepted()
    {
        var (_, diags) = Run(">>DEFINE X AS 1\n>>DEFINE X AS 2 OVERRIDE\n");
        Assert.False(Has1618(diags));
    }

    [Fact] // SR2 second bullet — a previous OFF clears the variable, so a later redefine is not a violation.
    public void Redefine_AfterOff_Accepted()
    {
        var (_, diags) = Run(">>DEFINE X AS 1\n>>DEFINE X OFF\n>>DEFINE X AS 2\n");
        Assert.False(Has1618(diags));
    }

    [Fact] // GR4 — AS PARAMETER sources the value from the operating environment (numeric here). The compilation-
           // variable-name is a COBOL user-defined word (§8.3.2.1 — letters, digits, hyphen and underscore;
           // ⚠ the lexer does not yet accept the underscore §8.3.2.1 permits — kb/Work PB159 reports it).
    public void Parameter_SourcesFromEnvironment()
    {
        Environment.SetEnvironmentVariable("CN-CC-TEST-PARAM", "42");
        try
        {
            var (text, diags) = Run(">>DEFINE CN-CC-TEST-PARAM AS PARAMETER\n>>IF CN-CC-TEST-PARAM = 42\nKEEP\n>>END-IF\n");
            Assert.False(Has1618(diags));
            Assert.Contains("KEEP", text);   // the >>IF matched → the guarded line survives
        }
        finally { Environment.SetEnvironmentVariable("CN-CC-TEST-PARAM", null); }
    }

    [Fact] // GR4 — when the environment supplies no value, the variable is NOT defined.
    public void Parameter_Unavailable_NotDefined()
    {
        Environment.SetEnvironmentVariable("CN-CC-TEST-UNSET", null);
        var (text, _) = Run(">>DEFINE CN-CC-TEST-UNSET AS PARAMETER\n>>IF CN-CC-TEST-UNSET DEFINED\nKEEP\n>>END-IF\n");
        Assert.DoesNotContain("KEEP", text);   // undefined → the >>IF DEFINED is false → the line drops
    }

    [Fact] // kb/Work PB1533 — a COBOL word is case-insensitive (§8.3.1), so two spellings of ONE compilation-variable-name
           // read ONE environment variable: the implementor-defined method (DOC-A.1-49) names the variable by the
           // name's canonical UPPER-CASE spelling, whatever the spelling in the directive.
    public void Parameter_TwoSpellingsOfOneName_ReadOneVariable()
    {
        Environment.SetEnvironmentVariable("CN-CC-TEST-SPELLING", "7");
        try
        {
            foreach (string spelling in (string[])["cn-cc-test-spelling", "Cn-Cc-Test-Spelling", "CN-CC-TEST-SPELLING"])
            {
                var (text, diags) = Run($">>DEFINE {spelling} AS PARAMETER\n>>IF {spelling} = 7\nKEEP\n>>END-IF\n");
                Assert.False(Has1618(diags));
                Assert.Contains("KEEP", text);   // defined from the ONE variable, whichever way the name is written
            }
        }
        finally { Environment.SetEnvironmentVariable("CN-CC-TEST-SPELLING", null); }
    }

    [Theory] // DOC-A.1-49 (kb/Work PB1533): the value's CATEGORY — NUMERIC when the whole text parses as an invariant-culture
             // decimal number (surrounding white space, a sign, '.' as the decimal point and ',' as a group separator are
             // accepted), otherwise ALPHANUMERIC with the text exactly as given. §7.3.8.2 SR1 a): a relation's operands are of
             // the same category, so the literal the relation compares against names the category the value has.
    [InlineData("7", "7")]
    [InlineData(" -2.5 ", "-2.5")]
    [InlineData("1,000", "1000")]
    [InlineData("007", "7")]
    [InlineData("abc", "\"abc\"")]
    [InlineData("7x", "\"7x\"")]
    [InlineData("1,5", "15")]   // the comma is a GROUP separator, never a decimal comma
    public void Parameter_ValueCategory_FollowsTheDocumentedTyping(string value, string literal)
    {
        const string name = "CN-CC-TEST-TYPING";
        Environment.SetEnvironmentVariable(name, value);
        try
        {
            var (text, diags) = Run($">>DEFINE {name} AS PARAMETER\n>>IF {name} = {literal}\nKEEP\n>>END-IF\n");
            Assert.False(diags.HasErrors, string.Join("\n", diags.Diagnostics));
            Assert.Contains("KEEP", text);
        }
        finally { Environment.SetEnvironmentVariable(name, null); }
    }

    [Fact] // kb/Work PB1367 — §7.3.11.3 SR2 reaches the PARAMETER alternative: without OVERRIDE, a name already defined
           // may be redefined only to the SAME value, and "no value from the environment" is not the same value.
    public void Parameter_NoValue_RedefiningADefinedName_WithoutOverride_Rejected1618()
    {
        Environment.SetEnvironmentVariable("CN-CC-TEST-PX", null);
        var (_, diags) = Run(">>DEFINE CN-CC-TEST-PX AS 1\n>>DEFINE CN-CC-TEST-PX AS PARAMETER\n");
        Assert.True(Has1618(diags));

        var (text, over) = Run(">>DEFINE CN-CC-TEST-PX AS 1\n>>DEFINE CN-CC-TEST-PX AS PARAMETER OVERRIDE\n>>IF CN-CC-TEST-PX DEFINED\nKEEP\n>>END-IF\n");
        Assert.False(Has1618(over));   // OVERRIDE is unconditional (GR3); no value ⇒ not defined (GR4)
        Assert.DoesNotContain("KEEP", text);
    }

    [Fact] // kb/Work PB1367 — the PARAMETER alternative with a DIFFERENT environment value is the same SR2 violation.
    public void Parameter_DifferentValue_RedefiningADefinedName_WithoutOverride_Rejected1618()
    {
        Environment.SetEnvironmentVariable("CN-CC-TEST-PY", "2");
        try
        {
            Assert.True(Has1618(Run(">>DEFINE CN-CC-TEST-PY AS 1\n>>DEFINE CN-CC-TEST-PY AS PARAMETER\n").Diags));
            Assert.False(Has1618(Run(">>DEFINE CN-CC-TEST-PY AS 2\n>>DEFINE CN-CC-TEST-PY AS PARAMETER\n").Diags));   // the SAME value
        }
        finally { Environment.SetEnvironmentVariable("CN-CC-TEST-PY", null); }
    }

    private static bool Has1619(DiagnosticBag b) => b.Diagnostics.Any(d => d.Code == "COBOLNET1619");

    [Fact] // ⛔ Ledger C2 — the CLOSED DEFECT: a MULTI-TOKEN arithmetic operand now EVALUATES (§7.3.6) instead of
           // silently binding to its first token. >>DEFINE X AS 2 * 3 + 1 defines 7 (was 2), so >>IF X = 7 matches.
    public void Define_ArithmeticExpressionOperand_Evaluated()
    {
        var (text, _) = Run(">>DEFINE X AS 2 * 3 + 1\n>>IF X = 7\nKEEP\n>>END-IF\n");
        Assert.Contains("KEEP", text);
    }

    [Fact] // §7.3.11.4 GR5 — a single numeric literal keeps its fractional value (not INTEGER-PART truncated).
    public void Define_SingleFractionLiteral_KeepsValue()
    {
        var (text, _) = Run(">>DEFINE Q AS 0.25\n>>IF Q = 0.25\nKEEP\n>>END-IF\n");
        Assert.Contains("KEEP", text);
    }

    [Fact] // §7.3.7 — a boolean-EXPRESSION operand now EVALUATES: FLG = B"1100" B-OR B"0011" = B"1111".
    public void Define_BooleanExpressionOperand_Evaluated()
    {
        var (text, _) = Run(">>DEFINE FLG AS B\"1100\" B-OR B\"0011\"\n>>IF FLG = B\"1111\"\nKEEP\n>>END-IF\n");
        Assert.Contains("KEEP", text);
    }

    [Fact] // §7.3.3 SR10 — a floating-point literal in a directive operand is a loud COBOLNET1619, not a value.
    public void Define_FloatLiteral_Rejected1619()
    {
        var (_, diags) = Run(">>DEFINE X AS 1.5E3\n");
        Assert.True(Has1619(diags));
    }

    [Fact] // §7.3.6.2 SR1c — a division by zero in a compile-time arithmetic operand is COBOLNET1619.
    public void Define_DivByZero_Rejected1619()
    {
        var (_, diags) = Run(">>DEFINE X AS 5 / 0\n");
        Assert.True(Has1619(diags));
    }

    [Fact] // §7.3.13.3 SR11 — an EVALUATE selection object of a different category than the subject is COBOLNET1619.
    public void Evaluate_ObjectCategoryMismatch_Rejected1619()
    {
        var (_, diags) = Run(">>EVALUATE 5\n>>WHEN \"A\"\nX\n>>END-EVALUATE\n");
        Assert.True(Has1619(diags));
    }

    /// <summary>&gt;&gt;PUSH / &gt;&gt;POP are recognized (consumed), not left as stray tokens.
    /// ⛔ The operand is REQUIRED: §7.3.22.2 and §7.3.20.2 print <c>{ directive-name | ALL }</c> in plain braces
    /// — verified against the printed page, folio 82 — so exactly one alternative shall be written, and
    /// §7.3.22.4 GR1/GR2 define an effect only for each of the two. This test wrote the BARE forms until
    /// kb/Work PB794 and passed, which made it a green test pinning an under-rejection
    /// (feedback_green_test_can_hold_a_gap_open); the bare form's own diagnostic is asserted below.</summary>
    [Fact]
    public void PushPop_Recognized_Consumed()
    {
        var (text, diags) = Run(">>PUSH ALL\nLINE-A\n>>POP ALL\n");
        Assert.False(diags.HasErrors);
        Assert.DoesNotContain(">>PUSH", text);
        Assert.DoesNotContain(">>POP", text);
        Assert.Contains("LINE-A", text);

        var (_, bare) = Run(">>PUSH\nLINE-A\n>>POP\n");
        Assert.Equal(2, bare.Diagnostics.Count(d => d.Code == "COBOLNET1911"));
    }

    [Theory] // every standard §7.3 directive is RECOGNIZED (consumed with its operand), never a stray token.
    [InlineData(">>DISPLAY \"hi\"")]
    [InlineData(">>FLAG-02 ON")]
    [InlineData(">>FLAG-14 OFF")]
    [InlineData(">>CALL-CONVENTION COBOL")]
    [InlineData(">>LEAP-SECOND ON")]
    [InlineData(">>LISTING ON")]
    public void StandardDirective_Recognized_Consumed(string directive)
    {
        var (text, _) = Run(directive + "\nLINE-B\n");
        Assert.DoesNotContain(">>", text);      // the whole directive line is consumed (operand and all)
        Assert.Contains("LINE-B", text);
    }
}

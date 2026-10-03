// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Globalization;
using CobolNet.Editions;
using CobolNet.Frontend.Diagnostics;
using CobolNet.Frontend.Expressions;
using CobolNet.Frontend.Generated;
using CobolNet.Frontend.Parsing;

namespace CobolNet.Frontend.Preprocessor;

// The DISPLAY directive (ISO §7.3.12, kb/Work PB807 + PB1538) — the one directive of the text-manipulation stage that
// PRODUCES output. Its operands are the compile-time operands every other directive reads (§7.3.12.3 SR2/SR3 point at
// §7.3.6 and §7.3.7), so they are parsed by the one directive-expression grammar and evaluated by the one shared
// evaluator; what is specific to DISPLAY — the PARAMETER phrase, the UPON phrase's choice indicators, the image each
// value is transferred as — lives here and nowhere else.
public static partial class ConditionalCompilationProcessor
{
    /// <summary>Process one <c>&gt;&gt;DISPLAY</c> line's operand text: parse it (§7.3.12.2), evaluate each operand in
    /// order (§7.3.12.4 GR4), resolve the destinations (GR5, GR6), and — when nothing is wrong and every PARAMETER value
    /// is available (GR3) — transfer ONE line per destination. A violation is reported and nothing is transferred: a
    /// half-evaluated line is not what the author wrote.</summary>
    private static void ApplyDisplay(string rest, CompileTimeExpressionEvaluator evaluator, DirectiveDiag diag,
        CompilationInputs inputs, DiagnosticBag? bag)
    {
        if (rest.Length == 0) return;   // §7.3.12.2 writes at least one operand: that PRESENCE is the central check's (COBOLNET1911)
        if (DirectiveExpressionFragment.ParseDisplay(rest) is not { } fragment) { diag.Malformed(">>DISPLAY", rest); return; }

        var images = new List<string>();
        bool valid = true, available = true;
        foreach (var operand in fragment.displayDirectiveOperand())
        {
            if (operand.compileTimeOperand() is { } compileTime)
            {
                diag.FlagArithmetic(compileTime);          // b COMPILE-TIME-ARITHMETIC-EXPRESSIONS (§7.3.15.4 GR4 b), as every evaluated operand
                diag.GateBooleanOperators(compileTime);
                if (evaluator.EvaluateOperand(compileTime, ">>DISPLAY") is { } value) images.Add(DisplayImage(value));
                else valid = false;                        // already reported by the shared evaluator
                continue;
            }
            // PARAMETER compilation-variable-name-1 — §7.3.12.4 GR3: the value comes from the operating environment by the
            // implementor-defined method of DOC-A.1-55 (the DEFINE PARAMETER lookup, one rule); "If no value is made
            // available from the operating environment no transfer shall take place".
            if (ParameterText(operand.cobolWord(1).GetText(), inputs) is { } text) images.Add(text);
            else available = false;
        }
        var destinations = DisplayDestinations(fragment.displayUponPhrase(), diag);
        if (!valid || !available || destinations is null || bag is null) return;

        // GR1 / GR4: the operands, in the order written, as one line; DOC-A.1-53 joins the images with one space.
        string line = string.Join(' ', images);
        foreach (var destination in destinations) bag.Transfer(destination, line);
    }

    /// <summary>The value of the PARAMETER phrase's compilation-variable-name from the operating environment (§7.3.11.4
    /// GR4, §7.3.12.4 GR3), or null when none is made available: the environment variable named by the name in its
    /// canonical UPPER-CASE spelling (DOC-A.1-49, DOC-A.1-55) — a COBOL word is case-insensitive (§8.3.1), so every
    /// spelling of one name reads ONE variable. The ONE lookup of the DEFINE and DISPLAY directives, recorded on the
    /// compilation's inputs (kb/Work PB985, PB1533).</summary>
    private static string? ParameterText(string name, CompilationInputs inputs) =>
        inputs.GetEnvironmentVariable(name.ToUpperInvariant());

    /// <summary>The streams an UPON phrase names, in the order written, or null (reported) when it breaks its format.
    /// §7.3.12.4 GR6: no UPON phrase is as if UPON LISTING; GR5 a): LISTING is "the same device as that used for source
    /// listings" — none is produced (DOC-A.1-117), so it is the compiler's standard output; GR5 b): compile-time-device-1
    /// is "the device defined by the implementor" — an OUTPUT-capable device-name of the ONE implementor-name table
    /// (<see cref="ImplementorNames"/>, DOC-A.1-54), so the directive and a SPECIAL-NAMES entry cannot disagree about
    /// what a device-name means. §7.3.12.2's braces carry §5.2.6.4 CHOICE INDICATORS: one or more of the device group
    /// (<c>compile-time-device-1 …</c>, itself repeatable) and LISTING, EACH AT MOST ONCE, in any order — so LISTING
    /// twice, or two separate runs of devices, is refused.</summary>
    private static List<CompileOutputStream>? DisplayDestinations(CobolParserCore.DisplayUponPhraseContext? upon, DirectiveDiag diag)
    {
        if (upon is null) return [CompileOutputStream.Standard];
        var destinations = new List<CompileOutputStream>();
        bool listing = false, devices = false, inDeviceRun = false, valid = true;
        foreach (var wordNode in upon.cobolWord())
        {
            string word = wordNode.GetText();
            if (word.Equals("LISTING", StringComparison.OrdinalIgnoreCase))
            {
                if (listing) { diag.DisplayUpon("LISTING is written twice — the choice indicators of §5.2.6.4 admit each alternative at most once"); valid = false; }
                listing = true;
                inDeviceRun = false;
                destinations.Add(CompileOutputStream.Standard);
                continue;
            }
            if (ImplementorNames.Lookup(word) is not { Kind: SystemNameKind.Device } row || !row.Device.HasFlag(DeviceCapability.Output))
            {
                diag.DisplayUpon($"'{word}' is not an output device this implementation makes available as compile-time-device-1 "
                    + $"(ISO §7.3.12.4 GR5 b); the device-names are {ImplementorNames.DescribeDevices()}, and LISTING names the listing");
                valid = false;
                continue;
            }
            if (!inDeviceRun)
            {
                if (devices) { diag.DisplayUpon($"a second group of devices begins at '{word}' — the choice indicators of §5.2.6.4 admit each alternative at most once"); valid = false; }
                devices = true;
                inDeviceRun = true;
            }
            destinations.Add(row.Device.HasFlag(DeviceCapability.StandardError) ? CompileOutputStream.Error : CompileOutputStream.Standard);
        }
        return valid ? destinations : null;
    }

    /// <summary>The image a value is transferred as (DOC-A.1-53, §7.3.12.4 GR1 — "any conversion of data required is
    /// defined by the implementor"): an alphanumeric or national value as its characters, a boolean value as its
    /// <c>0</c> and <c>1</c> characters, a numeric value in the shortest decimal form — a leading <c>-</c> only when
    /// negative, at least one integer digit, a <c>.</c> and the fraction digits only when the value is not an integer,
    /// no trailing zeros.</summary>
    private static string DisplayImage(CtValue value) => value.Category switch
    {
        CtCategory.Numeric => DecimalImage(value.Number),
        CtCategory.Boolean => value.Bits!.Bits,
        _ => value.Text,
    };

    private static string DecimalImage(CobolNet.Runtime.CobolDec value)
    {
        if (value.Sig == 0) return "0";
        bool negative = value.Sig < 0;
        string digits = (negative ? -value.Sig : value.Sig).ToString(CultureInfo.InvariantCulture);
        int exponent = value.Exp;
        while (exponent < 0 && digits[^1] == '0') { digits = digits[..^1]; exponent++; }   // 1.50 is 1.5
        string body = exponent >= 0 ? digits + new string('0', exponent)
            : digits.Length > -exponent ? digits[..^-exponent] + "." + digits[^-exponent..]
            : "0." + new string('0', -exponent - digits.Length) + digits;
        return negative ? "-" + body : body;
    }
}

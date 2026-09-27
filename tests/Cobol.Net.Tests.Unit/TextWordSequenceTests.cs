// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Frontend.Preprocessor;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// <see cref="TextWordSequence.Matches"/> — the public face of the ONE text-word equality (ISO/IEC 1989:2023
/// §7.2.3.4 9) c)), which §13.10.3 SR9 asks when a constant-name is duplicated (kb/Work PB1230): "the specification
/// of arithmetic-expression-1, literal-1, data-name-1, data-name-2, or compilation-variable-name-1 shall be the same
/// as specified in the other constant-name". Each case pins one clause of the matching rule, and the "different"
/// cases pin the PB1230 defect itself: specifications that FOLD to the same value are still different specifications.
/// </summary>
public sealed class TextWordSequenceTests
{
    [Theory]
    // c) 1. — a separator space (any run of them) is a single space; the spacing of the written text is not compared.
    [InlineData("2 + 3", "2   +   3")]
    // c) 3. — a COBOL word compares without regard to letter case.
    [InlineData("LENGTH OF W", "length of w")]
    // c) 4. a. — the two representations of the quotation symbol match.
    [InlineData("\"AB\"", "'AB'")]
    // c) 3. — the hexadecimal format's content compares without regard to case.
    [InlineData("X\"4a\"", "X\"4A\"")]
    // a numeric literal with a comma decimal separator is ONE text-word (the comma is not followed by a space).
    [InlineData("1,5", "1,5")]
    public void SameSpecification(string a, string b) => Assert.True(TextWordSequence.Matches(a, b));

    [Theory]
    // kb/Work PB1230 — the forms SR9 must tell apart although they denote one value.
    [InlineData("5", "2 + 3")]
    [InlineData("LENGTH OF W", "7")]
    [InlineData("5", "+5")]
    [InlineData("5", "05")]
    [InlineData("-5", "- 5")]                  // a literal vs. a unary operator applied to one (§8.3.3.3.2 rule 2)
    [InlineData("\"A\" & \"B\"", "\"AB\"")]    // c) 2. — a concatenation's operands and operator are text-words
    // c) 3. — the non-hexadecimal alphanumeric format keeps its letter case.
    [InlineData("\"ab\"", "\"AB\"")]
    public void DifferentSpecification(string a, string b) => Assert.False(TextWordSequence.Matches(a, b));
}

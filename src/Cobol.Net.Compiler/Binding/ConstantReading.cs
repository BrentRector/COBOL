// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Binding;

/// <summary>What a WORD written at a literal position stands for, under ISO §13.10.3 SR2 — "Except in a compiler
/// directive, constant-name-1 may be used anywhere that a format specifies a literal of the class and category of
/// constant-name-1" — and §13.10.4 GR1/GR2: the constant is "as if literal-1 or the text represented by
/// compilation-variable-name-1 were written where constant-name-1 is written", with that literal's class and
/// category. <see cref="Value"/> is the literal's class, category and value when <see cref="Kind"/> is
/// <see cref="ConstantReadingKind.Constant"/>; the position holds it to its own rules exactly as it holds a written
/// literal (kb/Work PB2588).</summary>
internal readonly record struct ConstantReading(ConstantReadingKind Kind, ConcatFolder.Folded Value)
{
    /// <summary>The word names no constant entry in scope.</summary>
    public static ConstantReading NotAConstant => new(ConstantReadingKind.NotAConstant, default);

    /// <summary>The word names a constant entry whose own operand was refused; that entry reports it, so the
    /// position recovers silently.</summary>
    public static ConstantReading Refused => new(ConstantReadingKind.Refused, default);

    public static ConstantReading Of(ConcatFolder.Folded value) => new(ConstantReadingKind.Constant, value);

    /// <summary>⛔ THE ONE "a word stands at a literal position only as a constant-name" message (kb/Work PB1942,
    /// PB2588): every position whose literal may be a constant-name reports a word that names none in these words.</summary>
    /// <param name="operand">The standard's name for the operand ("literal-7").</param>
    /// <param name="word">The word as written.</param>
    public static string NotAConstantMessage(string operand, string word) =>
        $"{operand} is a literal, and '{word}' names no constant entry — a word stands there only as a constant-name "
        + "(ISO §13.10.3 SR2: \"constant-name-1 may be used anywhere that a format specifies a literal of the class "
        + "and category of constant-name-1\")";
}

/// <summary>The three answers a <see cref="ConstantReading"/> gives.</summary>
internal enum ConstantReadingKind
{
    /// <summary>No constant entry in scope is named by the word.</summary>
    NotAConstant,
    /// <summary>A constant entry is named, and its own operand was refused (reported at the entry).</summary>
    Refused,
    /// <summary>A constant entry is named; <see cref="ConstantReading.Value"/> is its literal.</summary>
    Constant,
}

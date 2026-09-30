// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Frontend.Expressions;

/// <summary>The KIND of a compile-time expression diagnostic. The shared <see cref="CompileTimeExpressionEvaluator"/>
/// builds the message text; each consumer maps the kind to its own diagnostic CODE (a bare message delegate would
/// lose the code — the byte-identical-binder requirement), e.g. the CONSTANT-entry binder routes
/// <see cref="ArithmeticRule"/> to its <c>ConstantEntryRule</c> descriptor and <see cref="NumericSeparator"/> to
/// COBOLNET0895, while the frontend conditional-compilation stage routes both to COBOLNET1619.</summary>
public enum CtDiagCode
{
    /// <summary>A §7.3.6 compile-time arithmetic formation/evaluation rule (SR1a exponentiation, SR1c division by
    /// zero, SR1b operand-not-a-literal, SR2 overflow, or an unsupported expression shape).</summary>
    ArithmeticRule,

    /// <summary>A §12.3.7 GR14a numeric-literal decimal-separator violation (the DECIMAL-POINT IS COMMA rule).</summary>
    NumericSeparator,

    /// <summary>A compiler-directive expression formation rule OTHER than the shared §7.3.6 arithmetic core — the
    /// §7.3.3 SR10 master constraint (no floating-point literal / figurative constant / concatenation in a
    /// directive), a §7.3.7 boolean-operand rule, or a §7.3.8 constant-conditional-expression rule. Raised only by
    /// the directive-only entry points (<c>EvaluateBoolean</c> / <c>EvaluateOperand</c> / <c>EvaluateCce</c>), so a
    /// consumer that evaluates only arithmetic operands (the CONSTANT-entry binder) never sees it; the frontend
    /// routes it to COBOLNET1619.</summary>
    DirectiveRule,

    /// <summary>A compilation-variable-name that is a compiler-directive word (§8.12; §7.3.8.4.3 SR1, §7.3.11.3 SR1) —
    /// raised by the defined-condition evaluation, which only a directive's constant-conditional-expression reaches
    /// (kb/Work PB1366); the frontend routes it to COBOLNET2650.</summary>
    DirectiveWordAsName,
}

/// <summary>A CODE-preserving diagnostic sink for the shared compile-time expression evaluator. The evaluator
/// reports a <see cref="CtDiagCode"/> plus the fully-formed message; the caller maps the kind to its own
/// diagnostic channel (so the CONSTANT binder keeps its exact codes/descriptors and the frontend routes to its
/// own frontend code).</summary>
public interface ICtDiagnostics
{
    /// <summary>Report one compile-time expression diagnostic (already fully formed by the evaluator).</summary>
    void Report(CtDiagCode code, string message);
}

/// <summary>Per-consumer operand wording + citation so a shared diagnostic cites the section that actually governs
/// THAT consumer (spec-fidelity — the cited § must match the operation). The §7.3.6 rules are genuinely shared;
/// the operand-source clause and any consumer-specific citation are injected: the CONSTANT-entry binder names a
/// "constant-name" and cites §13.10.3; the frontend names a "compilation variable" and cites §7.3.6/§7.3.11.</summary>
/// <param name="OperandSource">The permitted non-literal operand source, e.g. "previously defined numeric
/// constant-names substituting them" (binder) or "previously defined numeric compilation variables"
/// (frontend).</param>
/// <param name="GoverningCitation">The section citation for the operand-source rule, e.g.
/// "ISO §13.10.3 SR7 / §7.3.6.2 SR1b" (binder) or "ISO §7.3.6.2 SR1b" (frontend).</param>
public sealed record CtOperandVocabulary(string OperandSource, string GoverningCitation);

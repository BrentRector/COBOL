// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.

using System.Collections.Frozen;
using CobolNet.Editions.Diagnostics;
using CobolNet.Frontend.Generated;

namespace CobolNet.Binding.Procedure;

using Core = CobolParserCore;

/// <summary>An identifier FORM that ISO §8.4.3 forbids as a receiving operand, each by a syntax rule written into the
/// format itself rather than into any statement (kb/Work PB1551, PB1782 step 1). The receiving chokepoint
/// (<see cref="ExpressionBinder.ResolveReceiving(Core.DataReferenceContext)"/> and its <c>receivingIdentifier</c>
/// overloads) classifies a receiver into one of these BEFORE it is resolved, and <see cref="ReceivingOperandForms"/>
/// holds the one row per form that names its rule — so a form cannot be added without deciding its receiving rule
/// (<c>ReceivingOperandFormDriftTests</c>).
/// <para>The two COMPUTED formats are not members: a function-identifier (§8.4.3.2.3 SR1) and an inline method
/// invocation (§8.4.3.4.3 SR1) are excluded from every purely receiving grammar rule structurally
/// (<c>InlineMethodInvocationOperandDriftTests</c>), and the mixed arithmetic rules name them
/// (<c>ArithmeticOperandRole</c>). A constant-name is not a member either: it is a literal (§13.10.4 GR1), not an
/// identifier format, and only its resolution tells it from a data-name.</para></summary>
internal enum NonReceivableForm
{
    /// <summary>§8.4.3.5.3 SR2 — an object-view (<c>identifier-1 AS …</c>), including a view of SELF, SUPER or NULL.</summary>
    ObjectView,
    /// <summary>§8.4.3.6.3 SR1 — the predefined object reference EXCEPTION-OBJECT.</summary>
    ExceptionObject,
    /// <summary>§8.4.3.7.3 SR1 — the predefined object reference (or predefined address) NULL.</summary>
    Null,
    /// <summary>§8.4.3.8.3 SR2 — SELF.</summary>
    Self,
    /// <summary>§8.4.3.8.3 SR2 — SUPER.</summary>
    Super,
    /// <summary>§8.4.3.11.3 SR5 — a data-address-identifier (<c>ADDRESS OF identifier-1</c>).</summary>
    DataAddress,
    /// <summary>§8.4.3.12.3 SR3 — a function-address-identifier (<c>ADDRESS OF FUNCTION …</c>).</summary>
    FunctionAddress,
    /// <summary>§8.4.3.13.3 SR4 — a program-address-identifier (<c>ADDRESS OF PROGRAM …</c>).</summary>
    ProgramAddress,
    /// <summary>§8.4.3.14.3 SR2 — LINAGE-COUNTER.</summary>
    LinageCounter,
    /// <summary>§8.4.3.15.3 SR3 — LINE-COUNTER.</summary>
    LineCounter,
}

/// <summary>⛔ THE ONE TABLE OF RECEIVING-OPERAND PROHIBITIONS BY IDENTIFIER FORM (kb/Work PB1551). Before it, the
/// rules lived in three places that disagreed: the receiving chokepoint named EXCEPTION-OBJECT, LINAGE-COUNTER and
/// LINE-COUNTER; SET Format 5 and INVOKE RETURNING named the object-view (each with its own message); and NULL, SELF,
/// SUPER and the address-identifiers reached no rule at all — a receiving slot that took only a data reference refused
/// them as a parse error re-coded to §8.9 (COBOLNET0901 "'SELF' is a reserved word … cannot be used as a user-defined
/// word"), a rule the program had not broken. Every row is asked by <see cref="ExpressionBinder"/>'s receiving
/// chokepoint and by nothing else.</summary>
internal static class ReceivingOperandForms
{
    /// <summary>One form's prohibition: the descriptor it reports under and the message it reports, given the operand as
    /// written.</summary>
    internal readonly record struct Rule(DiagnosticDescriptor Descriptor, Func<string, string> Message);

    /// <summary>Every <see cref="NonReceivableForm"/>, with the rule that forbids it. Each message quotes or names its own
    /// clause (cite.py --check: §8.4.3.5.3 2), §8.4.3.6.3 1), §8.4.3.7.3 1), §8.4.3.8.3 2), §8.4.3.11.3 5),
    /// §8.4.3.12.3 3), §8.4.3.13.3 4), §8.4.3.14.3 2), §8.4.3.15.3 3) — all OK).</summary>
    internal static readonly FrozenDictionary<NonReceivableForm, Rule> Rules = new Dictionary<NonReceivableForm, Rule>
    {
        [NonReceivableForm.ObjectView] = new(DiagnosticCatalog.ObjectViewReceiving, w =>
            $"'{w}' is written as a receiving operand: an object-view shall not be specified as a receiving operand "
            + "(ISO §8.4.3.5.3 SR2)"),
        [NonReceivableForm.ExceptionObject] = new(DiagnosticCatalog.ExceptionObjectReceiving, _ =>
            "EXCEPTION-OBJECT shall not be specified as a receiving operand (ISO §8.4.3.6.3 SR1) — it is the "
            + "predefined object reference for the CURRENT exception object (§8.4.3.6.4 GR1), set by the run unit when "
            + "an exception is raised"),
        [NonReceivableForm.Null] = new(DiagnosticCatalog.NullReceiving, w =>
            $"'{w}' is written as a receiving operand: NULL shall not be specified as a receiving operand (ISO "
            + "§8.4.3.7.3 SR1); write the item that is to receive NULL as the receiver (SET item TO NULL)"),
        [NonReceivableForm.Self] = new(DiagnosticCatalog.SelfSuperReceiving, w =>
            $"'{w}' is written as a receiving operand: SELF is an identifier format that shall not be specified as a "
            + "receiving operand (ISO §8.4.3.8.3 SR2)"),
        [NonReceivableForm.Super] = new(DiagnosticCatalog.SelfSuperReceiving, w =>
            $"'{w}' is written as a receiving operand: SUPER is an identifier format that shall not be specified as a "
            + "receiving operand (ISO §8.4.3.8.3 SR2)"),
        [NonReceivableForm.DataAddress] = new(DiagnosticCatalog.AddressIdentifierReceiving, w =>
            $"'{w}' is written as a receiving operand: a data-address-identifier shall not be specified as a "
            + "receiving operand (ISO §8.4.3.11.3 SR5); only SET's own Format-7 receiver ADDRESS OF data-name-1 "
            + "stores an address (§14.9.39.2)"),
        [NonReceivableForm.FunctionAddress] = new(DiagnosticCatalog.AddressIdentifierReceiving, w =>
            $"'{w}' is written as a receiving operand: a function-address-identifier shall not be specified as a "
            + "receiving operand (ISO §8.4.3.12.3 SR3)"),
        [NonReceivableForm.ProgramAddress] = new(DiagnosticCatalog.AddressIdentifierReceiving, w =>
            $"'{w}' is written as a receiving operand: a program-address-identifier shall not be specified as a "
            + "receiving operand (ISO §8.4.3.13.3 SR4)"),
        [NonReceivableForm.LinageCounter] = new(DiagnosticCatalog.LinageCounterReceiving, _ =>
            "LINAGE-COUNTER shall not be referenced as a receiving operand (ISO §8.4.3.14.3 SR2); only the "
            + "input-output control system may change its value (ISO §13.18.34.4 GR7 b)"),
        [NonReceivableForm.LineCounter] = new(DiagnosticCatalog.ReportLineCounterReceiving, _ =>
            "LINE-COUNTER shall not be referenced as a receiving operand (ISO §8.4.3.15.3 SR3)"),
    }.ToFrozenDictionary();

    /// <summary>The form of a <c>nonReceivableIdentifier</c> — the grammar's superset of the formats above that a
    /// receiving position writes. A view of SELF, SUPER or NULL is an object-view (the format written), so the view is
    /// asked first.</summary>
    internal static NonReceivableForm FormOf(Core.NonReceivableIdentifierContext n) =>
        n.objectViewPhrase().Length != 0 ? NonReceivableForm.ObjectView
        : n.selfAndSuper() is { } ss ? (ss.SELF() is not null ? NonReceivableForm.Self : NonReceivableForm.Super)
        : n.predefinedNull() is not null ? NonReceivableForm.Null
        : n.functionAddressIdentifier() is not null ? NonReceivableForm.FunctionAddress
        : NonReceivableForm.ProgramAddress;
}

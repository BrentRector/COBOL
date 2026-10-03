// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Editions;

namespace CobolNet.Binding.Bound;

// The ALTER / external-switch bound nodes (P7 Step 10n: the binder half moved to
// Binding/Procedure/Verbs/SetAlterBinder.cs; these types STAY here — BoundAlter/BoundGoToAlterable are
// VersionConformancePass gate anchors, and the source-generated visitor keys on this namespace).

/// <summary><c>SET {mnemonic-name-1}… TO {ON|OFF} …</c> (ISO §14.9.39 Format 3), resolved to implementor
/// switch-names: GR5 — each switch associated with a mnemonic-name is modified so that a condition-name associated
/// with it evaluates on (ON phrase) / off (OFF phrase). Both the mnemonic list and the outer group repeat (a
/// compound <c>SET A TO ON B TO OFF</c> is ONE statement); assignments apply in source order.</summary>
public sealed record BoundSetSwitches(IReadOnlyList<(string Name, bool On)> Switches) : BoundStatement;

/// <summary>A switch-status condition (ISO §8.8.4.6.2 — just <c>condition-name-1</c>): true when the external
/// switch <paramref name="ImplementorName"/> is set to the position the condition-name posits (GR1) — ON when
/// <paramref name="TestsOn"/>, OFF otherwise. A simple condition (§8.8.4.4), freely combinable with AND/OR/NOT.</summary>
public sealed record BoundSwitchCondition(string ImplementorName, bool TestsOn) : BoundCondition;

/// <summary>One <c>ALTER proc-1 TO [PROCEED TO] proc-2</c> entry, fully resolved: assign <paramref name="NewPc"/>
/// into the target paragraph's alterable-GO-TO field (COBOLNET_CONTROL_FLOW_DESIGN D4 — a mutable static int per
/// altered paragraph, NOT the legacy slot table).</summary>
public sealed record BoundAlterEntry(string AlterField, int NewPc);

/// <summary><c>ALTER</c> (ANSI X3.23-1985 ¶VI.6 — obsolete there; DELETED by ISO/IEC 1989:2002; the 2023 standard
/// has no ALTER and its GO TO has only Formats 1–2, §14.9.17): each entry replaces the GO TO target of the named
/// single-GO-TO paragraph. The most recent ALTER executed governs subsequent transfers.</summary>
public sealed record BoundAlter(IReadOnlyList<BoundAlterEntry> Entries) : BoundStatement;

/// <summary>A GO TO inside an ALTER-target paragraph (written-target or target-less): transfer to the CURRENT
/// value of the paragraph's <paramref name="AlterField"/> (D4 — <c>__pc = _alter_X; break;</c>).
/// <paramref name="DefaultPc"/> initializes the field: the written GO TO target's pc, or −1 for a target-less
/// <c>GO TO.</c> not yet ALTERed — executing it then is undefined (ANSI-85), realized as −1 = dispatcher exit.</summary>
public sealed record BoundGoToAlterable(string AlterField, int DefaultPc, int SourceLine = 0) : BoundStatement;

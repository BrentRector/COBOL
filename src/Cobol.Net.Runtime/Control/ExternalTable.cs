// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Runtime;

/// <summary>The EC-EXTERNAL conformance-check selector bits (ISO §14.8.4 / §14.9.4.4 GR3e's rule→condition
/// table). A bit is set in an element's mask when checking for the corresponding level-3 condition is enabled
/// there (>>TURN, §7.3.25); the §14.8.4.1 "both the activating and activated runtime elements" rule is realized
/// as the bitwise AND of the CALL-site mask and the activated element's before-Environment-division mask —
/// each condition pairs independently.</summary>
[Flags]
public enum ExternalChecks
{
    None = 0,
    /// <summary>EC-EXTERNAL-FORMAT-CONFLICT — §14.8.4.3 / §13.18.22 GR6 (Fatal).</summary>
    FormatConflict = 1,
    /// <summary>EC-EXTERNAL-DATA-MISMATCH — §14.8.4.2 file status / linage / relative key storage identity (Fatal).</summary>
    DataMismatch = 2,
    /// <summary>EC-EXTERNAL-FILE-MISMATCH — §14.8.4.4 / §12.4.5.3 GR1 a–m entry consistency (Fatal).</summary>
    FileMismatch = 4,
    /// <summary>EC-EXTERNAL-IMP — implementor-defined mismatch (Imp). This implementation defines NO
    /// implementor-specific external checks, so the condition has no raise site (a conforming choice —
    /// the condition is implementor-defined by Table 13).</summary>
    Imp = 8,
}

/// <summary>One runtime element's compile-time description of an external item or external file connector —
/// the facts §14.8.4 compares across the run unit's describers. Every field is a compile-time constant emitted
/// at the describing element's activation entry.
/// RECORD kind (§14.8.4.3 / §13.18.22 GR6): <see cref="ByteCount"/> = the record's byte count;
/// <see cref="ValueImage"/> = the record name's OWN VALUE clause specification as a canonical KEY, else null (the
/// GR6 "identical VALUE clause specification" identity: the clause's words are case-folded, a literal's characters
/// are kept exactly — <c>OoEmitter.ValueSpecificationKey</c>; it is NOT a composed image); a complete-record
/// REDEFINES contributes nothing (GR6 explicitly exempts it for non-strong records — the descriptor is built
/// from the base record only); <see cref="StrongTypeKey"/> = the external strong TYPE name (null when not
/// strongly typed) and <see cref="ConstantRecord"/> = the CONSTANT RECORD presence (§14.8.4.3 ¶3).
/// FILE kind (§14.8.4.2 / §14.8.4.4): <see cref="FileStatusRef"/> / <see cref="RelativeKeyRef"/> /
/// <see cref="LinageRef"/> = the corresponding-external-item identities of the file-referencing control items
/// ("EXTNAME.SUB.PATH", "!" when the item exists but is NOT external, null when the clause is absent);
/// <see cref="SelectFingerprint"/> = the §12.4.5.3 GR1 a–m canonical fingerprint of the file control entry.</summary>
public sealed record ExternalDescriptor(
    string Kind,
    int ByteCount = 0,
    string? ValueImage = null,
    string? StrongTypeKey = null,
    bool ConstantRecord = false,
    string? FileStatusRef = null,
    string? RelativeKeyRef = null,
    string? LinageRef = null,
    string? SelectFingerprint = null);

/// <summary>
/// The run-unit EXTERNAL data store (ISO §8.6.7 / §13.18.22): ONE storage copy per external name for the whole
/// run unit, represented as the record's character image (the same Tier-B string-canonical shape the data model
/// uses for shared storage — never a persisted byte substrate). Every program describing the same external name
/// windows the same cell; CANCEL does NOT reset it (§14.9.5 GR8). The §13.18.22 GR6 / §14.8.4 conformance checks
/// run through <see cref="Describe"/> — each describing runtime element registers its compile-time
/// <see cref="ExternalDescriptor"/> at activation entry and is compared against every OTHER describer already
/// registered (the run-unit-global SHALLs of §14.8.4.2–.4), raising the paired EC-EXTERNAL condition when the
/// CALL-site/activated-element enablement gate passes (§14.8.4.1 / §14.9.4.4 GR3e).
/// One INSTANCE per run unit on <see cref="RunUnit"/> ("once per run unit" is the spec's own scoping,
/// §14.6.2.3.2); the static <see cref="ExternalStore"/> shim keeps the emitted surface unchanged pre-G8.
/// </summary>
public sealed class ExternalTable
{
    private readonly Dictionary<string, StorageCell> _cells = new(ExternalizedNames.Comparer);
    // Per external name: each describing element's descriptor, keyed by the describer's unit path. A
    // re-activation re-describes identically (compile-time constants), so same-key replacement is a no-op;
    // CANCEL does not remove entries (the external storage — and hence its description — persists, §14.9.5 GR8).
    private readonly Dictionary<string, Dictionary<string, ExternalDescriptor>> _describers = new(ExternalizedNames.Comparer);

    /// <summary>The run-unit cell for <paramref name="name"/>, created with <paramref name="initialImage"/> on
    /// first reference (ISO §14.6.2.3.2 — external data takes its initial state once per run unit). The cell
    /// is the ONE shared-storage shape (<see cref="StorageCell"/>), so ADDRESS OF an EXTERNAL item needs no
    /// special case.</summary>
    public StorageCell Cell(string name, string initialImage)
    {
        if (!_cells.TryGetValue(name, out var h)) _cells[name] = h = new StorageCell { Ref = initialImage };
        return h;
    }

    /// <summary>The same cell, created by <paramref name="create"/> when the run unit first names it — the form a
    /// cell with a SEEDED dynamic-length half needs (kb/Work PB1026), since its seed must run once, at creation,
    /// and never again on a later describer's access.</summary>
    public StorageCell Cell(string name, Func<StorageCell> create)
    {
        if (!_cells.TryGetValue(name, out var h)) _cells[name] = h = create();
        return h;
    }

    /// <summary>Register <paramref name="describer"/>'s description of external <paramref name="name"/> and check
    /// its conformance against every OTHER describer already registered in the run unit (ISO §14.8.4; raise points
    /// §14.9.4.4 GR3e / §14.9.23.4 GR7d). <paramref name="gate"/> is the pre-ANDed enablement mask — CALL-site
    /// mask &amp; activated element's before-Environment-division mask (§14.8.4.1's both-elements rule) — so a
    /// zero gate stores without checking (checking not enabled ⇒ the condition is not raised, §14.6.13.1.1).
    /// A detected violation whose bit is in the gate throws <see cref="CobolCallException"/> carrying the Table 13
    /// level-3 name: the activation unwinds to the CALL site ("the program call is not successful", §14.9.4.4 GR3e), where
    /// the ON EXCEPTION phrase or the §14.6.13.1.3 sequence takes it (§14.9.4.4 GR3h).</summary>
    public void Describe(string describer, string name, ExternalDescriptor desc, ExternalChecks gate)
    {
        // An external FILE CONNECTOR and an external RECORD are different external object classes that may share
        // one name (an FD IS EXTERNAL externalizes both under the FD name, §13.18.22.4 GR4a/GR4b) — bucket by
        // kind so the two registrations never overwrite or cross-compare.
        string key = desc.Kind + ":" + name;
        if (!_describers.TryGetValue(key, out var entries))
            _describers[key] = entries = new Dictionary<string, ExternalDescriptor>(ExternalizedNames.Comparer);
        if (gate != ExternalChecks.None)
            foreach (var (other, prior) in entries)
            {
                if (ExternalizedNames.Same(other, describer)) continue;
                if ((gate & ExternalChecks.FormatConflict) != 0 && desc.Kind == "record"
                    && (prior.ByteCount != desc.ByteCount || prior.ValueImage != desc.ValueImage
                        || prior.StrongTypeKey != desc.StrongTypeKey || prior.ConstantRecord != desc.ConstantRecord))
                    throw new CobolCallException(
                        $"external record '{name}': the descriptions in '{other}' and '{describer}' do not conform "
                        + $"— byte count {prior.ByteCount} vs {desc.ByteCount}, VALUE/strong-type/CONSTANT-RECORD identity "
                        + "(ISO §14.8.4.3 / §13.18.22.4 GR6 — EC-EXTERNAL-FORMAT-CONFLICT)",
                        "EC-EXTERNAL-FORMAT-CONFLICT");
                if ((gate & ExternalChecks.DataMismatch) != 0 && desc.Kind == "file"
                    && (AnyNonExternalRef(prior) || AnyNonExternalRef(desc)                      // §14.8.4.2 conjunct 1: SHALL BE external
                        || prior.FileStatusRef != desc.FileStatusRef || prior.RelativeKeyRef != desc.RelativeKeyRef
                        || prior.LinageRef != desc.LinageRef))                                   // conjunct 2: same corresponding item
                    throw new CobolCallException(
                        $"external file '{name}': the FILE STATUS / LINAGE / RELATIVE KEY data items of '{other}' and "
                        + $"'{describer}' are not external data items and/or do not refer to the same corresponding "
                        + "storage in each runtime element (ISO §14.8.4.2 — EC-EXTERNAL-DATA-MISMATCH)",
                        "EC-EXTERNAL-DATA-MISMATCH");
                if ((gate & ExternalChecks.FileMismatch) != 0 && desc.Kind == "file"
                    && prior.SelectFingerprint != desc.SelectFingerprint)
                    throw new CobolCallException(
                        $"external file '{name}': the file control entries of '{other}' and '{describer}' are not "
                        + $"consistent ('{prior.SelectFingerprint}' vs '{desc.SelectFingerprint}') "
                        + "(ISO §14.8.4.4 / §12.4.5.3 GR1 — EC-EXTERNAL-FILE-MISMATCH)",
                        "EC-EXTERNAL-FILE-MISMATCH");
            }
        entries[describer] = desc;
    }

    /// <summary>§14.8.4.2 conjunct 1: a present-but-non-external file-referencing item is the "!" sentinel (emitted by
    /// the codegen when a specified FILE STATUS / RELATIVE KEY / LINAGE item is not an external data item). It is a
    /// whole ";"-separated token — the entire single string for FILE STATUS / RELATIVE KEY, one token among several
    /// for LINAGE (alongside a dotted external-item identity or an "=&lt;integer&gt;" literal, neither of which can
    /// contain "!" or ";"), so whole-token equality is exact. A null ref (clause unspecified) is not a violation. This
    /// closes the both-"!" false negative — two non-external describers used to compare EQUAL (<c>"!" == "!"</c>).</summary>
    private static bool AnyNonExternalRef(ExternalDescriptor d) =>
        IsNonExternal(d.FileStatusRef) || IsNonExternal(d.RelativeKeyRef) || IsNonExternal(d.LinageRef);

    private static bool IsNonExternal(string? r) => r is { } s && s.Split(';').Contains("!");
}

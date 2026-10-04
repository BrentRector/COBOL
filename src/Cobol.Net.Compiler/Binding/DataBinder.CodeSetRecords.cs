// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding.Model;
using CobolNet.Editions.Diagnostics;

namespace CobolNet.Binding;

/// <summary>
/// The CODE-SET clause's record-description screen — <b>ISO §13.18.13.3 SR3 a) and b)</b>: <i>"if alphabet-name-1
/// is specified, all elementary data items of all record description entries associated with the file shall be
/// described as usage display, and any signed numeric data items shall be described with the SIGN IS SEPARATE
/// clause"</i> (b) says the same of alphabet-name-2 over usage national). <b>§13.18.52.3 SR3</b> is the same rule
/// written from the SIGN clause's side — <i>"If the CODE-SET clause is specified in a file description entry, any
/// signed numeric data description entries associated with that file description entry shall be described with
/// the SIGN IS SEPARATE clause"</i> — so ONE screen answers both, and it is this one.
///
/// <para><b>Why a declared PASS and not <c>BindCodeSetClause</c> (kb/Work PB536).</b> The screen asks what a
/// record item IS DESCRIBED WITH, and two of the three facts it reads are settled by LATER passes than the
/// file-section entry walk that used to run it:</para>
/// <list type="bullet">
/// <item>the SIGN, because <b>§13.18.52.4 GR1</b> — <i>"The SIGN clause specifies the position and the mode of
/// representation of the operational sign for the numeric item to which it applies, or for each numeric item
/// subordinate to the group to which it applies"</i> — makes a GROUP-level clause describe every subordinate
/// signed numeric item, and that propagation is <see cref="InheritSignClauses"/>;</item>
/// <item>the USAGE, because <b>§13.18.60.4 GR1</b> gives a group's usage to its elementary items, and that
/// propagation is <c>UsageInheritancePass</c>;</item>
/// <item>and the record's LEAVES themselves, because a record described with TYPE or SAME AS has none until
/// <c>ExpandTypes</c> has composed them (§13.18.57.4 GR1 / §13.18.49.4 GR1).</item>
/// </list>
/// <para>Run from inside <c>BindFileSection</c>, the screen read the entry-bind default <c>SignKind</c> — trailing
/// over-punch — for every leaf, and so REFUSED the legal <c>01 R. 05 G SIGN IS LEADING SEPARATE. 10 N PIC S9(4).</c>
/// under a CODE-SET FD with COBOLNET1672, while the compiler's OWN storage model gave N a separate sign character.
/// A screen that reads an EFFECTIVE description fact must run no earlier than the pass that settles it; placing it
/// at <see cref="PassPhase.SignResolved"/> is what makes that true by construction rather than by care, and the
/// pipeline's phase contract (<c>BindPipeline.ValidateDag</c>) asserts it at startup.</para>
///
/// <para><b>What stayed behind.</b> Everything in <c>BindCodeSetClause</c> that reads the CLAUSE rather than the
/// records — SR1/SR2 alphabet resolution and class, SR3's "not both alphabets" sentence, and the GR2/GR6 medium
/// decision — still runs at the FD, because those facts are complete the moment the clause is read. Only the
/// selected CLASS travels, on <see cref="FileModel.CodeSetRecordClass"/>.</para>
///
/// <para><b>Edition axis.</b> Plain syntax rules at every edition the CODE-SET clause exists in (85 through 2023);
/// the binder is edition-agnostic and the clause's own gates are <c>VersionConformancePass</c>'s.</para>
/// </summary>
public sealed partial class DataBinder
{
    /// <summary>§13.18.13.3 SR3 a)/b) over every FD that carried a CODE-SET clause — one verdict per offending
    /// elementary record item, reported AT THAT ITEM (the FD line named the file but never the item the
    /// programmer has to change).</summary>
    internal void CheckCodeSetRecordItems()
    {
        foreach (var file in _files)
        {
            if (file.CodeSetRecordClass is not { } wantNational) continue;
            // SR3's antecedent — "If there are record description entries associated with the file" — is about the
            // entries the program WROTE: the implied record an FD without any receives (§14.9.30.4 GR6) is not one,
            // so a file with none has nothing to screen and CODE-SET naming either alphabet (or both, which
            // §13.18.13.4 GR4's second sentence expressly contemplates) is legal (kb/Work PB1219).
            foreach (var rec in file.WrittenRecords) CheckCodeSetRecordItem(rec, wantNational);
        }
    }

    /// <summary>The SR3 a)/b) walk of one record description entry. Elementary items only — both sentences say
    /// "all elementary data items" — and the sign sentence is asked only of the items the usage sentence
    /// admitted, exactly as the rule's "and" joins them.</summary>
    private void CheckCodeSetRecordItem(DataItem it, bool wantNational)
    {
        foreach (var child in it.Children) CheckCodeSetRecordItem(child, wantNational);
        if (it.Pic is not { } pic || it.Children.Count > 0) return;   // elementary items only (SR3 a/b)
        using var _ = Edition.At(it);
        bool right = !wantNational && pic.Usage is Usage.Display || wantNational && pic.Usage is Usage.National;
        if (!right)
            Edition.Error(DiagnosticCatalog.CodeSetClauseViolation, $"CODE-SET: the record item "
                + $"'{it.CobolName ?? "FILLER"}' is not usage {(wantNational ? "NATIONAL" : "DISPLAY")} — all "
                + "elementary data items of all record description entries shall be described as usage "
                + $"{(wantNational ? "national" : "display")} (ISO §13.18.13.3 SR3 {(wantNational ? "b" : "a")})");
        // §13.18.13.3 SR3 a/b sentence 2 = §13.18.52.3 SR3. "Described with the SIGN IS SEPARATE clause" includes
        // a clause INHERITED from a containing group — §13.18.52.4 GR1 applies the group's clause "for each
        // numeric item subordinate to the group to which it applies" — which is why this pass runs after
        // InheritSignClauses rather than at the FD (kb/Work PB536).
        else if (pic is { Signed: true } sp && !sp.SignKind.Contains("Separate"))
            Edition.Error(DiagnosticCatalog.CodeSetClauseViolation, $"CODE-SET: the signed numeric record "
                + $"item '{it.CobolName ?? "FILLER"}' shall be described with the SIGN IS SEPARATE clause "
                + $"(ISO §13.18.13.3 SR3 {(wantNational ? "b" : "a")}; §13.18.52.3 SR3)");
    }
}

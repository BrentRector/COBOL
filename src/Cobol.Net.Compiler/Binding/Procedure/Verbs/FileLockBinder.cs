// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding.Bound;
using CobolNet.Binding.Model;
using CobolNet.Editions.Diagnostics;
using CobolNet.Frontend.Generated;

namespace CobolNet.Binding.Procedure;

using Core = CobolParserCore;

/// <summary>
/// The COBOL-2002 file-sharing / record-locking verb binder (ISO §14.9.47 UNLOCK, the READ/WRITE/REWRITE
/// record-lock phrases §14.9.30/.51/.35, the RETRY phrase §14.7.9). The SHARING clause + OPEN SHARING phrase +
/// connector-registry semantics live in DataBinder / BindOpen / the runtime; this collaborator binds UNLOCK,
/// validates the per-verb lock-phrase syntax rules (COBOLNET1512 for the AUTOMATIC-locking ban §14.9.30 SR4 and
/// its WRITE/REWRITE twins; COBOLNET1818 for §14.9.30 SR3's IGNORING-LOCK/LOCK exclusion), and supplies the lock/RETRY services the
/// verb binders thread into their bound nodes — READ/WRITE/REWRITE carry <c>Lock</c>+<c>Retry</c> (all
/// organizations), DELETE RECORD / DELETE FILE carry <c>Retry</c> (§14.9.10 GR6/GR15) — which the emitters
/// route through the runtime's governed entries (ReadShared/ReadKeyedShared/WriteShared/RewriteShared/
/// DeleteShared). <c>BindRetry</c> stays a shared spine member on the host (OPEN consumes it directly).
/// </summary>
internal sealed class FileLockBinder(BinderContext ctx, StatementBinder host)
{
    /// <summary>Bind <c>UNLOCK file [RECORD[S]]</c> (ISO §14.9.47): release this connector's record locks on the
    /// file. SR1 — not a sort/merge file.</summary>
    public BoundStatement BindUnlock(Core.UnlockStatementContext ul)
    {
        // UNLOCK is a COBOL-2002 introduction (§14.9.47), parsed at ALL editions (superset grammar). The edition
        // gate moved to the post-bind VersionConformancePass (rearch PHASE-03 Step 14b) — it fires on the
        // self-identifying BoundUnlock node this method produces, so the binder is edition-agnostic here.
        string name = ul.fileName().GetText();
        // The ONE file-name resolution step (kb/Work PB236 — §8.4.2.1 through COBOLNET1639); the old
        // BoundUnsupported staged an UNDECLARED file-name to a run-time loud with no compile-time word.
        if (!ctx.Validation.ResolveFile(ul.fileName(), "UNLOCK", out var file)) return BoundRejected.Reported(ctx.Edition);
        if (file.IsSortMerge)
            ctx.Edition.Error("COBOLNET1512", $"UNLOCK may not name the sort/merge file '{name}' "
                + "(ISO §14.9.47.3 SR1)");
        ctx.Data.ScreenApplyCommitSubject(file, true, "UNLOCK", "ISO §14.9.47.3 SR2: \"File-name-1 shall not be a "
            + "file specified in an APPLY COMMIT clause\"");   // kb/Work PB666
        return new BoundUnlock(file, ul.RECORDS() is not null || ul.RECORD() is not null);
    }

    /// <summary>Bind and validate a verb's LOCK-RETENTION phrase — the printed <c>[ WITH LOCK | WITH NO LOCK ]</c>
    /// bracket of ISO §14.9.30.2 / §14.9.51.2 / §14.9.35.2 — against the file's effective LOCK MODE (§14.9.30 SR4
    /// / §14.9.51 SR22 / §14.9.35 SR4 → COBOLNET1512): no explicit lock phrase is permitted when the effective
    /// LOCK MODE is AUTOMATIC, because the lock is then implicit. Called from the READ/WRITE/REWRITE binders,
    /// which thread the returned <see cref="BoundRecordLock"/> into their bound nodes for the emitters'
    /// governed-verb routing (§9.1.16).
    /// <para>⛔ THE DOC COMMENT HERE USED TO CLAIM IT ENFORCED SR3 ("IGNORING LOCK and WITH LOCK are mutually
    /// exclusive") AND THE BODY NEVER DID (kb/Work PB331): the only test was the AUTOMATIC one, which is SR4.
    /// The forbidden pair was unwritable purely because the grammar had merged the two printed brackets, so
    /// splitting them correctly would have opened a silent hole. SR3 now has a real check —
    /// <see cref="CheckIgnoringLock"/> — and this method's contract is exactly bracket 2.</para></summary>
    public BoundRecordLock CheckRecordLockPhrase(FileModel file, Core.RecordLockPhraseContext? lock_, string verb)
    {
        if (lock_ is null) return BoundRecordLock.None;
        // The lock-retention phrase is a COBOL-2002 introduction; its edition gate (RecordLockPhrase2002) fires on
        // RECOGNITION in the VersionConformancePass parse-arm (VisitRecordLockPhrase, the verb named from the parent
        // statement type); Step 14h.4a. This method keeps only the §14.9.30/.51/.35 SR validation.
        var kind = lock_.NO() is not null ? BoundRecordLock.WithNoLock : BoundRecordLock.WithLock;
        ScreenLockPhraseAgainstAutomatic(file, verb, kind == BoundRecordLock.WithNoLock ? "WITH NO LOCK" : "WITH LOCK");
        return kind;
    }

    /// <summary>Bind and validate the IGNORING LOCK alternative of READ's OTHER lock bracket — the printed
    /// <c>[ ADVANCING ON LOCK | IGNORING LOCK | retry-phrase ]</c> of ISO §14.9.30.2, which §5.2.6.1 makes an
    /// INDEPENDENT selection from the retention bracket. Enforces the two syntax rules that name the phrase:
    /// <list type="bullet">
    /// <item>SR3 — <i>"The LOCK phrase shall not be specified in the same READ statement as the IGNORING LOCK
    /// phrase."</i> ⚠ "The LOCK phrase" is WITH LOCK and NOT WITH NO LOCK: §14.9.30.4 GR11 b) names "the NO LOCK
    /// phrase" and GR11 d) "the LOCK phrase" as different phrases in this same statement, so
    /// <c>IGNORING LOCK WITH NO LOCK</c> is legal and shall compile.</item>
    /// <item>SR4 — the AUTOMATIC-locking ban lists IGNORING LOCK among its three phrases, so it applies here too.
    /// </item></list></summary>
    public bool CheckIgnoringLock(FileModel file, Core.ReadLockContentionPhraseContext? contention,
        BoundRecordLock retention)
    {
        if (contention?.readIgnoringLock() is null) return false;
        ScreenLockPhraseAgainstAutomatic(file, "READ", "IGNORING LOCK");
        if (retention == BoundRecordLock.WithLock)
            ctx.Edition.Error(DiagnosticCatalog.ReadIgnoringWithLock,
                $"READ on file '{file.CobolName}': the LOCK phrase shall not be specified in the same READ "
                + "statement as the IGNORING LOCK phrase (ISO §14.9.30.3 SR3). WITH NO LOCK is a DIFFERENT "
                + "phrase (§14.9.30.4 GR11 b)/d)) and may be combined with IGNORING LOCK.");
        return true;
    }

    /// <summary>§14.9.30.3 SR4 / §14.9.51.3 SR22 / §14.9.35.3 SR4 — <i>"If automatic locking has been specified
    /// for file-name-1, none of the phrases IGNORING LOCK, WITH LOCK, or WITH NO LOCK shall be specified."</i>
    /// ONE screen for all three phrases, so the two brackets cannot drift apart on the rule that names both.
    /// <para>The APPLY COMMIT twin rides the same screen for the same reason (kb/Work PB666): §14.9.30.3 SR5 forbids
    /// all three phrases on a READ of a file subject to an APPLY COMMIT clause and §14.9.35.3 SR5 the two retention
    /// phrases on a REWRITE. WRITE's format has the retention bracket and §14.9.51.3 states no such rule, so a
    /// WRITE is not asked.</para></summary>
    private void ScreenLockPhraseAgainstAutomatic(FileModel file, string verb, string phrase)
    {
        if (file.LockMode is { Kind: LockKind.Automatic })
            ctx.Edition.Error("COBOLNET1512", $"{verb} on file '{file.CobolName}': the {phrase} phrase may not "
                + "be specified when the file's LOCK MODE is AUTOMATIC — the lock is implicit "
                + "(ISO §14.9.30.3 SR4 / §14.9.51.3 SR22 / §14.9.35.3 SR4)");
        string? applyCommitRule = verb switch
        {
            "READ" => "ISO §14.9.30.3 SR5: \"If file-name-1 is subject to an APPLY COMMIT clause, none of the phrases "
                + "IGNORING LOCK, WITH LOCK, or WITH NO LOCK shall be specified\"",
            "REWRITE" => "ISO §14.9.35.3 SR5: \"If the rewrite file is subject to an APPLY COMMIT clause, neither the "
                + "WITH LOCK phrase nor the WITH NO LOCK phrase shall be specified\"",
            _ => null,
        };
        if (applyCommitRule is not null)
            ctx.Data.ScreenApplyCommitSubject(file, true, $"{verb} with the {phrase} phrase", applyCommitRule);
    }

    /// <summary>Bind a RETRY phrase on a verb (READ/WRITE/REWRITE/DELETE) — the same shape as OPEN's, and the
    /// same division of labour: the binder carries the form and its arithmetic expression, and every §14.7.9.3
    /// general rule is executed once at <c>FileRegistry.RetryLoop</c>. Returns null when absent.</summary>
    public RetrySpec? BindVerbRetry(Core.RetryPhraseContext? rp) => rp is null ? null : host.BindRetry(rp);
}

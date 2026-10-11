## Entry 564 — 2026-06-10 20:45 PDT — Qualified RECORD KEY / ALTERNATE KEY operands (§8.4.2.2) — IX215A compiles and EXCEEDS its golden (807 conformance)

**The rlix brief's C8, exactly as diagnosed:** `DataBinder.BindFileControl` captured key operands with
`GetText()`, gluing a qualified reference into `IX-FD3-KEYINIX-FD3-RECKEY-AREA` — unresolvable, so IX215A
(whose THREE same-named `IX-FD3-KEY` items under three areas are the POINT of the test, §8.4.2.2) failed with
COBOLNET0863/0862. The capture now keeps the base word + the IN/OF qualifier words (`KeyReference` — the FILE
STATUS / RENAMES capture pattern applied to keys; §12.4.5.12.3 SR2 / §12.4.5.6 SR2 place the item within the
file's records, nothing restricts it to an unqualified name), and `ResolveFiles` resolves through the new
`FindQualified` (base matches the item, each qualifier matches SOME ancestor in written innermost→outermost
order with skips — the §8.4.2.2 rule). START's key operand already went through the ONE reference resolver
(qualification worked); its SR6 storage-position matching then works unchanged. New differential test
(`Indexed_QualifiedRecordAndAlternateKeys_ResolveByQualifier`) locks the shape against the legacy oracle.

**IX215A now compiles and runs all 39 tests PASS — and EXCEEDS its golden, joining IX210A/IX214A in the C9
family:** zero produced-side diffs; the golden's only divergence is the `*** INFORMATION ***NO FURTHER
INFORMATION, SEE PROGRAM.` line after EVERY row (PASS rows included). Verified in IX215A's own source: that
text is printed ONLY by FAIL-ROUTINE when COMPUTED-X is space, and PRINT-DETAIL reaches FAIL-ROUTINE only when
`P-OR-F = "FAIL*"` — impossible on a PASS row under any conforming reading of the IF (§14.9.17). The golden
fossilizes a legacy control-flow quirk. **Swept-only pending owner sign-off to re-baseline IX210A/IX214A/
IX215A from the conforming runs** (per the brief's C9 recommendation). **807 conformance + 15 unit green.**


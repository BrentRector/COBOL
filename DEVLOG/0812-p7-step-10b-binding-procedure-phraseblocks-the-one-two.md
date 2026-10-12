## Entry 812 — 2026-07-11 19:03 PDT — P7 Step 10b: `Binding/Procedure/PhraseBlocks` — the ONE two-branch phrase extractor replaces the 8 per-verb clones

**What (the phase doc's ON/NOT-ON extractor, with the audit's shape corrections).** New
`Binding/Procedure/PhraseBlocks` (the Step-10 collaborator directory's first resident):
`Split(blocks, notFirst, bind)` + the `StartsWithNot` discriminator (moved from StatementBinder). The key
design point the audit demanded: Split's POSITIONAL SWAP (`notFirst ? (b1, b0) : (b0, b1)`) is TOTAL over
every legal grammar shape — NOT-only (one NOT-led block) → (null, b0); RETURN's §14.9.34.3 SR4 reversed
PAIR (two NOT-led blocks) → (b1, b0); the normal order → (b0, b1) — so RETURN's hand-rolled reversal
disappears into the same call every other site makes, and the three rules whose grammar cannot produce a
NOT-led form (readAtEnd / writeAtEndOfPage / deleteFileOnException) pass `StartsWithNot` as a provable
no-op. Converted: the 4 inline clones (sequential READ AT END, WRITE AT EOP, keyed READ AT END, DELETE FILE
ON EXCEPTION), the RETURN reversed site, and the 3 per-record helpers (`BuildSizeError`,
`KeyedInvalidPhrase` — now one-line record wrappers over Split; `StrUnstrOverflow` DELETED, its two callers
call Split directly). CALL is correctly NOT converted — it binds two separate grammar contexts, no
pair-extraction clone exists there (the audit's stale-anchor finding).

**Verify.** Sln Debug clean; 33 characterization (32 snapshots byte-exact) · 281 unit · 3166 conformance —
verdicts read as separate actions.


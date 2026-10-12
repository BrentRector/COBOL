## Entry 595 — 2026-07-03 21:02 PDT — The W2 adversarial review: 6 confirmed / 0 refuted / 7 minors — every actionable finding FIXED in-session

**Three refute-oriented lenses (spec / regression / test-quality) + per-finding adversarial verification (9
agents, 760k tokens) over commit a3e29b6. Every serious finding verified REAL; fixes landed with the W1.5
batch (one commit — the tree interleaves shared files):**
1. **Trailing `,` separator misbind** — the `;` bug's exact twin (`77 X PIC 99, VALUE 3.` silently classifies
   numeric-edited "99,"; §8.3.5 r2 + §13.18.40.3 SR7 prove the source conforming). Analyze CANNOT fix it
   (SR7 needs clause-position context) → DOCUMENTED (VCR Table 7 row 7.14), the W3 lexer cure owns BOTH
   separators. Also fixed the strip-to-empty leak: Analyze now strips exactly ONE trailing `;` and 0808s an
   empty/multi-`;` picture (`PIC ;` no longer births a zero-length item).
2. **USAGE INDEX receivers rode the 0902 edition row** — "permitted through 2014" was a FALSE spec claim:
   §14.9.25.3 SR1 + §13.18.60 GR10 prohibit class-index MOVE operands at EVERY edition. NEW version-invariant
   COBOLNET0809 (sender + receiver arms, error even permissive); Usage.Index exempted from the SR5 rows.
3. **Ref-mod slice round-trip loss** — `MOVE SPACE TO N(1:2)` printed [003]/NUM (the spliced image round-
   tripped through the backing long; the result FLIPPED with whether unrelated code image-backed N).
   §8.4.2.4/§8.4.3.3.4 GR5 want [  3]/NOTNUM. Fixed: `MarkRefModStoreImage` — EVERY MOVE with a ref-mod
   target over an eligible numeric-DISPLAY item image-backs it at bind time (any sender; digits round-trip
   either way). Verified: [  3]/NOTNUM + [AB6], and the slice MOVE stays legal at 2023 strict.
4. **QUOTE→numeric is 2014-OBSOLETE** (Annex E.2 item 21 — the ONE figurative the change annex tracks
   separately; VCR row 1's blanket "not even flagged obsolete in 2014" was wrong for it). NEW dual row
   `move-quote-numeric-obsolete-2014` (85 / obsolete 2014 / removed 2023): 0903 warning at 2014, 0902 at
   2023, silent at 85/2002. ObsoleteMatrix theory bounded below removedIn; the obsolete warning asserts the
   FIXED 0903 band code (the contract), expectDiagnostic keeps the removal code. VCR row 28 → PARTIAL.
5. **exit-method/function**: the 0900-at-85 introduction leg was untested (now pinned in
   EditionGateDiagnosticTests) and the pending rationale was stale — corrected: the sources compile at
   2002/2014 only because the placement SR is unimplemented (runtime-loud stub, not a silent misbind);
   conforming positive sources need METHOD-ID/FUNCTION-ID units (Phase 3/4c).
6. Minors fixed: `.err` files now pin construct-specific substrings (all 18 re-verified against the CLI);
   the 0808 wording no longer overclaims SR2 "combination" (membership only; the §13.18.40.6 precedence-table
   pass queued); `'$'` is whitelisted ONLY as the actual currency picture symbol (PIC $$$9 under CURRENCY "W"
   now 0808s — the ungated always-on leniency closed; NC107A/NC108M carry no `$` pictures, corpus-safe);
   the loose "Phase 4" assertions are delimiter-safe; programName/section band-token slots have end-to-end
   0901 facts; HIGH/LOW-VALUE pre-removal fills pinned by comparison (legacy CBL0906 makes them
   un-cross-checkable). Documented residue (VCR row 1 + registry citation): Tier-A REDEFINES-class and
   BINARY/PACKED/COMP-5/float receivers keep the NARROW runtime-loud guard (legal-per-2014 shapes, zero
   corpus hits).


## Entry 443 — Docs stand on their own (provenance stripped) + a maintained doc index (DOC_INDEX.md)

Owner follow-ups to the consolidation (442): (1) *"reference docs do not need mention of prior dead ends or now-deleted
docs — they should stand on their own"*; (2) *"add a table-of-contents of available docs + their subject matter,
referenced by CLAUDE.md, so future sessions know what docs exist, what they contain, and which to update as the
compiler evolves."*

**(1) Provenance strip (7 agents over 25 reference docs).** Removed all consolidation archaeology — "Consolidated from N
prior docs", "(see Provenance below)" + Provenance footers, "MERGED A/B/C (from <doc>)" section labels (kept the content,
renamed headings to describe it), inline "the prior essay said X" notes (rewritten as standalone facts), and every
reference to a now-deleted doc. The status banners stay but were reworded to be self-contained (subject · real status ·
stack .NET 10 / C# 14 · → MASTER_PLAN). Legitimate forward context ("the byte engine is being islanded") was kept — it's
the current direction, not a dead end. Verified: the only residual hits are false positives (a doc's own title
containing a deleted doc's name as a substring — "Console I**O & Environment-Variable**", "**CobolSharp Debugger
Architecture** — Breakpoints", "CI-**CD, Version Pinning**" — and "provenance" as the SLSA/SBOM supply-chain term in the
packaging doc).

**(2) `docs/DOC_INDEX.md`** — the map of all 126 docs: 9 categorized tables (plan SSOTs · doctrine · decomposition
guides · frontend · subsystem design-references · per-feature plans · terminal/screen · ledgers · spec-text), each row =
doc · type (LIVE/DESIGN/LEDGER/SPEC) · subject. Subjects auto-extracted from each doc's banner/intro (heading/metadata
lines skipped) with the high-value SSOT rows hand-authored. Opens with a **maintenance guide**: which docs to keep
current, the one-canonical-per-subsystem rule (extend, never fork), and "new subsystem ⇒ new doc with a status banner +
a new index row." Referenced from `CLAUDE.md` (a new "DOC MAP" section). Docs-only — guard unaffected.


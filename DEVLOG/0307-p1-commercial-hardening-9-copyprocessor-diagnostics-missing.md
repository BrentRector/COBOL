## Entry 307 — P1 commercial-hardening #9: CopyProcessor diagnostics (missing / circular / depth)

Fourth P1 item, Deliverable A. `CopyProcessor` was a pure string→string transform with no diagnostic
channel: a missing copybook was turned into a transparent COBOL comment (`*> COPY X — copybook not found`)
and continued, so an unresolvable COPY produced a cascade of downstream "undefined name" noise instead of
the real cause; circular includes hit the same branch (mislabeled "not found"); exceeding the 20-level
nesting limit silently returned unexpanded text.

**Change.** Thread an optional `DiagnosticBag` + source file name + `strict` flag through the `CopyProcessor`
primary constructor (all optional, so the standalone `preprocess` CLI and the existing unit test still
construct it with no bag → silent, behavior unchanged). `Compilation.Preprocess` now passes the bag (created
before Preprocess) and `strict = Options.Dialect != Default`. In `ExpandCopyStatements`:
- missing copybook → **CBL3620** (ISO §7.2.3.4 GR 2), *dialect-gated* to strict — Default/--nist keep the
  lenient comment fallback, so the NIST copy-library suite is unaffected by construction;
- circular include → **CBL3621** (ISO §7.2.3.3 SR 1), unconditional (split out from the old shared "not
  found" branch — it was previously mislabeled);
- depth > 20 → **CBL3622**, unconditional.
CBL3621/3622 are safe to fire unconditionally because no corpus program is circular or >20-deep. A `LineOf`
helper gives an approximate line within the (current) text for the location.

**Deferred (documented).** (a) Malformed COPY…REPLACING / REPLACE diagnostics — missing `BY`, unterminated
`==` pseudo-text, empty pseudo-text-1 (CBL3623-3625) — live in the static REPLACE-parsing methods and are
rarer; a separate pass. (b) Deliverable B (copybook source-mapping: diagnostics that point at the copybook
file+line) needs a preprocessor line-provenance map and is coupled to item-7's SourceLocation — deferred.

**Verify:** build 0 warnings; +4 unit (`CopyDiagnosticTests`: missing copybook strict → CBL3620, Default →
none + comment fallback; resolved copybook strict → none; A→B→A circular → CBL3621). Full guard **ALL GREEN
— 1019 unit (1015 +4) / 347 integration / 349 NIST baselines, 0 FAIL\*** — confirms the COPY-heavy SM suite
and X-card copybooks still resolve under Default.


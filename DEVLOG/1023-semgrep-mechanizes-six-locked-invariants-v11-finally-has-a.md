## Entry 1023 — 2026-07-26 15:52 PDT — Semgrep mechanizes six locked invariants; V11 finally has a number (423), and a self-inflicted lesson about rules that silently never fire

Plugin audit first, since it framed everything else. Of the 255 plugins in the marketplace, ~250 are SaaS
integrations with no bearing on a self-contained compiler. Two were worth installing and one was worth deleting:

- **`semgrep`** and **`serena`** installed — both cost **zero** always-on tokens (they are MCP servers; nothing
  enters context until invoked).
- **`pr-review-toolkit` removed** (owner's call, and the numbers agreed): 2,038 always-on tokens of a 3,010 total,
  paid EVERY turn for six generic agents used once per batch — agents that know nothing about ISO citations,
  edition gating, or the byte-substrate ban. Replaced by a project `review` skill costing ~100 always-on, with the
  owner's four required dimensions and this project's actual criteria. **Plugin budget 3,010 → 972 per turn.**
- Explicitly rejected: `code-modernization` (1,138 always-on). Its description says "Modernize legacy codebases
  (COBOL, …)" and it is the most tempting name in the catalog — but its workflow is assess → extract-rules →
  reimagine → **transform**, i.e. converting COBOL *applications* into other languages. We build the compiler. It is
  pointed the wrong way.

**The substance: doctrine became lint.** `COBOLNET_DESIGN.md` §1.2's four locked invariants were prose only a
reviewer would catch, and only if they remembered. Six of them are now Semgrep rules
(`scripts/semgrep/invariants.yml`): banned `decimal`/`BigInteger` in the runtime value model, persisted `byte[]`
fields, silent `// TODO` against the loud-failure invariant, raw diagnostic-code literals, and a bound node
carrying rendered text against backend neutrality. Semgrep parses C# natively on Windows at ~98% (pip install; the
old WSL-only constraint is gone).

**⛔ The lesson, and it is the important part of this entry: two rules SILENTLY NEVER FIRED.** I invented a
project-local suppression convention — a `pattern-not-inside` matching a `// semgrep-ok(rule): reason` comment.
Comments are not matchable context in Semgrep, and the construct did not error: it **disabled the rules while the
scan reported "Scan completed successfully"**. Two more rules were dead for a different reason (`$MODS byte[] $NAME`
and a positional-record parameter pattern are not expressible in Semgrep's C# grammar). Four of six rules were
broken and every one of them looked green.

I only caught it because I ran a negative control — a file of deliberate violations — instead of trusting a clean
scan. That is the same trap as calling a red gate a "flake": absence of findings was read as absence of defects.
Hence `scripts/semgrep/testdata/Violations.cs` and `scripts/semgrep/verify.py`, which asserts EVERY rule still
fires on the deliberate violations before it will report on the real tree. Suppression now uses Semgrep's built-in
`// nosemgrep: <rule-id> - <reason>` — the existing mechanism, not a second one I invented, which is the
singular-pattern rule applied to my own tooling.

**What the rules found on a supposedly clean tree, after four rounds of tightening (19 → 9 → 8 → the real set):**

- **`raw-diagnostic-code-literal`: 423.** This is ledger item **V11** — the compiler-channel raw `edition.Error(
  "COBOLNET0886", …)` calls that bypass `DiagnosticCatalog`. Plan §0 has always described V11 as known, accepted
  debt, but **nobody had ever quantified it.** It is 423 call sites. The baseline locks that number so it cannot
  grow, and it becomes V11's burn-down metric — the same role the inventory GAP plays for P14.
- **`no-decimal`: 3** in `CobolDate.cs` — runtime code using the type invariant 2 bans outright.
- **`no-biginteger`: 4** in `CobolNum.cs`, `PicInfo.cs`, `CobolIntrinsics.Text.cs` — in the runtime value model.
- **`bound-node-carries-rendered-text`: 3** in `BoundTree.cs` — candidate backend-neutrality seam violations, the
  invariant that is supposed to be *proven* by a second backend.

Rule-tightening was itself instructive: the first `decimal`/`BigInteger` rules flagged `IntrinsicBinder`'s
HIGHEST/LOWEST-ALGEBRAIC folding, where `(1 << 128) - 1` genuinely exceeds `Int128`. That is compile-time metadata
arithmetic feeding a rendered literal, not runtime storage — invariant 2 governs representation at run time. The
rules are now scoped to the runtime value model, with the reason recorded inline. Likewise the first
raw-literal rule matched 633 sites by sweeping up the generated `ConstructRegistry.g.cs` and comments that merely
*discuss* a past code collision; narrowing to the actual bypass shape (a literal passed to an emit API) is what
produced the honest 423.

The ten findings behind those numbers are owner decisions, not mine to silently "fix" — they are recorded here and
the baseline prevents drift while they are adjudicated.


## Entry 923 — 2026-07-19 18:55 PDT — Session bootstrap for the grammar batch: a ~40-site diagnostic-code drift in the remaining-waves scout, and the Wave H legacy-token risk CLEARED

Started the P13 grammar batch (the top of the D16 close-line). Bootstrap per plan §0: probe green
(`phase-13-m4-2023` @ `6d6c655d`, clean, pushed, next-free `COBOLNET1578`, 104 VCR todos, 166/109 corpus),
baseline battery launched, and a 7-construct research fan-out started. Before touching code, four pre-checks
turned up one large piece of drift and cleared one flagged blocker. Owner directive received mid-session:
*"update docs as you identify errors so they drift for a minimal amount of time"* — so this landed immediately
rather than being batched into the implementation commits ([[feedback_propagate_reconciliations]] updated with
the timing rule).

**1. The drift: `PHASE-13-remaining-waves-scout.md` duplicated live diagnostic-band state in ~40 places.**
The scout was produced by a 9-agent parallel re-scout, and **every wave section independently allocated codes
starting from the then-next-free 1570** — so Wave E, Wave F, Wave G, Wave H, Wave D, PICTURE EDITING,
PERFORM Fmt 3, SUPPRESS WHEN, COBOL-WORDS, PUSH/POP and FLAG-14 all claim overlapping numbers in 1570–1574.
They collide with each other *and* with what actually shipped: 1570–1577 are now fully consumed. The scout's
own hedge ("final numbering reconciled at implementation time") was too weak to stop a reader copying a number.

The fix is the plan's **single-write rule** applied literally: live state is written ONLY in §0; every other
site POINTS there. Renumbering ~40 speculative claims inline was explicitly rejected — most belong to waves
that haven't landed, so any renumbering re-drifts the moment one of them does. Instead: ONE authoritative
top banner that supersedes every local number, plus an **as-landed mapping table** (1570 `value-numeric-
edited-oversize` ← Wave G VCR 34's guess of 1570, the one that happened to match; 1571 `debug-sub-facility-
staged` ← Wave F's 1570; 1572 `merge-in-sort-merge-proc` ← Wave G VCR 27's 1571; 1573 `external-file-status-
consistency` ← Wave E VCR 18's 1570; 1575 `external-relative-key-consistency` ← Wave E VCR 31's 1571) so the
landed waves' sections stay *readable* rather than merely wrong. The still-unallocated sections (the grammar
batch, Wave D, Wave H) are named explicitly as needing fresh codes from §0. Seven of the most load-bearing
sites additionally got inline markers with symbolic placeholders (`[EDIT-A/B/C]`, `[PERF3-A/B]`,
`<SUPPRESS-SR7>`, `<PUSHPOP-GR2>`, `<FLAG14-TWIN>`) so a reader landing mid-document can't copy a dead number.

Worth noting the mechanism: this is the *same* failure class as the two collisions the plan-vs-spec review
already fixed (1573→1576, 1518→1577). Parallel agents each reading "next free = N" and each claiming from N
is a systematic hazard of the ≤10-agent fan-out pattern — which is exactly why plan §3 says *pre-allocate
diagnostic ranges before any parallel fan-out*. The scout predates that rule; this is its residue.

**2. The Wave H legacy-token blocker: CLEARED, no action needed.** The scout's MECHANISM CORRECTION
(DEVLOG 903) requires real `RECEIVE`/`SEND`/`VALIDATE` lexer tokens (an IDENTIFIER-led `statement` alternative
poisons the ALL(*) boolean-factor DFA), and flagged one open risk: a bare `RECEIVE`/`SEND` in a legacy guard
program would change its parse. The CM-series **is** present — `tests/nist/programs/CM{101,102,103,104,105,201,
202,303,401}M.cob`, 9 programs, containing genuine bare statements (`RECEIVE CM-INQUE-1 MESSAGE INTO
INCOMING-MSG`, `SEND CM-OUTQUE-1 FROM MSG-70 WITH EMI`) — **but all 9 are `pending` / `cataloged (not
asserted)` in `tests/nist/corpus.tsv` with no golden in `tests/nist/valid/`, so none executes in the guard.**
The token addition is legacy-safe. Recorded at the site that posed the question so it is never re-investigated.

Separately scanned the other six candidate tokens: `VALIDATE`/`EDITING`/`FINALLY`/`LOCATION` appear in the
corpus only inside comment lines, string literals, or as substrings of hyphenated user words (`SEND-SWITCH`,
`RECEIVE-ECHO-AND-LOG`, `EXCEPTION-LOCATION-N`) which lex as single IDENTIFIERs — no bare-word collision.
`COMMIT` appears as a deliberate user word in `tests/conformance/negative/user-word-commit.cob`
(`*> reject-at: 2023`), which is precisely why COMMIT/ROLLBACK take the diagnostic-layer route with no grammar
rule, and why every new token must go through the `cobolWord` nameSlot funnel rather than being unconditional.

**3. Two probe claims independently re-verified** (not taken on faith): next-free `COBOLNET1578` confirmed by a
full 15xx catalog scan, and the apparent duplicate `COBOLNET1535` is a *documented* two-rule reuse
(`DiagnosticCatalog.cs:186`, the 1533 pattern), not a third collision.

**4. The batch's real coupling identified.** `EDITING`, `FINALLY`, `LOCATION`, `COMMIT`, `ROLLBACK`, `VALIDATE`
are all already rows in `reserved-words.json` but have **no lexer tokens** — so six of the seven constructs
converge on `Core/CobolLexer.g4`. That, not any individual construct, is what makes this a single serial batch
under one full legacy guard. (`SUPPRESS` and `COLLATING` already have tokens.)

Docs-only change; no source touched, so the in-flight baseline battery is unaffected.


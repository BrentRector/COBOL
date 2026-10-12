## Entry 270 — USE declarative fires on a non-phrase exception (READ/WRITE/REWRITE/DELETE) → IX114A/115A/116A

IX114A/115A/116A open IX-FS3 I-O, CLOSE it, then READ / WRITE / DELETE it — expecting the not-open status
(47/48/49) **and** the `USE AFTER EXCEPTION ON IX-FS3` declarative to execute (which records the PASS and
GO TOs away). The statuses were already correct, but the declarative never fired, so the mainline ran and
recorded "SHOULD HAVE EXECUTED DECLARATIVES".

Two gaps in declarative emission (ISO §14.6.6 — the USE procedure services any exception condition the
statement's own handling phrase does not):
- **LowerWrite and LowerDelete never called `EmitUseDeclarative` at all** (only OPEN/CLOSE/READ/REWRITE
  did). A plain `WRITE`/`DELETE` that raised 48/49 silently fell through.
- **A READ/REWRITE *with* an AT END / INVALID KEY phrase skipped the declarative entirely** — the phrase
  branch returned first. But a phrase services only its own condition: `AT END` services at-end (10), an
  `INVALID KEY` phrase services 21/22/23/24. A *different* exception (47 not-open, …) must still fire the
  declarative even though a phrase is present (IX114A's `READ … AT END` got 47).

Fix — unified across all four statements: emit the USE-declarative check **before** the phrase branch, with
exclusion flags. `IrCheckUseDeclarative` / `FileRuntime.ShouldRunUseDeclarative` gained `excludeAtEnd` and
`excludeInvalidKey`: when set (because the statement carries that phrase), the declarative is suppressed for
exactly the conditions the phrase handles (10, or 21/22/23/24) but still fires for every other exception.
With no phrase, nothing is excluded (a phraseless READ at-end still fires the declarative — SQ203A GF-03).
This is why the gating is essential: without it, SQ203A GF-02's `READ … AT END` on a status-10 optional-
absent file would double-fire the AT END phrase *and* the declarative.

`LowerRead` restructured (declarative before the AT END / INVALID KEY branches); `LowerWrite` (excludeInvalidKey)
and `LowerDelete` (excludeInvalidKey) gained the call; `LowerRewrite` moved its existing call ahead of the
INVALID KEY branch with excludeInvalidKey so a 44/49/43 REWRITE fires the declarative even with an INVALID KEY
phrase present. IX114A/115A/116A → CLEAN, baselined. Guard ALL GREEN: 1000 unit / 347 integration / **247
NIST** (94 NC + 42 IF + 12 SM + 4 IC + 59 SQ + 19 RL + 17 IX), 0 regressions — the change touches every
file-I/O statement's declarative path; all declarative-bearing baselines (SQ141A/144A/203A/228A, IX114-116A)
hold. Remaining IX FAIL* (9): read-position 43/46 (IX119A/IX109A), optional-absent READ 10 (IX218A),
sequential WRITE ascending-key 21 (IX109A/112A), access-mode-aware DELETE/REWRITE (IX103A/203A/106A/110A),
variable-length indexed records (IX105A).


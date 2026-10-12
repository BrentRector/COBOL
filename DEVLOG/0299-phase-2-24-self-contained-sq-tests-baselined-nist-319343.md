## Entry 299 — Phase 2: 24 self-contained SQ tests baselined (NIST 319→343)

Worked the resume-prompt's "Phase 2 — SQ un-surveyed tail." The earlier file-I/O push (DEVLOG 237–268)
baselined 59 SQ and left ~24 un-surveyed; the maturing FILE-CONTROL/status subsystem has since made all of
them pass, but `run-suite.sh` couldn't be trusted to prove it (stale-`TF###` CLEAN). So I re-ran each of the
24 from a **fully clean** output dir (`rm TF*` before each run) — the guard's own start-clean semantics — and
all 24 came back 0 FAIL\* with EXECUTED>0:

  SQ103A SQ115A SQ122A SQ123A SQ125A SQ129A SQ132A SQ134A SQ135A SQ137A SQ138A SQ139A SQ140A SQ147A SQ148A
  SQ151A SQ152A SQ153A SQ205A SQ212A SQ215A SQ225A SQ226A SQ229A

Verification (each baseline must be real, deterministic, and order-stable):
- **Self-contained / order-independent.** None of the 24 reference a shared `XXXXD###` (consume) or `XXXXP###`
  (produce) chain file — they use only `XXXXX###`, which for SEQUENTIAL is program-id-qualified (DEVLOG 268/280),
  so each test creates/reads only its own isolated files. They passed from a clean dir with NO other program
  having run, i.e. they need no external producer. That also means their guard output is identical to their
  isolation output regardless of position — confirmed by the full guard run matching every captured baseline.
- **Non-vacuous.** The multi-test programs are substantial (SQ103A 30/30, SQ226A 37/37, SQ134A 15/15,
  SQ123A 9/9, SQ122A 7/7). The 1-of-1 programs are specific I/O-status condition tests, each checking a
  COMPUTED status against the ISO-required value — SQ129A "OPEN ABSENT FILE INPUT" (35 via a USE declarative),
  SQ137A "READ AFTER AT END" (46), SQ147A "READ CLOSED FILE" (47) — not blank-vs-blank passes.
- **Deterministic.** Each run twice from clean, reports diff-identical.

Captured all 24 baselines, appended them as one block after the existing SQ section in `NIST_TESTS`, full
guard ALL GREEN: 1000 unit / 347 integration / **343 NIST**, 0 regressions. **SQ suite COMPLETE:** all 85
SQ programs accounted for = **83 baselined** + SQ303M/SQ401M flagging (no CCVS report, excluded by design).


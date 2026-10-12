## Entry 992 — 2026-07-22 21:40 PDT — Code↔spec audit: 38 candidate conformance bugs the differentials structurally couldn't see

The design-doc audit (Entry 990) only caught bugs where a doc happened to describe the wrong thing. The owner asked to
audit the CODE more broadly. Ran a code-vs-spec audit (`wf_4ce42db6`, 14 agents, one per behavioral area:
arithmetic, MOVE, conditions, INSPECT/STRING/UNSTRING, intrinsics, files, tables/ref-mod, PICTURE/USAGE/VALUE,
PERFORM, inter-program, OO, exceptions/EC, ACCEPT/DISPLAY/SET, edition gating), each adversarially checking the
implementation against the ISO spec rule-by-rule.

**Result: 38 candidate conformance bugs across all 14 areas (24 blocker/major).** SSOT
`docs/rearchitecture/CODE-SPEC-AUDIT.md` (CA1–CA38). Examples: composite-of-operands wrongly counts the GIVING
receiver → REJECTS conformant `ADD A B GIVING C` (§14.9.2.3 SR1b); `ROUNDED MODE PROHIBITED` applied to an inexact
INTERMEDIATE quotient → spurious size error (§14.7.4.3 GR7); `MOVE` numeric-edited→numeric-edited skips de-editing →
`234.00` instead of `012.34` (§14.9.25.4 GR5). With the 6 design-doc code-bugs (V54–V59) that is ~44 candidate
conformance bugs. **CANDIDATE — agent-surfaced, VERIFY-then-fix; some will be false positives.**

**Why NIST / GnuCOBOL / our corpus missed them (the important part):** none is a SPEC oracle. NIST is COBOL-85 only
(zero post-85 coverage) + a fixed corpus with known holes + stdout-only. The GnuCOBOL differential is compile
accept/reject, not a runtime-output oracle, and GnuCOBOL ≠ the spec. Our own goldens are happy-path AND many derive
their "expected" output from the legacy byte-engine — and the audit repeatedly found the legacy carries the identical
bug, so a differential is BLIND to a bug both sides share (the same "port the legacy" root cause). The audit is the
first spec-first check, a different KIND of verification. **Durable fix: convert each VERIFIED finding into a
SPEC-DERIVED golden (expected value computed from the spec, not the legacy), closing the coverage gap where the
differentials are blind.**


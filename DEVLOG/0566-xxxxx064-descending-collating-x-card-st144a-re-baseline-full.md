## Entry 566 — 2026-06-10 20:56 PDT — XXXXX064 descending-collating X-card + ST144A re-baseline (FULL legacy guard re-proved) → ST144A locked (809 conformance)

**The last unsubstituted collating X-card** (st-chain brief §4.6): `XXXXX064` is the implementor's native
collating sequence in DESCENDING order as a 51-char literal — the exact mirror of the XXXXX063 ascending
literal (ST144A's own source comments carry the ASCII sample). Unsubstituted, the greenfield stored the raw
token text in the expected-order items while the LEGACY left them blank — and the golden had fossilized that
blank-placeholder run (`  052 052 052`), passing its NATIVE COLL.SEQUENCE checks blank-vs-blank. Added the
substitution to the SHARED `NistPreprocessor` (token-boundary anchored + MatchEvaluator, exactly the 063/065
pattern — IX106A embeds "…064…" inside a baselined data literal), **re-baselined `tests/nist/valid/ST144A.txt`
from the legacy run** (which now fills real key values — the ST137A/ST147A precedent, DEVLOG ~293), and
**re-ran the FULL legacy guard: 364 MATCH + 1204 unit + 536 integration, ALL GREEN** (mandatory for any
shared-frontend change). The greenfield byte-matches the new golden first try.

**Locked: ST144A.** **809 conformance + 15 unit green; 236 NIST programs locked. ST golden coverage is now
24/29 — the only ST programs not locked are the four Tier-C COMP-record SORTs (ST108A/127A/133A/134A, the
scheduled byte-island codec) and ST146A (legacy LOW-VALUE-tail golden hole, swept-only).**


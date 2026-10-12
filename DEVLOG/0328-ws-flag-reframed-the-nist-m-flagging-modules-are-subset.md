## Entry 328 — WS-FLAG reframed: the NIST `…M` flagging modules are subset-conformance tests; CobolSharp claims the HIGH subset

Starting the WS-FLAG harness, I first compiled the live `…M` modules under `--standard cobol2002` to see what
already flags. NC303M emitted **3 of its 4** expected flags (CBL3601 ALTER + CBL3605 bare GO TO ×2; only
DATE-COMPILED unflagged); SM301M and IF401M emitted **nothing** relevant. Reading `docs/FLAG_MANIFESTS.md` told
me why — and reframed the whole flagging axis.

The live `…M` modules are **two distinct classes**:

- **Class A — subset-level flags.** IF401M/402M/403M, IX301M/401M, RL301M/401M, SQ401M, SM301M/401M (and the
  ST/IC subset `…M`). Every flag is "NON-CONFORMING STANDARD — *feature X is above the minimum/Level-1 subset*":
  the high-subset intrinsics (ACOS…WHEN-COMPILED), INDEXED organization, ACCESS RANDOM/DYNAMIC, NOT INVALID KEY,
  SELECT OPTIONAL, RESERVE, ALTERNATE KEY, RECORD VARYING, LINAGE, COPY, … These are **standard** features that
  are only "non-conforming" *relative to a claimed lower subset*. CobolSharp implements the **full language
  (HIGH subset)**, so they are native and **emitting zero flags is the correct conformance behaviour.**
- **Class B — obsolete-element flags.** NC303M (DATE-COMPILED, ALTER, altered GO TO), SQ303M (MULTIPLE FILE TAPE,
  OPEN … REVERSED): genuinely obsolete '85 elements, flagged regardless of subset.

**Decision (parallels the DB/SG/CM call):** CobolSharp declares the **high/full COBOL-85 subset.** So the Class-A
`…M` modules are **N/A — a documented conformance-profile exclusion**, not work to implement; the alternative
(an archaic `--subset minimum|intermediate` mode that flags above-subset features to satisfy them) is surfaced to
the owner. Class B is real and small: implement obsolete-element **warnings** under `--standard cobol85` (ALTER
and bare GO TO already error under `cobol2002+`; '85 should *warn* OBSOLETE and still compile). This collapses
the apparent "~39 `…M`" flagging gap to a handful of obsolete-element targets + the removed-module flags handled
by WS-DIALECT. Plan §3 WS-FLAG updated; no code yet (analysis tick).


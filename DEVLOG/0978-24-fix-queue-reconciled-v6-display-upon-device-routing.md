## Entry 978 — 2026-07-22 10:40 PDT — §24 fix-queue: reconciled + V6 DISPLAY … UPON device routing LANDED

**Started phase-14 item ② — the §24 fix-queue** (`PHASE-13-plan-vs-spec-review.md` §24). First reconciled the 10-tier
queue against current code with an 8-agent triage workflow (`wf_3757f8d4-b04`): **5 LANDED** (V43, C2, C3, C4, V41 —
grammar batch / Wave-D), **1 DECISION already resolved** (D17 OO-surface scope → P14 Step-0-W, so tier-1 V16–V19/R18/R19
are P14-wave-routed, not this cycle), **33 OPEN + 6 PARTIAL** (the reconciled queue saved to
`scratchpad/p13-fixqueue-reconciled.json`). Then executed the top mechanical major:

**V6 — DISPLAY … UPON silently dropped (ISO §14.9.11.3 SR2 / §14.9.11.4 GR8) — LANDED.** The grammar parsed
`displayUpon` but the binder never read it: every DISPLAY went to the standard device regardless of the named mnemonic,
and an undeclared / input-only mnemonic compiled clean. Fix: `BindDisplay` binds `displayUpon` via `BindDisplayUpon`
(mirrors `BindAcceptFromMnemonic`) — SR2 rejects an undeclared mnemonic or a device not capable of receiving data
(`COBOLNET0817`); the output-capable implementor device-names are CONSOLE / SYSOUT (→ standard output) and SYSERR
(→ standard error), the §12.3.7.3-delegated choice now documented in `CONFORMANCE.md §7` A.1 items 2 (ACCEPT default
device) + 59 (DISPLAY standard display device). `BoundDisplay.ToStdErr` carries the routing (default false ⇒ no-UPON
byte-identical); `EmitDisplay` writes `System.Console.Error` for SYSERR else `System.Console`. Fixed the
`AcceptDisplayEmitter` §14.9.8→§14.9.11 citation nit.

Gate: `DisplayUponTests` 4/4 (SYSOUT/CONSOLE/default→stdout · SYSERR→stderr · undeclared→0817 · input-only→0817) ·
characterization 33/33 byte-identical · DISPLAY/ACCEPT conformance 99/99 · no golden uses `DISPLAY … UPON` (0 corpus
break). RESIDUAL (a separate finding, tracked): `MnemonicRegistry` still does no device-name validation at the
SPECIAL-NAMES declaration (§12.3.7 rule 8). NEXT §24 items: the EC-seam batch (V4/V3/F10), the ref-mod cluster
(C14/V48/V45), then the misc-semantics + doc-slice sweep.


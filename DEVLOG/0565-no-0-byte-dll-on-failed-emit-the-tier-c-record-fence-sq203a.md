## Entry 565 — 2026-06-10 20:50 PDT — No 0-byte dll on failed emit + the Tier-C record fence → SQ203A locked, the "hostpolicy flake" class retired (808 conformance)

**Two build-hygiene fixes from the briefs (st-chain §4.4 / rlix C2-B):** (1) `RoslynBackend` deletes the
partial/0-byte output a failed `Emit` leaves behind — running such a file dies with the misleading
"hostpolicy.dll required" error that the sweeps had classified as an environmental flake (it never was: every
instance was a deterministic backend failure). With the sweep's `-s` check (560) the phantom class is fully
retired. (2) `EmitImageInto` now fences a GROUP record area with non-character (COMP/binary) leaves LOUD
(§1.4) instead of emitting the string-into-struct assignment whose CS0029 killed the whole backend compile —
ST108A/ST127A/ST133A/ST134A now classify honestly as the Tier-C byte-island record codec
(COBOLNET_DESIGN §4.2, the scheduled work item), reachable-feature-by-feature instead of all-or-nothing.

**SQ203A locked:** the brief had grouped it with the CS0029 family, but its actual blocker was the group FILE
STATUS store that DEVLOG 559 fixed — behind its SQ202A chain it byte-matches the golden ("FILE PRESENT" on the
OPTIONAL consumer side). **808 conformance + 15 unit green; 235 NIST programs locked.**


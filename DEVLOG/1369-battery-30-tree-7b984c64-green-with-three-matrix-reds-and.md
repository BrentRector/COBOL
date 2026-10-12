## Entry 1369 — 2026-08-29 04:22 PDT — Battery #30 (tree 7b984c64): green with THREE matrix reds and FOUR differential flips, every one attributed to the PB130/PB132 Format-1 narrowing — the probe respelled, the baseline re-baselined, PB162 filed for the missing --permissive lane

The accumulated PB128–PB132 + PB161 batch's comprehensive battery. Greenfield Conformance 4982/4985 — the
three reds were ONE cause: the `call-by-value-2002` version-matrix probe spelled FORMAT-1 `CALL … USING BY
VALUE W`, which PB130/PB132 now correctly reject at every edition (§14.9.4.2 Format 1 prints no BY VALUE
arm); the probe was respelled to the conforming Format 2 (AS NESTED, contained callee with a BY VALUE
formal — the 85 leg still draws its COBOLNET0900 grammar gate) and the full VersionMatrix leg re-ran
GREEN. Unit 4589/4589 · characterization 33/33 · NIST 353 MATCH / 0 REGRESSION (audit CLEAN) · guard
verdict ALL GREEN.

The GnuCOBOL differential ran 1323 cases with FOUR per-case flips, all AGREE_ACCEPT →
WE_REJECT_THEY_ACCEPT and all one cause: run_extensions:25 (`USING X-1 BY CONTENT 1`), run_extensions:3435
/ 3460 (`USING X BY VALUE 2` on C$TOUPPER/C$TOLOWER), run_misc:1624 (`USING BY CONTENT 1` / `BY CONTENT
LENGTH OF MYTAB`) — each a GnuCOBOL EXTENSION spelling of a Format-1 CALL that strict ISO rejects. Each
source was inspected (no AS phrase in any of them), the rejection verified ISO-correct, and the committed
baseline REGENERATED deliberately — the diff is exactly those four rows. The real-world gap this exposes
— no --permissive lane accepts the vendor spellings with a warning — is kb/Work PB162 (open, MINOR:
nobody gets a wrong answer; strict stays exactly as landed).

Totals now 581/465/207/70; the four moved out of AGREE_ACCEPT and into the documented
WE_REJECT_THEY_ACCEPT DEFAULT_DIALECT band alongside the other strict-vs-extension rejections.


## Entry 1849 — 2026-10-03 12:34 PDT — cobc 3.2.0 is installed in WSL and confirms PB1945's probe

The owner built GnuCOBOL 3.2.0 from the repo-pinned tarball (`tests/external/gnucobol-3.2.tar.xz`, SHA-256
`3bb48af4…c2ee2`) into `/usr/local`, so `cobc` now resolves to `/usr/local/bin/cobc` (3.2.0), with the apt 3.1.2 still at
`/usr/bin/cobc` as a cross-check. The `>>CALL-CONVENTION` probe of DEVLOG 1848, re-run on 3.2.0, gives the identical
result: `COBOL`, `EXTERN`, `STDCALL` and `STATIC` are accepted; `C`, `PASCAL` and an arbitrary word are rejected. The three
names are therefore stable across both releases, and PB1945 now waits only on its owner decision (keep COBOL-only and
document it, or implement a native-call mapping). The corpus the differential pins and the binary now agree on a version.

**Files:** `kb/Work/PB1945.md`. No code changed, no diagnostic code used.


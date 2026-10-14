## Entry 2022 — 2026-10-14 02:46 PDT — PB3146: LICENSE names WiseOwl COBOL as the licensed work, and every package carries that LICENSE instead of declaring MIT

At the owner's request (2026-10-14), `LICENSE` now names the Licensed Work `WiseOwl COBOL`. It had named
`CobolSharp`, the legacy engine deleted from main in PHASE 15 Cut 2 (kb/Work PB2110).

The sweep for other places that state the license found one more. `Directory.Build.props` declared
`PackageLicenseExpression` MIT for every project, so any package `dotnet pack` built would have offered the product
under MIT, against the repository's Business Source License. No package was ever published (nuget.org has no
`wiseowl.cobol`). Every packable project now packs `LICENSE` at the package root and declares it with
`PackageLicenseFile`. It has to be a file: nuget.org accepts a license expression only for an OSI- or FSF-approved
license, and the SPDX id `BUSL-1.1` would drop the license's parameters. Verified with `dotnet pack` of
`src/Cobol.Net.Runtime`: the package holds `LICENSE` and its nuspec reads `<license type="file">LICENSE</license>`.

The owner has also asked whether the Business Source License is the right license before the compiler is complete.
That is still open; this change fixes only the name and the metadata.

The package design (DESIGN-USER-DOCUMENTATION.md section 6) and the distribution note PB1613 now state the settled choice; they had offered the BUSL-1.1 expression as an option.


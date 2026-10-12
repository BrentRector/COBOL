## Entry 1699 — 2026-09-25 00:33 PDT — Register: PB1568–PB1572 from golden lane #2 and wave 59; MANDATORY-PRACTICES I7

Register-only checkpoint between landings (no compiler change).

- **New notes:** PB1568 (analysis — an erratum the PRINTED standard carries: UNSTRING GR11 g) cites "12b through 12f",
  meaning 11b–11f; rendered page 802), PB1569 (wrong answer — a TYPE subject's bit group is not given level-1
  alignment, BYTE-LENGTH 01 for 02; fixed on the w59m branch, landing in train 61), PB1570 (the sort-file store is a
  process-wide static — cross-run-unit sharing), PB1571 (analysis — survey GnuCOBOL for the PB1036 D-SMA precedence
  choice, which was made without cobc on the host), PB1572 (wrong answer — a store into a bit-group member inside a
  Tier-B REDEFINES is lost).
- **Widened:** PB1177 (a declarative RESUME inside a SORT procedure continues where §14.9.40.4 GR17 ends the SORT —
  now reachable through PB1036's raises), PB1453 (its SET half fixed by PB1549 on the w59i branch; the relation-operand
  half remains), PB1555 (now blocks the misc-p37 indexed key-path golden), PB1559 (a second rule violation filed under
  the 0899 band, §13.18.57.3 SR11), PB996 (COBOLNET1557 cites §13.18.49.3 SR3).
- **Also filed (wave-59 leads):** PB1573 (build-local.ps1 drops failing-assertion text, so reds arrive unattributed),
  PB1574 (the code-citation audit is not in CI — how 14 wrong clause numbers reached main), PB1575 (an unknown
  `>>` directive gets an anonymous COBOL0001), PB1576 (no drift test ties ConformanceDescriptor to
  DescriptionMismatch), PB1577 (a symbolic character is refused as a function argument), PB1578 (analysis —
  SPECIAL-NAMES clause order), PB1579 (`int.TryParse` on COBOL integer literals at seven more binder sites).
- **MANDATORY-PRACTICES I7:** every golden and negative an implementer ADDS must run by name at its gate and the report
  quotes the pass line. Train 60's lander found a w59d negative with no `.err` — its implementer's gate had run only
  `Manifest_CoversEveryProgram`, which proves registration, not execution. The dispatch template carries I7 and
  `check_practices.py` fails a rendered spec that drops it.


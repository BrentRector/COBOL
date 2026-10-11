## Entry 287 — The "vacuous trio" are BUILDERS, not bugs; ST114M chain baselined; variable-length SORT bug found (NIST 286→287)

Investigated the still-000-of-000 trio ST109A/ST112M/ST122A. Key finding: **they are not vacuous failures —
they are pure file BUILDERS**, and 000-of-000 is their canonical NIST output. Each prints one comment and
exits: ST109A "HAS CREATED A FILE OF 40 VARIABLE-LENGTH-RECORDS … SORTED IN ST110 AND CHECKED IN ST111";
ST112M "HAS CREATED A 3-REEL FILE … PASSED TO ST113 FOR SORTING. **THIS COMMENT IS THE ONLY OUTPUT FOR
ST112**"; ST122A similarly feeds ST123A/ST124A. They run no PERFORM PASS/FAIL of their own (the framework
PASS/FAIL paragraphs are just definitions). So the real targets are their chain VERIFIERS, not the builders.

The chains are build → sort → verify triplets:
- **ST112M → ST113M → ST114M (3-reel file): ST114M now passes 10/10** → baselined. ST112M (builder, emits
  only the 000 comment) and ST113M (sorter, NO_OUTPUT) run as non-baselined producers, consecutive before
  ST114M. (Consistent with the ST102A precedent: a no-verify builder is a producer, not a 000-baseline.)
- **ST109A → ST110A → ST111A** and **ST122A → ST123A → ST124A: the verifiers FAIL 7 each with binary (NUL)
  output** — a genuine **variable-length-record SORT bug**. These are file `SORT … USING … GIVING` over
  RECORD CONTAINS 50 TO 100 files (three 01 sizes: 50/75/100). `EmitSortUsingFile` reads each input record
  into `inputFile.Record` (the FIRST 01 = the smallest, 50 bytes) and `IrSortRelease`s the SD record at its
  fixed length, so long records are truncated and the per-record actual length is lost; the round-tripped
  records come back shifted (record 1) or all-NUL (records 2+). The fix is the SORT analogue of the SQ/RL
  variable-length work (DEVLOG 245/256/257): read into the largest record area, carry each record's actual
  length through RELEASE → sort → RETURN, and write variable-length on GIVING. Logged as the next ST lead;
  not attempted this entry (substantial feature).

Guard ALL GREEN: 1000 unit / 347 integration / **287 NIST** (… + 40 IX + 17 ST), 0 regressions. ST: 17
baselined; remaining = the variable-length-SORT chains (ST111A/124A + builders ST109A/122A), the other chain
consumers ST107A/117A/121A, the FAIL* family ST126A/139A/140A/144A/146A/147A, NO_OUTPUT, ST115A timeout,
ST301M flagging.


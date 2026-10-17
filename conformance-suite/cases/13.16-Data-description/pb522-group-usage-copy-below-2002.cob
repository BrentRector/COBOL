*>--- conformance-case
*> id: pb522-group-usage-copy-below-2002
*> rules: [GR-13.16.4-1, GR-13.16.4-2]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB522 — the NEGATIVE twin of tests/conformance/2002/pb522_group_usage_travels_with_the_description.
      *> The clause whose travel that golden pins is a COBOL-2002 introduction, so the whole construct is refused
      *> below it: GROUP-USAGE (ISO §13.18.29) with the TYPEDEF/TYPE family (§13.18.58 / §13.18.57) and SAME AS
      *> (§13.18.49) — none of which exists in COBOL-85 — each gated by COBOLNET0900 at --std 85.
      *> The description copy is what the positive measures; this fixture keeps the copy from silently becoming
      *> reachable one edition too early.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB522GUGATE.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 NDEF IS TYPEDEF GROUP-USAGE NATIONAL.
          02 NG1.
             03 NN PIC N(4).
       01 VIA-TYPE-N TYPE NDEF.
       01 SRC-N GROUP-USAGE NATIONAL.
          02 SNG.
             03 SNN PIC N(4).
       01 SAME-N SAME AS SRC-N.
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY FUNCTION LENGTH(VIA-TYPE-N)
           DISPLAY FUNCTION LENGTH(SAME-N)
           STOP RUN.

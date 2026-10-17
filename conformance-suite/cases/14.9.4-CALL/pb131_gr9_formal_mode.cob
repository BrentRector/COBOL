*>--- conformance-case
*> id: pb131_gr9_formal_mode
*> rules: [SR-14.9.4.3-15, SR-14.9.4.3-19, SR-14.9.4.3-20,
*>    GR-14.9.4.4-6, GR-14.9.4.4-9]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB131 — Format 2's keyword-less argument takes its mode from the CORRESPONDING FORMAL
      *> (ISO 14.9.4.4 GR9 a)1 — GR5's transitivity is printed under FORMAT 1 and names only BY REFERENCE
      *> and BY CONTENT). Here `USING BY VALUE A B` against `USING BY VALUE LA BY REFERENCE LB`: B's formal
      *> is BY REFERENCE, so the callee's MOVE into LB is VISIBLE to the caller — B=0005. The old single
      *> transitive mode passed B detached BY VALUE and the writeback was lost (B stayed 0, silently, on
      *> conforming source). The formal lookup rides the bind-time AS NESTED callee table, which also
      *> enforces SR15's scope (negative pb131-as-nested-scope).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB131GM.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A PIC 9(4) VALUE 5.
       01 B PIC 9(4) VALUE 0.
       PROCEDURE DIVISION.
       MAIN.
           CALL "S1" AS NESTED USING BY VALUE A B
           DISPLAY "B=" B
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. S1.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LA PIC 9(4).
       01 LB PIC 9(4).
       PROCEDURE DIVISION USING BY VALUE LA BY REFERENCE LB.
       M1.
           MOVE LA TO LB
           GOBACK.
       END PROGRAM S1.
       END PROGRAM PB131GM.

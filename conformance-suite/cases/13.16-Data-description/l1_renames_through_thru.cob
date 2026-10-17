      *>--- conformance-case
      *> id: l1_renames_through_thru
      *> rules: [SR-13.16.3-22, SR-13.18.45.3-1, SR-13.18.45.3-9,
      *>    FMT-13.18.45.2]
      *> accept: [2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §13.16.3 SR22 data description entry, FORMAT 2 (renames) —
      *> "The words THROUGH and THRU are equivalent."
      *>   python scripts/spec/cite.py --check 13.16.3 "The words
      *>   THROUGH and THRU are equivalent."
      *>   -> OK  §13.16.3 22)  (Syntax rules)
      *>
      *> SCOPE — WHICH FORMAT THE RULE GOVERNS. SR22 stands under the
      *> "FORMAT 2" heading of §13.16.3; the "FORMATS 3 AND 4" heading
      *> comes AFTER it and opens SR23. So the rule is about the
      *> §13.16.2 Format 2 (renames) entry
      *>   66 data-name-1 RENAMES data-name-4 [{THROUGH|THRU}
      *>   data-name-5] .
      *> — not the level-88 VALUE ... THRU of formats 3 and 4.
      *>
      *> DERIVATION. Equivalence means the two spellings select the
      *> same alternative of that brace, so both 66 entries are the
      *> same clause and §13.18.45.4 GR2 gives each the SAME area:
      *>   python scripts/spec/cite.py --check 13.18.45.4 "When the
      *>   THROUGH phrase is specified, data-name-1 defines an
      *>   alphanumeric group item that includes all elementary items
      *>   starting with data-name-2"
      *>   -> OK  §13.18.45.4 2)  (General rules)
      *> "...an alphanumeric group item that includes all elementary
      *> items starting with data-name-2 ... and concluding with
      *> data-name-3". WS-A holds AB, WS-B holds CD, WS-C holds EF, so
      *> three consequences follow and each is displayed:
      *>   1. R1=[ABCD] and R2=[ABCD] — both areas start at WS-A and
      *>      conclude with WS-B, so both read the same four bytes;
      *>   2. R1=[WXYZ] after MOVE "WXYZ" TO WS-R2 — one storage area,
      *>      so a write through the THROUGH-spelled name is read back
      *>      through the THRU-spelled one;
      *>   3. C=[EF] — the area CONCLUDES with WS-B for both spellings;
      *>      neither reaches into WS-C.
      *> The legs are distinguishable: a compiler that accepted only
      *> THRU would reject the WS-R2 entry outright; one that treated
      *> THROUGH as a different (say, single-item) rename would print
      *> R2=[AB] on line 2 or leave R1 unchanged on line 3.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1RNTHR.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-G.
          05 WS-A PIC X(2).
          05 WS-B PIC X(2).
          05 WS-C PIC X(2).
       66 WS-R1 RENAMES WS-A THRU WS-B.
       66 WS-R2 RENAMES WS-A THROUGH WS-B.
       PROCEDURE DIVISION.
       MAIN-P.
           MOVE "AB" TO WS-A.
           MOVE "CD" TO WS-B.
           MOVE "EF" TO WS-C.
           DISPLAY "R1=[" WS-R1 "]".
           DISPLAY "R2=[" WS-R2 "]".
           MOVE "WXYZ" TO WS-R2.
           DISPLAY "R1=[" WS-R1 "]".
           DISPLAY "C=[" WS-C "]".
           STOP RUN.

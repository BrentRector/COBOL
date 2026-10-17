      *>--- conformance-case
      *> id: l1c30_thru_through_equivalent
      *> rules: [SR-12.3.7.3-12]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §12.3.7.3 SR12 — THROUGH and THRU are equivalent
      *>   "12) The words THROUGH and THRU are equivalent."
      *>   cite.py --check 12.3.7.3 "The words THROUGH and THRU are
      *>     equivalent." -> OK §12.3.7.3 12)
      *>   cite.py --check 12.3.7.4 "may specify characters of the
      *>     native character set in either ascending or descending
      *>     sequence" -> OK §12.3.7.4 7) 2.  (cite.py mislabels list
      *>     items, PB1554: the text is GR7 k) 5.)
      *>   cite.py --check 12.3.7.4 "Any characters of the native
      *>     collating sequence that are not specified in the literal
      *>     phrase shall assume a position" -> OK §12.3.7.4 7) 2.
      *>     (PB1554 again: the text is GR7 k) 3.)
      *>   cite.py --check 12.3.7.4 "The characters specified by the
      *>     values of the literals in this clause define the
      *>     exclusive set of characters of which class-name-1
      *>     consists." -> OK §12.3.7.4 12)
      *> Two SIBLING programs (not contained, so GR1 does not pass one
      *> unit's SPECIAL-NAMES to the other) define the SAME alphabet
      *> and class, L1C30C spelling the range THROUGH and L1C30D
      *> spelling it THRU, in both SR12 sites: the ALPHABET literal
      *> phrase and the CLASS clause. Equivalence = identical results.
      *> DERIVATION (same for both spellings; tag THROUGH / THRU):
      *>   ALPHABET AL IS "D" THROUGH "B": a DESCENDING range (GR7 k)
      *>   5.) - positions D=1 C=2 B=3; "A" is unspecified, so it
      *>   follows all three (GR7 k) 3.). Program collating sequence:
      *>     "D" < "B"  -> D-LT-B=Y  (native order would say N)
      *>     "C" < "B"  -> C-LT-B=Y  (native: N)
      *>     "B" < "A"  -> B-LT-A=Y  (native: N)
      *>   CLASS CL IS "3" THROUGH "0": GR12 - the contiguous native
      *>   characters "3" down to "0", i.e. {0,1,2,3}:
      *>     "0123" IS CL -> 0123-IN=Y ; "0124" IS CL -> 0124-IN=N
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C30C.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       OBJECT-COMPUTER. X PROGRAM COLLATING SEQUENCE IS AL.
       SPECIAL-NAMES.
           ALPHABET AL IS "D" THROUGH "B"
           CLASS CL IS "3" THROUGH "0".
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-A PIC X VALUE "A".
       01 W-B PIC X VALUE "B".
       01 W-C PIC X VALUE "C".
       01 W-D PIC X VALUE "D".
       01 W-X PIC X(4) VALUE "0123".
       01 W-Y PIC X(4) VALUE "0124".
       PROCEDURE DIVISION.
       MAIN.
           IF W-D < W-B
               DISPLAY "THROUGH D-LT-B=Y"
           ELSE
               DISPLAY "THROUGH D-LT-B=N".
           IF W-C < W-B
               DISPLAY "THROUGH C-LT-B=Y"
           ELSE
               DISPLAY "THROUGH C-LT-B=N".
           IF W-B < W-A
               DISPLAY "THROUGH B-LT-A=Y"
           ELSE
               DISPLAY "THROUGH B-LT-A=N".
           IF W-X IS CL
               DISPLAY "THROUGH 0123-IN=Y"
           ELSE
               DISPLAY "THROUGH 0123-IN=N".
           IF W-Y IS CL
               DISPLAY "THROUGH 0124-IN=Y"
           ELSE
               DISPLAY "THROUGH 0124-IN=N".
           CALL "L1C30D".
           STOP RUN.
       END PROGRAM L1C30C.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C30D.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       OBJECT-COMPUTER. X PROGRAM COLLATING SEQUENCE IS AL.
       SPECIAL-NAMES.
           ALPHABET AL IS "D" THRU "B"
           CLASS CL IS "3" THRU "0".
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-A PIC X VALUE "A".
       01 W-B PIC X VALUE "B".
       01 W-C PIC X VALUE "C".
       01 W-D PIC X VALUE "D".
       01 W-X PIC X(4) VALUE "0123".
       01 W-Y PIC X(4) VALUE "0124".
       PROCEDURE DIVISION.
       MAIN.
           IF W-D < W-B
               DISPLAY "THRU D-LT-B=Y"
           ELSE
               DISPLAY "THRU D-LT-B=N".
           IF W-C < W-B
               DISPLAY "THRU C-LT-B=Y"
           ELSE
               DISPLAY "THRU C-LT-B=N".
           IF W-B < W-A
               DISPLAY "THRU B-LT-A=Y"
           ELSE
               DISPLAY "THRU B-LT-A=N".
           IF W-X IS CL
               DISPLAY "THRU 0123-IN=Y"
           ELSE
               DISPLAY "THRU 0123-IN=N".
           IF W-Y IS CL
               DISPLAY "THRU 0124-IN=Y"
           ELSE
               DISPLAY "THRU 0124-IN=N".
           EXIT PROGRAM.
       END PROGRAM L1C30D.

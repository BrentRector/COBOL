*>--- conformance-case
*> id: intrinsics_smallest_algebraic
*> rules: [RV-15.83.4-2]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §15.83 SMALLEST-ALGEBRAIC — the smallest positive value that may increment argument-1 (P11 Step 7
      *> golden; the function itself was already a compile-time PICTURE fold via BindAlgebraicFold). Values from
      *> the §15.83.4 RVR2 NOTE table (docs/rearchitecture/PHASE-11-scout-notes.md spec:concat-smallest):
      *> S999 -> +1 (scale 0 => 10^0); 99V9(3) -> 0.001 (scale 3 => 10^-3); S9PP -> +100 (P-scaling, scale -2
      *> => 10^2); BINARY-CHAR UNSIGNED -> +1 (integer). NEW-IN-2023 (Annex E.3 item 29 "has been added").
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P11SMALLALG.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X-INT   PIC S999.
       01 X-FRAC  PIC 99V9(3).
       01 X-PSCL  PIC S9PP.
       01 X-BC    USAGE BINARY-CHAR UNSIGNED.
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "SA-INT=" FUNCTION SMALLEST-ALGEBRAIC(X-INT)
           DISPLAY "SA-FRAC=" FUNCTION SMALLEST-ALGEBRAIC(X-FRAC)
           DISPLAY "SA-PSCL=" FUNCTION SMALLEST-ALGEBRAIC(X-PSCL)
           DISPLAY "SA-BC=" FUNCTION SMALLEST-ALGEBRAIC(X-BC)
           STOP RUN.

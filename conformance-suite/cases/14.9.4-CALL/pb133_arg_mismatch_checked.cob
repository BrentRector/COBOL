*>--- conformance-case
*> id: pb133_arg_mismatch_checked
*> rules: [GR-14.9.4.4-3]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB133 wave C2b - ISO 14.8.2.1 via 14.9.4.4 GR3d: a dynamic Format-1 CALL supplying one
      *> argument against two REQUIRED formals raises EC-PROGRAM-ARG-MISMATCH at activation - but ONLY
      *> "if checking for it is enabled in both the activated program and activating runtime element"
      *> (the >>TURN covers both units' PD entries and the site). GR3h routes it to the ON EXCEPTION
      *> phrase. Derived: MISMATCH-CAUGHT.
      >>TURN EC-PROGRAM-ARG-MISMATCH CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. AM1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-A PIC 9(4) VALUE 5.
       PROCEDURE DIVISION.
       MAIN.
           CALL "AM1S" USING W-A ON EXCEPTION DISPLAY "MISMATCH-CAUGHT"
           END-CALL
           STOP RUN.
       END PROGRAM AM1.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. AM1S.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-A PIC 9(4).
       01 L-B PIC 9(4).
       PROCEDURE DIVISION USING L-A L-B.
       P.
           GOBACK.
       END PROGRAM AM1S.

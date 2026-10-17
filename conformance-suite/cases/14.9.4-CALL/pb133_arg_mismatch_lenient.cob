*>--- conformance-case
*> id: pb133_arg_mismatch_lenient
*> rules: [GR-14.9.4.4-3]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB133 wave C2b - the GR3d gate's LENIENT half: with checking NOT enabled (14.6.13.1.1),
      *> the same one-argument call to a two-formal program PROCEEDS - the missing trailing argument
      *> behaves as OMITTED (the documented posture; the design doc's "a missing parameter behaves as
      *> omitted"), so the callee runs and its reference to L-B reads the benign lenient zero.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. AM1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-A PIC 9(4) VALUE 5.
       PROCEDURE DIVISION.
       MAIN.
           CALL "AM1S" USING W-A
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
           DISPLAY "S-RAN B=" L-B
           GOBACK.
       END PROGRAM AM1S.

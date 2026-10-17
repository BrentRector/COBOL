      *>--- conformance-case
      *> id: l1c21-common-outermost-program
      *> rules: [SR-11.10.3-4]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §11.10.3 SR4 — COMMON on an OUTERMOST program is illegal
      *> "The COMMON clause may be specified only if the program is
      *> contained within another program."
      *>   cite.py --check 11.10.3 -> OK  §11.10.3 4)  (Syntax rules)
      *> L1C21J is the outermost program of its compilation group (it
      *> is contained in nothing), yet its PROGRAM-ID says IS COMMON.
      *> Everything else is valid in every edition, so the only reason
      *> to reject is SR4: COBOLNET0887 ("COMMON may be specified only
      *> in a CONTAINED program"). Its contained program L1C21K, which
      *> MAY be COMMON, is present to show that the diagnostic keys on
      *> containment and not on the clause itself.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C21J IS COMMON PROGRAM.
       PROCEDURE DIVISION.
       MAIN-PARA.
           CALL "L1C21K"
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C21K IS COMMON PROGRAM.
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY "K"
           EXIT PROGRAM.
       END PROGRAM L1C21K.
       END PROGRAM L1C21J.

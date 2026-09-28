      *> ISO §12.3.4 GR1 + §12.3.8.4 GR10/GR12 - a CLASS definition's
      *> REPOSITORY reaches its methods (kb/Work PB1100)
      *> RULE §12.3.4 GR1: "The entries explicitly or implicitly
      *>   specified in the configuration section of a source unit that
      *>   contains other source units apply to each directly or
      *>   indirectly contained source unit."
      *>   cite.py --check 12.3.4 "apply to each directly or indirectly
      *>   contained source unit" -> OK §12.3.4 1)
      *> RULE §12.3.8.4 GR12: "Within the scope of the containing
      *>   environment division, a reference to function-prototype-
      *>   name-1 is a reference to a user-defined function".
      *>   cite.py --check 12.3.8.4 "a reference to function-prototype-
      *>   name-1 is a reference to a user-defined function" -> OK
      *>   §12.3.8.4 12)
      *> RULE §12.3.8.4 GR10: "Program-prototype-name-1 is the name of
      *>   a program prototype that may be used throughout the scope of
      *>   the containing environment division."
      *>   cite.py --check 12.3.8.4 "Program-prototype-name-1 is the name
      *>   of a program prototype that may be used throughout the scope
      *>   of the containing environment division" -> OK §12.3.8.4 10)
      *> §12.3.3 SR3 bars the REPOSITORY paragraph only in a FACTORY or
      *>   OBJECT paragraph, and SR2 bars a configuration section in a
      *>   method, so the CLASS-ID's own environment division is the
      *>   one that carries it (cite.py --check 12.3.3 "The SOURCE-
      *>   COMPUTER, OBJECT-COMPUTER, and REPOSITORY paragraphs shall not
      *>   be specified in a factory definition or an instance
      *>   definition" -> OK §12.3.3 3)).
      *> SET-UP: PB1100D doubles its argument. Class PB1100C's
      *>   REPOSITORY names FUNCTION PB1100D and PROGRAM PB1100S (adds
      *>   100 to its argument). The FACTORY method FTWICE and the
      *>   OBJECT method OTWICE reference PB1100D; the FACTORY method
      *>   FCALL calls PB1100S by its program-prototype-name.
      *> EXPECTED OUTPUT, DERIVED:
      *>   F=0043  FTWICE: PB1100D(21) + 1 = 42 + 1.
      *>   O=0010  OTWICE on a new object: PB1100D(5) = 10.
      *>   C=0121  FCALL: CALL PB1100S USING X adds 100 to X (21, by
      *>           reference) and returns X: 121.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1100P.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB1100C.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-R PIC 9(4) VALUE 0.
       01 W-N PIC 9(4) VALUE 21.
       01 W-M PIC 9(4) VALUE 5.
       01 W-O USAGE OBJECT REFERENCE PB1100C.
       PROCEDURE DIVISION.
       P-MAIN.
           INVOKE PB1100C "FTWICE" USING W-N RETURNING W-R
           DISPLAY "F=" W-R
           INVOKE PB1100C "NEW" RETURNING W-O
           INVOKE W-O "OTWICE" USING W-M RETURNING W-R
           DISPLAY "O=" W-R
           INVOKE PB1100C "FCALL" USING W-N RETURNING W-R
           DISPLAY "C=" W-R
           STOP RUN.
       END PROGRAM PB1100P.
       IDENTIFICATION DIVISION.
       FUNCTION-ID. PB1100D.
       DATA DIVISION.
       LINKAGE SECTION.
       01 N PIC 9(4).
       01 R PIC 9(4).
       PROCEDURE DIVISION USING N RETURNING R.
       P-MAIN.
           COMPUTE R = N * 2
           GOBACK.
       END FUNCTION PB1100D.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1100S.
       DATA DIVISION.
       LINKAGE SECTION.
       01 N PIC 9(4).
       PROCEDURE DIVISION USING N.
       P-MAIN.
           ADD 100 TO N
           GOBACK.
       END PROGRAM PB1100S.
       IDENTIFICATION DIVISION.
       CLASS-ID. PB1100C INHERITS BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE
           FUNCTION PB1100D
           PROGRAM PB1100S.
       IDENTIFICATION DIVISION.
       FACTORY.
       PROCEDURE DIVISION.
       METHOD-ID. FTWICE.
       DATA DIVISION.
       LINKAGE SECTION.
       01 X PIC 9(4).
       01 R PIC 9(4).
       PROCEDURE DIVISION USING X RETURNING R.
       P-MAIN.
           COMPUTE R = FUNCTION PB1100D(X) + 1.
       END METHOD FTWICE.
       METHOD-ID. FCALL.
       DATA DIVISION.
       LINKAGE SECTION.
       01 X PIC 9(4).
       01 R PIC 9(4).
       PROCEDURE DIVISION USING X RETURNING R.
       P-MAIN.
           CALL PB1100S USING X
           MOVE X TO R.
       END METHOD FCALL.
       END FACTORY.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. OTWICE.
       DATA DIVISION.
       LINKAGE SECTION.
       01 X PIC 9(4).
       01 R PIC 9(4).
       PROCEDURE DIVISION USING X RETURNING R.
       P-MAIN.
           MOVE FUNCTION PB1100D(X) TO R.
       END METHOD OTWICE.
       END OBJECT.
       END CLASS PB1100C.

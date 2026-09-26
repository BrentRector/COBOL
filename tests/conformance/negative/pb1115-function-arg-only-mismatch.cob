      *> reject-at: 2002 2014 2023
      *> kb/Work PB1115 - ISO 14.8.2.3.2 rule 3: "If either the argument or
      *> the formal parameter is described with an object-class-name, the
      *> corresponding formal parameter or argument shall be described
      *> with the same object-class-name, and the FACTORY and ONLY phrases
      *> shall be the same." 8.4.3.2.3 SR13 imports it into a function
      *> activation. W-O is OBJECT REFERENCE N1115OC ONLY and the formal
      *> is OBJECT REFERENCE N1115OC -> COBOLNET2470. (Before the fix this
      *> compiled and ran silently: both sides share one .NET carrier.)
      *> cite.py --check 14.8.2.3.2 "and the FACTORY and ONLY phrases
      *>   shall be the same" -> OK  14.8.2.3.2 3)
       IDENTIFICATION DIVISION.
       FUNCTION-ID. N1115OF.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS N1115OC.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-O USAGE OBJECT REFERENCE N1115OC.
       01 L-R PIC X(4).
       PROCEDURE DIVISION USING L-O RETURNING L-R.
           IF L-O = NULL MOVE "NULL" TO L-R ELSE MOVE "OBJ " TO L-R
           END-IF
           GOBACK.
       END FUNCTION N1115OF.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. N1115OM.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           FUNCTION N1115OF
           CLASS N1115OC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-O USAGE OBJECT REFERENCE N1115OC ONLY.
       01 WR PIC X(4).
       PROCEDURE DIVISION.
           MOVE FUNCTION N1115OF(W-O) TO WR
           DISPLAY "R=" WR
           STOP RUN.
       END PROGRAM N1115OM.

       IDENTIFICATION DIVISION.
       CLASS-ID. N1115OC.
       IDENTIFICATION DIVISION.
       FACTORY.
       END FACTORY.
       IDENTIFICATION DIVISION.
       OBJECT.
       END OBJECT.
       END CLASS N1115OC.

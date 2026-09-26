      *> reject-at: 2002 2014 2023
      *> kb/Work PB1115 - ISO 14.8.2.3.2 rule 1: "If either the argument or
      *> the formal parameter is a universal object reference, the
      *> corresponding formal parameter or argument shall be a universal
      *> object reference." 8.4.3.2.3 SR13 imports it into a function
      *> activation. The formal is universal and W-O is described with a
      *> class-name -> COBOLNET2470. (Before the fix it compiled and
      *> failed only at run time through the .NET carrier.)
      *> cite.py --check 14.8.2.3.2 "If either the argument or the formal
      *>   parameter is a universal object reference" -> OK  14.8.2.3.2 1)
       IDENTIFICATION DIVISION.
       FUNCTION-ID. N1115UF.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS N1115UC.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-O USAGE OBJECT REFERENCE.
       01 L-R PIC X(4).
       PROCEDURE DIVISION USING L-O RETURNING L-R.
           IF L-O = NULL MOVE "NULL" TO L-R ELSE MOVE "OBJ " TO L-R
           END-IF
           GOBACK.
       END FUNCTION N1115UF.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. N1115UM.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           FUNCTION N1115UF
           CLASS N1115UC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-O USAGE OBJECT REFERENCE N1115UC.
       01 WR PIC X(4).
       PROCEDURE DIVISION.
           MOVE FUNCTION N1115UF(W-O) TO WR
           DISPLAY "R=" WR
           STOP RUN.
       END PROGRAM N1115UM.

       IDENTIFICATION DIVISION.
       CLASS-ID. N1115UC.
       IDENTIFICATION DIVISION.
       FACTORY.
       END FACTORY.
       IDENTIFICATION DIVISION.
       OBJECT.
       END OBJECT.
       END CLASS N1115UC.

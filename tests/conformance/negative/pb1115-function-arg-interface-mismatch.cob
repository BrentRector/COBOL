      *> reject-at: 2002 2014 2023
      *> kb/Work PB1115 - ISO 14.8.2.3.2 rule 2: "If either the argument or
      *> the formal parameter is described with an interface-name, the
      *> corresponding formal parameter or argument shall be described
      *> with the same interface-name." 8.4.3.2.3 SR13 imports it into a
      *> function activation. The formal names interface N1115II and
      *> W-O names class N1115IC (which implements it) -> COBOLNET2470.
      *> cite.py --check 14.8.2.3.2 "described with an interface-name, the
      *>   corresponding formal parameter or argument shall be described
      *>   with the same interface-name" -> OK  14.8.2.3.2 2)
       IDENTIFICATION DIVISION.
       FUNCTION-ID. N1115IF.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           INTERFACE N1115II.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-O USAGE OBJECT REFERENCE N1115II.
       01 L-R PIC X(4).
       PROCEDURE DIVISION USING L-O RETURNING L-R.
           IF L-O = NULL MOVE "NULL" TO L-R ELSE MOVE "OBJ " TO L-R
           END-IF
           GOBACK.
       END FUNCTION N1115IF.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. N1115IM.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           FUNCTION N1115IF
           CLASS N1115IC
           INTERFACE N1115II.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-O USAGE OBJECT REFERENCE N1115IC.
       01 WR PIC X(4).
       PROCEDURE DIVISION.
           MOVE FUNCTION N1115IF(W-O) TO WR
           DISPLAY "R=" WR
           STOP RUN.
       END PROGRAM N1115IM.

       IDENTIFICATION DIVISION.
       INTERFACE-ID. N1115II.
       PROCEDURE DIVISION.
       METHOD-ID. PING.
       PROCEDURE DIVISION.
       END METHOD PING.
       END INTERFACE N1115II.

       IDENTIFICATION DIVISION.
       CLASS-ID. N1115IC.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           INTERFACE N1115II.
       IDENTIFICATION DIVISION.
       FACTORY.
       END FACTORY.
       IDENTIFICATION DIVISION.
       OBJECT. IMPLEMENTS N1115II.
       PROCEDURE DIVISION.
       METHOD-ID. PING.
       PROCEDURE DIVISION.
       END METHOD PING.
       END OBJECT.
       END CLASS N1115IC.

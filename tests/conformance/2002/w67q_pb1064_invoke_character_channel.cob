      *> kb/Work PB1064 - the INVOKE lane of the PB992 character channel.
      *> ISO §14.2.3 GR8: "If the argument is passed by reference, the
      *> activated runtime element operates as if the formal parameter
      *> occupies the same storage area as the argument." A method is an
      *> activated runtime element (§14.9.23.4 GR7 e) -> §14.2.3), so the
      *> CHARACTERS a method leaves in a BY REFERENCE formal are the
      *> argument's characters. §14.9.25.4 GR4: a group moved to an
      *> elementary item "is treated exactly as if it were an alphanumeric
      *> to alphanumeric elementary move, except that there is no
      *> conversion" - so a group of spaces moved to a PIC 9(3) formal
      *> leaves three spaces, and the caller's N shows them: [   ], never
      *> a converted [000]. The same holds for a signed item (12AB), for
      *> characters that travel INTO the method (ABC, displayed there and
      *> back), for the RETURNING content (§14.9.23.4 GR8 places the
      *> result into identifier-4, and §14.6.5 makes the result "the
      *> content of the data item" the RETURNING phrase names: XYZ), for a
      *> universal receiver (§14.9.23.3 SR6, BY REFERENCE implied) and for
      *> a packed-decimal formal, whose storage takes the group's bytes
      *> X"123C" - the packed representation of +123.
       IDENTIFICATION DIVISION.
       CLASS-ID. W67QCC INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. M.
       DATA DIVISION.
       LOCAL-STORAGE SECTION.
       01 G.
          05 GX PIC X(3) VALUE SPACES.
       LINKAGE SECTION.
       01 L PIC 9(3).
       PROCEDURE DIVISION USING BY REFERENCE L.
           MOVE G TO L
           GOBACK.
       END METHOD M.
       METHOD-ID. S.
       DATA DIVISION.
       LOCAL-STORAGE SECTION.
       01 G.
          05 GX PIC X(4) VALUE "12AB".
       LINKAGE SECTION.
       01 L PIC S9(4).
       PROCEDURE DIVISION USING BY REFERENCE L.
           MOVE G TO L
           GOBACK.
       END METHOD S.
       METHOD-ID. P.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L PIC 9(3).
       PROCEDURE DIVISION USING BY REFERENCE L.
           DISPLAY "IN-P [" L "]"
           GOBACK.
       END METHOD P.
       METHOD-ID. R.
       DATA DIVISION.
       LOCAL-STORAGE SECTION.
       01 G.
          05 GX PIC X(3) VALUE "XYZ".
       LINKAGE SECTION.
       01 L PIC 9(3).
       PROCEDURE DIVISION RETURNING L.
           MOVE G TO L
           GOBACK.
       END METHOD R.
       METHOD-ID. K.
       DATA DIVISION.
       LOCAL-STORAGE SECTION.
       01 G.
          05 GX PIC X(2) VALUE X"123C".
       LINKAGE SECTION.
       01 L PIC S9(3) COMP-3.
       PROCEDURE DIVISION USING BY REFERENCE L.
           MOVE G TO L
           GOBACK.
       END METHOD K.
       END OBJECT.
       END CLASS W67QCC.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. W67QCM.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS W67QCC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 O  USAGE OBJECT REFERENCE W67QCC.
       01 U  USAGE OBJECT REFERENCE.
       01 N  PIC 9(3) VALUE 5.
       01 SN PIC S9(4) VALUE 7.
       01 H.
          05 HX PIC X(3) VALUE "ABC".
       01 N2 PIC 9(3) VALUE 1.
       01 N3 PIC 9(3) VALUE 2.
       01 N4 PIC 9(3) VALUE 3.
       01 KP PIC S9(3) COMP-3 VALUE 9.
       PROCEDURE DIVISION.
           INVOKE W67QCC "NEW" RETURNING O
           INVOKE O "M" USING BY REFERENCE N
           DISPLAY "N=[" N "]"
           INVOKE O "S" USING BY REFERENCE SN
           DISPLAY "SN=[" SN "]"
           MOVE H TO N2
           INVOKE O "P" USING BY REFERENCE N2
           DISPLAY "N2=[" N2 "]"
           INVOKE O "R" RETURNING N3
           DISPLAY "N3=[" N3 "]"
           SET U TO O
           INVOKE U "M" USING N4
           DISPLAY "N4=[" N4 "]"
           INVOKE O "K" USING BY REFERENCE KP
           DISPLAY "KP=[" KP "]"
           STOP RUN.
       END PROGRAM W67QCM.

      *> kb/Work PB1137 - INVOKE arguments bind through the rules CALL's
      *> operands take (ISO 14.9.23.3 / 14.9.23.4 GR6). Every value below is
      *> derived from the standard, not from a run:
      *>  ADDR   an ADDRESS OF through a UNIVERSAL receiver is identifier-3
      *>         (SR9; SR6 implies BY REFERENCE) and a sending operand
      *>         (SR19): the method sees the address, the caller's pointer
      *>         is untouched -> OK.
      *>  PROP   a keyword-less OBJECT PROPERTY is not a data item of the
      *>         four storage sections (8.4.3.9.4 GR1 temp-1), so
      *>         14.9.23.4 GR6 a) 2.
      *>         assumes BY CONTENT: the method's MOVE 7 is lost and BAL
      *>         stays 00100.
      *>  KONST  a constant-name is literal-2 (13.10.3 SR2) -> LN=0042,
      *>         bare and BY CONTENT.
      *>  HEX    X"4142434445" is an alphanumeric literal (8.3.3.2) -> ABCDE;
      *>         X"41" moves into PIC X(5) as "A    " (MOVE rules).
      *>  NAT    N"ABCDE" is a national literal (8.3.3.5) -> ABCDE.
      *>  NULL   NULL is an identifier of category object reference
      *>         (8.4.3.1.3 SR7, 8.4.3.7.3 SR2) -> the formal is NULL.
      *>  SELF   SELF is an identifier (8.4.3.8) passed BY CONTENT; the
      *>         instance method's class is the formal's class (SET
      *>         rules, 14.9.39.3 SR12) -> the formal is an OBJECT.
      *>  PC     a keyword-less PAGE-COUNTER is in no storage section, so
      *>         GR6 a) 2. assumes BY CONTENT: the method sees 1 (INITIATE,
      *>         8.4.3.15.4 GR2) and its MOVE 5 is lost -> PC=0001.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1137IA.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB1137K
           PROPERTY BAL.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPTF ASSIGN TO "pb1137ia.txt".
       DATA DIVISION.
       FILE SECTION.
       FD RPTF REPORT IS RPT.
       WORKING-STORAGE SECTION.
       01 K CONSTANT AS 42.
       01 X PIC X(4) VALUE "WXYZ".
       01 P USAGE POINTER.
       01 Q USAGE POINTER.
       01 N4 PIC 9(4) VALUE 0.
       01 O USAGE OBJECT REFERENCE PB1137K.
       01 U USAGE OBJECT REFERENCE.
       REPORT SECTION.
       RD RPT.
       01 DL TYPE DETAIL LINE PLUS 1.
          05 DC COLUMN 1 PIC 9(4) SOURCE N4.
       PROCEDURE DIVISION.
       MAIN.
           INVOKE PB1137K "NEW" RETURNING O.
           SET U TO O.
           SET P TO ADDRESS OF X.
           INVOKE U "ECHOP" USING ADDRESS OF X RETURNING Q.
           IF Q = ADDRESS OF X AND P = ADDRESS OF X
               DISPLAY "ADDR OK" ELSE DISPLAY "ADDR BAD".
           INVOKE O "TAKE" USING BAL OF O.
           INVOKE O "SHOW".
           INVOKE O "TAKEN" USING K.
           INVOKE O "TAKEN" USING BY CONTENT K.
           INVOKE O "TAKEX" USING BY CONTENT X"4142434445".
           INVOKE O "TAKEX" USING X"41".
           INVOKE O "TAKENAT" USING BY CONTENT N"ABCDE".
           INVOKE O "TAKEO" USING NULL.
           INVOKE O "TAKEO" USING BY CONTENT NULL.
           INVOKE O "GOSELF".
           OPEN OUTPUT RPTF.
           INITIATE RPT.
           INVOKE O "TAKEPC" USING PAGE-COUNTER.
           MOVE PAGE-COUNTER TO N4.
           DISPLAY "PC=" N4.
           TERMINATE RPT.
           CLOSE RPTF.
           STOP RUN.
       END PROGRAM PB1137IA.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB1137K INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 BAL PIC 9(5) VALUE 100 PROPERTY.
       PROCEDURE DIVISION.
       METHOD-ID. ECHOP.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LP USAGE POINTER.
       01 LR USAGE POINTER.
       PROCEDURE DIVISION USING LP RETURNING LR.
           SET LR TO LP.
       END METHOD ECHOP.
       METHOD-ID. TAKE.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LK PIC 9(5).
       PROCEDURE DIVISION USING LK.
           DISPLAY "PROP LK=" LK.
           MOVE 7 TO LK.
       END METHOD TAKE.
       METHOD-ID. SHOW.
       PROCEDURE DIVISION.
           DISPLAY "PROP BAL=" BAL.
       END METHOD SHOW.
       METHOD-ID. TAKEN.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LN PIC 9(4).
       PROCEDURE DIVISION USING LN.
           DISPLAY "KONST LN=" LN.
       END METHOD TAKEN.
       METHOD-ID. TAKEX.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LX PIC X(5).
       PROCEDURE DIVISION USING LX.
           DISPLAY "HEX LX=[" LX "]".
       END METHOD TAKEX.
       METHOD-ID. TAKENAT.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LNN PIC N(5).
       PROCEDURE DIVISION USING LNN.
           DISPLAY "NAT LNN=" LNN.
       END METHOD TAKENAT.
       METHOD-ID. TAKEO.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LO USAGE OBJECT REFERENCE PB1137K.
       PROCEDURE DIVISION USING LO.
           IF LO = NULL DISPLAY "NULL LO=NULL"
           ELSE DISPLAY "NULL LO=OBJECT".
       END METHOD TAKEO.
       METHOD-ID. GOSELF.
       PROCEDURE DIVISION.
           INVOKE SELF "TAKEO" USING SELF.
           INVOKE SELF "TAKEO" USING BY CONTENT SELF.
       END METHOD GOSELF.
       METHOD-ID. TAKEPC.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LPC PIC 9(4).
       PROCEDURE DIVISION USING LPC.
           DISPLAY "PC LPC=" LPC.
           MOVE 5 TO LPC.
       END METHOD TAKEPC.
       END OBJECT.
       END CLASS PB1137K.

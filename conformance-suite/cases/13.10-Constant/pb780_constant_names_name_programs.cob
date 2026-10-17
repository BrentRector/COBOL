*>--- conformance-case
*> id: pb780_constant_names_name_programs
*> rules: [SR-13.10.3-2]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB780 - a constant-name stands wherever a format writes a literal that NAMES a program, a method or a
      *> report code. Each position below was refused before the fix (COBOLNET1639 "not defined" at CALL, CANCEL and
      *> ADDRESS OF PROGRAM; COBOLNET1676 at CALL ... AS NESTED; COBOLNET0866 at INVOKE; COBOLNET2713 at CODE IS).
      *>
      *> RULES (cite.py --check, all OK):
      *> 13.10.3 SR2: "Except in a compiler directive, constant-name-1 may be used anywhere that a format specifies a
      *>   literal of the class and category of constant-name-1."
      *> 13.10.4 GR1: the effect is "as if literal-1 ... were written where constant-name-1 is written".
      *> The positions: CALL literal-1 (14.9.4.2; SR2 "an alphanumeric or national literal"), CALL literal-1 AS NESTED
      *> (14.9.4.3 SR15), CANCEL literal-1 (14.9.5.2), ADDRESS OF PROGRAM literal-1 (8.4.3.13.2; SR2), INVOKE
      *> object-class-name-1 literal-1 (14.9.23.2; SR3 "If object-class-name-1 is specified, literal-1 shall be
      *> specified") and CODE IS literal-1 (13.18.12.2; SR1 "an alphanumeric literal").
      *>
      *> DERIVATION. KS and KN spell the same program-name, alphanumeric and national (14.9.4.4 GR3 b) through 8.3.2.2),
      *> so CALL KS and CALL KN each activate PB780SUB: "IN SUB" twice. CANCEL KS KN cancels it twice (nothing shown).
      *> ADDRESS OF PROGRAM KS addresses the OUTERMOST program PB780SUB (8.4.3.13.4 GR2), so CALL PP prints "IN SUB".
      *> CALL KT AS NESTED activates the directly contained PB780NST: "IN NESTED". INVOKE PB780CL KM invokes the factory
      *> method GREET: "IN GREET". The report's one detail line is led by its code KC, "Q" (13.18.12.4 GR1), so its
      *> record reads Q + "HELLO": [QHELLO].
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB780CN.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           SYMBOLIC CHARACTERS SYM-X0A SYM-X0D
               ARE 11 14.
       REPOSITORY.
           CLASS PB780CL.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT ASSIGN TO "pb780cn.txt".
           SELECT CHK ASSIGN TO "pb780cn.txt".
       DATA DIVISION.
       FILE SECTION.
       FD  RPT REPORT IS R-K.
       FD  CHK.
       01  CHK-REC PIC X.
       WORKING-STORAGE SECTION.
       01  KS CONSTANT AS "PB780SUB".
       01  KN CONSTANT AS N"PB780SUB".
       01  KT CONSTANT AS "PB780NST".
       01  KM CONSTANT AS "GREET".
       01  KC CONSTANT AS "Q".
       01  PP USAGE PROGRAM-POINTER.
       01  WS-EOF  PIC X     VALUE "N".
       01  WS-I    PIC 99    VALUE 0.
       01  WS-LINE PIC X(12) VALUE SPACES.
       REPORT SECTION.
       RD  R-K CODE IS KC.
       01  DE-K TYPE DE LINE PLUS 1.
           02  COLUMN 1 PIC X(5) VALUE "HELLO".
       PROCEDURE DIVISION.
       MAIN-P.
           CALL KS.
           CALL KN.
           CANCEL KS KN.
           SET PP TO ADDRESS OF PROGRAM KS.
           CALL PP.
           CALL KT AS NESTED.
           INVOKE PB780CL KM.
           OPEN OUTPUT RPT.
           INITIATE R-K.
           GENERATE DE-K.
           TERMINATE R-K.
           CLOSE RPT.
           OPEN INPUT CHK.
           PERFORM UNTIL WS-EOF = "Y"
               READ CHK
                   AT END MOVE "Y" TO WS-EOF
                   NOT AT END PERFORM TAKE-BYTE
               END-READ
           END-PERFORM.
           CLOSE CHK.
           IF WS-I > 0
               PERFORM SHOW-LINE
           END-IF.
           STOP RUN.
       TAKE-BYTE.
           IF CHK-REC = SYM-X0A
               PERFORM SHOW-LINE
           ELSE
               IF CHK-REC NOT = SYM-X0D
                   ADD 1 TO WS-I
                   MOVE CHK-REC TO WS-LINE (WS-I:1)
               END-IF
           END-IF.
       SHOW-LINE.
           IF WS-LINE NOT = SPACES
               DISPLAY "[" WS-LINE (1:WS-I) "]"
           END-IF.
           MOVE SPACES TO WS-LINE.
           MOVE 0 TO WS-I.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB780NST.
       PROCEDURE DIVISION.
           DISPLAY "IN NESTED".
           GOBACK.
       END PROGRAM PB780NST.
       END PROGRAM PB780CN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB780SUB.
       PROCEDURE DIVISION.
           DISPLAY "IN SUB".
           GOBACK.
       END PROGRAM PB780SUB.
       IDENTIFICATION DIVISION.
       CLASS-ID. PB780CL.
       IDENTIFICATION DIVISION.
       FACTORY.
       PROCEDURE DIVISION.
       METHOD-ID. GREET.
       PROCEDURE DIVISION.
           DISPLAY "IN GREET".
       END METHOD GREET.
       END FACTORY.
       IDENTIFICATION DIVISION.
       OBJECT.
       END OBJECT.
       END CLASS PB780CL.

      *> kb/Work PB2618, DETERMINATION D-PROP1 (docs/CONFORMANCE.md): a property reference that a statement stores
      *> only IN PART (STRING's identifier-3, ISO 14.9.43.4 GR7; a reference-modified receiver, 8.4.3.3.4 GR5) runs
      *> the class's GET first when it has one; WITH NO GET the receiving temporary (8.4.3.9.4 GR2) starts as its
      *> initial value, spaces for this alphanumeric property, so the positions the statement does not reach are
      *> spaces AT EVERY EXECUTION. The temporary is program storage, and a second execution of the same statement
      *> used to start from what the first one left in it ("QBCDEF" below, where "Q     " is owed).
      *> EXPECTED, derived line by line (NM is PIC X(6) VALUE "------" PROPERTY WITH NO GET; SHOW displays it):
      *>   pass 1  STRING "ABCDEF" DELIMITED BY SPACE: all six positions          "1 [ABCDEF]"
      *>           MOVE "AB" TO NM OF D(3:2): positions 3-4, the rest spaces       "1 [  AB  ]"
      *>   pass 2  STRING "Q" DELIMITED BY SPACE (SRC is "Q     "): position 1     "2 [Q     ]"
      *>           MOVE "Q " TO NM OF D(3:2): positions 3-4, the rest spaces       "2 [  Q   ]"
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2618P2.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB2618NG
           PROPERTY NM.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 D USAGE OBJECT REFERENCE PB2618NG.
       01 SRC PIC X(6).
       01 K PIC 9.
       PROCEDURE DIVISION.
       MAIN.
           INVOKE PB2618NG "NEW" RETURNING D
           PERFORM VARYING K FROM 1 BY 1 UNTIL K > 2
               IF K = 1
                   MOVE "ABCDEF" TO SRC
               ELSE
                   MOVE "Q" TO SRC
               END-IF
               STRING SRC DELIMITED BY SPACE INTO NM OF D
               INVOKE D "SHOW" USING K
               MOVE SRC(1:2) TO NM OF D(3:2)
               INVOKE D "SHOW" USING K
           END-PERFORM
           STOP RUN.
       END PROGRAM PB2618P2.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB2618NG INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE
           CLASS PB2618NG.
       IDENTIFICATION DIVISION.
       OBJECT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 NM PIC X(6) VALUE "------" PROPERTY WITH NO GET.
       PROCEDURE DIVISION.
       IDENTIFICATION DIVISION.
       METHOD-ID. SHOW.
       DATA DIVISION.
       LINKAGE SECTION.
       01 PASS-NO PIC 9.
       PROCEDURE DIVISION USING PASS-NO.
           DISPLAY PASS-NO " [" NM "]".
       END METHOD SHOW.
       END OBJECT.
       END CLASS PB2618NG.

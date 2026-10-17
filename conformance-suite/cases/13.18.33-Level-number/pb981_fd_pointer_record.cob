*>--- conformance-case
*> id: pb981_fd_pointer_record
*> rules: [GR-13.18.33.4-3]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB981 - determination D-FRA (docs/CONFORMANCE.md section 3): a level-1 POINTER record is
      *> legal in the file section (ISO 13.18.60.3 SR14 - "only for an elementary data item at level 1")
      *> and ISO 13.18.33.4 GR3 makes it an implicit redefinition of the FD's area. A pointer has no
      *> character image (CONFORMANCE.md A.1 item 216), so it is an OUT-OF-LINE record: a READ does not
      *> reach it (its value is unchanged), and a WRITE of it sends the zero-length record.
      *>   record 1 "0123456789" : R2 = 0123456789, RP still addresses X
      *>   record 2 : the zero-length image fitted to the fixed record size - R2 is spaces
      *> This used to be refused (COBOLNET1697, a REDEFINES rule the source never invoked, then COBOLNET0899).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB981PTR.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "pb981ptr.dat"
               ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD F.
       01 R2 PIC X(10).
       01 RP USAGE POINTER.
       WORKING-STORAGE SECTION.
       01 X PIC X(4) VALUE "WXYZ".
       01 P2 USAGE POINTER.
       PROCEDURE DIVISION.
       MAIN.
           SET RP TO ADDRESS OF X
           SET P2 TO ADDRESS OF X
           OPEN OUTPUT F
           MOVE "0123456789" TO R2
           WRITE R2
           WRITE RP
           CLOSE F
           OPEN INPUT F
           READ F
           DISPLAY "R2=[" R2 "]"
           IF RP = P2
               DISPLAY "RP UNCHANGED"
           ELSE
               DISPLAY "RP CHANGED"
           END-IF
           READ F
           DISPLAY "R2=[" R2 "]"
           CLOSE F
           STOP RUN.

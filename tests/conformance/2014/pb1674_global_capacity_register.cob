      *> kb/Work PB1674 - a GLOBAL record's OCCURS DYNAMIC CAPACITY register is a global name.
      *> ISO 13.18.38.3 SR30: data-name-3 "shall be treated as though implicitly defined at the same
      *> level as the entry containing the OCCURS clause" - so it is subordinate to the GLOBAL record
      *> D-G, and ISO 8.4.6.2.2: "All data-names and screen-names subordinate to a global name are
      *> global names". A contained program (and one it contains) reads AND sets the register, and
      *> the container sees each store (one table, one capacity). ISO 8.4.6.2.1 1) applies the normal
      *> rules for qualification over the whole name set first and 3) a) then picks the item declared
      *> in the referencing element: in PB1674SH a local D-CAP hides the register, while D-CAP OF D-G
      *> reaches only the register.
      *> Expected values: the initial capacity is FROM 1 raised to the VALUE TO subscript 3 (ISO
      *> 13.18.63.4 GR16 a), within 1..4); each SET then sets the capacity. Fails if the contained
      *> programs draw "D-CAP is not defined" (the defect), if a store is lost, or if the local
      *> D-CAP is not the one PB1674SH reads (SH CAP=7) or D-CAP OF D-G is not the register.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1674OUT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 D-G GLOBAL.
           05 D-TAB OCCURS DYNAMIC CAPACITY IN D-CAP FROM 1 TO 4.
               10 D-X PIC X(2) VALUES ARE "AB" "CD" FROM (1) TO (3).
       PROCEDURE DIVISION.
           DISPLAY "OUT0 CAP=" D-CAP
           CALL "PB1674IN"
           DISPLAY "OUT1 CAP=" D-CAP
           CALL "PB1674SH"
           DISPLAY "OUT2 CAP=" D-CAP
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1674IN.
       PROCEDURE DIVISION.
           DISPLAY "IN CAP=" D-CAP
           SET D-CAP TO 2
           DISPLAY "IN SET=" D-CAP OF D-G " " D-X (2)
           CALL "PB1674DP"
           GOBACK.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1674DP.
       PROCEDURE DIVISION.
           DISPLAY "DEEP CAP=" D-CAP
           SET D-CAP TO 4
           GOBACK.
       END PROGRAM PB1674DP.
       END PROGRAM PB1674IN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1674SH.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 D-CAP PIC 9 VALUE 7.
       PROCEDURE DIVISION.
           DISPLAY "SH CAP=" D-CAP " " D-CAP OF D-G
           MOVE 8 TO D-CAP
           DISPLAY "SH MOV=" D-CAP
           GOBACK.
       END PROGRAM PB1674SH.
       END PROGRAM PB1674OUT.

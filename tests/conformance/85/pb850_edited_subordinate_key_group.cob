      *> ISO 1989:2023 12.4.5.12.3 SR2 names "a data item of category
      *> alphanumeric or category national" for the RECORD KEY. An edited
      *> elementary key is neither (8.5.2.1 Table 2 lists alphanumeric-edited
      *> as a category of its own; the negative golden
      *> pb850-record-key-alphanumeric-edited). A GROUP key holding an edited
      *> subordinate is still legal: 13.18.29.4 GR3 makes a group with no
      *> GROUP-USAGE clause an alphanumeric group item, and 8.5.2.3 3) makes
      *> that category alphanumeric whatever its subordinates are. This golden
      *> pins that the tightened screen (kb/Work PB850) still admits it and the
      *> key works: records written out of key order come back in key order,
      *> and a random READ finds a record by the edited image of its key.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB850GK.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT IXF ASSIGN TO "pb850gk.dat"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS DYNAMIC
               RECORD KEY IS IX-KEY
               FILE STATUS IS IX-STATUS.
       DATA DIVISION.
       FILE SECTION.
       FD IXF.
       01 IX-REC.
          05 IX-KEY.
             10 IX-KEY-ED PIC XXBXX.
             10 IX-KEY-NO PIC 9(3).
          05 IX-DATA PIC X(8).
       WORKING-STORAGE SECTION.
       01 IX-STATUS PIC XX.
       01 EOF-FLAG PIC X VALUE "N".
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT IXF
           MOVE "CDEF" TO IX-KEY-ED
           MOVE 2 TO IX-KEY-NO
           MOVE "SECOND" TO IX-DATA
           WRITE IX-REC
           MOVE "ABCD" TO IX-KEY-ED
           MOVE 1 TO IX-KEY-NO
           MOVE "FIRST" TO IX-DATA
           WRITE IX-REC
           CLOSE IXF
           DISPLAY "WRITE " IX-STATUS
           OPEN INPUT IXF
           PERFORM UNTIL EOF-FLAG = "Y"
               READ IXF NEXT RECORD
                   AT END MOVE "Y" TO EOF-FLAG
                   NOT AT END DISPLAY "[" IX-KEY "] " IX-DATA
               END-READ
           END-PERFORM
           MOVE "CD EF002" TO IX-KEY
           READ IXF KEY IS IX-KEY
               INVALID KEY DISPLAY "NOT FOUND " IX-STATUS
               NOT INVALID KEY DISPLAY "FOUND " IX-DATA
           END-READ
           CLOSE IXF
           STOP RUN.

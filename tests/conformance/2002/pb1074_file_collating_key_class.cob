      *> kb/Work PB1074 — the file-control COLLATING SEQUENCE applies BY THE KEY'S CLASS (ISO §12.4.5.7.4):
      *> GR2 "An alphanumeric collating sequence referenced by alphabet-name-1 applies to any record keys of
      *> class alphanumeric", GR3 "A national collating sequence referenced by alphabet-name-2 applies to any
      *> record keys of class national", GR5 "If alphabet-name-2 is not specified ... the native national
      *> collating sequence applies to any record keys of class national". So an alphanumeric alphabet never
      *> reorders a national key, nor re-judges its uniqueness (§12.4.5.12.4 GR1).
      *> File IXF: national prime key, alphanumeric alternate key, FOR ALPHANUMERIC IS REV and FOR NATIONAL IS
      *> NNAT (the NATIVE national sequence): the prime key walks A M Z, the alternate key Z M A.
      *> File G: national prime key under COLLATING SEQUENCE IS EQV ("A" ALSO "B" — alphanumeric): N"A" and N"B"
      *> are unequal in the native national sequence, so both WRITEs succeed.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1074KC.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           ALPHABET REV IS "ZYXWVUTSRQPONMLKJIHGFEDCBA"
           ALPHABET EQV IS "A" ALSO "B"
           ALPHABET NNAT FOR NATIONAL IS NATIVE.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT IXF ASSIGN TO "pb1074kc.dat"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS DYNAMIC
               RECORD KEY IS IX-KEY
               ALTERNATE RECORD KEY IS IX-ALT WITH DUPLICATES
               FILE STATUS IS WS-FS
               COLLATING SEQUENCE FOR ALPHANUMERIC IS REV
                                  FOR NATIONAL IS NNAT.
           SELECT G ASSIGN TO "pb1074kg.dat"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS DYNAMIC
               RECORD KEY IS G-KEY
               FILE STATUS IS WS-FS
               COLLATING SEQUENCE IS EQV.
       DATA DIVISION.
       FILE SECTION.
       FD IXF.
       01 IX-REC.
          05 IX-KEY  PIC N(1).
          05 IX-ALT  PIC X(1).
       FD G.
       01 G-REC.
          05 G-KEY   PIC N(1).
          05 G-DAT   PIC X(2).
       WORKING-STORAGE SECTION.
       01 WS-EOF PIC 9 VALUE 0.
       01 WS-FS  PIC X(2) VALUE "00".
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT IXF.
           MOVE N"M" TO IX-KEY MOVE "M" TO IX-ALT WRITE IX-REC.
           MOVE N"A" TO IX-KEY MOVE "A" TO IX-ALT WRITE IX-REC.
           MOVE N"Z" TO IX-KEY MOVE "Z" TO IX-ALT WRITE IX-REC.
           CLOSE IXF.
           OPEN INPUT IXF.
           DISPLAY "PRIME:" WITH NO ADVANCING.
           PERFORM UNTIL WS-EOF = 1 OR WS-FS NOT = "00"
               READ IXF NEXT
                   AT END MOVE 1 TO WS-EOF
                   NOT AT END DISPLAY " " IX-ALT WITH NO ADVANCING
               END-READ
           END-PERFORM.
           DISPLAY " .".
           MOVE "Z" TO IX-ALT.
           START IXF KEY IS >= IX-ALT.
           MOVE 0 TO WS-EOF.
           DISPLAY "ALT:" WITH NO ADVANCING.
           PERFORM UNTIL WS-EOF = 1 OR WS-FS NOT = "00"
               READ IXF NEXT
                   AT END MOVE 1 TO WS-EOF
                   NOT AT END DISPLAY " " IX-ALT WITH NO ADVANCING
               END-READ
           END-PERFORM.
           DISPLAY " .".
           CLOSE IXF.
           OPEN OUTPUT G.
           MOVE N"A" TO G-KEY MOVE "G1" TO G-DAT WRITE G-REC.
           DISPLAY "GW1 " WS-FS.
           MOVE N"B" TO G-KEY MOVE "G2" TO G-DAT WRITE G-REC.
           DISPLAY "GW2 " WS-FS.
           CLOSE G.
           STOP RUN.

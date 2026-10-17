      *>--- conformance-case
      *> id: pb164_index_group_image
      *> rules: [GR-13.18.60.4-10, GR-14.9.11.4-6, DOC-A.1-211]
      *> accept: [2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB164IX.
      *> kb/Work PB164 + R40 (OWNER DECISION 2026-08-30) - AN INDEX
      *> DATA ITEM'S IMAGE IS ITS OCCURRENCE NUMBER AS AN 8-BYTE
      *> BIG-ENDIAN TWO'S-COMPLEMENT BINARY (A.1 ITEM 211, 4.2.16),
      *> SO AN INDEX-LEAF GROUP CROSSES MOVE-AS-GROUP AND CALL BY
      *> REFERENCE (14.2.3 GR8) LIKE EVERY OTHER PINNED FORM. THE
      *> BYTE ORDER IS DISCRIMINATED WITH FUNCTION ORD READS (BYTES
      *> 4-10 OF THE IMAGE ARE ZERO, BYTE 11 CARRIES OCCURRENCE 5);
      *> THE ROUND-TRIPPED VALUE IS OBSERVED ONLY THROUGH REFERENCES
      *> 13.18.60.3 SR10 PERMITS - SET AND A RELATION CONDITION.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-T.
          05 WS-E PIC X OCCURS 9 INDEXED BY IX.
       01 GI.
          05 GA PIC X(3) VALUE "ABC".
          05 GN USAGE INDEX.
       01 GI2.
          05 GA-2 PIC X(3).
          05 GN-2 USAGE INDEX.
       01 XI PIC X(11).
       01 WS-D PIC 9.
       PROCEDURE DIVISION.
       MAIN.
           SET IX TO 5
           SET GN TO IX
           MOVE GI TO XI
           DISPLAY "B=[" FUNCTION ORD(XI(4:1)) " "
               FUNCTION ORD(XI(11:1)) "]"
           MOVE XI TO GI2
           SET IX TO GN-2
           MOVE 0 TO WS-D
           IF IX = 5
               MOVE 1 TO WS-D
           END-IF
           DISPLAY "RT=" WS-D " " GA-2
           CALL "SUBIX" USING GI
           SET IX TO GN
           IF IX = 7
               DISPLAY "CALL=7"
           END-IF
           STOP RUN.
       END PROGRAM PB164IX.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SUBIX.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-T2.
          05 WS-E2 PIC X OCCURS 9 INDEXED BY JX.
       LINKAGE SECTION.
       01 LGI.
          05 LGA PIC X(3).
          05 LGN USAGE INDEX.
       PROCEDURE DIVISION USING LGI.
       MAIN.
           SET JX TO 7
           SET LGN TO JX
           GOBACK.
       END PROGRAM SUBIX.

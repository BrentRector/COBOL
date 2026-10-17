      *>--- conformance-case
      *> id: l1_search_when_boolean_omitted
      *> rules: [GR-8.8.4.3.4-2, SR-14.9.37.3-6]
      *> accept: [2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §14.9.37.3 SR6 — the two §8.8.4 sub-forms that need a
      *> carrier the one-table breadth golden cannot supply: the simple
      *> boolean condition (§8.8.4.3) and the simple omitted-argument
      *> condition (§8.8.4.8, whose SR1 requires a formal parameter of
      *> the source element the condition is written in).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1SRWBO.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-T.
          05 WS-E OCCURS 5 TIMES INDEXED BY IX.
             10 WS-B PIC 1.
       01 WS-N PIC 9 VALUE 0.
       01 WS-ARG PIC 9 VALUE 1.
       PROCEDURE DIVISION.
       MAIN-P.
           MOVE B"0" TO WS-B (1).
           MOVE B"0" TO WS-B (2).
           MOVE B"1" TO WS-B (3).
           MOVE B"0" TO WS-B (4).
           MOVE B"0" TO WS-B (5).
      *> §8.8.4.3.4 GR1: the boolean expression IS the condition and is
      *> true where the position holds 1 — occurrence 3.
           SET IX TO 1.
           SEARCH WS-E
               AT END DISPLAY "B1=ATEND"
               WHEN WS-B(IX)
                   SET WS-N TO IX
                   DISPLAY "B1=" WS-N
           END-SEARCH.
      *> §8.8.4.3.4 GR2: NOT reverses it — true at occurrence 1.
           SET IX TO 1.
           SEARCH WS-E
               AT END DISPLAY "B2=ATEND"
               WHEN NOT WS-B(IX)
                   SET WS-N TO IX
                   DISPLAY "B2=" WS-N
           END-SEARCH.
           CALL "L1SRWBOS" AS NESTED USING OMITTED.
           CALL "L1SRWBOS" AS NESTED USING WS-ARG.
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1SRWBOS.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-U.
          05 WS-Z PIC 9 OCCURS 3 TIMES INDEXED BY JX.
       01 WS-M PIC 9 VALUE 0.
       LINKAGE SECTION.
       01 L-Q PIC 9.
       PROCEDURE DIVISION USING OPTIONAL L-Q.
       SUB-P.
           MOVE 1 TO WS-Z (1).
           MOVE 2 TO WS-Z (2).
           MOVE 3 TO WS-Z (3).
      *> §8.8.4.8.4 GR1a: true when the activating statement wrote
      *> OMITTED for this formal parameter. On the first activation the
      *> condition is true on the first evaluation (occurrence 1); on
      *> the second an argument was supplied, so it is false at every
      *> occurrence and the AT END phrase is taken.
           SET JX TO 1.
           SEARCH WS-Z
               AT END DISPLAY "OM=ATEND"
               WHEN L-Q IS OMITTED
                   SET WS-M TO JX
                   DISPLAY "OM=" WS-M
           END-SEARCH.
           GOBACK.
       END PROGRAM L1SRWBOS.
       END PROGRAM L1SRWBO.

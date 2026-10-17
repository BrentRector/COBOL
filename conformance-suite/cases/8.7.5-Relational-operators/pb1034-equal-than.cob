*>--- conformance-case
*> id: pb1034-equal-than
*> rules: [SR-8.7.5.2-2, FMT-8.7.5.1, FMT-8.8.4.2.2]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB1034. ISO 1989:2023 8.8.4.2.2 Format 1 prints IS [NOT] EQUAL TO: EQUAL's optional word is TO,
      *> never THAN (the underlined words are EQUAL; IS and TO are the optional ones). `EQUAL THAN` is in no
      *> format. The operator rule used to admit it beside EQUAL TO.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1034NEG5.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A PIC 9 VALUE 5.
       01 B PIC 9 VALUE 3.
       PROCEDURE DIVISION.
       MAIN.
           IF A EQUAL THAN B
               DISPLAY "EQ"
           ELSE
               DISPLAY "NE"
           END-IF
           STOP RUN.

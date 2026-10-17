*>--- conformance-case
*> id: find_string_phrase_order
*> rules: [AR-15.3-7]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work R20 (ledger F28) - FIND-STRING's general format fixes phrase ORDER and MULTIPLICITY:
      *> argument-1 argument-2 [LAST] [[START AFTER] argument-3] [ANYCASE] (ISO 15.37.2, the underlined
      *> words). The five legal shapes below pin the positional walk; the negative fixture
      *> find-string-dangling-start-after pins the case that mattered - a START AFTER with no argument-3
      *> used to DISCARD the two written words and silently degrade to the plain two-argument form.
      *> Values derived from 15.37.4: "ABCABCABC" - first "ABC" at 1; LAST at 7; skip-one at 4.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. R20FSORD.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 H PIC X(9) VALUE "ABCABCABC".
       01 N PIC X(3) VALUE "ABC".
       01 P PIC 9.
       PROCEDURE DIVISION.
           MOVE FUNCTION FIND-STRING(H N) TO P.
           DISPLAY "PLAIN=" P.
           MOVE FUNCTION FIND-STRING(H N LAST) TO P.
           DISPLAY "LAST=" P.
           MOVE FUNCTION FIND-STRING(H N START AFTER 1) TO P.
           DISPLAY "SKIP=" P.
           MOVE FUNCTION FIND-STRING(H N 1 ANYCASE) TO P.
           DISPLAY "BARE3=" P.
           MOVE FUNCTION FIND-STRING(H N LAST ANYCASE) TO P.
           DISPLAY "BOTH=" P.
           STOP RUN.

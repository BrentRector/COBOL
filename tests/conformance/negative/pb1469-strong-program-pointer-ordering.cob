      *> reject-at: 2002 2014 2023
      *> ISO 8.8.4.2.3 SR4 - "Strongly-typed group items that contain elementary items of class boolean,
      *> message-tag, object, or pointer, may be compared only for equality or inequality." Class POINTER is
      *> three categories (8.5.2.1 Table 2): data-pointer, program-pointer and function-pointer. The screen used
      *> to list CATEGORIES and missed program-pointer, so this ordering compiled clean and aborted the run unit
      *> (kb/Work PB1469). The equality of the same groups is legal (strong_group_element_order, P1/P2).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1469-PP-ORDER.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 P-T TYPEDEF STRONG.
          05 PX USAGE PROGRAM-POINTER.
       01 T1 TYPE P-T.
       01 T2 TYPE P-T.
       PROCEDURE DIVISION.
       MAIN-PARA.
           IF T1 > T2 DISPLAY "GT" ELSE DISPLAY "LE" END-IF
           STOP RUN.

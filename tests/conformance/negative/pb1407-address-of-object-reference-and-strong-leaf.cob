      *> reject-at: 2002 2014 2023
      *> kb/Work PB1407. ISO 8.4.3.11.3 SR2: "Identifier-1 shall not reference an object reference or an elementary item
      *> subordinate to a strongly-typed group item." Both halves are refused COBOLNET2785 (the SR2 text names the rule),
      *> in the SET sender and the relation operand. The legal neighbours compile: the address of the strongly-typed
      *> GROUP itself (a restricted data-pointer, 8.4.3.11.4 GR2) and of a leaf of a WEAKLY typed record.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1407N2.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 O USAGE OBJECT REFERENCE.
       01 T-REC IS TYPEDEF STRONG.
          05 T-A PIC X(4).
       01 W-REC IS TYPEDEF.
          05 W-A PIC X(4).
       01 S TYPE T-REC.
       01 WK TYPE W-REC.
       01 P USAGE POINTER.
       01 T-RP IS TYPEDEF USAGE POINTER TO T-REC.
       01 PS TYPE T-RP.
       PROCEDURE DIVISION.
       MAIN.
           SET P TO ADDRESS OF O
           SET P TO ADDRESS OF T-A OF S
           IF P = ADDRESS OF T-A OF S
               DISPLAY "EQ"
           END-IF
           SET PS TO ADDRESS OF S
           SET P TO ADDRESS OF W-A OF WK
           STOP RUN.

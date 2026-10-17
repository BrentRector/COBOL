*>--- conformance-case
*> id: pb73_table16_function_type_and_boolean_view
*> rules: [GR-8.4.3.3.4-1, GR-8.4.3.3.4-2, GR-8.4.3.3.4-6,
*>    SR-14.9.25.3-10, GR-15.2-1, GR-15.2-4, GR-15.2-5, DOC-A.1-92]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> PB73 - two Table-16 (ISO 14.9.25.3 SR10) derivations, adjudicated 2026-08-18.
      *>
      *> (1) A function's 15.2 TYPE decides its Table-16 row as a MOVE sender. An INTEGER function ("an operational
      *>     sign and no digits to the right of the decimal point", 15.2 item 5) is the Integer row - alphanumeric and
      *>     national receivers are legal (the item-92 text form: the significant digits, no padding). A NUMERIC
      *>     function (item 4 - "an operational sign", nothing said about the decimal point) is the Noninteger row:
      *>     legal only into a numeric or numeric-edited receiver - 8.4.3.2.3 SR11 states the same principle for the
      *>     integer-operand positions ("a numeric function shall not be specified where an integer operand is
      *>     required, even though a particular reference of the numeric function might yield an integer value").
      *>     The negatives pb73-move-numeric-function-to-* pin the refusal; --permissive keeps the former admission
      *>     as its literal text, with a warning.
      *> (2) 8.4.3.3.4 GR2 ("operated upon for purposes of reference modification as if it were redefined as a
      *>     data item of class and category alphanumeric") governs the OPERATION - the positions - and GR6 the
      *>     RESULT: "the same class, category, and usage as that defined for identifier-1" except an EXHAUSTIVE
      *>     list (edited -> un-edited, numeric -> alphanumeric/national) that names neither boolean nor
      *>     alphabetic. So a DISPLAY-FORM boolean's slice is BOOLEAN (GR1 gives a boolean item boolean positions
      *>     in either form; GR5a makes them character positions unless the usage is bit) exactly as a BIT-form
      *>     one's is: it moves to boolean and alphanumeric receivers, is a class-boolean argument, and is refused
      *>     into PIC 9 (the negatives pb73-move-display-boolean-view-to-numeric / -bit-boolean-view-to-numeric).
      *>     The same reading keeps an ALPHABETIC slice alphabetic - PB72's 2026-08-09 erasure is reversed
      *>     (negatives pb73-move-alphabetic-view-to-boolean / -to-numeric).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB73T16.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X10  PIC X(10).
       01 N9   PIC 9.
       01 NUM  PIC 9(3)V99 VALUE 12.5.
       01 B4   PIC 1(4) VALUE B"1010".
       01 BB4  PIC 1(4) USAGE BIT VALUE B"1010".
       01 BB2  PIC 1(2) USAGE BIT.
       PROCEDURE DIVISION.
      *> integer-typed functions: Table 16's Integer row admits an alphanumeric receiver
           MOVE FUNCTION ORD("A") TO X10
           DISPLAY "ORD=[" X10 "]"
           MOVE FUNCTION LENGTH(X10) TO X10
           DISPLAY "LEN=[" X10 "]"
           MOVE FUNCTION MAX(3 -14 8) TO X10
           DISPLAY "MAX=[" X10 "]"
           MOVE FUNCTION ABS(-7) TO X10
           DISPLAY "ABS=[" X10 "]"
           MOVE FUNCTION INTEGER-PART(NUM) TO X10
           DISPLAY "IPT=[" X10 "]"
      *> numeric-typed functions: the Noninteger row still admits a NUMERIC receiver
           MOVE FUNCTION SQRT(16) TO N9
           DISPLAY "SQRT=" N9
           MOVE FUNCTION NUMVAL("7.9") TO N9
           DISPLAY "NUMVAL=" N9
      *> a display-form boolean's ref-mod view is BOOLEAN (GR6 base): boolean and alphanumeric receivers, a
      *> class-boolean argument (15.45.3 r1), and an alphanumeric sender into it (Alphanumeric -> Boolean: Yes)
           MOVE B4(3:2) TO X10
           DISPLAY "B4(3:2)->X=[" X10 "]"
           MOVE B4(3:2) TO BB2
           DISPLAY "B4(3:2)->BB2=" BB2
           COMPUTE N9 = FUNCTION INTEGER-OF-BOOLEAN(B4(2:3))
           DISPLAY "IOB(B4(2:3))=" N9
           MOVE "11" TO B4(2:2)
           DISPLAY "B4=" B4
      *> a BIT-form boolean's view is boolean over BIT positions (GR1 / GR5a): the same cells
           MOVE BB4(2:2) TO BB2
           DISPLAY "BB4(2:2)->BB2=" BB2
           COMPUTE N9 = FUNCTION INTEGER-OF-BOOLEAN(BB4(1:3))
           DISPLAY "IOB(BB4(1:3))=" N9
           STOP RUN.

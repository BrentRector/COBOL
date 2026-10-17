*>--- conformance-case
*> id: pb982_bare_operand_conditions
*> rules: []
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB982 - the bare operands that ARE conditions, beside the ones that are not.
      *>
      *> THE RULE. ISO 8.8.4.2.1: "The simple conditions are the relation, boolean, class, condition-name,
      *> switch-status, sign, and omitted-argument conditions." A bare data item that is none of them is
      *> refused (COBOLNET2318; the negative twins tests/conformance/negative/pb982-*). This golden pins the
      *> bare operands the refusal must NOT reach:
      *>   - a level-88 condition-name (8.8.4.5), bare and under NOT;
      *>   - a one-position boolean item, a simple boolean condition (8.8.4.3) - a 2002 introduction, which is
      *>     why this golden runs at COBOL-2002;
      *>   - a bare operand that is the OBJECT of an abbreviated combined relation (8.8.4.12): the last stated
      *>     subject and operator are inserted, so IF N = 1 OR 5 is IF N = 1 OR N = 5.
      *> EXPECTED VALUES, DERIVED. N = 5, WS-X = "5", WB = B"1", N-FIVE is VALUE 5:
      *>   IF N-FIVE          -> true             -> "88=Y"
      *>   IF NOT N-FIVE      -> false            -> "NOT88=N"
      *>   IF WB              -> WB = B"1" true   -> "BOOL=Y"
      *>   IF N = 1 OR 5      -> N = 1 OR N = 5   -> "ABBR=Y"
      *>   IF N > 7 OR < 3    -> false            -> "ABBR2=N"
      *>   IF WS-X = "4" OR WS-X -> WS-X = WS-X   -> "ABBR3=Y"
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB982POS.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N PIC 9 VALUE 5.
          88 N-FIVE VALUE 5.
       01 WS-X PIC X VALUE "5".
       01 WB PIC 1 VALUE B"1".
       PROCEDURE DIVISION.
           IF N-FIVE DISPLAY "88=Y" ELSE DISPLAY "88=N" END-IF
           IF NOT N-FIVE DISPLAY "NOT88=Y" ELSE DISPLAY "NOT88=N" END-IF
           IF WB DISPLAY "BOOL=Y" ELSE DISPLAY "BOOL=N" END-IF
           IF N = 1 OR 5 DISPLAY "ABBR=Y" ELSE DISPLAY "ABBR=N" END-IF
           IF N > 7 OR < 3 DISPLAY "ABBR2=Y" ELSE DISPLAY "ABBR2=N" END-IF
           IF WS-X = "4" OR WS-X
               DISPLAY "ABBR3=Y"
           ELSE
               DISPLAY "ABBR3=N"
           END-IF
           STOP RUN.

      *>--- conformance-case
      *> id: pb420_initialize_float_receiver_85
      *> rules: [GR-14.9.20.4-4, GR-14.9.20.4-6]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB420 — the IMPLICIT MOVE of ISO 14.9.20.4 GR4 into a FLOATING-POINT receiver, at the edition
      *> where the construct is oldest. USAGE COMP-1 / COMP-2 and the INITIALIZE REPLACING phrase are both
      *> COBOL-85, so every line below is conforming COBOL-85 source and the defect this file pins was
      *> reachable at EVERY edition: one float leaf anywhere in the group made the whole statement abort the
      *> run unit, while the identical explicit MOVE the same rule prescribes compiled and ran.
      *>
      *> THE RULE. 14.9.20.4 GR4: "Whether identifier-1 references an elementary item or a group item, the
      *> effect of the execution of the INITIALIZE statement is as though a series of implicit MOVE or SET
      *> statements, each of which has an elementary data item as its receiving operand, were executed." It
      *> then splits on the RECEIVER's category and on nothing else — "If the category of a receiving-operand
      *> is data-pointer, function-pointer, message-tag, object-reference, or program-pointer, the implicit
      *> statement is: SET receiving-operand TO sending-operand. Otherwise, the implicit statement is: MOVE
      *> sending-operand TO receiving-operand." A COMP-1/COMP-2 item is class numeric, category NUMERIC
      *> (8.5.2.4), so it takes the MOVE arm with no exemption of any kind: the same conversion, the same
      *> EC-DATA-OVERFLOW check (14.9.25.4 GR6 d)4.a) and the same store as `MOVE <the same sender> TO <it>`.
      *>
      *> EXPECTED, DERIVED FROM THE RULE BEFORE THE RUN:
      *>   T1  REPLACING NUMERIC DATA BY 7.5 — GR5c2 makes all three numeric leaves receiving-operands and
      *>       GR6b makes literal-1 the sender. The implicit MOVE into F1 (COMP-1) and F2 (COMP-2) stores the
      *>       algebraic value: 7.5 is exactly representable in both IEEE binary32 and binary64, so both read
      *>       back 7.5. The implicit MOVE into N (PIC 9(3)) aligns by decimal point and truncates the
      *>       fraction the receiver has no positions for (14.6.8.2) -> 007.
      *>   T2  REPLACING NUMERIC DATA BY S, an identifier-2 sender (COMP-2 holding 4.25, again exact in both
      *>       precisions). Same GR6b arm, the other operand form -> 4.25 / 4.25 / 004.
      *>   T3  the bare form: no VALUE and no REPLACING phrase, so GR5c4 makes every possible receiving-operand
      *>       one, and GR6c's table gives a NUMERIC receiver "Figurative constant ZEROES" -> 0 / 0 / 000.
      *>   T4  a TABLE of COMP-2. GR5b2: "If the elementary data item is a table element, each occurrence of
      *>       the elementary data item is a possible receiving-operand" -> every occurrence takes 3.5.
      *>   T5  the BYTE IDENTITY the one-seam repair must preserve. W holds the twelve window characters an
      *>       explicit `MOVE 3.5` into both leaves deposits; the INITIALIZE then re-deposits into the same
      *>       REDEFINES window. 13.18.44.4 GR1 gives R and R2 one storage area, so the implicit MOVE must
      *>       leave exactly the bytes the explicit one did -> MATCH.
      *>   T6  ACCEPT's Format-2 transfer, whose 14.9.1.4 GR6 is the same rule read from the other end: "The
      *>       ACCEPT statement causes the information requested to be transferred to the data item specified
      *>       by identifier-2 according to the rules for the MOVE statement." GR12 makes DAY-OF-WEEK a
      *>       conceptual unsigned integer, 1 = Monday through 7 = Sunday, so a float receiver holds a value
      *>       in that closed range on every day the test can run.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB420FL85.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 S USAGE COMP-2.
       01 G.
          05 F1 USAGE COMP-1.
          05 F2 USAGE COMP-2.
          05 N PIC 9(3).
       01 T.
          05 TE OCCURS 3 TIMES USAGE COMP-2.
       01 R.
          05 RF1 USAGE COMP-2.
          05 RF2 USAGE COMP-1.
       01 R2 REDEFINES R PIC X(12).
       01 W PIC X(12).
       01 DW USAGE COMP-2.
       PROCEDURE DIVISION.
       MAIN.
           INITIALIZE G REPLACING NUMERIC DATA BY 7.5.
           DISPLAY "T1=[" F1 "][" F2 "][" N "]".
           MOVE 4.25 TO S.
           INITIALIZE G REPLACING NUMERIC DATA BY S.
           DISPLAY "T2=[" F1 "][" F2 "][" N "]".
           INITIALIZE G.
           DISPLAY "T3=[" F1 "][" F2 "][" N "]".
           INITIALIZE T REPLACING NUMERIC DATA BY 3.5.
           DISPLAY "T4=[" TE (1) "][" TE (2) "][" TE (3) "]".
           MOVE 3.5 TO RF1.
           MOVE 3.5 TO RF2.
           MOVE R2 TO W.
           MOVE 0 TO RF1.
           MOVE 0 TO RF2.
           INITIALIZE R REPLACING NUMERIC DATA BY 3.5.
           IF R2 = W
               DISPLAY "T5=[MATCH]"
           ELSE
               DISPLAY "T5=[DIFFER]"
           END-IF.
           ACCEPT DW FROM DAY-OF-WEEK.
           IF DW >= 1 AND DW <= 7
               DISPLAY "T6=[IN-RANGE]"
           ELSE
               DISPLAY "T6=[OUT-OF-RANGE]"
           END-IF.
           STOP RUN.

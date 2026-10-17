      *> kb/Work PB3268 - the 2002 additions to the multi-receiver / EVALUATE one-evaluation rule
      *> (the 85 golden pb3268_function_value_one_evaluation carries the rule and its citations: ISO 14.9.25.4
      *> GR1 "evaluated only once, immediately before data is moved to the first of the receiving operands",
      *> its MOVE a TO temp / MOVE temp TO b / MOVE temp TO c equivalence, 15.4.1 "the returned value is the
      *> same for all instances of a given function", 14.9.13.4 GR3). FUNCTION PI is 2002, and so is a
      *> fixed-point item of more than 18 digit positions (13.18.40.3 SR14: 1 through 31).
      *>
      *> Derived: PI is 3.14159265358979323846..., truncated by the MOVE (14.6.8.2 rule 4) to 15 places
      *> 3.141592653589793 and to 2 places 3.14; E is 2.71828182845904523536... -> 2.718281828459045;
      *> NUMVAL of a 25-integer-digit text is that integer exactly (15.67.4 r1; DOC-A.1-92) - the intermediate
      *> used to hold 21 integer digits and stored the 21 LOW-order ones in the second receiver.
      *>
      *> P1  PI to Q1 Q2 beside the control Q3: 3141592653589793 in all three.
      *> P2  PI to a 2-place and a 15-place receiver: 00314 / 3141592653589793.
      *> P3  E to two PIC 9V9(15) beside the control: 2718281828459045 in all three.
      *> P4  NUMVAL of 1234567890123456789012345 to two PIC 9(25) beside the control: the whole integer in
      *>     all three (the second receiver held 0000567890123456789012345).
      *> P5  EVALUATE FUNCTION PI against 3.141592653 and 3.14159265358979: neither is the value PI has -> OTHER.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3268PI.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 Q1 PIC 9V9(15).
       01 Q2 PIC 9V9(15).
       01 Q3 PIC 9V9(15).
       01 S1 PIC 9(3)V99.
       01 B1 PIC 9(25).
       01 B2 PIC 9(25).
       01 B3 PIC 9(25).
       PROCEDURE DIVISION.
           MOVE FUNCTION PI TO Q1 Q2
           MOVE FUNCTION PI TO Q3
           DISPLAY "P1=" Q1 " " Q2 " " Q3
           MOVE FUNCTION PI TO S1 Q1
           DISPLAY "P2=" S1 " " Q1
           MOVE FUNCTION E TO Q1 Q2
           MOVE FUNCTION E TO Q3
           DISPLAY "P3=" Q1 " " Q2 " " Q3
           MOVE FUNCTION NUMVAL("1234567890123456789012345") TO B1 B2
           MOVE FUNCTION NUMVAL("1234567890123456789012345") TO B3
           DISPLAY "P4=" B1 " " B2 " " B3
           EVALUATE FUNCTION PI
               WHEN 3.141592653 DISPLAY "P5=EQ9"
               WHEN 3.14159265358979 DISPLAY "P5=EQ14"
               WHEN OTHER DISPLAY "P5=OTHER"
           END-EVALUATE
           STOP RUN.

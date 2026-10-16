      *> kb/Work PB3268 - a NUMERIC function moved to MORE THAN ONE receiver under ARITHMETIC IS
      *> STANDARD-DECIMAL. ISO 14.9.25.4 GR1: a function-identifier "is evaluated only once, immediately before
      *> data is moved to the first of the receiving operands", and the rule writes the shape out as MOVE a TO
      *> temp / MOVE temp TO b / MOVE temp TO c, "where 'temp' is an intermediate result item provided by the
      *> implementor". 15.4.1: "When standard-decimal arithmetic or standard-binary arithmetic is in effect, the
      *> returned value for numeric and integer functions is contained in a temporary standard data item in the
      *> intermediate form defined for the arithmetic mode in effect" - a 34-digit SDIDI, so the receivers of the
      *> one MOVE are owed the digits the SDIDI holds, not the nine fraction digits the implementor's fixed
      *> S9(21)V9(9) intermediate kept.
      *>
      *> Derived: PI is 3.14159265358979323846..., truncated by the MOVE (14.6.8.2 rule 4) to 15 places
      *> 3.141592653589793; E is 2.71828182845904523536... -> 2.718281828459045; SQRT(2) is
      *> 1.41421356237309504880... -> 1.414213562373095; NUMVAL("0.12345678901234567890") is exactly that
      *> value (15.67.4 r1) -> its 20 places.
      *>
      *> D1  PI to Q1 Q2 beside the control Q3: 3141592653589793 in all three.
      *> D2  E to Q1 Q2 beside the control: 2718281828459045 in all three.
      *> D3  SQRT(2) to Q1 Q2 beside the control: 1414213562373095 in all three.
      *> D4  NUMVAL of the 20-place text to two PIC V9(20) beside the control: 12345678901234567890 in all three.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3268SD.
       OPTIONS.
           ARITHMETIC IS STANDARD-DECIMAL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 Q1 PIC 9V9(15).
       01 Q2 PIC 9V9(15).
       01 Q3 PIC 9V9(15).
       01 N1 PIC V9(20).
       01 N2 PIC V9(20).
       01 N3 PIC V9(20).
       PROCEDURE DIVISION.
           MOVE FUNCTION PI TO Q1 Q2
           MOVE FUNCTION PI TO Q3
           DISPLAY "D1=" Q1 " " Q2 " " Q3
           MOVE FUNCTION E TO Q1 Q2
           MOVE FUNCTION E TO Q3
           DISPLAY "D2=" Q1 " " Q2 " " Q3
           MOVE FUNCTION SQRT(2) TO Q1 Q2
           MOVE FUNCTION SQRT(2) TO Q3
           DISPLAY "D3=" Q1 " " Q2 " " Q3
           MOVE FUNCTION NUMVAL("0.12345678901234567890") TO N1 N2
           MOVE FUNCTION NUMVAL("0.12345678901234567890") TO N3
           DISPLAY "D4=" N1 " " N2 " " N3
           STOP RUN.

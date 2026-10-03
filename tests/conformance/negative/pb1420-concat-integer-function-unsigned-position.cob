      *> reject-at: 2023
      *> ISO 8.4.3.2.3 SR12: "An integer function other than the integer form of the ABS function shall not be
      *> specified where an unsigned integer is required" (cite.py --check 8.4.3.2.3 OK, rule 12). 15.18.3 r3
      *> requires a numeric CONCAT argument to be an unsigned integer (cite.py --check 15.18.3 "If argument-1 or
      *> argument-2 is numeric, it shall be usage display or national and shall be an unsigned integer" -> OK), and
      *> FUNCTION INTEGER is an integer function (15.2 item 5) that is not ABS.
      *> The standard settles it on the function's TYPE, as SR11 does for an integer operand: INTEGER(3) yields the
      *> integer 3 and is still barred. The legal ABS form is conformance:2023/pb1420_abs_integer_form_unsigned_position.
      *> IT COMPILED CLEAN AND ABORTED AT RUN TIME ('intrinsic string argument BoundComputedOperand'), kb/Work PB1420.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1420NEG1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W PIC X(10).
       PROCEDURE DIVISION.
           MOVE FUNCTION CONCAT(FUNCTION INTEGER(3) "A") TO W
           STOP RUN.

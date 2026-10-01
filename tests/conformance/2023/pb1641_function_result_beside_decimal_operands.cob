      *> kb/Work PB1641 - a returned value is not a floating-point operand. Under native arithmetic
      *>   the returned value of a floating-math function is implementor-defined (15.4.1: "the
      *>   characteristics and representation of the returned value are defined by the implementor"),
      *>   and nothing licenses carrying a fixed-point operand in binary64 beside it.  The ISO rule is
      *>   stated for ADD (14.9.2.4 GR4) and SUBTRACT (14.9.44.4 GR4): an operand NOT described with
      *>   binary-char, binary-short, binary-long, binary-double, float-short, float-long or
      *>   float-extended is carried with "enough places ... so as not to lose significant digits"
      *>   (14.7.7 r2 lists those usages, a floating-point literal and an intrinsic function as
      *>   separate bullets); for MULTIPLY, DIVIDE, COMPUTE and conditions the same lane is this
      *>   compiler's determination (8.8.1.3, docs/CONFORMANCE.md DOC-A.1-123), which GnuCOBOL's
      *>   decimal evaluation of every intrinsic result supports.  Carrying 13.2 as the binary64
      *>   13.199999999999999 loses a digit: 13.2 + SQRT(16) stored 17.1.  Every expected value below is exact decimal
      *>   arithmetic on a function value that is itself exact (SQRT(16) = 4, SQRT(4) = 2,
      *>   SQRT(0.04) = 0.2, LOG10(100) = 2, 4 ** 0.5 = 2), stored by truncation (14.7.4.3 r2).
      *>   cite.py --check 14.9.2.4 "enough places shall be carried so as not to lose any
      *>     significant digits during execution" -> OK 14.9.2.4 4)
      *>   cite.py --check 14.9.44.4 "enough places shall be carried so as not to lose significant
      *>     digits during execution" -> OK 14.9.44.4 4)
      *>   cite.py --check 15.4.1 "the characteristics and representation of the returned value are
      *>     defined by the implementor" -> OK 15.4.1
      *>   cite.py --check 15.84.4 "the returned value is the absolute value of the approximation
      *>     of the square root of argument-1" -> OK 15.84.4 4)  (exactly 4 for 16)
      *>   cite.py --check 14.7.4.3 "If the ROUNDED phrase is not specified, execution is as if
      *>     ROUNDED MODE IS TRUNCATION had been specified" -> OK 14.7.4.3 2)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1641FRB.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A  PIC 99V9 VALUE 13.2.
       01 B  PIC 9V9  VALUE 3.3.
       01 R  PIC 99V9.
       01 E  PIC 99.9.
       01 TENTH PIC 9V9 VALUE 0.1.
       01 FL COMP-2 VALUE 0.1.
       PROCEDURE DIVISION.
       MAIN.
      *> ADD ... GIVING: 13.2 + 4 = 17.2
           ADD A FUNCTION SQRT(16) GIVING R
           MOVE R TO E
           DISPLAY "ADD=" E
      *> COMPUTE with a literal: 13.2 + 4 = 17.2
           COMPUTE R = 13.2 + FUNCTION SQRT(16)
           MOVE R TO E
           DISPLAY "COMPUTE-LIT=" E
      *> COMPUTE with the item: 13.2 + 4 = 17.2
           COMPUTE R = A + FUNCTION SQRT(16)
           MOVE R TO E
           DISPLAY "COMPUTE-ITEM=" E
      *> MULTIPLY Format 1: 3.3 * 2 = 6.6
           MULTIPLY FUNCTION SQRT(4) BY B
           MOVE B TO E
           DISPLAY "MULTIPLY=" E
      *> SUBTRACT ... GIVING: 13.2 - 4 = 9.2
           SUBTRACT FUNCTION SQRT(16) FROM A GIVING R
           MOVE R TO E
           DISPLAY "SUBTRACT=" E
      *> a product: 13.2 * 2 = 26.4
           COMPUTE R = A * FUNCTION SQRT(4)
           MOVE R TO E
           DISPLAY "PRODUCT=" E
      *> another floating-math function: LOG10(100) = 2, so 13.2 + 2 = 15.2
           COMPUTE R = A + FUNCTION LOG10(100)
           MOVE R TO E
           DISPLAY "LOG10=" E
      *> a native non-integer power is an approximation too: 4 ** 0.5 = 2, so 13.2 + 2 = 15.2
           COMPUTE R = A + 4 ** 0.5
           MOVE R TO E
           DISPLAY "POWER=" E
      *> a negated function value: 13.2 + (-4) = 9.2
           COMPUTE R = A + (-FUNCTION SQRT(16))
           MOVE R TO E
           DISPLAY "NEGATED=" E
      *> two function values in one expression: 13.2 + 4 + 0.2 = 17.4
           COMPUTE R = A + FUNCTION SQRT(16) + FUNCTION SQRT(0.04)
           MOVE R TO E
           DISPLAY "TWO=" E
      *> the receiver-less channel is the same arithmetic: 0.1 + 0.2 = 0.3 EXACTLY, so the
      *> relation is true (binary64 0.1 + 0.2 is 0.30000000000000004).
           IF TENTH + FUNCTION SQRT(0.04) = 0.3
               DISPLAY "RELATION=TRUE"
           ELSE
               DISPLAY "RELATION=FALSE"
           END-IF
      *> the function alone is unchanged: SQRT(16) is exactly 4
           COMPUTE R = FUNCTION SQRT(16)
           MOVE R TO E
           DISPLAY "ALONE=" E
      *> a floating-point ITEM still owns its expression: a COMP-2 operand evaluates the whole
      *> expression in IEEE binary64 (docs/CONFORMANCE.md DOC-A.1-123 - the documented native
      *> determination for an operand described with a float usage), where 0.1 + 0.2 is
      *> 0.30000000000000004 and is NOT equal to the binary64 nearest 0.3.
           IF FL + FUNCTION SQRT(0.04) = 0.3
               DISPLAY "FLOAT-ITEM-RELATION=TRUE"
           ELSE
               DISPLAY "FLOAT-ITEM-RELATION=FALSE"
           END-IF
           STOP RUN.

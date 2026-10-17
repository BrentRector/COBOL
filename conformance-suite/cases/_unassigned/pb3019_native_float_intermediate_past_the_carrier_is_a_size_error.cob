*>--- conformance-case
*> id: pb3019_native_float_intermediate_past_the_carrier_is_a_size_error
*> rules: []
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB3019 and PB3017 - under NATIVE arithmetic a binary64
      *>   operand that the 128-bit integer intermediate cannot hold is the
      *>   SIZE ERROR condition of the intermediate, never a saturated
      *>   sentinel; and DIVIDE ... REMAINDER's subsidiary quotient keeps
      *>   every digit a binary-capacity receiver holds.
      *>
      *>   14.7.5 5): "if native arithmetic is in effect and the implementor
      *>   defines that the range of values allowed for the intermediate
      *>   data item is to be checked, when an arithmetic operation on the
      *>   intermediate data item would cause the size error condition"
      *>   (cite.py --check 14.7.5 "implementor defines that the range of
      *>   values allowed for the intermediate data item is to be checked"
      *>   -> OK 14.7.5 5)); WiseOwl COBOL defines the range checked
      *>   (docs/CONFORMANCE.md DOC-A.1-179).  14.7.5 rule 1 leaves every
      *>   resultant unchanged then (cite.py --check 14.7.5 "remain
      *>   unchanged from the values they had at the start" -> OK 14.7.5
      *>   1)).  F1 = 1.0E+40 and F2 = -1.0E+40 are binary64 values far past
      *>   the intermediate's 1.7E+38; the sentinel the alignment used to
      *>   land (2**127 - 1) is a LEGAL value of a 16-byte COMP-5 item, so
      *>   its quotient by 7 was stored and the statement took NOT ON SIZE
      *>   ERROR.  F3 = 1.0E+10 is the in-range control:
      *>   1E10 / 3 = 3333333333 remainder 1 (3 * 3333333333 = 9999999999).
      *>
      *>   14.9.12.4 6) c): "a subsidiary quotient is developed that is
      *>   signed and derived from the quotient by truncation of digits at
      *>   the least significant end and that has the same number of
      *>   digits and the same decimal point location as the data item
      *>   referenced by identifier-3" (cite.py --check 14.9.12.4 "has the
      *>   same number of digits and the same decimal point location as
      *>   the data item referenced by identifier-3" -> OK 14.9.12.4 6)).
      *>   A binary-capacity item (COMP-5) owns its whole container
      *>   (13.18.60.4 12), "The implementor may allow a wider range",
      *>   cite.py OK; DOC-A.1-123), so the quotient it HOLDS is the full
      *>   quotient and the remainder is dividend - quotient * divisor:
      *>   123456 / 7 = 17636 remainder 4 into PIC 9(4) COMP-5 (a 2-byte
      *>   container, 17636 < 65536), and 9000000000000000000 / 7 =
      *>   1285714285714285714 remainder 2 into PIC 9(18) COMP-5 (an 8-byte
      *>   container, 1285714285714285714 < 2**64).  QQ copies each
      *>   quotient with an arithmetic read, which sees the held value.
      *>   The PICTURE-truncating DISPLAY control keeps the PICTURE's digit
      *>   count (PB129): 12345 / 7 = 1763 remainder 4 fits PIC 9(4).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3019A.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 F1 COMP-2 VALUE 1.0E+40.
       01 F2 COMP-2 VALUE -1.0E+40.
       01 F3 COMP-2 VALUE 1.0E+10.
       01 Q PIC S9(19) COMP-5 VALUE 7.
       01 R PIC S9(19) COMP-5 VALUE 7.
       01 Q4 PIC 9(4) COMP-5 VALUE 0.
       01 Q18 PIC 9(18) COMP-5 VALUE 0.
       01 BIG PIC 9(19) COMP-5 VALUE 9000000000000000000.
       01 QD PIC 9(4) VALUE 0.
       01 QQ PIC 9(19) VALUE 0.
       PROCEDURE DIVISION.
       MAIN.
           DIVIDE F1 BY 7 GIVING Q REMAINDER R
               ON SIZE ERROR DISPLAY "1E40/7 size error"
               NOT ON SIZE ERROR DISPLAY "1E40/7 ok"
           END-DIVIDE.
           DISPLAY "  Q " Q " R " R.
           DIVIDE F2 BY 7 GIVING Q REMAINDER R
               ON SIZE ERROR DISPLAY "-1E40/7 size error"
               NOT ON SIZE ERROR DISPLAY "-1E40/7 ok"
           END-DIVIDE.
           DISPLAY "  Q " Q " R " R.
           DIVIDE F3 BY 3 GIVING Q REMAINDER R
               ON SIZE ERROR DISPLAY "1E10/3 size error"
               NOT ON SIZE ERROR DISPLAY "1E10/3 ok"
           END-DIVIDE.
           DISPLAY "  Q " Q " R " R.
           DIVIDE 123456 BY 7 GIVING Q4 REMAINDER R
               ON SIZE ERROR DISPLAY "123456/7 size error"
               NOT ON SIZE ERROR DISPLAY "123456/7 ok"
           END-DIVIDE.
           COMPUTE QQ = Q4.
           DISPLAY "  QQ " QQ " R " R.
           DIVIDE BIG BY 7 GIVING Q18 REMAINDER R
               ON SIZE ERROR DISPLAY "9E18/7 size error"
               NOT ON SIZE ERROR DISPLAY "9E18/7 ok"
           END-DIVIDE.
           COMPUTE QQ = Q18.
           DISPLAY "  QQ " QQ " R " R.
           DIVIDE 12345 BY 7 GIVING QD REMAINDER R
               ON SIZE ERROR DISPLAY "12345/7 size error"
               NOT ON SIZE ERROR DISPLAY "12345/7 ok"
           END-DIVIDE.
           DISPLAY "  QD " QD " R " R.
           STOP RUN.

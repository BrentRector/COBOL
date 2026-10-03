      *> reject-at: 2023
      *> ISO 8.4.3.2.3 SR12 (cite.py --check 8.4.3.2.3 "An integer function other than the integer form of the ABS
      *> function shall not be specified where an unsigned integer is required" -> OK) at 15.12.3 r1's below-base-11
      *> half: "if the base specified in argument-2 is less than 11, [argument-1] shall also be an unsigned integer
      *> data item or literal" (cite.py --check 15.12.3 OK). Base 10 is below 11 and FUNCTION INTEGER is an integer
      *> function other than ABS, so argument-1 is barred; the same call at base 16 is legal
      *> (conformance:2023/pb1420_abs_integer_form_unsigned_position) because the unsigned-integer half no longer applies.
      *> IT COMPILED CLEAN AND ABORTED AT RUN TIME, kb/Work PB1420.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1420NEG2.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W PIC X(10).
       PROCEDURE DIVISION.
           MOVE FUNCTION BASECONVERT(FUNCTION INTEGER(10) 10 16) TO W
           STOP RUN.

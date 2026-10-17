      *>--- conformance-case
      *> id: l1c01_move_implied_point
      *> rules: [GR-14.6.8.2-3]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §14.6.8.2 r3 — a receiver with no decimal point
      *> specification has its point after its rightmost digit
      *> THE RULE: "If the data description of the receiving data item
      *> does not include an explicit decimal point specification, the
      *> data item is treated as if it had an assumed decimal point
      *> immediately following its rightmost digit."
      *>   cite.py --check 14.6.8.2 "If the data description of the
      *>     receiving data item does not include an explicit decimal
      *>     point specification, the data item is treated as if it had
      *>     an assumed decimal point immediately following its
      *>     rightmost digit" -> OK §14.6.8.2 3)
      *>   cite.py --check 14.6.8.2 "If the receiving operand is a
      *>     fixed-point numeric item, the data is aligned by decimal
      *>     point and is transferred to the receiving digits with zero
      *>     fill or truncation on either end as required"
      *>     -> OK §14.6.8.2 4)
      *> No receiver below has V, '.', or P, so each is an integer
      *> receiver; r4 then aligns on that point: every fraction digit
      *> is truncated, integer digits are right-aligned and zero
      *> filled. A point anywhere else would move the digits (a point
      *> before the first digit would make line A 45600).
      *> DERIVATION of every .out line:
      *>   N5 PIC 9(5) <- 123.456: int 123, fraction cut   -> A [00123]
      *>   N5 PIC 9(5) <- S3 (9(3)V9(3)) = 987.654          -> B [00987]
      *>   N2 PIC 99   <- 0.5: int 0                        -> C [00]
      *>   N3 PIC 999  <- 12345.9: high-order 12 truncated  -> D [345]
      *>   E3 PIC ZZ9 (edited, no point) <- 7.9: int 7, Z Z
      *>      suppress the leading zeros                    -> E [  7]
      *>   E4 PIC 9B99 <- 123.99: 123, B inserts a space    -> F [1 23]
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C01M.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 S3 PIC 9(3)V9(3) VALUE 987.654.
       01 N5 PIC 9(5).
       01 N2 PIC 99.
       01 N3 PIC 999.
       01 E3 PIC ZZ9.
       01 E4 PIC 9B99.
       PROCEDURE DIVISION.
       MAIN.
           MOVE 123.456 TO N5
           DISPLAY "A [" N5 "]"
           MOVE S3 TO N5
           DISPLAY "B [" N5 "]"
           MOVE 0.5 TO N2
           DISPLAY "C [" N2 "]"
           MOVE 12345.9 TO N3
           DISPLAY "D [" N3 "]"
           MOVE 7.9 TO E3
           DISPLAY "E [" E3 "]"
           MOVE 123.99 TO E4
           DISPLAY "F [" E4 "]"
           STOP RUN.

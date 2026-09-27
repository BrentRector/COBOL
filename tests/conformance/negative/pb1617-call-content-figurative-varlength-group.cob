      *> reject-at: 2014 2023
      *> kb/Work PB1617 - a figurative constant BY CONTENT into a
      *> VARIABLE-LENGTH GROUP formal parameter does not conform.
      *> ISO 14.8.2.2: "If either the formal parameter or the argument is
      *> a variable length group, the formal parameter and the argument
      *> shall be compatible, as described in 8.5.1.12", and 8.5.1.12.1:
      *> a variable-length group "may not undergo a comparison or a move
      *> operation, in either direction, explicitly or otherwise, unless
      *> the other operand is a compatible group". A figurative constant
      *> is not a group, so no conforming MOVE (14.8.2.2 rule 2) exists.
      *> cite.py --check 8.5.1.12.1 "may not undergo a comparison or a
      *>   move operation, in either direction, explicitly or otherwise,
      *>   unless the other operand is a compatible group" -> OK 8.5.1.12.1
      *> cite.py --check 14.8.2.2 "If either the formal parameter or the
      *>   argument is a variable length group, the formal parameter and
      *>   the argument shall be compatible" -> OK 14.8.2.2
      *> Before the fix the pair compiled and ALL "*" crossed silently.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1617VM.
       PROCEDURE DIVISION.
           CALL "PB1617VS" AS NESTED USING BY CONTENT ALL "*"
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1617VS.
       DATA DIVISION.
       LINKAGE SECTION.
       01 V.
          05 VN PIC 9.
          05 VT PIC X(2) OCCURS DYNAMIC CAPACITY IN VC.
       PROCEDURE DIVISION USING V.
           DISPLAY "V=[" VN "]"
           GOBACK.
       END PROGRAM PB1617VS.
       END PROGRAM PB1617VM.

      *> ISO 15.4 Returned values - the implementor's maximum length for
      *> a returned value (Annex A.1 item 93; docs/CONFORMANCE.md row
      *> DOC-A.1-93). 15.4: "The evaluation of a function produces a
      *> returned value in a temporary elementary data item. If the
      *> length of the returned value exceeds the maximum length
      *> specified by the implementor for a returned value, an
      *> EC-ARGUMENT-FUNCTION exception condition is set to exist."
      *>
      *> THE DETERMINATION (kb/Work PB2631). The maximum is the
      *> carrier's own ceiling, 1,073,741,791 character positions - the
      *> same number row DOC-A.1-62 documents for a dynamic-length item.
      *> The standard leaves the number to the implementor, so the
      *> owner's rule-1 precedence takes GnuCOBOL's answer, and GnuCOBOL
      *> 3.2 returns a 10,000-position UPPER-CASE, REVERSE, CONCAT and
      *> SUBSTITUTE whole. It is NOT the 8,191-position literal maximum
      *> (8.3.3.4.3 SR1), which bounds what source text can write; a
      *> returned value is sized by run-time data. The over-maximum leg,
      *> where the raise and the zero-length result happen, is in the
      *> sibling golden l1_returned_value_limit_boolean_repro, the one
      *> function whose length a program can name directly without
      *> building the value.
      *>
      *> BASECONVERT is the function whose returned length grows past
      *> its argument: 15.12.4 r1 returns "an integer value expressed in
      *> the base specified by argument-3", and 15.12.3 r3
      *> makes an input legal only while "neither it nor the returned
      *> value would exceed that defined by the implementor for a data
      *> item".
      *> B8191  "7" followed by 2 047 "F" is 3 + 8 188 = 8 191 binary
      *>        digits, all ones: the INSPECT count IS the returned
      *>        length.
      *> B8192  2 048 "F" is 8 192 binary digits, one past the literal
      *>        maximum and far inside the returned-value maximum, so it
      *>        converts whole: 8 192 ones. The receiver is pre-filled
      *>        with "*" first so the count cannot be a leftover.
      *> UC     UPPER-CASE of a 10 000-position item (15.97.4 r5: the
      *>        same length as argument-1 under a one-to-one case
      *>        correspondence) is 10 000 positions.
      *> CC     CONCAT of two 5 000-position items (15.18.4 r1: all of
      *>        the characters) is 10 000 positions.
      *> SB     SUBSTITUTE doubling each of 5 000 "b" (15.87.4 r2) is
      *>        10 000 positions.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1RVLIM.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-OK  PIC X(2048).
       01 W-OVR PIC X(2048).
       01 W-OUT PIC X(8192).
       01 W-CNT PIC 9(5).
       01 W-BIG PIC X(10000) VALUE ALL "a".
       01 W-HLF PIC X(5000) VALUE ALL "b".
       01 W-LEN PIC 9(5).
       PROCEDURE DIVISION.
       MAIN.
           MOVE ALL "F" TO W-OK
           MOVE "7" TO W-OK(1:1)
           MOVE ALL "F" TO W-OVR
           MOVE FUNCTION BASECONVERT(W-OK, 16, 2) TO W-OUT
           MOVE ZERO TO W-CNT
           INSPECT W-OUT TALLYING W-CNT FOR ALL "1"
           DISPLAY "B8191=" W-CNT
           DISPLAY "B8191HEAD=[" W-OUT(1:4) "]"
           MOVE ALL "*" TO W-OUT
           MOVE FUNCTION BASECONVERT(W-OVR, 16, 2) TO W-OUT
           MOVE ZERO TO W-CNT
           INSPECT W-OUT TALLYING W-CNT FOR ALL "1"
           DISPLAY "B8192=" W-CNT
           DISPLAY "B8192TAIL=[" W-OUT(8189:4) "]"
           MOVE FUNCTION LENGTH(FUNCTION UPPER-CASE(W-BIG)) TO W-LEN
           DISPLAY "UC=" W-LEN
           MOVE FUNCTION LENGTH(FUNCTION CONCAT(W-HLF W-HLF)) TO W-LEN
           DISPLAY "CC=" W-LEN
           MOVE FUNCTION LENGTH(
               FUNCTION SUBSTITUTE(W-HLF "b" "bb")) TO W-LEN
           DISPLAY "SB=" W-LEN
           STOP RUN.

      *> ISO/IEC 1989:2023 §14.9.25.4 GR2 and GR3 — THE ZERO-LENGTH-LITERAL SUBSTITUTION (kb/Work PB425).
      *> GR2: "If literal-1 is an alphanumeric or national zero-length literal and the receiving operand is
      *> other than a dynamic-length elementary item, literal-1 is treated as if it were the figurative
      *> constant SPACE."  GR3: the same sentence for "a boolean zero-length literal" and ZERO.
      *> Every expected value below is COMPUTED FROM THE RULE, receiver by receiver:
      *>   GR2 (a plain "" and an N"" sender) is figurative SPACE, sized to the receiver by §8.3.3.6.4 GR2 and
      *>   then EDITED by §14.9.25.4 GR6 where the receiver edits, so PIC XX/XX shows "  /  " — the insertion
      *>   position takes its own character.  Against a BOOLEAN receiver the fill is the boolean character '0'
      *>   (determination D-B2, CONFORMANCE.md §3: Table 17 gives SPACE category boolean there while
      *>   §8.3.3.6.4 GR5 defines no boolean space, and §14.6.8.6 fills a boolean receiver "with zero fill").
      *>   GR3 (a B"" sender) is figurative ZERO, whose alphanumeric character value is '0' (§8.3.3.6.4 GR4;
      *>   Table 17 gives ZERO against an alphanumeric receiving operand the category alphanumeric), so
      *>   PIC X(3) is "000" and PIC XX/XX is "00/00" — NOT spaces.
      *> Each sender reaches only the receiver categories Table 16 (§14.9.25.3 SR10) admits for its own
      *> category: alphanumeric → all seven; national → boolean/national/numeric; boolean →
      *> alphanumeric(-edited)/boolean/national.  The zero-length literal keeps its OWN category for that
      *> screen — GR2/GR3 substitute a VALUE, not a written figurative constant — which is why B"" into a
      *> numeric receiver is a rejection (tests/conformance/negative/pb425-boolean-zero-length-to-numeric).
      *> The boolean and national literal forms and PIC A/1/N receivers are COBOL-2002 additions and the
      *> zero-length literal a COBOL-2014 one (kb/Work PB895, VCR row 7.30), so 2014 is the first edition
      *> that can write this program; the behaviour does not differ by edition above it.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB425-ZERO-LEN-LITERAL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R-ALPHABETIC PIC A(3) VALUE "ABC".
       01 R-ALNUM      PIC X(3) VALUE "???".
       01 R-ALNUM-ED   PIC XX/XX VALUE "AB/CD".
       01 R-BOOL       PIC 1(4) VALUE B"1111".
       01 R-NAT        PIC N(3) VALUE N"ABC".
       01 R-NUM        PIC 9(3) VALUE 123.
       01 R-NUM-ED     PIC ZZ9.
       PROCEDURE DIVISION.
       MAIN.
           MOVE 123 TO R-NUM-ED.
      *> ── GR2, the ALPHANUMERIC zero-length literal, at every Table 16 receiver ──
           MOVE "" TO R-ALPHABETIC.
           MOVE "" TO R-ALNUM.
           MOVE "" TO R-ALNUM-ED.
           MOVE "" TO R-BOOL.
           MOVE "" TO R-NAT.
           MOVE "" TO R-NUM.
           MOVE "" TO R-NUM-ED.
           DISPLAY "A-ALPHABETIC=[" R-ALPHABETIC "]".
           DISPLAY "A-ALNUM=[" R-ALNUM "]".
           DISPLAY "A-ALNUMED=[" R-ALNUM-ED "]".
           DISPLAY "A-BOOL=[" R-BOOL "]".
           DISPLAY "A-NAT=[" R-NAT "]".
           DISPLAY "A-NUM=[" R-NUM "]".
           DISPLAY "A-NUMED=[" R-NUM-ED "]".
      *> ── GR2, the NATIONAL zero-length literal ──
           MOVE B"1111" TO R-BOOL.
           MOVE N"ABC" TO R-NAT.
           MOVE 123 TO R-NUM.
           MOVE N"" TO R-BOOL.
           MOVE N"" TO R-NAT.
           MOVE N"" TO R-NUM.
           DISPLAY "N-BOOL=[" R-BOOL "]".
           DISPLAY "N-NAT=[" R-NAT "]".
           DISPLAY "N-NUM=[" R-NUM "]".
      *> ── GR3, the BOOLEAN zero-length literal ──
           MOVE "???" TO R-ALNUM.
           MOVE "AB/CD" TO R-ALNUM-ED.
           MOVE B"1111" TO R-BOOL.
           MOVE N"ABC" TO R-NAT.
           MOVE B"" TO R-ALNUM.
           MOVE B"" TO R-ALNUM-ED.
           MOVE B"" TO R-BOOL.
           MOVE B"" TO R-NAT.
           DISPLAY "B-ALNUM=[" R-ALNUM "]".
           DISPLAY "B-ALNUMED=[" R-ALNUM-ED "]".
           DISPLAY "B-BOOL=[" R-BOOL "]".
           DISPLAY "B-NAT=[" R-NAT "]".
      *> ── THE EQUIVALENCE GR2 STATES: MOVE "" and MOVE SPACE are ONE statement ──
           MOVE 123 TO R-NUM.
           MOVE "" TO R-NUM.
           MOVE 123 TO R-NUM-ED.
           MOVE SPACE TO R-NUM-ED.
           IF R-NUM = R-NUM-ED
               DISPLAY "SAME-AS-SPACE"
           ELSE
               DISPLAY "DIFFERENT=[" R-NUM "][" R-NUM-ED "]"
           END-IF.
           STOP RUN.

      *> kb/Work PB1907, docs/CONFORMANCE.md D-INS1. ISO 14.9.22.4 GR13
      *> and Annex A.2 item 21 d): what WiseOwl COBOL does when
      *> identifier-1, identifier-3 or identifier-4 of an INSPECT
      *> TALLYING occupies the same storage as a counter (identifier-2).
      *> The standard leaves the result undefined:
      *>   cite.py --check 14.9.22.4 "If identifier-1, identifier-3, or
      *>   identifier-4 occupies the same storage area as identifier-2,
      *>   the result of the execution of this statement is undefined,
      *>   even if they are defined by the same data description entry."
      *>   -> OK 14.9.22.4 13)
      *>   cite.py --check A.2 "occupies the same storage area as the
      *>   TALLYING identifier" -> OK A.2 21) d)
      *>   cite.py --check 4.4 "A COBOL run unit that allows these
      *>   situations to happen is a conforming run unit" -> OK 4.4 2)
      *> So this golden pins a DOCUMENTED IMPLEMENTOR CHOICE, not a
      *> result the standard defines. The choice (GnuCOBOL 3.2,
      *> measured, CLAUDE.md rule 1): each TALLYING operand takes its
      *> count from the statement's state AFTER the operands before it
      *> stored theirs, so the counter's new value is visible to the
      *> image, to a later operand's pattern, and to its BEFORE/AFTER
      *> delimiter. GR11 still ADDS each count to its counter.
      *> A statement whose counters can overlap nothing it reads is
      *> not affected: it stays the one shared comparison cycle of
      *> GR8 (control lines C1 and C2).
      *> EXPECTED:
      *>   D1  G1 = T(0)+"AAAAA", TALLYING T FOR ALL "A": one operand,
      *>       its count (5) is stored after the scan        [5AAAAA]
      *>   D2  G2 = T(0)+"1A1A1", T FOR ALL "A" T FOR ALL "2": the
      *>       first operand counts two "A" and stores T=2; the second
      *>       sees "21A1A1", counts the "2" in T itself -> T=3
      *>                                                    [31A1A1]
      *>   D3  T3 = "1A2B2", C3 = 1, C3 FOR ALL "A" C3 FOR ALL C3:
      *>       one "A" -> C3=2; the pattern C3 is now "2" and matches
      *>       twice -> C3=4                                    [4]
      *>   D4  one operand, the counter is the INSPECT item: C4 = 11,
      *>       two "1" -> 11+2                                 [13]
      *>   D5  T6 = "AB1AB2", C6 = 1, C6 FOR ALL "A" C6 FOR CHARACTERS
      *>       BEFORE C6: two "A" -> C6=3; BEFORE "3" is absent, so
      *>       the region is the whole image and CHARACTERS counts the
      *>       four characters the "A" operand did not consume
      *>       -> 3+4                                           [7]
      *>   RM  reference-modified: G7 = T(0)+"AA", INSPECT G7(1:3)
      *>       TALLYING T FOR ALL "A" T FOR ALL "2": "0AA" -> T=2,
      *>       then "2AA" -> T=3                              [3AA]
      *>   C1  counters in separate records overlap nothing: the ONE
      *>       cycle of GR8 runs, ALL "BC" is tried first at each
      *>       position and fails, ALL "AB" matches at 1 and 4:
      *>       BC=0 AB=2 (ISO 14.9.22.4 GR8 a), b))        [00,02]
      *>   C2  the counter is in a REDEFINES of the INSPECT item's
      *>       record, so the compiler cannot prove it disjoint, but
      *>       it sits over a different part: S2RT is S2T, S2D is
      *>       "AAXA". Re-running the cycle changes nothing:
      *>       3 "A" then 1 "X" -> 4                        [4AAXA]
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1907IT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G1.
          05 G1T PIC 9 VALUE 0.
          05 G1D PIC X(5) VALUE "AAAAA".
       01 G2.
          05 G2T PIC 9 VALUE 0.
          05 G2D PIC X(5) VALUE "1A1A1".
       01 T3   PIC X(5) VALUE "1A2B2".
       01 C3   PIC 9 VALUE 1.
       01 C4   PIC 99 VALUE 11.
       01 T6   PIC X(6) VALUE "AB1AB2".
       01 C6   PIC 9 VALUE 1.
       01 G7.
          05 G7T PIC 9 VALUE 0.
          05 G7D PIC X(2) VALUE "AA".
       01 T8   PIC X(6) VALUE "ABCABC".
       01 C8A  PIC 99 VALUE 0.
       01 C8B  PIC 99 VALUE 0.
       01 S2.
          05 S2T PIC 9 VALUE 0.
          05 S2D PIC X(4) VALUE "AAXA".
       01 S2R REDEFINES S2.
          05 S2RT PIC 9.
          05 S2RD PIC X(4).
       PROCEDURE DIVISION.
       MAIN.
           INSPECT G1 TALLYING G1T FOR ALL "A"
           DISPLAY "D1=[" G1 "]"
           INSPECT G2 TALLYING G2T FOR ALL "A" G2T FOR ALL "2"
           DISPLAY "D2=[" G2 "]"
           INSPECT T3 TALLYING C3 FOR ALL "A" C3 FOR ALL C3
           DISPLAY "D3=[" C3 "]"
           INSPECT C4 TALLYING C4 FOR ALL "1"
           DISPLAY "D4=[" C4 "]"
           INSPECT T6 TALLYING C6 FOR ALL "A" C6 FOR CHARACTERS
                      BEFORE C6
           DISPLAY "D5=[" C6 "]"
           INSPECT G7(1:3) TALLYING G7T FOR ALL "A" G7T FOR ALL "2"
           DISPLAY "RM=[" G7 "]"
           INSPECT T8 TALLYING C8A FOR ALL "BC" C8B FOR ALL "AB"
           DISPLAY "C1=[" C8A "," C8B "]"
           INSPECT S2D TALLYING S2RT FOR ALL "A" S2RT FOR ALL "X"
           DISPLAY "C2=[" S2 "]"
           STOP RUN.

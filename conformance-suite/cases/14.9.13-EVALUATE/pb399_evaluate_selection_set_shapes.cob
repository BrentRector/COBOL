      *>--- conformance-case
      *> id: pb399_evaluate_selection_set_shapes
      *> rules: [SR-14.9.13.3-2, SR-14.9.13.3-4, SR-14.9.13.3-9]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> The EVALUATE selection-set shapes ISO §14.9.13.3 admits, pinned so the SR2 / SR4 / SR9 screens
      *> (kb/Work PB399) reject only what the standard rejects.
      *>
      *> SR2: "The number of selection objects within each set of selection objects shall be equal to the
      *> number of selection subjects."  A SET of selection objects is ONE WHEN phrase, so consecutive WHEN
      *> phrases sharing one imperative-statement are counted separately (leg TWO-PHRASES).
      *> SR7 c): "The word ANY may correspond to a selection subject of any type", and §14.9.13.4 GR4 a) 1.
      *> makes such a pair true without consulting the subject (leg ANY-OK) — ANY still OCCUPIES a position
      *> and is counted.
      *> SR4: "The two operands in a range-expression shall be of the same class and shall not be of class
      *> boolean, message-tag, object, or pointer."  CLASS is §8.5.2.1 Table 2's, so a PIC A pair is a legal
      *> same-class (alphabetic) range (leg ALPHA-RANGE) and a numeric pair a legal numeric one.
      *> SR9: "Neither identifier-3 nor identifier-4 shall reference a variable-length group."  §8.5.1.12.1
      *> defines that term as "a group item whose data description has at least one dynamic-length elementary
      *> item or dynamic-capacity table as a subordinate item" — an OCCURS DEPENDING ON group is NOT one, its
      *> size being its maximum, so leg ODO-RANGE is legal source and shall compile (kb/Work PB399's own
      *> repro program had this premise backwards).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB399SETS.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-A     PIC 9    VALUE 2.
       01 WS-B     PIC X    VALUE "M".
       01 WS-P     PIC A(3) VALUE "BBB".
       01 WS-LOA   PIC A(3) VALUE "AAA".
       01 WS-HIA   PIC A(3) VALUE "CCC".
       01 WS-S     PIC X(3) VALUE "BBB".
       01 WS-N1    PIC 9    VALUE 3.
       01 WS-G1.
          05 WS-G1-C PIC X OCCURS 1 TO 5 DEPENDING ON WS-N1.
       01 WS-G2.
          05 WS-G2-C PIC X OCCURS 1 TO 5 DEPENDING ON WS-N1.
       PROCEDURE DIVISION.
       MAIN.
      *> Two subjects, two objects — SR2 satisfied; GR4 a) ANDs the pairs.
           EVALUATE WS-A ALSO WS-B
               WHEN 2 ALSO "M"
                   DISPLAY "PAIR-BOTH"
               WHEN OTHER
                   DISPLAY "PAIR-NONE"
           END-EVALUATE.
      *> ANY occupies the first position (SR7 c) and pairs true without reading WS-A (GR4 a) 1.).
           EVALUATE WS-A ALSO WS-B
               WHEN ANY ALSO "M"
                   DISPLAY "ANY-OK"
               WHEN OTHER
                   DISPLAY "ANY-NO"
           END-EVALUATE.
      *> Two WHEN phrases, one body: each is its OWN set of selection objects and each has two.
           EVALUATE WS-A ALSO WS-B
               WHEN 9 ALSO "Z"
               WHEN 2 ALSO "M"
                   DISPLAY "TWO-PHRASES"
               WHEN OTHER
                   DISPLAY "NO-PHRASE"
           END-EVALUATE.
      *> A numeric range: §14.7.8 rule 1 — "the range of values includes literal-1, literal-2, and all
      *> algebraic values between", and 2 is between 1 and 3.
           EVALUATE WS-A
               WHEN 1 THRU 3
                   DISPLAY "NUM-RANGE"
               WHEN OTHER
                   DISPLAY "NUM-OUT"
           END-EVALUATE.
      *> A class-ALPHABETIC range (both ends PIC A) against a class-alphabetic subject: SR4's same-class
      *> test is satisfied, and §8.8.4.2.1 then compares them as alphanumeric operands over the program
      *> collating sequence — "AAA" <= "BBB" <= "CCC" in the native sequence.
           EVALUATE WS-P
               WHEN WS-LOA THRU WS-HIA
                   DISPLAY "ALPHA-RANGE"
               WHEN OTHER
                   DISPLAY "ALPHA-OUT"
           END-EVALUATE.
      *> An OCCURS DEPENDING ON group at BOTH ends — a fixed-length group by §8.5.1.12.1, so SR9 does not
      *> reach it.  With WS-N1 = 3 both images are three characters.
           MOVE "AAA" TO WS-G1.
           MOVE "CCC" TO WS-G2.
           EVALUATE WS-S
               WHEN WS-G1 THRU WS-G2
                   DISPLAY "ODO-RANGE"
               WHEN OTHER
                   DISPLAY "ODO-OUT"
           END-EVALUATE.
           STOP RUN.

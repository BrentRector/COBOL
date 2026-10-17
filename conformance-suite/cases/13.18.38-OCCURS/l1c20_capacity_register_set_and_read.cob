      *>--- conformance-case
      *> id: l1c20_capacity_register_set_and_read
      *> rules: [SR-13.18.38.3-32, GR-13.18.38.4-15]
      *> accept: [2014]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §13.18.38.4 GR15 / §13.18.38.3 SR32 — the CAPACITY
      *> register holds the current capacity; SET Format 14 is the one
      *> statement that may name it as a receiving item.
      *>
      *> THE RULES.
      *> §13.18.38.4 GR15: "Data-name-3 defines a numeric data item
      *>   that contains the current capacity of the associated
      *>   table. Data-name-3 shall not be referenced as a receiving
      *>   operand."
      *>   OK  §13.18.38.4 15)  (General rules)
      *> §13.18.38.3 SR32: "Data-name-3 shall not be referenced as a
      *>   receiving item, except as the operand of a variable-table
      *>   format SET statement."
      *>   OK  §13.18.38.3 32)  (Syntax rules)
      *> §14.9.39.3 SR29: SET Format 14 "Data-name-2 shall reference a
      *>   data item defined in the CAPACITY phrase of a
      *>   dynamic-capacity-table format OCCURS clause."
      *>   OK  §14.9.39.3 29)  (Syntax rules)
      *> §14.9.39.4 GR30 a) "If TO is specified, the new capacity is
      *>   specified by integer-1 or arithmetic-expression-4." b) UP:
      *>   "adding integer-1 or the value of arithmetic-expression-4
      *>   to the current capacity" c) DOWN: "subtracting ..."
      *>   OK  §14.9.39.4 30)  (General rules)  [a), b), c)]
      *> §8.5.1.9.1: "The current capacity of a dynamic-capacity table
      *>   may be initialized explicitly in the FROM phrase"
      *>   OK  §8.5.1.9.1 (General), the paragraph after item 3)
      *> §8.5.1.9.3: a receiving reference whose subscript exceeds the
      *>   current capacity: "a new element is automatically created
      *>   and the capacity of the table is increased to the value
      *>   given by the subscript"
      *>   OK  §8.5.1.9.3   (Implicit changes in capacity)
      *> The register is only ever a SENDING operand here (MOVE source,
      *> relation operand) except as SET Format 14's data-name-2.
      *> The negatives l1c20-capacity-register-{move-receiver,
      *> add-receiver,perform-varying} pin SR32's refusal half.
      *>
      *> DERIVATION (expected capacity TO 9 is never exceeded, so no
      *> EC-BOUND-SET; D is PIC 99, so each capacity shows 2 digits).
      *> CAP0=02   FROM 2 initializes the current capacity to 2.
      *> CAP1=04   MOVE to T-E (4): subscript 4 > 2, capacity -> 4.
      *> CAP2=06   SET T-CAP TO 6: GR30 a), new capacity 6.
      *> CAP3=08   SET T-CAP UP BY 2: GR30 b), 6 + 2 = 8.
      *> CAP4=03   SET T-CAP DOWN BY 5: GR30 c), 8 - 5 = 3.
      *> CAP5=05   SET T-CAP TO W + 1 (W = 4): GR30 a) with an
      *>           arithmetic-expression-4, 4 + 1 = 5.
      *> EQ5       the register compares equal to 5 in a relation.
      *> E1=123    element 1 (set first) survives every capacity
      *>           change: only occurrences above the new capacity
      *>           are deleted (§8.5.1.9.4).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C20F.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 D       PIC 99.
       01 W       PIC 9 VALUE 4.
       01 T.
          05 T-E PIC 9(3) OCCURS DYNAMIC CAPACITY IN T-CAP
                 FROM 2 TO 9.
       PROCEDURE DIVISION.
       MAIN.
           MOVE 123 TO T-E (1).
           MOVE T-CAP TO D.
           DISPLAY "CAP0=" D.
           MOVE 7 TO T-E (4).
           MOVE T-CAP TO D.
           DISPLAY "CAP1=" D.
           SET T-CAP TO 6.
           MOVE T-CAP TO D.
           DISPLAY "CAP2=" D.
           SET T-CAP UP BY 2.
           MOVE T-CAP TO D.
           DISPLAY "CAP3=" D.
           SET T-CAP DOWN BY 5.
           MOVE T-CAP TO D.
           DISPLAY "CAP4=" D.
           SET T-CAP TO W + 1.
           MOVE T-CAP TO D.
           DISPLAY "CAP5=" D.
           IF T-CAP = 5
               DISPLAY "EQ5"
           ELSE
               DISPLAY "NE5"
           END-IF.
           DISPLAY "E1=" T-E (1).
           STOP RUN.

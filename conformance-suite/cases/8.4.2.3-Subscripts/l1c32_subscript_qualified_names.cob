      *>--- conformance-case
      *> id: l1c32_subscript_qualified_names
      *> rules: [SR-8.4.2.3.3-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §8.4.2.3.3 SR1 — the subscripted name is an ordinary qualified name (§8.4.2.2)
      *>
      *> THE RULE. §8.4.2.3.3 1): "Qualified-data-name-1 and qualified-condition-name-1 are
      *> defined in 8.4.2.2, Qualification."
      *>   cite.py: OK  §8.4.2.3.3 1)  (Syntax rules)
      *> §8.4.2.3.2 prints the formats as "qualified-data-name-1 [ ( subscript ... ) ]" and
      *> "qualified-condition-name-1 [ ( subscript ... ) ]": the qualifiers (OF/IN) are
      *> written BEFORE the subscripts, and a subscript is an arithmetic expression, whose
      *> identifier may itself be a qualified name. §8.4.2.2 lets a name be qualified by
      *> any higher-level name that makes it unique (CA OF G2 skips the level of E).
      *>   cite.py: OK  §8.4.2.3.2   (General format)
      *>
      *> G1 and G2 both contain a table E (with condition-name CA) and a counter W, so
      *> every unqualified E, CA or W is ambiguous; only the §8.4.2.2 qualification picks
      *> the table and the subscript.
      *>
      *> DERIVATION of every expected line:
      *>   MOVE "ABC1" TO G1, "XYZ2" TO G2: E OF G1 = A,B,C; W OF G1 = 1;
      *>                                    E OF G2 = X,Y,Z; W OF G2 = 2.
      *>   MOVE "Q" TO E OF G1 (W OF G2): subscript 2 -> G1 = "AQC1"
      *>   MOVE "R" TO E IN G2 (W IN G1): subscript 1 -> G2 = "RYZ2"
      *>   line 1: "AQC1 RYZ2"
      *>   S-1 CA OF E OF G1 (1): E OF G1 (1) = "A" -> T
      *>   S-2 CA OF G2 (1): E OF G2 (1) = "R" -> F
      *>   MOVE "A" TO E OF G2 (3); S-3 CA IN G2 (3): "A" -> T
      *>   S-4 CA OF G1 (W OF G2): E OF G1 (2) = "Q" -> F
      *>   line 6: E OF G2 (W OF G1) = E OF G2 (1) = "R"
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C32D.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G1.
          05 E         PIC X OCCURS 3.
             88 CA     VALUE "A".
          05 W         PIC 9.
       01 G2.
          05 E         PIC X OCCURS 3.
             88 CA     VALUE "A".
          05 W         PIC 9.
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE "ABC1" TO G1.
           MOVE "XYZ2" TO G2.
           MOVE "Q" TO E OF G1 (W OF G2).
           MOVE "R" TO E IN G2 (W IN G1).
           DISPLAY G1 " " G2.
           IF CA OF E OF G1 (1) DISPLAY "S-1 T" ELSE DISPLAY "S-1 F".
           IF CA OF G2 (1) DISPLAY "S-2 T" ELSE DISPLAY "S-2 F".
           MOVE "A" TO E OF G2 (3).
           IF CA IN G2 (3) DISPLAY "S-3 T" ELSE DISPLAY "S-3 F".
           IF CA OF G1 (W OF G2) DISPLAY "S-4 T" ELSE DISPLAY "S-4 F".
           DISPLAY E OF G2 (W OF G1).
           STOP RUN.

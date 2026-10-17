*>--- conformance-case
*> id: boolean_ops
*> rules: [GR-8.8.4.2.1-4, GR-8.8.4.3.4-1, SR-14.9.8.3-2, GR-14.9.8.4-3,
*>    FMT-8.8.4.3.2]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §8.7.2 / §8.8.2 / §14.9.8 Format 2 — COBOL-2002 BOOLEAN OPERATORS B-AND / B-OR / B-XOR / B-NOT.
      *> The operators combine boolean ('0'/'1') values positionwise (rule 9 right-zero-extension, rule 10
      *> result = the larger operand). Covers COMPUTE Format 2 — the canonical use, per the spec's Annex D.10
      *> examples — with nesting/precedence via parentheses and a figurative ALL B"…" operand, plus the SIMPLE
      *> boolean condition over a length-1 boolean item (§8.8.4.3). The Annex A Table A.2 oracle:
      *> 1100 B-AND 0101 = 0100, B-OR = 1101, B-XOR = 1001, B-NOT 1100 = 0011.
      *> (The boolean RELATION and B-op condition forms — `IF (a B-AND b) = c` — are staged residue this
      *> increment; the operators work in COMPUTE, which needs no parentheses. DEVLOG 621.)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. BOOLEANOPS.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A       PIC 1(4) VALUE B"1100".
       01 B       PIC 1(4) VALUE B"0101".
       01 R       PIC 1(4).
       01 F       PIC 1 VALUE B"1".
       01 G       PIC 1 VALUE B"0".
       PROCEDURE DIVISION.
       MAIN.
      *> COMPUTE Format 2 — the four operators against the Table A.2 oracle.
           COMPUTE R = A B-AND B.
           DISPLAY "AND=" R.
           COMPUTE R = A B-OR B.
           DISPLAY "OR=" R.
           COMPUTE R = A B-XOR B.
           DISPLAY "XOR=" R.
           COMPUTE R = B-NOT A.
           DISPLAY "NOT=" R.
      *> Nesting + precedence via parentheses: (1100 B-AND 0101)=0100, B-OR 0010 = 0110.
           COMPUTE R = (A B-AND B) B-OR B"0010".
           DISPLAY "NEST=" R.
      *> A figurative ALL B"1" operand materializes to the operand width (§8.3.3.6.4 GR2): 1100 B-AND 1111 = 1100.
           COMPUTE R = A B-AND ALL B"1".
           DISPLAY "ALL=" R.
      *> The boolean RELATION (equality-only, §8.8.4.2.2): a boolean expression compared to a boolean literal.
           IF A B-AND B = B"0100"
               DISPLAY "REL-EQ=YES"
           ELSE
               DISPLAY "REL-EQ=NO".
           IF A B-XOR B = B"1111"
               DISPLAY "REL-NE=YES"
           ELSE
               DISPLAY "REL-NE=NO".
      *> The SIMPLE boolean condition (§8.8.4.3): a length-1 boolean item, a B-op expression, and B-NOT.
           IF F DISPLAY "F-ON" ELSE DISPLAY "F-OFF".
           IF G DISPLAY "G-ON" ELSE DISPLAY "G-OFF".
           IF F B-OR G DISPLAY "FG-OR-ON" ELSE DISPLAY "FG-OR-OFF".
           IF F B-AND G DISPLAY "FG-AND-ON" ELSE DISPLAY "FG-AND-OFF".
           IF B-NOT G DISPLAY "NOT-G-ON" ELSE DISPLAY "NOT-G-OFF".
           STOP RUN.

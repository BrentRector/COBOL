      *>--- conformance-case
      *> id: l1c19_national_literal_quotes_class
      *> rules: [SR-8.3.3.5.3-3, GR-8.3.3.5.4-2]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §8.3.3.5.3 3) — doubled quotation symbol in a national
      *>   literal
      *> Also pins §8.3.3.5.4 2) (class and category national, both
      *>   formats).
      *> SR3: "Two contiguous quotation symbol characters matching the
      *>   quotation
      *> symbol used in the opening delimiter represent a single
      *>   occurrence of
      *> that quotation symbol character in the content of the literal."
      *> OK  §8.3.3.5.3 3)  (Syntax rules)
      *> GR2: "National literals are of the class and category national"
      *> OK  §8.3.3.5.4 2)  (General rules)
      *> 15.26.3 1) DISPLAY-OF: "Argument-1 shall be of class national."
      *> 15.26.4 r1/r4: each national character converts to its
      *>   alphanumeric
      *> character; the length is the number of characters.
      *> Derivation (DISPLAY-OF result and FUNCTION LENGTH, in
      *>   characters):
      *>  Q1 N"A""B": "" matches the opening quote -> one "   A"B  3
      *>  Q2 N'A''B': '' matches the opening apostrophe      A'B  3
      *>  Q3 N'A"B' : " does not match ' - ordinary content  A"B  3
      *>  Q4 N"A''B": '' does not match " - both kept        A''B 4
      *>  Q5 N"""" : one " - content is one quotation mark  "    1
      *>  V1 VALUE N'a''b' in PIC N(3)                       a'b  3
      *>  X1 NX"00410042" is format 2 (4 hex digits per national
      *>     character, docs/CONFORMANCE.md DOC-A.1-122) = N"AB";
      *>     DISPLAY-OF accepts it only because it is class national
      *>     (GR2 applies to ALL FORMATS)                        AB   2
      *>  CAT a national literal MOVEd to a PIC N item (national to
      *>     national elementary move) compares equal: Y
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C19H.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 V1 PIC N(3) VALUE N'a''b'.
       01 WN PIC N(2).
       01 L PIC 9.
       PROCEDURE DIVISION.
           MOVE FUNCTION LENGTH(N"A""B") TO L
           DISPLAY "Q1=" FUNCTION DISPLAY-OF(N"A""B") " " L
           MOVE FUNCTION LENGTH(N'A''B') TO L
           DISPLAY "Q2=" FUNCTION DISPLAY-OF(N'A''B') " " L
           MOVE FUNCTION LENGTH(N'A"B') TO L
           DISPLAY "Q3=" FUNCTION DISPLAY-OF(N'A"B') " " L
           MOVE FUNCTION LENGTH(N"A''B") TO L
           DISPLAY "Q4=" FUNCTION DISPLAY-OF(N"A''B") " " L
           MOVE FUNCTION LENGTH(N"""") TO L
           DISPLAY "Q5=" FUNCTION DISPLAY-OF(N"""") " " L
           MOVE FUNCTION LENGTH(V1) TO L
           DISPLAY "V1=" FUNCTION DISPLAY-OF(V1) " " L
           MOVE FUNCTION LENGTH(NX"00410042") TO L
           DISPLAY "X1=" FUNCTION DISPLAY-OF(NX"00410042") " " L
           MOVE NX"00410042" TO WN
           IF WN = N"AB"
               DISPLAY "CAT=Y"
           ELSE
               DISPLAY "CAT=N"
           END-IF
           STOP RUN.

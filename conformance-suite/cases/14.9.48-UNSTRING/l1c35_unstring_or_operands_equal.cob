      *>--- conformance-case
      *> id: l1c35_unstring_or_operands_equal
      *> rules: [GR-14.9.48.4-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §14.9.48.4 GR1 — rules for DELIMITED BY operands apply to
      *>   OR ones
      *> "All references to identifier-2 and literal-1 apply equally to
      *> identifier-3 and literal-2, respectively, and all recursions
      *>   thereof."
      *> OK  §14.9.48.4 1)  (General rules)
      *> Every delimiter below that matches is an OR operand
      *>   (identifier-3 or
      *> literal-2); the DELIMITED BY operand "#" never occurs in S.
      *>   Rules
      *> stated only for identifier-2/literal-1 must therefore apply to
      *>   them:
      *> OK  §14.9.48.4 7) (ALL) "one occurrence or two or more
      *>   contiguous
      *>     occurrences of literal-1 (figurative constant or not) or
      *>       the
      *>     content of the data item referenced by identifier-2 are
      *>       treated
      *>     as if they were only one occurrence" + one occurrence is
      *>       moved
      *>     to identifier-5 (GR7 second paragraph, GR11d).
      *> OK  §14.9.48.4 9) "all of the characters shall be present in
      *>     contiguous positions of the sending item, and in the order
      *>     given, to be recognized as a delimiter" (identifier-3
      *>       "-+").
      *> OK  §14.9.48.4 11) b) "the examination proceeds left to right
      *>   until
      *>     a delimiter specified by either literal-1 or the value of
      *>       the
      *>     data item referenced by identifier-2 is encountered"
      *> GR7 first paragraph: the figurative constant SPACE (literal-2)
      *> stands for a single-character alphanumeric literal.
      *> Derivation. S = "AB**CD-+EF GH" (13 chars). DELIMITED BY "#"
      *> OR ALL "*" OR W-DL (= "-+") OR SPACE; E1..E4 preset to "@@".
      *> R1=[AB  ] E1=[* ] C1=2  "**" at 3-4 is ONE delimiter (ALL on
      *>     literal-2); one occurrence "*" moved to E1 PIC XX.
      *> R2=[CD  ] E2=[-+] C2=2  identifier-3 "-+" at 7-8, a
      *>   two-character
      *>     delimiter moved whole to E2.
      *> R3=[EF  ] E3=[  ] C3=2  SPACE at 11 is the delimiter; " " moved
      *>   to
      *>     E3 PIC XX with space fill.
      *> R4=[GH  ] E4=[  ] C4=2  "GH" ends at the end of S: E4 space
      *>   filled.
      *> If ALL were ignored on the OR operand, R2 would be space-filled
      *> (two contiguous delimiters, GR8) and "CD" would land in R3.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C35B.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 S    PIC X(13) VALUE "AB**CD-+EF GH".
       01 W-DL PIC XX VALUE "-+".
       01 R1 PIC X(4).
       01 R2 PIC X(4).
       01 R3 PIC X(4).
       01 R4 PIC X(4).
       01 E1 PIC XX VALUE "@@".
       01 E2 PIC XX VALUE "@@".
       01 E3 PIC XX VALUE "@@".
       01 E4 PIC XX VALUE "@@".
       01 C1 PIC 9.
       01 C2 PIC 9.
       01 C3 PIC 9.
       01 C4 PIC 9.
       PROCEDURE DIVISION.
           UNSTRING S DELIMITED BY "#" OR ALL "*" OR W-DL OR SPACE
               INTO R1 DELIMITER IN E1 COUNT IN C1
                    R2 DELIMITER IN E2 COUNT IN C2
                    R3 DELIMITER IN E3 COUNT IN C3
                    R4 DELIMITER IN E4 COUNT IN C4
           END-UNSTRING
           DISPLAY "R1=[" R1 "] E1=[" E1 "] C1=" C1
           DISPLAY "R2=[" R2 "] E2=[" E2 "] C2=" C2
           DISPLAY "R3=[" R3 "] E3=[" E3 "] C3=" C3
           DISPLAY "R4=[" R4 "] E4=[" E4 "] C4=" C4
           STOP RUN.

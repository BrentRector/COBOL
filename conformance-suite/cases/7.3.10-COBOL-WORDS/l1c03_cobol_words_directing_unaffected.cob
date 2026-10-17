      *>--- conformance-case
      *> id: l1c03_cobol_words_directing_unaffected
      *> rules: [GR-7.3.10.4-6]
      *> accept: [2023]
      *> files: [l1c03_cwbook.cpy]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §7.3.10.4 6) — COBOL-WORDS does not reach the compiler
      *> directing statements (COPY, REPLACE) or compiler directives
      *> (>>IF, >>SOURCE)
      *>
      *> "6) A COBOL-WORDS directive does not affect any Compiler
      *> directing statements or Compiler directives."
      *>   cite.py: OK  §7.3.10.4 6)  (General rules)
      *> "The compiler directing statements are the COPY statement and
      *> the REPLACE statement."
      *>   cite.py: OK  §7.2.2.2   (Compiler directing statements)
      *> What UNDEFINE / SUBSTITUTE would otherwise do: "any syntax
      *> requiring the use of the COBOL word that is the content of
      *> literal-3 shall not be available for use in this compilation
      *> group" (UNDEFINE)
      *>   cite.py: OK  §7.3.10.4 3)  (General rules)
      *> "the COBOL word that is the content of literal-5 shall be used
      *> in any syntax where the COBOL word that is the content of
      *> literal-4 is documented as required or optional" (SUBSTITUTE)
      *>   cite.py: OK  §7.3.10.4 4)  (General rules)
      *> >>IF: "If constant-conditional-expression-1 evaluates to TRUE,
      *> all lines of text-1 are included in the resultant text and all
      *> lines of text-2 are omitted from the resultant text."
      *>   cite.py: OK  §7.3.16.4 2)  (General rules)
      *> >>SOURCE: "the source text or library text following the
      *> directive ... shall be treated as fixed form if FIXED is
      *> specified, or as free form if FREE is specified"
      *>   cite.py: OK  §7.3.24.3 1)  (General rules)
      *>
      *> COPY, REPLACE, END-IF and SOURCE are UNDEFINEd and IF is
      *> SUBSTITUTEd by WHENIF, yet GR6 keeps the COPY and REPLACE
      *> statements and the >>IF / >>END-IF / >>SOURCE directives
      *> working. The IF STATEMENT (not a directive) is spelled WHENIF,
      *> which proves the SUBSTITUTE is in force for ordinary syntax.
      *>
      *> DERIVATION of each expected line.
      *>   DISPLAY CP-ITEM   -> CP-ITEM exists only because COPY
      *>                        incorporated l1c03_cwbook.cpy (VALUE
      *>                        "COPIED") -> "COPIED".
      *>   DISPLAY OLDTXT    -> REPLACE turned OLDTXT into the literal
      *>                        "REPLACED" -> "REPLACED".
      *>   >>IF L1C03VAR = 1 -> TRUE (DEFINE ... AS 1), so text-1 is
      *>                        kept and text-2 dropped -> "DIRECTIVE
      *>                        IF TRUE" and no "...FALSE" line.
      *>   WHENIF 1 = 1 ...  -> the IF statement: "STATEMENT IF".
      *>   The procedure division from >>SOURCE FORMAT IS FREE on is
      *>   written in free form starting in column 1; it compiles only
      *>   if that directive took effect.
      *>   "END" closes the run.
       >>COBOL-WORDS UNDEFINE "COPY"
       >>COBOL-WORDS UNDEFINE "REPLACE"
       >>COBOL-WORDS SUBSTITUTE "IF" BY "WHENIF"
       >>COBOL-WORDS UNDEFINE "END-IF"
       >>COBOL-WORDS UNDEFINE "SOURCE"
       >>DEFINE L1C03VAR AS 1
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C03D.
       REPLACE ==OLDTXT== BY =="REPLACED"==.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       COPY "l1c03_cwbook.cpy".
       >>SOURCE FORMAT IS FREE
PROCEDURE DIVISION.
MAIN-P.
    DISPLAY CP-ITEM.
    DISPLAY OLDTXT.
>>IF L1C03VAR = 1
    DISPLAY "DIRECTIVE IF TRUE".
>>ELSE
    DISPLAY "DIRECTIVE IF FALSE".
>>END-IF
    WHENIF 1 = 1 DISPLAY "STATEMENT IF".
    DISPLAY "END".
    STOP RUN.

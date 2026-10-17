      *>--- conformance-case
      *> id: l1c30_national_count_at_native_bound
      *> rules: [SR-12.3.7.3-14]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §12.3.7.3 SR14 c)4 + SR17 c)5 — national count at bound
      *> THE RULES:
      *>   SR14 c)4 (ALPHABET, NATIONAL phrase): "The number of
      *>   characters specified shall not exceed the number of
      *>   characters in the native national character set."
      *>   cite.py --check 12.3.7.3 "The number of characters
      *>     specified shall not exceed the number of characters in
      *>     the native national character set." -> OK §12.3.7.3 14)
      *>   SR17 c)5 (CLASS, NATIONAL phrase): "The number of
      *>   characters specified shall not exceed the number of
      *>   characters in the native national character set or, when
      *>   the IN phrase is specified, the number of characters in the
      *>   character set referenced by alphabet-name-4."
      *>   cite.py --check 12.3.7.3 "the number of characters in the
      *>     character set referenced by alphabet-name-4."
      *>     -> OK §12.3.7.3 17)
      *>   cite.py --check 12.3.7.4 "The characters specified by the
      *>     values of the literals in this clause define the
      *>     exclusive set of characters of which class-name-1
      *>     consists." -> OK §12.3.7.4 12)
      *>   cite.py --check 12.3.7.4 "may specify characters of the
      *>     native character set in either ascending or descending
      *>     sequence" -> OK §12.3.7.4 7) 2.  (cite.py mislabels list
      *>     items, PB1554: the text is GR7 k) 5.)
      *> "Shall not EXCEED" makes EQUAL legal. The native national set
      *> is the 65,536 UTF-16 code units, ordinal n = code unit n-1
      *> (docs/CONFORMANCE.md DOC-A.1-8, §12.3.7.4 GR6). Both clauses
      *> below name EXACTLY 65,536 characters - the bound - so the
      *> source is legal and must compile; a count check off by one
      *> would reject it. The over-bound side cannot be written
      *> without naming a character twice (SR14 a)) or an ordinal out
      *> of range (SR14 c)1 / SR17 c)2), so only the at-bound side of
      *> these rules is observable.
      *>   NREV = 65536 THRU 1: all 65,536 national characters in
      *>          reverse native order (GR7 k) 5. descending range):
      *>          SR14 c)4 at the bound.
      *>   NALL = CLASS ... 1 THRU 65536: SR17 c)5 native arm at the
      *>          bound. (The IN arm at the bound is held as a
      *>          suspected defect: an alphabet naming all 65,536
      *>          characters has no ordinals - see the lane report.)
      *> 2002 dir: the NATIONAL phrase, national literals and
      *> PROGRAM COLLATING SEQUENCE FOR NATIONAL enter at 2002.
      *> DERIVATION of every .out line:
      *>   N"B" (U+0042) < N"A" (U+0041) under the reversed national
      *>   PCS (§12.3.6.4)                  -> NB-LT-NA=Y (native: N)
      *>   N"€A" IS NALL: U+20AC and U+0041 are both among the 65,536
      *>                                    -> EURO-A-IN-NALL=Y
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C30B.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       OBJECT-COMPUTER. X
           PROGRAM COLLATING SEQUENCE FOR NATIONAL IS NREV.
       SPECIAL-NAMES.
           ALPHABET NREV FOR NATIONAL IS 65536 THRU 1
           CLASS NALL FOR NATIONAL IS 1 THRU 65536.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-NA PIC N VALUE N"A".
       01 W-NB PIC N VALUE N"B".
       01 W-NE PIC N(2) VALUE N"€A".
       PROCEDURE DIVISION.
       MAIN.
           IF W-NB < W-NA
               DISPLAY "NB-LT-NA=Y"
           ELSE
               DISPLAY "NB-LT-NA=N"
           END-IF
           IF W-NE IS NALL
               DISPLAY "EURO-A-IN-NALL=Y"
           ELSE
               DISPLAY "EURO-A-IN-NALL=N"
           END-IF
           STOP RUN.

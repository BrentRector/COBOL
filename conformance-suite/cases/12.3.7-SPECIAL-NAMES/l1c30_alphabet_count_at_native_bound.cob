      *>--- conformance-case
      *> id: l1c30_alphabet_count_at_native_bound
      *> rules: [SR-12.3.7.3-14]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §12.3.7.3 SR14 b)4 — alphabet at the native-set count
      *> THE RULE (SR14 b), ALPHANUMERIC specified or implied):
      *>   "4. The number of characters specified shall not exceed the
      *>   number of characters in the native alphanumeric character
      *>     set."
      *>   cite.py --check 12.3.7.3 "The number of characters specified
      *>     shall not exceed the number of characters in the native
      *>     alphanumeric character set." -> OK §12.3.7.3 14)
      *>   cite.py --check 12.3.7.3 "Each numeric literal shall be an
      *>     unsigned integer and shall have a value within the range of
      *>     one through the maximum number of characters in the native
      *>     alphanumeric character set." -> OK §12.3.7.3 14)
      *>   cite.py --check 12.3.7.4 "may specify characters of the
      *>     native
      *>     character set in either ascending or descending sequence"
      *>     -> OK §12.3.7.4 7) 2.  (cite.py mislabels the list item,
      *>       PB1554;
      *>     the text is GR7 k) 5.)
      *> "Shall not EXCEED" makes EQUAL legal. The native alphanumeric
      *>   set
      *> is the 65,536 UTF-16 code units, ordinal n = code unit n-1
      *> (docs/CONFORMANCE.md DOC-A.1-8, §12.3.7.4 GR6), so "65536 THRU
      *>   1"
      *> specifies exactly 65,536 characters - the bound itself. The
      *> source is legal and MUST compile; an off-by-one count check
      *> (">=" for ">") would reject it. The violating side (> 65,536)
      *> cannot be written without naming a character twice, which SR14
      *> a) already forbids, so only the at-bound side is observable.
      *> GR7 k) 5. assigns the range in DESCENDING native order:
      *>   position
      *> p holds native ordinal 65537-p, i.e. the collating sequence is
      *> the exact reverse of the native order.
      *> DERIVATION of every .out line (native codes: "0"=48 "A"=65
      *> "B"=66 "a"=97; under the reversed sequence a higher code
      *> collates LOWER):
      *>   "B" (66) < "A" (65)   -> B-LT-A=Y  (native order: N)
      *>   "a" (97) < "A" (65)   -> LA-LT-A=Y (native order: N)
      *>   "A" (65) < "0" (48)   -> A-LT-0=Y  (native order: N)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C30A.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       OBJECT-COMPUTER. X PROGRAM COLLATING SEQUENCE IS REV.
       SPECIAL-NAMES.
           ALPHABET REV IS 65536 THRU 1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-UA PIC X VALUE "A".
       01 W-UB PIC X VALUE "B".
       01 W-LA PIC X VALUE "a".
       01 W-ZR PIC X VALUE "0".
       PROCEDURE DIVISION.
       MAIN.
           IF W-UB < W-UA
               DISPLAY "B-LT-A=Y"
           ELSE
               DISPLAY "B-LT-A=N".
           IF W-LA < W-UA
               DISPLAY "LA-LT-A=Y"
           ELSE
               DISPLAY "LA-LT-A=N".
           IF W-UA < W-ZR
               DISPLAY "A-LT-0=Y"
           ELSE
               DISPLAY "A-LT-0=N".
           STOP RUN.

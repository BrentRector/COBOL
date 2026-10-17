      *>--- conformance-case
      *> id: l1c30_alphabet_implied_alphanumeric
      *> rules: [SR-12.3.7.3-13]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §12.3.7.3 SR13 — no class phrase implies ALPHANUMERIC
      *>   "13) When the ALPHABET clause is specified with neither the
      *>   ALPHANUMERIC phrase nor the NATIONAL phrase, the
      *>   ALPHANUMERIC phrase is implied."
      *>   cite.py --check 12.3.7.3 "When the ALPHABET clause is
      *>     specified with neither the ALPHANUMERIC phrase nor the
      *>     NATIONAL phrase, the ALPHANUMERIC phrase is implied."
      *>     -> OK §12.3.7.3 13)
      *> The unmarked alphabet AL is used at three sites that each
      *> accept ONLY an alphanumeric alphabet and give it meaning only
      *> as one:
      *>   cite.py --check 12.3.6.3 "Alphabet-name-1 shall reference an
      *>     alphabet that defines an alphanumeric collating sequence."
      *>     -> OK §12.3.6.3 1)  (PCS FOR ALPHANUMERIC IS AL)
      *>   cite.py --check 12.3.7.3 "When the IN phrase is specified,
      *>     alphabet-name-3 shall reference an alphabet that defines
      *>     an alphanumeric character set" -> OK §12.3.7.3 16)
      *>     (SYMBOLIC CHARACTERS FOR ALPHANUMERIC ... IN AL)
      *>   cite.py --check 12.3.7.4 "the value of figurative constant
      *>     symbolic-character-1 is the representation of the coded
      *>     character at ordinal position integer-1"
      *>     -> OK §12.3.7.4 11)
      *>   cite.py --check 12.3.7.4 "The order in which the literals
      *>     appear in the ALPHABET clause specifies, in ascending
      *>     sequence, the ordinal number of the character within the
      *>     collating sequence being specified." -> OK §12.3.7.4 7) 2.
      *> GR7 a) then makes AL's sequence the ALPHANUMERIC one, so it
      *> governs alphanumeric comparisons and NOT national ones (the
      *> national program collating sequence stays native, §12.3.6.4).
      *> 2002 dir: the FOR ALPHANUMERIC / FOR NATIONAL phrases and
      *> national literals enter at 2002.
      *> DERIVATION (AL IS "C" "B" "A": positions C=1 B=2 A=3):
      *>   alphanumeric "C" < "A" under AL        -> AN C-LT-A=Y
      *>   national N"C" < N"A", native order      -> NAT C-LT-A=N
      *>   SC1 = ordinal 1 of AL's coded set = "C" -> SC1=[C]
      *>   SC3 = ordinal 3 of AL's coded set = "A" -> SC3=[A]
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C30E.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       OBJECT-COMPUTER. X
           PROGRAM COLLATING SEQUENCE FOR ALPHANUMERIC IS AL.
       SPECIAL-NAMES.
           ALPHABET AL IS "C" "B" "A"
           SYMBOLIC CHARACTERS FOR ALPHANUMERIC SC1 SC3 ARE 1 3 IN AL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-A  PIC X VALUE "A".
       01 W-C  PIC X VALUE "C".
       01 W-NA PIC N VALUE N"A".
       01 W-NC PIC N VALUE N"C".
       PROCEDURE DIVISION.
       MAIN.
           IF W-C < W-A
               DISPLAY "AN C-LT-A=Y"
           ELSE
               DISPLAY "AN C-LT-A=N"
           END-IF
           IF W-NC < W-NA
               DISPLAY "NAT C-LT-A=Y"
           ELSE
               DISPLAY "NAT C-LT-A=N"
           END-IF
           DISPLAY "SC1=[" SC1 "]"
           DISPLAY "SC3=[" SC3 "]"
           STOP RUN.

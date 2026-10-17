*>--- conformance-case
*> id: pb856_inspect_national_identifier_operands
*> rules: [SR-14.9.22.3-1, SR-14.9.22.3-2]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB856 — ISO 14.9.22.3 SR2: "Identifier-3, ... , identifier-n shall reference an elementary item
      *> described implicitly or explicitly as usage display or national." The NATIONAL half of the rule was never
      *> written: a national identifier operand drew COBOLNET0847 quoting the rule that admits it. Every operand
      *> position reads the one predicate now, and each is exercised with a national identifier (SR4 keeps the
      *> whole operand set national, so identifier-1 is national too). Expected values by 14.9.22.4:
      *>   TALLYING FOR ALL NA over "ABABA"                  -> 3   (GR12 a: one per match)
      *>   TALLYING FOR CHARACTERS BEFORE INITIAL NB         -> 1   (GR8 e, bounded by BEFORE: only "A")
      *>   REPLACING ALL NA BY NB                            -> BBBBB
      *>   CONVERTING NA TO NZ                               -> ZBZBZ
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB856POS.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 NS PIC N(5) VALUE N"ABABA".
       01 NR PIC N(5) VALUE N"ABABA".
       01 NC PIC N(5) VALUE N"ABABA".
       01 NA PIC N(1) VALUE N"A".
       01 NB PIC N(1) VALUE N"B".
       01 NZ PIC N(1) VALUE N"Z".
       01 CT PIC 9(3) VALUE 0.
       01 CB PIC 9(3) VALUE 0.
       PROCEDURE DIVISION.
       MAIN.
           INSPECT NS TALLYING CT FOR ALL NA
           INSPECT NS TALLYING CB FOR CHARACTERS BEFORE INITIAL NB
           INSPECT NR REPLACING ALL NA BY NB
           INSPECT NC CONVERTING NA TO NZ
           DISPLAY "CT=" CT " CB=" CB
           DISPLAY "NR=[" NR "] NC=[" NC "]"
           STOP RUN.

*>--- conformance-case
*> id: l1_fdt_argument1_literal_kinds
*> rules: [AR-15.40.3-1]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §15.40.3 r1 - "Argument-1 shall be a national or alphanumeric
      *> literal." BOTH admitted spellings, and the OBSERVABLE that tells
      *> them apart: §15.40.1's type table makes the FUNCTION's type follow
      *> argument-1's ("Alphanumeric | Alphanumeric", "National | National"),
      *> so an alphanumeric format literal yields a result a PIC X item may
      *> receive and a national one yields a national result.
      *>
      *> ⚠ ONLY ONE OF THE TWO MOVES BELOW DISCRIMINATES, and §14.9.25.3
      *> rule 10's Table 16 says which. Its Alphanumeric row gives National
      *> "Yes", so the NAT- lines would compile even if the result were
      *> mis-typed alphanumeric; its National row gives Alphanumeric "No", so
      *> the AN- lines are the ones a mis-typing breaks. The missing half -
      *> that the NATIONAL literal really does make the function national -
      *> is the negative l1-fdt-national-result-to-alphanumeric, which is
      *> what makes r1's national leg OBSERVABLE rather than merely
      *> accepted. This file's own job is the accept side of both spellings:
      *> both are admitted and both compute the same value.
      *>
      *> The other half of r1 - LITERAL-ness - is the negative corpus:
      *> l1-fdt-argument1-not-a-literal (COBOLNET1517, a PIC X data item) and
      *> l1-fdt-argument1-boolean-literal (COBOLNET1627, a literal that is
      *> neither national nor alphanumeric).
      *>
      *> Derivation of the values (§15.5.2 integer date 1 = 1601-01-01, so
      *> 143951 = 1995-02-15; §15.5.5 45296 seconds past midnight = 12:34:56;
      *> §15.3.3.7 combined = date, T, time, basic with basic and extended
      *> with extended; §15.3.3.1 an extended common time format carries its
      *> two colons in the data, §15.3.1.2 an extended calendar date its two
      *> hyphens).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1FDT02.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R  PIC X(40).
       01 NR PIC N(40).
       01 D  PIC 9(7) VALUE 143951.
       01 S  PIC 9(5) VALUE 45296.
       PROCEDURE DIVISION.
       MAIN.
      *> The ALPHANUMERIC-literal row of §15.40.1's table.
           MOVE FUNCTION FORMATTED-DATETIME("YYYYMMDDThhmmss" D S) TO R
           DISPLAY "AN-BASIC=" R
      *> The NATIONAL-literal row of the same table.
           MOVE FUNCTION FORMATTED-DATETIME(N"YYYYMMDDThhmmss" D S)
               TO NR
           DISPLAY "NAT-BASIC=" NR
      *> Both rows again over an EXTENDED combined format, so the spelling of
      *> the literal cannot be confused with the shape of the format.
           MOVE FUNCTION FORMATTED-DATETIME("YYYY-MM-DDThh:mm:ss" D S)
               TO R
           DISPLAY "AN-EXT=" R
           MOVE FUNCTION FORMATTED-DATETIME(N"YYYY-MM-DDThh:mm:ss" D S)
               TO NR
           DISPLAY "NAT-EXT=" NR
           STOP RUN.

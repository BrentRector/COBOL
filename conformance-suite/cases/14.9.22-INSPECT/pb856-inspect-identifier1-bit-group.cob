*>--- conformance-case
*> id: pb856-inspect-identifier1-bit-group
*> rules: [SR-14.9.22.3-1]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB856 — ISO 14.9.22.3 SR1: "Identifier-1 shall reference either an alphanumeric or national group
*> item or an elementary item described implicitly or explicitly as usage display or national." The group arm
*> names TWO kinds of group item; the binder used to admit ANY group, so a GROUP-USAGE BIT group (13.18.29.4
*> GR1; a bit group item, not an alphanumeric one, by 3.11) passed. SR1 and SR2 now read one shared elementary predicate and SR1's group arm asks
*> the one group-kind classifier. GROUP-USAGE BIT is COBOL-2002, so 85 refuses the clause itself instead.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB856NBG.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G GROUP-USAGE BIT.
          05 B1 PIC 1(4) VALUE B"1010".
       01 CT PIC 9(3) VALUE 0.
       PROCEDURE DIVISION.
       MAIN.
           INSPECT G TALLYING CT FOR ALL B"1"
           STOP RUN.

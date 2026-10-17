*>--- conformance-case
*> id: pb1019-address-of-formal-at-85
*> rules: []
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB1019 - ADDRESS OF (the data-address-identifier, ISO 8.4.3.11) and the data-pointer
      *> SET are COBOL-2002 additions, so below 2002 taking the address of a LINKAGE formal is refused
      *> by the edition gate (COBOLNET0900) - the construct the 2002 golden
      *> pb1019_address_of_linkage_formal exercises exists only where the identifier does.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1019NEG.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 P USAGE POINTER.
       LINKAGE SECTION.
       01 L PIC X(4).
       PROCEDURE DIVISION USING L.
           SET P TO ADDRESS OF L.
           EXIT PROGRAM.

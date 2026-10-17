*>--- conformance-case
*> id: oo-parametric-polymorphism
*> rules: [GR-9.3.5.3-7, GR-9.3.6-L4.1, GR-9.3.6-L4.2, GR-9.3.6-L4.3]
*> source-format: free
*> portable: unverified
*>---
*> ISO 9.3.5.3 "Parametric polymorphism, sometimes called method overloading, allows methods within a class
*> to have the same method name, differing in the number and type of parameters"; rule 7 makes it OPTIONAL
*> and Annex A.4.10 item 3 lists it. COBOL.NET does not claim it: two METHOD-IDs of one name in one class
*> are refused by name with COBOLNET0822.
*> This is the FIRST fixture for 0822 - the diagnostic shipped with no negative at all, and its message
*> cited "ISO 12063", which is not a clause of ISO/IEC 1989:2023 (a stray line anchor;
*> DESIGN-SPEC-RECONCILIATION finding 44). The citation is repaired in the same change set.
       IDENTIFICATION DIVISION.
       CLASS-ID. OVLDCLS.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. SHOW.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L1 PIC 9(4).
       PROCEDURE DIVISION USING L1.
       MAIN.
           DISPLAY "NUM=" L1.
       END METHOD SHOW.
       METHOD-ID. SHOW.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L2 PIC X(4).
       PROCEDURE DIVISION USING L2.
       MAIN.
           DISPLAY "TXT=" L2.
       END METHOD SHOW.
       END OBJECT.
       END CLASS OVLDCLS.

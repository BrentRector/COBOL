*>--- conformance-case
*> id: pb457-capacity-register-under-table
*> rules: [SR-13.18.38.3-30, SR-14.9.39.3-29]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB457 - the CAPACITY register of a dynamic-capacity table that is itself subordinate to a
      *> table. DEFINING the nested table is legal (ISO 8.5.1.9.1 item 3: a dynamic-capacity table "may be
      *> nested in any combination to the same number of levels as a fixed-capacity table"), and naming its
      *> register is legal, but no REFERENCE to that register is writable: ISO 13.18.38.3 SR30 places
      *> data-name-3 "at the same level as the entry containing the OCCURS clause" - inside OUT-E - so ISO
      *> 8.4.2.3.3 SR3/SR5 require one subscript per enclosing OCCURS clause while ISO 13.18.38.3 SR31 forbids
      *> subscripting data-name-3 at all. The definition is accepted; only the reference is refused.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB457-CAP-NEST.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T0.
          05 OUT-E OCCURS 3 TIMES.
             10 IN-E PIC 9(3) OCCURS DYNAMIC CAPACITY IN C2 FROM 1 TO 9.
       PROCEDURE DIVISION.
       MAIN-PARA.
           SET C2 TO 4.
           STOP RUN.

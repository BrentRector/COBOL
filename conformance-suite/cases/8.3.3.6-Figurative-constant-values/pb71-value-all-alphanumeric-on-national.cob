*>--- conformance-case
*> id: pb71-value-all-alphanumeric-on-national
*> rules: [SR-8.3.3.6.3-2]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §13.18.63.3 SR5: the VALUE of a national item shall be a national literal or a figurative constant;
      *> the figurative ALL "AB" carries an ALPHANUMERIC literal-1 (§8.3.3.6.3 SR2 / MOVE §14.9.25.4 GR7 Table 17 -
      *> its category is literal-1's), so it does not seed a national item. kb/Work PB71: the VALUE validator now
      *> classifies literal-1 through the ONE literal-class classifier and keeps this rejection (COBOLNET0898)
      *> while admitting ALL N"…" / NX"…" on the same item.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB71NVALALNUM.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 NV PIC N(3) VALUE ALL "AB".
       PROCEDURE DIVISION.
           STOP RUN.

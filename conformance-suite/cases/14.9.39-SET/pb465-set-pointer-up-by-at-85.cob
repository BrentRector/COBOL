*>--- conformance-case
*> id: pb465-set-pointer-up-by-at-85
*> rules: [GR-14.9.39.4-19]
*> source-format: free
*> portable: unverified
*>---
*> ISO 1989:2023 14.9.39 Format 10 - SET {identifier-9}... {UP|DOWN} BY arithmetic-expression-3 - is a
*> COBOL-2002 introduction, and so are the two general rules that govern it (GR19's EC-SIZE-ADDRESS on an
*> amount that "does not evaluate to an integer" and GR20's EC-RANGE-PTR on a resulting address "outside the
*> range of values allowed by the implementor for a data-pointer data item"). COBOL-85 has no data-pointer
*> category at all, so the whole statement is refused at --std 85 by the pointer-arithmetic-2002 edition gate.
*>
*> kb/Work PB465 - the COMPLEMENT of tests/conformance/2002/pb465_set_pointer_range_ec.cob. That golden pins
*> what the two rules DO at the edition that introduces them; this one pins that they are not available below
*> it, so a repair to GR19/GR20 cannot quietly make the construct compile at 85. The program also trips the
*> USAGE POINTER and SET ADDRESS OF gates - every data-pointer construct is post-85 - so the expected .err
*> names THIS construct's gate text rather than the bare COBOLNET0900 code, which any of the three would
*> satisfy: a case that can pass on a sibling's diagnostic is evidence about the sibling.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB465SETPTR85.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 BUF PIC X(8) VALUE "ABCDEFGH".
       01 P USAGE POINTER.
       PROCEDURE DIVISION.
       MAIN-P.
           SET P TO ADDRESS OF BUF
           SET P UP BY 2
           STOP RUN.

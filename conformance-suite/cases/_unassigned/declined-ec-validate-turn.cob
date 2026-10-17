*>--- conformance-case
*> id: declined-ec-validate-turn
*> rules: []
*> source-format: free
*> portable: unverified
*>---
*> Annex A.4.14 item 10: "EC-VALIDATE exception conditions in ... the TURN compiler directive". Before
*> COBOLNET1710 this compiled CLEAN - ExceptionCatalog carried the whole EC-VALIDATE family with ZERO
*> setting sites, so a program could enable checking for a condition nothing in this implementation can
*> ever set. 14.6.13.1.1 licenses RAISING nothing for an unimplemented optional element; it does not
*> license accepting the NAME.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. DCLECVT.
       PROCEDURE DIVISION.
       >>TURN EC-VALIDATE CHECKING ON
           DISPLAY "UNREACHABLE".
           STOP RUN.

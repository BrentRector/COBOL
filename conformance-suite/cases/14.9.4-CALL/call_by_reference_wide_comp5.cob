*>--- conformance-case
*> id: call_by_reference_wide_comp5
*> rules: [SR-14.9.4.3-5, GR-14.9.4.4-5]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work R12 - a native fixed-point leaf crosses the CALL boundary in a cell of its OWN carrier
      *> type, so a BinaryCapacity item's full container range survives BY REFERENCE aliasing.
      *> Derived from ISO/IEC 1989:2023:
      *>   14.2.3 GR8 - "the activated runtime element operates as if the formal parameter occupies the
      *>                same storage area as the argument": the callee's ADD is visible to the caller, and
      *>                a container-range value (13.18.60.4 GR12; the R10 ownership decision) must cross
      *>                whole. Before R12 the wide/unsigned tiers rode a half-implemented string crossing:
      *>                the generated C# did not compile for BY REFERENCE, and BY CONTENT truncated to the
      *>                picture-digit image.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. R12MAIN.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W18 PIC 9(18) COMP-5.
       01 W19 PIC 9(19) COMP-5.
       01 BIG PIC 9(20).
       PROCEDURE DIVISION.
      *> The 8-byte unsigned container max (2^64-1) survives the round trip untouched.
           MOVE FUNCTION HIGHEST-ALGEBRAIC(W18) TO W18.
           CALL "R12ECHO" USING W18.
           MOVE W18 TO BIG.
           DISPLAY "W18-AFTER=" BIG.
      *> The 16-byte tier: the callee's ADD 1 reaches the caller's storage (GR8 aliasing).
           MOVE 5 TO W19.
           CALL "R12ADD1" USING W19.
           DISPLAY "W19-AFTER=" W19.
           STOP RUN.
       END PROGRAM R12MAIN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. R12ECHO.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L18 PIC 9(18) COMP-5.
       PROCEDURE DIVISION USING L18.
           GOBACK.
       END PROGRAM R12ECHO.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. R12ADD1.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L19 PIC 9(19) COMP-5.
       PROCEDURE DIVISION USING L19.
           ADD 1 TO L19.
           GOBACK.
       END PROGRAM R12ADD1.

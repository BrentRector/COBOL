*>--- conformance-case
*> id: pb485-level-78-working-storage
*> rules: [SR-13.16.3-1, SR-13.18.33.3-5]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 13.18.33.3 SR5: "Data description entries in the
      *> working-storage section, local-storage section, and linkage
      *> section shall have level-numbers 66, 77, 88, or 1 through 49."
      *> 78 is the MicroFocus/GnuCOBOL constant extension; ISO defines no
      *> such level and the conforming spelling is the 13.10 CONSTANT
      *> entry (see conformance/2023/pb485_constant_entry_spelling.cob).
      *> REJECTS AT EVERY EDITION: the SR5 set is identical in 1985, 2002,
      *> 2014 and 2023, so this is not an edition gate, and --permissive
      *> has no arm for it (that mode softens REMOVED constructs only).
      *> Before the screen this bound as a memberless GROUP nested under V
      *> -- 78 exceeds every open level -- compiled clean in the strict
      *> 2023 lane and threw NotImplementedCobolFeatureException at RUN
      *> time on the ref-mod below. 4.2.2 requires the compile-time
      *> indication. kb/Work PB485.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB485N1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  V PIC X(20) VALUE SPACES.
       78  K VALUE 5.
       PROCEDURE DIVISION.
           MOVE "AB" TO V (K:2)
           DISPLAY V
           STOP RUN.

      *>--- conformance-case
      *> id: pb153-allocate-strong-group-unrestricted-returning
      *> rules: [SR-14.9.3.3-5]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO 14.9.3.3 SR5: "If both data-name-1 and data-name-2 are
      *> specified and data-name-1 references a strongly-typed group
      *> item, the data item referenced by data-name-2 shall be
      *> restricted to the type of data-name-1."
      *> THIS COMPILED CLEAN before kb/Work PB153 - the Annex D.9.2.2
      *> type-safety guarantee was silently defeated, because the
      *> strong-type use-restriction network was built out across
      *> MOVE/CALL/ACCEPT/STRING/REDEFINES/RENAMES/intrinsics and the
      *> POINTER subsystem was never wired into it.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB153N1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 TPT TYPEDEF STRONG.
          05 F PIC 9(4).
       01 V TYPE TPT BASED.
       01 P USAGE POINTER.
       PROCEDURE DIVISION.
       MAIN.
           ALLOCATE V RETURNING P.
           STOP RUN.

      *>--- conformance-case
      *> id: pb153-set-address-strong-to-unrestricted
      *> rules: [SR-14.9.39.3-19]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO 14.9.39.3 SR19, second sentence - THE RECEIVER ARM of
      *> SET Format 7: "If data-name-1 is a strongly-typed group item
      *> or a restricted pointer, identifier-6 shall reference a
      *> data-pointer restricted to the type of data-name-1."
      *> 'SET ADDRESS OF based TO ptr' and 'SET ptr TO ADDRESS OF x'
      *> are SEPARATE code paths in PtrBinder; its sibling fixture
      *> pb153-set-unrestricted-to-address-of-strong covers the other,
      *> because a sweep that fixes one arm of a two-arm dispatch is
      *> this repo's most reproducible defect shape.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB153N3.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 TPT TYPEDEF STRONG.
          05 F PIC 9(4).
       01 V TYPE TPT BASED.
       01 Q USAGE POINTER.
       PROCEDURE DIVISION.
       MAIN.
           SET ADDRESS OF V TO Q.
           STOP RUN.

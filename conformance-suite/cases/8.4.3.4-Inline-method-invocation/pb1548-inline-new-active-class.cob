*>--- conformance-case
*> id: pb1548-inline-new-active-class
*> rules: [SR-8.4.3.4.3-4]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §8.4.3.4.3 SR4: "The data item referenced in the RETURNING phrase of the invoked method's procedure
      *> division header shall not be described with the ANY LENGTH clause or with the ACTIVE-CLASS phrase."
      *>                                                            cite.py: OK §8.4.3.4.3 4)
      *> §16.2 gives BaseFactoryInterface's New the header `01 outObject usage object reference active-class.
      *> Procedure division returning outObject.` (cite.py: OK §16.2), so New - although PB1548IC inherits it
      *> from BASE and `INVOKE PB1548IC "New" RETURNING O` is legal - cannot be invoked INLINE
      *> (kb/Work PB1548) -> COBOLNET2140.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1548IM.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB1548IC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W PIC X(4).
       PROCEDURE DIVISION.
       MAIN.
           MOVE PB1548IC :: "New" :: "NAME" TO W
           STOP RUN.
       END PROGRAM PB1548IM.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB1548IC INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. NAME.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LK PIC X(4).
       PROCEDURE DIVISION RETURNING LK.
           MOVE "IC" TO LK.
       END METHOD NAME.
       END OBJECT.
       END CLASS PB1548IC.

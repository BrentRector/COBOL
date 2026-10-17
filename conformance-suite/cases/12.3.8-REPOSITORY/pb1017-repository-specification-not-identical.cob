      *>--- conformance-case
      *> id: pb1017-repository-specification-not-identical
      *> rules: [SR-12.3.8.3-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB1017 - ISO 12.3.8.3 SR1: "If any object-class-name-1,
      *> interface-name-2, program-prototype-name-1,
      *> function-prototype-name-1, intrinsic-function-name-1 or
      *> property-name-1 is specified more than once in the REPOSITORY
      *> paragraph, all the specifications for that name shall be
      *> identical." Three repeated names below, each specified twice
      *> in two DIFFERENT ways that the old per-kind externalized-name
      *> comparison could not see:
      *>   PB1017HB - the same class-specifier with a different EXPANDS
      *>              USING list (two different classes, 12.3.8.4 GR5);
      *>   PB1017IF - once as a CLASS, once as an INTERFACE;
      *>   MAX      - once as an intrinsic-function-name-1, once as a
      *>              user-defined function-prototype-name-1.
      *> Each draws COBOLNET1761. The identical repeat of PB1017BOX is
      *> conforming and draws nothing.
       IDENTIFICATION DIVISION.
       CLASS-ID. PB1017BOX.
       END CLASS PB1017BOX.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB1017BAG.
       END CLASS PB1017BAG.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB1017HOLD USING ELEM.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS ELEM.
       END CLASS PB1017HOLD.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1017NG.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB1017BOX
           CLASS PB1017BOX
           CLASS PB1017BAG
           CLASS PB1017HOLD
           CLASS PB1017HB EXPANDS PB1017HOLD USING PB1017BOX
           CLASS PB1017HB EXPANDS PB1017HOLD USING PB1017BAG
           CLASS PB1017IF
           INTERFACE PB1017IF
           FUNCTION MAX INTRINSIC
           FUNCTION MAX.
       PROCEDURE DIVISION.
           STOP RUN.
       END PROGRAM PB1017NG.

      *>--- conformance-case
      *> id: pb1017_repository_identical_repeats
      *> rules: [SR-12.3.8.3-1]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB1017 - ISO 12.3.8.3 SR1: "If any object-class-name-1,
      *> interface-name-2, program-prototype-name-1,
      *> function-prototype-name-1, intrinsic-function-name-1 or
      *> property-name-1 is specified more than once in the REPOSITORY
      *> paragraph, all the specifications for that name shall be
      *> identical." The rule forbids only a DIFFERENT repeat; an
      *> IDENTICAL one is conforming, whatever the specifier kind and
      *> whatever phrases it carries. Every name below is specified
      *> twice, identically (a spelling in another case is the same
      *> user-defined word, 8.3.2), and the program must compile and
      *> run: the expansion is one class instance (12.3.8.4 GR5), and
      *> the keyword-omitted intrinsic reference MAX(3 5) is 5
      *> (FUNCTION ... INTRINSIC, 12.3.8.4 GR14; MAX).
       IDENTIFICATION DIVISION.
       CLASS-ID. PB1017PBOX.
       END CLASS PB1017PBOX.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB1017PHOLD USING ELEM.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS ELEM.
       END CLASS PB1017PHOLD.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1017OK.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB1017PBOX
           CLASS pb1017pbox
           CLASS PB1017PHOLD
           CLASS PB1017PHB EXPANDS PB1017PHOLD USING PB1017PBOX
           CLASS PB1017PHB EXPANDS PB1017PHOLD USING pb1017pbox
           FUNCTION MAX INTRINSIC
           FUNCTION MAX INTRINSIC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N PIC 9.
       PROCEDURE DIVISION.
           COMPUTE N = MAX(3 5).
           DISPLAY "MAX " N.
           STOP RUN.
       END PROGRAM PB1017OK.

      *> kb/Work PB2554 - the mirror of pb2554_class_comma_driver_period:
      *> the CLASS is compiled WITHOUT DECIMAL-POINT IS COMMA and the
      *> driver is compiled WITH it. ISO 12.3.7.4 GR14 b) makes the
      *> comma the decimal separator only in a source element that
      *> specifies the clause; the formal ZZ9.99 is described by the
      *> class, so its period is the decimal separator no matter how
      *> the driver is configured (12.3.4 GR1: contained units only).
      *> Expected values (derived from the rules, not from a run):
      *>   BY CONTENT 12,5 into ZZ9.99                  -> [ 12.50]
      *>   BY CONTENT 12 into ZZ9.99                    -> [ 12.00]
      *>   BY CONTENT N (PIC 99V99 = 3,07) into ZZ9.99  -> [  3.07]
      *>   BY CONTENT E (PIC ZZ9,99 = 7,25, the driver's comma
      *>     picture) into ZZ9.99                       -> [  7.25]
      *>   the driver's own DISPLAY of a comma literal  -> 12,5
       IDENTIFICATION DIVISION.
       CLASS-ID. PB2554B INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY. CLASS BASE CLASS PB2554B.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       IDENTIFICATION DIVISION.
       METHOD-ID. SHOW.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L PIC ZZ9.99.
       PROCEDURE DIVISION USING L.
           DISPLAY "[" L "]"
           GOBACK.
       END METHOD SHOW.
       END OBJECT.
       END CLASS PB2554B.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2554BR.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           DECIMAL-POINT IS COMMA.
       REPOSITORY. CLASS PB2554B.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 O USAGE OBJECT REFERENCE PB2554B.
       01 N PIC 99V99 VALUE 3,07.
       01 E PIC ZZ9,99.
       PROCEDURE DIVISION.
       MAIN.
           MOVE 7,25 TO E
           INVOKE PB2554B "NEW" RETURNING O
           INVOKE O "SHOW" USING BY CONTENT 12,5
           INVOKE O "SHOW" USING BY CONTENT 12
           INVOKE O "SHOW" USING BY CONTENT N
           INVOKE O "SHOW" USING BY CONTENT E
           DISPLAY 12,5
           STOP RUN.
       END PROGRAM PB2554BR.

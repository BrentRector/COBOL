      *> kb/Work PB2554 - an INVOKE formal's PICTURE is edited under the
      *> DECIMAL-POINT mode of the source element that DESCRIBES it.
      *> ISO 12.3.7.4 GR14 b): "For the basic format of the PICTURE
      *> clause, the character written in character-strings, and
      *> inserted in numeric-edited items to represent the decimal
      *> separator shall be the comma." Here the CLASS is compiled
      *> under DECIMAL-POINT IS COMMA and the driver is not: 12.3.4 GR1
      *> hands the entries to CONTAINED units only, so the formal
      *> ZZ9,99 is edited with the comma as its decimal separator.
      *> Expected values (derived from the rules, not from a run):
      *>   BY CONTENT 12.5 into ZZ9,99                  -> [ 12,50]
      *>   BY CONTENT 12 into ZZ9,99                    -> [ 12,00]
      *>   BY CONTENT N (PIC 99V99 = 3.07) into ZZ9,99  -> [  3,07]
      *>   BY CONTENT E (PIC ZZ9.99 = 7.25, the driver's period
      *>     picture) into ZZ9,99                       -> [  7,25]
      *>   the driver's own DISPLAY of a period literal -> 12.5
       IDENTIFICATION DIVISION.
       CLASS-ID. PB2554A INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           DECIMAL-POINT IS COMMA.
       REPOSITORY. CLASS BASE CLASS PB2554A.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       IDENTIFICATION DIVISION.
       METHOD-ID. SHOW.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L PIC ZZ9,99.
       PROCEDURE DIVISION USING L.
           DISPLAY "[" L "]"
           GOBACK.
       END METHOD SHOW.
       END OBJECT.
       END CLASS PB2554A.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2554AR.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY. CLASS PB2554A.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 O USAGE OBJECT REFERENCE PB2554A.
       01 N PIC 99V99 VALUE 3.07.
       01 E PIC ZZ9.99.
       PROCEDURE DIVISION.
       MAIN.
           MOVE 7.25 TO E
           INVOKE PB2554A "NEW" RETURNING O
           INVOKE O "SHOW" USING BY CONTENT 12.5
           INVOKE O "SHOW" USING BY CONTENT 12
           INVOKE O "SHOW" USING BY CONTENT N
           INVOKE O "SHOW" USING BY CONTENT E
           DISPLAY 12.5
           STOP RUN.
       END PROGRAM PB2554AR.

      *> kb/Work PB2618. ISO 14.9.43.4 GR7: "At the end of execution of the STRING statement, only the portion of the
      *> data item referenced by identifier-3 that was referenced during the execution of the STRING statement is
      *> changed. All other portions of the data item referenced by identifier-3 will contain data that was present
      *> before this execution of the STRING statement." An object property stands in for its data item (8.4.3.9.4),
      *> and what was present before is the property's value: its GET runs before the statement and its SET after, so
      *> the positions STRING did not reach keep their value. They used to be replaced by the receiving temporary's
      *> initial spaces. A reference-modified receiver is the same shape: 8.4.3.3.4 GR5 makes it "a unique data item
      *> that is a subset of the data item referenced by identifier-1", and the rest of the property is not stored.
      *> EXPECTED, derived line by line (NM is PIC X(6) VALUE "ABCDEF"; D and E are two objects of the class):
      *>   1  STRING "Q" ... INTO NM OF D references position 1 only (GR6, GR3 c)): "QBCDEF"; E is untouched
      *>                                                                              "1 [QBCDEF]/[ABCDEF]"
      *>   2  MOVE "XY" TO NM OF D(3:2) stores the two positions of the subset: "QBXYEF"   "2 [QBXYEF]"
      *>   3  STRING "MN" ... INTO NM OF D WITH POINTER P, P = 5, references positions 5 and 6 (GR6): "QBXYMN";
      *>      P is 5 + 2 = 7 at the end                                                   "3 [QBXYMN] P=7"
      *>   4  MOVE "RS" TO NM OF D(2:2) stores item positions 2-3: "QRSYMN"             "4 [QRSYMN]"
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2618P1.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB2618VC
           PROPERTY NM.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 D USAGE OBJECT REFERENCE PB2618VC.
       01 E USAGE OBJECT REFERENCE PB2618VC.
       01 P PIC 9.
       PROCEDURE DIVISION.
       MAIN.
           INVOKE PB2618VC "NEW" RETURNING D
           INVOKE PB2618VC "NEW" RETURNING E
           STRING "Q" DELIMITED BY SIZE INTO NM OF D
           DISPLAY "1 [" NM OF D "]/[" NM OF E "]"
           MOVE "XY" TO NM OF D(3:2)
           DISPLAY "2 [" NM OF D "]"
           MOVE 5 TO P
           STRING "MN" DELIMITED BY SIZE INTO NM OF D WITH POINTER P
           DISPLAY "3 [" NM OF D "] P=" P
           MOVE "RS" TO NM OF D(2:2)
           DISPLAY "4 [" NM OF D "]"
           STOP RUN.
       END PROGRAM PB2618P1.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB2618VC INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE
           CLASS PB2618VC.
       IDENTIFICATION DIVISION.
       OBJECT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 NM PIC X(6) VALUE "ABCDEF" PROPERTY.
       PROCEDURE DIVISION.
       END OBJECT.
       END CLASS PB2618VC.

      *> kb/Work PB1404 - 8.3.2.2: "Except for method-names and property-names, when two or more source elements
      *>   identify something with the same externalized name, they refer to the same instance." Two programs that
      *>   describe the EXTERNAL data item SHR1404 (the same kind of item, one externalized name) are NOT a conflict:
      *>   they share ONE item, which is what the EXTERNAL clause is for (13.18.22). The data item named after the
      *>   AS literal "SHR1404-ALIAS" in the second program is a DIFFERENT externalized name from every program-name.
      *>   The negative twins (a program or a file under the same name as an EXTERNAL data item) are
      *>   conformance/negative/pb1404-*.
      *>   Derived output: the first program stores AB12 and the second, describing the same item, sees it; the
      *>   aliased item is its own item (Q9). Each leg can fail: the pair is refused as a duplicate externalized
      *>   name (COBOLNET2213), or the second program's item is a private copy (SEEN would be spaces).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1404OK.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 SHR1404 PIC X(4) EXTERNAL.
       PROCEDURE DIVISION.
           MOVE "AB12" TO SHR1404
           CALL "RD1404"
           STOP RUN.
       END PROGRAM PB1404OK.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. RD1404.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 SHR1404 PIC X(4) EXTERNAL.
       01 OTHER1404 PIC XX EXTERNAL AS "SHR1404-ALIAS".
       PROCEDURE DIVISION.
           MOVE "Q9" TO OTHER1404
           DISPLAY "SEEN=" SHR1404 " OTHER=" OTHER1404
           GOBACK.
       END PROGRAM RD1404.

*>--- conformance-case
*> id: find_string_argument1_classes
*> rules: [AR-15.37.3-1]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §15.37.3 rule 1 — "Argument-1 shall be a data item or literal of class alphabetic,
      *> alphanumeric, or national." The rule names THREE CLASSES and TWO SPECIES (data item, literal),
      *> and §8.5.2.1 realises each class through more than one kind of item, so this pins the rule's
      *> whole admitted set rather than the one PIC X item every earlier case happens to use.
      *>
      *> §8.5.2.1: "an alphanumeric group item has class and category alphanumeric", and its Table 2
      *> puts categories Alphanumeric, Alphanumeric-edited and "Numeric-edited (if usage is display)"
      *> in class Alphanumeric, and National / National-edited in class National.
      *> §8.4.3.3.4 r6: "The unique data item has the same class, category, and usage as that defined
      *> for identifier-1" — so a reference-modified alphanumeric item is still class alphanumeric.
      *>
      *> NUMED is Table 2's third class-alphanumeric species, category numeric-edited with usage display —
      *> the one that LOOKS numeric, which is exactly the species a class screen gets wrong. It is written
      *> with a numeric-edited argument-2 because that is a pairing §15.37.3 r2 admits ("If argument-1 is of
      *> class alphabetic or alphanumeric, argument-2 shall be a data item or literal of either class
      *> alphabetic or alphanumeric" — Table 2 makes BOTH items class alphanumeric). The OTHER pairing r2
      *> equally admits — the same argument-1 beside an alphanumeric LITERAL — is legal source this compiler
      *> REJECTS (COBOLNET1627). That is a defect of r2's CROSS screen, not of r1's own screen: it belongs to
      *> inventory row AR-15.37.3-2, which today records verdict CONFORMS / state OK, and the repro that
      *> falsifies that reading is repro/find-string-numeric-edited-arg1.cob. r1's admitted set — three
      *> classes × the data-item and literal species, every Table 2 category among them — is complete here.
      *>
      *> Every value below is §15.37.4 r1's first occurrence (r1 + LAST for NATLAST) over the operand
      *> content named in its comment.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1FSARG1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 AA PIC A(9)  VALUE "ABCABCABC".
       01 AX PIC X(9)  VALUE "ABCABCABC".
       01 AE PIC XXBXX VALUE "AB CD".
       01 GRP.
          05 G1 PIC X(4) VALUE "ABCD".
          05 G2 PIC X(4) VALUE "EFGH".
       01 NH PIC N(9) VALUE N"ABCABCABC".
       01 NN PIC N(3) VALUE N"ABC".
       01 EDH PIC ZZZ9.
       01 EDN PIC Z9.
       01 P  PIC 9(4).
       PROCEDURE DIVISION.
       MAIN.
      *> Class ALPHABETIC (§8.5.2.2, Table 2 row 1) — "ABCABCABC" holds "BC" at 2, 5, 8; first = 2.
      *> r2 admits the alphanumeric literal argument-2 beside an alphabetic argument-1.
           MOVE FUNCTION FIND-STRING(AA "BC") TO P.
           DISPLAY "ALPHA=" P.
      *> Class ALPHANUMERIC, category alphanumeric — "CA" at 3 and 6; first = 3.
           MOVE FUNCTION FIND-STRING(AX "CA") TO P.
           DISPLAY "ANUM=" P.
      *> Class ALPHANUMERIC as an alphanumeric GROUP item (§8.5.2.1) — the group's characters are
      *> "ABCDEFGH", so "DE" straddles the G1/G2 boundary and starts at 4.
           MOVE FUNCTION FIND-STRING(GRP "DE") TO P.
           DISPLAY "GROUP=" P.
      *> Class ALPHANUMERIC, category ALPHANUMERIC-EDITED (Table 2) — the item holds "AB CD", so the
      *> three characters "B C" start at 2.
           MOVE FUNCTION FIND-STRING(AE "B C") TO P.
           DISPLAY "ANUMEDIT=" P.
      *> Class ALPHANUMERIC, category NUMERIC-EDITED with usage display (Table 2 row Alphanumeric:
      *> "Numeric-edited (if usage is display)"). MOVE 15 leaves EDH's four character positions "  15"
      *> and EDN's two "15", so §15.37.4 r1's first occurrence of "15" within "  15" is at 3.
           MOVE 15 TO EDH.
           MOVE 15 TO EDN.
           MOVE FUNCTION FIND-STRING(EDH EDN) TO P.
           DISPLAY "NUMED=" P.
      *> Argument-1 as a LITERAL, the species r1 names beside "data item" — "CAB" at 3 and 6; first = 3.
           MOVE FUNCTION FIND-STRING("ABCABCABC" "CAB") TO P.
           DISPLAY "LITALPH=" P.
      *> Class NATIONAL as a data item; r2 then requires a national argument-2. The national character
      *> positions hold "ABCABCABC", so "ABC" is at 1, 4, 7 — first = 1, and LAST (r1) = 7.
           MOVE FUNCTION FIND-STRING(NH NN) TO P.
           DISPLAY "NATITEM=" P.
           MOVE FUNCTION FIND-STRING(NH NN LAST) TO P.
           DISPLAY "NATLAST=" P.
      *> Class NATIONAL as a LITERAL — "BCA" at 2 and 5; first = 2.
           MOVE FUNCTION FIND-STRING(N"ABCABCABC" N"BCA") TO P.
           DISPLAY "LITNAT=" P.
      *> A reference-modified alphanumeric item keeps its class (§8.4.3.3.4 r6) and is still a data
      *> item — AX(4:6) is "ABCABC", in which "CA" starts at 3.
           MOVE FUNCTION FIND-STRING(AX(4:6) "CA") TO P.
           DISPLAY "REFMOD=" P.
           STOP RUN.
       END PROGRAM L1FSARG1.

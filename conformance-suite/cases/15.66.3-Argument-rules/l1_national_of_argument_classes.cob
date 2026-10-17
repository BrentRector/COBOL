*>--- conformance-case
*> id: l1_national_of_argument_classes
*> rules: [AR-15.66.3-1]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §15.66.3 r1 — NATIONAL-OF's ARGUMENT-1 CLASS SCREEN, ON ITS ADMITTED SIDE.
      *> r1 word for word: "Argument-1 shall be of class alphabetic or class alphanumeric."
      *>   python scripts/spec/cite.py --check 15.66.3 "Argument-1 shall be of class alphabetic or class
      *>   alphanumeric."  ->  OK  §15.66.3 1)  (Argument rules)
      *>
      *> ⛔ r1 NAMES TWO CLASSES AND THE CORPUS EXERCISED ONLY ONE. Every NATIONAL-OF call site in
      *> tests/conformance feeds an ALPHANUMERIC operand — a literal (2023/national_of_argument_forms), a PIC X
      *> item (2002/national_intrinsics, 2002/pb59_repertoire_identity) or an alphanumeric group
      *> (2023/pb59_operand_category_total). §8.5.2.2: "An elementary data item described as alphabetic by its
      *> PICTURE character-string is of category alphabetic", and §8.5.2.1's Table 2 pairs category Alphabetic
      *> with class ALPHABETIC — its own row, distinct from Alphanumeric. This compiler's PicCategory folds the
      *> two into one member ("Alphanumeric (X) or alphabetic (A)" is ONE category there), so the ALPHABETIC leg
      *> is precisely where an argument-class screen can over-reject legal source with no fixture noticing.
      *> The rejected side is negative/national-of-wrong-category (class national) and
      *> negative/l1-national-of-numeric-argument1 (class numeric).
      *>
      *> THE VALUES. §15.66.4 r1: "A character string is returned with each alphanumeric character in argument-1
      *> converted to its corresponding national coded character set representation. The implementor defines the
      *> correspondence of characters." COBOL.NET's documented correspondence is the TOTAL UTF-16 IDENTITY
      *> (docs/CONFORMANCE.md, Annex A.1 item 33 / item 188), so 'A' converts to the national 'A' and no
      *> character lacks a correspondent — §15.66.4 r2/r3's substitution never applies here.
      *> §15.66.4 r4: "The length of the returned value is the number of character positions of usage national
      *> required to hold the converted argument and depends on the number of characters contained in
      *> argument-1." A three-character argument-1 therefore returns 3 national character positions, which
      *> §15.50.4 r2 reports as 3.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1NOFCLS.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 L1N-A3 PIC A(3) VALUE "ABC".
       01 L1N-X3 PIC X(3) VALUE "XYZ".
       01 L1N-NR PIC N(3).
       01 L1N-L  PIC 9.
       PROCEDURE DIVISION.
       MAIN.
      *> Class ALPHABETIC — r1's first admitted class, and the leg nothing exercised.
           MOVE FUNCTION NATIONAL-OF(L1N-A3) TO L1N-NR.
           DISPLAY "ALPHABETIC=" L1N-NR.
           MOVE FUNCTION LENGTH(FUNCTION NATIONAL-OF(L1N-A3)) TO L1N-L.
           DISPLAY "ALPHALEN=" L1N-L.
      *> Class ALPHANUMERIC — r1's second admitted class, the control that a shared screen still admits it.
           MOVE FUNCTION NATIONAL-OF(L1N-X3) TO L1N-NR.
           DISPLAY "ALPHANUM=" L1N-NR.
           MOVE FUNCTION LENGTH(FUNCTION NATIONAL-OF(L1N-X3)) TO L1N-L.
           DISPLAY "ANUMLEN=" L1N-L.
           STOP RUN.
       END PROGRAM L1NOFCLS.

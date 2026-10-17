      *>--- conformance-case
      *> id: l1c32_index_name_relation
      *> rules: [GR-8.8.4.2.13-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §8.8.4.2.13 1) — two index-names compare by their occurrence numbers
      *>
      *> THE RULE. §8.8.4.2.13: "Relation tests may be made only between
      *>   1) two index-names. The result is the same as if the corresponding occurrence
      *>      numbers were compared."
      *>   cite.py: OK  §8.8.4.2.13 1)  (Comparisons involving index-names or index data items)
      *>
      *> I1/J1 index T1 (elements of 4 characters), I2 indexes T2 (elements of 8
      *> characters), so an index VALUE that were a displacement would differ from the
      *> occurrence number differently for each table. The rule compares OCCURRENCE
      *> numbers, so the element sizes must not matter.
      *>
      *> DERIVATION of every expected line (T = condition true, F = false):
      *>   SET I1 I2 J1 TO 3 (all at occurrence 3).
      *>   X-1 I1 = I2: 3 = 3 -> T   (displacements 8 and 16 would say F)
      *>   X-2 I1 = J1: 3 = 3 -> T
      *>   SET I2 TO 2 (occurrence 2; displacement 8, the same as I1's at 3).
      *>   X-3 I1 = I2: 3 = 2 -> F   (equal displacements would say T)
      *>   X-4 I1 > I2: 3 > 2 -> T
      *>   X-5 I2 < I1: 2 < 3 -> T
      *>   X-6 I1 NOT < I2: 3 not < 2 -> T
      *>   SET I2 TO 5, SET I1 TO 4.
      *>   X-7 I1 < I2: 4 < 5 -> T   (displacements 12 < 32 agree; checks direction)
      *>   X-8 I2 > I1: 5 > 4 -> T
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C32C.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T1.
          05 E1        PIC X(4) OCCURS 5 INDEXED BY I1 J1.
       01 T2.
          05 E2        PIC X(8) OCCURS 5 INDEXED BY I2.
       PROCEDURE DIVISION.
       MAIN-PARA.
           SET I1 I2 J1 TO 3.
           IF I1 = I2 DISPLAY "X-1 T" ELSE DISPLAY "X-1 F".
           IF I1 = J1 DISPLAY "X-2 T" ELSE DISPLAY "X-2 F".
           SET I2 TO 2.
           IF I1 = I2 DISPLAY "X-3 T" ELSE DISPLAY "X-3 F".
           IF I1 > I2 DISPLAY "X-4 T" ELSE DISPLAY "X-4 F".
           IF I2 < I1 DISPLAY "X-5 T" ELSE DISPLAY "X-5 F".
           IF I1 NOT < I2 DISPLAY "X-6 T" ELSE DISPLAY "X-6 F".
           SET I2 TO 5.
           SET I1 TO 4.
           IF I1 < I2 DISPLAY "X-7 T" ELSE DISPLAY "X-7 F".
           IF I2 > I1 DISPLAY "X-8 T" ELSE DISPLAY "X-8 F".
           STOP RUN.

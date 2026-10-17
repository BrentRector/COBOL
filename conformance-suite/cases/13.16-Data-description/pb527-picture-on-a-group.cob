*>--- conformance-case
*> id: pb527-picture-on-a-group
*> rules: [SR-13.16.3-11, SR-13.18.40.3-1]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 13.18.40.3 SR1, ALL FORMATS: "The PICTURE clause may be specified only at the elementary level."
      *> 8.5.1.3.1 says which entries those are: "The most basic subdivisions of a record, that is, those not
      *> further subdivided, are called elementary items". G is further subdivided, so it is not elementary and
      *> the PICTURE clause on it is illegal source. No edition relaxes the rule - it is 85-era and reads
      *> identically in all four, which is why this fixture rejects at all four.
      *> The shape had NO enforcement site anywhere (kb/Work PB527) and no representation in the bound model
      *> either: DataItem.IsElementary is `Pic is not null` and IsGroup is `Pic is null and Children.Count > 0`,
      *> so an entry with both is NEITHER. Measured before the screen landed: this program compiled clean at
      *> every edition, dropped A from the emitted record struct, and the DISPLAY below crashed the BACKEND with
      *> `error CS1061: 'string' does not contain a definition for 'A'` and a path to a generated .g.cs file -
      *> an internal failure shown to the user in place of a COBOL diagnostic.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB527NEGGROUPPIC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  G  PIC X(3).
           05  A  PIC X.
       PROCEDURE DIVISION.
       MAIN.
           MOVE "QRS" TO G
           DISPLAY A
           STOP RUN.

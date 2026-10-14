## Filed PB3158: an ALIGNED item in a bit group that starts mid-byte aligns to the group, not the record

The clerk filed kb/Work/PB3158 from the "New defects" section of the w1077d PB3156 report (PR #62). An ALIGNED item inside a
GROUP-USAGE BIT group that itself starts mid-byte is aligned from the group's origin instead of a byte boundary of the record
(`BitLayout.Walk`, `IntrinsicBinder` `VariableLengthGroupSum`), so FUNCTION LENGTH answers 31 for 29, a group MOVE throws
`ArgumentOutOfRangeException`, and BYTE-LENGTH stops with "fixed extent is negative". It is a new mechanism: PB3156 fixed the
extent of an ALIGNED table and named this as its lead; no open note owned it. Rule: ISO 13.18.1.4 GR1.

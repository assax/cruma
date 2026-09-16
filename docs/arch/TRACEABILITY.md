# TRACEABILITY.md

Append-only, jedna vazba na radek.

```text
A-1    -> frame.md (cely rozsah odvozen z konceptu)
O-2    -> frame.md#7 kandidat stack (.NET / Blazor)
(greenfield) zadne ANALYSIS-discovered lokace; scope hinty budou tvaru new — C-n
K-1    -> koncept 28 x frame.md#2.1          # vyreseno topologii
K-2    -> koncept 15 x koncept 18 x frame.md#2.1
K-3    -> koncept 10 x koncept 25
K-4    -> koncept 6 x koncept 13 x koncept 22
K-5    -> koncept 11 x koncept 15
K-6    -> koncept 22 x frame.md#2.1
K-7    -> koncept 19 x koncept 17
K-8    -> koncept 23 x frame.md#2.1
K-9    -> koncept 8.2
FR-1   <- koncept 4, 5.2, 31.1
FR-1   assumes: A-3
FR-3   assumes: A-3
FR-6   <- koncept 19, K-2
FR-9   <- koncept 8.2, K-9
FR-12  <- koncept 10, 25, K-3
FR-16  <- koncept 11.4, K-5
FR-21  <- frame.md#7
FR-21  assumes: A-5
FR-23  <- koncept 13, K-4
FR-24  <- koncept 22, K-6, O-9
FR-26  <- koncept 15, frame.md#2.1
FR-28  <- koncept 18, K-2
FR-29  <- frame.md#2.1
FR-30  <- frame.md#2.1, O-7
FR-30  assumes: A-4
FR-33  <- koncept 23, K-8
FR-37  <- O-10
NFR-5  <- K-6, O-9
NFR-7  <- O-11
NFR-7  assumes: A-2
NFR-9  <- koncept 28, frame.md#2.1, K-1
NFR-12 <- koncept 25, O-5
NFR-15 <- koncept 14, frame.md#2.1, frame.md#7
NFR-16 <- koncept 6, K-4
S-1    -> FR-1, NFR-1
S-2    -> NFR-2
S-3    -> FR-7, FR-8, FR-9, FR-10, FR-11
FR-1..FR-5   -> C-1, C-8, C-9, C-13      -> I-1
FR-6         -> C-4, C-8, C-13 (I-1); C-9 (I-2)
FR-7         -> C-3, C-10                -> I-1 (zaklad), I-2 (tabulky, barvy)
FR-8         -> C-9                      -> I-1
FR-9         -> C-3, C-7a, C-7b, C-10    -> I-2
FR-10, FR-11 -> C-3, C-10                -> I-2
FR-12        -> C-3, C-7b, C-13          -> I-2
FR-13..16,25 -> C-15a, C-15b, C-9, C-11  -> I-3
FR-17..21,23 -> C-2, C-8, C-9, C-13      -> I-4
FR-22        -> C-2, C-15a, C-15b, C-9   -> I-4
FR-24        -> C-5, C-8, C-13           -> I-1
FR-26, FR-27 -> C-6, C-8, C-11, C-13     -> I-1
FR-28        -> C-4, C-6, C-9, C-13      -> I-1
FR-29, FR-30 -> C-12, C-13               -> I-1
FR-31, FR-32 -> C-13, C-14, C-11, C-12   -> I-1
FR-33        -> C-11, C-12, C-16         -> I-2
FR-34        -> C-9                      -> I-1
FR-35        -> C-17, C-13               -> I-1
FR-36        -> C-18                     -> I-1
FR-37        -> C-6, C-11, C-13          -> I-1
C-1..C-19    <- D-1..D-9 (plan.md par.1)
C-1    assumes: A-3
C-2    assumes: A-5
C-12   assumes: A-4
NFR-7  -> C-4, C-7a, C-7b, C-8, C-13     # assumes: A-2
(greenfield) scope hinty pro vsechny komponenty: new — C-n
```

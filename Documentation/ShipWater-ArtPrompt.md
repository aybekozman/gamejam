# Kıç köpüğü üretimi

Yerleşik `image_gen` aracı kullanıldı; CLI/API fallback kullanılmadı. Gerçek alpha şeffaflığı talep edildi ve üretimdeki alpha korundu.

Kaynak çıktı: `Assets/Sprites/ShipWaterGenerated/SternFoam_Sheet_v1.png`.
Oyunda kullanılan dört kare: `Assets/Sprites/ShipWaterProcessed/Stern_0.png` … `Stern_3.png`.

## Tam üretim istemi

Use case: stylized-concept. Production game sprite sheet: FOUR animation frames of a SMALL TOP-DOWN SHIP STERN TURBULENCE wake, arranged in an exact 2 by 2 grid on a genuinely transparent background. Each frame isolated in its equal square cell with generous transparent margins. Crisp hand-placed pixel art matching a wooden top-down boat on turquoise sea. Palette only ivory foam, pale aqua and a few muted cyan pixels. The boat travels UP; at the TOP CENTER of each wake a SHORT BROAD FLAT softly scalloped foam attachment touches the stern, then a soft irregular oval stream of broken bubble clusters trails DOWN, widening slightly then dissolving into sparse tiny pixels at the bottom. This is churning stern water, NOT a pointed triangular V-shaped bow wave. No solid white triangle, no sharp upper point, no strong long diagonal arms. Many transparent holes between small foam clusters, restrained contrast and density. Four consecutive subtle bubbling phases with IDENTICAL bounding scale and IDENTICAL attachment point at top center; no shift or zoom. Each wake about twice as tall as wide. Design at low pixel resolution, chunky crisp 3-6 image-pixel blocks, limited 6-color palette, no antialiasing, no blur, no glow, no mist, no dark water rectangle, no shadows, no labels, no lettering, no boat, no ocean background. Deliver a clean transparent RGBA animation sprite sheet suitable for Unity Point filtering.

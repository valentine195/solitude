# SEC-0 visual asset family

First NPC art pass, based on the supplied SEC-0 concept. Generated with built-in
`image_gen`; exact prompts are in `prompts.json`. No NPC gameplay is implemented. The reference scene uses a neutral unlit
material to check source colors independently of room illumination.

## Delivery

- `Assets/_Project/Features/Npcs/Sec0/Sprites`: 72 native transparent frames.
- `Animations`: 12 four-frame movement clips, 12 idle clips, four alert blink
  clips, and two separate four-frame effect clips.
- `PF_Sec0Visual.prefab`: body renderer and Animator, default routine south idle.
- `PF_Sec0BackupFx.prefab`: looping radio signal, pivot at the exclamation origin.
- `PF_Sec0StunFx.prefab`: one-shot pulse, pivot at the emitter; authored facing east.
- `Assets/_Project/Art/SOLITUDE/ReferenceScenes/Sec0ArtValidation.unity`:
  independent art comparison scene; excluded from shipping build settings.
- `work/Reviews/Sec0`: atlas exports, GIF previews, native-scale comparisons,
  and a verified 2× native-pixel Unity camera capture. These review images are outside Unity's asset tree.

## Art contract

32×32 frames; body approximately 27 px high, one 32 px tile footprint.
32 PPU; Point filtering; uncompressed; no mipmaps; hard binary alpha.
Body wheel contact at y=29 (top-down image coordinates), Unity pivot (0.5, 3/32).
Blue routine, amber investigating, red hostile. Side marker lamps remain amber.
North faces away and shows rear grille/status lamps rather than a front sensor.
No baked shadows or ground geometry.

Direction names: south, west, east, north. Movement runs at 8 fps; alert blinks
at 6 fps; radio waves at 6.67 fps; stun pulse at 10 fps. Animators provide named
states for direct `Animator.Play`, without gameplay transitions or AI parameters.
The next behavior pass should explicitly choose states. Layer FX at the antenna
or emitter; rotate the east-facing pulse for the firing direction. Effects remain
separate so the body retains its pixel scale and floor-contact anchor.

## Source and processing

Raw generations are preserved here. Locomotion cells were extracted and centered
on 256×256 canvases without resizing, then processed with a uniform 1/8
nearest-neighbor reduction. Effects were padded to 640×640 cells and reduced
uniformly by 1/20. No fractional scaling or per-frame resizing was used.
The bundled sprite processor performed chroma removal, slicing, alignment and QC.
`prepare_assets.py` locks the limited palette, applies deterministic lamp-state
swaps, aligns wheel/signal anchors, exports clips' frame manifest and GIF previews.
`Sec0ArtBuilder.cs` runs through Unity's live Editor API to configure imports,
create clips/controllers/prefabs and capture the independent reference scene.
Existing asset GUIDs are preserved on reruns.

All source sheets passed strict geometry QC: 16 body frames and four frames per
effect; no empty frames, source/output edge contact or paste clamping.

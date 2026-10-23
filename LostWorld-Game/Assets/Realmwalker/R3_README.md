# R3 — Realmwalker presentation (Jungle)

Play: **Tools > Lost Worlds > R3 > Play Jungle Preview**. Focus Game view. A/D or arrows steer, Space jumps, E activates the endpoint lever. Movement remains R2 at 2.8 m/s. Jump near the bridge takeoff edge. The runner and follow camera end at the transition to Ruins; other realms remain unchanged.

Six native reusable clips: Idle, Run, Takeoff, Airborne, Fall, Land. Humanoid muscle/body curves preserve the authored standing height; three existing cape joints use secondary transform curves. The existing Animator/Avatar and skin assets are retained. Root motion is disabled. The presentation component reads R2 state and crossfades clips; it neither moves the player nor listens for input. Stride rate follows measured forward speed. Pausing or being blocked blends to Idle.

The camera reuses the existing realm camera and URP configuration, using a perspective 58-degree view, offset (0, 3.6, -6.5), 0.13-second smoothing, and a lightweight collision probe. It restores the camera settings when disabled. No shake, head bob, extra camera, listener or package is added.

Validation: **Tools > Lost Worlds > R3 > Validate Jungle Animation and Camera** reuses focused R2 movement checks and verifies R3 clip/state/pose/camera references and actual limb motion. Visual previews are separate from timing-sensitive movement tests. **Apply Animation and Camera** is an authoring tool; it is not needed for already-saved assets and will replace the six generated clips/controller states.

Limits: Unity-authored stylized animation, not motion capture. No foot-placement IK, terrain-adaptive stride, cloth physics or finger/facial animation. Some foot sliding or clothing intersections can occur on uneven ground. The camera collision probe reduces clipping but cannot guarantee clearance at every decorative surface. A human full-course comfort/appearance check is still required; automated Play Mode checks are not human gameplay testing.

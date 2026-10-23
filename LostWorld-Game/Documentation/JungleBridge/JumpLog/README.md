# Bridge fallen log

One static fallen log at world **(0, 2.325, 73)**, beneath `Environment/JungleScenicPolish/WideWoodenBridge/FallenLog_JumpObstacle`.

- 2.4m wide across the center running line, 0.45m high/deep; bottom rests at deck Y=2.10.
- Rounded solid CapsuleCollider, no Rigidbody or gameplay script. Existing rough wood material reused.
- Deck, stream, rails and approaches unchanged. No gaps. Side bypasses remain available; a protagonist following the centerline physically meets the log.
- Existing JUMP input/controller integration is unchanged. This checkout does not contain Person 1's player implementation, so the real jump arc, collision radius and step offset cannot be certified here. A controller with a step offset of 0.45m or greater could step over it; configure/resize with Person 1 rather than changing controller logic in this task.

The existing **Tools > Lost Worlds > Jungle > Validate Scenic Bridge** now validates continuous deck support independently from the intentional obstacle, then checks log dimensions, three center-running-line collision rays, two capsule bypasses and overhead capsule clearance. **Add Bridge Jump Log** is idempotent; the saved scene already contains the log.

Manual: reopen Jungle from disk, frame the named log, run Validate Scenic Bridge, then test approaching and jumping with Person 1's real prefab when available. Confirm tokens at Z=69/77 and the portal remain accessible. No token, interaction, jump logic or hardware changes. No Git push or Windows build.

Validation completed: 65 bridge samples, three centerline collision rays, two safe bypasses and overhead clearance passed. Existing Jungle road/reference checks passed. P13 full journey passed 122 checks with no runtime Console warnings/errors. No compiler errors/warnings were introduced. The actual scene and editor tool match the validated temporary copy by SHA-256. Logs and FINAL_QA.txt are stored alongside this guide.

using UnityEngine;
using LostWorlds.Person2.Hardware;
using LostWorlds.Person2.Integration;
namespace Realmwalker {
 [DisallowMultipleComponent,RequireComponent(typeof(CharacterController))]
 public sealed class RealmwalkerRunner : MonoBehaviour,IAutoRunPauseReceiver {
  [SerializeField,Min(.1f)] float forwardSpeed=3.1f,acceleration=10,laneSpacing=2,lateralSpeed=7,jumpHeight=1.35f,gravity=22;
  [SerializeField,Min(.05f)] float commandInterval=.18f;
  [SerializeField] float centerX=0,fallStopY=-6;
  [SerializeField] bool enableKeyboardSimulator=true,enableHardwareAdapter=true;
  CharacterController motor;Animator animator;IntegrationSession session;
  float speed,vertical,nextDirection;int lane;bool pause,jumpQueued;
  public int Lane=>lane; public bool Grounded {get;private set;} public bool Fallen {get;private set;}
  public bool Paused=>pause||session==null||!session.CommandsReady||session.PlayerReference!=transform||Fallen;
  public float VerticalSpeed=>vertical; public int AcceptedJumps {get;private set;}
  static readonly int SpeedId=Animator.StringToHash("ForwardSpeed"),LateralId=Animator.StringToHash("LateralSpeed"),VerticalId=Animator.StringToHash("VerticalSpeed"),GroundId=Animator.StringToHash("Grounded"),PauseId=Animator.StringToHash("Paused");
  void Awake(){motor=GetComponent<CharacterController>();animator=GetComponent<Animator>();}
  void OnDisable(){if(session!=null){session.PlayerBridge.PlayerCommandRequested-=Receive;if(session.PlayerReference==transform)session.BindPlayer(null);}session=null;speed=0;vertical=0;jumpQueued=false;Grounded=false;}
  void Bind(){var s=IntegrationSession.Instance;if(s==null||s.CurrentRealm==null||s.CurrentRealm.gameObject.scene!=gameObject.scene||s.IsTransitioning)return;session=s;session.PlayerBridge.PlayerCommandRequested+=Receive;session.BindPlayer(transform);var simulator=session.GetComponent<HardwareCommandSimulator>();if(enableKeyboardSimulator&&simulator!=null)simulator.enabled=true;var adapter=session.GetComponent<ProcessedHardwareAdapter>();if(enableHardwareAdapter&&adapter!=null)adapter.enabled=true;}
  public void SetAutoRunPaused(bool value){pause=value;if(value){speed=0;jumpQueued=false;}}
  void Receive(HardwareCommand command){if(Paused)return;if(command==HardwareCommand.JUMP){if(Grounded&&vertical<=0&&!jumpQueued)jumpQueued=true;return;}if(Time.unscaledTime<nextDirection)return;if(command==HardwareCommand.LEFT||command==HardwareCommand.RIGHT){lane=Mathf.Clamp(lane+(command==HardwareCommand.LEFT?-1:1),-1,1);nextDirection=Time.unscaledTime+commandInterval;}}
  /// <summary>Explicit spawn/test placement; never called automatically on a miss. No checkpoint or progression logic.</summary>
  public void ResetMotion(Vector3 position){motor.enabled=false;transform.position=position;motor.enabled=true;lane=Mathf.Clamp(Mathf.RoundToInt((position.x-centerX)/laneSpacing),-1,1);vertical=0;speed=0;jumpQueued=false;Grounded=false;Fallen=false;nextDirection=0;}
  void Update(){if(session==null)Bind();if(session==null)return;if(motor==null||!motor.enabled)return;float dt=Mathf.Min(Time.deltaTime,.05f);if(dt<=0)return;
   if(transform.position.y<fallStopY){Fallen=true;vertical=0;speed=0;jumpQueued=false;}
   bool stopped=Paused;speed=stopped||!session.ForwardRequested?0:Mathf.MoveTowards(speed,forwardSpeed,acceleration*dt);
   if(!Fallen){if(Grounded&&vertical<0)vertical=-2;if(jumpQueued&&!stopped&&Grounded){vertical=Mathf.Sqrt(2*gravity*jumpHeight);Grounded=false;AcceptedJumps++;}jumpQueued=false;vertical-=gravity*dt;
    float target=centerX+lane*laneSpacing;float dx=stopped?0:Mathf.MoveTowards(transform.position.x,target,lateralSpeed*dt)-transform.position.x;
    var before=transform.position;var flags=motor.Move(new Vector3(dx,vertical*dt,speed*dt));Grounded=(flags&CollisionFlags.Below)!=0&&vertical<=0;if((flags&CollisionFlags.Above)!=0&&vertical>0)vertical=0;if(Grounded)vertical=-2;
    if(animator!=null){animator.SetFloat(SpeedId,(transform.position.z-before.z)/dt);animator.SetFloat(LateralId,(transform.position.x-before.x)/dt);}
   }
   if(animator!=null){animator.SetFloat(VerticalId,vertical);animator.SetBool(GroundId,Grounded);animator.SetBool(PauseId,stopped);}
  }
 }
}


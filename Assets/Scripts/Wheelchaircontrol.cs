using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Wheelchaircontrol : MonoBehaviour
{
    public Main Main;
    public Bluetooth Bluetooth;
    public WebSocketClient WebSocketClient;

    //for converting text to floats

    float RightWheelDifference;
    float LeftWheelDifference;

    //finding the difference between target angle and game angle
    float LeftDifference;
    float RightDifference;

    //wheelchair parts
    public GameObject LeftPushWheel;
    public GameObject RightPushWheel;

    public WheelCollider LeftCollider;
    public WheelCollider RightCollider;

    public WheelCollider LeftCastorCollider;
    public WheelCollider RightCastorCollider;

    public GameObject LeftCastor;
    public GameObject RightCastor;

    public GameObject LeftFork;
    public GameObject RightFork;

    public Transform CenterOfGravity;
    private Rigidbody RigidBodyin;

    //wheel calculation parts
    private int PointsPerRotation = 600;
    private float DegreesPerPoint;
    private float RightWheelDifferencedegrees;
    float LeftWheelDifferencedegrees;
    //pid controller Torque
    public float kpTorque;
    public float kiTorque;
    public float kdTorque;
    private float prevErrorTorque;

    //pid controller Brake
    public float kpBrake;
    public float kiBrake;
    public float kdBrake;
    private float prevErrorBrake;

    //PID controllers controlling force for breaking and rotation
    private PIDController LeftPIDController;
    private PIDController RightPIDController;
    private PIDController TorqueForcePIDcontrollerLeft;
    private PIDController TorqueForcePIDcontrollerRight;

    private float PreviousRightWheelValue;
    private float PreviousLeftWheelValue;
    private Quaternion RightWheelRealLife;
    private Quaternion LeftWheelRealLife;

    private Vector3 RightWheelReal;
    private Vector3 LeftWheelReal;
    float RightWheelTargetAngle;
    float LeftWheelTargetAngle;
    private float LeftRotationGame = 0;
    private float RightRotationGame = 0;
    private double SpeedData;

    private int timescalled = 0;

    public bool SpeedLogSwitch = false;



    public float Maximumpushperframe = 1000;


    private float MaxArduinoUInt = 65535;
    private bool FirstInput = true;

    //debug screen settings
    public bool debugMode;
    public GameObject DebugArray;
    public Text DebugRightWheel;
    public Text DebugRightWheelDifference;
    public Text DebugRightRotationGame;
    public Text DebugWheelTargetAngle;
    public Text DebugRightDifference;
    public Text DebugTorque;
    public Text Gamewheeldebug2debug;
    public Text Speed;
    public Text FPS;
    private float Nastygamewheeldebug;

    private double TimeSinceLastSpeedCall;
    private double TimeSinceSpeedCallStart;
    public class PIDController
    {
        [Tooltip("Proportional constant (counters current error)")]
        public float Kp = 0.2f; //proportional constant

        [Tooltip("Integral constant (counters cumulated error)")]
        public float Ki = 0.05f; //integral constant

        [Tooltip("Derivative constant (fights oscillation)")]
        public float Kd = 1f; //derivative constant

        [Tooltip("Current control value")]
        public float value = 0; //control value

        private float lastError;
        private float integral;

        public PIDController(float Kp, float Ki, float Kd)
        {
            this.Kp = Kp;
            this.Ki = Ki;
            this.Kd = Kd;
        }
        public float Update(float error)
        {
            return Update(error, Time.deltaTime);
        }

        public float Update(float error, float dt)
        {
            float derivative = (error - lastError) / dt;
            integral += error * dt;
            lastError = error;

            value = Kp * error + Ki * integral + Kd * derivative;
            return value;
        }
    }
    // Start is called before the first frame update
    void Start()
    {
        //calculates how many degrees are per signal change in the incremental encoder
        DegreesPerPoint = 360f / PointsPerRotation;

        //Sets center of gravity for wheelchair, makes it more stable
        RigidBodyin = GetComponent<Rigidbody>();
        RigidBodyin.centerOfMass = CenterOfGravity.localPosition;

        //PID controller initialisation
        LeftPIDController = new PIDController(kpTorque, kiTorque, kdTorque);
        RightPIDController = new PIDController(kpTorque, kiTorque, kdTorque);
        TorqueForcePIDcontrollerLeft = new PIDController(kpBrake, kiBrake, kdBrake);
        TorqueForcePIDcontrollerRight = new PIDController(kpBrake, kiBrake, kdBrake);
        if(debugMode)
        {
            DebugArray.SetActive(true);
        }
        else
        {
            DebugArray.SetActive(false);
        }



    }

    // Update is called once per frame
    void Update()
    {
        if(debugMode)
        {
            float current = 0;
            current = (int)(1f / Time.unscaledDeltaTime);
            FPS.text = "FPS: " + current.ToString();



            DebugRightWheelDifference.text = "Right wheel difference: " + RightWheelDifference.ToString();
            DebugRightRotationGame.text = "Right wheel rotation (game): " + RightRotationGame.ToString();
            DebugWheelTargetAngle.text = "Right wheel target angle: " + RightWheelTargetAngle.ToString();
            DebugRightDifference.text = "Right wheel difference: " + RightDifference.ToString();
            DebugTorque.text = "Right Wheel Torque: " + RightCollider.motorTorque.ToString();
            if(Input.GetKey("w"))
            {
                RightWheelTargetAngle += 20f;
                LeftWheelTargetAngle += 20f;
            }
            if (Input.GetKey("s"))
            {
                RightWheelTargetAngle -= 20f;
                LeftWheelTargetAngle -= 20f;
            }
            if (Input.GetKey("d"))
            {
                RightWheelTargetAngle += 20f;
                LeftWheelTargetAngle -= 20f;
            }
            if (Input.GetKey("a"))
            {
                RightWheelTargetAngle -= 20f;
                LeftWheelTargetAngle += 20f;
            }
        }

    }

    private void FixedUpdate()
    {
        
        Vector3 pos;
        Quaternion Leftrot = LeftCollider.transform.localRotation;
        LeftCollider.GetWorldPose(out pos, out Leftrot);
        TimeSinceLastSpeedCall += Time.fixedDeltaTime;
        TimeSinceSpeedCallStart += Time.fixedDeltaTime;

        //LeftRotationGame = Leftrot.eulerAngles.x;
        Quaternion Rightrot = RightCollider.transform.localRotation;
        RightCollider.GetWorldPose(out pos, out Rightrot);

        //RightRotationGame = Rightrot.eulerAngles.x;
        


        //step one, get the angle of the wheels from the game, should be between 0 and 359.9999

        if (RightRotationGame + (((RightCollider.rpm * 360) / 60) * Time.deltaTime) > 360) // the wheel has made a full rotation, reset back to zero
        {
            RightRotationGame = RightRotationGame + (((RightCollider.rpm * 360) / 60) * Time.deltaTime);
            RightRotationGame = RightRotationGame - 360;
        }
        else if (RightRotationGame + (((RightCollider.rpm * 360) / 60) * Time.deltaTime) < 0) //the game wheel has gone backward through zero, gotta add 360!
        {
            RightRotationGame = RightRotationGame + (((RightCollider.rpm * 360) / 60) * Time.deltaTime);
            RightRotationGame = RightRotationGame + 360;
        }
        else // otherwise just add to the last game rotation
        {
            RightRotationGame = RightRotationGame + (((RightCollider.rpm * 360) / 60) * Time.deltaTime);
        }
        if(debugMode)
        {
            Nastygamewheeldebug = Nastygamewheeldebug + (((RightCollider.rpm * 360) / 60) * Time.deltaTime);
            Gamewheeldebug2debug.text = Nastygamewheeldebug.ToString();
            
        }


        if (LeftRotationGame + (((LeftCollider.rpm * 360) / 60) * Time.deltaTime) > 360)  // the wheel has made a full rotation, reset back to zero
        {
            LeftRotationGame = LeftRotationGame + (((LeftCollider.rpm * 360) / 60) * Time.deltaTime);
            LeftRotationGame = LeftRotationGame - 360;
        }
        else if (LeftRotationGame + (((LeftCollider.rpm * 360) / 60) * Time.deltaTime) < 0) //the game wheel has gone backward through zero, gotta add 360!
        {
            LeftRotationGame = LeftRotationGame + (((LeftCollider.rpm * 360) / 60) * Time.deltaTime);
            LeftRotationGame = LeftRotationGame + 360;
        }
        else // otherwise just add to the last game rotation
        {
            LeftRotationGame = LeftRotationGame + (((LeftCollider.rpm * 360) / 60) * Time.deltaTime);
        }


        //find the difference between the real world wheel and game wheel

        if (LeftWheelTargetAngle + (360 - LeftRotationGame) < LeftRotationGame + (360 - LeftWheelTargetAngle))
        {
            if (LeftWheelTargetAngle + (360 - LeftRotationGame) < Mathf.Abs(LeftWheelTargetAngle - LeftRotationGame))
            {
                LeftDifference = LeftWheelTargetAngle + (360 - LeftRotationGame);
            }
            else
            {
                if (LeftWheelTargetAngle > LeftRotationGame)
                {
                    LeftDifference = LeftWheelTargetAngle - LeftRotationGame;
                }
                else
                {
                    LeftDifference = (LeftRotationGame - LeftWheelTargetAngle) * -1;
                }
            }
        }
        else
        {
            if (LeftRotationGame + (360 - LeftWheelTargetAngle) < Mathf.Abs(LeftWheelTargetAngle - LeftRotationGame))
            {
                LeftDifference = LeftRotationGame + (360 - LeftWheelTargetAngle) * -1;
            }
            else
            {
                if (LeftWheelTargetAngle > LeftRotationGame)
                {
                    LeftDifference = LeftWheelTargetAngle - LeftRotationGame;
                }
                else
                {
                    LeftDifference = LeftRotationGame - LeftWheelTargetAngle * -1;
                }
            }
        }
        if (RightWheelTargetAngle + (360 - RightRotationGame) < RightRotationGame + (360 - RightWheelTargetAngle))
        {
            if (RightWheelTargetAngle + (360 - RightRotationGame) < Mathf.Abs(RightWheelTargetAngle - RightRotationGame))
            {
                RightDifference = RightWheelTargetAngle + (360 - RightRotationGame);
            }
            else
            {
                if (RightWheelTargetAngle > RightRotationGame)
                {
                    RightDifference = RightWheelTargetAngle - RightRotationGame;
                }
                else
                {
                    RightDifference = (RightRotationGame - RightWheelTargetAngle) * -1;
                }
            }
        }
        else
        {
            if (RightRotationGame + (360 - RightWheelTargetAngle) < Mathf.Abs(RightWheelTargetAngle - RightRotationGame))
            {
                RightDifference = RightRotationGame + (360 - RightWheelTargetAngle) * -1;
            }
            else
            {
                if (RightWheelTargetAngle > RightRotationGame)
                {
                    RightDifference = RightWheelTargetAngle - RightRotationGame;
                }
                else
                {
                    RightDifference = RightRotationGame - RightWheelTargetAngle * -1;
                }
            }
        }
        
        float Leftcollideractualtorque;
        float RightCollideractualtorque;

        float trynewLeftDifference = Mathf.DeltaAngle(LeftRotationGame, LeftWheelTargetAngle);
        float trynewRightDifference = Mathf.DeltaAngle(RightRotationGame, RightWheelTargetAngle);

        float LeftTurnTorque = LeftPIDController.Update(trynewLeftDifference);
        float LeftspinTorque = TorqueForcePIDcontrollerLeft.Update(LeftCollider.rpm);
        Leftcollideractualtorque = LeftTurnTorque + LeftspinTorque;

        float RightTurnTorque = RightPIDController.Update(trynewRightDifference);
        float RightSpinTorque = TorqueForcePIDcontrollerRight.Update(RightCollider.rpm);
        RightCollideractualtorque = RightTurnTorque + RightSpinTorque;

        LeftCollider.motorTorque = Mathf.Clamp(Leftcollideractualtorque, -50000f, 50000f);
        RightCollider.motorTorque = Mathf.Clamp(RightCollideractualtorque, -50000f, 50000f);


        if (LeftWheelTargetAngle != 0)
        {
            timescalled++;
        }
        //enable the below line for debug logging
        //timescalled++;
        //Debug.Log("Right game: " + RightRotationGame + " RightActual: " + RightWheelTargetAngle + " Right power: " + RightCollideractualtorque + " RPM: " + RightCollider.rpm);
        
        //time to update the visual wheels
        Quaternion temprot;
        Vector3 temppos;

        LeftCollider.GetWorldPose(out temppos, out temprot);

        LeftPushWheel.transform.rotation = temprot;

        RightCollider.GetWorldPose(out temppos, out temprot);

        RightPushWheel.transform.rotation = temprot;

    }
    public void WheelchairMovement(float LeftWheel, float RightWheel)
    {
        if(debugMode) { DebugRightWheel.text = RightWheel.ToString(); }
        if (FirstInput)
        {
            PreviousLeftWheelValue = LeftWheel;
            PreviousRightWheelValue = RightWheel;
            FirstInput = false;
        }

        //try to get rid of random numbers.
        RightWheelDifference = RightWheel - PreviousRightWheelValue;
        LeftWheelDifference = LeftWheel - PreviousLeftWheelValue;

        //time to sort out numbers going past the Uint limit and going from 0 to 65535 and back
        if (RightWheelDifference > Maximumpushperframe)
        {
            RightWheelDifference -= MaxArduinoUInt;
        }
        else if (RightWheelDifference < (Maximumpushperframe * -1))
        {
            RightWheelDifference += MaxArduinoUInt;
        }

        if (LeftWheelDifference > Maximumpushperframe)
        {
            LeftWheelDifference -= MaxArduinoUInt;
        }
        else if (LeftWheelDifference < (Maximumpushperframe * -1))
        {
            LeftWheelDifference += MaxArduinoUInt;
        }
        if (RightWheelDifference > 80 || RightWheelDifference < -80 ||
             LeftWheelDifference > 80 || LeftWheelDifference < -80)
        {
            Main.BluetoothMessage("Left: " + LeftWheelDifference + " Right: " + RightWheelDifference);
            RightWheelDifference = PreviousRightWheelValue;
            LeftWheelDifference = PreviousLeftWheelValue;
            FirstInput = true;
            return;
        }
        SpeedData += (RightWheelDifference + LeftWheelDifference) / 2;
        if (SpeedLogSwitch && TimeSinceLastSpeedCall > 2) //send out speed data via webosckets
        {
            Debug.Log("entered speedlogswitch town, and 2 seconds have passed!");
            
            //step 2, multiply the number by the distance per pulse we worked out

            double DistanceTraveledmm = SpeedData * 0.17104226669; //mm


            double milimeterspersecond = DistanceTraveledmm / TimeSinceLastSpeedCall;

            double MilesPerHour = milimeterspersecond * 0.00223694;

            Debug.Log("Miles per hour calculated at:" + MilesPerHour.ToString() + "MPH");

            //WebSocketClient.SendMessageToSocket("messagePair " + MilesPerHour.ToString() + "MPH. " + TimeSinceSpeedCallStart + " seconds ellapsed");
            Speed.text = SpeedData.ToString().Substring(0, 4) + "P. " + TimeSinceLastSpeedCall.ToString().Substring(0, 4) + "S. " + milimeterspersecond.ToString().Substring(0, 4) + "MM/S. " + MilesPerHour.ToString().Substring(0, 4) + "MPH";
            TimeSinceLastSpeedCall = 0;
            SpeedData = 0;
        }
        //TimeSinceLastSpeedCall = 0; //we don't the value to be crazy high the first time Speedlogswitch is called, so we reset it every time wheelchairmovement is called regardless of whether or not speed is being logged
        RightWheelDifferencedegrees = RightWheelDifference * DegreesPerPoint;
        LeftWheelDifferencedegrees = LeftWheelDifference * DegreesPerPoint;

        //current idea, use diameter of irl wheel and game wheel to find how to you would travelspinning the wheel a set angle,
        //find the difference 

        if (RightWheelTargetAngle + RightWheelDifferencedegrees > 360f) // over a full rotation, reset to 0
        {
            RightWheelTargetAngle = RightWheelTargetAngle - 360f;
        }
        else if (RightWheelTargetAngle + RightWheelDifferencedegrees < 0) //gone back a rotation, add 360
        {
            RightWheelTargetAngle = RightWheelTargetAngle + 360f;
        }

        if (LeftWheelTargetAngle + LeftWheelDifferencedegrees > 360f) // over a full rotation, reset to 0
        {
            LeftWheelTargetAngle = LeftWheelTargetAngle - 360f;
        }
        else if (LeftWheelTargetAngle + LeftWheelDifferencedegrees < 0) //gone back a rotation, add 360
        {
            LeftWheelTargetAngle = LeftWheelTargetAngle + 360f;
        }
        RightWheelTargetAngle = RightWheelTargetAngle + RightWheelDifferencedegrees;
        LeftWheelTargetAngle = LeftWheelTargetAngle + LeftWheelDifferencedegrees;

        //end of function, set new Previous values and reset timesinceLastCall
        PreviousRightWheelValue = RightWheel;
        PreviousLeftWheelValue = LeftWheel;



    }
    public void ResetWheelChair()
    {

        FirstInput = true;
        LeftRotationGame = 0;
        RightRotationGame = 0;
        LeftWheelTargetAngle = 0;
        RightWheelTargetAngle = 0;
        LeftPIDController = new PIDController(kpTorque, kiTorque, kdTorque);
        RightPIDController = new PIDController(kpTorque, kiTorque, kdTorque);
        TorqueForcePIDcontrollerLeft = new PIDController(kpBrake, kiBrake, kdBrake);
        TorqueForcePIDcontrollerRight = new PIDController(kpBrake, kiBrake, kdBrake);
    }
    private void OnCollisionEnter(Collision collision)
    {
        //string material;
        if (collision.collider.material.name == "Bad (Instance)")
        {
            //GetComponent<AudioSource>().Play();
            //AgilityTest.GetComponent<AgilityTest>().BadCollision(); //tell the Agility test we have collided with a bad object!
        }

    }
    private void OnCollisionStay(Collision collision) // while you're touching an object, maximum resistance!
    {
        //UIControl.GetComponent<UIControl>().WallCollisionResistance();

        //LA 100 set resistance 100
        //RA 100 set right resistance 100
        Bluetooth.send("LA100");
        Bluetooth.send("RA100");
        Main.BluetoothMessage("HIT!");
        
    }
    private void OnCollisionExit(Collision collision) //you've gone off the object? back to normal
    {
        //UIControl.GetComponent<UIControl>().SetResistance();
        Bluetooth.send("LA0");
        Bluetooth.send("RA0");
        Main.BluetoothMessage("UNHIT!");
    }
}

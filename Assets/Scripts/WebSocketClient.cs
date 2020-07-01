using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using WebSocketSharp;


//client
public class WebSocketClient : MonoBehaviour 
{
    WebSocket MyWebSocket;
    [SerializeField] int PortNumber = 9999;
    [SerializeField] string IPAddressString = "intelliroller.co.uk/assesor";
    [SerializeField] string HandleString = "pollingservice";

    public Main Main;
    public Bluetooth Bluetooth;
    public SpeedTest SpeedTest;
    public Wheelchaircontrol Wheelchaircontrol;

    public float LeftServo;
    public float RightServo;

    private bool SpeedSwitch = false;
    private bool AgilitySwitch = false;
    private bool CoOrdinationSwitch = false;
    private bool StopSwitch = false;

    public bool LagTestSwitch = false;
    public float LagTestTime;

    public string messageforws = "";

    // Use this for initialization
    void Start () 
    {
        InitAndConnectWebSocket();
        //test send message
        SendMessageToSocket("hello");
    }
    private void FixedUpdate()
    {
        if(messageforws != "")
        {
            SendMessageToSocket(messageforws);
            messageforws = "";
        }
    }
    private void Update()
    {
        if(SpeedSwitch)
        {
            StartCoroutine(SpeedTest.RunSpeedTest());
            SpeedSwitch = false;
        } else if (AgilitySwitch) {
            //Agility stuff
            AgilitySwitch = false;
        } else if (CoOrdinationSwitch)
        {
            //balloon stuff
            CoOrdinationSwitch = false;
        } else if (StopSwitch)
        {
            //reset code here
            SpeedTest.endTest();
        }

        if(LagTestSwitch)
        {
            LagTestTime += Time.deltaTime;
        }
    }

    /// <summary>
    /// Getter for the combined websocket creation string
    /// </summary>
    /// <returns>The web socket string.</returns>
    string GetWebSocketString()
    {
        return "wss://" + IPAddressString + ":" + PortNumber + "/" + HandleString;
    }

    /// <summary>
    /// Inits the Websocket, initiates a connection to it, and adds callback events
    /// Presumeably the callbacks happen on the main thread?
    /// </summary>
    void InitAndConnectWebSocket()
    {
        Debug.Log("Starting websocket client...");
        MyWebSocket = new WebSocket("wss://intelliroller.co.uk/assesor");
        MyWebSocket.EmitOnPing = true;
        MyWebSocket.OnError += WebSocketError;
        MyWebSocket.OnMessage += WebSocketMessageReceived;
        MyWebSocket.OnOpen += WebSocketOpened;
        MyWebSocket.OnClose += WebSocketOnClose;
        MyWebSocket.Connect();
        Debug.Log("...Done starting websocket client = " + GetWebSocketString());
    }


    /// <summary>
    /// Callback when socket is closed
    /// </summary>
    /// <param name="sender">Sender.</param>
    /// <param name="e">E.</param>
    void WebSocketOnClose(object sender, CloseEventArgs e)
    {
        Debug.Log("WebSocketClient OnClose: " + e.Reason);
        Main.ConnectionLost();
    }


    /// <summary>
    /// Callback when errors are received
    /// </summary>
    /// <param name="sender">Sender.</param>
    /// <param name="e">E.</param>
    void WebSocketError(object sender, ErrorEventArgs e)
    {
        Debug.LogError("WebSocketClient OnError: " + e.Message);
    }


    /// <summary>
    /// Callback that is made when a message is received from the websocket
    /// Merely prints out the 'Data' field from the messageeventargs
    /// </summary>
    /// <param name="sender">Sender.</param>
    /// <param name="e">E.</param>
    void WebSocketMessageReceived(object sender, MessageEventArgs e)
    {
        string message = e.Data;
        if (e.IsPing)
        {
            Debug.Log("Ping!");
        }
        else if (e.Data == "Speedlogstart")
        {
            Wheelchaircontrol.SpeedLogSwitch = true;
            //message = "messagePair Starting speed logging now...";
        }
        else if (e.Data == "Speedlogstop")
        {
            Wheelchaircontrol.SpeedLogSwitch = false;
        }
        else if(e.Data == "Disconnected from pair")
        {
            Main.ConnectionLost();
        }
        else if (message == "lagtest")
        {
            //send the message to the arduino
            Bluetooth.send("lagtest");
            messageforws = "messagePair Recieved (game). Sending to treadmill";
            LagTestSwitch = true;
        }
        else if(message.Contains("RB*"))
        {
            Bluetooth.send("MA" + message.Substring(3));
            LeftServo += float.Parse(message.Substring(3));
            RightServo += float.Parse(message.Substring(3));
        }
        else if( message.Contains("LB*"))
        {
            Bluetooth.send("MA-"+ message.Substring(3));
            LeftServo -= float.Parse(message.Substring(3));
            RightServo -= float.Parse(message.Substring(3));
        }
        else if (message.Contains("RL*"))
        {
            //Raise left AL100
            Bluetooth.send("AL" + message.Substring(3));
            LeftServo += float.Parse(message.Substring(3));
        }
        else if (message.Contains("RR*"))
        {
            //Raise right AL100
            Bluetooth.send("AR" + message.Substring(3));
            RightServo += float.Parse(message.Substring(3));
        }
        else if (message.Contains("LL*"))
        {
            //Lower left AL-100
            Bluetooth.send("AL-" + message.Substring(3));
            LeftServo -= float.Parse(message.Substring(3));
        }
        else if (message.Contains("LR*"))
        {
            //Lower right AR-100
            Bluetooth.send("AR-" + message.Substring(3));
            RightServo -= float.Parse(message.Substring(3));
        }
        else if (message.Contains("R*"))
        {
            //Reset write code for this
            Bluetooth.send("AR" + (-RightServo).ToString());
            Bluetooth.send("AL" + (-LeftServo).ToString());
            RightServo = 0;
            LeftServo = 0;
            ActuatorReset();
        }
        else if (message.Contains("SpeedTest"))
        {
            //speed test
            Debug.Log("Entering speed test");
            SpeedSwitch = true;
            
            Debug.Log("Exceuted");
        }
        else if (message.Contains("CoordinationTest"))
        {
            //balloon
            //StartCoroutine(SpeedTest.RunSpeedTest());
            CoOrdinationSwitch = true;
        }
        else if (message.Contains("AgilityTest"))
        {
            //agility test
            AgilitySwitch = true;

        }
        else if (message.Contains("Stop"))
        {
            //stop all tests, reset.
            StopSwitch = true;
        }
        else
        {
            Debug.Log(message);
        }


        Debug.Log("WebSocketClient OnMessage: " + message);
        Main.IncomingMessage(message);
    }

    /// <summary>
    /// Callback that is run when the socket is opened
    /// merely prints out the received params
    /// </summary>
    /// <param name="sender">Sender.</param>
    /// <param name="e">E.</param>
    void WebSocketOpened(object sender, System.EventArgs e)
    {
        Debug.Log("WebSocketClient OnOpen: " + e.ToString());
    }

    public void ActuatorReset()
    {

    }

    /// <summary>
    /// Sends a string message to socket.
    /// </summary>
    /// <param name="message">Message.</param>
    public void SendMessageToSocket(string message)
    {
        if (MyWebSocket != null &&  MyWebSocket.IsAlive)
        {
            Debug.Log("WebSocketClient Sending msg:" +  message);
            //MyWebSocket.Send(Encoding.ASCII.GetBytes(message)) I did this because it wasn't sending the speed test results properly, maybe the quotes get fucky while encoding
            MyWebSocket.Send(message);
        }
    }


    /// <summary>
    /// Method called from unity when the component is 'destroyed'
    /// Here we ought to close the socket like a responsible user.
    /// </summary>
    private void OnDestroy()
    {
        Debug.Log("Closing WebSocketClient: " + GetWebSocketString());
        MyWebSocket.Close();
    }
}

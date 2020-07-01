using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Main : MonoBehaviour
{
    // Start is called before the first frame update
    public GameObject Websocketscript;
    public WebSocketClient WebSocketClient;
    public Text UIOutput;
    public GameObject UIBox;
    bool connected = false;
    public GameObject LoadingAnimation;
    public GameObject KeyBox;
    public Text KeyboxOutput;
    public GameObject BluetoothBox;
    public Text BluetoothText;
    public TextMesh Scoreboard1;
    public TextMesh ScoreBoard2;
    int key;
    int timescalled = 0;
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        //WebSocketClient.SendMessageToSocket("help");
        if (connected == false)
        {
            UIBox.SetActive(true);
            MainUIBox("Connecting to Intelliroller services...");
            if (key == 0)
            {
                WebSocketClient.SendMessageToSocket("generateKey");
            }
            MainUIBox("Please tell the assesor the pairing code below:");
            LoadingAnimation.SetActive(false);
            KeyBox.SetActive(true);
            KeyboxOutput.text = key.ToString();
        }
        else
        { //connected to an assesor
            MainUIBox("Connection established!");
            KeyBox.SetActive(false);
            UIBox.SetActive(false);
        }
    }
    public void Scoreboard(string Message)
    {
        Scoreboard1.text = Message;
        ScoreBoard2.text = Message;
    }
    public void IncomingMessage(string Message)
    {
        //deal with incoming websocket messages here
        if(Message.Length == 4 && int.TryParse(Message, out int i))
        {
            //we here boys
            try
            {
                key = int.Parse(Message);
            } 
            catch
            {
                Debug.Log("Woops, that's not a key");
            }
        } else if (Message == "Matched")
        {
            connected = true;
        }

        
    }
    public void BluetoothMessage(string message)
    {
        if (message == "remove") //send a blank message to deactivate box
        {
            BluetoothBox.SetActive(false);
        }
        else
        {
            BluetoothBox.SetActive(true);
            BluetoothText.text = message;
            Debug.Log("Got to bluetooth message!" + " " + message);
        }
    }
    public void ConnectionLost ()
    {
        key = 0;
        connected = false;
    }
    public void connectedToAssesor()
    {
        connected = true;
    }
    void MainUIBox (string Message)
    {
        if(Message == "remove")//send a remove message to deactivate box
        {
            UIBox.SetActive(false);
        }
        else
        {
            UIBox.SetActive(true);
        }
        UIOutput.text = Message;
    }

}


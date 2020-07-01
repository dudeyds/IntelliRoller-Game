using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using System.Collections.Generic;

using TechTweaking.Bluetooth;

public class Bluetooth : MonoBehaviour
{

    private BluetoothDevice device;
    public Text statusText;
    public Wheelchaircontrol Wheelchaircontrol;
    public WebSocketClient WebSocketClient;
    public Main Main;
    private string MessageToSend;
    private List<string> MessageToSendList = new List<string>();
    bool messagesent = true;
    // Use this for initialization
    void Awake()
    {
        device = new BluetoothDevice();

        if (BluetoothAdapter.isBluetoothEnabled())
        {
            connect();
        }
        else
        {

            //BluetoothAdapter.enableBluetooth(); //you can by this force enabling Bluetooth without asking the user
            statusText.text = "Status : Please enable your Bluetooth";
            Main.BluetoothMessage("Status : Please enable your Bluetooth");

            BluetoothAdapter.OnBluetoothStateChanged += HandleOnBluetoothStateChanged;
            BluetoothAdapter.listenToBluetoothState(); // if you want to listen to the following two events  OnBluetoothOFF or OnBluetoothON

            BluetoothAdapter.askEnableBluetooth();//Ask user to enable Bluetooth

        }
    }

    void Start()
    {
        BluetoothAdapter.OnDeviceOFF += HandleOnDeviceOff;//This would mean a failure in connection! the reason might be that your remote device is OFF

        BluetoothAdapter.OnDeviceNotFound += HandleOnDeviceNotFound; //Because connecting using the 'Name' property is just searching, the Plugin might not find it!.
    }

    private void connect()
    {

        statusText.text = "Status : Trying To Connect";
        Main.BluetoothMessage("Status : Trying To Connect");


        /* The Property device.MacAdress doesn't require pairing. 
		 * Also Mac Adress in this library is Case sensitive,  all chars must be capital letters
		 */
        device.MacAddress = "98:D3:51:F5:E5:67";

        /* device.Name = "My_Device";
		* 
		* Trying to identefy a device by its name using the Property device.Name require the remote device to be paired
		* but you can try to alter the parameter 'allowDiscovery' of the Connect(int attempts, int time, bool allowDiscovery) method.
		* allowDiscovery will try to locate the unpaired device, but this is a heavy and undesirable feature, and connection will take a longer time
		*/


        /*
		 * 10 equals the char '\n' which is a "new Line" in Ascci representation, 
		 * so the read() method will retun a packet that was ended by the byte 10. simply read() will read lines.
		 * If you don't use the setEndByte() method, device.read() will return any available data (line or not), then you can order them as you want.
		 */
        device.setEndByte(10);


        /*
		 * The ManageConnection Coroutine will start when the device is ready for reading.
		 */
        device.ReadingCoroutine = ManageConnection;


        statusText.text = "Status : trying to connect";
        Main.BluetoothMessage("Status : trying to connect");

        device.connect();

    }


    //############### Handlers/Recievers #####################
    void HandleOnBluetoothStateChanged(bool isBtEnabled)
    {
        if (isBtEnabled)
        {
            connect();
            //We now don't need our recievers
            BluetoothAdapter.OnBluetoothStateChanged -= HandleOnBluetoothStateChanged;
            BluetoothAdapter.stopListenToBluetoothState();
        }
    }

    //This would mean a failure in connection! the reason might be that your remote device is OFF
    void HandleOnDeviceOff(BluetoothDevice dev)
    {
        if (!string.IsNullOrEmpty(dev.Name))
        {
            statusText.text = "Status : can't connect to '" + dev.Name + "', device is OFF ";
            Main.BluetoothMessage("Status : can't connect to '" + dev.Name + "', device is OFF ");
            connect();
        }
        else if (!string.IsNullOrEmpty(dev.MacAddress))
        {
            statusText.text = "Status : can't connect to '" + dev.MacAddress + "', device is OFF ";
            Main.BluetoothMessage("Status : can't connect to '" + dev.MacAddress + "', device is OFF ");
            connect();
        }
    }

    //Because connecting using the 'Name' property is just searching, the Plugin might not find it!.
    void HandleOnDeviceNotFound(BluetoothDevice dev)
    {
        if (!string.IsNullOrEmpty(dev.Name))
        {
            statusText.text = "Status : Can't find a device with the name '" + dev.Name + "', device might be OFF or not paird yet ";
            Main.BluetoothMessage("Status : Can't find a device with the name '" + dev.Name + "', device might be OFF or not paird yet ");

        }
    }

    public void disconnect()
    {
        if (device != null)
            device.close();
    }
    public void send(string message)
    {
        MessageToSendList.Add(message);
        //MessageToSend = message;
        messagesent = false;
    }
    
    //############### Reading Data  #####################
    IEnumerator ManageConnection(BluetoothDevice device)
    {
        statusText.text = "Status : Connected & Can read";
        Main.BluetoothMessage("Remove");

        while (device.IsReading)
        {
            BtPackets packets = device.readAllPackets();

            if (packets != null)
            {

                for (int i = 0; i < packets.Count; i++)
                {

                    //packets.Buffer contains all the needed packets plus a header of meta data (indecies and sizes) 
                    //To parse a packet we need the INDEX and SIZE of that packet.
                    int indx = packets.get_packet_offset_index(i);
                    int size = packets.get_packet_size(i);

                    string content = System.Text.ASCIIEncoding.ASCII.GetString(packets.Buffer, indx, size);
                    statusText.text = "MSG : " + content;
                    if(content.Contains(" : "))
                    {
                        //it's a movement command!
                        string RightWheelInput = content.Remove(content.IndexOf(" : "));
                        string LeftWheelInput = content.Replace(RightWheelInput + " : ", "");
                        try
                        {
                            float RightWheel = float.Parse(RightWheelInput);
                            float LeftWheel = float.Parse(LeftWheelInput);
                            if( //test to make sure the data makes sense, the blueooth module is picking up nasty power
                                RightWheel > 0 && RightWheel < 65536 &&
                                LeftWheel > 0 && LeftWheel < 65536
                                )
                            {
                                Wheelchaircontrol.WheelchairMovement(LeftWheel, RightWheel);
                                //device.send(System.Text.Encoding.ASCII.GetBytes("MA100" + (char)10));
                            }
                            
                        }
                        catch { }
                        
                    }
                    else if(content.Contains("lagtestreply"))
                    {
                        //we got the reply from the arduino, now to send it back!
                        WebSocketClient.messageforws = "messagePair lagtestreply";
                        WebSocketClient.LagTestSwitch = false;

                        statusText.text = WebSocketClient.LagTestTime.ToString() + "ms for a reply from arduino";
                        WebSocketClient.messageforws = WebSocketClient.LagTestTime.ToString() + "ms for a reply from arduino";
                        WebSocketClient.LagTestTime = 0f;
                    }
                }
            }
            if(!messagesent)
            {
                foreach (string message in MessageToSendList)
                {
                    device.send(System.Text.Encoding.ASCII.GetBytes(message + (char)10));
                    
                }
                MessageToSendList.Clear();
                //device.send(System.Text.Encoding.ASCII.GetBytes(MessageToSend + (char)10));
                messagesent = true;
            }
            yield return null;
        }

        statusText.text = "Status : Done Reading";

    }
    //############### Deregister Events  #####################
    void OnDestroy()
    {
        BluetoothAdapter.OnDeviceOFF -= HandleOnDeviceOff;
        BluetoothAdapter.OnDeviceNotFound -= HandleOnDeviceNotFound;

    }

}

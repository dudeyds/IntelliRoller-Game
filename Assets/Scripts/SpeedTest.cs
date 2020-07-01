using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;

public class SpeedTest : MonoBehaviour
{
    public GameObject Wheelchair;
    public GameObject SpeedTestObject;
    public GameObject StartGate;
    public GameObject FinishGate;
    public Main Main;
    public WebSocketClient WebSocketClient;
    public Wheelchaircontrol WheelchairControl;
    public Image FadePanel;
    public Material FinishLineMat;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public void Test()
    {
        Main.BluetoothMessage("This is s s s s s test");
    }
    public IEnumerator RunSpeedTest()
    {
        Debug.Log("In RunSpeedTest");
        //fade to balck here
        StartCoroutine("FadeToBlack", true);
        yield return new WaitForSeconds(2);
        //reset actuators here
        WebSocketClient.ActuatorReset();
        if(WebSocketClient.LeftServo > WebSocketClient.RightServo) //which side will take the longest?
        {
            //wait for the amount of milliseconds Leftservo requires
            yield return new WaitForSeconds(WebSocketClient.LeftServo / 1000);
        }
        else
        {
            //wait for the amount of milliseconds RightServoRequires
            yield return new WaitForSeconds(WebSocketClient.RightServo / 1000);
        }
        //move the wheelchair to the starting position
        Wheelchair.transform.position = new Vector3(12.62f, 0.126f, -9.757f);
        Wheelchair.transform.rotation = Quaternion.Euler(new Vector3(0, 0, 0));
        //reset wheelchair positioning and movement
        WheelchairControl.ResetWheelChair();
        SpeedTestObject.SetActive(true);
        //make the finish line glow
        StartCoroutine(FinishLineGlow());
        //fadeback
        StartCoroutine("FadeToBlack", false);

        yield return new WaitForSeconds(2); //wait for fade to back to end
        //play mario kart
        yield return new WaitForSeconds(.4f);
        Main.Scoreboard("3");
        yield return new WaitForSeconds(1f);
        Main.Scoreboard("2");
        yield return new WaitForSeconds(1f);
        Main.Scoreboard("1");
        yield return new WaitForSeconds(.2f);
        
        for (float I = 0; I > -1.1f; I -= 0.04f) //lower the start gate!
        {
            StartGate.transform.position = new Vector3(StartGate.transform.position.x, StartGate.transform.position.y - 0.04f, StartGate.transform.position.z);
            yield return new WaitForSeconds(.02f);
        }

        //let's go!
        float timer = 0;

        do
        {
            //todo, add acceleration points every set amount of time, record this to send to the cloud later.
            timer += Time.deltaTime;
            Main.Scoreboard(timer.ToString());
            yield return null;
        }
        while (Wheelchair.transform.position.z < FinishGate.transform.position.z - 3); //while you haven't crossed the finish line

        //race complete

        //stop the finishline glowing
        StopCoroutine(FinishLineGlow());

        //deal with the results
        Main.Scoreboard("Time: " + timer.ToString());
        string SpeedTestMessage = "AddTestEvent Speed*" + DateTime.Now.Year.ToString() + "-" + DateTime.Now.Month.ToString() + "-" + DateTime.Now.Day.ToString() +
            " " + DateTime.Now.Hour.ToString() + ":" + DateTime.Now.Minute.ToString() + ":" + DateTime.Now.Second.ToString() + "*" + timer.ToString();
        Debug.Log(SpeedTestMessage);
        WebSocketClient.SendMessageToSocket(SpeedTestMessage);
        WebSocketClient.messageforws = SpeedTestMessage;

        //end the test
        endTest();
        



    }
    public IEnumerator FadeToBlack(bool direction)
    {
        if(direction)
        {
            for (float f = 0f; f <= 1f; f += 0.005f)
            {
                Color C = FadePanel.color;
                C.a = f;
                FadePanel.color = C;
                yield return new WaitForSeconds(.01f);
            }
        }
        else
        {
            for (float f = 1f; f >= 0f; f -= 0.005f)
            {
                Color C = FadePanel.color;
                C.a = f;
                FadePanel.color = C;
                yield return new WaitForSeconds(.01f);
            }
        }
        
    }
    public IEnumerator FinishLineGlow()
    {

        Color c = FinishLineMat.color;
        while (true)
        {
            for (float i = 0; i < 200; i += 2f)
            {

                c.a = i / 1000;
                FinishLineMat.color = c;
                yield return null;
            }
            for (float i = 200; i > 0; i -= 2f)
            {
                c.a = i / 1000;
                FinishLineMat.color = c;
                yield return null;
            }
        }
    }

    public void endTest()
    {
        //stop finishline glowing if it is
        SpeedTestObject.SetActive(false);
    }
}

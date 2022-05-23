using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Text;
using System.IO;
using System;
public class TextUi : MonoBehaviour
{

    public int formatSize = 10;
    StreamWriter writer;
    public TopView mapObject;
    public AltitudeGraph altitudeGraph;

    public TMP_InputField portInput;

    private TMP_Text infoText;

    public Vector3 canPos, mag, accel, gyro;

    public float temp, hum, pressure, bat;

    public string currentPort;

    public bool connected = false;
    public bool error = false;
    // Start is called before the first frame update
    void Start()
    {
        infoText = gameObject.GetComponent<TMP_Text>();

        string path = Application.dataPath + "savedData" + System.DateTime.UtcNow.ToString("HH_mm_ss__dd_MMMM") + ".csv";
        writer = new StreamWriter(path);

        writer.WriteLine("Time,PosX,PosY,Altitude,MagX,MagY,MagZ,AccelX,AccelY,AccelZ,GyroX,GyroY,GyroZ,Tmp,Hum,Press");
    }

    public void updatePosition(Vector3 newPosition)
    {
        canPos = newPosition;
        UpdateData();
    }

    public void updateMag(Vector3 newMag, Vector3 newAccel, Vector3 newGyro)
    {
        mag = newMag;
        accel = newAccel;
        gyro = newGyro;
        UpdateData();
    }

    public void updateEnv(float newTemp, float newHum, float newPres)
    {
        temp = newTemp;
        hum = newHum;
        pressure = newPres;
        UpdateData();
    }

    public void UpdateBat(float newBat)
    {
        bat = newBat;
    }

    private float calculateAltitude()
    {

        return 44330 * (1- Mathf.Pow((pressure/1015), 1f/5.255f));

    }

    // Update is called once per frame
    void Update( ) {

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Application.Quit();
        }


        currentPort = portInput.text;


        if (!connected) {
            infoText.text = "Not connected!!";
        }
/*        if (error)
        {
            infoText.text = "ERROR!";
        }*/
    }

    string formatFloat(float input)
    {
        string result = "";

        if (input >= 0)
        {
            result += " ";
        }

        result += input.ToString();



        while (result.Length < formatSize)
        {
            result += " ";
        }


        return result;
    }

    string formatVector(Vector3 input, string name)
    {
        string result = "";
        result += name + ": ";

        result += formatFloat(input.x) + " / ";
        result += formatFloat(input.y) + " / ";
        result += formatFloat(input.z) + " / ";

        result += "\n";

        return result;

    }

    string convertVectorToCSV(Vector3 input )
    {
        string result = "";
        result += input.x.ToString() + ".";
        result += input.y.ToString() + ".";
        result += input.z.ToString();

        return result;
    }

    void UpdateData() {

        infoText.text = "<mspace=0.55em>" + formatVector(canPos, "pos  ") + formatVector(mag, "mag  ") + formatVector(accel, "accel") + formatVector(gyro, "gyro ")
                        + "temp: " + formatFloat(temp) + " humidity: " + formatFloat(hum) + "\npressure: " + formatFloat(pressure) + " bat: " + formatFloat(bat)
                        + "\naltitude: " + formatFloat(calculateAltitude());
            ;


        mapObject.addPoint(canPos);

        altitudeGraph.addPoint(canPos.z);


        string data = System.DateTime.UtcNow.ToString("HH:mm:ss  dd MMMM") + "." +
                         convertVectorToCSV(canPos) + "." +
                         convertVectorToCSV(mag) + "." +
                         convertVectorToCSV(accel) + "." +
                         convertVectorToCSV(gyro) + "." +
                         temp.ToString() + "." + hum.ToString() + "." + pressure.ToString();

        data = data.Replace(",", "c");
        data = data.Replace(".", ",");
        data = data.Replace("c", ".");

        writer.WriteLine(data);

    }

    private void OnDisable()
    {
        writer.Flush();
        writer.Close();
    }
}

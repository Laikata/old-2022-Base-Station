using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TextUi : MonoBehaviour
{

    public int formatSize = 10;

    public TopView mapObject;
    public AltitudeGraph altitudeGraph;


    private TMP_Text infoText;

    public Vector3 canPos, mag, accel, gyro;

    public float temp, hum, pressure, bat;


    // Start is called before the first frame update
    void Start()
    {
        infoText = gameObject.GetComponent<TMP_Text>();
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


    // Update is called once per frame
    void Update( ) {
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

    void UpdateData() {


        infoText.text = "<mspace=0.55em>" + formatVector(canPos, "pos  ") + formatVector(mag, "mag  ") + formatVector(accel, "accel") + formatVector(gyro, "gyro ")
                        + "temp: " + formatFloat(temp) + " humidity: " + formatFloat(hum) + " pressure: " + formatFloat(pressure)
            ;


        mapObject.addPoint(canPos);

        altitudeGraph.addPoint(canPos.z);
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TextUi : MonoBehaviour
{

    public GameObject mapObject;
    public GameObject altitudeGraph;


    private TMP_Text infoText;

    public Vector3 canPos, mag, accel, gyro;

    public float temp, hum, pressure;


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

    public void updateTempAndPres(float newTemp, float newHum)
    {
        temp = newTemp;
        hum = newHum;
        UpdateData();
    }


    // Update is called once per frame
    void Update( ) {
    }

    void UpdateData() {


        infoText.text = "<mspace=0.55em> can position: " + canPos.x.ToString() + " " + canPos.y.ToString() + " " + canPos.z.ToString() + " " + "\n"
                      + "mag: " + mag.ToString() + "\naccel: " + accel.ToString() + "\ngyro: " + gyro.ToString() + "\n"
                      + "temp: " + temp.ToString() + " humidity: " + hum.ToString() + " pressure: " + pressure.ToString();


        mapObject.GetComponent<TopView>().addPoint(canPos);

        altitudeGraph.GetComponent<AltitudeGraph>().addPoint(canPos.z);
    }
}

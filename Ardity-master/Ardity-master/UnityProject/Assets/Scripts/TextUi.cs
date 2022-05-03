using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TextUi : MonoBehaviour
{
    private Text infoText;

    public Vector3 canPos, mag, accel, gyro;

    public float temp, hum, pressure;


    // Start is called before the first frame update
    void Start()
    {
        infoText = gameObject.GetComponent<Text>();
    }

    // Update is called once per frame
    void Update()
    {
        infoText.text = "can position: " + canPos.ToString() + "\n"
                      + "mag: " + mag.ToString() + "\naccel: " + accel.ToString() + "\ngyro: " + gyro.ToString() + "\n"
                      + "temp: " + temp.ToString() + " humidity: " + hum.ToString() + " pressure: " + pressure.ToString();
    }
}

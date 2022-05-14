using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AltitudeGraph : MonoBehaviour
{
    public float maxDisplayX = 10;
    public float maxDisplayY = 10;

    public GameObject dot;

    private float maxX = 0;
    private float maxY = 1500;
    private float minX = 0;
    private float minY = 0;

    private LineRenderer line;


    private List<Vector3> allPositions;

    // Start is called before the first frame update
    void Start()
    {
        line = gameObject.GetComponent<LineRenderer>();


        allPositions = new List<Vector3>();
    }


    Vector3 mapPosition(Vector3 pos)
    {
        Vector3 newPos = new Vector3(0, 0, 0);

        newPos.x = ((pos.x - minX) * (maxDisplayX - 0) / (maxX - minX) + 0) + transform.position.x;
        newPos.y = ((pos.y - minY) * (maxDisplayY - 0) / (maxY - minY) + 0) + transform.position.y;

        return newPos;
    }

    // Update is called once per frame
    void Update()
    {

    }

    void UpdatePoints()
    {
        line.positionCount = gameObject.transform.childCount;

        for (int i = 0; i < gameObject.transform.childCount; i++)
        {
            GameObject currentDot = gameObject.transform.GetChild(i).gameObject;

            Vector3 newPos = mapPosition(allPositions[i]);

            line.SetPosition(i, newPos);

            currentDot.transform.position = newPos;
        }

    }

    public void addPoint(float altitude)
    {
        Debug.Log(altitude.ToString());

        allPositions.Add(new Vector3(gameObject.transform.childCount, altitude, 0));

        GameObject newDot = Instantiate(dot, Vector3.zero, Quaternion.identity);

        newDot.transform.parent = this.gameObject.transform;

        maxX = gameObject.transform.childCount;
        

        UpdatePoints();


    }
}

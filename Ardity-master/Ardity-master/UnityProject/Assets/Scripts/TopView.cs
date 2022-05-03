using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TopView : MonoBehaviour
{

    public float maxDisplayX = 10;
    public float maxDisplayY = 10;

    public GameObject dot;

    private float maxX = -10000;
    private float maxY = -10000;
    private float minX = 10000;
    private float minY = 10000;

    private LineRenderer line;

    public Vector3 testPos;

    private List<Vector3> allPositions;

    // Start is called before the first frame update
    void Start()
    {
        line = gameObject.GetComponent<LineRenderer>();

        testPos = new Vector3(5, 5);

        allPositions = new List<Vector3>();
    }


    Vector3 mapPosition(Vector3 pos)
    {
        Vector3 newPos = new Vector3(0, 0, 0);

        newPos.x = ((pos.x - minX) * (maxDisplayX - 0) / (maxX - minX) + 0) + transform.position.x;
        newPos.y = (pos.y - minY) * (maxDisplayY - 0) / (maxY - minY) + 0 + transform.position.y;

        return newPos;
    }

    // Update is called once per frame
    void Update()
    {
        if (Random.value < 0.005)
        {
            testPos.x += Random.Range(-0.00002f, 0.00005f);
            testPos.y += Random.Range(-0.00002f, 0.00005f);


            AddPoint(testPos);
        }
    }

    void UpdatePoints()
    {
        line.positionCount = gameObject.transform.childCount;

        for (int i = 0;i < gameObject.transform.childCount; i++)
        {
            GameObject currentDot = gameObject.transform.GetChild(i).gameObject;

            Vector3 newPos = mapPosition(allPositions[i]);

            line.SetPosition(i, newPos);

            currentDot.transform.position = newPos;
        }
        
    }

    void AddPoint(Vector3 pos)
    {
        Debug.Log(pos.ToString());

        allPositions.Add(new Vector3(pos.x, pos.y, 0));

        GameObject newDot = Instantiate(dot, Vector3.zero, Quaternion.identity);

        newDot.transform.parent = this.gameObject.transform;

        if (pos.x > maxX) { maxX = pos.x; }
        if (pos.y > maxY) { maxY = pos.y; }
        if (pos.x < minX) { minX = pos.x; }
        if (pos.y < minY) { minY = pos.y; }

        UpdatePoints();


    }

}

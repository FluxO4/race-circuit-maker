

using UnityEngine;
using System.Collections.Generic;

public class Bella : MonoBehaviour
{
    public GameObject mainPrefab; // Assign your main prefab in the Inspector
    public GameObject widthPrefab; // Assign your width prefab in the Inspector
    public GameObject parentGameObject; // Assign your parent GameObject in the Inspector
    private GameObject currentGameObject;
    public int gap = 5;
    public int sidegap = 2;
    private float curveWidth = 0f; // Initial curve width
    private List<GameObject> curvePoints = new List<GameObject>(); // List to keep track of curve points

    void Start()
    {
        // Instantiate the initial GameObject and set it as a child of the parent
        currentGameObject = InstantiateMainCurvePoint(new Vector3(0, 0, 0));
    }

    public void AddGameObjectAndUpdateReference()
    {
        if (currentGameObject != null)
        {
            Vector3 newPosition = currentGameObject.transform.position + new Vector3(gap, 0, 0);
            currentGameObject = InstantiateMainCurvePoint(newPosition);
        }
        else
        {
            Debug.LogError("Original GameObject is null!");
        }
    }

    private GameObject InstantiateMainCurvePoint(Vector3 position)
    {
        // Instantiate the main GameObject
        GameObject mainInstance = Instantiate(mainPrefab, position, Quaternion.identity, parentGameObject.transform);
        curvePoints.Add(mainInstance);

        // Instantiate the width GameObjects based on the current curve width
        for (int i = 1; i <= curveWidth; i++)
        {
            Instantiate(widthPrefab, position + new Vector3(0, 0, i * sidegap), Quaternion.identity, mainInstance.transform);
            Instantiate(widthPrefab, position + new Vector3(0, 0, -i * sidegap), Quaternion.identity, mainInstance.transform);
        }

        return mainInstance;
    }

    // Function to increase the curve width
    public void IncreaseCurveWidth()
    {
        curveWidth++; // Increase width by one step (sidegap units)

        foreach (GameObject point in curvePoints)
        {
            Vector3 position = point.transform.position;
            Instantiate(widthPrefab, position + new Vector3(0, 0, curveWidth * sidegap), Quaternion.identity, point.transform);
            Instantiate(widthPrefab, position + new Vector3(0, 0, -curveWidth * sidegap), Quaternion.identity, point.transform);
        }
    }
}

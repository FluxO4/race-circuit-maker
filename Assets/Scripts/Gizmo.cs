using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Gizmo : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        InstantiateGizmo();
        
    }


    public GameObject prefabToInstantiate; // Assign this in the Inspector
    public Camera camera;
    HashSet<GameObject> instantiatedObjects = new HashSet<GameObject>();

    public void InstantiateGizmo()
    {
        if (Input.GetMouseButtonDown(0)) // Left mouse button clicked
        {
            RaycastHit hit;
            Ray ray = camera.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray, out hit))
            {
                GameObject hitObject = hit.collider.gameObject;
                if (hitObject != null && !instantiatedObjects.Contains(hitObject))
                {
                    // Instantiate the prefab at the clicked GameObject's position
                    Instantiate(prefabToInstantiate, hit.collider.gameObject.transform.position, Quaternion.identity);
                    instantiatedObjects.Add(hitObject);
                }
            }
        }
    }
}

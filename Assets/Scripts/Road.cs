using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Road : MonoBehaviour
{
    public List<Point> associatedPoints;

    //ADD: enum variable called TextureType

    public void RoadHighlight(bool activate)
    {
        //I dunno, gives it a temporary material or something to brighten it up, or maybe unity editor library has highlighting functions of its own. If the latter is the case, move this function to the editor script and add Road as a parameter, let's not inherit UnityEditor in this script
    }

    public void buildRoad()
    {
        //Again, if editor stuff is required, move this to the Editor script

        //Builds the road mesh
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}

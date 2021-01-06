using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
public class Manager : MonoBehaviour {

    [SerializeField]
    private string file;
    [SerializeField]
    private GameObject emptyRoad;

    private Root root;
    private List<Road> roads;
    private List<Tuple<int, int>> coordOutliers;
    private List<Tuple<int, int>> coordNonOutliers;


    void Start() {
        roads = new List<Road>();
        root = JsonParser.Parse("Assets/Data/" + file);
        coordOutliers = new List<Tuple<int, int>>();
        coordNonOutliers = new List<Tuple<int, int>>();
        SetUpRoads();

        DrawRoads();

    }

    /// <summary> Link roads coordinates with LineRenderer </summary>
    void DrawRoads() {
        foreach (Road r in roads) {
            GameObject emptyRoadGO = Instantiate(emptyRoad);
            LineRenderer lr = emptyRoadGO.GetComponent<LineRenderer>();

            foreach (Vector3 v in r.positions) {
                lr.positionCount++;
                lr.SetPosition(lr.positionCount - 1, v);
            }
            lr.name = r.nom;
            lr.widthMultiplier = r.largeur;
        }
    }

    /// <summary> Create roads objects from JSON </summary>
    void SetUpRoads() {

        List<float>[] listsCoordinates = {
            new List<float>(), new List<float>(), new List<float>()
        };

        // For each enties in the json
        foreach (Feature f in root.features) {
            int importance;
            string nom, id;
            float largeur;

            // Setting "importance" from the string or 0 if it is not a number
            int.TryParse(f.properties.IMPORTANCE, out importance);

            // Setting the name from numero, name and id
            nom = f.properties.NUMERO + " " + f.properties.NOM_VOIE_G + " "+ f.properties.ID;

            largeur = (float)f.properties.LARGEUR;

            id = f.properties.ID;

            List<Vector3> positions = new List<Vector3>();

            // Loop through each point of the road
            foreach (List<double> l in f.geometry.coordinates[0]) {
                float x = (float)l[0];
                float y = (float)l[2];
                float z = (float)l[1];

                listsCoordinates[0].Add(x);
                listsCoordinates[1].Add(y);
                listsCoordinates[2].Add(z);

                //lstAltitudes.Add(y);
                positions.Add(new Vector3(x, y, z));
            }


            roads.Add(new Road(positions, largeur, nom, importance, id));
        }
        
        listsCoordinates[0].Sort();
        listsCoordinates[1].Sort();
        listsCoordinates[2].Sort();

        int iupper = (int)((listsCoordinates[1].Count + 1) * 0.75);
        float maxValue = listsCoordinates[1][iupper];

        float medX = listsCoordinates[0][(int)((listsCoordinates[0].Count + 1) * 0.5)];
        float medY = listsCoordinates[1][(int)((listsCoordinates[1].Count + 1) * 0.5)];
        float medZ = listsCoordinates[2][(int)((listsCoordinates[2].Count + 1) * 0.5)];



        Debug.Log("Max value : " + maxValue);

        for (int i = 0; i < roads.Count; i++)
        {
            for (int j = 0; j < roads[i].positions.Count;j++)
            {
                float x = roads[i].positions[j].x - medX;
                float y = roads[i].positions[j].y - medY;
                float z = roads[i].positions[j].z - medZ;

                roads[i].positions[j] = new Vector3(x, y, z);

                if (roads[i].positions[j].y > maxValue)
                {
                    coordOutliers.Add(new Tuple<int, int>(i,j));
                }
                else
                {
                    coordNonOutliers.Add(new Tuple<int, int>(i, j));
                }
            }
        }

        foreach(Tuple<int,int> tOutlier in coordOutliers)
        {
            RecalulateAltitude2(tOutlier);
        }

    }


    void RecalulateAltitude2(Tuple<int, int> tOutlier)
    {
        float nearestDistance = float.MaxValue;
        Vector3 nearestVec = Vector3.zero;
        Vector3 vecOutlier = roads[tOutlier.Item1].positions[tOutlier.Item2];
        
        foreach (Tuple<int, int> tNonoutlier in coordNonOutliers)
        {
            Vector3 vecNonOutlier = roads[tNonoutlier.Item1].positions[tNonoutlier.Item2]; 
            float distance = Vector2.Distance(new Vector2(vecOutlier.x, vecOutlier.z), new Vector2(vecNonOutlier.x, vecNonOutlier.z));
            if ( distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestVec = vecNonOutlier;
            }
        }

        roads[tOutlier.Item1].positions[tOutlier.Item2] = new Vector3(vecOutlier.x, nearestVec.y , vecOutlier.z);

    }




    /* Recalculate altitude for outlier
     * Returns : true if ok, false instead
     */
    bool RecalulateAltitude(Road road, float maxValue) {
        List<int> indexOutliers = new List<int>();
        List<int> indexNonOutliers = new List<int>();

        for (int i = 0; i < road.positions.Count; i ++) {
            if (road.positions[i].y > maxValue) {
                indexOutliers.Add(i);

            } else {
                indexNonOutliers.Add(i);
            }
        }

        if (indexNonOutliers.Count == 0) {
            return false;
        }

        foreach (int index in indexOutliers) {
            road.positions[index] = new Vector3(
                road.positions[index].x,
                road.positions[indexNonOutliers[0]].y,
                road.positions[index].z
                );
        }
        return true;
    }


}

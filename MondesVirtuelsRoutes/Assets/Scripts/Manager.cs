using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Manager : MonoBehaviour {

    [SerializeField]
    private string file;
    [SerializeField]
    private GameObject emptyRoad;

    private Root root;
    private List<Road> roads;

    void Start() {
        roads = new List<Road>();
        root = JsonParser.Parse("Assets/Data/" + file);

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

            lr.widthMultiplier = r.largeur;
        }
    }

    /// <summary> Create roads objects from JSON </summary>
    void SetUpRoads() {
        foreach (Feature f in root.features) {
            int importance = int.Parse(f.properties.IMPORTANCE);
            float largeur = (float)f.properties.LARGEUR;
            string id = f.properties.ID;
            List<Vector3> positions = new List<Vector3>();

            foreach (List<double> l in f.geometry.coordinates[0]) {
                positions.Add(new Vector3((float)(l[0] - 841251.2), (float)(l[2] - 225.9), (float)(l[1] - 6518666.3)));
            }

            roads.Add(new Road(positions, largeur, importance, id));
        }
    }


}

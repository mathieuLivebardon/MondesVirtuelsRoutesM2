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
            lr.name = r.nom;
            lr.widthMultiplier = r.largeur;
        }
    }

    /// <summary> Create roads objects from JSON </summary>
    void SetUpRoads() {

        float xOffset = (float)root.features[0].geometry.coordinates[0][0][0];
        float zOffset = (float)root.features[0].geometry.coordinates[0][0][1];

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
                float x = (float)l[0] - xOffset;
                float y = 0;
                float z = (float)l[1] - zOffset;

                positions.Add(new Vector3(x, y, z));
            }

            roads.Add(new Road(positions, largeur, nom, importance, id));
        }
    }
}

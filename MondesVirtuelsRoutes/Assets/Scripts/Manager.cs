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
        foreach (Feature f in root.features) {
            int importance = 0;
            try {
                importance = int.Parse(f.properties.IMPORTANCE);
            } catch(System.Exception e) {
                continue;
            }

            string name = f.properties.NUMERO + " " + f.properties.NOM_VOIE_G;

            float largeur = (float)f.properties.LARGEUR;
            string id = f.properties.ID;
            List<Vector3> positions = new List<Vector3>();

            foreach (List<double> l in f.geometry.coordinates[0]) {

                float x = (float)(l[0] - 844522.7);
                float y = (float)(l[2] - 173);
                float z = (float)(l[1] - 6522266.8);

                if (y > 3000) {
                    continue;
                }

                positions.Add(new Vector3(x, y, z));
            }

            roads.Add(new Road(positions, largeur, name, importance, id));
        }
    }


}

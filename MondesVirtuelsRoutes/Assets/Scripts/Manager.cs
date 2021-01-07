using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Manager : MonoBehaviour {

    [SerializeField]
    private string file;
    [SerializeField]
    private GameObject emptyRoad;
    [SerializeField]
    private GameObject terrain;

    private Root root;
    private List<Road> roads;

    [SerializeField]
    private bool fixTerrainCoord;


    void Start() {
        roads = new List<Road>();
        root = JsonParser.Parse("Assets/Data/" + file);

        if (fixTerrainCoord)
            FixTerrainCoord();
        SetUpRoads();
        DrawRoads();
    }

    private void FixTerrainCoord() {
        Mesh mesh = terrain.GetComponentInChildren<MeshFilter>().mesh;
        terrain.transform.GetChild(0).RotateAround(mesh.bounds.center, Vector3.right, -90.0f);
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

            int previousIndexTriangle = -1;
            int currentIndexTriangle;

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
                float z = (float)l[1] - zOffset;
                float y = GetAltitudeFromMap(new Vector3(x, 0, z), out currentIndexTriangle);

                if (previousIndexTriangle > 0 && currentIndexTriangle != previousIndexTriangle) {
                    // Il faut trouver l'arrete commune, et rajouter un point au niveau de l'arrete

                    // récuperer les triangles
                    // check si 2 vertices sont en commun

                    // triangles[indice] -> tableau de vertices

                    Mesh mesh = terrain.GetComponentInChildren<MeshFilter>().mesh;

                    List<Vector3> previousVertices = new List<Vector3> {
                        mesh.vertices[mesh.triangles[previousIndexTriangle * 3 + 0]],
                        mesh.vertices[mesh.triangles[previousIndexTriangle * 3 + 1]],
                        mesh.vertices[mesh.triangles[previousIndexTriangle * 3 + 2]]
                    };
                    List<Vector3> currentVertices = new List<Vector3> {
                        mesh.vertices[mesh.triangles[currentIndexTriangle * 3 + 0]],
                        mesh.vertices[mesh.triangles[currentIndexTriangle * 3 + 1]],
                        mesh.vertices[mesh.triangles[currentIndexTriangle * 3 + 2]]
                    };
                    List<Vector3> sharedVertices = new List<Vector3>();
                    for (int i = 0; i < previousVertices.Count; i ++) {
                        for (int j = 0; j < currentVertices.Count; j++) {
                            if (previousVertices[i] == currentVertices[j]) {
                                if (! sharedVertices.Contains(previousVertices[i]))
                                    sharedVertices.Add(previousVertices[i]);
                            }
                        }
                    }
                    if (sharedVertices.Count == 2) {
                        // calculer un point sur l'arrete

                        Vector2 roadDirection =

                    }
                }

                previousIndexTriangle = currentIndexTriangle;

                positions.Add(new Vector3(x, y, z));
            }

            roads.Add(new Road(positions, largeur, nom, importance, id));
        }
    }

    /// <summary>Get the Y coordinate which will fit with the terrain depending on X & Z or 0.0</summary>
    private float GetAltitudeFromMap(Vector3 pos, out int indexTriangle) {
        RaycastHit hit;
        indexTriangle = 0;
        if (Physics.Raycast(pos, Vector3.down, out hit)) {
            indexTriangle = hit.triangleIndex;
            return hit.point.y;
        } else if (Physics.Raycast(pos, Vector3.up, out hit)) {
            indexTriangle = hit.triangleIndex;
            return hit.point.y;
        }
        return 0.0f;
    }
}
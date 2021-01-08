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
    [SerializeField]
    private GameObject sphere;

    private Root root;
    private List<Road> roads;

    [SerializeField]
    private bool fixTerrainCoord;
    [SerializeField]
    private bool drawSpheres;


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
        Vector3[] newVertices = new Vector3[mesh.vertices.Length];
        for (int i = 0; i < mesh.vertices.Length; i++) {
            // foreach (Vector3 p in mesh.vertices) {
            Vector3 p = mesh.vertices[i];
            Vector3 reversed = new Vector3(p.x, p.z, -p.y);
            /*if (drawSpheres)
                Instantiate(sphere, reversed, Quaternion.identity);*/

            newVertices[i] = reversed;
        }

        terrain.GetComponentInChildren<MeshFilter>().mesh.vertices = newVertices;
        terrain.GetComponentInChildren<MeshFilter>().mesh.RecalculateNormals();

        MeshCollider mc = terrain.GetComponentInChildren<MeshCollider>();
        if (mc == null)
            mc = terrain.transform.GetChild(0).gameObject.AddComponent<MeshCollider>();
        else {
            mc.sharedMesh = null;
            mc.sharedMesh = mesh;
        }
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

        // For each enties (roads) in the json
        foreach (Feature f in root.features) {
            int previousIndexTriangle = -1;
            int currentIndexTriangle;

            int importance;
            string nom, id;
            float largeur;

            // Setting "importance" from the string or 0 if it is not a number
            int.TryParse(f.properties.IMPORTANCE, out importance);

            // Setting the name from numero, name and id
            nom = f.properties.NUMERO + " " + f.properties.NOM_VOIE_G + " " + f.properties.ID;

            largeur = (float)f.properties.LARGEUR;

            id = f.properties.ID;

            Vector3 previousVertice = Vector3.negativeInfinity;
            Vector3 currentVertice;
            List<Vector3> positions = new List<Vector3>();

            // Loop through each point of the road
            foreach (List<double> l in f.geometry.coordinates[0]) {
                float x = (float)l[0] - xOffset;
                float z = (float)l[1] - zOffset;
                float y = GetAltitudeFromMap(new Vector3(x, 0, z), out currentIndexTriangle);

                currentVertice = new Vector3(x, y, z);


                if (ManageTriangleIntersection(previousIndexTriangle, currentIndexTriangle, out List<Vector3> sharedVertices) == 2) {
                    // calculer un point sur l'arrete


                    if (previousVertice != Vector3.negativeInfinity) {
                        Vector3 directionRoad = (currentVertice - previousVertice).normalized;
                        Vector3 directionEdge = (sharedVertices[1] - sharedVertices[0]).normalized;

                        Vector2 current2D = new Vector2(currentVertice.x, currentVertice.z);
                        Vector2 previous2D = new Vector2(previousVertice.x, previousVertice.z);

                        Vector2 firstPoint_Edge2D = new Vector2(sharedVertices[0].x, sharedVertices[0].z);
                        Vector2 secondPoint_Edge2D = new Vector2(sharedVertices[1].x, sharedVertices[1].z);

                        Vector2 intersection = GetIntersectionPointCoordinates(previous2D, current2D, firstPoint_Edge2D, secondPoint_Edge2D, out bool found);
                        if (found) {
                            float t = (intersection.x - firstPoint_Edge2D.x) / (secondPoint_Edge2D.x - firstPoint_Edge2D.x);
                            Vector3 pointOnEdge = (sharedVertices[0] + (sharedVertices[1] - sharedVertices[0]) * t);

                            positions.Add(pointOnEdge);
                        }
                    }
                }


                previousVertice = currentVertice;
                previousIndexTriangle = currentIndexTriangle;

                positions.Add(new Vector3(x, y, z));
            }

            roads.Add(new Road(positions, largeur, nom, importance, id));
        }
    }

    private void print(object s) {
        Debug.Log(s.ToString());
    }

    /** Depending on triangle index, find (if it exist) the shared edge
     * Return the amount of shared vertices
     */
    private int ManageTriangleIntersection(int previousIndexTriangle, int currentIndexTriangle, out List<Vector3> sharedVertices) {
        sharedVertices = new List<Vector3>();

        if (previousIndexTriangle > 0 && currentIndexTriangle != previousIndexTriangle) {
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

            for (int i = 0; i < previousVertices.Count; i++) {
                for (int j = 0; j < currentVertices.Count; j++) {
                    if (previousVertices[i] == currentVertices[j]) {
                        if (!sharedVertices.Contains(previousVertices[i]))
                            sharedVertices.Add(previousVertices[i]);
                    }
                }
            }
            return sharedVertices.Count;
        }
        return 0;
    }

    /// <summary>Get the Y coordinate which will fit with the terrain depending on X & Z or 0.0</summary>
    private float GetAltitudeFromMap(Vector3 pos, out int indexTriangle) {
        RaycastHit hit;
        Vector3 rayOrigin = new Vector3(pos.x, 9999, pos.z);

        indexTriangle = 0;
        if (Physics.Raycast(rayOrigin, Vector3.down, out hit)) {
            indexTriangle = hit.triangleIndex;
            return hit.point.y;
        } 
        return 0.0f;
    }

    /// <summary>
    /// Gets the coordinates of the intersection point of two lines.
    /// </summary>
    /// <param name="A1">A point on the first line.</param>
    /// <param name="A2">Another point on the first line.</param>
    /// <param name="B1">A point on the second line.</param>
    /// <param name="B2">Another point on the second line.</param>
    /// <param name="found">Is set to false of there are no solution. true otherwise.</param>
    /// <returns>The intersection point coordinates. Returns Vector2.zero if there is no solution.</returns>
    public Vector2 GetIntersectionPointCoordinates(Vector2 A1, Vector2 A2, Vector2 B1, Vector2 B2, out bool found) {
        float tmp = (B2.x - B1.x) * (A2.y - A1.y) - (B2.y - B1.y) * (A2.x - A1.x);

        if (tmp == 0) {
            // No solution!
            found = false;
            return Vector2.zero;
        }

        float mu = ((A1.x - B1.x) * (A2.y - A1.y) - (A1.y - B1.y) * (A2.x - A1.x)) / tmp;

        found = true;

        return new Vector2(
            B1.x + (B2.x - B1.x) * mu,
            B1.y + (B2.y - B1.y) * mu
        );
    }
}
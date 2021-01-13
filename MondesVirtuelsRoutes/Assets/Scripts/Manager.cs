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
    public float distance;
    Vector3 offset;

    void Start() {
        roads = new List<Road>();
        root = JsonParser.Parse("Assets/Data/" + file);

        if (fixTerrainCoord)
            FixTerrainCoord();

        SetUpRoads();
        DrawRoads();
    }

    private void FixTerrainCoord() {

        Mesh mesh = terrain.GetComponent<MeshFilter>().mesh;

        offset = mesh.bounds.center;
        Debug.Log(offset);
        GameObject parent = new GameObject("mapCenter");
        parent.transform.position = mesh.bounds.center;
        terrain.transform.parent = parent.transform;
        parent.transform.position = Vector3.zero;
        parent.transform.Rotate(Vector3.right, -90);

        MeshCollider mc = terrain.GetComponentInChildren<MeshCollider>();
        if (mc == null)
            mc = terrain.transform.gameObject.AddComponent<MeshCollider>();
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
            int previousIndexTriangle = int.MinValue;
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
            for (int i = 0; i< f.geometry.coordinates[0].Count;i++) {
                List<double> l = f.geometry.coordinates[0][i];
                float x = (float)l[0] - xOffset;
                float z = (float)l[1] - zOffset;
                float y = GetAltitudeFromMap(new Vector3(x, 9999, z), out currentIndexTriangle);

                currentVertice = new Vector3(x, y, z);

                int counterA = 0;

                Vector3 temporaryPoint = Vector3.negativeInfinity;

                while (temporaryPoint != currentVertice && counterA < 100) {

                    int counterB = 0;

                    temporaryPoint = currentVertice;

                    int amountSharedVertices = ManageTriangleIntersection(previousIndexTriangle, currentIndexTriangle, out List<Vector3> sharedVertices);

                    int temporaryIndexTriangle = currentIndexTriangle;

                    float multiplier = 0.5f;

                    while ((amountSharedVertices < 2) && counterB < 100) {
                        
                        Vector3 verticeInTheMiddle = previousVertice + (temporaryPoint - previousVertice) * multiplier;
                        
                        if (Vector3.Distance(verticeInTheMiddle, previousVertice) < distance)
                        {
                            break;
                        }

                        verticeInTheMiddle.y = GetAltitudeFromMap(new Vector3(verticeInTheMiddle.x, 9999, verticeInTheMiddle.z), out temporaryIndexTriangle);

                        amountSharedVertices = ManageTriangleIntersection(previousIndexTriangle, temporaryIndexTriangle, out sharedVertices);

                        temporaryPoint = verticeInTheMiddle;

                        counterB++;

                        if (counterB >= 10) {
                            print("counterB : " + counterB);

                            print("currentVertice : " + currentVertice);

                            /*print("previousVertice : " + previousVertice);
                            print("currentVertice : " + currentVertice);
                            print("temporaryPoint : " + temporaryPoint);

                            print("previousIndexTriangle : " + previousIndexTriangle);
                            print("temporaryIndexTriangle : " + temporaryIndexTriangle);*/
                        }
                    }

                    // If there is two vertices between previous and current triangles,
                    // both triangles are neighbours, and we have to create a vertice
                    // on the shared edge
                    if (amountSharedVertices == 2) {
                        if (GetPointOnEdge(previousVertice, currentVertice, sharedVertices[0], sharedVertices[1], out Vector3 pointOnEdge)) {
                            if ((pointOnEdge.y- previousVertice.y) > distance)
                            {
                                positions.Add(pointOnEdge);
                            }
                            else if ((pointOnEdge.y - currentVertice.y) > distance)
                            {
                                positions.Add(pointOnEdge);
                            }
                        }
                    }

                    counterA++;

                    previousVertice = temporaryPoint;
                    previousIndexTriangle = temporaryIndexTriangle;

                    positions.Add(temporaryPoint);

                    if (counterA >= 10) {
                        print("counterA : " + counterA);
                    }
                }
            }

            roads.Add(new Road(positions, largeur, nom, importance, id));
        }
    }




    private bool GetPointOnEdge(Vector3 roadA, Vector3 roadB, Vector3 edgeA, Vector3 edgeB, out Vector3 pointOnEdge) {

        pointOnEdge = Vector3.negativeInfinity;

        if (roadA == Vector3.negativeInfinity) {
            return false;
        }

        edgeA = new Vector3(edgeA.x - offset.x,  edgeA.z - offset.z, -edgeA.y + offset.y);
        edgeB = new Vector3(edgeB.x - offset.x,  edgeB.z - offset.z, -edgeB.y + offset.y);


        Vector2 firstPoint_road2D = new Vector2(roadA.x, roadA.z);
        Vector2 secondPoint_road2D = new Vector2(roadB.x, roadB.z);

        Vector2 firstPoint_Edge2D = new Vector2(edgeA.x, edgeA.z);
        Vector2 secondPoint_Edge2D = new Vector2(edgeB.x, edgeB.z);

        Debug.Log("firstPoint_Edge2D : "+firstPoint_Edge2D);
        Debug.Log("secondPoint_Edge2D : "+secondPoint_Edge2D);

        Vector2 intersection = GetIntersectionPointCoordinates(firstPoint_road2D, secondPoint_road2D, firstPoint_Edge2D, secondPoint_Edge2D, out bool found);
        Debug.Log("intersection : " + intersection);
        if (found) {
            float t = (intersection.x - firstPoint_Edge2D.x) / (secondPoint_Edge2D.x - firstPoint_Edge2D.x);
            pointOnEdge = (edgeA + (edgeB - edgeA) * t);
            Debug.Log("pointOnEdge : " + pointOnEdge);
            return true;
        }

        return false;
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
        return 3;
    }

    /// <summary>Get the Y coordinate which will fit with the terrain depending on X & Z or 0.0</summary>
    private float GetAltitudeFromMap(Vector3 pos, out int indexTriangle) {
        RaycastHit hit;
        indexTriangle = 0;
        if (Physics.Raycast(pos, Vector3.down, out hit)) {
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
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
    [SerializeField]
    private bool drawSpheres;

    Mesh msh;

    Vector3[] verticies;
    int[] triangles;

    int total_vert;
    int total_tri;

    void Start() {
        roads = new List<Road>();
        root = JsonParser.Parse("Assets/Data/" + file);

        if (fixTerrainCoord)
            FixTerrainCoord();

        SetUpRoads();
        DrawRoads();

        //ADDING THE ROAD MESH

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
        foreach (Road r in roads)
        {
            GameObject emptyRoadGO = Instantiate(emptyRoad);
            LineRenderer lr = emptyRoadGO.GetComponent<LineRenderer>();

            foreach (Vector3 v in r.positions)
            {
                lr.positionCount++;
                lr.SetPosition(lr.positionCount - 1, v);
            }
            lr.name = r.nom;
            lr.widthMultiplier = r.largeur;

        }
        InitMesh();

        DrawMesh();

    }

    /// <summary> Create roads objects from JSON </summary>
    void SetUpRoads() {
        float xOffset = (float)root.features[0].geometry.coordinates[0][0][0];
        float zOffset = (float)root.features[0].geometry.coordinates[0][0][1];

        // For each enties (roads) in the json
        foreach (Feature f in root.features) {
            int previousIndexTriangle = int.MinValue;
            int currentIndexTriangle = 0;

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

                    while ((amountSharedVertices < 2 && previousIndexTriangle != temporaryIndexTriangle) && counterB < 100) {
                        Vector3 verticeInTheMiddle = previousVertice + (temporaryPoint - previousVertice) * 0.5f;
                        verticeInTheMiddle.y = GetAltitudeFromMap(new Vector3(verticeInTheMiddle.x, 9999, verticeInTheMiddle.z), out temporaryIndexTriangle);

                        amountSharedVertices = ManageTriangleIntersection(previousIndexTriangle, temporaryIndexTriangle, out sharedVertices);

                        temporaryPoint = verticeInTheMiddle;

                        counterB++;

                        if (counterB >= 100) {
                            print("counterB : " + counterB);

                            print("previousVertice : " + previousVertice);
                            print("currentVertice : " + currentVertice);
                            print("temporaryPoint : " + temporaryPoint);

                            print("amountSharedVertices : " + amountSharedVertices);
                        }
                    }

                    counterA++;

                    previousVertice = temporaryPoint;
                    previousIndexTriangle = temporaryIndexTriangle;

                    positions.Add(temporaryPoint);

                    if (counterA >= 100) {
                        print("counterA : " + counterA);
                    }
                }


                /*if (previousIndexTriangle == currentIndexTriangle) {
                    // On ajoute : is ok
                } else if (ManageTriangleIntersection(previousIndexTriangle, currentIndexTriangle, out List<Vector3> sharedVertices) == 2) { // Deux triangles consecutifs
                    // On ajoute un point sur l'arrete
                } else {
                    // On ajoute un point au milieu
                }*/




                /*bool addPoint = false;
                int nbIntersection = ManageTriangleIntersection(previousIndexTriangle, currentIndexTriangle, out List<Vector3> sharedVertices);
                if (nbIntersection < 2)
                {
                    Vector3 verticeInTheMiddle = (currentVertice - previousVertice) * 0.5f;
                    verticeInTheMiddle.y = GetAltitudeFromMap(new Vector3(verticeInTheMiddle.x, 9999, verticeInTheMiddle.z), out currentIndexTriangle);
                    currentVertice = verticeInTheMiddle;

                    addPoint = true;
                } else if (nbIntersection == 2) {

                }
                else{
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
                

                if(!addPoint)
                { 
                    previousVertice = currentVertice;
                    previousIndexTriangle = currentIndexTriangle;
                    positions.Add(new Vector3(x, y, z));
                }*/
                /*previousVertice = currentVertice;
                previousIndexTriangle = currentIndexTriangle;
                positions.Add(new Vector3(x, y, z));*/
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

    private void InitMesh()
    {

        total_vert = 0;
        total_tri = 0;
        foreach (Road road in roads)
        {
            total_vert += road.positions.Count * 2;
            //total_tri += (road.positions.Count - 1) * 6; 
        }

        total_tri = (total_vert - 2) * 3;
        Debug.Log("total verts " + total_vert);
        Debug.Log("total triangles " + total_tri);
    }
    private void DrawMesh ()
    {
        msh = new Mesh();
        GetComponent<MeshFilter>().mesh = msh;


        //int rpcount = road.positions.Count;
        verticies = new Vector3[total_vert];
        triangles = new int[total_tri];
        //triangles = new int[(verticies.Length - 2) * 3];

        int vert_inc = 0;

        foreach (Road road in roads)
        {
            int rpcount = road.positions.Count;

            for (int i = 0; i < road.positions.Count - 1; i+=2)
            {
                verticies[vert_inc + i] = road.positions[i] + Quaternion.AngleAxis(90, Vector3.up) * ((road.positions[i + 1] - road.positions[i]).normalized);
                verticies[vert_inc + i + 1] = road.positions[i] + Quaternion.AngleAxis(-90, Vector3.up) * ((road.positions[i + 1] - road.positions[i]).normalized);
            

            }
            verticies[vert_inc + rpcount - 2] = road.positions[rpcount - 1] + Quaternion.AngleAxis(90, Vector3.up) * ((road.positions[rpcount - 1] - road.positions[rpcount - 2]).normalized);
            verticies[vert_inc + rpcount - 1] = road.positions[rpcount - 1] + Quaternion.AngleAxis(-90, Vector3.up) * ((road.positions[rpcount - 1] - road.positions[rpcount - 2]).normalized);

            vert_inc += rpcount;

        }

        int tri_counter = 0;

        int cp = 0;

        foreach (Road road in roads)
        {
            //int triangles_lenght = 6 * (road.positions.Count - 1);
            int triangles_lenght = 6 * (roads[0].positions.Count - 1);
            for (int i = 0, tri = 0; i < triangles_lenght - 5; i += 6, tri += 2)
            {
                triangles[tri_counter + i] = tri_counter + tri;
                triangles[tri_counter + i + 1] = tri_counter + tri + 1;
                triangles[tri_counter + i + 2] = tri_counter + tri + 2;
                triangles[tri_counter + i + 3] = tri_counter + tri + 3;
                triangles[tri_counter + i + 4] = tri_counter + tri + 2;
                triangles[tri_counter + i + 5] = tri_counter + tri + 1;

                Debug.Log($"{triangles[tri_counter + i]}, {triangles[tri_counter + i + 1]}, {triangles[tri_counter + i + 2]}," +
                    $"{triangles[tri_counter + i + 3]}, {triangles[tri_counter + i + 4]}, {triangles[tri_counter + i + 5]}");
            }
            Debug.Log(cp++);
            
            tri_counter += triangles_lenght;

        }

        msh.Clear();
        msh.vertices = verticies;
        msh.triangles = triangles;

    }

    private void OnDrawGizmos()
    {
        foreach (Vector3 vec in verticies)
        {
            Gizmos.DrawSphere(vec, .1f);
        }
    }
}
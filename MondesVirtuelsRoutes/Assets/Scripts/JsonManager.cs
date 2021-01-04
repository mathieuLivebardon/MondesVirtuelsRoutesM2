using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JsonManager : MonoBehaviour {
    void Start() {
        Root r = JsonParser.Parse("Assets/Data/Route_Primaire_Zone_Amphi.geojson.json");
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Road {

    public string id { get; }
    public int importance { get; }
    public float largeur { get; }
    public List<Vector3> positions { get; }

    public Road(List<Vector3> positions, float largeur = 5, int importance = 2, string id = "") {
        this.id = id;
        this.importance = importance;
        this.largeur = largeur;
        this.positions = positions;
    }

    public override string ToString() {
        string print = "[" + id + "] ";
        print += importance + " - " + largeur + " (" + positions.Count + ")";
        return print;
    }
}

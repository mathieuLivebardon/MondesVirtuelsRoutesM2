using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;


public class JsonParser {
    public static Root Parse(string path) {
        try {
            StreamReader sr = new StreamReader(path);
            string strFile = sr.ReadToEnd();
            strFile = strFile.Replace("\n", "");
            strFile = strFile.Replace("\t", "");

            return JsonConvert.DeserializeObject<Root>(strFile);

        } catch (Exception e) {
            Debug.LogError("The file could not be read:");
            Debug.LogException(e);
        }
        return null;
    }
}

public class Properties {
    public string ID { get; set; }
    public double PREC_PLANI { get; set; }
    public double PREC_ALTI { get; set; }
    public string NATURE { get; set; }
    public string NUMERO { get; set; }
    public string NOM_VOIE_G { get; set; }
    public string NOM_VOIE_D { get; set; }
    public string IMPORTANCE { get; set; }
    public string CL_ADMIN { get; set; }
    public string GESTION { get; set; }
    public string MISE_SERV { get; set; }
    public string IT_VERT { get; set; }
    public string IT_EUROP { get; set; }
    public string FICTIF { get; set; }
    public string FRANCHISST { get; set; }
    public double LARGEUR { get; set; }
    public string NOM_ITI { get; set; }
    public int NB_VOIES { get; set; }
    public int POS_SOL { get; set; }
    public string SENS { get; set; }
    public object ALIAS_G { get; set; }
    public object ALIAS_D { get; set; }
    public string INSEECOM_G { get; set; }
    public string INSEECOM_D { get; set; }
    public string CODEVOIE_G { get; set; }
    public string CODEVOIE_D { get; set; }
    public string CODEPOST_G { get; set; }
    public string CODEPOST_D { get; set; }
    public string TYP_ADRES { get; set; }
    public int BORNEDEB_G { get; set; }
    public int BORNEDEB_D { get; set; }
    public int BORNEFIN_G { get; set; }
    public int BORNEFIN_D { get; set; }
    public string ETAT { get; set; }
    public double Z_INI { get; set; }
    public double Z_FIN { get; set; }
}

public class Geometry {
    public string type { get; set; }
    public List<List<List<double>>> coordinates { get; set; }
}

public class Feature {
    public string type { get; set; }
    public Properties properties { get; set; }
    public Geometry geometry { get; set; }
}

public class Root {
    public string type { get; set; }
    public string name { get; set; }
    public List<Feature> features { get; set; }
}

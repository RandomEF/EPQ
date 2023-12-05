using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SpecularProof : MonoBehaviour
{
    public TMP_Text text;
    public GameObject obj;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
            float smoothness = obj.GetComponent<Renderer>().material.GetFloat("_Glossiness");
            text.text = smoothness.ToString();
    }
}

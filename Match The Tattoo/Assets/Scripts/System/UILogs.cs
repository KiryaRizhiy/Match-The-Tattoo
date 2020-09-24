using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UILogs : MonoBehaviour
{
    private Text text;

    void Start()
    {
        text = GetComponent<Text>();
    }

    // Update is called once per frame
    void Update()
    {
        if (text != null)
            text.text = Logger.GetAllContent();
        else
            GetComponent<InputField>().text = Logger.GetAllContent();
    }
}

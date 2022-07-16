using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PageSwitcher : MonoBehaviour
{
    GameObject[] children = null;

    void Awake()
    {
        int numChildren = transform.childCount;
        children = new GameObject[numChildren];
        for (int i = 0; i < numChildren; i++)
        {
            children[i] = transform.GetChild(i).gameObject;
            children[i].SetActive(i == 0);
        };
    }

    public void SetActivePage(GameObject activePage)
    {
        foreach(GameObject child in children)
        {
            child.SetActive(child == activePage);
        }
    }
}

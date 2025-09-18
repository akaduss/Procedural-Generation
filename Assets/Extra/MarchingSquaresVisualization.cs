using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;
 
public class MarchingSquaresVisualization : MonoBehaviour
{

    List<Vector3> vertices;
    List<List<int>> triangles;
    public GraphicNode[] nodes;
    public Transform[] nodeP;

    public TextMeshProUGUI binaryText;
    public TextMeshProUGUI configText;

    public void SelectNode(int index)
    {
        nodes[index].Toggle();
        int val = 0;
        int n = 8;
        string binary = "";
        for (int i = 0; i < nodes.Length; i++)
        {
            if (nodes[i].isControl)
            {
                if (nodes[i].active)
                {
                    binary += "1";
                    val += n;
                }
                else
                {
                    binary += "0";
                }
                n = n >> 1;
                //print (n);
            }
        }
        //print (val);
        binaryText.text = binary;
        configText.text = "=" + val;
        Triangulate(val);

        Mesh mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = mesh;
        GetComponent<MeshRenderer>();

        mesh.subMeshCount = triangles.Count;
        mesh.vertices = vertices.ToArray();
        for (int i = 0; i < triangles.Count; i++)
        {
            mesh.SetTriangles(triangles[i].ToArray(), i);
        }


        //mesh.triangles = triangles.ToArray();
        mesh.RecalculateNormals();
    }

    void Triangulate(int configuration)
    {
        for (int i = 0; i < nodes.Length; i++)
        {
            if (nodes[i].isControl == false)
            {
                nodes[i].Clear();
            }
        }
        vertices = new List<Vector3>();
        triangles = new List<List<int>>();

        switch (configuration)
        {
            case 0: // 0000
                return;
            case 15: // 1111
                MeshFromPoints(nodeP[0].position, nodeP[2].position, nodeP[4].position, nodeP[6].position);
                Active(0, 2, 4, 6);
                break;

            // 1 point:
            case 8: // 1000
                    //print ("AWE");
                MeshFromPoints(nodeP[0].position, nodeP[1].position, nodeP[7].position);
                Active(0, 1, 7);
                break;
            case 4: // 0100
                MeshFromPoints(nodeP[1].position, nodeP[2].position, nodeP[3].position);
                Active(1, 2, 3);
                break;
            case 2: // 0010
                MeshFromPoints(nodeP[3].position, nodeP[4].position, nodeP[5].position);
                Active(3, 4, 5);
                break;
            case 1: // 0001
                MeshFromPoints(nodeP[5].position, nodeP[6].position, nodeP[7].position);
                Active(5, 6, 7);
                break;

            // 2 points:
            case 12: // 1100
                MeshFromPoints(nodeP[0].position, nodeP[2].position, nodeP[3].position, nodeP[7].position);
                Active(0, 2, 3, 7);
                break;
            case 9: // 1001
                Active(0, 1, 5, 6);
                MeshFromPoints(nodeP[0].position, nodeP[1].position, nodeP[5].position, nodeP[6].position);
                break;
            case 6: // 0110
                Active(1, 2, 4, 5);
                MeshFromPoints(nodeP[1].position, nodeP[2].position, nodeP[4].position, nodeP[5].position);
                break;
            case 3: // 0011
                Active(3, 4, 6, 7);
                MeshFromPoints(nodeP[3].position, nodeP[4].position, nodeP[6].position, nodeP[7].position);
                break;
            case 10: // 1010
                Active(0, 1, 3, 4, 5, 7);
                MeshFromPoints(nodeP[0].position, nodeP[1].position, nodeP[3].position, nodeP[4].position, nodeP[5].position, nodeP[7].position);
                break;
            case 5: // 0101
                Active(1, 2, 3, 5, 6, 7);
                MeshFromPoints(nodeP[1].position, nodeP[2].position, nodeP[3].position, nodeP[5].position, nodeP[6].position, nodeP[7].position);
                break;

            // 3 points:
            case 14: // 1110
                Active(0, 2, 4, 5, 7);
                MeshFromPoints(nodeP[0].position, nodeP[2].position, nodeP[4].position, nodeP[5].position, nodeP[7].position);
                break;
            case 11: // 1011
                Active(0, 1, 3, 4, 6);
                MeshFromPoints(nodeP[0].position, nodeP[1].position, nodeP[3].position, nodeP[4].position, nodeP[6].position);
                break;
            case 7: // 0111
                Active(1, 2, 4, 6, 7);
                MeshFromPoints(nodeP[1].position, nodeP[2].position, nodeP[4].position, nodeP[6].position, nodeP[7].position);
                break;
            case 13: // 1101
                Active(0, 2, 3, 5, 6);
                MeshFromPoints(nodeP[0].position, nodeP[2].position, nodeP[3].position, nodeP[5].position, nodeP[6].position);
                break;
        }
    }

    void Active(params int[] a)
    {
        string letters = "ABCDEFGHIJKLMOP";
        for (int i = 0; i < a.Length; i++)
        {
            nodes[a[i]].SetActive();
            nodes[a[i]].text.text = letters[i] + "";
        }
    }

    void MeshFromPoints(params Vector3[] nodes)
    {
        for (int i = 0; i < nodes.Length; i++)
        {
            //print (nodes[i]);
        }
        if (nodes.Length >= 3)
            CreateTriangle(nodes[0], nodes[1], nodes[2]);
        if (nodes.Length >= 4)
            CreateTriangle(nodes[0], nodes[2], nodes[3]);
        if (nodes.Length >= 5)
            CreateTriangle(nodes[0], nodes[3], nodes[4]);
        if (nodes.Length >= 6)
            CreateTriangle(nodes[0], nodes[4], nodes[5]);
    }


    void CreateTriangle(Vector3 a, Vector3 b, Vector3 c)
    {
        vertices.Add(a);
        vertices.Add(b);
        vertices.Add(c);

        List<int> t = new List<int>();

        t.Add(vertices.Count - 3);
        t.Add(vertices.Count - 2);
        t.Add(vertices.Count - 1);
        triangles.Add(t);
    }

}
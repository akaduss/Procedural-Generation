using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;

public class MeshGenerator : MonoBehaviour
{

    [SerializeField] private MeshFilter wallsMeshFilter;
    private SquareGrid squareGrid;
    private List<Vector3> vertices;
    private List<int> triangles;

    private Dictionary<int, List<Triangle>> triangleDict = new Dictionary<int, List<Triangle>>();
    private List<List<int>> outlines = new List<List<int>>();
    private HashSet<int> checkedVertices = new HashSet<int>();

    private const int VERTEX_INDEX_NOT_ASSIGNED = -1;

    public void GenerateMesh(int[,] map, float squareSize)
    {
        triangleDict.Clear();
        outlines.Clear();
        checkedVertices.Clear();

        squareGrid = new SquareGrid(map, squareSize);
        vertices = new List<Vector3>();
        triangles = new List<int>();

        for (int x = 0; x < squareGrid.squares.GetLength(0); x++)
        {
            for (int y = 0; y < squareGrid.squares.GetLength(1); y++)
            {
                TriangulateSquare(squareGrid.squares[x, y]);
            }
        }

        Mesh generatedMesh = new Mesh();
        GetComponent<MeshFilter>().mesh = generatedMesh;

        generatedMesh.vertices = vertices.ToArray();
        generatedMesh.triangles = triangles.ToArray();
        generatedMesh.RecalculateNormals();

        int tileAmount = 10;
        Vector2[] uvs = new Vector2[vertices.Count];
        for(int i = 0; i < vertices.Count; i++)
        {
            float percentX = Mathf.InverseLerp(-map.GetLength(0) / 2 * squareSize, map.GetLength(0) / 2 * squareSize, vertices[i].x) * tileAmount;
            float percentY = Mathf.InverseLerp(-map.GetLength(0) / 2 * squareSize, map.GetLength(0) / 2 * squareSize, vertices[i].z) * tileAmount;
            uvs[i] = new Vector2(percentX, percentY);
        }
        generatedMesh.uv = uvs;

        CreateWallMesh();

    }

    private void CreateWallMesh()
    {
        CalculateMeshOutline();

        List<Vector3> wallVertices = new List<Vector3>();
        List<int> wallTriangles = new List<int>();
        Mesh wallMesh = new Mesh();
        float wallHeight = 5f;

        foreach (List<int> outline in outlines)
        {
            for(int i = 0; i < outline.Count - 1; i++)
            {
                int startIndex = wallVertices.Count;
                wallVertices.Add(vertices[outline[i]]); // left
                wallVertices.Add(vertices[outline[i + 1]]); // right 
                wallVertices.Add(vertices[outline[i]] - Vector3.up * wallHeight); // bottom left
                wallVertices.Add(vertices[outline[i + 1]] - Vector3.up * wallHeight); // bottom right

                wallTriangles.Add(startIndex + 0);
                wallTriangles.Add(startIndex + 2);
                wallTriangles.Add(startIndex + 3);

                wallTriangles.Add(startIndex + 3);
                wallTriangles.Add(startIndex + 1);
                wallTriangles.Add(startIndex + 0);


            }
        }

        wallMesh.vertices = wallVertices.ToArray();
        wallMesh.triangles = wallTriangles.ToArray();
        wallsMeshFilter.mesh = wallMesh;

        MeshCollider wallCollider = wallsMeshFilter.gameObject.AddComponent<MeshCollider>();
        wallCollider.sharedMesh = wallMesh;


    }

    private void TriangulateSquare(Square square)
    {
        switch (square.configuration)
        {
            case 0:
                break;
            case 1:
                MeshFromPoints(square.centerLeft, square.centerBottom, square.bottomLeft);
                break;
            case 2:
                MeshFromPoints(square.bottomRight, square.centerBottom, square.centerRight);
                break;
            case 3:
                MeshFromPoints(square.centerRight, square.bottomRight, square.bottomLeft, square.centerLeft);
                break;

            //Outline directions inconsistent??
            //case 1:
            //    MeshFromPoints(square.centerBottom, square.bottomLeft, square.centerLeft); 
            //    break;
            //case 2:
            //    MeshFromPoints(square.centerRight, square.bottomRight, square.centerBottom);
            //    break;
            //case 4:
            //    MeshFromPoints(square.centerTop, square.topRight, square.centerRight);
            //    break;
            case 4:
                MeshFromPoints(square.topRight, square.centerRight, square.centerTop);
                break;
            case 5:
                MeshFromPoints(square.centerTop, square.topRight, square.centerRight, square.centerBottom, square.bottomLeft, square.centerLeft);
                break;
            case 6:
                MeshFromPoints(square.centerTop, square.topRight, square.bottomRight, square.centerBottom);
                break;
            case 7:
                MeshFromPoints(square.centerTop, square.topRight, square.bottomRight, square.bottomLeft, square.centerLeft);
                break;
            case 8:
                MeshFromPoints(square.topLeft, square.centerTop, square.centerLeft);
                break;
            case 9:
                MeshFromPoints(square.topLeft, square.centerTop, square.centerBottom, square.bottomLeft);
                break;
            case 10:
                MeshFromPoints(square.topLeft, square.centerTop, square.centerRight, square.bottomRight, square.centerBottom, square.centerLeft);
                break;
            case 11:
                MeshFromPoints(square.topLeft, square.centerTop, square.centerRight, square.bottomRight, square.bottomLeft);
                break;
            case 12:
                MeshFromPoints(square.topLeft, square.topRight, square.centerRight, square.centerLeft);
                break;
            case 13:
                MeshFromPoints(square.topLeft, square.topRight, square.centerRight, square.centerBottom, square.bottomLeft);
                break;
            case 14:
                MeshFromPoints(square.topLeft, square.topRight, square.bottomRight, square.centerBottom, square.centerLeft);
                break;
            case 15:
                MeshFromPoints(square.topLeft, square.topRight, square.bottomRight, square.bottomLeft);
                checkedVertices.Add(square.topLeft.vertexIndex);
                checkedVertices.Add(square.topRight.vertexIndex);
                checkedVertices.Add(square.bottomRight.vertexIndex);
                checkedVertices.Add(square.bottomLeft.vertexIndex);
                break;
            default:
                break;
        }
    }

    private void MeshFromPoints(params Node[] points)
    {
        AssignVertices(points);

        if (points.Length >= 3)
            CreateTriangle(points[0], points[1], points[2]);
        if (points.Length >= 4)
            CreateTriangle(points[0], points[2], points[3]);
        if (points.Length >= 5)
            CreateTriangle(points[0], points[3], points[4]);
        if (points.Length >= 6)
            CreateTriangle(points[0], points[4], points[5]);
    }

    private void AssignVertices(Node[] points)
    {
        for (int i = 0; i < points.Length; i++)
        {
            if (points[i].vertexIndex == -1)
            {
                points[i].vertexIndex = vertices.Count;
                vertices.Add(points[i].position);
            }
        }
    }

    private void CreateTriangle(Node a, Node b, Node c)
    {
        triangles.Add(a.vertexIndex);
        triangles.Add(b.vertexIndex);
        triangles.Add(c.vertexIndex);

        Triangle triangle = new Triangle(a.vertexIndex, b.vertexIndex, c.vertexIndex);
        AddTriangleToDictionary(triangle.vertexIndexA, triangle);
        AddTriangleToDictionary(triangle.vertexIndexB, triangle);
        AddTriangleToDictionary(triangle.vertexIndexC, triangle);
    }

    private void CalculateMeshOutline()
    {
        for (int vertexIndex = 0; vertexIndex < vertices.Count; vertexIndex++)
        {
            if(checkedVertices.Contains(vertexIndex) == false)
            {
                int newOutlineVertex = GetConnectedOutlineVertex(vertexIndex);
                if (newOutlineVertex != VERTEX_INDEX_NOT_ASSIGNED)
                {
                    checkedVertices.Add(vertexIndex);

                    List<int> newOutline = new List<int>();
                    newOutline.Add(vertexIndex);
                    outlines.Add(newOutline);
                    FollowOutline(newOutlineVertex, outlines.Count - 1);
                    outlines[outlines.Count - 1].Add(vertexIndex);
                }
            }
        }
    }

    private void FollowOutline(int vertexIndex, int outlineIndex)
    {
        outlines[outlineIndex].Add(vertexIndex);
        checkedVertices.Add(vertexIndex);
        int nextVertexIndex = GetConnectedOutlineVertex(vertexIndex);

        if(nextVertexIndex != VERTEX_INDEX_NOT_ASSIGNED)
        {
            FollowOutline(nextVertexIndex, outlineIndex);
        }
    }

    private void AddTriangleToDictionary(int vertexIndexKey, Triangle triangle)
    {
        if (triangleDict.ContainsKey(vertexIndexKey))
        {
            triangleDict[vertexIndexKey].Add(triangle);
        }
        else
        {
            List<Triangle> triangleList = new List<Triangle>();
            triangleList.Add(triangle);
            triangleDict.Add(vertexIndexKey, triangleList);
        }
    }

    //My function
    private bool IsOutlineEdge(int vertexA, int vertexB)
    {
        int sharedTriangleCount = 0;
        foreach (var triangle in triangleDict[vertexA])
        {
            if (triangle.Contains(vertexB))
                sharedTriangleCount++;
            if (sharedTriangleCount > 1)
                break;
        }

        return sharedTriangleCount == 1;
    }

    //private bool IsOutlineEdge(int vertexA, int vertexB)
    //{
    //    List<Triangle> trianglesContainingVertexA = triangleDict[vertexA];
    //    int sharedTriangleCount = 0;

    //    for (int i = 0; i < trianglesContainingVertexA.Count; i++)
    //    {
    //        if (trianglesContainingVertexA[i].Contains(vertexB))
    //        {
    //            sharedTriangleCount++;
    //            if (sharedTriangleCount > 1)
    //                break;
    //        }
    //    }
    //
    //    return sharedTriangleCount == 1;
    //}

    private int GetConnectedOutlineVertex(int vertexIndex)
    {
        List<Triangle> trianglesContainingVertex = triangleDict[vertexIndex];

        for (int i = 0; i < trianglesContainingVertex.Count; i++)
        {
            Triangle triangle = trianglesContainingVertex[i];

            for (int j = 0; j < 3; j++)
            {
                int vertexB = triangle[j];
                if (vertexB != vertexIndex && !checkedVertices.Contains(vertexB))
                {
                    if (IsOutlineEdge(vertexIndex, vertexB))
                        return vertexB;
                }
            }
        }

        return VERTEX_INDEX_NOT_ASSIGNED;
    }

    private int GetConnectedOutlineVertex2(int vertexIndex)
    {
        List<Triangle> trianglesContainingVertex = triangleDict[vertexIndex];

        foreach (var triangle in trianglesContainingVertex)
        {
            foreach (var vertIndex in triangle.vertices)
            {
                if(IsOutlineEdge(vertexIndex, vertIndex))
                    return vertIndex;
            }
        }

        return VERTEX_INDEX_NOT_ASSIGNED;
    }

    public class SquareGrid
    {
        public Square[,] squares;

        public SquareGrid(int[,] map, float squareSize)
        {
            int controlNodeCountX = map.GetLength(0);
            int controlNodeCountY = map.GetLength(1);
            float mapWidth = controlNodeCountX * squareSize;
            float mapHeight = controlNodeCountY * squareSize;

            ControlNode[,] controlNodes = new ControlNode[controlNodeCountX, controlNodeCountY];

            for (int x = 0; x < controlNodeCountX; x++)
            {
                for (int y = 0; y < controlNodeCountY; y++)
                {
                    Vector3 position = new(-mapWidth / 2 + x * squareSize + squareSize / 2, 0,
                        -mapHeight / 2 + y * squareSize + squareSize / 2);

                    bool isActive = map[x, y] == 1;
                    controlNodes[x, y] = new ControlNode(position, isActive, squareSize);
                }
            }

            squares = new Square[controlNodeCountX - 1, controlNodeCountY - 1];
            for (int x = 0; x < controlNodeCountX - 1; x++)
            {
                for (int y = 0; y < controlNodeCountY - 1; y++)
                {
                    squares[x, y] = new Square(controlNodes[x, y + 1], controlNodes[x + 1, y + 1], controlNodes[x + 1, y], controlNodes[x, y]);
                }
            }
        }

    }

    public class Square
    {
        public ControlNode topLeft, topRight, bottomRight, bottomLeft;
        public Node centerTop, centerRight, centerBottom, centerLeft;
        public int configuration;

        public Square(ControlNode topLeft, ControlNode topRight, ControlNode bottomRight, ControlNode bottomLeft)
        {
            this.topLeft = topLeft;
            this.topRight = topRight;
            this.bottomRight = bottomRight;
            this.bottomLeft = bottomLeft;

            centerTop = this.topLeft.right;
            centerRight = this.bottomRight.above;
            centerBottom = this.bottomLeft.right;
            centerLeft = this.bottomLeft.above;

            if (topLeft.isActive) configuration += 8;
            if (topRight.isActive) configuration += 4;
            if (bottomRight.isActive) configuration += 2;
            if (bottomLeft.isActive) configuration += 1;

        }
    }

    public class Node
    {
        public Vector3 position;
        public int vertexIndex;

        public Node(Vector3 position)
        {
            this.position = position;
            this.vertexIndex = VERTEX_INDEX_NOT_ASSIGNED;
        }

    }

    public class ControlNode : Node
    {
        public bool isActive;
        public Node above, right;

        public ControlNode(Vector3 position, bool isActive, float squareSize) : base(position)
        {
            this.isActive = isActive;
            above = new Node(position + Vector3.forward * squareSize / 2f);
            right = new Node(position + Vector3.right * squareSize / 2f);
        }
    }

    private void OnDrawGizmos()
    {
        //if (squareGrid == null)
        //    return;


        //for (int x = 0; x < squareGrid.squares.GetLength(0); x++)
        //{
        //    for (int y = 0; y < squareGrid.squares.GetLength(1); y++)
        //    {
        //        Gizmos.color = squareGrid.squares[x, y].topLeft.isActive ? Color.black : Color.white;
        //        Gizmos.DrawCube(squareGrid.squares[x, y].topLeft.position, Vector3.one * .4f);

        //        Gizmos.color = squareGrid.squares[x, y].topRight.isActive ? Color.black : Color.white;
        //        Gizmos.DrawCube(squareGrid.squares[x, y].topRight.position, Vector3.one * .4f);

        //        Gizmos.color = squareGrid.squares[x, y].bottomRight.isActive ? Color.black : Color.white;
        //        Gizmos.DrawCube(squareGrid.squares[x, y].bottomRight.position, Vector3.one * .4f);

        //        Gizmos.color = squareGrid.squares[x, y].bottomLeft.isActive ? Color.black : Color.white;
        //        Gizmos.DrawCube(squareGrid.squares[x, y].bottomLeft.position, Vector3.one * .4f);

        //        Gizmos.color = Color.grey;
        //        Gizmos.DrawCube(squareGrid.squares[x, y].centerTop.position, Vector3.one * .1f);
        //        Gizmos.DrawCube(squareGrid.squares[x, y].centerLeft.position, Vector3.one * .1f);
        //        Gizmos.DrawCube(squareGrid.squares[x, y].centerRight.position, Vector3.one * .1f);
        //        Gizmos.DrawCube(squareGrid.squares[x, y].centerBottom.position, Vector3.one * .1f);

        //    }
        //}

    }

    private struct Triangle
    {
        public int vertexIndexA;
        public int vertexIndexB;
        public int vertexIndexC;
        public int[] vertices;

        public Triangle(int a, int b, int c)
        {
            vertexIndexA = a;
            vertexIndexB = b;
            vertexIndexC = c;

            vertices = new int[3];
            vertices[0] = a;
            vertices[1] = b;
            vertices[2] = c;
        }

        public readonly int this[int i]
        {
            get
            {
                return vertices[i];
            }
        }

        public readonly bool Contains(int vertexIndex)
        {
            return vertexIndex == vertexIndexA || vertexIndex == vertexIndexB || vertexIndex == vertexIndexC;
        }
    }

}

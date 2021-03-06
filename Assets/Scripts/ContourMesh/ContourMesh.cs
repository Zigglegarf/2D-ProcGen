﻿using System.Collections.Generic;
using UnityEngine;

public class ContourMesh
{
    public MeshData mesh;
    private List<MarchingSquare> SquareGrid;
    private List<Vector3> verticies;
    private List<int> triangles;
    private int meshWidth, meshHeight, xOffset, zOffset;
    private bool[,] map, simpleGeometry;
    private float width, height, squareSize;
    private bool drawContours = true, drawSimple = true, uniqueVertcies = true;

    public ContourMesh(bool[,] _map, float _squareSize, bool _drawContours = true, bool _drawSimple = true, bool _uniqueVerticies = true, int _xOffset = 0, int _zOffset = 0)
    {
        mesh = new MeshData();
        map = _map;
        squareSize = _squareSize;

        meshWidth = map.GetLength(0) / 2;
        meshHeight = map.GetLength(1) / 2;

        width = meshWidth * squareSize;
        height = meshHeight * squareSize;

        drawContours = _drawContours;
        drawSimple = _drawSimple;
        uniqueVertcies = _uniqueVerticies;

        xOffset = _xOffset;
        zOffset = _zOffset;
    }

    public MeshData GetMeshData()
    {
        GetGeometry();

        // Really slow
        if (uniqueVertcies)
            GetUniqueVerticies();

       // Debug.Log("Verticies: " + verticies.Count);

        mesh.verticies = verticies;
        mesh.triangles = triangles;

        return mesh;
    }

    private void GetGeometry()
    {
        verticies = new List<Vector3>();
        triangles = new List<int>();

        if (drawSimple)
            simpleGeometry = new bool[meshWidth, meshHeight];

        if (drawContours)
            SquareGrid = new List<MarchingSquare>();

        if (drawContours || drawSimple)
        {
            for (int a = 0; a < meshWidth; a++)
            {
                for (int b = 0; b < meshHeight; b++)
                {
                    bool pointA = map[(2 * a), (2 * b) + 1],
                         pointB = map[(2 * a) + 1, (2 * b) + 1],
                         pointC = map[(2 * a) + 1, (2 * b)],
                         pointD = map[(2 * a), (2 * b)];

                    if (pointA && pointB && pointC && pointD)
                    {
                        if (drawSimple)
                            simpleGeometry[a, b] = true;
                    }
                    else if (drawContours)
                    {
                        Vector3 pos = new Vector3(((xOffset + a) * squareSize) - (width / 2), 0, ((b + zOffset) * squareSize) - (height / 2));
                        SquareGrid.Add(new MarchingSquare(pos, squareSize, pointA, pointB, pointC, pointD));
                    }
                }
            }
        }

        if (drawSimple)
            GetSimpleGeometry();

        if (drawContours)
            GetContourGeometry();
    }

    private void GetSimpleGeometry()
    {
        bool newZ = true;
        MapData workspace;
        List<MapData> zStart = new List<MapData>();
        List<MapData> zEnd = new List<MapData>();

        for (int a = 0; a < simpleGeometry.GetLength(0); a++)
        {
            newZ = true;

            for (int b = 0; b < simpleGeometry.GetLength(1); b++)
            {
                if (simpleGeometry[a, b] && b < simpleGeometry.GetLength(1) - 1)
                {
                    if (newZ)
                    {
                        workspace.x = a;
                        workspace.z = b;

                        zStart.Add(workspace);
                        newZ = false;
                    }
                }
                else if (!newZ)
                {
                    workspace.x = a;

                    if (!simpleGeometry[a, b])
                        workspace.z = b - 1;
                    else                  
                        workspace.z = b;
                    
                    zEnd.Add(workspace);
                    newZ = true;
                }

                if (simpleGeometry[a, b] && b == simpleGeometry.GetLength(1) - 1 && !simpleGeometry[a, b - 1])
                {
                    workspace.x = a;
                    workspace.z = b;

                    zStart.Add(workspace);
                    zEnd.Add(workspace);
                }
            }
        }

        GetTrianglesFromIndicies(zStart, zEnd);
    }

    private void GetTrianglesFromIndicies(List<MapData> zStart, List<MapData> zEnd)
    {
        List<Vector3> rectangles = new List<Vector3>();
        Vector3 a, b, c, d;

        for (int n = 0; n < zStart.Count; n++)
        {
            Vector3 startCenter = new Vector3((((zStart[n].x + xOffset) * squareSize) - (width / 2)), 0, ((zStart[n].z  + zOffset) * squareSize) - (height / 2));
            Vector3 endCenter = new Vector3((((zEnd[n].x + xOffset) * squareSize) - (width / 2)), 0, ((zEnd[n].z + zOffset) * squareSize) - (height / 2));

            a = new Vector3(startCenter.x - (squareSize / 2), 0, startCenter.z - (squareSize / 2));
            b = new Vector3(startCenter.x + (squareSize / 2), 0, startCenter.z - (squareSize / 2));
            c = new Vector3(endCenter.x + (squareSize / 2), 0, endCenter.z + (squareSize / 2));
            d = new Vector3(endCenter.x - (squareSize / 2), 0, endCenter.z + (squareSize / 2));

            rectangles.Add(d);
            rectangles.Add(c);
            rectangles.Add(b);
            rectangles.Add(a);
        }

        verticies.AddRange(rectangles);

        for (int n = 0; n < rectangles.Count; n += 4)
        {
            GetTriangle(n, n + 1, n + 2);
            GetTriangle(n, n + 2, n + 3);
        }

        //Debug.Log("Simple Verticies: " + rectangles.Count);
    }

    private void GetTriangle(int a, int b, int c)
    {
        triangles.Add(a);
        triangles.Add(b);
        triangles.Add(c);
    }

    private void GetContourGeometry()
    {
        MeshData workspace = new MeshData();

        for (int n = 0; n < SquareGrid.Count; n++)
        {
            workspace = SquareGrid[n].GetPoints(verticies.Count);

            verticies.AddRange(workspace.verticies);
            triangles.AddRange(workspace.triangles);
        }
    }

    // Find non-redundant verticies and update triangles
    private void GetUniqueVerticies()
    {
        List<Vector3> uniqueVerticies = new List<Vector3>();
        List<int> updatedTriangles = new List<int>();
        bool matchFound = false;
        uniqueVerticies.Add(verticies[0]);

        //Debug.Log("Unculled Verticies: " + verticies.Count);

        for (int a = 1; a < verticies.Count; a++)
        {
            for (int b = 0; b < uniqueVerticies.Count; b++)
            {
                if (verticies[a] == uniqueVerticies[b])
                    matchFound = true;
            }

            if (!matchFound) uniqueVerticies.Add(verticies[a]);
            matchFound = false;
        }

        for (int a = 0; a < triangles.Count; a++)
        {
            for (int b = 0; b < uniqueVerticies.Count; b++)
            {
                if (verticies[triangles[a]] == uniqueVerticies[b])
                    updatedTriangles.Add(b);
            }
        }
        
        verticies = uniqueVerticies;
        triangles = updatedTriangles;
    }
}
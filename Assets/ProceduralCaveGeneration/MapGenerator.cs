using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class MapGenerator : MonoBehaviour
{
    [Header("Map Settings")]
    [SerializeField] private int width;
    [SerializeField] private int height;
    [Range(1, 100)]
    [SerializeField] private int borderSize = 1; // Should be at least 1 for wall generation to work
    [SerializeField] private int squareSize = 1;
    [SerializeField] private int regionSizeThreshold = 50;
    [SerializeField] private string seed;
    [SerializeField] private bool useRandomSeed;

    [Range(1, 100)]
    [SerializeField] private int mapFillPercent;

    private int[,] map;

    private void Start()
    {
        GenerateMap();
    }

    private void Update()
    {
        if (Keyboard.current.rKey.isPressed)
        {
            GenerateMap();
        }
    }

    private void GenerateMap()
    {
        map = new int[width, height];
        RandomFillMap();

        int iterationCount = 12;
        for (int i = 0; i < iterationCount; i++)
        {
            map = SmoothenMap();
        }

        ProcessMap();

        int[,] borderedMap = new int[width + borderSize * 2, height + borderSize * 2];
        for (int x = 0; x < borderedMap.GetLength(0); x++)
        {
            for (int y = 0; y < borderedMap.GetLength(1); y++)
            {
                if (x >= borderSize && x < width + borderSize && y >= borderSize && y < height + borderSize)
                {
                    borderedMap[x, y] = map[x - borderSize, y - borderSize];
                }
                else
                {
                    borderedMap[x, y] = 1;
                }
            }
        }

        MeshGenerator meshGenerator = GetComponent<MeshGenerator>();
        meshGenerator.GenerateMesh(borderedMap, squareSize);

    }

    private bool IsInMapRange(int x, int y) => (x >= 0 && y >= 0 && x < width && y < height);

    private void RandomFillMap()
    {
        if (useRandomSeed)
        {
            seed = Guid.NewGuid().ToString();
        }

        System.Random pseudoRandom = new(seed.GetHashCode());

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                //Fill map borders
                if (x == 0 || x == width - 1 || y == 0 || y == height - 1)
                {
                    map[x, y] = 1;
                }
                else
                {
                    map[x, y] = pseudoRandom.Next(0, 100) < mapFillPercent ? 1 : 0;
                }
            }
        }

    }

    private int[,] SmoothenMap()
    {
        int[,] newMap = new int[width, height];
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                int neighbourWallCount = GetSurroundingWallCount(x, y);
                int neighbourThreshold = 4;

                if (neighbourWallCount > neighbourThreshold)
                {
                    newMap[x, y] = 1;
                }
                else if (neighbourWallCount < neighbourThreshold)
                {
                    newMap[x, y] = 0;
                }
                else
                {
                    newMap[x, y] = map[x, y];
                }
            }
        }

        return newMap;
    }

    private int GetSurroundingWallCount(int gridX, int gridY)
    {
        int wallCount = 0;

        // Loop over the 3x3 square centered on (gridX, gridY)
        for (int neighbourX = gridX - 1; neighbourX <= gridX + 1; neighbourX++)
        {
            for (int neighbourY = gridY - 1; neighbourY <= gridY + 1; neighbourY++)
            {
                // Check if (neighbourX, neighbourY) is inside the map bounds
                if (IsInMapRange(neighbourX, neighbourY))
                {
                    // Skip the center cell (we don’t count itself)
                    if (neighbourX != gridX || neighbourY != gridY)
                    {
                        wallCount += map[neighbourX, neighbourY];
                        // since walls = 1, empty = 0, this adds 1 if it's a wall
                    }
                }
                else
                {
                    // If neighbor is outside map, treat it as a wall
                    wallCount++;
                }
            }
        }

        return wallCount;
    }

    private void ProcessMap()
    {
        List<List<Coordinates>> wallRegions = GetRegions(1);

        foreach (List<Coordinates> region in wallRegions)
        {
            if (region.Count < regionSizeThreshold)
            {
                foreach (Coordinates tile in region)
                {
                    map[tile.x, tile.y] = 0;
                }
            }
        }

        List<List<Coordinates>> roomRegions = GetRegions(0);
        List<Room> survivedRooms = new List<Room>();

        foreach (List<Coordinates> region in roomRegions)
        {
            if (region.Count < regionSizeThreshold)
            {
                foreach (Coordinates tile in region)
                {
                    map[tile.x, tile.y] = 1;
                }
            }
            else
            {
                survivedRooms.Add(new Room(region, map));
            }
        }

        survivedRooms.Sort();
        survivedRooms[0].isMain = true;
        survivedRooms[0].isConnectedToMainRoom = true;

        ConnectClosestRooms(survivedRooms);
    }

    private void ConnectClosestRooms(List<Room> allRooms, bool forceConnectionFromMainroom = false)
    {
        List<Room> roomsNotConnectedToMain = new List<Room>();
        List<Room> roomsConnectedToMain = new List<Room>();

        if (forceConnectionFromMainroom)
        {
            foreach(Room room in allRooms)
            {
                if (room.isConnectedToMainRoom)
                {
                    roomsConnectedToMain.Add(room);
                }
                else
                {
                    roomsNotConnectedToMain.Add(room);
                }
            }
        }
        else
        {
            roomsConnectedToMain = allRooms;
            roomsNotConnectedToMain = allRooms;
        }

        int bestDistance = 0;
        Coordinates bestTileA = new();
        Coordinates bestTileB = new();
        Room bestRoomA = new();
        Room bestRoomB = new();
        bool possibleConnectionFound = false;

        foreach (Room roomA in roomsNotConnectedToMain)
        {
            if (!forceConnectionFromMainroom)
            {
                possibleConnectionFound = false;
                if(roomA.connectedRooms.Count > 0)
                {
                    continue;
                }
            }

            foreach (var roomB in roomsConnectedToMain)
            {
                if (roomA == roomB || roomA.IsConnected(roomB)) continue;

                for (int tileIndexA = 0; tileIndexA < roomA.edgeTiles.Count; tileIndexA++)
                {
                    for (int tileIndexB = 0; tileIndexB < roomB.edgeTiles.Count; tileIndexB++)
                    {
                        Coordinates tileA = roomA.edgeTiles[tileIndexA];
                        Coordinates tileB = roomB.edgeTiles[tileIndexB];
                        int distanceBetweenRooms =
                            (tileA.x - tileB.x) * (tileA.x - tileB.x) +
                            (tileA.y - tileB.y) * (tileA.y - tileB.y);
                        if (distanceBetweenRooms < bestDistance || !possibleConnectionFound)
                        {
                            bestDistance = distanceBetweenRooms;
                            possibleConnectionFound = true;
                            bestTileA = tileA;
                            bestTileB = tileB;
                            bestRoomA = roomA;
                            bestRoomB = roomB;
                        }
                    }
                }
            }

            if (possibleConnectionFound && !forceConnectionFromMainroom)
            {
                CreatePassage(bestRoomA, bestRoomB, bestTileA, bestTileB);
            }
        }

        if (possibleConnectionFound && forceConnectionFromMainroom)
        {
            CreatePassage(bestRoomA, bestRoomB, bestTileA, bestTileB);
            ConnectClosestRooms(allRooms, true);
        }

        if (!forceConnectionFromMainroom)
        {
            ConnectClosestRooms(allRooms, true);
        }
    }

    private void CreatePassage(Room roomA, Room roomB, Coordinates tileA, Coordinates tileB)
    {
        Room.ConnectRooms(roomA, roomB);
        // TODO
        Debug.DrawLine(CoordinatesToWorldPoint(tileA), CoordinatesToWorldPoint(tileB), Color.green, float.MaxValue);

        List<Coordinates> line = GetLine(tileA, tileB);
        foreach (Coordinates coord in line)
        {
            DrawCircle(coord, 1);
        }


    }

    private void DrawCircle(Coordinates c, int r)
    {
        for(int x = -r; x <= r; x++)
        {
            for (int y = -r; y <= r; y++)
            {
                if(x*x + y*y <= r * r)
                {
                    int drawX = c.x + x;
                    int drawY = c.y + y;

                    if(IsInMapRange(drawX, drawY))
                    {
                        map[drawX, drawY] = 0;
                    }
                }
            }
        }
    }

    private List<Coordinates> GetLine(Coordinates from, Coordinates to)
    {
        var line = new List<Coordinates>();

        int x = from.x;
        int y = from.y;

        int dx = to.x - from.x;
        int dy = to.y - from.y;

        bool inverted = false;
        int step = Math.Sign(dx);
        int gradientStep = Math.Sign(dy);

        int longest = Mathf.Abs(dx);
        int shortest = Mathf.Abs(dy);

        if(longest < shortest)
        {
            inverted = true;
            longest = Mathf.Abs(dy);
            shortest = Mathf.Abs(dx);

            step = Math.Sign(dy);
            gradientStep = Math.Sign(dx);
        }

        int gradientAccumulation = longest / 2;
        for(int i = 0; i < longest; i++)
        {
            line.Add(new Coordinates(x, y));

            if (inverted)
            {
                y += step;
            }
            else
            {
                x += step;
            }

            gradientAccumulation += shortest;
            if(gradientAccumulation >= longest)
            {
                if (inverted)
                {
                    x += gradientStep;
                }
                else
                {
                    y += gradientStep;
                }

                gradientAccumulation -= longest;
            }
        }

        return line;
    }

    private Vector3 CoordinatesToWorldPoint(Coordinates tile)
    {
        return new Vector3(-width / 2 + 0.5f + tile.x, 2, -height / 2 + 0.5f + tile.y);
    }

    private List<Coordinates> GetRegionTiles(int startX, int startY)
    {
        List<Coordinates> tiles = new();
        int[,] mapFlags = new int[width, height];
        int tileType = map[startX, startY];

        Queue<Coordinates> queue = new();
        queue.Enqueue(new Coordinates(startX, startY));
        mapFlags[startX, startY] = 1;

        while (queue.Count > 0)
        {
            Coordinates tile = queue.Dequeue();
            tiles.Add(tile);

            for (int x = tile.x - 1; x <= tile.x + 1; x++)
            {
                for (int y = tile.y - 1; y <= tile.y + 1; y++)
                {
                    if (IsInMapRange(x, y) && (y == tile.y || x == tile.x))
                    {
                        if (mapFlags[x, y] == 0 && map[x, y] == tileType)
                        {
                            mapFlags[x, y] = 1;
                            queue.Enqueue(new Coordinates(x, y));
                        }
                    }
                }
            }
        }

        return tiles;
    }

    private List<List<Coordinates>> GetRegions(int tileType)
    {
        List<List<Coordinates>> regions = new();
        int[,] mapFlags = new int[width, height];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (mapFlags[x, y] == 0 && map[x, y] == tileType)
                {
                    List<Coordinates> newRegion = GetRegionTiles(x, y);
                    regions.Add(newRegion);

                    foreach (Coordinates tile in newRegion)
                    {
                        mapFlags[tile.x, tile.y] = 1;
                    }
                }
            }
        }

        return regions;
    }

    private class Room : IComparable<Room>
    {
        public List<Coordinates> tiles;
        public List<Coordinates> edgeTiles;
        public List<Room> connectedRooms;
        public int roomSize;
        public bool isMain;
        public bool isConnectedToMainRoom;

        public Room() { }

        public Room(List<Coordinates> roomTiles, int[,] map)
        {
            tiles = roomTiles;
            roomSize = tiles.Count;
            connectedRooms = new();
            edgeTiles = new();

            foreach (Coordinates tile in tiles)
            {
                for (int x = tile.x - 1; x <= tile.x + 1; x++)
                {
                    for (int y = tile.y - 1; y <= tile.y + 1; y++)
                    {
                        if (x == tile.x || y == tile.y)
                        {
                            if (map[x, y] == 1)
                            {
                                edgeTiles.Add(tile);
                            }
                        }
                    }
                }
            }
        }

        public void SetConnectedToMainRoom()
        {
            if (!isConnectedToMainRoom)
            {
                isConnectedToMainRoom = true;
                foreach (Room room in connectedRooms)
                {
                    room.SetConnectedToMainRoom();
                }
            }
        }

        public static void ConnectRooms(Room roomA, Room roomB)
        {
            if (roomA.isConnectedToMainRoom)
            {
                roomB.SetConnectedToMainRoom();
            }
            else if (roomB.isConnectedToMainRoom)
            {
                roomA.SetConnectedToMainRoom();
            }

            roomA.connectedRooms.Add(roomB);
            roomB.connectedRooms.Add(roomA);
        }

        public bool IsConnected(Room otherRoom)
        {
            return connectedRooms.Contains(otherRoom);
        }

        public int CompareTo(Room otherRoom)
        {
            return otherRoom.roomSize.CompareTo(roomSize);
        }
    }

    private struct Coordinates
    {
        public int x, y;

        public Coordinates(int tileX, int tileY)
        {
            this.x = tileX;
            this.y = tileY;
        }

    }

}

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class Terrain_File
{
    public origin_class origin;
    public float tileDimension;
    public float heightmapResolution;
    public List<tile_class> tiles;

    [Serializable]
    public class origin_class
    {
        public float longitude;
        public float latitude;
    }

    [Serializable]
    public class tile_class
    {
        public int x;
        public int y;
        public int min;
        public int max;
    }
}

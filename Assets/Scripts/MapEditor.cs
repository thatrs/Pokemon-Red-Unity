﻿using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;
using System.Text;
using System.Net;
using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Threading;
#if (UNITY_EDITOR)
using UnityEditor;
#endif
using System.Runtime.Serialization.Formatters.Binary;
//Save/Loader for Tile Pool
public class Serializer
{
    //Should test WWW loading code be enabled, or default to file loading?
    public static bool wwwLoad = true;

    public static T[,] Load2D<T>(string filename) where T : class
    {


        try
        {
           

                using (Stream stream = File.OpenRead(filename))
                {
                    BinaryFormatter formatter = new BinaryFormatter();
                    return formatter.Deserialize(stream) as T[,];
                }

        }
        catch (Exception e)
        {
            Debug.Log(e.Message);
        }

        return default(T[,]);
    }

    public static void Save2D<T>(string filename, T[,] data) where T : class
    {
        using (Stream stream = File.OpenWrite(filename))
        {
            BinaryFormatter formatter = new BinaryFormatter();
            formatter.Serialize(stream, data);
        }
    }


 

}

//Properties of tile, including collision, and others
public class Tile {
    public string path;
    public string tag;
    public bool isAnimated;
   
    public Tile(string s, string t, bool isanim){
        path = s;
        tag = t;
        isAnimated = isanim;
    }

}



public class MapData{
    public int mapNum;
    public long headerAddr;
    public int bank;
    public int tilesetNumber;
    public int originalHeight, originalWidth;
    public int height, width;
    public int mapPointer;
    public int textPointer;
    public int scriptPointer;
    public int connection;
    public int[,] connectionData;
    public int objectDataPointer;
    public int[] tileMap;
   
 

}
public class TileIndex{
    public string indiceString;
}
public class BlockIndex{
    public TileIndex[] indices;
}
public class TilesetIndex
{
    public BlockIndex[] indices = new BlockIndex[24];
}

#if UNITY_EDITOR
[ExecuteInEditMode]
public class MapEditor : MonoBehaviour
{
    public GameObject container;
    int currentTileIndex = 0;
    public GameObject template;
    public List<Tile> tilepool = new List<Tile>();
    public string MapSaveName;
    public int currentMap;
    bool hasDrawnGrid = false;
    public TilesetIndex tSetIndexes;
    public MapData[] maps = new MapData[0];
    public static Color[] colors = {
        new Color(1, 1, 1, 1), //0xff
    new Color(.5625f, .5625f, .5625f, 1), //0x90
    new Color(.25f, .25f, .25f, 1), //0x40
    new Color(0,0,0,1) //0x00
};


    public void SpawnTile(Vector2 snappos,GameObject parent){
        //Spawn an editing GameObject that represents a tile.
        GameObject go;

            go = Instantiate(template, parent.transform, true);

       
                go.transform.localPosition = snappos;
                go.tag = tilepool[currentTileIndex].tag;
        go.GetComponent<SpriteRenderer>().sprite = Resources.LoadAll<Sprite>(tilepool[currentTileIndex].path.Replace("Assets/Resources/", "").Replace(".png", ""))[0];
        if (go.tag == "TallGrass")
        {

            go.transform.Translate(0, 0, -1);
        }
        else
        {
            DestroyImmediate(go.GetComponent<TallGrass>());
        }
                if (!tilepool[currentTileIndex].isAnimated)
                {
            DestroyImmediate(go.GetComponent<AnimatedTile>());
        }
                

    }
    public void SpawnMap(){
        //Spawn the map with the current index stored in currentMap
        GameObject mapContainer = new GameObject("Map " + currentMap);
        mapContainer.transform.SetParent(container.transform);
        mapContainer.transform.position = new Vector2(GameConstants.mapWidth/2, GameConstants.mapHeight/2);
        MapData map  = maps[currentMap];
        string indicelist;
        string i1, i2, i3, i4;
        //Create string variables to retrieve the values from the string;
        for (int i = 0; i < map.tileMap.Length; i++){
            //is a tileset slot missing?
            if(tSetIndexes.indices[map.tilesetNumber].indices.Length != 0) 
                //set it as a null tile to not cause errors
                indicelist = tSetIndexes.indices[map.tilesetNumber].indices[map.tileMap[i]].indiceString;
            else indicelist = "0, 0, 0, 0";
            if (indicelist == "") indicelist = "0, 0, 0, 0";
            i1 = indicelist.Substring(0, indicelist.IndexOf(","));
            indicelist = indicelist.Substring(indicelist.IndexOf(" ") + 1);
             i2 = indicelist.Substring(0, indicelist.IndexOf(","));
            indicelist =  indicelist.Substring(indicelist.IndexOf(" ") + 1);
            i3 = indicelist.Substring(0, indicelist.IndexOf(","));
            indicelist = indicelist.Substring(indicelist.IndexOf(" ") + 1);
            i4 = indicelist.Substring(0);
            int[] indices = new int[] { int.Parse(i1), int.Parse(i2), int.Parse(i3), int.Parse(i4) };
            for (int j = 0; j < 4; j++){
                currentTileIndex = indices[j];
                //account for the map's dimensions to center it with the parent's transform
                SpawnTile(new Vector2(-map.width + 2*(i % map.width)  + j % 2, map.height -2 * (i / map.width) - Mathf.FloorToInt((float)j/4 + 0.51f)),mapContainer);
            }
        }


    }
   

  
   

    public void setCurrentMap(int index){
        currentMap = index;
       
    } 

}
[CustomEditor(typeof(MapEditor))]
public class MapTileEditor : Editor
{
   
    private static bool m_editMode = false;
    private static bool m_editMode2 = false;
    public int curmap = 0;
    //assume that the atlas's size will be what it is manually set to;
    public Vector2 atlasSize = new Vector2(1024, 512);


public override void OnInspectorGUI()
{
  DrawDefaultInspector();
  MapEditor me = (MapEditor)target;
        //iterate through the gameobjects in the map container, and parse their values;
        if(GUILayout.Button("Save Map to file")){
            GridTile[,] loadedTiles  = Serializer.Load2D<GridTile>(Application.streamingAssetsPath + "/map.txt");
            Get get = new Get();

            List<GameObject> foundtiles = new List<GameObject>();

            int index = 0;
            //look into each map for their tiles;
            foreach(Transform mapChild in me.container.transform ){
                foreach (Transform child in mapChild)
                {
                    foundtiles.Add(child.gameObject);
                }
            }
            index = 0;
            foreach(GameObject child in foundtiles){
                int x = (int)child.transform.position.x;
                int y = (int)child.transform.position.y;
                if (loadedTiles[x, y] == null)
                    loadedTiles[x,y] = new GridTile(null, null, null, false, 0, 0, "", false, false, false, 1, "");
                string tempname;
                tempname = child.GetComponent<SpriteRenderer>().sprite.name;
                if (tempname.Contains("_"))
                {
                    tempname = tempname.Substring(0, tempname.IndexOf('_'));
                }
                loadedTiles[x, y].mainSprite = tempname;
                loadedTiles[x, y].tag = child.tag;
                if (loadedTiles[x, y].tag.Contains("Wall")) loadedTiles[x, y].isWall = true;
                loadedTiles[x, y].isWarp = child.GetComponent<TileWarp>() ? true : false;
                loadedTiles[x, y].isAnimated = child.GetComponent<AnimatedTile>() ? true : false;
                if (loadedTiles[x, y].isAnimated){
                    tempname = AssetDatabase.GetAssetPath(child.GetComponent<SpriteRenderer>().sprite);
                    if (tempname.Contains("_"))
                    {
                        tempname = tempname.Substring(0, tempname.IndexOf('_'));
                    }
                    Sprite[] tiles = Resources.LoadAll<Sprite>(tempname.Replace("Assets/Resources/", "").Replace(".png",""));
                    loadedTiles[x, y].frames = tiles.Length;
                   
                }

             
                TallGrass grass = null;
                if (child.GetComponent<TallGrass>())
                {
                    grass = child.GetComponent<TallGrass>();
                }


                if (grass != null)
                {
                    loadedTiles[x, y].hasGrass = true;
                    loadedTiles[x, y].tallGrass = grass.info;

                   }

                else
                {
                    loadedTiles[x, y].tallGrass = null;
                    loadedTiles[x, y].hasGrass = false;
                }
                loadedTiles[x, y].tileWarp = child.GetComponent<TileWarp>() ? child.GetComponent<TileWarp>().info : null;
                loadedTiles[x, y].tiledata = child.GetComponent<itemData>() ? child.GetComponent<itemData>().data : null;
                loadedTiles[x, y].posx = x;
                loadedTiles[x, y].posy = y;
                index++;
            }
            Serializer.Save2D<GridTile>(Application.streamingAssetsPath + "/map.txt",loadedTiles);
           
        }
        if(GUILayout.Button("Save Empty map to file")){
            GridTile[,] nullMap = new GridTile[GameConstants.mapWidth, GameConstants.mapHeight];
            //for (int x = 0; x < GameConstants.mapWidth; x++){
            //    for (int y = 0; y < GameConstants.mapHeight; y++){
            //        nullMap[x, y] = null;
            //    }
            //}
            Serializer.Save2D<GridTile>(Application.streamingAssetsPath + "/map.txt",nullMap);
        }
        if (GUILayout.Button("Add all tiles"))
        {
            me.tilepool.Clear();
            me.tilepool.Resize(866);
            foreach(Texture2D tile in Resources.LoadAll<Texture2D>("interiortiles/")){
                string path = AssetDatabase.GetAssetPath(tile);
                string tag = "Untagged";
                bool isAnimated = false;
                if(path.Contains("/Collision")){
                    tag = "WallObject";
                }
                if (path.Contains("/TallGrass"))
                {
                    tag = "TallGrass";
                }
                if(path.Contains("/Ledges")){
                    if (path.Contains("/Left"))
                    {
                        tag = "LedgeLeft";
                    }  
                    if (path.Contains("/Right"))
                    {
                        tag = "LedgeRight";
                    }
                    if (path.Contains("/Down"))
                    {
                        tag = "LedgeDown";
                    }
                }
                string thisIndex = tile.name;
                int index = int.Parse(thisIndex.Replace("tile", ""));
                if (tile.width > 16 || tile.height > 16) isAnimated = true;
                me.tilepool[index] = new Tile(AssetDatabase.GetAssetPath(tile), tag, isAnimated);
                
                    
            }
        }
        if(GUILayout.Button("Save tile pool to file")){
            string data = JsonConvert.SerializeObject(me.tilepool);
            data = JValue.Parse(data).ToString(Formatting.Indented);
            File.WriteAllText(Application.streamingAssetsPath + "/tilePoolData.json", data);
        }
        if (GUILayout.Button("Load tile pool from file"))
        {
            FileStream file;
            StreamReader streamReader;
            file = new FileStream(Application.streamingAssetsPath + "/tilePoolData.json", FileMode.Open, FileAccess.Read);
            streamReader = new StreamReader(file);
            string data = streamReader.ReadToEnd();
            me.tilepool = JsonConvert.DeserializeObject<List<Tile>>(data);
            file.Close();
        
        }


        if (GUILayout.Button("Load data"))
        {
            FileStream file;
            StreamReader streamReader;
            file = new FileStream(Application.streamingAssetsPath + "/blockIndexData.json", FileMode.Open, FileAccess.Read);
            streamReader = new StreamReader(file);
            string data = streamReader.ReadToEnd();
            me.tSetIndexes = JsonConvert.DeserializeObject<TilesetIndex>(data);
            file.Close();
            file = new FileStream(Application.streamingAssetsPath + "/romMapData.json", FileMode.Open, FileAccess.Read);
            streamReader = new StreamReader(file);
            data = streamReader.ReadToEnd();
            me.maps = JsonConvert.DeserializeObject<MapData[]>(data);
            file.Close();
            Debug.Log(me.tSetIndexes.indices.Length);
  
        }
        if(GUILayout.Button("Spawn map")){
            
           me.SpawnMap();
        }
      
       

     
  }


}
#endif

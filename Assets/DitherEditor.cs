
//TODO youtube tutorial about converting link to gb mode
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using Lunar.Utils;
using System.IO;
using System;
using Lunar.Widgets;
using System.Text;

public class DitherEditor : MonoBehaviour {

    public enum DitherKind
    {
        Linear,
        Radial,
        Custom
    }

    public class Palette
    {
        public string name;
        public List<string> colors = new List<string>();
    }

    private GUIContent[] patternContentList;
    private ComboBox patternComboBox;

    private GUIContent[] ditherModeContentList;
    private ComboBox ditherModeCombobox;

    private GUIStyle listStyle = new GUIStyle();

    private GUIContent[] paletteNames;
    private ComboBox paletteCombobox;

    private const int fixedModes = 3;

    private string[] ditherToolbar = new string[] { "Linear", "Radial", "Image" };
    private string[] patternNames = new string[] { "Solid", "Bayer2", "Bayer4", "Bayer8", "Chain", "Circles", "Chess", "Commas", "Crosses", "Curls", "Dots", "Feathers", "Fur", "Grid", "Hair", "Halftone", "Lines", "Machine", "Maze", "Metal", "Scales", "Stripes", "Swirls", "Rocky", "Tiles" };

    public RawImage largeImg;
    public RawImage smallImg;
    private Texture2D outputTexture;
    private float[] customDitherSource;

    private bool equalize =false;

    private DitherKind ditherKind = DitherKind.Linear;

    public int canvasSize = 128;

    private bool rotatePat = false;
    private bool showGrad = false;

    public struct Pattern
    {
        public float[] values;
        public int width;
        public int height;
    }

    private List<Pattern> customPatterns = new List<Pattern>();
    private float[] ranges = new float[maxColors];

    private List<Palette> palettes = new List<Palette>();
    private int currentPaletteIndex = -1;

    private Pattern LoadPattern(string name)
    {
        var tex = Resources.Load<Texture2D>("patterns/"+name);
        var pixels = tex.GetPixels();

        var pat = new Pattern();
        pat.values = new float[pixels.Length];
        pat.width = tex.width;
        pat.height = tex.height;
        for (int i=0; i<pixels.Length; i++)
        {
            pat.values[i] = pixels[i].r;
        }

        return pat;
    }

	// Use this for initialization
	void Start () {
        listStyle.normal.textColor = Color.white;
        listStyle.onHover.background =
        listStyle.hover.background = new Texture2D(2, 2);
        listStyle.padding.left =
        listStyle.padding.right =
        listStyle.padding.top =
        listStyle.padding.bottom = 4;

        patternContentList = new GUIContent[patternNames.Length];
        for (int i=0; i<patternNames.Length; i++)
        {
            patternContentList[i] = new GUIContent(patternNames[i]);
        }
        patternComboBox = new ComboBox("button", "box", listStyle);

        ditherModeContentList = new GUIContent[ditherToolbar.Length];
        for (int i = 0; i < ditherToolbar.Length; i++)
        {
            ditherModeContentList[i] = new GUIContent(ditherToolbar[i]);
        }
        ditherModeCombobox = new ComboBox("button", "box", listStyle);

        tempCanvasSize = canvasSize.ToString();

        for (int i=fixedModes + 1; i<patternNames.Length; i++)
        {
            var pat = LoadPattern(patternNames[i].ToLower());
            customPatterns.Add(pat);
        }
        
        exportPath = PlayerPrefs.GetString("path", Path.Combine( Directory.GetCurrentDirectory(), "dither.png")); 

        colors = new ColorEntry[maxColors];
        for (int i=0; i<maxColors; i++)
        {
            colors[i] = new ColorEntry();
            colors[i].hex = "#FFFFFF";
            colors[i].percentage = 0.5f;
        }

        Palette pal;

        var palPath = Path.Combine(Application.persistentDataPath, "palette.txt");
        Debug.Log("palette path: " + palPath);
        bool palFileExists = File.Exists(palPath);
        if (palFileExists)
        {
            var lines = File.ReadAllLines(palPath);
            foreach (var line in lines )
            {
                var s = line.Split(',');
                if (s.Length>=3)
                {
                    pal = new Palette();
                    pal.name = s[0];
                    for (int i=1; i<s.Length; i++)
                    {
                        if (i>=maxColors)
                        {
                            break;
                        }
                        string color = s[i].Trim();
                        if (!color.StartsWith("#"))
                        {
                            color = "#" + color;
                        }

                        pal.colors.Add(color);
                    }

                    palettes.Add(pal);
                }
            }
        }

        if (palettes.Count == 0)
        {
            pal = new Palette();
            pal.name = "demo";
            pal.colors.Add("#E9345C");
            pal.colors.Add("#FF9200");
            pal.colors.Add("#EFDE72");
            pal.colors.Add("#B1AE52");
            pal.colors.Add("#2CB299");
            pal.colors.Add("#74D6F5");
            pal.colors.Add("#B2689E");
            pal.colors.Add("#D38E8E");
            palettes.Add(pal);

            pal = new Palette();
            pal.name = "GB";
            pal.colors.Add("#0F380F");
            pal.colors.Add("#306230");
            pal.colors.Add("#8BAC0F");
            pal.colors.Add("#9BBC0F");
            palettes.Add(pal);

            pal = new Palette();
            pal.name = "CGA";
            pal.colors.Add("#000000");
            pal.colors.Add("#55FFFF");
            pal.colors.Add("#FF55FF");
            pal.colors.Add("#FFFFFF");
            palettes.Add(pal);

            if (!palFileExists)
            {
                try
                {
                    var sb = new StringBuilder();
                    foreach (var p in palettes)
                    {
                        sb.Append(p.name);                        
                        for (int i=0; i<p.colors.Count; i++)
                        {
                            sb.Append(',');
                            sb.Append(p.colors[i]);
                        }
                        sb.AppendLine();
                    }

                    File.WriteAllText(palPath, sb.ToString());
                }
                catch (Exception e)
                {
                    Debug.Log(e.ToString());
                }
            }
        }

        SelectPalette(0);
        paletteNames = new GUIContent[palettes.Count];
        for (int i=0; i<palettes.Count; i++)
        {
            paletteNames[i] = new GUIContent(palettes[i].name);
        }
        paletteCombobox = new ComboBox("button", "box", listStyle);

        UpdateDither();
	}

    private void SelectPalette(int index)
    {
        if (currentPaletteIndex == index)
        {
            return;
        }

        currentPaletteIndex = index;
        var pal = palettes[index];

        colorCount = pal.colors.Count;

        for (int i=0; i<colorCount; i++)
        {
            colors[i].hex = pal.colors[i];
            ColorUtility.TryParseHtmlString(colors[i].hex, out colors[i].color);
            colors[i].percentage = 0.5f;
        }

        for  (int i=colorCount; i<maxColors; i++)
        {
            colors[i].hex = i % 2 == 0 ? "#000000" : "#FFFFFF";
            colors[i].percentage = 0;
        }
    }

    private void UpdateDither()
    {
        if (customDitherSource != null && customDitherSource.Length != canvasSize * canvasSize)
        {
            customDitherSource = null;
        }

        float cos = Mathf.Cos(angle * Mathf.Deg2Rad);
        float sin = Mathf.Sin(angle * Mathf.Deg2Rad);

        var pixels = new Color[canvasSize * canvasSize];

        float range = 0;
        float add = (colors[colorCount - 1].percentage) / (float)(colorCount - 1);
        if (mode == 0)
        {
            add = 0;
        }

        for (int i=0; i<colorCount; i++)
        {
            range += colors[i].percentage + add;
            ranges[i] = range;
        }

        //var target = new Vector2(cos * texture.width, sin * texture.height);
        var src = new Vector2(0, (1 - angle) * canvasSize);
        var dst = new Vector2(canvasSize, angle * canvasSize);

        var diff = dst - src;
        var normal = diff.normalized;
        var maxDist = dst.magnitude;

        var mid = (src + dst) * 0.5f;


        maxDist = dst.magnitude;
        normal = dst.normalized;
        var tangent = new Vector2(-normal.y, normal.x) * maxDist;

        var lineA = new Vector2(1, 1);
        var lineB = new Vector2(1, -1);


        for (int j = 0; j < canvasSize; j++)
        {
            for (int i = 0; i < canvasSize; i++)
            {
                var p = new Vector2((i / (float)canvasSize) - 0.5f, (j / (float)canvasSize) - 0.5f);

                float x = p.x * cos + p.y * sin;
                x += 0.5f;

                float y = p.x * -sin + p.y * cos;
                y += 0.5f;

                float dist;

                switch (ditherKind)
                {
                    case DitherKind.Linear:
                        {
                            if (x < 0) { x = 0; }
                            else
                            if (x > 1) { x = 1; }

                            dist = x;
                            break;
                        }

                    case DitherKind.Radial:
                        {
                            var center = new Vector2(radX, radY);
                            dist = (center - p).magnitude;
                            dist /= radScale;
                            if (dist>1)
                            {
                                dist = 1;
                            }

                            dist = 1 - dist;
                            break;
                        }

                    case DitherKind.Custom:
                        {
                            if (customDitherSource == null)
                            {
                                dist = 1;
                            }
                            else
                            {
                                dist = customDitherSource[i + j * canvasSize];
                            }
                            
                            break;
                        }

                    default:return;

                }
                                


                int baseColor = 0;
                
                if (dist>=1)
                {
                    baseColor =  mode == 0 ? colorCount -1 : colorCount - 2;
                }
                else
                {
                    for (int k = 0; k < colorCount; k++)
                    {
                        if (dist <= ranges[k])
                        {
                            baseColor = k;
                            break;
                        }
                    }
                }

                float baseRange = baseColor > 0 ? ranges[baseColor - 1] : 0;
                float rangeSize = ranges[baseColor] - baseRange;

                float n = (dist - baseRange) / rangeSize;

                if (equalize)
                {
                    baseColor = Mathf.FloorToInt(dist * (colorCount - 1));
                    n = ((dist * (colorCount - 1)) - baseColor);
                }
                               
                //float n = s - Mathf.Floor(s);
                
                //n = 1 - n;

                n = Mathf.Floor(n * steps);
                n /= (float)(steps-1);

                if (baseColor >= colorCount)
                {
                    baseColor = colorCount - 1;
                    n = 1;
                }
                else
                if (baseColor < 0)
                {
                    baseColor = 0;
                    n = 0;
                }

                /*float s = n / (float)(steps-1);
                float d = s - Mathf.Floor(s);*/

                int tx = rotatePat ? (int)(x * canvasSize) : i;
                int ty = rotatePat ? (int)(y * canvasSize) : j;

                bool dith;

                if (mode<= fixedModes)
                {
                    switch (mode)
                    {
                        case 0: dith = false; break;
                        case 1: dith = DitherUtils.ColorDither(DitherMode.Bayer2x2, tx, ty, n); break;
                        case 2: dith = DitherUtils.ColorDither(DitherMode.Bayer4x4, tx, ty, n); break;
                        case 3: dith = DitherUtils.ColorDither(DitherMode.Bayer8x8, tx, ty, n); break;
                        default:return;
                    }
                    
                }
                else
                {
                    var pat = customPatterns[mode - (fixedModes + 1)];
                    dith = DitherUtils.ColorDither(pat.values, pat.width, pat.height, tx, ty, n);
                }                               

                var ofs = i + j * canvasSize;
                                
                if (showGrad)
                {
                    pixels[ofs] = new Color(dist, dist, dist);
                    //pixels[ofs] = new Color(n, n, n);
                }
                else
                {
                    pixels[ofs] = dith ? colors[baseColor + 1].color : colors[baseColor].color;
                }
                
                //pixels[ofs] = colors[baseColor];
            }
        }

        if (outputTexture == null || outputTexture.width != canvasSize)
        {
            outputTexture = new Texture2D(canvasSize, canvasSize, TextureFormat.ARGB32, false);
            outputTexture.filterMode = FilterMode.Point;

            largeImg.texture = outputTexture;
            smallImg.texture = outputTexture;

            int targetScale;

            if (canvasSize >= 250)
            {
                targetScale = 1;
            }
            else
            if (canvasSize >= 160)
            {
                targetScale = 2;
            }
            else
            if (canvasSize >= 128)
            {
                targetScale = 3;
            }
            else
            if (canvasSize >= 64)
            {
                targetScale = 4;
            }
            else
            {
                targetScale = 5;
            }

            var rt = largeImg.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(outputTexture.width, outputTexture.height);
            rt.localScale = new Vector3(targetScale, targetScale, targetScale);

            rt = smallImg.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(outputTexture.width, outputTexture.height);

            smallImg.enabled = canvasSize <= 250;
        }

        outputTexture.SetPixels(pixels);
        outputTexture.Apply();
    }

    // Update is called once per frame
    void Update () {
        UpdateDither();	
	}

    #region MATH
    public static bool LineIntersection(Vector2 p1, Vector2 p2, Vector2 p3, Vector2 p4, out Vector2 hit)
    {
        var b = p2 - p1;
        var d = p4 - p3;

        hit = Vector2.zero;

        float b_dot_d_perp = b.x * d.y - b.y * d.x;
        if (b_dot_d_perp == 0)
        {
            return false;
        }

        var c = p3 - p1;
        float t = (c.x * d.y - c.y * d.x) / b_dot_d_perp;
        if (t < 0 || t > 1)
        {
            return false;
        }
        float u = (c.x * b.y - c.y * b.x) / b_dot_d_perp;
        if (u < 0 || u > 1)
        {
            return false;
        }

        hit = new Vector2(p1.x + t * b.x, p1.y + t * b.y);
        return true;
    }

    //Compute the dot product AB . AC
    private float DotProduct(Vector2 pointA, Vector2 pointB, Vector2 pointC)
    {
        Vector2 AB;
        Vector2 BC;
        AB = pointB - pointA;
        BC = pointC - pointB;
        var dot = AB.x * BC.y + AB.y * BC.y;
        return dot;
    }

    //Compute the cross product AB x AC
    private float CrossProduct(Vector2 pointA, Vector2 pointB, Vector2 pointC)
    {
        Vector2 AB;
        Vector2 AC;
        AB = pointB - pointA;
        AC = pointC - pointA;
        var cross = AB.x * AC.y - AB.y * AC.x;
        return cross;
    }

    //Compute the distance from A to B
    private float Distance(Vector2 pointA, Vector2 pointB)
    {
        var d1 = pointA.x - pointB.x;
        var d2 = pointA.y - pointB.y;

        return Mathf.Sqrt(d1 * d1 + d2 * d2);
    }

    //Compute the distance from AB to C
    //if isSegment is true, AB is a segment, not a line.
    private float LineToPointDistance2D(Vector2 pointA, Vector2 pointB, Vector2 pointC, bool isSegment)
    {
        var dist = CrossProduct(pointA, pointB, pointC) / Distance(pointA, pointB);
        if (isSegment)
        {
            double dot1 = DotProduct(pointA, pointB, pointC);
            if (dot1 > 0)
                return Distance(pointB, pointC);

            double dot2 = DotProduct(pointB, pointA, pointC);
            if (dot2 > 0)
                return Distance(pointA, pointC);
        }

        return -dist;
        //        return Mathf.Abs(dist);
    }
    #endregion

    private int angle = 0;
    private int mode = 2;
    private int steps = 5;

    private const int maxColors = 8;

    public class ColorEntry
    {
        public string hex;
        public Color color;
        public float percentage;
    }

    private ColorEntry[] colors;

    private int colorCount;

    private string exportPath;

    private string tempCanvasSize;

    private float radX = 0.0f;
    private float radY = 0.0f;
    private float radScale = 0.5f;

    void OnGUI()
    {
        int minSteps, maxSteps;

        switch (mode)
        {            
            case 1: minSteps = 3; maxSteps = 5; break;
            case 2: minSteps = 3; maxSteps = 9; break;
            case 3: minSteps = 3; maxSteps = 17; break;
            default: minSteps = 3; maxSteps = 17; break;
        }

        int row2 = 250;
        GUI.Label(new Rect(row2, 25, 100, 20), "Colors");
        colorCount = (int)GUI.HorizontalSlider(new Rect(row2, 50, 100, 25), colorCount, 2, maxColors);

        if (mode>0)
        {
            GUI.Label(new Rect(row2, 75, 100, 20), "Steps");
            steps = (int)GUI.HorizontalSlider(new Rect(row2, 100, 100, 25), steps, minSteps, maxSteps);
        }

        showGrad = GUI.Toggle(new Rect(row2, 125, 100, 25), showGrad, "View Gradient");

        int row3 = 425;

        if (ditherKind == DitherKind.Linear)
        {
            if (mode >0 )
            {
                rotatePat = GUI.Toggle(new Rect(row3, 125, 100, 25), rotatePat, "Rotate Pattern");
            }
        }
        else
        {
            rotatePat = false;
        }

        switch (ditherKind)
        {
            case DitherKind.Linear:
                {
                    GUI.Label(new Rect(row3, 25, 100, 20), "Angle");
                    angle = (int)GUI.HorizontalSlider(new Rect(row3, 50, 200, 25), angle, 0, 360);

                    string tempAngle = GUI.TextField(new Rect(row3 + 210, 50, 100, 20), angle.ToString());
                    int.TryParse(tempAngle, out angle);

                    if (angle > 360) { angle = 360; }
                    if (angle < 0) { angle = 0; }
                    break;
                }

            case DitherKind.Radial:
                {
                    GUI.Label(new Rect(row3, 25, 100, 20), "Offset X");
                    radX = GUI.HorizontalSlider(new Rect(row3, 50, 100, 25), radX, -0.5f, 0.5f);
                    GUI.Label(new Rect(row3+110, 25, 100, 20), "Offset Y");
                    radY = GUI.HorizontalSlider(new Rect(row3 + 110, 50, 100, 25), radY, -0.5f, 0.5f);
                    GUI.Label(new Rect(row3 + 220, 25, 100, 20), "Scale");
                    radScale = GUI.HorizontalSlider(new Rect(row3 + 220, 50, 100, 25), radScale, 0.25f, 1);

                    break;
                }

            case DitherKind.Custom:
                {
                    GUI.Label(new Rect(row3, 25, 100, 20), "Greyscale Source");
                    if (GUI.Button(new Rect(row3, 50, 150, 20), "Paste From Clipboard"))
                    {
                        int width, height;
                        var pixels = Clipboard.ReadPixels(out width, out height);
                        if (pixels != null)
                        {
                            canvasSize = Math.Max(width, height);
                            customDitherSource = new float[canvasSize * canvasSize];

                            int padX = (canvasSize - width) / 2;
                            int padY = (canvasSize - height) / 2;

                            for (int j = 0; j < canvasSize; j++)
                            {
                                for (int i = 0; i < canvasSize; i++)
                                {
                                    float dist;
                                    if (i<width && j<height)
                                    {
                                        var pixel = pixels[i + j * width];
                                        if (pixel.r == pixel.g && pixel.r == pixel.b)
                                        {
                                            dist = pixel.r / 255.0f;
                                        }
                                        else
                                        {
                                            dist = (0.3f * pixel.r + 0.59f * pixel.g + 0.11f * pixel.b) / 255.0f;
                                        }                                        
                                    }
                                    else
                                    {
                                        dist = 0;
                                    }

                                    int targetOfs = i + padX + (j + padY) * canvasSize;
                                    if (targetOfs<customDitherSource.Length)
                                    {
                                        customDitherSource[targetOfs] = dist;
                                    }                                    
                                }
                            }
                        }
                    }

                    break;
                }
        }

        GUI.Label(new Rect(row3, 75, 100, 20), "Pattern");
        mode = (int)GUI.HorizontalSlider(new Rect(row3, 100, 100, 25), mode, 0, patternNames.Length-1);
        mode = patternComboBox.Show(new Rect(row3 + 115, 95, 100, 20), mode, patternContentList);
        //GUI.Label(new Rect(row3 + 120, 90, 100, 20), patternNames[mode]);

        //ditherKind = (DitherKind) GUI.Toolbar(new Rect(row3 + 220, 90, 160, 20), (int)ditherKind, ditherToolbar);
        ditherKind = (DitherKind) ditherModeCombobox.Show(new Rect(row3 + 220, 95, 100, 20), (int)ditherKind, ditherModeContentList);

        int row4 = Screen.width - 225;

        int lY = 350;
        GUI.Label(new Rect(row4, lY + 10, 220, 20), "FREE dithering tool");
        GUI.Label(new Rect(row4, lY + 25, 220, 20), "Made by LUNAR LABS");
        GUI.Label(new Rect(row4, lY + 50, 220, 20), "Contact me for feedback / ideas!");

        GUI.Label(new Rect(row4, lY + 75, 220, 20), "Links:");
        if (GUI.Button(new Rect(row4, lY + 100, 150, 20), "Website"))
        {
            Application.OpenURL("http://lunarlabs.pt");
        }

        if (GUI.Button(new Rect(row4, lY + 125, 150, 20), "Twitter"))
        {
            Application.OpenURL("https://twitter.com/onihunters");
        }

        if (GUI.Button(new Rect(row4, lY + 150, 150, 20), "Facebook"))
        {
            Application.OpenURL("https://www.facebook.com/lunarlabspt/");
        }

        if (GUI.Button(new Rect(row4, lY + 175, 150, 20), "Tumblr"))
        {
            Application.OpenURL("http://onihunters.tumblr.com/");
        }

        if (GUI.Button(new Rect(row4, lY + 200, 150, 20), "itch.io"))
        {
            Application.OpenURL("https://lunarlabs.itch.io");
        }

        if (GUI.Button(new Rect(row4, lY + 225, 150, 20), "Asset Store"))
        {
            Application.OpenURL("https://www.assetstore.unity3d.com/en/#!/search/page=1/sortby=popularity/query=publisher:6001");
        }


        if (GUI.Button(new Rect(20, Screen.height - 30, 150, 20), "Exit"))
        {
            Application.Quit();
        }

        int row1 = 25;

        int y = 25;

        for (int i=0; i<colorCount; i++)
        {
            GUIDrawRect(new Rect(row1 - 22, y, 20, 20), colors[i].color);
            //GUI.Label(new Rect(row1, y, 100, 20), "Color "+(i+1).ToString());

            colors[i].hex = GUI.TextField(new Rect(row1, y, 80, 20), colors[i].hex);

            if (i<colorCount-1 && !equalize)
            {
                colors[i].percentage = GUI.HorizontalSlider(new Rect(row1 + 90, 5 + y, 80, 20), colors[i].percentage, 0, 1);
            }

            y += 25;
        }

        equalize = GUI.Toggle(new Rect(row1 - 20, y, 150, 25), equalize, "Equalize percentages");
        y += 25;

        GUI.Label(new Rect(row1 - 20, y, 100, 20), "Palette");
        SelectPalette(paletteCombobox.Show(new Rect(row1 + 60, y, 100, 20), currentPaletteIndex, paletteNames));
        y += 25;

        GUI.Label(new Rect(row2, Screen.height - 100, 100, 20), "Canvas size");
        tempCanvasSize = GUI.TextField(new Rect(row2, Screen.height - 75, 100, 20), tempCanvasSize);

        if (GUI.Button(new Rect(row2 + 105, Screen.height - 75, 100, 20), "Apply"))
        {
            int.TryParse(tempCanvasSize, out canvasSize);
            if (canvasSize>512)
            {
                canvasSize = 512;
            }
        }

        int ppx = row3 + 175;
        
        exportPath = GUI.TextField(new Rect(ppx, Screen.height - 35, Screen.width - (ppx+20), 25), exportPath);
        if (GUI.Button(new Rect(row3, Screen.height - 35, 150, 25), "Export to File"))
        {
            try
            {
                var bytes = outputTexture.EncodeToPNG();
                File.WriteAllBytes(exportPath, bytes);

                PlayerPrefs.SetString("path", exportPath);
                PlayerPrefs.Save();

                var url = "file://" + exportPath.Replace('\\', '/');
                Debug.Log(url);
                Application.OpenURL(url);
            }
            catch (Exception e)
            {
                Debug.Log(e.ToString());
            }
        }

        if (Clipboard.isClipboardSupported(Clipboard.Format.Image) && GUI.Button(new Rect(row2, Screen.height - 35, 150, 25), "Export to Clipboard"))
        {
            //Debug.Log("clip: "+Clipboard.ReadText());
            /*var bmp = Clipboard.ReadTexture(false);
            if (bmp != null)
            {
                var bytes = bmp.EncodeToPNG();
                File.WriteAllBytes("test.png", bytes);
            }*/

            //Clipboard.Write("собака!");
            if (Clipboard.WriteImage(outputTexture))
            {
                Debug.Log("Copied to clipboard");
            }
            else
            {
                Debug.Log("Clipboard failed");
            }
            
        }

        float sum = 0;
        for (int i=0; i<colorCount; i++)
        {
            sum += colors[i].percentage;

            Color temp;
            if (ColorUtility.TryParseHtmlString(colors[i].hex, out temp))
            {
                colors[i].color = temp;
            }
        }

        for (int i = 0; i < colorCount; i++)
        {
            colors[i].percentage /= sum;
        }
    }

    #region UTILS
    private static Texture2D _staticRectTexture;
    private static GUIStyle _staticRectStyle;

    // Note that this function is only meant to be called from OnGUI() functions.
    public static void GUIDrawRect(Rect position, Color color)
    {
        if (_staticRectTexture == null)
        {
            _staticRectTexture = new Texture2D(1, 1);
        }

        if (_staticRectStyle == null)
        {
            _staticRectStyle = new GUIStyle();
        }

        _staticRectTexture.SetPixel(0, 0, Color.white);
        _staticRectTexture.Apply();

        _staticRectStyle.normal.background = _staticRectTexture;

        GUI.color = color;
        GUI.Box(position, GUIContent.none, _staticRectStyle);
        GUI.color = Color.white;
    }
    #endregion

}

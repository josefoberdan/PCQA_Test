using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEngine;

public static class PointCloudLoader
{
    //PLY ASCII ou binary_little_endian
    public static bool LoadPly(string path, out Vector3[] positions, out Color32[] colors)
    {
        positions = null;
        colors = null;

        if (!File.Exists(path))
        {
            Debug.LogError("PointCloudLoader: arquivo PLY não encontrado em: " + path);
            return false;
        }

        string[] headerLines;
        bool isBinary = false;
        int vertexCount = 0;
        List<string> propertyOrder = new List<string>();
        
        using (var sr = new StreamReader(path))
        {
            var headerList = new List<string>();
            string line;
            while ((line = sr.ReadLine()) != null)
            {
                headerList.Add(line);
                if (line.StartsWith("format ascii")) isBinary = false;
                else if (line.StartsWith("format binary_little_endian")) isBinary = true;

                if (line.StartsWith("element vertex"))
                {
                    string[] parts = line.Split(' ');
                    vertexCount = int.Parse(parts[2]);
                }
                else if (line.StartsWith("property"))
                {
                    string[] parts = line.Split(' ');
                    propertyOrder.Add(parts[parts.Length - 1]);
                }
                else if (line.StartsWith("end_header"))
                {
                    break;
                }
            }
            headerLines = headerList.ToArray();
        }

        if (vertexCount == 0)
        {
            Debug.LogError("PointCloudLoader: arquivo PLY sem vértices definidos: " + path);
            return false;
        }

        var points = new Vector3[vertexCount];
        var cols = new Color32[vertexCount];

        if (!isBinary)
        {
            var lines = File.ReadAllLines(path);
            int startIndex = Array.FindIndex(lines, l => l.StartsWith("end_header")) + 1;

            for (int i = 0; i < vertexCount && (startIndex + i) < lines.Length; i++)
            {
                var parts = lines[startIndex + i]
                    .Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 3) continue;

                float x = float.Parse(parts[0], CultureInfo.InvariantCulture);
                float y = float.Parse(parts[1], CultureInfo.InvariantCulture);
                float z = float.Parse(parts[2], CultureInfo.InvariantCulture);

                byte r = 255, g = 255, b = 255;
                if (parts.Length >= 6)
                {
                    r = byte.Parse(parts[3]);
                    g = byte.Parse(parts[4]);
                    b = byte.Parse(parts[5]);
                }

                points[i] = new Vector3(x, y, z);
                cols[i] = new Color32(r, g, b, 255);
            }

            Debug.Log($"PointCloudLoader: formato ASCII detectado ({vertexCount} vértices) - {path}");
        }
        
        else
        {
            using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read))
            using (var br = new BinaryReader(fs))
            {
                
                long headerSize = System.Text.Encoding.ASCII.GetByteCount(string.Join("\n", headerLines)) + 1;
                fs.Seek(headerSize, SeekOrigin.Begin);

                for (int i = 0; i < vertexCount; i++)
                {
                    float x = br.ReadSingle();
                    float y = br.ReadSingle();
                    float z = br.ReadSingle();
                    byte r = br.ReadByte();
                    byte g = br.ReadByte();
                    byte b = br.ReadByte();
                    points[i] = new Vector3(x, y, z);
                    cols[i] = new Color32(r, g, b, 255);
                }
            }

            Debug.Log($"PointCloudLoader: formato binário detectado ({vertexCount} vértices) - {path}");
        }

        positions = points;
        colors = cols;
        return true;
    }
}

using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Collections.Generic;
using UnityEditor;

namespace DartsRoguelike.Mock.Editor
{
    // TMP's standard importer writes to Assets/TextMesh Pro. Extract its bundled
    // resources under Mock instead, to respect this prototype's directory boundary.
    public static class MockTextResources
    {
        public static string Import()
        {
            string package=Path.Combine(UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(TMPro.TMP_FontAsset).Assembly).resolvedPath,
                "Package Resources/TMP Essential Resources.unitypackage");
            var entries=new Dictionary<string,Dictionary<string,byte[]>>();
            using(var file=File.OpenRead(package))
            using(var gzip=new GZipStream(file,CompressionMode.Decompress))
            {
                byte[] header=new byte[512];
                while(Read(gzip,header,512)==512)
                {
                    string name=Encoding.UTF8.GetString(header,0,100).TrimEnd('\0');
                    if(name.Length==0)break;
                    string octal=Encoding.ASCII.GetString(header,124,12).Trim('\0',' ');
                    int size=octal.Length==0?0:Convert.ToInt32(octal,8);
                    if(size<0||size>64*1024*1024)throw new InvalidDataException("Unexpected archive entry size.");
                    byte[] data=new byte[size];
                    if(Read(gzip,data,size)!=size)throw new EndOfStreamException();
                    int pad=(512-size%512)%512;
                    if(pad>0)Read(gzip,new byte[pad],pad);
                    var parts=name.TrimEnd('/').Split('/');
                    if(parts.Length!=2)continue;
                    if(!entries.TryGetValue(parts[0],out var group)){group=new Dictionary<string,byte[]>();entries.Add(parts[0],group);}
                    group[parts[1]]=data;
                }
            }
            string root=Path.GetFullPath("Assets/Mock") + Path.DirectorySeparatorChar;
            int count=0;
            foreach(var pair in entries)
            {
                var group=pair.Value;
                if(!group.TryGetValue("pathname",out var pathBytes))continue;
                string original=Encoding.UTF8.GetString(pathBytes).TrimEnd('\0','\n','\r');
                if(!original.StartsWith("Assets/TextMesh Pro/",StringComparison.Ordinal))continue;
                string destination=Path.GetFullPath("Assets/Mock/TextResources/"+original.Substring("Assets/TextMesh Pro/".Length));
                if(!destination.StartsWith(root,StringComparison.OrdinalIgnoreCase))throw new InvalidDataException("Out of scope archive path.");
                // Reuse no project asset: collisions fail before any overwrite.
                if(group.TryGetValue("asset",out var bytes))
                {
                    if(File.Exists(destination))continue;
                    string existing=AssetDatabase.GUIDToAssetPath(pair.Key);
                    if(!string.IsNullOrEmpty(existing)&&!existing.StartsWith("Assets/Mock/"))
                        throw new InvalidOperationException("Resource GUID already exists at "+existing);
                    Directory.CreateDirectory(Path.GetDirectoryName(destination));
                    File.WriteAllBytes(destination,bytes);
                    if(group.TryGetValue("asset.meta",out var meta))File.WriteAllBytes(destination+".meta",meta);
                    count++;
                }
            }
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            return "Imported "+count+" bundled TMP resources into Assets/Mock/TextResources";
        }
        static int Read(Stream stream,byte[] buffer,int length)
        {
            int total=0;
            while(total<length){int n=stream.Read(buffer,total,length-total);if(n==0)break;total+=n;}
            return total;
        }
    }
}

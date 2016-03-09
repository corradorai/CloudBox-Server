using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;

namespace cloudbox_server
{
    public static class VersionsManager
    {
        private static BinaryFormatter formatter = new BinaryFormatter();

        public static List<ClientFileInfo> getInfoVersions(string username)
        {
            int v = 0;
            List<ClientFileInfo> myVersions = new List<ClientFileInfo>();
            try
            {
                while ((File.Exists(username + "_version_" + v + ".dat")) && v < 5) 
                {
                    List<ClientFileInfo> l = LoadVersion(username + "_version_" + v + ".dat");
                    ClientFileInfo curV = new ClientFileInfo();
                    curV.Size = 0;
                    curV.LastUpd = l[0].LastUpd;
                    foreach(ClientFileInfo c in l)
                    {                       
                        curV.Size+= c.Size;
                    }
                    curV.LastUpd = File.GetLastWriteTime(username + "_version_" + v + ".dat");
                    curV.Path=username + "_version_" + v + ".dat";
                    curV.Name="Nome: Version"+v;
                    curV.RelativePath = "Numero di file: " + l.Count;
                    myVersions.Add(curV);
                    v++;
                } 


            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            return myVersions;
        }
        

        public static void SaveCurrentVersion(List<ClientFileInfo> cli,string username)
        {
            // Gain code access to the file that we are going
            // to write to
           
            try
            {
                int v = 0, id=0;
                DateTime oldest;
                if (File.Exists(username + "_version_" + v + ".dat"))
                {
                    oldest = File.GetLastWriteTime(username + "_version_" + v + ".dat");
                    while ((File.Exists(username + "_version_" + v + ".dat")) && v < 5)
                    {                   
                        v++;
                    }
                    if (v >= 5)
                    {
                        v = 0;
                        while ((File.Exists(username + "_version_" + v + ".dat")) && v < 5)
                        {
                            DateTime d = File.GetLastWriteTime(username + "_version_" + v + ".dat");
                            if (oldest.CompareTo(d) > 0)
                            {
                                oldest = d;
                                id = v;
                            }
                            v++;
                        }
                    }
                    else id = v;
                }
                else
                {
                    v = 0;
                }
                v = id;
                // Create a FileStream that will write data to file.
                FileStream writerFileStream =
                    new FileStream(username+"_version_"+v+".dat",FileMode.Create, FileAccess.Write);
                // Save our dictionary of friends to file
                formatter.Serialize(writerFileStream, cli);

                // Close the writerFileStream when we are done.
                writerFileStream.Close();
                Console.WriteLine(username + "_version_" + v + ".dat " + File.GetLastWriteTime(username + "_version_" + v + ".dat"));
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            } // end try-catch
        } // end public bool Load()

        public static List<ClientFileInfo> LoadVersion(string versionPath)
        {

            // Check if we had previously Save information of our friends
            // previously

            List<ClientFileInfo> version= new List<ClientFileInfo>();
            if (File.Exists(versionPath))
            {

                try
                {
                    // Create a FileStream will gain read access to the 
                    // data file.
                   // File.SetAttributes("testversion.dat", FileAttributes.Normal);
                    FileStream readerFileStream = new FileStream(versionPath,
                        FileMode.Open, FileAccess.Read);
                    // Reconstruct information of our friends from file.
                    version = (List<ClientFileInfo>)
                        formatter.Deserialize(readerFileStream);
                    // Close the readerFileStream when we are done
                    readerFileStream.Close();

                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);

                } // end try-catch

            } // end if
            return version;

        } // end public bool Load()
    }
}

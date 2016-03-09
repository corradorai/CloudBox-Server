using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.IO;


namespace cloudbox_server
{
    public enum commands
    {
        LISTFILE = 1,
        PUSHFILE = 2,
        DELETEFILE = 3,
        UPDATEFILE = 4,
        GETFILE = 8,
        GETALL = 9,
        ACK = 5,
        ERROR = 6,
        FINISH = 7,
        SIGNUP = 10,
        LOGIN = 11,
        LOGOUT = 12,
        RENAMED = 14,
        HELLO = 15,
        SPACE = 27,
        HISTORY = 30,
        UNKNOWN = -1,
        LOGGED = 33,
        LISTINFOVERSIONS=44,
        LISTVERSION=55,
        FINALDELETE=19,
        CHANGED=80
    }

    public static class MessageUtils
    {
        public static string EncodeLoginMessage(string name, string passSalt, string sessionSalt)
        {
            LoginInfo l = new LoginInfo
            {
                Name = name,
                PswSalt = passSalt,
                SessionSalt = sessionSalt
            };
            string json = JsonConvert.SerializeObject(l, Formatting.Indented);
            return json + "<EOF>";
        }

        public static LoginInfo DecodeLoginMessage(string p)
        {
            string s = p.Replace("<EOF>", "");
            LoginInfo h;
            try
            {
                h = JsonConvert.DeserializeObject<LoginInfo>(s);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
                return null;
            }
            return h;
        }

        public static string EncodePresentationMessage(string name, string pass, commands c)
        {
            PresentationInfo p = new PresentationInfo
            {
                Name = name,
                Psw = pass,
                cmd = c
            };
            string json = JsonConvert.SerializeObject(p, Formatting.Indented);
            return json + "<EOF>";
        }

        public static PresentationInfo decodePresentationInfo(string p)
        {
            string s = p.Replace("<EOF>", "");
            PresentationInfo h;
            try
            {
                h = JsonConvert.DeserializeObject<PresentationInfo>(s);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
                return null;
            }
            return h;
        }


        public static string EncodeFileVersionsMessage(FileVersions fv)
        {
            FileVersions f = new FileVersions
            {
                Name = fv.Name,
                Path = fv.Path,
                Size = fv.Size,
                LastAcces = fv.LastAcces,
                LastUpd = fv.LastUpd,
                Hash = fv.Hash,
                versions = fv.versions,
                RelativePath = fv.RelativePath,
                ContentHash = fv.ContentHash
            };
            string json = JsonConvert.SerializeObject(f, Formatting.Indented);
            return json + "<EOF>";
        }

        public static FileVersions DecodeFileVersionsMessage(string s)
        {
            string s1 = s.Replace("<EOF>", "");
            FileVersions f;
            try
            {
                f = JsonConvert.DeserializeObject<FileVersions>(s1);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
                return null;
            }
            return f;
        }

        public static string EncodeFileInfoMessage(string n, string p, long s, DateTime lastA, DateTime lastU, string h)
        {
            ClientFileInfo f = new ClientFileInfo
            {
                Name = n,
                Path = p,
                Size = s,
                LastAcces = lastA,
                LastUpd = lastU,
                Hash = h
            };
            string json = JsonConvert.SerializeObject(f, Formatting.Indented);
            return json + "<EOF>";
        }

        public static string EncodeFileInfoMessage(ClientFileInfo f)
        {
            string json = JsonConvert.SerializeObject(f, Formatting.Indented);
            return json + "<EOF>";
        }

        public static ClientFileInfo DecodeFileInfoMessage(string s)
        {
            string s1 = s.Replace("<EOF>", "");
            ClientFileInfo f;
            try
            {
                f = JsonConvert.DeserializeObject<ClientFileInfo>(s1);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
                return null;
            }
            return f;
        }

        public static string EncodeHashMessage(string hash, string path)
        {
            HashInfo h = new HashInfo
            {
                Hash = hash,
                Path = path
            };
            string json = JsonConvert.SerializeObject(h, Formatting.Indented);

            return json + "<EOF>";
        }

        public static HashInfo DecodeHashMessage(string s)
        {
            string s1 = s.Replace("<EOF>", "");
            HashInfo h;
            try
            {
                h = JsonConvert.DeserializeObject<HashInfo>(s1);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
                return null;
            }
            return h;
        }

        public static string EncodeSpaceInfoMessage(long used, long total)
        {
            SpaceInfo h = new SpaceInfo
            {
                SpaceUsed = used,
                TotalSpace = total
            };
            string json = JsonConvert.SerializeObject(h, Formatting.Indented);

            return json + "<EOF>";
        }

        public static SpaceInfo DecodeSpaceInfoMessage(string s)
        {
            string s1 = s.Replace("<EOF>", "");
            SpaceInfo h;
            try
            {
                h = JsonConvert.DeserializeObject<SpaceInfo>(s1);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
                return null;
            }
            return h;
        }

        public static string EncodeAckMessage()
        {
            string j = JsonConvert.SerializeObject(commands.ACK, Formatting.Indented);
            return j + "<EOF>";
        }

        public static string EncodeErrorMessage()
        {
            string j = JsonConvert.SerializeObject(commands.ERROR, Formatting.Indented);
            return j + "<EOF>";
        }

        public static string EncodeFinishMessage()
        {
            string j = JsonConvert.SerializeObject(commands.FINISH, Formatting.Indented);
            return j + "<EOF>";
        }

        public static commands DecodeResponseMessage(string r)
        {
            string s1 = r.Replace("<EOF>", "");
            commands c;
            try
            {
                c = JsonConvert.DeserializeObject<commands>(s1);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
                return commands.UNKNOWN;
            }
            return c;
        }

        public static byte[] StringToByte(string s)
        {
            byte[] m = Encoding.UTF8.GetBytes(s);
            return m;
        }


    }

    public class HashInfo
    {
        public string Hash { get; set; }
        public string Path { get; set; }
    }

    public class PresentationInfo
    {
        public string Name { get; set; }
        public string Psw { get; set; }
        public commands cmd { get; set; }
    }

    [Serializable]
    public class ClientFileInfo : IEquatable<ClientFileInfo>
    {
        public ClientFileInfo() { }
        public ClientFileInfo(FileInfo c)
        {
            Name = c.Name;
            Size = c.Length;
            Path = c.FullName;
            LastAcces = c.LastAccessTime;
            LastUpd = c.LastWriteTime;
            IsFolder = c.Attributes.HasFlag(FileAttributes.Directory);
        }
        public string Name { get; set; }
        public string OldPath { get; set; }
        public long Size { get; set; }
        public string Path { get; set; }
        public DateTime LastAcces { get; set; }
        public DateTime LastUpd { get; set; }
        public string Hash { get; set; }
        public string ContentHash { get; set; }
        public string RelativePath { get; set; }
        public bool IsFolder { get; set; }

        public bool Equals(ClientFileInfo other)
        {
            ClientFileInfo c = other;

            if (Name == c.Name && OldPath == c.OldPath && Size == c.Size /*&& Path == c.Path && LastAcces == c.LastAcces && LastUpd == c.LastUpd && Hash == c.Hash*/) return true;
            else return false;
        }
    }

    public class LoginInfo
    {
        public string Name { get; set; }
        public string PswSalt { get; set; }
        public string SessionSalt { get; set; }
    }

    public class SpaceInfo
    {
        public long SpaceUsed { get; set; }
        public long TotalSpace { get; set; }

    }



    //quando si richiede tutto lo storico bisogna utilizzare un vettore di FileVersions
    //Ad ogni elemento del vettore corrisponde un file
    public class FileVersions
    {
        //queste info riguardano il file in esame
        //corrispondono a quelle della versione più recente
        //in modo da potervi accedere subito in fase di visualizzazione
        public FileVersions() { }
        public FileVersions(FileInfo c)
        {
            Name = c.Name;
            Size = c.Length;
            Path = c.FullName;
            LastAcces = c.LastAccessTime;
            LastUpd = c.LastWriteTime;
        }
        public FileVersions(ClientFileInfo c)
        {
            Name = c.Name;
            Size = c.Size;
            Path = c.Path;
            LastAcces = c.LastAcces;
            LastUpd = c.LastUpd;
            ContentHash = c.ContentHash;
            RelativePath = c.RelativePath;
            IsFolder = c.IsFolder;
            Hash = c.Hash;
        }
        public string Name { get; set; }
        public long Size { get; set; }
        public string Path { get; set; }
        public DateTime LastAcces { get; set; }
        public DateTime LastUpd { get; set; }
        public string Hash { get; set; }
        public string ContentHash { get; set; }
        public string RelativePath { get; set; }
        public bool IsFolder { get; set; }


        //Questa lista, invece, contiene tutte le versioni
        //del suddetto file
        public List<ClientFileInfo> versions { get; set; }
    }

}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.Odbc;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using System.IO;

namespace cloudbox_server
{
    public static class DatabaseUtils
    {
       private static string server = "127.0.0.1";
       private static string database = "test";
       private static string uid = "root";
       private static string password = "********"; //choose your own
       private static string myConnectionString = "SERVER=" + server + ";" + "DATABASE=" +
        database + ";" + "UID=" + uid + ";" + "PASSWORD=" + password + ";" + " Convert Zero Datetime=True;";

        public static void DeleteFromCurrentConf(string username,ClientFileInfo fi) {

            MySqlConnection conn;
            MySqlCommand cmd;
            MySqlTransaction trans = null;
            string current = username + "curr";
            conn = new MySqlConnection(myConnectionString);
            try
            {
                conn.Open();
                cmd = conn.CreateCommand();
                trans = conn.BeginTransaction();
                cmd.Transaction = trans;
                cmd.CommandText = "SET autocommit = 0";
                cmd.ExecuteNonQuery();
                cmd.CommandText = "DELETE FROM "+current+" WHERE `hash`=\""+fi.Hash+"\" ";  
                cmd.ExecuteNonQuery();
                DirectoryInfo dir = new DirectoryInfo(TcpSslServer.SslTcpServer.FileStoragePath + username);
                long totalsize = dir.GetFiles().Sum(file => file.Length);
                cmd.CommandText = "UPDATE `users` SET `SpaceUsed`="+totalsize+" WHERE `username`='" + username + "'";
                cmd.ExecuteNonQuery();
                trans.Commit();
                Console.WriteLine("committed!");
                
            }
            catch (Exception ex)
            {
                try
                {
                    if (trans != null)
                        trans.Rollback();
                    throw new Exception(ex.Message);
                }
                catch (MySqlException mse)
                {
                    Console.WriteLine(mse);
                    throw new Exception(ex.Message);
                }
            }
        
        }

        public static void PurgeAll(string username)
        {

            MySqlConnection conn;
            MySqlCommand cmd;
            MySqlTransaction trans = null;
            string current = username + "curr";
            conn = new MySqlConnection(myConnectionString);
            try
            {
                conn.Open();
                cmd = conn.CreateCommand();
                trans = conn.BeginTransaction();
                cmd.Transaction = trans;
                cmd.CommandText = "SET autocommit = 0";
                cmd.ExecuteNonQuery();
                cmd.CommandText = "DELETE FROM "+current+" WHERE  1";
                cmd.ExecuteNonQuery();
                cmd.CommandText = "DELETE FROM "+username+" WHERE  1";
                cmd.ExecuteNonQuery();
                cmd.CommandText = "UPDATE `users` SET `SpaceUsed`= 0 WHERE `username`='" + username + "'";
                cmd.ExecuteNonQuery();
                trans.Commit();
                Console.WriteLine("committed!");

            }
            catch (Exception ex)
            {
                try
                {
                    if (trans != null)
                        trans.Rollback();
                    throw new Exception(ex.Message);
                }
                catch (MySqlException mse)
                {
                    Console.WriteLine(mse);
                    throw new Exception(ex.Message);
                }
            }

        }

        //It will connect to database and insert new entry for "utente" and "currentconf" tables
        public static void InsertNewFile(string username, string hash, string fileName, string clientPath, long size, DateTime LastAcces, DateTime LastUpd)
        {
            MySqlConnection conn;
            MySqlCommand cmd;
            MySqlTransaction trans=null;
            clientPath = clientPath.Replace("\\", "\\\\");
            conn = new MySqlConnection( myConnectionString);
            try
            {
                conn.Open();
                cmd = conn.CreateCommand();
                trans = conn.BeginTransaction();
                cmd.Transaction = trans;
                cmd.CommandText = "SET autocommit = 0";
                cmd.ExecuteNonQuery();
                cmd.CommandText = "INSERT INTO `utente`(`hash`, `filename`, `clientpath`,`size`, `lastAccess`, `lastUpd`) VALUES (\"" + hash + "\",\"" + fileName + "\",\"" + clientPath + "\"," + size + ",\"" + LastAcces.ToString("yyyy-MM-dd HH:mm:ss") + "\",\"" + LastUpd.ToString("yyyy-MM-dd HH:mm:ss") + "\")";
                cmd.ExecuteNonQuery();
                cmd.CommandText = "INSERT INTO `currentconf`(`hash`, `filename`, `clientpath`,`size`, `lastAccess`, `lastUpd`) VALUES (\"" + hash + "\",\"" + fileName + "\",\"" + clientPath + "\"," + size + ",\"" + LastAcces.ToString("yyyy-MM-dd HH:mm:ss") + "\",\"" + LastUpd.ToString("yyyy-MM-dd HH:mm:ss") + "\")";
                cmd.ExecuteNonQuery();
                DirectoryInfo dir = new DirectoryInfo(TcpSslServer.SslTcpServer.FileStoragePath + username);
                long totalsize = dir.GetFiles().Sum(file => file.Length);
                cmd.CommandText = "UPDATE `users` SET `SpaceUsed`="+totalsize+" WHERE `username`='" + username + "'";
                cmd.ExecuteNonQuery();
                trans.Commit();
                Console.WriteLine("committed!");
            }
            catch (Exception ex)
            {
                try
                {
                    if(trans!=null)
                    trans.Rollback();
                    throw new Exception(ex.Message);
                }
                catch(MySqlException mse)
                {
                    Console.WriteLine(mse);
                    throw new Exception(ex.Message);
                }
            }

        }

        public static void InsertNewFile(string username,ClientFileInfo f)
        {
            MySqlConnection conn;
            MySqlCommand cmd;
            MySqlTransaction trans = null;
            string clientPath = f.Path.Replace("\\", "\\\\");
            string relativePath = f.RelativePath.Replace("\\", "\\\\");
            conn = new MySqlConnection(myConnectionString);
            string current = username + "curr";
            try
            {
                conn.Open();
                cmd = conn.CreateCommand();
                trans = conn.BeginTransaction();
                cmd.Transaction = trans;
                cmd.CommandText = "SET autocommit = 0";
                cmd.ExecuteNonQuery();
                cmd.CommandText = "INSERT INTO "+username+"(`hash`,`contenthash`, `filename`, `clientpath`,`relativepath`,`size`, `lastAccess`, `lastUpd`) VALUES (\"" + f.Hash + "\",\"" + f.ContentHash + "\",'"+f.Name+"',  \"" + clientPath + "\", '"+relativePath+"', " + f.Size + ",\"" + f.LastAcces.ToString("yyyy-MM-dd HH:mm:ss") + "\",\"" + f.LastUpd.ToString("yyyy-MM-dd HH:mm:ss") + "\")";
                cmd.ExecuteNonQuery();
                cmd.CommandText = "INSERT INTO "+current+"(`hash`,`contenthash`, `filename`, `clientpath`,`relativepath`,`size`, `lastAccess`, `lastUpd`) VALUES (\"" + f.Hash + "\",\"" + f.ContentHash + "\", '"+f.Name+"', \"" + clientPath + "\",'"+relativePath+"',  " + f.Size + ",\"" + f.LastAcces.ToString("yyyy-MM-dd HH:mm:ss") + "\",\"" + f.LastUpd.ToString("yyyy-MM-dd HH:mm:ss") + "\")";
                cmd.ExecuteNonQuery();
                 DirectoryInfo dir = new DirectoryInfo(TcpSslServer.SslTcpServer.FileStoragePath + username);
                long totalsize = dir.GetFiles().Sum(file => file.Length);
                cmd.CommandText = "UPDATE `users` SET `SpaceUsed`="+totalsize+ " WHERE `username`='" + username + "'";
                cmd.ExecuteNonQuery();
                trans.Commit();
                Console.WriteLine("committed!");
            }
            catch (Exception ex)
            {
                try
                {
                    if (trans != null)
                        trans.Rollback();
                    throw new Exception(ex.Message);
                }
                catch (MySqlException mse)
                {
                    Console.WriteLine(mse);
                    throw new Exception(ex.Message);
                }
            }

        }

        //Insert a new fle in table utente and update currentconf
        public static void UpdateFile(string username,string hash, string fileName, string clientPath, long size, DateTime LastAcces, DateTime LastUpd)
        {
            MySqlConnection conn;
            MySqlCommand cmd;
            MySqlTransaction trans = null;
            conn = new MySqlConnection(myConnectionString);
            clientPath = clientPath.Replace("\\", "\\\\");
            try
            {
                conn.Open();
                cmd = conn.CreateCommand();
                trans = conn.BeginTransaction();
                cmd.Transaction = trans;
                cmd.CommandText = "SET autocommit = 0";
                cmd.ExecuteNonQuery();
                cmd.CommandText = "INSERT INTO `utente`(`hash`, `filename`, `clientpath`,`size`, `lastAccess`, `lastUpd`) VALUES (\"" + hash + "\",\"" + fileName + "\",\"" + clientPath + "\"," + size + ",\"" + LastAcces.ToString("yyyy-MM-dd HH:mm:ss") + "\",\"" + LastUpd.ToString("yyyy-MM-dd HH:mm:ss") + "\")";
                cmd.ExecuteNonQuery();
                DirectoryInfo dir = new DirectoryInfo(TcpSslServer.SslTcpServer.FileStoragePath + username);
                long totalsize = dir.GetFiles().Sum(file => file.Length);
                cmd.CommandText = "UPDATE `users` SET `SpaceUsed`="+totalsize+" WHERE `username`='" + username + "'";
                cmd.ExecuteNonQuery();
                cmd.CommandText = "DELETE FROM `currentconf` WHERE `clientpath`= \""+clientPath+"\" ";
                cmd.ExecuteNonQuery();
                cmd.CommandText = "INSERT INTO `currentconf`(`hash`, `filename`, `clientpath`,`size`, `lastAccess`, `lastUpd`) VALUES (\"" + hash + "\",\"" + fileName + "\",\"" + clientPath + "\"," + size + ",\"" + LastAcces.ToString("yyyy-MM-dd HH:mm:ss") + "\",\"" + LastUpd.ToString("yyyy-MM-dd HH:mm:ss") + "\")";
                cmd.ExecuteNonQuery();

                trans.Commit();
                Console.WriteLine("committed!");
            }
            catch (Exception ex)
            {
                try
                {
                    if (trans != null)
                        trans.Rollback();
                    throw new Exception(ex.Message);
                }
                catch (MySqlException mse)
                {
                    Console.WriteLine(mse);
                    throw new Exception(ex.Message);
                }
            }
        }

        public static void UpdateFile(string username, ClientFileInfo f)
        {
            MySqlConnection conn;
            MySqlCommand cmd;
            MySqlTransaction trans = null;
            conn = new MySqlConnection(myConnectionString);
            string clientPath = f.Path.Replace("\\", "\\\\");
            string relativePath = f.RelativePath.Replace("\\", "\\\\");
            string current = username + "curr";
            try
            {
                conn.Open();
                Console.WriteLine("into update");
                cmd = conn.CreateCommand();
                trans = conn.BeginTransaction();
                cmd.Transaction = trans;
                cmd.CommandText = "SET autocommit = 0";
                cmd.ExecuteNonQuery();
                cmd.CommandText = "INSERT INTO "+username+"(`hash`,`contenthash`, `filename`, `clientpath`,`relativepath`,`size`, `lastAccess`, `lastUpd`) VALUES (\"" + f.Hash + "\",\"" + f.ContentHash + "\",'" + f.Name + "',  \"" + clientPath + "\", '" + relativePath + "', " + f.Size + ",\"" + f.LastAcces.ToString("yyyy-MM-dd HH:mm:ss") + "\",\"" + f.LastUpd.ToString("yyyy-MM-dd HH:mm:ss") + "\")";
                cmd.ExecuteNonQuery();
                DirectoryInfo dir = new DirectoryInfo(TcpSslServer.SslTcpServer.FileStoragePath + username);
                long totalsize = dir.GetFiles().Sum(file => file.Length);
                cmd.CommandText = "UPDATE `users` SET `SpaceUsed`=" + totalsize+ " WHERE `username`='" + username + "'";
                cmd.ExecuteNonQuery();
                cmd.CommandText = "DELETE FROM "+current+" WHERE `clientpath`= \"" + clientPath + "\" ";
                cmd.ExecuteNonQuery();
                cmd.CommandText = "INSERT INTO " + current + "(`hash`,`contenthash`, `filename`, `clientpath`,`relativepath`,`size`, `lastAccess`, `lastUpd`) VALUES (\"" + f.Hash + "\",\"" + f.ContentHash + "\", '" + f.Name + "', \"" + clientPath + "\",'" + relativePath + "',  " + f.Size + ",\"" + f.LastAcces.ToString("yyyy-MM-dd HH:mm:ss") + "\",\"" + f.LastUpd.ToString("yyyy-MM-dd HH:mm:ss") + "\")";
                cmd.ExecuteNonQuery();
                trans.Commit();
                Console.WriteLine("committed!");
            }
            catch (Exception ex)
            {
                try
                {
                    if (trans != null)
                        trans.Rollback();
                    throw new Exception(ex.Message);
                }
                catch (MySqlException mse)
                {
                    Console.WriteLine(mse);
                    throw new Exception(ex.Message);
                }
            }
        }

        public static List<ClientFileInfo> GetCurrentConf(string username)
        {
            List<ClientFileInfo> finfo=new List<ClientFileInfo>();
            MySqlConnection conn;
            MySqlCommand cmd;
            MySqlTransaction trans = null;
            MySqlDataReader reader;
            conn = new MySqlConnection(myConnectionString);
            string current = username + "curr";
            try
            {
                conn.Open();
                cmd = conn.CreateCommand();
                trans = conn.BeginTransaction();
                cmd.Transaction = trans;
                cmd.CommandText = "SET autocommit = 0";
                cmd.ExecuteNonQuery();
                cmd.CommandText = "Select * FROM "+current+"  ";
                reader = cmd.ExecuteReader();
                if (!reader.HasRows) {
                    reader.Close();
                    throw new Exception("No rows!!");
                }

                while (reader.Read()) {
                    string hash = reader.GetString("hash");
                    string fileName = reader.GetString("filename");
                    string clientPath = reader.GetString("clientpath");
                    DateTime lastAccess = reader.GetDateTime("lastAccess");
                    DateTime lastUpd = reader.GetDateTime("lastUpd");
                    ClientFileInfo fi = new ClientFileInfo();
                    
                    fi.Hash = hash;
                    fi.Name = fileName;
                    fi.Path = clientPath;
                    fi.Size = reader.GetUInt32("size");
                    fi.RelativePath = reader.GetString("relativepath");
                    fi.ContentHash = reader.GetString("contenthash");
                    fi.LastAcces = lastAccess;
                    fi.LastUpd = lastUpd;
                    finfo.Add(fi);
                }
                reader.Close();
                trans.Commit();
                Console.WriteLine("committed!");
            }
            catch (Exception ex)
            {
                try
                {
                    if (trans != null)
                        trans.Rollback();
                    throw new Exception(ex.Message);
                }
                catch (MySqlException mse)
                {
                    Console.WriteLine(mse);
                    
                    throw new Exception(ex.Message);
                }
            }

            return finfo;
        }


        internal static bool InsertUser(string usr, string pswSalted,string salt)
        {
            MySqlConnection conn;
            MySqlCommand cmd;
            MySqlTransaction trans = null;
            conn = new MySqlConnection(myConnectionString);
            string current = usr + "curr";
            try
            {
                conn.Open();
                cmd = conn.CreateCommand();
                trans = conn.BeginTransaction();
                cmd.Transaction = trans;
                cmd.CommandText = "SET autocommit = 0";
                cmd.ExecuteNonQuery();
                cmd.CommandText = "INSERT INTO `users`(`username`, `password`,`salt`) VALUES (\"" + usr + "\",\"" + pswSalted + "\",\"" +salt+ "\")";
                cmd.ExecuteNonQuery();
                cmd.CommandText = "CREATE TABLE "+usr+" LIKE utente";
                cmd.ExecuteNonQuery();
                cmd.CommandText = "delete from " + usr + " where 1";
                cmd.ExecuteNonQuery();
                cmd.CommandText = "CREATE TABLE " + current + " LIKE currentconf";
                cmd.ExecuteNonQuery();
                cmd.CommandText = "delete from " + current + " where 1";
                cmd.ExecuteNonQuery();
                trans.Commit();
                Console.WriteLine("committed!");
            }
            catch (Exception ex)
            {
                try
                {
                    if (trans != null)
                        trans.Rollback();
                    throw new Exception(ex.Message);
                }
                catch (MySqlException mse)
                {
                    Console.WriteLine(mse);
                    throw new Exception(ex.Message);
                }
            }
            return true;
            
        }

        internal static bool IsAuth(string usr, string token)
        {
            MySqlConnection conn;
            MySqlCommand cmd;
            MySqlTransaction trans = null;
            MySqlDataReader reader;
            int n = -1;

            conn = new MySqlConnection(myConnectionString);
            try
            {
                conn.Open();
                cmd = conn.CreateCommand();
                trans = conn.BeginTransaction();
                cmd.Transaction = trans;
                cmd.CommandText = "SET autocommit = 0";
                cmd.ExecuteNonQuery();
                cmd.CommandText = "SELECT count(*) as num from users where `username`=\"" + usr + "\" and `sessionToken`=\"" + token + "\" and valid=true";
                reader = cmd.ExecuteReader();
                if (!reader.HasRows)
                {
                    reader.Close();
                    throw new Exception("No rows!!");
                }

                while (reader.Read())
                {
                    n = reader.GetInt32("num");
                    Console.WriteLine(n);
                }
                reader.Close();
                trans.Commit();
                conn.Close();
                Console.WriteLine("committed!");
            }

            catch (Exception ex)
            {
                try
                {
                    if (trans != null)
                        trans.Rollback();
                    throw new Exception(ex.Message);
                }
                catch (MySqlException mse)
                {
                    Console.WriteLine(mse);
                    throw new Exception(ex.Message);
                }
            }
            return n > 0;

        }

        internal static bool ExistUser(string usr, string psw)
        {
            MySqlConnection conn;
            MySqlCommand cmd;
            MySqlTransaction trans = null;
            MySqlDataReader reader;
            int n=-1;

            conn = new MySqlConnection(myConnectionString);
             try
            {
                conn.Open();
                cmd = conn.CreateCommand();
                trans = conn.BeginTransaction();
                cmd.Transaction = trans;
                cmd.CommandText = "SET autocommit = 0";
                cmd.ExecuteNonQuery();
                cmd.CommandText = "SELECT count(*) as num from users where `username`=\"" + usr + "\" and `sessionToken`=\"" + psw + "\" ";
                reader = cmd.ExecuteReader();
                if (!reader.HasRows) {
                    reader.Close();
                    throw new Exception("No rows!!");
                }

                while (reader.Read()) {
                    n = reader.GetInt32("num");
                    Console.WriteLine(n);
                }
                reader.Close();
                trans.Commit();
                conn.Close();
                Console.WriteLine("committed!");
            }
            
            catch (Exception ex)
            {
                try
                {
                    if (trans != null)
                        trans.Rollback();
                    throw new Exception(ex.Message);
                }
                catch (MySqlException mse)
                {
                    Console.WriteLine(mse);
                    throw new Exception(ex.Message);
                }
            }
            return n>0;

        }
        internal static bool ExistUserName(string usr)
        {
            MySqlConnection conn;
            MySqlCommand cmd;
            MySqlTransaction trans = null;
            MySqlDataReader reader;
            int n = -1;

            conn = new MySqlConnection(myConnectionString);
            try
            {
                conn.Open();
                cmd = conn.CreateCommand();
                trans = conn.BeginTransaction();
                cmd.Transaction = trans;
                cmd.CommandText = "SET autocommit = 0";
                cmd.ExecuteNonQuery();
                cmd.CommandText = "SELECT count(*) as num from users where `username`=\"" + usr + "\" ";
                reader = cmd.ExecuteReader();
                if (!reader.HasRows)
                {
                    reader.Close();
                    throw new Exception("No rows!!");
                }

                while (reader.Read())
                {
                    n = reader.GetInt32("num");
                }
                reader.Close();
                trans.Commit();
                conn.Close();
                Console.WriteLine("committed!");
            }

            catch (Exception ex)
            {
                try
                {
                    if (trans != null)
                        trans.Rollback();
                    throw new Exception(ex.Message);
                }
                catch (MySqlException mse)
                {
                    Console.WriteLine(mse);
                    throw new Exception(ex.Message);
                }
            }
            return n > 0;

        }

        internal static string getUserSalt(string username)
        {
            MySqlConnection conn;
            MySqlCommand cmd;
            MySqlTransaction trans = null;
            MySqlDataReader reader;
            string p = "";

            conn = new MySqlConnection(myConnectionString);
            try
            {
                conn.Open();
                cmd = conn.CreateCommand();
                trans = conn.BeginTransaction();
                cmd.Transaction = trans;
                cmd.CommandText = "SET autocommit = 0";
                cmd.ExecuteNonQuery();
                cmd.CommandText = "SELECT `salt` from users where `username`=\"" + username + "\" ";
                reader = cmd.ExecuteReader();
                if (!reader.HasRows)
                {
                    reader.Close();
                    throw new Exception("No rows!!");
                }

                while (reader.Read())
                {
                    p = reader.GetString("salt");
                    Console.WriteLine(p);

                }
                reader.Close();
                trans.Commit();
                conn.Close();
                Console.WriteLine("committed!");
                return p;
            }

            catch (Exception ex)
            {
                try
                {
                    if (trans != null)
                        trans.Rollback();
                    throw new Exception(ex.Message);
                }
                catch (MySqlException mse)
                {
                    Console.WriteLine(mse);
                    throw new EntryPointNotFoundException("database error");
                }
            }
           
        }

        internal static string getPassword(string username)
        {
            MySqlConnection conn;
            MySqlCommand cmd;
            MySqlTransaction trans = null;
            MySqlDataReader reader;
            string p="";

            conn = new MySqlConnection(myConnectionString);
            try
            {
                conn.Open();
                cmd = conn.CreateCommand();
                trans = conn.BeginTransaction();
                cmd.Transaction = trans;
                cmd.CommandText = "SET autocommit = 0";
                cmd.ExecuteNonQuery();
                cmd.CommandText = "SELECT password from users where `username`=\"" + username + "\" ";
                reader = cmd.ExecuteReader();
                if (!reader.HasRows)
                {
                    reader.Close();
                    throw new Exception ("No rows!!");
                }

                while (reader.Read())
                {
                    p = reader.GetString("password");
                    Console.WriteLine(p);
                    
                }
                reader.Close();
                trans.Commit();
                conn.Close();
                Console.WriteLine("committed!");
                return p;
            }

            catch (Exception ex)
            {
                try
                {
                    if (trans != null)
                        trans.Rollback();
                    throw new Exception(ex.Message);
                }
                catch (MySqlException mse)
                {
                    Console.WriteLine(mse);
                    throw new EntryPointNotFoundException("database error");
                }
            }
            

        }

        internal static void UpdateUserToken(string username, string token)
        {
            MySqlConnection conn;
            MySqlCommand cmd;
            MySqlTransaction trans = null;
            conn = new MySqlConnection(myConnectionString);
            try
            {
                conn.Open();
                cmd = conn.CreateCommand();
                trans = conn.BeginTransaction();
                cmd.Transaction = trans;
                cmd.CommandText = "SET autocommit = 0";
                cmd.ExecuteNonQuery();
                cmd.CommandText = " UPDATE `users` SET `sessionToken`=\""+token+"\", valid=true WHERE `username` = \""+username+"\" ";
                cmd.ExecuteNonQuery();

                trans.Commit();
                conn.Close();
                Console.WriteLine("committed!");
            }
            catch (Exception ex)
            {
                try
                {
                    if (trans != null)
                        trans.Rollback();
                    throw new Exception(ex.Message);
                }
                catch (MySqlException mse)
                {
                    Console.WriteLine(mse);
                    throw new Exception(ex.Message);
                }
            }
            
        }

        internal static bool ExistFile(string username,string hash)
        {
            MySqlConnection conn;
            MySqlCommand cmd;
            MySqlTransaction trans = null;
            MySqlDataReader reader;
            int n = -1;
            string current = username + "curr";
            conn = new MySqlConnection(myConnectionString);
            try
            {
                conn.Open();
                cmd = conn.CreateCommand();
                trans = conn.BeginTransaction();
                cmd.Transaction = trans;
                cmd.CommandText = "SET autocommit = 0";
                cmd.ExecuteNonQuery();
                cmd.CommandText = "SELECT count(*) as num from "+current+" where `hash`=\"" + hash+ "\" ";
                reader = cmd.ExecuteReader();
                if (!reader.HasRows)
                {
                    reader.Close();
                    throw new Exception("No rows!!");
                }

                while (reader.Read())
                {
                    n = reader.GetInt32("num");
                }
                reader.Close();
                trans.Commit();
                conn.Close();
                Console.WriteLine("committed!");
            }

            catch (Exception ex)
            {
                try
                {
                    if (trans != null)
                        trans.Rollback();
                    throw new Exception(ex.Message);
                }
                catch (MySqlException mse)
                {
                    Console.WriteLine(mse);
                    throw new Exception(ex.Message);
                }
            }
            return n > 0;

        }

        internal static List<FileVersions> getAllVersions(string username)
        {
            //Creo una lista di FileVersion
            //Faccio una query in cui seleziono tutti i file dello storico where path is equal
            //per ognuno di questi aggiungo un elemento alla lista di FileVersion
            //faccio un'altra query in cui seleziono tutte le versioni di quel file e 
            //riempio la lista di versioni nella struttura FileVersion.
            //ritorno la lista di FileVersion
           
            List<FileVersions> versions=new List<FileVersions>();
            MySqlConnection conn;
            MySqlCommand cmd;
            MySqlTransaction trans = null;
            MySqlDataReader reader;
            conn = new MySqlConnection(myConnectionString);
            try
            {
                conn.Open();
                cmd = conn.CreateCommand();
                trans = conn.BeginTransaction();
                cmd.Transaction = trans;
                cmd.CommandText = "SET autocommit = 0";
                cmd.ExecuteNonQuery();
                cmd.CommandText = " SELECT * FROM "+username+" GROUP BY `clientpath` having max(`timestamp`)  ";
                reader = cmd.ExecuteReader();
                if (!reader.HasRows) {
                    reader.Close();
                    throw new Exception("No rows!!");
                }

                while (reader.Read()) {
                    string hash = reader.GetString("hash");
                    string fileName = reader.GetString("filename");
                    string clientPath = reader.GetString("clientpath");
                    DateTime lastAccess = reader.GetDateTime("lastAccess");
                    DateTime lastUpd = reader.GetDateTime("lastUpd");
                    FileVersions fv = new FileVersions();
                    fv.Hash = hash;
                    fv.Name = fileName;
                    fv.Path = clientPath;
                    fv.Size = reader.GetUInt32("size");
                    fv.LastAcces = lastAccess;
                    fv.LastUpd = lastUpd;
                    fv.ContentHash = reader.GetString("contenthash");
                    fv.RelativePath = reader.GetString("relativepath");                    
                    versions.Add(fv);
                }
                reader.Close();
                

                //fare la seconda query                 
                for (int i=0; i < versions.Count; i++)
                {
                    MySqlDataReader reader2;
                    
                    string clientPath = versions[i].Path.Replace("\\", "\\\\");
                   // Console.WriteLine(versions[i].Path+ " ---> "+clientPath );
                   // cmd.CommandText = " SELECT * FROM `utente` WHERE `clientpath`=\"" + versions[i].Path+ "\" ";
                    cmd.CommandText = " SELECT * FROM  "+username+" WHERE `clientpath`='" + clientPath + "' ";

                    reader2 = cmd.ExecuteReader();
                    if (!reader2.HasRows)
                    {
                        reader2.Close();
                        throw new Exception("2No rows!!");
                    }
                    versions[i].versions= new List<ClientFileInfo>();
                    while (reader2.Read())
                    {
                        ClientFileInfo cfi = new ClientFileInfo();
                        cfi.Hash = reader2.GetString("hash");
                        cfi.Name = reader2.GetString("filename");
                        cfi.Path = reader2.GetString("clientpath");
                        cfi.LastAcces = reader2.GetDateTime("lastAccess");
                        cfi.LastUpd = reader2.GetDateTime("lastUpd");
                        cfi.RelativePath = reader2.GetString("relativepath");
                        cfi.ContentHash = reader2.GetString("contenthash");
                        cfi.Size = reader2.GetInt32("size");
                        versions[i].versions.Add(cfi);                       
                    }
                    reader2.Close();
                }
                
                trans.Commit();
                Console.WriteLine("committed getAllVersion!");
            }
            catch (Exception ex)
            {
                try
                {
                    if (trans != null)
                        trans.Rollback();
                    throw new Exception(ex.Message);
                }
                catch (MySqlException mse)
                {
                    Console.WriteLine(mse);
                    throw new Exception(ex.Message);
                }
            }

            return versions;
        }




        internal static SpaceInfo GetSpaces(string username)
        {
            MySqlConnection conn;
            MySqlCommand cmd;
            MySqlTransaction trans = null;
            MySqlDataReader reader;

            conn = new MySqlConnection(myConnectionString);
            try
            {
                SpaceInfo sp = new SpaceInfo();
                conn.Open();
                cmd = conn.CreateCommand();
                trans = conn.BeginTransaction();
                cmd.Transaction = trans;
                cmd.CommandText = "SET autocommit = 0";
                cmd.ExecuteNonQuery();
                cmd.CommandText = "SELECT space, SpaceUsed from users where `username`=\"" + username + "\" ";                
                reader = cmd.ExecuteReader();
                if (!reader.HasRows)
                {
                    reader.Close();
                    throw new Exception("No rows!!");
                }

                while (reader.Read())
                {
                    sp.TotalSpace = (long)reader.GetInt64("space");
                    sp.SpaceUsed = (long)reader.GetInt64("SpaceUsed");
                    Console.WriteLine("spazio totale: " + sp.TotalSpace+" spazio usato "+sp.SpaceUsed);
                }
                reader.Close();
                trans.Commit();
                conn.Close();
                Console.WriteLine("committed!");
                return sp;
            }

            catch (Exception ex)
            {
                try
                {
                    if (trans != null)
                        trans.Rollback();
                    throw new Exception(ex.Message);
                }
                catch (MySqlException mse)
                {
                    Console.WriteLine(mse);
                    throw new Exception(ex.Message);
                }
            }

        }

        internal static int CanInsert(string username, long size, string clientPath)
        {
            MySqlConnection conn;
            MySqlCommand cmd;
            MySqlTransaction trans = null;
            MySqlDataReader reader,reader2;
            int numV = 0;
            long space;
            int n=-1;
            clientPath = clientPath.Replace("\\", "\\\\");

            conn = new MySqlConnection(myConnectionString);
            try
            {
                conn.Open();
                cmd = conn.CreateCommand();
                trans = conn.BeginTransaction();
                cmd.Transaction = trans;
                cmd.CommandText = "SET autocommit = 0";
                cmd.ExecuteNonQuery();
               
                cmd.CommandText = "SELECT SpaceUsed, space, vpf from users where `username`=\"" + username + "\" ";
                reader2 = cmd.ExecuteReader();
                if (!reader2.HasRows)
                {
                    reader2.Close();
                    throw new Exception("No rows!!");
                }

                while (reader2.Read())
                {
                    numV = reader2.GetInt32("vpf");
                    long spaceUsed=(long)reader2.GetInt64("SpaceUsed");
                    space = reader2.GetInt64("space");
                    Console.WriteLine("vpf: " + numV + " spaceUsed: " + spaceUsed+" total space "+space);
                    if ( size>(space-spaceUsed))
                    {
                        reader2.Close();
                        throw new Exception("Impossibile aggiungere nuovi file: spazio non disponibile");
                    }
                }
                reader2.Close();

                cmd.CommandText = "SELECT count(*) as n from "+username+" where `clientpath`='" + clientPath + "' ";
                reader = cmd.ExecuteReader();
                if (!reader.HasRows)
                {
                    reader.Close();
                    throw new Exception("No rows!!");
                }

                while (reader.Read())
                {
                    n=reader.GetInt32("n");
                    Console.WriteLine("versioni per "+clientPath+": "+n);                    
                }
                reader.Close();

                if (n >= numV && n!=-1)
                {
                    //rimuovere il file più vecchio
                    cmd.CommandText = "SELECT hash from "+username+" where `clientpath`='" + clientPath + "' group by clientpath having(min(lastupd)) ";
                    reader2 = cmd.ExecuteReader();
                    if (!reader2.HasRows)
                    {
                        reader2.Close();
                        throw new Exception("No rows!!");
                    }
                    string h = null;
                    while (reader2.Read())
                    {
                        h = reader2.GetString("hash");
                    }
                    try
                    {
                        File.Delete(TcpSslServer.SslTcpServer.FileStoragePath + "\\" + username + "\\" + h);
                    }
                    catch (Exception)
                    {
                        reader2.Close();
                        reader.Close();
                        throw new Exception();
                    }                    
                    reader2.Close();
                    cmd.CommandText = "delete from "+username+" where hash='" + h + "'";
                    cmd.ExecuteNonQuery();
                }

                trans.Commit();
                conn.Close();
                Console.WriteLine("committed!");
                return 1;
            }

            catch (Exception ex)
            {
                try
                {
                    if (trans != null)
                        trans.Rollback();
                    throw new Exception(ex.Message);
                }
                catch (MySqlException mse)
                {
                    Console.WriteLine(mse);
                    throw new Exception(ex.Message);
                }
            }
        }

        internal static bool RenameFile(string username, ClientFileInfo fi)
        {
            MySqlConnection conn;
            MySqlCommand cmd;
            MySqlTransaction trans = null;
            string newPath = fi.Path.Replace("\\", "\\\\");
            string newName = fi.Name.Replace("\\", "\\\\");
            string oldpath = fi.OldPath.Replace("\\", "\\\\");
            string relativePath = fi.RelativePath.Replace("\\", "\\\\");
            conn = new MySqlConnection(myConnectionString);
            string current = username + "curr";
            try
            {
                conn.Open();
                cmd = conn.CreateCommand();
                trans = conn.BeginTransaction();
                cmd.Transaction = trans;
                cmd.CommandText = "SET autocommit = 0";
                cmd.ExecuteNonQuery();                    
                        cmd.CommandText = "DELETE FROM "+current+" WHERE `clientpath`= \"" + oldpath + "\" ";
                        cmd.ExecuteNonQuery();
                        cmd.CommandText = "INSERT INTO "+username+"(`hash`,`contenthash`, `filename`, `clientpath`,`relativepath`,`size`, `lastAccess`, `lastUpd`) VALUES (\"" + fi.Hash + "\",\"" + fi.ContentHash + "\", '"+fi.Name+"',  \"" + newPath + "\", '" + relativePath + "', " + fi.Size + ",\"" + fi.LastAcces.ToString("yyyy-MM-dd HH:mm:ss") + "\",\"" + fi.LastUpd.ToString("yyyy-MM-dd HH:mm:ss") + "\")";
                        cmd.ExecuteNonQuery();
                        cmd.CommandText = "INSERT INTO " + current + "(`hash`,`contenthash`, `filename`, `clientpath`,`relativepath`,`size`, `lastAccess`, `lastUpd`) VALUES (\"" + fi.Hash + "\",\"" + fi.ContentHash + "\", '" + fi.Name + "', \"" + newPath + "\",'" + relativePath + "',  " + fi.Size + ",\"" + fi.LastAcces.ToString("yyyy-MM-dd HH:mm:ss") + "\",\"" + fi.LastUpd.ToString("yyyy-MM-dd HH:mm:ss") + "\")";
                        cmd.ExecuteNonQuery();
                        DirectoryInfo dir = new DirectoryInfo(TcpSslServer.SslTcpServer.FileStoragePath + username);
                        long totalsize = dir.GetFiles().Sum(file => file.Length);
                        cmd.CommandText = "UPDATE `users` SET `SpaceUsed`="+totalsize+" WHERE `username`='" + username + "'";
                        cmd.ExecuteNonQuery();                   
                trans.Commit();
                Console.WriteLine("committed!");
                return true;
            }
            catch (Exception ex)
            {
                try
                {
                    if (trans != null)
                        trans.Rollback();
                    throw new Exception(ex.Message);
                }
                catch (MySqlException mse)
                {
                    Console.WriteLine(mse);
                    throw new Exception(ex.Message);
                }
            }   
        }

        internal static string GetHashFromPath(string username,string p)
        {
            MySqlConnection conn;
            MySqlCommand cmd;
            MySqlTransaction trans = null;
            MySqlDataReader reader;
            string h = null;
            conn = new MySqlConnection(myConnectionString);
            string current = username + "curr";
            try
            {
                Console.WriteLine(p);
                conn.Open();
                cmd = conn.CreateCommand();
                trans = conn.BeginTransaction();
                cmd.Transaction = trans;
                cmd.CommandText = "SET autocommit = 0";
                cmd.ExecuteNonQuery();
                cmd.CommandText = "SELECT hash from "+current+" where `clientpath`=\"" + p.Replace("\\", "\\\\") + "\" ";
                reader = cmd.ExecuteReader();
                if (!reader.HasRows)
                {
                    reader.Close();
                    Console.WriteLine(p + ": no such file or directory in db");
                    throw new Exception("No rows!!");
                }

                while (reader.Read())
                {
                    h = reader.GetString("hash");
                    Console.WriteLine(h);
                }
                reader.Close();
                trans.Commit();
                conn.Close();
                Console.WriteLine("committed!");
                return h;
            }

            catch (Exception ex)
            {
                try
                {
                    if (trans != null)
                        trans.Rollback();
                    throw new Exception(ex.Message);
                }
                catch (MySqlException mse)
                {
                    Console.WriteLine(mse);
                    throw new Exception(ex.Message);
                }
            }
            
        }

        internal static void Logout(string username)
        {
            MySqlConnection conn;
            MySqlCommand cmd;
            MySqlTransaction trans = null;
            conn = new MySqlConnection(myConnectionString);
            try
            {
                conn.Open();
                cmd = conn.CreateCommand();
                trans = conn.BeginTransaction();
                cmd.Transaction = trans;
                cmd.CommandText = "SET autocommit = 0";
                cmd.ExecuteNonQuery();
                cmd.CommandText = "UPDATE `users` SET `valid`= false WHERE `username`='" + username + "'";
                cmd.ExecuteNonQuery();
                trans.Commit();
                Console.WriteLine("committed!");
            }
            catch (Exception ex)
            {
                try
                {
                    if (trans != null)
                        trans.Rollback();
                    throw new Exception(ex.Message);
                }
                catch (MySqlException mse)
                {
                    Console.WriteLine(mse);
                    throw new Exception(ex.Message);
                }
            }   
        }

        internal static List<string> GetAllUsers()
        {
            List<string> users= new List<string>();
            MySqlConnection conn;
            MySqlCommand cmd;
            MySqlTransaction trans = null;
            MySqlDataReader reader;
            conn = new MySqlConnection(myConnectionString);
            try
            {
                conn.Open();
                cmd = conn.CreateCommand();
                trans = conn.BeginTransaction();
                cmd.Transaction = trans;
                cmd.CommandText = "SET autocommit = 0";
                cmd.ExecuteNonQuery();
                cmd.CommandText = "Select username FROM `users`";
                reader = cmd.ExecuteReader();
                if (!reader.HasRows)
                {
                    reader.Close();
                    throw new Exception("No rows!!");
                }

                while (reader.Read())
                {
                    users.Add(reader.GetString("username"));
                }
                reader.Close();
                trans.Commit();
                Console.WriteLine("committed!");
            }
            catch (Exception ex)
            {
                try
                {
                    if (trans != null)
                        trans.Rollback();
                    throw new Exception(ex.Message);
                }
                catch (MySqlException mse)
                {
                    Console.WriteLine(mse);

                    throw new Exception(ex.Message);
                }
            }

            return users;
        }

        internal static bool IsFalseUpdate(string username,ClientFileInfo f)
        {
            //se nella current configuration esiste un file con quel path e quel contenthash
            MySqlConnection conn;
            MySqlCommand cmd;
            MySqlTransaction trans = null;
            MySqlDataReader reader;
            int n = -1;
            string current = username + "curr";
            conn = new MySqlConnection(myConnectionString);
            try
            {
                conn.Open();
                cmd = conn.CreateCommand();
                trans = conn.BeginTransaction();
                cmd.Transaction = trans;
                cmd.CommandText = "SET autocommit = 0";
                cmd.ExecuteNonQuery();
                cmd.CommandText = "SELECT count(*) as num from "+current+" where `clientpath`=\"" + f.Path+ "\" and `contenthash`=\"" + f.ContentHash+ "\" ";
                reader = cmd.ExecuteReader();
                if (!reader.HasRows)
                {
                    reader.Close();
                    throw new Exception("No rows!!");
                }

                while (reader.Read())
                {
                    n = reader.GetInt32("num");
                    Console.WriteLine("False update: "+n);
                }
                reader.Close();
                trans.Commit();
                conn.Close();
                Console.WriteLine("committed!");
            }

            catch (Exception ex)
            {
                try
                {
                    if (trans != null)
                        trans.Rollback();
                    throw new Exception(ex.Message);
                }
                catch (MySqlException mse)
                {
                    Console.WriteLine(mse);
                    throw new Exception(ex.Message);
                }
            }
            return n > 0;
        }

        internal static void DeleteEntry(string username, string p)
        {
            MySqlConnection conn;
            MySqlCommand cmd;
            MySqlTransaction trans = null;
            string current = username + "curr";
            conn = new MySqlConnection(myConnectionString);
            try
            {
                conn.Open();
                cmd = conn.CreateCommand();
                trans = conn.BeginTransaction();
                cmd.Transaction = trans;
                cmd.CommandText = "SET autocommit = 0";
                cmd.ExecuteNonQuery();
                cmd.CommandText = "DELETE FROM "+current+" WHERE `hash`=\"" + p+ "\" ";
                cmd.ExecuteNonQuery();
                cmd.CommandText = "DELETE FROM "+username+" WHERE `hash`=\"" + p + "\" ";
                cmd.ExecuteNonQuery();
                trans.Commit();
                Console.WriteLine("committed!");

            }
            catch (Exception ex)
            {
                try
                {
                    if (trans != null)
                        trans.Rollback();
                    throw new Exception(ex.Message);
                }
                catch (MySqlException mse)
                {
                    Console.WriteLine(mse);
                    throw new Exception(ex.Message);
                }
            }
        }

        internal static void FinalDeleteEntry(string username, string p)
        {
            MySqlConnection conn;
            MySqlCommand cmd;
            MySqlTransaction trans = null;
            string current = username + "curr";
            conn = new MySqlConnection(myConnectionString);
            try
            {
                conn.Open();
                cmd = conn.CreateCommand();
                trans = conn.BeginTransaction();
                cmd.Transaction = trans;
                cmd.CommandText = "SET autocommit = 0";
                cmd.ExecuteNonQuery();
                cmd.CommandText = "DELETE FROM " + current + " WHERE `hash`=\"" + p + "\" ";
                cmd.ExecuteNonQuery();
                cmd.CommandText = "DELETE FROM " + username + " WHERE `hash`=\"" + p + "\" ";
                cmd.ExecuteNonQuery();
                DirectoryInfo dir = new DirectoryInfo(TcpSslServer.SslTcpServer.FileStoragePath + username);
                long totalsize = dir.GetFiles().Sum(file => file.Length);
                cmd.CommandText = "UPDATE `users` SET `SpaceUsed`=" + totalsize + " WHERE `username`='" + username + "'";
                cmd.ExecuteNonQuery();
                trans.Commit();
                Console.WriteLine("committed!");

            }
            catch (Exception ex)
            {
                try
                {
                    if (trans != null)
                        trans.Rollback();
                    throw new Exception(ex.Message);
                }
                catch (MySqlException mse)
                {
                    Console.WriteLine(mse);
                    throw new Exception(ex.Message);
                }
            }
        }

        internal static void DeleteUser(string username)
        {
            MySqlConnection conn;
            MySqlCommand cmd;
            MySqlTransaction trans = null;
            string current = username + "curr";
            conn = new MySqlConnection(myConnectionString);
            try
            {
                conn.Open();
                cmd = conn.CreateCommand();
                trans = conn.BeginTransaction();
                cmd.Transaction = trans;
                cmd.CommandText = "SET autocommit = 0";
                cmd.ExecuteNonQuery();                
                cmd.CommandText = "DELETE FROM users WHERE `username`=\"" + username + "\" ";
                cmd.ExecuteNonQuery();
                trans.Commit();
                Console.WriteLine("committed!");

            }
            catch (Exception ex)
            {
                try
                {
                    if (trans != null)
                        trans.Rollback();
                    throw new Exception(ex.Message);
                }
                catch (MySqlException mse)
                {
                    Console.WriteLine(mse);
                    throw new Exception(ex.Message);
                }
            }
        }

        internal static int getUserVpf(string username)
        {
            MySqlConnection conn;
            MySqlCommand cmd;
            MySqlTransaction trans = null;
            MySqlDataReader reader;
            int n = -1;
            string current = username + "curr";
            conn = new MySqlConnection(myConnectionString);
            try
            {
                conn.Open();
                cmd = conn.CreateCommand();
                trans = conn.BeginTransaction();
                cmd.Transaction = trans;
                cmd.CommandText = "SET autocommit = 0";
                cmd.ExecuteNonQuery();
                cmd.CommandText = "SELECT vpf from users where `username`='"+username+"' ";
                reader = cmd.ExecuteReader();
                if (!reader.HasRows)
                {
                    reader.Close();
                    throw new Exception("No rows!!");
                }

                while (reader.Read())
                {
                    n = reader.GetInt32("vpf");
                }
                reader.Close();
                trans.Commit();
                conn.Close();
                Console.WriteLine("committed!");
                return n;
            }

            catch (Exception ex)
            {
                try
                {
                    if (trans != null)
                        trans.Rollback();
                    throw new Exception(ex.Message);
                }
                catch (MySqlException mse)
                {
                    Console.WriteLine(mse);
                    throw new Exception(ex.Message);
                }
            }
            return n ;
        }

        internal static bool ExistFileIntoHistory(string username, string hash)
        {

            MySqlConnection conn;
            MySqlCommand cmd;
            MySqlTransaction trans = null;
            MySqlDataReader reader;
            int n = -1;
            string current = username + "curr";
            conn = new MySqlConnection(myConnectionString);
            try
            {
                conn.Open();
                cmd = conn.CreateCommand();
                trans = conn.BeginTransaction();
                cmd.Transaction = trans;
                cmd.CommandText = "SET autocommit = 0";
                cmd.ExecuteNonQuery();
                cmd.CommandText = "SELECT count(*) as num from " + username + " where `hash`=\"" + hash + "\" ";
                reader = cmd.ExecuteReader();
                if (!reader.HasRows)
                {
                    reader.Close();
                    throw new Exception("No rows!!");
                }

                while (reader.Read())
                {
                    n = reader.GetInt32("num");
                }
                reader.Close();
                trans.Commit();
                conn.Close();
                Console.WriteLine("committed!");
            }

            catch (Exception ex)
            {
                try
                {
                    if (trans != null)
                        trans.Rollback();
                    throw new Exception(ex.Message);
                }
                catch (MySqlException mse)
                {
                    Console.WriteLine(mse);
                    throw new Exception(ex.Message);
                }
            }
            return n > 0;
        }

        internal static void InsertNewFileIntoCurrentConf(string username, ClientFileInfo f)
        {
            MySqlConnection conn;
            MySqlCommand cmd;
            MySqlTransaction trans = null;
            string clientPath = f.Path.Replace("\\", "\\\\");
            string relativePath = f.RelativePath.Replace("\\", "\\\\");
            conn = new MySqlConnection(myConnectionString);
            string current = username + "curr";
            try
            {
                conn.Open();
                cmd = conn.CreateCommand();
                trans = conn.BeginTransaction();
                cmd.Transaction = trans;
                cmd.CommandText = "SET autocommit = 0";
                cmd.ExecuteNonQuery();
                cmd.CommandText = "INSERT INTO " + current + "(`hash`,`contenthash`, `filename`, `clientpath`,`relativepath`,`size`, `lastAccess`, `lastUpd`) VALUES (\"" + f.Hash + "\",\"" + f.ContentHash + "\", '" + f.Name + "', \"" + clientPath + "\",'" + relativePath + "',  " + f.Size + ",\"" + f.LastAcces.ToString("yyyy-MM-dd HH:mm:ss") + "\",\"" + f.LastUpd.ToString("yyyy-MM-dd HH:mm:ss") + "\")";
                cmd.ExecuteNonQuery();
               
                trans.Commit();
                Console.WriteLine("committed!");
            }
            catch (Exception ex)
            {
                try
                {
                    if (trans != null)
                        trans.Rollback();
                    throw new Exception(ex.Message);
                }
                catch (MySqlException mse)
                {
                    Console.WriteLine(mse);
                    throw new Exception(ex.Message);
                }
            }
        }

        internal static void UpdateUserSpaceAndVpf(string username, long v, long space)
        {
            MySqlConnection conn;
            MySqlCommand cmd;
            MySqlTransaction trans = null;
            string current = username + "curr";
            conn = new MySqlConnection(myConnectionString);
            try
            {
                conn.Open();
                cmd = conn.CreateCommand();
                trans = conn.BeginTransaction();
                cmd.Transaction = trans;
                cmd.CommandText = "SET autocommit = 0";
                cmd.ExecuteNonQuery();
                Console.WriteLine("into db: " + v + " " + space+" username: "+username);
                cmd.CommandText = "UPDATE `users` SET space="+space+", vpf="+v+"  WHERE `username`='" + username + "'";
                cmd.ExecuteNonQuery();
                trans.Commit();
                Console.WriteLine("committed!");

            }
            catch (Exception ex)
            {
                try
                {
                    if (trans != null)
                        trans.Rollback();
                    throw new Exception(ex.Message);
                }
                catch (MySqlException mse)
                {
                    Console.WriteLine(mse);
                    throw new Exception(ex.Message);
                }
            }
        }
    }
    
}

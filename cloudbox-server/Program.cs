using System;
using System.Collections;
using System.Net;
using System.Net.Sockets;
using System.Net.Security;
using System.Security.Authentication;
using System.Text;
using System.Security.Cryptography.X509Certificates;
using System.IO;
using System.Web;
using System.Threading;
using cloudbox_server;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Windows;
using System.Linq;
using System.Threading.Tasks;

//using System.Security.Cryptography.X509Certificates.X509Certificate2;

namespace TcpSslServer
{

    

    public sealed class SslTcpServer
    {
        private static Object LOCK = new Object();
        private static string certificate;
        private static SslTcpServer server;
        private static Thread serverThread;
        //static X509Certificate serverCertificate = null;
        X509Certificate2 serverCertificate2 = null;
        public static string FileStoragePath="C:\\storageServer\\";
        private static TcpListener listener;
        // The certificate parameter specifies the name of the file  
        // containing the machine certificate.
 
        public static void StopServer()
        {
           
                try
                {
                    //await Task.Factory.StartNew(()=>listener.Stop());
                    Interlocked.Exchange(ref Constants.IsStart, 0);
                    Console.WriteLine("Server Stoppato");
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            
        }

        public static void StartServer()
        {
            lock (LOCK)
            {
                try
                {
                    Interlocked.Exchange(ref Constants.IsStart, 1);
                    Monitor.PulseAll(LOCK);
                    Console.WriteLine("Server Avviato");
                }
                catch (Exception e)
                {
                    Console.WriteLine("into start " + e.Message);
                }                
            }
        }


        public void RunServer(string certificate)
        {
            //serverCertificate = X509Certificate.CreateFromCertFile(certificate);
            lock (LOCK)
            {
                try
                {
                    serverCertificate2 = new X509Certificate2();
                    serverCertificate2.Import(certificate);

                    // Create a TCP/IP (IPv4) socket and listen for incoming connections.
                    listener = new TcpListener(IPAddress.Any, 443);
                    listener.Start();
                    Interlocked.Exchange(ref Constants.IsStart, 1);
                    while (true)
                    {
                        while (Interlocked.Equals(Constants.IsStart, 0))
                        {
                            Console.WriteLine("in waiting");
                            Monitor.Wait(LOCK);
                        }                       
                        Console.WriteLine("Waiting for a client to connect...");
                        // Application blocks while waiting for an incoming connection. 
                        // Type CNTL-C to terminate the server.                    
                       TcpClient client = listener.AcceptTcpClient();
                        //qua bisogna sganciare un thread che gestisce il client così il processo
                        //padre torna in ascolto.
                        Thread t = new Thread(() => ProcessClient(client));
                        t.Start();
                        
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("Into run server " + e.Message);
                }
                finally
                {
                }
            }
        }


        void ProcessClient(TcpClient client)
        {
            

            // A client has connected. Create the  
            // SslStream using the client's network stream.
            
            SslStream sslStream = new SslStream(
                client.GetStream(), false);
            // Authenticate the server but don't require the client to authenticate. 
            try
            {

                sslStream.AuthenticateAsServer(serverCertificate2,
                    false, SslProtocols.Tls, true);
                // Display the properties and settings for the authenticated stream.
                CertificateUtils.DisplaySecurityLevel(sslStream);
                CertificateUtils.DisplaySecurityServices(sslStream);
                CertificateUtils.DisplayCertificateInformation(sslStream);
                CertificateUtils.DisplayStreamProperties(sslStream);

                // Set timeouts for the read and write to 5 seconds.
                sslStream.ReadTimeout = 5000;
                sslStream.WriteTimeout = 5000;
                // Read a message from the client.   
                Console.WriteLine("Waiting for client message...");
                string m;
                try
                {
                    m = ReadMessage(sslStream);
                }
                catch (IOException)
                {
                    Console.WriteLine("ReadTimeout exedeed! Connection will be closed...");
                    sslStream.Close();
                    client.Close();
                    return;
                }
                PresentationInfo p = MessageUtils.decodePresentationInfo(m);
                if (p != null) Console.WriteLine("user: " + p.Name + " token: " + p.Psw + " cmd: " + p.cmd);
                if (p != null && p.cmd == commands.HELLO)
                {
                    Hello(sslStream);
                }
                else if (p != null && p.cmd == commands.SIGNUP)
                {
                    SignUp(sslStream, p.Name, p.Psw);
                }
                else if (p != null && p.cmd == commands.LOGIN)
                {
                    Login(sslStream, p.Name);
                }
                else if (p == null || !AuthenticateClient(p.Name, p.Psw))
                {
                    
                    sslStream.Write(MessageUtils.StringToByte(MessageUtils.EncodeErrorMessage()));
                    throw new FormatException();
                }
                else
                {
                    Console.WriteLine(p.Name + " " + p.Psw);
                    dispatchCommands(p.cmd, sslStream,p);
                    //se qualcosa va male nella dispatch 
                    //ritorna in ogni caso qua e quello che viene fatto è chiudere ordinatamente la connessione
                }
            }
            catch (FormatException e)
            {
                Console.WriteLine("Exception: {0} FORMAT ERROR ", e.Message);


            }
            catch (AuthenticationException e)
            {
                Console.WriteLine("Exception: {0}", e.Message);
                if (e.InnerException != null)
                {
                    Console.WriteLine("Inner exception: {0}", e.InnerException.Message);
                }
                Console.WriteLine("Authentication failed - closing the connection.");

            }
            catch (Exception e) {
                Console.WriteLine("Connection error: " + e.Message);
            }
            finally
            {
                // The client stream will be closed with the sslStream 
                // because we specified this behavior when creating 
                // the sslStream.
                client.Close();
                sslStream.Close();                
                Console.WriteLine("connection closed");
            }
        //qua termina il thread che si occupa di gestire la connessione con il client
        }


        /*
         * l'utente mi manda solo l'username. Io genero un salt associato a quella sessione e lo rimando
         * indietro con un pacchetto di loginInfo("", sale pass, sale sessione)
         * poi l'utente mi manda il login con il token..vedo che corrisponde e me lo salvo nel db
         */
        
        private void Login(SslStream sslStream, string username)
        {
            try
            {
                string sessionSalt = SecurityUtils.Gen32Salt();
                if (!DatabaseUtils.ExistUserName(username))
                {
                    sslStream.Write(MessageUtils.StringToByte(MessageUtils.EncodeErrorMessage()));
                }
                else
                {
                    sslStream.Write(MessageUtils.StringToByte(MessageUtils.EncodeAckMessage()));
                    string salt = DatabaseUtils.getUserSalt(username);
                    string passSalted=DatabaseUtils.getPassword(username);
                    sslStream.Write(MessageUtils.StringToByte(MessageUtils.EncodeLoginMessage("", salt, sessionSalt)));
                    string m = ReadMessage(sslStream);
                    PresentationInfo pi = MessageUtils.decodePresentationInfo(m);
                    if (pi == null) {
                        sslStream.Write(MessageUtils.StringToByte(MessageUtils.EncodeErrorMessage()));
                        return;
                    }          
                    //verifico che il token che mi manda il client sia uguale a quello che ho calcolato
                    Console.WriteLine(pi.Psw);
                    string token = SecurityUtils.GetMd5Hash(MD5.Create(), passSalted + sessionSalt);
                    Console.WriteLine("mytoken: "+token);
                    if (token.CompareTo(pi.Psw) == 0)
                    {
                        DatabaseUtils.UpdateUserToken(username, token);
                        sslStream.Write(MessageUtils.StringToByte(MessageUtils.EncodeAckMessage()));
                    }
                    else {
                        sslStream.Write(MessageUtils.StringToByte(MessageUtils.EncodeErrorMessage()));
                    }
                }
            }
            catch (IOException e)
            {
                Console.WriteLine(e.Message + " Error: read timeout");
            }
            catch (EntryPointNotFoundException ) {
                sslStream.Write(MessageUtils.StringToByte(MessageUtils.EncodeErrorMessage()));                
            }
        }

        /*
         *la password è in chiaro. Genero un sale associato a quell'utente. Calcolo md5(password+saleutente)
         *e lo memorizzo nel db in cui inserisco anche il saleutente.
         */
        private void SignUp(SslStream sslStream,string usr, string psw)
        {
            try{

                string salt = SecurityUtils.Gen32Salt();
                string pswSalted = SecurityUtils.GetMd5Hash(MD5.Create(), psw + salt);
                DatabaseUtils.InsertUser(usr,pswSalted,salt);
                Directory.CreateDirectory(FileStoragePath + "\\" + usr);
                sslStream.Write(MessageUtils.StringToByte(MessageUtils.EncodeAckMessage()));
            }
            catch(Exception e){
            sslStream.Write(MessageUtils.StringToByte(MessageUtils.EncodeErrorMessage()));
            }
               
        }

       private void dispatchCommands(commands com, SslStream stream,PresentationInfo pi){

           commands c =com;
           switch (c){
               case commands.LISTFILE:
                   //this funcion is deprecated!
                   listfile(pi.Name,stream);
                    
                   break;
               case commands.DELETEFILE:
                   if (DeleteFile(pi.Name,stream) < 0)
                   {
                       Console.WriteLine("DEBUG: error while delete file! abort...");
                       return;
                   }
                   break;
               case commands.PUSHFILE:
                   if (PushFile(stream,pi.Name) < 0) {
                       Console.WriteLine("DEBUG: error while receving file! abort...");
                       return;
                   }

                   break;
               case commands.UPDATEFILE:
                   if (UpdateFile(pi.Name,stream) < 0)
                   {
                       Console.WriteLine("DEBUG: error while update file! abort...");
                       return;
                   }
                   break;
               case commands.GETFILE:
                   if (GetFile(stream,pi.Name) < 0)
                   {
                       Console.WriteLine("DEBUG: error while upload file! abort...");
                       return;
                   }
                   break;
               case commands.GETALL:
                   if (GetAllFiles(pi.Name,stream) < 0)
                   {
                       Console.WriteLine("DEBUG: error while get all files! abort...");
                       return;
                   }
                   break;
               case commands.HISTORY:
                   if (listAllVersions(pi.Name,stream) < 0)
                   {
                       Console.WriteLine("DEBUG: error while list all versions! abort...");
                       return;
                   }
                   break;
               case commands.SPACE:
                   if (Space(stream,pi) < 0)
                   {
                       Console.WriteLine("DEBUG: error while send space! abort...");
                       return;
                   }
                   break;
               case commands.RENAMED:
                   if (Renamed(stream, pi) < 0)
                   {
                       Console.WriteLine("DEBUG: error while rename! abort...");
                       return;
                   }
                   break;
               case commands.LOGOUT:
                   if (Logout(stream, pi) < 0)
                   {
                       Console.WriteLine("DEBUG: error while Logout! abort...");
                       return;
                   }
                   break;
               case commands.LISTINFOVERSIONS:
                   if (ListInfoVersion(stream, pi) < 0)
                   {
                       Console.WriteLine("DEBUG: error while list info versions! abort...");
                       return;
                   }
                   break;
               case commands.LISTVERSION:
                   if (ListVersion(stream, pi) < 0)
                   {
                       Console.WriteLine("DEBUG: error while list version! abort...");
                       return;
                   }
                   break;
               case commands.FINALDELETE:
                   if (FinalDelete(stream, pi) < 0)
                   {
                       Console.WriteLine("DEBUG: error while final delete! abort...");
                       return;
                   }
                   break;
               case commands.CHANGED:
                   if (Changed(stream, pi) < 0)
                   {
                       Console.WriteLine("DEBUG: error while changed! abort...");
                       return;
                   }
                   break;
               case commands.LOGGED:
                   if (AuthenticateClient(pi.Name,pi.Psw))
                   {
                       stream.Write(MessageUtils.StringToByte(MessageUtils.EncodeAckMessage()));
                       return;
                   }
                   else
                   {
                       stream.Write(MessageUtils.StringToByte(MessageUtils.EncodeErrorMessage()));
                       return;
                   }
               default:
                   stream.Write(MessageUtils.StringToByte(MessageUtils.EncodeErrorMessage()));
                   break;
           }
       
       }

       private int Changed(SslStream stream, PresentationInfo pi)
       {
           try
           {
               Interlocked.Exchange(ref Constants.IsChanged, 1);
           }
           catch(Exception e)
           {
               return -1;
           }
           return 1;
       }

       private int FinalDelete(SslStream stream, PresentationInfo pi)
       {
           try
           {
               stream.Write(MessageUtils.StringToByte(MessageUtils.EncodeAckMessage()));
               string m = ReadMessage(stream);
               ClientFileInfo cli = MessageUtils.DecodeFileInfoMessage(m);
               //se ho ricevuto i dati in un formato errato mando un messaggio di errore e termino il thread!
               if (cli == null)
               {
                   stream.Write(MessageUtils.StringToByte(MessageUtils.EncodeErrorMessage()));
                   return -1;
               }
               if (File.Exists(FileStoragePath + pi.Name + "\\" + cli.Hash))
               {
                   File.Delete(FileStoragePath + pi.Name + "\\" + cli.Hash);
                   DatabaseUtils.FinalDeleteEntry(pi.Name, cli.Hash);
                   stream.Write(MessageUtils.StringToByte(MessageUtils.EncodeAckMessage()));
                   return 1;
               }
               else
               {
                   stream.Write(MessageUtils.StringToByte(MessageUtils.EncodeErrorMessage()));
                   return -1;
               }
           }
           catch(IOException)
           {
               return -1;
           }
           catch (Exception)
           {
               stream.Write(MessageUtils.StringToByte(MessageUtils.EncodeErrorMessage()));
               return -1;

           }
       }

       private int ListVersion(SslStream stream, PresentationInfo pi)
       {
           try
           {
               stream.Write(MessageUtils.StringToByte(MessageUtils.EncodeAckMessage()));

               string m = ReadMessage(stream);
               ClientFileInfo cli = MessageUtils.DecodeFileInfoMessage(m);
               //se ho ricevuto i dati in un formato errato mando un messaggio di errore e termino il thread!
               if (cli == null)
               {
                   stream.Write(MessageUtils.StringToByte(MessageUtils.EncodeErrorMessage()));
                   return -1;
               }
               Console.WriteLine(cli.Path);
               List<ClientFileInfo> fInfo = VersionsManager.LoadVersion(cli.Path);
               stream.Write(MessageUtils.StringToByte(MessageUtils.EncodeAckMessage()));

               foreach (ClientFileInfo f in fInfo)
               {
                   //invio ack
                   stream.Write(MessageUtils.StringToByte(MessageUtils.EncodeAckMessage()));
                   //invio f
                   stream.Write(MessageUtils.StringToByte(
                       // MessageUtils.EncodeFileInfoMessage(f.Name, f.Path, f.Size, f.LastAcces, f.LastUpd, f.Hash)));
                   MessageUtils.EncodeFileInfoMessage(f)));
               }
               stream.Write(MessageUtils.StringToByte(MessageUtils.EncodeFinishMessage()));
               return 1;
           }
           catch (IOException e)
           {
               Console.WriteLine("Timeout exception: " + e.Message);
               return -1;
           }
           catch (Exception e)
           {
               Console.WriteLine("An error occurs during listFile: " + e.Message);
               if (e.Message.CompareTo("No rows!!") == 0)
               {
                   stream.Write(MessageUtils.StringToByte(MessageUtils.EncodeFinishMessage()));
                   return 1;
               }
               else
               {
                   stream.Write(MessageUtils.StringToByte(MessageUtils.EncodeErrorMessage()));
               }
               return -1;
           }       
       }

       private int ListInfoVersion(SslStream stream, PresentationInfo pi)
       {
           try
           {
               stream.Write(MessageUtils.StringToByte(MessageUtils.EncodeAckMessage()));
               List<ClientFileInfo> fInfo = VersionsManager.getInfoVersions(pi.Name);

               foreach (ClientFileInfo f in fInfo)
               {
                   //invio ack
                   stream.Write(MessageUtils.StringToByte(MessageUtils.EncodeAckMessage()));
                   //invio f
                   stream.Write(MessageUtils.StringToByte(
                       // MessageUtils.EncodeFileInfoMessage(f.Name, f.Path, f.Size, f.LastAcces, f.LastUpd, f.Hash)));
                   MessageUtils.EncodeFileInfoMessage(f)));
               }
               stream.Write(MessageUtils.StringToByte(MessageUtils.EncodeFinishMessage()));
               return 1;
           }
           catch (IOException e)
           {
               Console.WriteLine("Timeout exception: " + e.Message);
               return -1;
           }
           catch (Exception e)
           {
               Console.WriteLine("An error occurs during listFile: " + e.Message);
               if (e.Message.CompareTo("No rows!!") == 0)
               {
                   stream.Write(MessageUtils.StringToByte(MessageUtils.EncodeFinishMessage()));
                   return 1;
               }
               else
               {
                   stream.Write(MessageUtils.StringToByte(MessageUtils.EncodeErrorMessage()));
               }
               return -1;
           }
       }

       private int Logout(SslStream stream, PresentationInfo pi)
       {
           try
           {
               DatabaseUtils.Logout(pi.Name);
               stream.Write(MessageUtils.StringToByte(MessageUtils.EncodeAckMessage()));
           }
           catch(IOException e ){
               Console.WriteLine("Timeout Exedeed! " + e);
               return -1;
           }
           catch (Exception e)
           {
               stream.Write(MessageUtils.StringToByte(MessageUtils.EncodeErrorMessage()));
               Console.WriteLine("Db error: " + e);
               return - 1;
           }
           return 1; 

       }

       private int Renamed(SslStream stream, PresentationInfo pi)
       {
           string m;
           string newHash=null;
           int n = -1;
           try
           {
               stream.Write(MessageUtils.StringToByte(MessageUtils.EncodeAckMessage()));

               m = ReadMessage(stream);

               ClientFileInfo f = MessageUtils.DecodeFileInfoMessage(m);
               //se ho ricevuto i dati in un formato errato mando un messaggio di errore e termino il thread!
               if (f == null)
               {
                   stream.Write(MessageUtils.StringToByte(MessageUtils.EncodeErrorMessage()));
                   return -1;
               }
               newHash = f.Hash;
               string oldHash = DatabaseUtils.GetHashFromPath(pi.Name,f.OldPath);
               if (DatabaseUtils.ExistFileIntoHistory(pi.Name,newHash))
               {
                   stream.Write(MessageUtils.StringToByte(MessageUtils.EncodeAckMessage()));
                   return 1;
               }
               Constants.inTransfer.Enqueue(newHash);
               File.Copy(FileStoragePath + "\\" + pi.Name + "\\" + oldHash, FileStoragePath + "\\" + pi.Name + "\\" + f.Hash);
               while(Constants.inTransfer.TryDequeue(out newHash));
               if (!DatabaseUtils.RenameFile(pi.Name, f))
               {
                   stream.Write(MessageUtils.StringToByte(MessageUtils.EncodeErrorMessage()));
                   File.Delete(FileStoragePath + "\\" + pi.Name + "\\" + f.Hash);
                   return -1;
               }
               VersionsManager.SaveCurrentVersion(DatabaseUtils.GetCurrentConf(pi.Name), pi.Name);              
               stream.Write(MessageUtils.StringToByte(MessageUtils.EncodeAckMessage()));
               n = 1;
           }
           catch (IOException)
           {
               Console.WriteLine("IOExeeded into renamed");
               n = -1;
           }
           catch (Exception e)
           {
               Console.WriteLine("In renamed exc: "+e.Message);
               stream.Write(MessageUtils.StringToByte(MessageUtils.EncodeErrorMessage()));
               n = -1;
           }
           finally
           {
               if (n < 0)
               {
                   try
                   {
                       File.Delete(FileStoragePath + "\\" + pi.Name + "\\" + newHash);
                   }
                   catch (Exception e) { Console.WriteLine("In renamed exc "+e.Message); }
               }
           }
           return n;
       }

       private int TestRenamed(string username,ClientFileInfo f)
       {
           string newHash = null;
           int n = -1;
           try
           {
               newHash = f.Hash;
               string oldHash = DatabaseUtils.GetHashFromPath(username, f.OldPath);
               File.Copy(FileStoragePath + "\\" + username + "\\" + oldHash, FileStoragePath + "\\" + username+ "\\" + f.Hash);
               if (!DatabaseUtils.RenameFile(username, f))
               {
                   File.Delete(FileStoragePath + "\\" +username + "\\" + f.Hash);
                   return -1;
               }

               n = 1;
           }
           catch (IOException)
           {
               Console.WriteLine("IOExeeded into renamed");
               n = -1;
           }
           catch (Exception)
           {
               n = -1;
           }
           finally
           {
               if (n < 0)
               {
                   try
                   {
                       File.Delete(FileStoragePath + "\\" + username + "\\" + newHash);
                   }
                   catch (Exception) { }
               }
           }
           return n;
       }

       private int Space(SslStream stream, PresentationInfo p)
       {
           SpaceInfo si = new SpaceInfo { 
                TotalSpace = -1,
                SpaceUsed = -1
           };
           try
           {
               stream.Write(MessageUtils.StringToByte(MessageUtils.EncodeAckMessage()));
               si = DatabaseUtils.GetSpaces(p.Name);
               stream.Write(MessageUtils.StringToByte(MessageUtils.EncodeSpaceInfoMessage(si.SpaceUsed,si.TotalSpace)));
               return 1;
           }
           catch (IOException)
           {
               stream.Write(MessageUtils.StringToByte(MessageUtils.EncodeSpaceInfoMessage(si.SpaceUsed, si.TotalSpace)));
               return -1;
           }
           catch (Exception) {
               stream.Write(MessageUtils.StringToByte(MessageUtils.EncodeSpaceInfoMessage(si.SpaceUsed, si.TotalSpace)));
               return -1;
           }

       }

       private void Hello(SslStream stream)
       {
           try
           {
               while (Interlocked.Equals(Constants.IsStart,1))
               {
                   stream.Write(MessageUtils.StringToByte(MessageUtils.EncodeAckMessage()));
                   Console.WriteLine("Hello");
                   Thread.Sleep(1000);
                   string m = ReadMessage(stream);
               }
               Console.WriteLine("Hello stop! Server in Stop");
            }
           catch(IOException){
               Console.WriteLine("Client offline. Stop hello messages.");
               return;
           }
       }
        
        //pe ogni entry nella tab currentconf invio prima il pacchetto con le info e poi il file
        //serve in caso voglia ripristinare l'intero contenuto 
       private int GetAllFiles(string username,SslStream stream)
       {
           /**
            * for i=0;  i<numEntry; i++
            *       invia ack
            *       crea oggetto FileInfo
            *       invia oggetto FileInfo
            *       Attendi Ack
            *       invia il file
            *       attenti Ack
            * end for
            * invia Finish
            * 
            */
           List<ClientFileInfo> fInfo = DatabaseUtils.GetCurrentConf(username);

           foreach (ClientFileInfo f in fInfo){
               Console.WriteLine(f.Hash + " " + f.Name + " " + f.Path + " " + f.Size);
            //invio ack
               stream.Write(MessageUtils.StringToByte(MessageUtils.EncodeAckMessage()));
               //invio f
               FileInfo fi = new FileInfo(FileStoragePath + "\\" + username + "\\" + f.Hash);
               stream.Write(MessageUtils.StringToByte(
              MessageUtils.EncodeFileInfoMessage(f.Name, f.Path, f.Size, fi.LastAccessTime,fi.LastWriteTime, f.Hash)));
               //attendo ack
               string m;
               try
               {
                   m = ReadMessage(stream);
               }
               catch (IOException)
               {
                   Console.WriteLine("ReadTimeout exedeed! Connection will be closed...");
                   return -1;
               }
               commands c = MessageUtils.DecodeResponseMessage(m);
               if (c == commands.ERROR || c == commands.UNKNOWN)
               {
                   //il client non ha ricevuto il file correttamente!
                   //da gestire...che si fa?
                   return -1;
               }
               //invio il file
               SendFile(stream, f.Hash, (int) f.Size,username);
               //attendo ack
               try
               {
                   m = ReadMessage(stream);
               }
               catch (IOException)
               {
                   Console.WriteLine("ReadTimeout exedeed! Connection will be closed...");
                  return -1;
               }
               c = MessageUtils.DecodeResponseMessage(m);
               if (c == commands.ERROR || c == commands.UNKNOWN)
               {
                   //il client non ha ricevuto il file correttamente!
                   //da gestire...che si fa?
                   return -1;
               }
           }
           //invio finish
           stream.Write(MessageUtils.StringToByte(MessageUtils.EncodeFinishMessage()));
           return 1;
       }

        
        //invia un file al client perchè magari lo ha cancellato per sbaglio e lo vuole ripristinare
        //il client mi deve mandare le info sul file che vuole ricevere.
       private int GetFile(SslStream stream,string username)
       {
           //write ack!
           stream.Write(MessageUtils.StringToByte(MessageUtils.EncodeAckMessage()));
           //ricevo le info del file
           string m ;
           try
           {
               m = ReadMessage(stream);

               ClientFileInfo f = MessageUtils.DecodeFileInfoMessage(m);
               //se ho ricevuto i dati in un formato errato mando un messaggio di errore e termino il thread!
               if (f == null)
               {
                   stream.Write(MessageUtils.StringToByte(MessageUtils.EncodeErrorMessage()));
                   return -1;
               }
               long size = f.Size;
               string clientPath = f.Path;
               string fileName = f.Name;
               DateTime lastU = f.LastUpd;
               DateTime lastA = f.LastAcces;
               string hash = f.Hash;
               int s;
               //faccio questo controllo in try catch per essere sicuro che il file esista davvero
               try
               {
                   FileInfo fInfo = new FileInfo(FileStoragePath + "\\" + username + "\\" + hash);
                   s = (int)fInfo.Length;
               }
               catch (Exception e)
               {
                   Console.WriteLine(e.Message);
                   stream.Write(MessageUtils.StringToByte(MessageUtils.EncodeErrorMessage()));
                   return -1;
               }
               //a questo punto sono sicuro di avere il file con quell'hash e gli mando l'ack
               stream.Write(MessageUtils.StringToByte(MessageUtils.EncodeAckMessage()));
               //a me serve fondamentalmente solo l'hash e la size
               Console.WriteLine(hash + " " + fileName + " " + size);
               SendFile(stream, hash, s,username);
           }
           catch (IOException)
           {
               Console.WriteLine("Read timeout exeeded. Connection will be closed...");
               return -1;
           }
           return 1;
       }

       private int SendFile(SslStream stream, string hash, int s,string username)
       {
           int curr = 0;
           int res = 1;
           FileStream file =null;
           try
           {
               file = File.OpenRead(FileStoragePath + "\\" + username + "\\" + hash);
               while (curr < s && res > 0)
               {
                   stream.Flush();
                   byte[] buffer = new byte[4096];
                   res = file.Read(buffer, 0, buffer.Length);
                   stream.Write(buffer, 0, res);
                   curr += res;
               }
           }
           catch (Exception e)
           {
               Console.WriteLine(e.Message);
               if (file != null)
               { file.Close(); }
               return -1;     
           }
           if (file != null)
           { file.Close(); }
               return 1;
           
       }

        //il client mi notifica che un certo file non è più presente quindi devo eliminarlo dalla
        //configurazione corrente dell'utente. Non devo toccare il file fisico e neanche il db dello
        //storico (tabella utente) in quanto può richiedermi di ripristinarlo.
       private int DeleteFile(string username,SslStream stream)
       {
           //write ack!
           stream.Write(MessageUtils.StringToByte(MessageUtils.EncodeAckMessage()));
           //ricevo le info del file
           string m;
           try
           {
               m = ReadMessage(stream);
           }
           catch (IOException)
           {
               Console.WriteLine("ReadTimeout exedeed! Connection will be closed...");
               return -1;
           }
           ClientFileInfo f = MessageUtils.DecodeFileInfoMessage(m);
           //se ho ricevuto i dati in un formato errato mando un messaggio di errore e termino il thread!
           if (f == null)
           {
               stream.Write(MessageUtils.StringToByte(MessageUtils.EncodeErrorMessage()));
               return -1;
           }
           long size = f.Size;
           string clientPath = f.Path;
           string fileName = f.Name;
           DateTime lastU = f.LastUpd;
           DateTime lastA = f.LastAcces;
           string hash = f.Hash;
           try
           {
               DatabaseUtils.DeleteFromCurrentConf(username,f);
               VersionsManager.SaveCurrentVersion(DatabaseUtils.GetCurrentConf(username), username);
           }
           catch(Exception e)
           {
               Console.WriteLine(e.Message);
               stream.Write(MessageUtils.StringToByte(MessageUtils.EncodeErrorMessage()));
               return -1;
           }
           stream.Write(MessageUtils.StringToByte(MessageUtils.EncodeAckMessage()));
           return 1;
       }

        //praticamente come pushfile solo che le operazioni da fare nel db sono leggermente diverse
        //per il file fisico tengo sia la versione precedente sia quella che mi arriva adesso dal client
        //tanto il nome con cui lo memorizzo corrisponde con il suo hash
       private int UpdateFile(string username,SslStream stream)
       {
           //write ack!
           stream.Write(MessageUtils.StringToByte(MessageUtils.EncodeAckMessage()));
           //ricevo le info del file
           string m;
           
           try{

               m = ReadMessage(stream);
           }
           catch(IOException){
            Console.WriteLine("Read Timeout exedeed! Connection will be closed...");
               return -1;
           }
               
               ClientFileInfo f = MessageUtils.DecodeFileInfoMessage(m);
               //se ho ricevuto i dati in un formato errato mando un messaggio di errore e termino il thread!
               if (f == null)
               {
                   stream.Write(MessageUtils.StringToByte(MessageUtils.EncodeErrorMessage()));
                   return -1;
               }
               long size = f.Size;
               string clientPath = f.Path;
               string fileName = f.Name;
               DateTime lastU = f.LastUpd;
               DateTime lastA = f.LastAcces;
               string hash = f.Hash;
               
               //vedo se l'hash del file è già presente nel db
               bool existFileFisico=false;
               bool existFileStorico = false;
               //bool falseUpdate=false;
                try
               {
                   existFileFisico = File.Exists(FileStoragePath + username + "\\" + hash);
                    existFileStorico = DatabaseUtils.ExistFileIntoHistory(username, f.Hash);
                   //falseUpdate = DatabaseUtils.IsFalseUpdate(username,f);
               }
               catch (Exception e) {
                   Console.WriteLine("DB-Error: " + e.Message);
                   stream.Write(MessageUtils.StringToByte(MessageUtils.EncodeErrorMessage()));
                   return -1;
               }

           ///////per evitare il doppio update dovuto al watcher
           //devo fare il controllo sulla current conf! Perchè lui vuole pusharmi un file
           //che ho nello storico ma deve esserci anche nella currentconf
           if (existFileStorico)
           {
               try
               {
                   if(!DatabaseUtils.ExistFile(username, hash))
                   {
                       DatabaseUtils.InsertNewFileIntoCurrentConf(username,f);
                   }
               }
               catch(Exception e)
               {
                   stream.Write(MessageUtils.StringToByte(MessageUtils.EncodeErrorMessage()));
                   return -1;
               }
               stream.Write(MessageUtils.StringToByte(MessageUtils.EncodeFinishMessage()));
               return 1;
           }
         //////////////////////////////////////////////////////////
           if (existFileFisico)
           {
               stream.Write(MessageUtils.StringToByte(MessageUtils.EncodeFinishMessage()));
               return 1;
           }

           try
           {
               if (DatabaseUtils.CanInsert(username, size, clientPath) < 0)
               {
                   stream.Write(MessageUtils.StringToByte(MessageUtils.EncodeErrorMessage()));
                   return -1;
               }
            }
           catch (Exception e)
           {
               Console.WriteLine(e.Message);
               stream.Write(MessageUtils.StringToByte(MessageUtils.EncodeErrorMessage()));
               return -1;
           }

           /*
            * PER ADESSO IGNORO IL FALSEUPDATE
           if (falseUpdate)
           {
               //copiare il file con il nuovo hash
               //aggiornare il db: bisogna inserire una riga nello storico e
               //aggiornare quella del db
               try
               {
                   string oldHash = DatabaseUtils.GetHashFromPath(username,f.Path);
                   File.Copy(FileStoragePath + "\\" + username + "\\" + oldHash, FileStoragePath + "\\" + username + "\\" + f.Hash);
                   DatabaseUtils.UpdateFile(username, f);
                   Console.WriteLine("falseupd");
                   stream.Write(MessageUtils.StringToByte(MessageUtils.EncodeFinishMessage()));
                   return 1;
               }
               catch (Exception)
               {
                   if (File.Exists(FileStoragePath + "\\" + username + "\\" + f.Hash))
                       File.Delete(FileStoragePath + "\\" + username + "\\" + f.Hash);
                   stream.Write(MessageUtils.StringToByte(MessageUtils.EncodeErrorMessage()));
                   return -1;
               }

           }
           */
               stream.Write(MessageUtils.StringToByte(MessageUtils.EncodeAckMessage()));
               int r = -1;
               if (!f.IsFolder /*&& !falseUpdate*/)
               {
                   r = receiveFile(stream, fileName, size, hash,username);
                   if (r < 0)
                   {
                       stream.Write(MessageUtils.StringToByte(MessageUtils.EncodeErrorMessage()));
                       return -1;
                   }
               }
               /**
                * Inserisco nella tabella utente il file
                * Elimino dalla tabella il vecchio hash e aggiungo il nuovo hash
                * **/
           try
           {
               DatabaseUtils.UpdateFile(username,f);
               VersionsManager.SaveCurrentVersion(DatabaseUtils.GetCurrentConf(username), username);
           }
           catch (Exception e)
           {
               Console.WriteLine("DB-Error: " + e.Message);
               stream.Write(MessageUtils.StringToByte(MessageUtils.EncodeErrorMessage()));
               return -1;
           }     
               stream.Write(MessageUtils.StringToByte(MessageUtils.EncodeAckMessage()));
               return 1;
       }

        //Ricevo un file dal client e poi aggiorno il db
       private int PushFile(SslStream stream, string username) {
           //write ack!
           stream.Write(MessageUtils.StringToByte(MessageUtils.EncodeAckMessage()));
           //ricevo le info del file
           string m;
           try
           {
               m = ReadMessage(stream);
           }
           catch (IOException)
           {
               Console.WriteLine("ReadTimeout exedeed! Connection will be closed...");
               return -1;
           }
           Console.WriteLine(m);
           ClientFileInfo f = MessageUtils.DecodeFileInfoMessage(m);
           Console.WriteLine(f);
           //se ho ricevuto i dati in un formato errato mando un messaggio di errore e termino il thread!
           if (f == null)
           {
               stream.Write(MessageUtils.StringToByte(MessageUtils.EncodeErrorMessage()));
               return -1;
           }
           long size = f.Size;
           string clientPath = f.Path;
           string fileName = f.Name;
           DateTime lastU = f.LastUpd;
           DateTime lastA = f.LastAcces;
           string hash = f.Hash;
           bool existFile = false;

           ///////////test per il doppio update causato dal watcher///////////
           try
           {
               existFile = File.Exists(FileStoragePath + "\\" + username + "\\" + hash);

           }
           catch (Exception e)
           {
               Console.WriteLine("DB-Error: " + e.Message);
               stream.Write(MessageUtils.StringToByte(MessageUtils.EncodeErrorMessage()));
               return -1;
           }
           if (existFile)
           {
               stream.Write(MessageUtils.StringToByte(MessageUtils.EncodeFinishMessage()));
               return 1;
           }
           /////////////////////////////////////////////////////////////


           //verifico se ho superato il max numero di versioni e/o se la memoria è sufficiente
           try
           {
               if (DatabaseUtils.CanInsert(username, size, clientPath)<0)
               {
                   stream.Write(MessageUtils.StringToByte(MessageUtils.EncodeErrorMessage()));
                   return -1;
               }
               else
               {
                   stream.Write(MessageUtils.StringToByte(MessageUtils.EncodeAckMessage()));
               }
           }
           catch (Exception e)
           {
               Console.WriteLine(e.Message);
               stream.Write(MessageUtils.StringToByte(MessageUtils.EncodeErrorMessage()));
               return -1;
           }

           if (!f.IsFolder)
           {
               //se è tutto ok invia un ack altrimenti invia un error
               if (receiveFile(stream, fileName, size, hash,username) < 0)
               {
                   stream.Write(MessageUtils.StringToByte(MessageUtils.EncodeErrorMessage()));
                   return -1;
               }
           }

          
           //stream.Write(MessageUtils.StringToByte(MessageUtils.EncodeAckMessage()));
           //la connessione può terminare qua!
           //Se il file è stato ricevuto correttamente e non ci sono stati errori
           //(se sono arrivato qua non ci sono stati errori di ricezione)
           //il server deve aggiornare la base dati
           /**
            * Inserisco nella tabella utente il file
            * Inserisco nella tabella CurrentConf il file
            * **/
           try
           {
               DatabaseUtils.InsertNewFile(username,f);
               VersionsManager.SaveCurrentVersion(DatabaseUtils.GetCurrentConf(username), username);
           }
           catch (Exception e)
           {
               Console.WriteLine(e.Message);
               stream.Write(MessageUtils.StringToByte(MessageUtils.EncodeErrorMessage()));
               return -1;
           }
           stream.Write(MessageUtils.StringToByte(MessageUtils.EncodeAckMessage()));
           return 1;
       }


       //leggo un file dallo stream di dimensione "size"
       private int receiveFile(SslStream stream,string fileName,long size, string hash,string username) {
           int cursize = 0;
           try
           {
               Console.WriteLine(FileStoragePath + "\\" + username + "\\" + hash);
               Constants.inTransfer.Enqueue(hash);
               FileStream fs = File.OpenWrite(FileStoragePath + "\\" + username + "\\" + hash);
               int block = -1;
               if (size == 0)
               {
                   fs.Close();
                   return 1;
               }
               do
               {
                   byte[] buffer = new byte[4096];
                   try
                   {
                       block = stream.Read(buffer, 0, buffer.Length);
                       if (cursize % (500 * 1024) == 0)
                       {
                           Console.WriteLine("File: " + fileName + " al: " + (int) ((float)cursize / (float)size * 100) + "%");
                       }
                   }
                   catch (IOException e)
                   {
                       Console.WriteLine(e);
                       fs.Close();
                       File.Delete(FileStoragePath + "\\" + username + "\\" + hash);
                       Console.WriteLine("File non ricevuto: " + fileName);
                       
                       while(Constants.inTransfer.TryDequeue(out hash));
                       return -1;
                   }
                   fs.Write(buffer, 0, block);
                   cursize += block;
               } while (cursize < size && block > 0);

               fs.Close();
               if (cursize < size)
               {
                   File.Delete(FileStoragePath + "\\" + username + "\\" + hash);
                   Console.WriteLine("File non ricevuto: " + fileName);
                   while (Constants.inTransfer.TryDequeue(out hash)) ;
                   return -1;
               }
           }
           catch (Exception)
           {
               try
               {
                   File.Delete(FileStoragePath + "\\" + username + "\\" + hash);
               }
               catch(Exception) { }
               Console.WriteLine("File non ricevuto: " + fileName);
               while (Constants.inTransfer.TryDequeue(out hash)) ;
               return -1;
           }
           Console.WriteLine("File: " + fileName + " ricevuto correttamente");
           while (Constants.inTransfer.TryDequeue(out hash)) ;
           return 1;
       }

       private int listAllVersions(string username,SslStream stream) {
           try {

               stream.Write(MessageUtils.StringToByte(MessageUtils.EncodeAckMessage()));
               List<FileVersions> versions = DatabaseUtils.getAllVersions(username);

               foreach (FileVersions fv in versions)
               {
                   Console.WriteLine(fv.Hash + " " + fv.Name + " " + fv.Path + " " + fv.Size + " last upd: " + fv.LastUpd + " last acc:" + fv.LastAcces);
                   //invio ack
                   stream.Write(MessageUtils.StringToByte(MessageUtils.EncodeAckMessage()));
                   //invio f
                   stream.Write(MessageUtils.StringToByte(
                  MessageUtils.EncodeFileVersionsMessage(fv)));
               }
               stream.Write(MessageUtils.StringToByte(MessageUtils.EncodeFinishMessage()));
               return 1;
           }
           catch (IOException e)
           {
               Console.WriteLine("Timeout exception: " + e.Message);
               return -1;
           }
           catch (Exception e)
           {
               Console.WriteLine("An error occurs during listFile: " + e.Message);
               if (e.Message.CompareTo("No rows!!") == 0)
               {
                   stream.Write(MessageUtils.StringToByte(MessageUtils.EncodeFinishMessage()));
               }
               else
               {
                   stream.Write(MessageUtils.StringToByte(MessageUtils.EncodeErrorMessage()));
               }
               return -1;
           }
       }
        
       //ping pong tra client e server: io mando le info di tutti i miei file.
        //ordine delle chiamate:ack; while(!finito){ send ack; send info;} send finito.
        private void listfile(string username,SslStream stream) {
            try
            {
                stream.Write(MessageUtils.StringToByte(MessageUtils.EncodeAckMessage()));
                List<ClientFileInfo> fInfo = DatabaseUtils.GetCurrentConf(username);

                foreach (ClientFileInfo f in fInfo)
                {
                    Console.WriteLine(f.Hash + " " + f.Name + " " + f.Path + " " + f.Size + " last upd: " + f.LastUpd + " last acc:" + f.LastAcces);
                    //invio ack
                    stream.Write(MessageUtils.StringToByte(MessageUtils.EncodeAckMessage()));
                    //invio f
                    stream.Write(MessageUtils.StringToByte(
                        // MessageUtils.EncodeFileInfoMessage(f.Name, f.Path, f.Size, f.LastAcces, f.LastUpd, f.Hash)));
                    MessageUtils.EncodeFileInfoMessage(f)));
                }
                stream.Write(MessageUtils.StringToByte(MessageUtils.EncodeFinishMessage()));
                /*if (Interlocked.Equals(Constants.IsChanged, 1))
                {
                    VersionsManager.SaveCurrentVersion(fInfo, username);
                    Interlocked.Exchange(ref Constants.IsChanged, 0);
                }*/
            }
            catch(IOException e){
                Console.WriteLine("Timeout exception: "+e.Message);
            }
            catch (Exception e) {
                Console.WriteLine("An error occurs during listFile: " + e.Message);
                if (e.Message.CompareTo("No rows!!") == 0)
                {
                    stream.Write(MessageUtils.StringToByte(MessageUtils.EncodeFinishMessage()));
                }
                else
                {
                    stream.Write(MessageUtils.StringToByte(MessageUtils.EncodeErrorMessage()));
                }
            }
          }


       private bool AuthenticateClient(string name, string psw ) {
           Console.WriteLine("In auth");
           try
           {
               if (!DatabaseUtils.IsAuth(name, psw)) {
                   Console.WriteLine("password errata");
                   return false;
               }
           }
           catch (Exception ) {
               Console.WriteLine("errore db autenticazione");
               return false;
           }
           
           return true;
        }

        //Legge dallo stream fino a <EOF>
        private string ReadMessage(SslStream sslStream)
        {
            // Read the  message sent by the client. 
            // The client signals the end of the message using the 
            // "<EOF>" marker.
            byte[] buffer = new byte[2048];
            StringBuilder messageData = new StringBuilder();
            try
            {
                int bytes = -1;
                do
                {
                    // Read the client's test message.
                    bytes = sslStream.Read(buffer, 0, buffer.Length);

                    // Use Decoder class to convert from bytes to UTF8 
                    // in case a character spans two buffers.
                    Decoder decoder = Encoding.UTF8.GetDecoder();
                    char[] chars = new char[decoder.GetCharCount(buffer, 0, bytes)];
                    decoder.GetChars(buffer, 0, bytes, chars, 0);
                    messageData.Append(chars);
                    // Check for EOF or an empty message. 
                    if (messageData.ToString().IndexOf("<EOF>") != -1)
                    {
                        break;
                    }
                } while (bytes != 0);
            }
            catch(IOException){
                throw new IOException();
            }
            return messageData.ToString();
        }
      
        [STAThread]
        public static int Main(string[] args)
        {
            certificate = null;
            server = new SslTcpServer();
            if (args == null || args.Length < 1)
            {
                DisplayUsage();
            }
            certificate = args[0];
             //test per il database
           try
           {
               Thread t = new Thread(() => VerifyCoerence());
               t.Start();


               Thread thread = new Thread(() =>
               {
                   Application app = new Application();
                   app.Run(new StartPage());                   
               });
              
               thread.SetApartmentState(ApartmentState.STA); //Set the thread to STA
               thread.Start();
                  serverThread = new Thread(() => { server.RunServer(certificate); });
                   serverThread.Start();
                   serverThread.Join();
             
                   // Thread.Sleep(5000);
              // Console.WriteLine("after start");
               //  StopServer();
               //  StartServer();
             /*  ClientFileInfo f = new ClientFileInfo();
               f.Path = "C:\\temp\\ckk.txt";
               f.OldPath= "C:\\temp\\ckk00.txt";
               f.Name = "ckk.txt";
               f.Hash = "nuovohash234";
               f.Size = 0;
               
               Console.WriteLine(server.TestRenamed("ciao",f));
               */

               //SpaceInfo si;
               //si = DatabaseUtils.GetSpaces("ciao");
               //Console.WriteLine(si.SpaceUsed + " " + si.TotalSpace);
               //DatabaseUtils.DeleteFromCurrentConf("hash");
               //DatabaseUtils.InsertNewFile("ciao","hash2-7", "name", "path", 1, DateTime.Now, DateTime.Now);
               //Console.WriteLine(DatabaseUtils.ExistFile("hash"));
               //Console.WriteLine(DatabaseUtils.CanInsert("ciao", 1, "C:\\temp\\file1.txt"));
               
               
               /*
               Directory.CreateDirectory(FileStoragePath + "\\ci");
               string n = "ci";
               FileStream fs = File.OpenWrite(FileStoragePath + "\\" + n + "\\"+ "f0a2f561beecb4c58923b819940f3abcbddb3425b999");
               string  s=SecurityUtils.Gen32Salt();
               fs.Write(MessageUtils.StringToByte(s), 0, s.Length);
               fs.Close();
               */
                 //DatabaseUtils.UpdateUserToken("corrado2207", "token2");

              // Console.WriteLine(DatabaseUtils.getUserSalt("corrado2207"));
               
             //  Console.WriteLine("salt: "+DatabaseUtils.getUserSalt("corrado2207"));
              // DateTime d = DateTime.Now;
              // Console.WriteLine(d);
               
               //DatabaseUtils.InsertNewFile("ciao","h2", "nome",FileStoragePath+"nome", 23,d, d);

               //DatabaseUtils.CanInsert("ciao", 12, FileStoragePath + "nome");
                // DatabaseUtils.UpdateFile("ciao","h5", "nome", FileStoragePath+"nome",0,d,d);
              /*  List<ClientFileInfo> fInfo = DatabaseUtils.GetCurrentConf();

                foreach (ClientFileInfo f in fInfo)
                {
                    Console.WriteLine(f.Hash + " " + f.Name + " " + f.Path + " " + f.Size+" "+f.LastAcces+" " +f.LastUpd );

                 }*/
                //thread.Join();
                   Console.WriteLine("CloudBox-Server Terminato. Premi un tasto per uscire");
                 Console.Read();

            }
            catch(Exception ex)
            {
                Console.WriteLine("Into main" + ex.Message);
                Console.Read();
            }
           
            return 0;
        }

        private static void VerifyCoerence()
        {
            //per ogni file fisico vedo se ho una entry nel db
            while (true) 
            {
                
                try
                {   
                        List<string> users = DatabaseUtils.GetAllUsers();

                        foreach (string u in users)
                        {
                            Console.WriteLine("Check per user " + u);
                            Dictionary<string, bool> sync = new Dictionary<string, bool>();
                            string[] allFiles;
                            try
                            {
                                allFiles = Directory.GetFiles(FileStoragePath + "\\" + u);

                            }
                            catch (Exception) { continue; }
                            foreach (string s in allFiles)
                            {
                                if(Constants.inTransfer.Contains<string>(s)) continue;
                                string s1 = s.Replace(FileStoragePath + "\\" + u+"\\", "");
                                sync.Add(s1, false);
                            }

                            List<FileVersions> versions = new List<FileVersions>();
                            try
                            {
                                versions = DatabaseUtils.getAllVersions(u);
                            }
                            catch (Exception) {  }

                            foreach (FileVersions fv in versions)
                            {
                                foreach (ClientFileInfo c in fv.versions)
                                {
                                    if (!sync.ContainsKey(c.Hash))
                                    {
                                        Console.WriteLine("Trovata entry senza file fisico");
                                        DatabaseUtils.DeleteEntry(u, c.Hash);
                                    }
                                    else
                                    {
                                        sync[c.Hash] = true;
                                    }
                                }  
                            }
                            foreach (string s in sync.Keys)
                            {
                                if (sync[s] == false)
                                {
                                    File.Delete(FileStoragePath + "\\" + u + "\\" + s);
                                    Console.WriteLine("Trovato file senza entry...Elimino file");
                                }
                            }
                        }        
                }
                catch (Exception e)
                {
                    Console.WriteLine("Error while verify coerence: " + e.Message);
                }
                Thread.Sleep(5000);
            }
        }


        private static void DisplayUsage()
        {
            Console.WriteLine("To start the server specify:");
            Console.WriteLine("serverSync certificateFile.cer");
            Environment.Exit(1);
        }

        internal static void TerminateAndExit()
        {
            try
            {
               //await Task.Factory.StartNew(()=> );
                Environment.Exit(0);
            }
            catch (Exception e)
            {
                Console.WriteLine("Error while terminate CloudBox-server" + e.Message);
            }

        }
    }


}

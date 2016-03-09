using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Forms.DataVisualization;
using System.Runtime.InteropServices;
using System.IO;

namespace cloudbox_server
{
    /// <summary>
    /// Interaction logic for StartPage.xaml
    /// </summary>
    public partial class StartPage : Window
    {
        private class UserInfo
        {
            public string username { get; set; }
            public long vpf { get; set; }
            public long space { get; set; }
        }

        string username;
        [DllImport("kernel32.dll")]
        static extern IntPtr GetConsoleWindow();

        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        const int SW_HIDE = 0;
        const int SW_SHOW = 5;


        public StartPage()
        {
            InitializeComponent();                      
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Button b =(Button) sender;
            if (b.Content.ToString().CompareTo("Stop Server") == 0)
            {
                b.Content = "Start Server";
                TcpSslServer.SslTcpServer.StopServer();
            }
            else
            {
                b.Content = "Stop Server";
                TcpSslServer.SslTcpServer.StartServer();

            }
        }

        private void RemoveAllPanelChildren(Panel p)
        {
            p.Children.RemoveRange(0, p.Children.Count);
        }

        private void ShowUser_Click(object sender, RoutedEventArgs e)
        {
            RemoveAllPanelChildren(panel2);
            try
            {
                TextBlock title = new TextBlock();
                title.FontSize = 25;
                title.Text = "Elenco Utenti";
                title.TextAlignment = TextAlignment.Center;
                panel2.Children.Add(title);
                ListBox elenco = new ListBox();
                elenco.Margin = new Thickness(20, 20, 20, 20);
                elenco.MaxHeight = 400;
                List<string> users = DatabaseUtils.GetAllUsers();
                foreach (string s in users)
                {
                    ListBoxItem t = new ListBoxItem();
                    t.Content = s;
                    t.MouseDoubleClick += ShowUserInfo_Click;
                    elenco.Items.Add(t);
                }

                panel2.Children.Add(elenco);
            }
            catch (Exception)
            {

            }
        }

        private void ShowUserInfo_Click(object sender, RoutedEventArgs e)
        {
            RemoveAllPanelChildren(panel2);
            try
            {
                ContextMenu cm= new ContextMenu();
                MenuItem myMenuItem = new MenuItem();
                myMenuItem.Header = "Delete";
                myMenuItem.Click += DeleteFile_RightClick;
                cm.Items.Add(myMenuItem);

                ListBoxItem user = (ListBoxItem)sender;
                TextBlock title = new TextBlock();
                title.FontSize = 35;
                title.Text = user.Content.ToString();
                username = user.Content.ToString();
                title.TextAlignment = TextAlignment.Center;
                panel2.Children.Add(title);
                DockPanel mydock = new DockPanel();
                StackPanel infoPanel = new StackPanel();                
                StackPanel allFileVersionsPanel = new StackPanel();
                //allFileVersionsPanel.Width = 200;
                UserInfo ui= new UserInfo();
                int vpf=DatabaseUtils.getUserVpf(user.Content.ToString()); 
                TextBlock info1 = new TextBlock();
                info1.Text = "Maximum versions per file: " + vpf;              
                TextBlock info2 = new TextBlock();
                SpaceInfo si = DatabaseUtils.GetSpaces(user.Content.ToString());
                info2.Text = "Total space: "+(int) (si.TotalSpace/((long)1024*(long)1024))+" MB";
                
                TextBlock info3 = new TextBlock();
                info3.Text = "Used Space: "+(int) (si.SpaceUsed/(long)(1024*1024))+" MB";

                Button removeUser_btn = new Button();
                removeUser_btn.Margin = new Thickness(10, 10, 10, 10); 
                removeUser_btn.Content = "Delete user";
                removeUser_btn.DataContext = user.Content.ToString();
                removeUser_btn.Click += RemoveUser_Click;

                Button showCurrentConf_btn = new Button();
                showCurrentConf_btn.Margin = new Thickness(10, 10, 10, 10);
                showCurrentConf_btn.Content = "Show Folder Content";
                showCurrentConf_btn.DataContext = user.Content.ToString();
                showCurrentConf_btn.Click += ShowCurrentConf_Click;

                Button refresh_btn= new Button();
                refresh_btn.Margin = new Thickness(10, 10, 10, 10);
                refresh_btn.Content = "Refresh";
                refresh_btn.DataContext = user.Content.ToString();
                refresh_btn.Click += Refresh_Click;
                 

                ui.username = user.Content.ToString();
                ui.vpf = vpf;
                ui.space = si.TotalSpace;

                Button userPreference_btn = new Button();
                userPreference_btn.Margin = new Thickness(10, 10, 10, 10);
                userPreference_btn.Content = "Settings";
                userPreference_btn.DataContext = ui;
                userPreference_btn.Click += UserSettings_Click;

                removeUser_btn.VerticalAlignment = VerticalAlignment.Bottom;
                TextBlock titleElenco = new TextBlock();
                titleElenco.Margin = new Thickness(10, 10, 10, 10);
                titleElenco.FontSize = 25;
                titleElenco.TextAlignment = TextAlignment.Center;
                titleElenco.Text = "Files Uploaded";
                allFileVersionsPanel.Children.Add(titleElenco);

                List<string> paths = new List<string>();

                ListBox elenco = new ListBox();
                elenco.MaxHeight = 200;

                List<ClientFileInfo> currentConf = new List<ClientFileInfo>();

                List<FileVersions> fvl = new List<FileVersions>();
                try
                {
                    fvl = DatabaseUtils.getAllVersions(user.Content.ToString());
                    currentConf = DatabaseUtils.GetCurrentConf(user.Content.ToString());
                }
                catch (Exception)
                {
                    ListBoxItem i = new ListBoxItem();
                    i.Content = "no file";
                    elenco.Items.Add(i);
                }
                foreach (FileVersions f in fvl)
                {
                    ListBoxItem i = new ListBoxItem();
                    i.Content = f.Path;
                    elenco.Items.Add(i);
                    foreach (ClientFileInfo c in f.versions)
                    {
                        ListBoxItem item = new ListBoxItem();
                        item.Content = "\t -" + c.Hash + " size: " +c.Size+" - last modify:" + c.LastUpd;
                        item.DataContext = c;
                       // item.ContextMenu = cm;
                        elenco.Items.Add(item);
                    }
                }

                foreach (ClientFileInfo c in currentConf)
                {
                    paths.Add(c.RelativePath);
                }
                
                allFileVersionsPanel.Children.Add(elenco);
                infoPanel.Children.Add(info1);
                infoPanel.Children.Add(info2);
                infoPanel.Children.Add(info3);
                infoPanel.Children.Add(removeUser_btn);
                infoPanel.Children.Add(userPreference_btn);
                infoPanel.Children.Add(showCurrentConf_btn);
                //infoPanel.Children.Add(refresh_btn);
                infoPanel.Margin = new System.Windows.Thickness(10,10,10,10);
                allFileVersionsPanel.Margin = new System.Windows.Thickness(10, 10, 10, 10);                
                mydock.Children.Add(infoPanel);
                mydock.Children.Add(allFileVersionsPanel);
                panel2.Children.Add(mydock);
            }
            catch (Exception exc)
            {
                Console.WriteLine(exc.Message);
            }

        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            panel2.UpdateLayout();
        }

        private static ListBoxItem UpwardSerch(DependencyObject source)
        {
            while (source != null && !(source is ListBoxItem))
            {
                source = VisualTreeHelper.GetParent(source);
            }
            return source as ListBoxItem;
        }


        private void DeleteFile_RightClick(object sender, RoutedEventArgs e)
        {
            try
            {
                ListBoxItem l=UpwardSerch(e.OriginalSource as DependencyObject);
                if (l == null)
                {
                    Console.WriteLine("l è nulll");
                    return;
                }
                var confirmResult = System.Windows.Forms.MessageBox.Show("This operation will delete permanently this file. Confirm?",
                                               "Exit",
                                              System.Windows.Forms.MessageBoxButtons.YesNo);
                if (confirmResult == System.Windows.Forms.DialogResult.Yes)
                {
                    ClientFileInfo cli = (ClientFileInfo)l.DataContext;                   
                    Task.Factory.StartNew(() => File.Delete(TcpSslServer.SslTcpServer.FileStoragePath+username+"\\"+cli.Hash));
                }    
            }
            catch(Exception ex){
                Console.WriteLine(ex.Message);
            }
        }


        private void ShowCurrentConf_Click(object sender, RoutedEventArgs e)
        {

            if (username == null)
            {
                return;
            }

            List<string> paths = new List<string>();

            ListBox elenco = new ListBox();
            elenco.MaxHeight = 200;

            List<ClientFileInfo> currentConf = new List<ClientFileInfo>();

            try
            {
                currentConf = DatabaseUtils.GetCurrentConf(username);
            }
            catch (Exception)
            {
                return;
            }
            
            foreach (ClientFileInfo c in currentConf)
            {
                paths.Add(c.RelativePath);
            }

            
            Configuration f1 = new Configuration(paths);
            f1.Text = username + " current configuration";
            f1.Show();

        }


       /* private void Form1_Load(object sender, EventArgs e)
        {
            var paths = new List<string>
                        {
                            @"C:\WINDOWS\AppPatch\MUI\040C",
                            @"C:\WINDOWS\Microsoft.NET\Framework\v2.0.50727",
                            @"C:\WINDOWS\Microsoft.NET\Framework\v2.0.50727\MUI",
                            @"C:\WINDOWS\addins",
                            @"C:\WINDOWS\AppPatch",
                            @"C:\WINDOWS\AppPatch\MUI",
                            @"C:\WINDOWS\Microsoft.NET\Framework\v2.0.50727\MUI\0409"
                        };

            treeView1.PathSeparator = @"\";

            PopulateTreeView(treeView1, paths, '\\');
        }
        */

        private static void PopulateTreeView(System.Windows.Forms.TreeView treeView, IEnumerable<string> paths, char pathSeparator)
        {
            System.Windows.Forms.TreeNode lastNode = null;
            string subPathAgg;
            foreach (string path in paths)
            {
                subPathAgg = string.Empty;
                foreach (string subPath in path.Split(pathSeparator))
                {
                    subPathAgg += subPath + pathSeparator;
                    System.Windows.Forms.TreeNode[] nodes = treeView.Nodes.Find(subPathAgg, true);
                    if (nodes.Length == 0)
                        if (lastNode == null)
                            lastNode = treeView.Nodes.Add(subPathAgg, subPath);
                        else
                            lastNode = lastNode.Nodes.Add(subPathAgg, subPath);
                    else
                        lastNode = nodes[0];
                }
            }
        }

        private void Window_Closing(object sender, RoutedEventArgs e)
        {
            var confirmResult = System.Windows.Forms.MessageBox.Show("This operation will close all connections. Confirm?",
                                                "Exit",
                                               System.Windows.Forms.MessageBoxButtons.YesNo);
            if (confirmResult == System.Windows.Forms.DialogResult.Yes)
            {
                //Termina il programma ed esce
                Console.WriteLine("pressed yes");
                TcpSslServer.SslTcpServer.TerminateAndExit();
                this.Close();
            }        
        }

        private void UserSettings_Click(object sender, RoutedEventArgs e)
        {
            Button b = (Button)sender;
            UserInfo ui = (UserInfo) b.DataContext;
            UserSettings s = new UserSettings(ui.username, ui.vpf, ui.space / (1024 * 1024));
            s.Show();
        }

        private void RemoveUser_Click(object sender, RoutedEventArgs e)
        {
            Button b=(Button) sender;
            Console.WriteLine(sender.ToString() + " "+e.ToString()+" Utente:"+b.DataContext);
            var confirmResult = System.Windows.Forms.MessageBox.Show("Are you sure to delete user " + b.DataContext + " ?",
                                     "Confirm Delete",
                                    System.Windows.Forms.MessageBoxButtons.YesNo);
            if (confirmResult == System.Windows.Forms.DialogResult.Yes)
            {
                // If 'Yes', do something here.
                Console.WriteLine("Deleted");
                DatabaseUtils.DeleteUser(b.DataContext.ToString());
                RemoveAllPanelChildren(panel2);
                Label l = new Label();
                l.Content = "User " + b.DataContext.ToString() + " deleted";
                l.Margin = new Thickness(20, 20, 20, 20);
                l.HorizontalAlignment = HorizontalAlignment.Center;
                l.VerticalAlignment = VerticalAlignment.Center;
                l.FontSize = 30;
                panel2.Children.Add(l);
            }
            else
            {            
                // If 'No', do something here.
                Console.WriteLine("NO Deleted");                
            }
        }

        
      

        private void StartStopServer_Click(object sender, RoutedEventArgs e)
        {
            Button b = (Button)sender;
            if (b.Content.ToString().CompareTo("Stop Server") == 0)
            {
                Console.WriteLine("Sono in START e vado in STOP");
                b.Content = "Start Server";
                Task.Factory.StartNew(()=> TcpSslServer.SslTcpServer.StopServer());
            }
            else
            {
                Console.WriteLine("Sono in STOP e vado in START");
                b.Content = "Stop Server";
                Task.Factory.StartNew(() =>  TcpSslServer.SslTcpServer.StartServer());

            }
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
             var confirmResult = System.Windows.Forms.MessageBox.Show("This operation will close all connections. Confirm?",
                                     "Exit",
                                    System.Windows.Forms.MessageBoxButtons.YesNo);
             if (confirmResult == System.Windows.Forms.DialogResult.Yes)
             {
                 //Termina il programma ed esce
                 Console.WriteLine("pressed yes");
                 TcpSslServer.SslTcpServer.TerminateAndExit();
                 this.Close();
             }
        }

        private void ShowHideLog_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Button b = (Button)sender;
                var handle = GetConsoleWindow();
                if (b.Content.ToString().CompareTo("Show Log") == 0)
                {
                    // Show
                    ShowWindow(handle, SW_SHOW);
                    b.Content = "Hide Log";
                }
                else
                {
                    // Hide
                    ShowWindow(handle, SW_HIDE);
                    b.Content = "Show Log";
                }
            }
            catch(Exception ){}                           
           
        }

        private void ListBoxItem_Selected(object sender, RoutedEventArgs e)
        {

        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            var confirmResult = System.Windows.Forms.MessageBox.Show("This operation will close all connections. Confirm?",
                                     "Exit",
                                    System.Windows.Forms.MessageBoxButtons.YesNo);
            if (confirmResult == System.Windows.Forms.DialogResult.Yes)
            {
                //Termina il programma ed esce
                Console.WriteLine("pressed yes");
                TcpSslServer.SslTcpServer.TerminateAndExit();
                this.Close();
            }
        }

        
    }
}

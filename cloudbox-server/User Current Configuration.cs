using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace cloudbox_server
{
    
        public partial class Configuration: Form
        {
            public Configuration(List<string> paths)
            {
                InitializeComponent();
               // treeView1.Width =   (int) Double.NaN ;
               // treeView1.Height = Double.NaN as int;
                ImageList imglist1 = new ImageList();
                System.Drawing.Image myImage = 
                 Image.FromFile
                (System.Environment.GetFolderPath
                (System.Environment.SpecialFolder.DesktopDirectory)
                + @"\fileicon.png");
                imglist1.Images.Add(myImage);

                System.Drawing.Image myImage2 =
                 Image.FromFile
                (System.Environment.GetFolderPath
                (System.Environment.SpecialFolder.DesktopDirectory)
                + @"\folder.png");
                imglist1.Images.Add(myImage2);

                treeView1.ImageList = imglist1;
                treeView1.PathSeparator = @"\";

                PopulateTreeView(treeView1, paths, '\\');
            }

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
                        {
                            if (lastNode == null)
                            {                        
                                lastNode = treeView.Nodes.Add(subPathAgg, subPath);                                
                            }
                            else
                            {                                
                                lastNode = lastNode.Nodes.Add(subPathAgg, subPath);                                
                            }
                        }

                        else
                        {
                            lastNode = nodes[0];
                        }
                     }
                }
                foreach(TreeNode n in treeView.Nodes)
                {
                    contanodi(n);
                }
            }

            

            private static int contanodi(TreeNode n)
            {
                int c = 0;
                if (n.Nodes.Count == 0)
                {
                    n.ImageIndex = 0;
                    n.SelectedImageIndex = 0;
                    return 1;
                }
                else
                {
                    n.ImageIndex = 1;
                    n.SelectedImageIndex = 1;
                }
                
                foreach (TreeNode node in n.Nodes)
                {
                    c += contanodi(node);
                }
                return c+1;
            }
            
        }

}

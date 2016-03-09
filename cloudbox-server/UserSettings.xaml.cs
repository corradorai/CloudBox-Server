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

namespace cloudbox_server
{
    /// <summary>
    /// Interaction logic for UserControl1.xaml
    /// </summary>
    public partial class UserSettings : Window
    {
        private string username;
        private long v,space;
        public UserSettings(string username,long v,long space)
        {
            this.username = username;
            this.v = v;
            this.space = space;
            InitializeComponent();
            TitleUser.Text = username;
            vpf_Slider.Minimum = 5;
            vpf_Slider.Maximum = 10;
            vpf_Slider.Value = v;
            vpf_Slider.ValueChanged+=vpf_slider_ValueChanged;

            TotalSpace_Slider.Minimum = 10;
            TotalSpace_Slider.Maximum = 2000;
            TotalSpace_Slider.Value = space;
            TotalSpace_Slider.ValueChanged+=TotalSpace_Slider_ValueChanged;
            TotalSpace.Text = space.ToString();
            vpf.Text = v.ToString();
        }

        private void vpf_slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Slider s = (Slider)sender;
            vpf.Text = ((long)s.Value).ToString();
        }

        private void TotalSpace_Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Slider s = (Slider)sender;
            TotalSpace.Text =  ((long)s.Value).ToString() + " MB";
            space = ((long)s.Value) * 1024 * 1024;
        }       

        private void vpf_Slider_ManipulationStarted(object sender, ManipulationStartedEventArgs e)
        {
            Slider s = (Slider)sender;
            vpf.Text = s.Value.ToString();
            v = int.Parse(vpf.Text);
        }


        //SAVE
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            //Update info into server
            try
            {
                Console.WriteLine("vpf: " + v + " space " + space+" user: "+username);
                DatabaseUtils.UpdateUserSpaceAndVpf(username, long.Parse(vpf.Text), space);
                this.Close();
            }
            catch (Exception)
            {
                MessageBox.Show("Server Error. Retry later", "ServerError",MessageBoxButton.OK);
            }
        }



        //CANCEL
        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}

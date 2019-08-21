using SeldatMRMS.Management.RobotManagent;
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
using System.Windows.Shapes;

namespace SeldatUnilever_Ver1._02.Management.RobotManagent
{
    /// <summary>
    /// Interaction logic for RobotMoving.xaml
    /// </summary>
    public partial class RobotMoving : Window
    {
        public Dictionary<String,RobotUnity> robotList;
        public RobotMoving(Dictionary<String,RobotUnity> robotList)
        {
            InitializeComponent();
            this.robotList = robotList;
        }
        public void  sld_x_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            SetLine();
            if (ck_robot1.IsChecked==true)
            {
                Point px = this.robotList.ElementAt(0).Value.properties.pose.Position;
                this.robotList.ElementAt(0).Value.properties.pose.Position= new Point((double)sld_x.Value, px.Y);
            }
            if (ck_robot2.IsChecked == true)
            {
                Point px = this.robotList.ElementAt(1).Value.properties.pose.Position;
                this.robotList.ElementAt(1).Value.properties.pose.Position = new Point((double)sld_x.Value, px.Y);
            }
            if (ck_robot3.IsChecked == true)
            {
                Point px = this.robotList.ElementAt(2).Value.properties.pose.Position;
                this.robotList.ElementAt(2).Value.properties.pose.Position = new Point((double)sld_x.Value, px.Y);
            }
        }

        private void sld_y_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            SetLine();
            if (ck_robot1.IsChecked == true)
            {
                Point py = this.robotList.ElementAt(0).Value.properties.pose.Position;
                this.robotList.ElementAt(0).Value.properties.pose.Position = new Point(py.X,(double)sld_y.Value);
            }
            if (ck_robot2.IsChecked == true)
            {
                Point py = this.robotList.ElementAt(1).Value.properties.pose.Position;
                this.robotList.ElementAt(1).Value.properties.pose.Position = new Point(py.X, (double)sld_y.Value);
            }
            if (ck_robot3.IsChecked == true)
            {
                Point py = this.robotList.ElementAt(2).Value.properties.pose.Position;
                this.robotList.ElementAt(2).Value.properties.pose.Position = new Point(py.X, (double)sld_y.Value);
            }
        }

        private void sld_theta_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            SetLine();
            if (ck_robot1.IsChecked == true)
            {
               this.robotList.ElementAt(0).Value.properties.pose.Angle= -sld_theta.Value;
            }
            if (ck_robot2.IsChecked == true)
            {
                this.robotList.ElementAt(1).Value.properties.pose.Angle = -sld_theta.Value ;
            }
            if (ck_robot3.IsChecked == true)
            {
                this.robotList.ElementAt(2).Value.properties.pose.Angle = -sld_theta.Value;
            }
        }
        public void SetLine()
        {
            if (ck_robot1_Line.IsChecked == true)
            {
                this.robotList.ElementAt(0).Value.SwitchToDetectLine(true);
            }
            else 
            {
                this.robotList.ElementAt(0).Value.SwitchToDetectLine(false);
            }
            if (ck_robot2_Line.IsChecked == true)
            {
                this.robotList.ElementAt(1).Value.SwitchToDetectLine(true);
            }
            else
            {
                this.robotList.ElementAt(1).Value.SwitchToDetectLine(false);
            }
            if (ck_robot3_Line.IsChecked == true)
            {
                this.robotList.ElementAt(2).Value.SwitchToDetectLine(true);
            }
            else
            {
                this.robotList.ElementAt(2).Value.SwitchToDetectLine(false);
            }
        }
        private void ck_robot1_Line_Checked(object sender, RoutedEventArgs e)
        {

        }

        private void ck_robot2_Line_Checked(object sender, RoutedEventArgs e)
        {
           
        }

        private void ck_robot3_Line_Checked(object sender, RoutedEventArgs e)
        {
           
        }
    }
}

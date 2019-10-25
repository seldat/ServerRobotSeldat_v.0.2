using SeldatMRMS.Management.RobotManagent;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
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
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class ForceGate : Window
    {
        RobotUnity robot;
        System.Timers.Timer TimerForceGate;
        public ForceGate(RobotUnity robot)
        {
            InitializeComponent();
            
            this.robot = robot;
            TimerForceGate = new System.Timers.Timer();
            TimerForceGate.Interval = 1000;
            TimerForceGate.Elapsed += OnForceGateIntterupTimer;
            TimerForceGate.AutoReset = false;
            TimerForceGate.Enabled = false;

        }

        private void OnForceGateIntterupTimer(object sender, ElapsedEventArgs e)
        {
            this.robot.onForceGoToGate = false;
            Close();
        }

        private void Btn_yes_Click(object sender, RoutedEventArgs e)
        {
           
        }

        private void Btn_no_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Window_Closed(object sender, EventArgs e)
        {
       
            this.robot.onForceGoToGate = false;
        }

        private void Btn_ok_Click(object sender, RoutedEventArgs e)
        {
        
            TimerForceGate.Enabled = true;
        }
    }
}

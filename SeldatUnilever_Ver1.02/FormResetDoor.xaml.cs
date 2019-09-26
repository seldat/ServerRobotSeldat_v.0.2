using SeldatMRMS;
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
using static DoorControllerService.DoorService;

namespace SeldatUnilever_Ver1._02
{
    /// <summary>
    /// Interaction logic for FormResetDoor.xaml
    /// </summary>
    public partial class FormResetDoor : Window
    {
        public FormResetDoor()
        {
            InitializeComponent();
        }

        private void CmdDoor1Reset_Click(object sender, RoutedEventArgs e)
        {
            Global_Object.onFlagDoorBusy = false;
            Global_Object.onFlagRobotComingGateBusy = false;
            Global_Object.setGateStatus((int)DoorId.DOOR_MEZZAMINE_UP_NEW, false); // gate 1
            Global_Object.doorManagementServiceCtrl.DoorMezzamineUpNew.ResetDoor();
            Global_Object.doorManagementServiceCtrl.DoorMezzamineUpNew.LampSetStateOff(DoorType.DOOR_FRONT);
            lblStatus.Content = "Gate 1 finished reset ";
        }

        private void CmdDoor2Reset_Click(object sender, RoutedEventArgs e)
        {
            Global_Object.onFlagDoorBusy = false;
            Global_Object.onFlagRobotComingGateBusy = false;
            Global_Object.setGateStatus((int)DoorId.DOOR_MEZZAMINE_UP, false); // gate 2
            Global_Object.doorManagementServiceCtrl.DoorMezzamineUp.ResetDoor();
            Global_Object.doorManagementServiceCtrl.DoorMezzamineUp.LampSetStateOff(DoorType.DOOR_FRONT);
            lblStatus.Content = "Gate 2 finished reset ";
        }
    }
}

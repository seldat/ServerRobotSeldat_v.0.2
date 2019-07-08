using SeldatMRMS.Management.RobotManagent;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SeldatUnilever_Ver1._02.Management.TrafficManager
{
    public static class TrafficRountineConstants
    {
        // check in c1 là vùng qua ready /gate /vim
        // G12 : Gate 1 2
        // G3 : Gate 3
        // Elevator : vùng thang máy
        // C2 : là điểm trong vùng VIM đi
         public static RegistryIntersectionZone RegIntZone_READY = new RegistryIntersectionZone("READY");
         public static RegistryIntersectionZone RegIntZone_GATE12 = new RegistryIntersectionZone("GATE12");
         public static RegistryIntersectionZone RegIntZone_GATE3 = new RegistryIntersectionZone("GATE3");
         public static RegistryIntersectionZone RegIntZone_ELEVATOR = new RegistryIntersectionZone("ELEVATOR");
         public static RegistryIntersectionZone RegIntZone_VIM = new RegistryIntersectionZone("VIM");
         // Từ C1 -> G3 
         // Check register G3 -> Ready ->Gate 12 -> Elevator
         public static bool Reg_checkinC1_G3(RobotUnity robot)
         {
            bool onRegReady = false;
            bool onRegG12 = false;
            bool onRegElev = false;
            bool onRegG3 = false;
            if(RegIntZone_READY.ProcessRegistryIntersectionZone(robot))
            {
                onRegReady = true;
            }
            if (RegIntZone_GATE12.ProcessRegistryIntersectionZone(robot))
            {
                onRegG12 = true;
            }
            if (RegIntZone_ELEVATOR.ProcessRegistryIntersectionZone(robot))
            {
                onRegElev = true;
            }
            if (RegIntZone_GATE3.ProcessRegistryIntersectionZone(robot))
            {
                onRegG3 = true;
            }
            if(onRegReady && onRegG12 && onRegElev && onRegG3)
            {
                return true;
            }
            return false;
        }
        // Từ C1 -> Elevator,VIM
        // Check register Elevator -> Ready ->Gate 12
        public static bool Reg_checkinC1_ElevatorAndVIM(RobotUnity robot)
        {
            bool onRegReady = false;
            bool onRegG12 = false;
            bool onRegElev = false;
            if (RegIntZone_READY.ProcessRegistryIntersectionZone(robot))
            {
                onRegReady = true;
            }
            if (RegIntZone_GATE12.ProcessRegistryIntersectionZone(robot))
            {
                onRegG12 = true;
            }
            if (RegIntZone_ELEVATOR.ProcessRegistryIntersectionZone(robot))
            {
                onRegElev = true;
            }
            if (onRegReady && onRegG12 && onRegElev)
            {
                return true;
            }
            return false;
        }
        // Từ C1 -> Gate12
        // Check register Elevator -> Ready ->Gate 12
        public static bool Reg_checkinC1_Gate12(RobotUnity robot)
        {
            bool onRegReady = false;
            bool onRegG12 = false;
            if (RegIntZone_READY.ProcessRegistryIntersectionZone(robot))
            {
                onRegReady = true;
            }
            if (RegIntZone_GATE12.ProcessRegistryIntersectionZone(robot))
            {
                onRegG12 = true;
            }
            if (onRegReady && onRegG12)
            {
                return true;
            }
            return false;
        }
        // Từ C1 -> Ready
        // Check register Ready
        public static bool Reg_checkinC1_Ready(RobotUnity robot)
        {
            bool onRegReady = false;
            if (RegIntZone_READY.ProcessRegistryIntersectionZone(robot))
            {
                onRegReady = true;
            }
            if (onRegReady)
            {
                return true;
            }
            return false;
        }



    }
}

// Tran Huu Luat 2019 09 07
// Thuật Toán Kiểm Tra Vùng Và Đăng Ký Vùng 
using SeldatMRMS.Management.RobotManagent;
using SeldatMRMS.Management.TrafficManager;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

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
        // Detect no Robot : Ready , G12, Elevator, G3
        // Check register G3 -> Ready ->Gate 12 -> Elevator
        public static bool Reg_checkinC1_G3(RobotUnity robot, TrafficManagementService traffic)
        {
            bool onRegReady = false;
            bool onRegG12 = false;
            bool onRegElev = false;
            bool onRegG3 = false;
            //kiem tra có robot trong vùng này, nếu có trả về false
            if (traffic.HasRobotUnityinArea("READY") || traffic.HasRobotUnityinArea("GATE12") || traffic.HasRobotUnityinArea("ELEVATOR") || traffic.HasRobotUnityinArea("GATE3"))
            {
                return false;
            }
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
            if (RegIntZone_GATE3.ProcessRegistryIntersectionZone(robot))
            {
                onRegG3 = true;
            }
            if (onRegReady && onRegG12 && onRegElev && onRegG3)
            {
                return true;
            }
            return false;
        }
        // Từ C1 -> Elevator,VIM
        // Detect no Robot : Ready , G12, Elevator
        // Check register Elevator -> Ready ->Gate 12
        public static bool Reg_checkinC1_ElevatorAndVIM(RobotUnity robot, TrafficManagementService traffic)
        {
            bool onRegReady = false;
            bool onRegG12 = false;
            bool onRegElev = false;
            //kiem tra có robot trong vùng này, nếu có trả về false
            if (traffic.HasRobotUnityinArea("READY") || traffic.HasRobotUnityinArea("GATE12") || traffic.HasRobotUnityinArea("ELEVATOR"))
            {
                return false;
            }
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
        // Detect no Robot : Ready , G12
        // Check register Elevator -> Ready ->Gate 12
        public static bool Reg_checkinC1_Gate12(RobotUnity robot, TrafficManagementService traffic)
        {
            bool onRegReady = false;
            bool onRegG12 = false;
            //kiem tra có robot trong vùng này, nếu có trả về false
            if (traffic.HasRobotUnityinArea("READY") || traffic.HasRobotUnityinArea("GATE12"))
            {
                return false;
            }
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
        // Detect no Robot : Ready
        // Check register Ready
        public static bool Reg_checkinC1_Ready(RobotUnity robot, TrafficManagementService traffic)
        {
            bool onRegReady = false;
            //kiem tra có robot trong vùng này, nếu có trả về false
            if (traffic.HasRobotUnityinArea("READY"))
            {
                return false;
            }
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

        // Từ C2 -> Gate12
        // Detect no Robot : G12
        // Check register Gate 12
        public static bool Reg_checkinC2_G12(RobotUnity robot, TrafficManagementService traffic)
        {
            bool onRegG12 = false;
            //kiem tra có robot trong vùng này, nếu có trả về false
            if (traffic.HasRobotUnityinArea("GATE12") || traffic.HasRobotUnityinArea("ELEVATOR") || traffic.HasRobotUnityinArea("GATE3"))
            {
                return false;
            }
            if (RegIntZone_GATE12.ProcessRegistryIntersectionZone(robot))
            {
                onRegG12 = true;
            }
            if (onRegG12)
            {
                return true;
            }
            return false;
        }
        // Từ C2 -> Gate 3
        // Detect no Robot : G12, Elevator ,G3
        // Check register G3 -> Ready ->Gate 12 -> Elevator
        public static bool Reg_checkinC2_G3(RobotUnity robot, TrafficManagementService traffic)
        {
            bool onRegG12 = false;
            bool onRegElev = false;
            bool onRegG3 = false;
            Point Rloc = robot.properties.pose.Position;
            //kiem tra có robot trong vùng này, nếu có trả về false
            if (traffic.HasRobotUnityinArea("GATE12") || traffic.HasRobotUnityinArea("ELEVATOR") ||  traffic.HasRobotUnityinArea("GATE3"))
            {
                return false;
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
            if (onRegG12 && onRegElev && onRegG3)
            {
                return true;
            }
            return false;
        }
        // Từ C2 -> Elevator,VIM
        // Detect no Robot : G12, Elevator
        // Check register Elevator  ->Gate 12
        public static bool Reg_checkinC2_ElevatorAndVIM(RobotUnity robot, TrafficManagementService traffic)
        {
            bool onRegG12 = false;
            bool onRegElev = false;
            Point Rloc = robot.properties.pose.Position;
            //kiem tra có robot trong vùng này, nếu có trả về false
            if (traffic.HasRobotUnityinArea("GATE12") || traffic.HasRobotUnityinArea("ELEVATOR"))
            {
                return false;
            }
            if (RegIntZone_GATE12.ProcessRegistryIntersectionZone(robot))
            {
                onRegG12 = true;
            }
            if (RegIntZone_ELEVATOR.ProcessRegistryIntersectionZone(robot))
            {
                onRegElev = true;
            }
            if (onRegG12 && onRegElev)
            {
                return true;
            }
            return false;
        }

        // Từ C2 -> Outer
        // Detect no Robot : G12, Ready
        // Check register  Ready ->Gate 12
        public static bool Reg_checkinC2_Outer(RobotUnity robot, TrafficManagementService traffic)
        {
            bool onRegG12 = false;
            bool onRegReady = false;
            Point Rloc = robot.properties.pose.Position;
            //kiem tra có robot trong vùng này, nếu có trả về false
            if (traffic.HasRobotUnityinArea("READY") || traffic.HasRobotUnityinArea("GATE12"))
            {
                return false;
            }
            if (RegIntZone_GATE12.ProcessRegistryIntersectionZone(robot))
            {
                onRegG12 = true;
            }

            if (RegIntZone_READY.ProcessRegistryIntersectionZone(robot))
            {
                onRegReady = true;
            }

            if (onRegG12 && onRegReady)
            {
                return true;
            }
            return false;
        }

        // Từ Gate12 ( C3) -> VIM and ELEVATOR
        // Detect no Robot : Elevator
        public static bool Reg_checkinGate12_ElevatorAndVIM(RobotUnity robot, TrafficManagementService traffic)
        {
            bool onRegElev = false;
            Point Rloc = robot.properties.pose.Position;
            //kiem tra có robot trong vùng này, nếu có trả về false
            if (traffic.HasRobotUnityinArea("ELEVATOR"))
            {
                return false;
            }
            if (RegIntZone_ELEVATOR.ProcessRegistryIntersectionZone(robot))
            {
                onRegElev = true;
            }
            if (onRegElev)
            {
                return true;
            }
            return false;
        }
        // Từ Gate12(C3) -> ELEVATOR-> G3
        // Detect no Robot : Elevator ,G3
        public static bool Reg_checkinGate12_Gate3(RobotUnity robot, TrafficManagementService traffic)
        {
            bool onRegElev = false;
            bool onRegG3 = false;
            //kiem tra có robot trong vùng này, nếu có trả về false
            if (traffic.HasRobotUnityinArea("GATE12") || traffic.HasRobotUnityinArea("ELEVATOR"))
            {
                return false;
            }
            if (RegIntZone_ELEVATOR.ProcessRegistryIntersectionZone(robot))
            {
                onRegElev = true;
            }
            if (RegIntZone_GATE3.ProcessRegistryIntersectionZone(robot))
            {
                onRegG3 = true;
            }
            if (onRegElev && onRegG3)
            {
                return true;
            }
            return false;
        }

        // Từ Gate12(C3) -> Ready and Outer
        // Detect no Robot : Ready
        public static bool Reg_checkinGate12_ReadyAndOuter(RobotUnity robot, TrafficManagementService traffic)
        {
            bool onRegReady = false;
            //kiem tra có robot trong vùng này, nếu có trả về false
            if (traffic.HasRobotUnityinArea("READY"))
            {
                return false;
            }
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

        // Từ Gate3(C4) -> Ready and Outer
        // Detect no Robot : ready, GATE12, ELEVATOR
        public static bool Reg_checkinGate3_ReadyAndOuter(RobotUnity robot, TrafficManagementService traffic)
        {
            bool onRegReady = false;
            bool onRegG12 = false;
            bool onRegElev = false;
            //kiem tra có robot trong vùng này, nếu có trả về false
            if (traffic.HasRobotUnityinArea("READY") || traffic.HasRobotUnityinArea("GATE12") || traffic.HasRobotUnityinArea("ELEVATOR"))
            {
                return false;
            }
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
            if (onRegG12 && onRegElev && onRegReady)
            {
                return true;
            }
            return false;
        }

        // Từ Gate3(C4) -> ELEVATOR-> Gate12
        // Detect no Robot :  GATE12, ELEVATOR
        public static bool Reg_checkinGate3_Gate12(RobotUnity robot, TrafficManagementService traffic)
        {
            bool onRegG12 = false;
            bool onRegElev = false;
            //kiem tra có robot trong vùng này, nếu có trả về false
            //kiem tra có robot trong vùng này, nếu có trả về false
            if (traffic.HasRobotUnityinArea("GATE12") || traffic.HasRobotUnityinArea("ELEVATOR"))
            {
                return false;
            }
            if (RegIntZone_GATE12.ProcessRegistryIntersectionZone(robot))
            {
                onRegG12 = true;
            }
            if (RegIntZone_ELEVATOR.ProcessRegistryIntersectionZone(robot))
            {
                onRegElev = true;
            }
            if (onRegG12 && onRegElev)
            {
                return true;
            }
            return false;
        }
        // Từ Gate3(C4) -> Elevator And VIM
        // Detect no Robot : ELEVATOR
        public static bool Reg_checkinGate3_ElevatorAndVIM(RobotUnity robot, TrafficManagementService traffic)
        {
            bool onRegElev = false;
            //kiem tra có robot trong vùng này, nếu có trả về false
            if (traffic.HasRobotUnityinArea("ELEVATOR"))
            {
                return false;
            }
            if (RegIntZone_ELEVATOR.ProcessRegistryIntersectionZone(robot))
            {
                onRegElev = true;
            }
            if (onRegElev)
            {
                return true;
            }
            return false;
        }

        // Từ Elevator(C5) -> Ready and Outer
        // Detect no Robot : ready, GATE12
        public static bool Reg_checkinElevator_ReadyAndOuter(RobotUnity robot, TrafficManagementService traffic)
        {
            bool onRegReady = false;
            bool onRegG12 = false;
            //kiem tra có robot trong vùng này, nếu có trả về false
            if (traffic.HasRobotUnityinArea("READY") || traffic.HasRobotUnityinArea("GATE12"))
            {
                return false;
            }
            if (RegIntZone_READY.ProcessRegistryIntersectionZone(robot))
            {
                onRegReady = true;
            }
            if (RegIntZone_GATE12.ProcessRegistryIntersectionZone(robot))
            {
                onRegG12 = true;
            }
            if (onRegG12 && onRegReady)
            {
                return true;
            }
            return false;
        }
        // Từ Elevator(C5) -> Gate12
        // Detect no Robot :  GATE12
        public static bool Reg_checkinElevator_Gate12(RobotUnity robot, TrafficManagementService traffic)
        {
            bool onRegG12 = false;
            //kiem tra có robot trong vùng này, nếu có trả về false
            if (traffic.HasRobotUnityinArea("GATE12") )
            {
                return false;
            }
            if (RegIntZone_GATE12.ProcessRegistryIntersectionZone(robot))
            {
                onRegG12 = true;
            }
            if (onRegG12)
            {
                return true;
            }
            return false;
        }

        // Từ Elevator(C5) -> Gate3
        // Detect no Robot : GATE3
        public static bool Reg_checkinElevator_Gate3(RobotUnity robot, TrafficManagementService traffic)
        {
            bool onRegG3 = false;
            //kiem tra có robot trong vùng này, nếu có trả về false
            if ( traffic.HasRobotUnityinArea("GATE3"))
            {
                return false;
            }
            if (RegIntZone_GATE3.ProcessRegistryIntersectionZone(robot))
            {
                onRegG3 = true;
            }
            if (onRegG3)
            {
                return true;
            }
            return false;
        }

        public static bool StationCheckInSpecialZone(Point startPos,Point endPos,RobotUnity robot,TrafficManagementService traffic)
        {
            String startZone = traffic.DetermineArea(startPos);
            String endZone = traffic.DetermineArea(endPos);

            #region OUTER (C1) -> READY , GATE12, ELEVATOR, GATE3, VIM
            // OUTER->GATE3
            if (startZone.Equals("OUTER") && endZone.Equals("GATE3"))
            {
               return Reg_checkinC1_G3(robot, traffic);
            }
            // OUTER->ELEVATOR && VIM
            if (startZone.Equals("OUTER") && endZone.Equals("ELEVATOR"))
            {
                return Reg_checkinC1_ElevatorAndVIM(robot, traffic);
            }
            if (startZone.Equals("OUTER") && endZone.Equals("VIM"))
            {
                return Reg_checkinC1_ElevatorAndVIM(robot, traffic);
            }
            // OUTER->GATE12
            if (startZone.Equals("OUTER") && endZone.Equals("GATE12"))
            {
                return Reg_checkinC1_Gate12(robot, traffic);
            }
            // OUTER->READY
            if (startZone.Equals("OUTER") && endZone.Equals("READY"))
            {
                return Reg_checkinC1_Gate12(robot, traffic);
            }
            #endregion
            #region VIM (C2) -> READY , GATE12, ELEVATOR, VIM, GATE3
            if (startZone.Equals("VIM") && endZone.Equals("GATE3"))
            {
                return Reg_checkinC2_G3(robot, traffic);
            }
            // VIM->ELEVATOR && VIM
            if (startZone.Equals("VIM") && endZone.Equals("ELEVATOR"))
            {
                return Reg_checkinC2_ElevatorAndVIM(robot, traffic);
            }
            if (startZone.Equals("VIM") && endZone.Equals("VIM"))
            {
                return Reg_checkinC2_ElevatorAndVIM(robot, traffic);
            }
            // VIM->READY && OUTER
            if (startZone.Equals("VIM") && endZone.Equals("READY"))
            {
                return Reg_checkinC2_Outer(robot, traffic);
            }
            if (startZone.Equals("VIM") && endZone.Equals("OUTER"))
            {
                return Reg_checkinC2_Outer(robot, traffic);
            }
            if (startZone.Equals("VIM") && endZone.Equals("GATE3"))
            {
                return Reg_checkinC2_G3(robot, traffic);
            }
            #endregion
            #region GATE12 (C3) -> READY, ELEVATOR, VIM , GATE3
            if (startZone.Equals("GATE12") && endZone.Equals("GATE3"))
            {
                return Reg_checkinGate12_Gate3(robot, traffic);
            }
            // GATE12->ELEVATOR && VIM
            if (startZone.Equals("GATE12") && endZone.Equals("ELEVATOR"))
            {
                return Reg_checkinGate12_ElevatorAndVIM(robot, traffic);
            }
            if (startZone.Equals("GATE12") && endZone.Equals("VIM"))
            {
                return Reg_checkinGate12_ElevatorAndVIM(robot, traffic);
            }
            // GATE12->READY && OUTER
            if (startZone.Equals("GATE12") && endZone.Equals("READY"))
            {
                return Reg_checkinGate12_ReadyAndOuter(robot, traffic);
            }
            if (startZone.Equals("GATE12") && endZone.Equals("OUTER"))
            {
                return Reg_checkinGate12_ReadyAndOuter(robot, traffic);
            }
            #endregion
            #region GATE3 (C4) -> READY, ELEVATOR, VIM , GATE12
            // GATE3->ELEVATOR && VIM
            if (startZone.Equals("GATE3") && endZone.Equals("ELEVATOR"))
            {
                return Reg_checkinGate3_ElevatorAndVIM(robot, traffic);
            }
            if (startZone.Equals("GATE3") && endZone.Equals("VIM"))
            {
                return Reg_checkinGate3_ElevatorAndVIM(robot, traffic);
            }
            // GATE3->READY && OUTER
            if (startZone.Equals("GATE3") && endZone.Equals("READY"))
            {
                return Reg_checkinGate3_ReadyAndOuter(robot, traffic);
            }
            if (startZone.Equals("GATE3") && endZone.Equals("OUTER"))
            {
                return Reg_checkinGate3_ReadyAndOuter(robot, traffic);
            }
            if (startZone.Equals("GATE3") && endZone.Equals("GATE12"))
            {
                return Reg_checkinGate3_Gate12(robot, traffic);
            }
            #endregion
            #region ELEVATOR (C5) -> READY, GATE12, GATE3
            if (startZone.Equals("ELEVATOR") && endZone.Equals("GATE12"))
            {
                return Reg_checkinElevator_Gate12(robot, traffic);
            }
            //  ELEVATOR->READY && OUTER
            if (startZone.Equals("ELEVATOR") && endZone.Equals("READY"))
            {
                return Reg_checkinElevator_ReadyAndOuter(robot, traffic);
            }
            if (startZone.Equals("ELEVATOR") && endZone.Equals("OUTER"))
            {
                return Reg_checkinElevator_ReadyAndOuter(robot, traffic);
            }
            if (startZone.Equals("ELEVATOR") && endZone.Equals("GATE3"))
            {
                return Reg_checkinElevator_Gate3(robot, traffic);
            }
            #endregion

            return false;
        }
        public static void ReleaseZone(RobotUnity robot)
        {
            RegIntZone_READY.Release(robot);
            RegIntZone_GATE12.Release(robot);
            RegIntZone_GATE3.Release(robot);
            RegIntZone_ELEVATOR.Release(robot);
        }

}
}

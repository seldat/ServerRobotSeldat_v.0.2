// Tran Huu Luat 2019 09 07
// Thuật Toán Kiểm Tra Vùng Và Đăng Ký Vùng 
using SeldatMRMS.Management.RobotManagent;
using SeldatMRMS.Management.TrafficManager;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using static SeldatMRMS.Management.RobotManagent.RobotUnityControl;
using static SeldatMRMS.ProcedureControlServices;
using static SelDatUnilever_Ver1._00.Management.TrafficManager.TrafficRounterService;

namespace SeldatUnilever_Ver1._02.Management.TrafficManager
{
    public static class TrafficRountineConstants
    {
        // check in c1 là vùng qua ready /gate /vim
        // G12 : Gate 1 2
        // G3 : Gate 3
        // Elevator : vùng thang máy
        // C2 : là điểm trong vùng VIM đi
        public enum StateCheckOPZS
        {
            REACHED,
            FORWARD,
            CHECKIN,
        }
        public static RegistryIntersectionZone RegIntZone_READY = new RegistryIntersectionZone("READY");
        public static RegistryIntersectionZone RegIntZone_GATE12 = new RegistryIntersectionZone("GATE12");
        public static RegistryIntersectionZone RegIntZone_GATE3 = new RegistryIntersectionZone("GATE3");
        public static RegistryIntersectionZone RegIntZone_ELEVATOR = new RegistryIntersectionZone("ELEVATOR");
        public static RegistryIntersectionZone RegIntZone_VIMBTLCAP = new RegistryIntersectionZone("VIM-BTLCAP");


        //Ready -> GATE12
        public static bool Reg_checkinReady_G12(RobotUnity robot, TrafficManagementService traffic)
        {
            bool onRegG12 = false;
            //kiem tra có robot trong vùng này, nếu có trả về false
            if (traffic.HasOtherRobotUnityinArea("GATE12", robot))
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
            RegIntZone_GATE12.Release(robot);
            return false;
        }

        //Ready -> G3 (C4)
        public static bool Reg_checkinReady_G3(RobotUnity robot, TrafficManagementService traffic)
        {
            bool onRegG12 = false;
            bool onRegElev = false;
            bool onRegG3 = false;
            //kiem tra có robot trong vùng này, nếu có trả về false
            if (traffic.HasOtherRobotUnityinArea("GATE12", robot) || traffic.HasOtherRobotUnityinArea("ELEVATOR", robot) || traffic.HasOtherRobotUnityinArea("GATE3", robot))
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
            RegIntZone_GATE12.Release(robot);
            RegIntZone_ELEVATOR.Release(robot);
            RegIntZone_GATE3.Release(robot);

            return false;
        }
        // Ready -> ELevator, VIM (C5)
        public static bool Reg_checkinReady__ElevatorAndVIM(RobotUnity robot, TrafficManagementService traffic)
        {
            bool onRegG12 = false;
            bool onRegElev = false;
            //kiem tra có robot trong vùng này, nếu có trả về false
            if (traffic.HasOtherRobotUnityinArea("GATE12", robot) || traffic.HasOtherRobotUnityinArea("ELEVATOR", robot))
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
            RegIntZone_GATE12.Release(robot);
            RegIntZone_ELEVATOR.Release(robot);
            return false;
        }
        // Ready -> OUTER
        public static bool Reg_checkinReady_ReadyandOuter(RobotUnity robot, TrafficManagementService traffic)
        {
            bool onRegReady = false;
            //kiem tra có robot trong vùng này, nếu có trả về false
            if (traffic.HasOtherRobotUnityinArea("READY", robot))
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
            RegIntZone_READY.Release(robot);
            return false;
        }

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
            if (traffic.HasOtherRobotUnityinArea("READY", robot) || traffic.HasOtherRobotUnityinArea("GATE12", robot) || traffic.HasOtherRobotUnityinArea("ELEVATOR", robot) || traffic.HasOtherRobotUnityinArea("GATE3", robot))
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
            RegIntZone_READY.Release(robot);
            RegIntZone_GATE12.Release(robot);
            RegIntZone_ELEVATOR.Release(robot);
            RegIntZone_GATE3.Release(robot);
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
            if (traffic.HasOtherRobotUnityinArea("READY", robot) || traffic.HasOtherRobotUnityinArea("GATE12", robot) || traffic.HasOtherRobotUnityinArea("ELEVATOR", robot))
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
            RegIntZone_READY.Release(robot);
            RegIntZone_GATE12.Release(robot);
            RegIntZone_ELEVATOR.Release(robot);

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
            //if (traffic.HasOtherRobotUnityinArea("READY", robot) || traffic.HasOtherRobotUnityinArea("GATE12", robot))
            if (traffic.HasOtherRobotUnityinArea("READY-GATE", robot) )
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
            RegIntZone_READY.Release(robot);
            RegIntZone_GATE12.Release(robot);
            return false;
        }
        // Từ C1 -> Ready
        // Detect no Robot : Ready
        // Check register Ready
        public static bool Reg_checkinC1_Ready(RobotUnity robot, TrafficManagementService traffic)
        {
            bool onRegReady = false;
            //kiem tra có robot trong vùng này, nếu có trả về false
            if (traffic.HasOtherRobotUnityinArea("READY", robot))
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
            RegIntZone_READY.Release(robot);
            return false;
        }

        // Từ C2 -> Gate12
        // Detect no Robot : G12
        // Check register Gate 12
        public static bool Reg_checkinC2_G12(RobotUnity robot, TrafficManagementService traffic)
        {
            bool onRegG12 = false;
            //kiem tra có robot trong vùng này, nếu có trả về false
            if (traffic.HasOtherRobotUnityinArea("GATE12", robot) 
                || traffic.HasOtherRobotUnityinArea("ELEVATOR", robot)
                || traffic.HasOtherRobotUnityinArea("GATE3", robot) 
                || traffic.HasOtherRobotUnityinArea("READY-GATE", robot) 
                || traffic.HasOtherRobotUnityinArea("C1", robot))
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
            RegIntZone_GATE12.Release(robot);
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
            if (traffic.HasOtherRobotUnityinArea("GATE12", robot) || traffic.HasOtherRobotUnityinArea("ELEVATOR", robot) || traffic.HasOtherRobotUnityinArea("GATE3", robot))
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
            RegIntZone_GATE12.Release(robot);
            RegIntZone_ELEVATOR.Release(robot);
            RegIntZone_GATE3.Release(robot);
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
            if (traffic.HasOtherRobotUnityinArea("GATE12", robot) || traffic.HasOtherRobotUnityinArea("ELEVATOR", robot))
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
            RegIntZone_GATE12.Release(robot);
            RegIntZone_ELEVATOR.Release(robot);
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
            if (traffic.HasOtherRobotUnityinArea("READY", robot) || traffic.HasOtherRobotUnityinArea("GATE12", robot))
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
            RegIntZone_GATE12.Release(robot);
            RegIntZone_READY.Release(robot);
            return false;
        }
        public static bool Reg_checkinC2_Ready(RobotUnity robot, TrafficManagementService traffic)
        {
            bool onRegG12 = false;
            bool onRegReady = false;
            Point Rloc = robot.properties.pose.Position;
            //kiem tra có robot trong vùng này, nếu có trả về false
            if (traffic.HasOtherRobotUnityinArea("READY-GATE", robot))
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
            RegIntZone_GATE12.Release(robot);
            RegIntZone_READY.Release(robot);
            return false;
        }

        // Từ Gate12 ( C3) -> VIM and ELEVATOR
        // Detect no Robot : Elevator
        public static bool Reg_checkinGate12_ElevatorAndVIM(RobotUnity robot, TrafficManagementService traffic)
        {
            bool onRegElev = false;
            Point Rloc = robot.properties.pose.Position;
            //kiem tra có robot trong vùng này, nếu có trả về false
            if (traffic.HasOtherRobotUnityinArea("ELEVATOR", robot))
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
            RegIntZone_ELEVATOR.Release(robot);
            return false;
        }
        // Từ Gate12(C3) -> ELEVATOR-> G3
        // Detect no Robot : Elevator ,G3
        public static bool Reg_checkinGate12_Gate3(RobotUnity robot, TrafficManagementService traffic)
        {
            bool onRegElev = false;
            bool onRegG3 = false;
            //kiem tra có robot trong vùng này, nếu có trả về false
            if (traffic.HasOtherRobotUnityinArea("GATE12", robot) || traffic.HasOtherRobotUnityinArea("ELEVATOR", robot))
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
            RegIntZone_ELEVATOR.Release(robot);
            RegIntZone_GATE3.Release(robot);
            return false;
        }

        // Từ Gate12(C3) -> Ready and Outer
        // Detect no Robot : Ready
        public static bool Reg_checkinGate12_ReadyAndOuter(RobotUnity robot, TrafficManagementService traffic)
        {
            bool onRegReady = false;
            //kiem tra có robot trong vùng này, nếu có trả về false
            if (traffic.HasOtherRobotUnityinArea("READY", robot))
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
            RegIntZone_READY.Release(robot);
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
            if (traffic.HasOtherRobotUnityinArea("READY", robot) || traffic.HasOtherRobotUnityinArea("GATE12", robot) || traffic.HasOtherRobotUnityinArea("ELEVATOR", robot))
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
            RegIntZone_READY.Release(robot);
            RegIntZone_GATE12.Release(robot);
            RegIntZone_ELEVATOR.Release(robot);
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
            if (traffic.HasOtherRobotUnityinArea("GATE12", robot) || traffic.HasOtherRobotUnityinArea("ELEVATOR", robot))
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
            RegIntZone_GATE12.Release(robot);
            RegIntZone_ELEVATOR.Release(robot);
            return false;
        }
        // Từ Gate3(C4) -> Elevator And VIM
        // Detect no Robot : ELEVATOR
        public static bool Reg_checkinGate3_ElevatorAndVIM(RobotUnity robot, TrafficManagementService traffic)
        {
            bool onRegElev = false;
            //kiem tra có robot trong vùng này, nếu có trả về false
            if (traffic.HasOtherRobotUnityinArea("ELEVATOR", robot))
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
            RegIntZone_ELEVATOR.Release(robot);
            return false;
        }

        // Từ Elevator(C5) -> Ready and Outer
        // Detect no Robot : ready, GATE12
        public static bool Reg_checkinElevator_ReadyAndOuter(RobotUnity robot, TrafficManagementService traffic)
        {
            bool onRegReady = false;
            bool onRegG12 = false;
            //kiem tra có robot trong vùng này, nếu có trả về false
            if (traffic.HasOtherRobotUnityinArea("READY", robot) || traffic.HasOtherRobotUnityinArea("GATE12", robot))
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
            RegIntZone_READY.Release(robot);
            RegIntZone_GATE12.Release(robot);
            return false;
        }
        // Từ Elevator(C5) -> Gate12
        // Detect no Robot :  GATE12
        public static bool Reg_checkinElevator_Gate12(RobotUnity robot, TrafficManagementService traffic)
        {
            bool onRegG12 = false;
            //kiem tra có robot trong vùng này, nếu có trả về false
            if (traffic.HasOtherRobotUnityinArea("GATE12", robot))
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
            RegIntZone_GATE12.Release(robot);
            return false;
        }

        // Từ Elevator(C5) -> Gate3
        // Detect no Robot : GATE3
        public static bool Reg_checkinElevator_Gate3(RobotUnity robot, TrafficManagementService traffic)
        {
            bool onRegG3 = false;
            //kiem tra có robot trong vùng này, nếu có trả về false
            if (traffic.HasOtherRobotUnityinArea("GATE3", robot))
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
            RegIntZone_GATE3.Release(robot);
            return false;
        }
        public static bool DetectRelease(RegistryRobotJourney rrj)
        {
            // xác định khu vực release
            String destOP = rrj.traffic.DetermineArea(rrj.endPoint, TypeZone.MAIN_ZONE);
            switch (destOP)
            {
                case "OUTER":
                    String startplace = rrj.traffic.DetermineArea(rrj.startPoint, TypeZone.MAIN_ZONE);
                    if (startplace.Equals("VIM"))
                    {
                        // release khi robot vào vùng OUTER
                        if (rrj.traffic.HasRobotUnityinArea("OUTER", rrj.robot))
                        {
                            ReleaseAll(rrj.robot);

                            // rrj.robot.ShowText("RELEASED ROBOT IN REGISTER LIST OF SEPCIAL ZONE FROM VIM -> OUTER");
                            return true;
                        }
                    }
                    break;
                case "VIM":
                    // xác định vùng đến cuối trong VIM.
                    String endPointName = rrj.traffic.DetermineArea(rrj.endPoint, TypeZone.OPZS);
                    if (rrj.traffic.HasRobotUnityinArea(endPointName, rrj.robot))
                    {
                        ReleaseAll(rrj.robot);
                        //  rrj.robot.ShowText("RELEASED ROBOT IN REGISTER LIST OF SEPCIAL ZONE" + endPointName);
                        return true;
                    }
                    break;
            }
            return false;
        }
        public static bool DetetectInsideStationCheck(RegistryRobotJourney rrj)
        {
            // xác định vùng đặt biệt trước khi bắt đầu frontline

            if (rrj.traffic.HasRobotUnityinArea("C1", rrj.robot) || rrj.traffic.HasRobotUnityinArea("C2", rrj.robot)
                || rrj.traffic.HasRobotUnityinArea("C3", rrj.robot) || rrj.traffic.HasRobotUnityinArea("C4", rrj.robot) ||
                rrj.traffic.HasRobotUnityinArea("C5", rrj.robot) || rrj.traffic.HasRobotUnityinArea("READY", rrj.robot))
            {
                // Robot được gửi lệnh Stop
                if (StationCheckInSpecialZone(rrj))
                {
                    rrj.robot.SetSpeedRegZone(RobotSpeedLevel.ROBOT_SPEED_NORMAL, false);
                    return false;
                }
                else
                {
                    rrj.robot.SetSpeedRegZone(RobotSpeedLevel.ROBOT_SPEED_STOP, true);
                    ReleaseAll(rrj.robot);
                    return true;
                }


            }
            else
            {
                // Robot van toc bình thuong
                rrj.robot.SetSpeedRegZone(RobotSpeedLevel.ROBOT_SPEED_NORMAL, false);
                return true;
            }
        }
        public static void delay(int ms)
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();
            while (true)
            {
                if (sw.ElapsedMilliseconds > ms) break;
            }
        }
        public static bool StationCheckInSpecialZone(RegistryRobotJourney rrj)
        {
            // vì "OUTER" có kiều là Main_Zone, nhưng vùng khac co kieu là OPZS
            String startZone = rrj.traffic.DetermineArea(rrj.startPoint, TypeZone.MAIN_ZONE).Equals("OUTER") ? "OUTER" : rrj.traffic.DetermineArea(rrj.startPoint, TypeZone.OPZS);
            String endZone = rrj.traffic.DetermineArea(rrj.endPoint, TypeZone.MAIN_ZONE).Equals("OUTER") ? "OUTER" : rrj.traffic.DetermineArea(rrj.endPoint, TypeZone.OPZS);
            rrj.robot.StartPointName = startZone;
            rrj.robot.EndPointName = endZone;
            #region READY -> GATE12, ELEVATOR, GATE3, VIM
            if (startZone.Equals("READY") && endZone.Equals("GATE12"))
            {
                return Reg_checkinReady_G12(rrj.robot, rrj.traffic);
            }
            if (startZone.Equals("READY") && endZone.Equals("ELEVATOR"))
            {
                return Reg_checkinReady__ElevatorAndVIM(rrj.robot, rrj.traffic);
            }
            if (startZone.Equals("READY") && endZone.Equals("VIM-BTLCAP"))
            {
                return Reg_checkinReady__ElevatorAndVIM(rrj.robot, rrj.traffic);
            }
            if (startZone.Equals("READY") && endZone.Equals("GATE3"))
            {
                return Reg_checkinReady_G3(rrj.robot, rrj.traffic);
            }
            if (startZone.Equals("READY") && endZone.Equals("OUTER"))
            {
                return Reg_checkinReady_ReadyandOuter(rrj.robot, rrj.traffic);
            }
            #endregion

            #region OUTER (C1) -> READY , GATE12, ELEVATOR, GATE3, VIM
            // OUTER->GATE3
            if (startZone.Equals("OUTER") && endZone.Equals("GATE3"))
            {
                return Reg_checkinC1_G3(rrj.robot, rrj.traffic);
            }
            // OUTER->ELEVATOR && VIM
            if (startZone.Equals("OUTER") && endZone.Equals("ELEVATOR"))
            {
                return Reg_checkinC1_ElevatorAndVIM(rrj.robot, rrj.traffic);
            }
            if (startZone.Equals("OUTER") && endZone.Equals("VIM-BTLCAP"))
            {
                return Reg_checkinC1_ElevatorAndVIM(rrj.robot, rrj.traffic);
            }
            // OUTER->GATE12
            if (startZone.Equals("OUTER") && endZone.Equals("GATE12"))
            {
                return Reg_checkinC1_Gate12(rrj.robot, rrj.traffic);
            }
            // OUTER->READY
            if (startZone.Equals("OUTER") && endZone.Equals("READY"))
            {
                return Reg_checkinC1_Ready(rrj.robot, rrj.traffic);
            }
            #endregion
            #region VIM (C2) -> READY , GATE12, ELEVATOR, VIM, GATE3
            if (startZone.Equals("VIM-BTLCAP") && endZone.Equals("GATE12"))
            {
                return Reg_checkinC2_G12(rrj.robot, rrj.traffic);
            }
            // VIM->ELEVATOR && VIM
            if (startZone.Equals("VIM-BTLCAP") && endZone.Equals("ELEVATOR"))
            {
                return Reg_checkinC2_ElevatorAndVIM(rrj.robot, rrj.traffic);
            }
            if (startZone.Equals("VIM-BTLCAP") && endZone.Equals("VIM-BTLCAP"))
            {
                return Reg_checkinC2_ElevatorAndVIM(rrj.robot, rrj.traffic);
            }
            // VIM->READY && OUTER
            if (startZone.Equals("VIM-BTLCAP") && endZone.Equals("READY"))
            {
                return Reg_checkinC2_Ready(rrj.robot, rrj.traffic);
            }
            if (startZone.Equals("VIM-BTLCAP") && endZone.Equals("OUTER"))
            {
                return Reg_checkinC2_Outer(rrj.robot, rrj.traffic);
            }
            if (startZone.Equals("VIM-BTLCAP") && endZone.Equals("GATE3"))
            {
                return Reg_checkinC2_G3(rrj.robot, rrj.traffic);
            }
            #endregion
            #region GATE12 (C3) -> READY, ELEVATOR, VIM , GATE3
            if (startZone.Equals("GATE12") && endZone.Equals("GATE3"))
            {
                return Reg_checkinGate12_Gate3(rrj.robot, rrj.traffic);
            }
            // GATE12->ELEVATOR && VIM
            if (startZone.Equals("GATE12") && endZone.Equals("ELEVATOR"))
            {
                return Reg_checkinGate12_ElevatorAndVIM(rrj.robot, rrj.traffic);
            }
            if (startZone.Equals("GATE12") && endZone.Equals("VIM-BTLCAP"))
            {
                return Reg_checkinGate12_ElevatorAndVIM(rrj.robot, rrj.traffic);
            }
            // GATE12->READY && OUTER
            if (startZone.Equals("GATE12") && endZone.Equals("READY"))
            {
                return Reg_checkinGate12_ReadyAndOuter(rrj.robot, rrj.traffic);
            }
            if (startZone.Equals("GATE12") && endZone.Equals("OUTER"))
            {
                return Reg_checkinGate12_ReadyAndOuter(rrj.robot, rrj.traffic);
            }
            #endregion
            #region GATE3 (C4) -> READY, ELEVATOR, VIM , GATE12
            // GATE3->ELEVATOR && VIM
            if (startZone.Equals("GATE3") && endZone.Equals("ELEVATOR"))
            {
                return Reg_checkinGate3_ElevatorAndVIM(rrj.robot, rrj.traffic);
            }
            if (startZone.Equals("GATE3") && endZone.Equals("VIM-BTLCAP"))
            {
                return Reg_checkinGate3_ElevatorAndVIM(rrj.robot, rrj.traffic);
            }
            // GATE3->READY && OUTER
            if (startZone.Equals("GATE3") && endZone.Equals("READY"))
            {
                return Reg_checkinGate3_ReadyAndOuter(rrj.robot, rrj.traffic);
            }
            if (startZone.Equals("GATE3") && endZone.Equals("OUTER"))
            {
                return Reg_checkinGate3_ReadyAndOuter(rrj.robot, rrj.traffic);
            }
            if (startZone.Equals("GATE3") && endZone.Equals("GATE12"))
            {
                return Reg_checkinGate3_Gate12(rrj.robot, rrj.traffic);
            }
            #endregion
            #region ELEVATOR (C5) -> READY, GATE12, GATE3
            if (startZone.Equals("ELEVATOR") && endZone.Equals("GATE12"))
            {
                return Reg_checkinElevator_Gate12(rrj.robot, rrj.traffic);
            }
            //  ELEVATOR->READY && OUTER
            if (startZone.Equals("ELEVATOR") && endZone.Equals("READY"))
            {
                return Reg_checkinElevator_ReadyAndOuter(rrj.robot, rrj.traffic);
            }
            if (startZone.Equals("ELEVATOR") && endZone.Equals("OUTER"))
            {
                return Reg_checkinElevator_ReadyAndOuter(rrj.robot, rrj.traffic);
            }
            if (startZone.Equals("ELEVATOR") && endZone.Equals("GATE3"))
            {
                return Reg_checkinElevator_Gate3(rrj.robot, rrj.traffic);
            }
            #endregion

            return false;
        }
        public static void ReleaseAll(RobotUnity robot)
        {
            RegIntZone_READY.Release(robot);
            RegIntZone_GATE12.Release(robot);
            RegIntZone_GATE3.Release(robot);
            RegIntZone_ELEVATOR.Release(robot);
        }
        public static void ReleaseAll(RobotUnity robot,String nameException)
        {
            RegIntZone_READY.Release(robot);
            RegIntZone_GATE12.Release(robot);
            RegIntZone_GATE3.Release(robot);
            RegIntZone_ELEVATOR.Release(robot);
        }

    }
}

﻿using SeldatMRMS.Management.RobotManagent;
using SeldatMRMS.Management.TrafficManager;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using static SelDatUnilever_Ver1._00.Management.TrafficManager.TrafficRounterService;

namespace SeldatMRMS.Management
{
    public class TrafficRobotUnity : RobotUnityService
    {
        public enum RobotBahaviorAtAnyPlace
        {
            ROBOT_PLACE_ROAD,
            ROBOT_PLACE_HIGHWAY,
            ROBOT_PLACE_BUFFER,
            ROBOT_PLACE_HIGHWAY_DETECTLINE,
            ROBOT_PLACE_HIGHWAY_READY,
            ROBOT_PLACE_IDLE
        }
        public class PriorityLevel
        {
            public PriorityLevel()
            {
                this.IndexOnMainRoad = 0;
                this.OnAuthorizedPriorityProcedure = false;
            }
            public int IndexOnMainRoad { get; set; } //  Index on Road;
            public bool OnAuthorizedPriorityProcedure { get; set; }

        }
        public enum TrafficBehaviorState
        {
            HEADER_TOUCH_TAIL,
            HEADER_TOUCH_HEADER,
            HEADER_TOUCH_SIDE,
            HEADER_TOUCH_NOTOUCH,
            MODE_FREE,
            SLOW_DOWN,
            NORMAL_SPEED
        }
        public enum RobotStatus
        {
            IDLE,
            WORKING,
            READYGO,
            CHARGING,
            READY

        }
        public class RobotRegistryToWorkingZone
        {
            public String WorkingZone;
            public bool onRobotGoingInsideZone = false;
            public bool onRobotwillCheckInsideGate = false;
            public RobotRegistryToWorkingZone()
            {
                WorkingZone = "";
                onRobotGoingInsideZone = false;
            }
            public void Release()
            {
                WorkingZone = "";
                onRobotGoingInsideZone = false;
            }
            public void SetZone(String namez)
            {
                WorkingZone = namez;
                onRobotGoingInsideZone = true;
            }
        }
        // public enum MvDirection{
        //     INCREASE_X = 0,
        // 	INCREASE_Y,
        // 	DECREASE_X,
        // 	DECREASE_Y
        // // }
        public enum BrDirection
        {
            FORWARD = 0,
            DIR_LEFT,
            DIR_RIGHT
        }
        // public class PointDetect {
        //     public Point p;
        //     public MvDirection mvDir;
        //     public PointDetect(Point p, MvDirection mv)
        //     {
        //         this.p = p;
        //         mvDir = mv;
        //     }
        // }

        // public class PointDetectBranching{
        //     public PointDetect xy;
        //     public BrDirection brDir;
        // }
        public enum PistonPalletCtrl
        {
            PISTON_PALLET_UP = 0,
            PISTON_PALLET_DOWN
        }
        public class JInfoPallet
        {
            public PistonPalletCtrl pallet;
            public BrDirection dir_main;
            public Int32 bay;
            public String hasSubLine;
            public BrDirection dir_sub;
            public BrDirection dir_out;
            public int line_ord;
            public Int32 row;
            // public Int32 palletId;
        }
        private List<RobotUnity> RobotUnitylist;
        public bool onFlagSupervisorTraffic;
        public bool onFlagSelfTraffic;
        public bool onFlagSafeYellowcircle = false;
        public bool onFlagSafeBluecircle = false;
        public bool onFlagSafeSmallcircle = false;
        public bool onFlagSafeOrgancircle = false;

        private Dictionary<String, RobotUnity> RobotUnityRiskList = new Dictionary<string, RobotUnity>();
        private TrafficBehaviorState TrafficBehaviorStateTracking;
        protected TrafficManagementService trafficManagementService;
        private RobotUnity robotModeFree;
        private const double DistanceToSetSlowDown = 80; // sau khi dừng robot phai doi khoan cach len duoc tren 8m thi robot bat dau hoat dong lai bình thuong 8m
        private const double DistanceToSetNormalSpeed = 12; // sau khi dừng robot phai doi khoan cach len duoc tren 8m thi robot bat dau hoat dong lai bình thuong 12m
        public RobotRegistryToWorkingZone robotRegistryToWorkingZone;
        public RobotStatus robotTag;
        public String STATE_SPEED = "";
        public TrafficRobotUnity() : base()
        {
            TurnOnSupervisorTraffic(false);
        
            RobotUnitylist = new List<RobotUnity>();
            prioritLevel = new PriorityLevel();
            robotRegistryToWorkingZone = new RobotRegistryToWorkingZone();
          robotTag = RobotStatus.IDLE;
          //  robotTag = RobotStatus.WORKING;
            onFlagReadyGo = false;


        }
    public void StartTraffic()
        {
            new Thread(TrafficUpdate).Start();

        }
        public PriorityLevel prioritLevel;
        public void RegisteRobotInAvailable(Dictionary<String, RobotUnity> RobotUnitylistdc)
        {
            foreach (var r in RobotUnitylistdc.Values)
            {
                if (!r.properties.NameId.Equals(this.properties.NameId))
                    this.RobotUnitylist.Add(r);
            }
            TrafficBehaviorStateTracking = TrafficBehaviorState.HEADER_TOUCH_NOTOUCH;
        }
        public void Registry(TrafficManagementService trafficManagementService)
        {
            this.trafficManagementService = trafficManagementService;
        }
        public bool CheckIntersection(bool turnon, bool sameindexRoad=false)
        {
            bool onstop = false;
            if (turnon)
            {
                if (RobotUnitylist.Count > 0)
                {
                    foreach (RobotUnity r in RobotUnitylist)
                    {
                        String lableR = r.properties.Label;
                        if (r.robotTag==RobotStatus.WORKING)
                        {
                           
                            Point thCV = TopHeaderCv();
                            Point mdCV0 = MiddleHeaderCv();
                            Point mdCV1 = MiddleHeaderCv1();
                            Point mdCV2 = MiddleHeaderCv2();
                            Point mdCV3 = MiddleHeaderCv3();
                            Point bhCV = BottomHeaderCv();
                            Point Rp = Global_Object.CoorCanvas(this.properties.pose.Position);
                            // bool onTouch= FindHeaderIntersectsFullRiskArea(this.TopHeader()) | FindHeaderIntersectsFullRiskArea(this.MiddleHeader()) | FindHeaderIntersectsFullRiskArea(this.BottomHeader());
                            // bool onTouch = r.FindHeaderIntersectsFullRiskAreaCv(thCV) | r.FindHeaderIntersectsFullRiskAreaCv(mdCV) | r.FindHeaderIntersectsFullRiskAreaCv(bhCV);
                            bool onTouchR = r.FindHeaderInsideCircleArea(Rp, r.Radius_S);
                            bool onTouch0 = r.FindHeaderInsideCircleArea(mdCV0,  r.Radius_S);
                            bool onTouch1 = r.FindHeaderInsideCircleArea(mdCV1,  r.Radius_S);
                            bool onTouch2 = r.FindHeaderInsideCircleArea(mdCV2,  r.Radius_S);
                            bool onTouch3= r.FindHeaderInsideCircleArea(mdCV3, r.Radius_S);

                            if (onTouchR || onTouch1 || onTouch2)
                            {
                                //  robotLogOut.ShowTextTraffic(r.properties.Label+" => CheckIntersection");   
                                    if (r.onFlagSafeBluecircle)
                                    {
                                       /* STATE_SPEED = "CHECKINT_WORKING_SECTION_NORMAL [FLAG] " + r.properties.Label;
                                        SetSpeed(RobotSpeedLevel.ROBOT_SPEED_NORMAL*/
                                    }
                                    else
                                    {
                                        STATE_SPEED = "CHECKINT_WORKING_SECTION_STOP " + r.properties.Label;
                                        SetSpeed(RobotSpeedLevel.ROBOT_SPEED_STOP);
                                        delay(5000);
                                        onstop = true;
                                    }
                                break;
                            }
                            else if (onTouch0)
                            {
                                //  robotLogOut.ShowTextTraffic(r.properties.Label+" => CheckIntersection");
                                STATE_SPEED = "CHECKINT_WORKING_SECTION_SLOW " + r.properties.Label;
                                SetSpeed(RobotSpeedLevel.ROBOT_SPEED_STOP);
                                onstop = true;
                                delay(5000);
                                break;

                            }
                          /*  else
                            {
                                STATE_SPEED = "CHECKINT_WORKING_SECTION_NORMAL ";
                                SetSpeed(RobotSpeedLevel.ROBOT_SPEED_NORMAL);
                            }*/
                        }
                      /*  else
                        {
                            STATE_SPEED = "CHECKINT_IDLE_SECTION_NORMAL ";
                            SetSpeed(RobotSpeedLevel.ROBOT_SPEED_NORMAL);
                        }*/
                    }
                }
            }
            return onstop;
           
        }

        public bool CheckInBuffer(bool turnon)
        {
            bool onstop = false;
            if (turnon)
            {
                if (RobotUnitylist.Count > 0)
                {
                    foreach (RobotUnity r in RobotUnitylist)
                    {
                        String lableR = r.properties.Label;
                        if (r.robotTag == RobotStatus.WORKING)
                        {

                            Point thCV = TopHeaderCv();
                            Point mdCV0 = MiddleHeaderCv();
                            Point mdCV1 = MiddleHeaderCv1();
                            Point mdCV2 = MiddleHeaderCv2();
                            Point mdCV3 = MiddleHeaderCv3();
                            Point bhCV = BottomHeaderCv();
                            Point Rp = Global_Object.CoorCanvas(this.properties.pose.Position);
                            // bool onTouch= FindHeaderIntersectsFullRiskArea(this.TopHeader()) | FindHeaderIntersectsFullRiskArea(this.MiddleHeader()) | FindHeaderIntersectsFullRiskArea(this.BottomHeader());
                            // bool onTouch = r.FindHeaderIntersectsFullRiskAreaCv(thCV) | r.FindHeaderIntersectsFullRiskAreaCv(mdCV) | r.FindHeaderIntersectsFullRiskAreaCv(bhCV);
                            bool onTouchR = r.FindHeaderInsideCircleArea(Rp, r.Radius_S);
                            bool onTouch0 = r.FindHeaderInsideCircleArea(mdCV0, r.Radius_S);
                            bool onTouch1 = r.FindHeaderInsideCircleArea(mdCV1, r.Radius_S);
                            bool onTouch2 = r.FindHeaderInsideCircleArea(mdCV2, r.Radius_S);
                            bool onTouch3 = r.FindHeaderInsideCircleArea(mdCV3, r.Radius_S);

                            if (onTouch3)
                            {
                                //  robotLogOut.ShowTextTraffic(r.properties.Label+" => CheckIntersection");   
                                if (r.onFlagSafeSmallcircle || r.onFlagSafeYellowcircle || r.onFlagSafeOrgancircle)
                                {
                                    STATE_SPEED = "CHECKINT_BUFFER_SECTION_STOP " + r.properties.Label;
                                    SetSpeed(RobotSpeedLevel.ROBOT_SPEED_STOP);
                                    delay(5000);
                                    onstop = true;
                                    break;
                                }
                                
                            }
                        }
                    }
                }
            }
            return onstop;

        }
        public void delay(int ms)
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();
            while (true)
            {
                if (sw.ElapsedMilliseconds > ms) break;
            }
        }
        public int CheckSafeDistance() // KIểm tra khoản cách an toàn/ nếu đang trong vùng close với robot khác thì giảm tốc độ, chuyển sang chế độ dò risk area
        {
            int iscloseDistance = 0;
            foreach (RobotUnity r in RobotUnitylist)
            {
                if (r.onFlagSupervisorTraffic)
                {
                    Point rP = MiddleHeaderCv();
                    // bool onFound = r.FindHeaderIsCloseRiskArea(this.properties.pose.Position);
                    bool onFound = r.FindHeaderIsCloseRiskAreaCv(rP);

                    if (onFound)
                    {
                        // if robot in list is near but add in risk list robot
                        //     robotLogOut.ShowTextTraffic(r.properties.Label + "- Intersection");

                        if (!RobotUnityRiskList.ContainsKey(r.properties.NameId))
                        {
                            RobotUnityRiskList.Add(r.properties.NameId, r);
                        }
                        // reduce speed robot control
                        iscloseDistance = 2;
                    }
                    else
                    {
                        // if robot in list is far but before registe in list, must remove in list
                        RemoveRiskList(r.properties.NameId);
                        double rd = ExtensionService.CalDistance(Global_Object.CoorCanvas(this.properties.pose.Position), Global_Object.CoorCanvas(r.properties.pose.Position));
                        if (rd < DistanceToSetSlowDown && rd > 60)
                            iscloseDistance = 1;
                        else
                            iscloseDistance = 0;

                    }
                }
            }
            return iscloseDistance;
        }
        public void RemoveRiskList(String NameID)
        {
            if (RobotUnityRiskList.ContainsKey(NameID))
            {
                RobotUnityRiskList.Remove(NameID);
            }
        }
        public void TurnOnSupervisorTraffic(bool onflagtraffic)
        {
            onFlagSupervisorTraffic = onflagtraffic;
            SetSafeSmallcircle(onflagtraffic);
            if (!onflagtraffic)
            {
                properties.L1 = 0;
                properties.L2 = 0;
                properties.WS = 0;
                properties.DistInter = 0;


                L1Cv = 0;
                L2Cv = 0;
                WSCv = 0;
                DistInterCv = 0;

                Radius_S = 0;
                Radius_B = 0;
                Radius_Y = 0;
                Radius_O= 0;
                onFlagSafeSmallcircle = false;

            }
        }
        public void TrafficUpdate()
        {
            while (true)
            {
                try
                {
                    prioritLevel.IndexOnMainRoad = trafficManagementService.FindIndexZoneRegister(properties.pose.Position);
                    if (onFlagSupervisorTraffic)
                    {

                        // cập nhật vùng riskzone // update vùng risk area cho robot
                        ZoneRegister rZR = trafficManagementService.Find(properties.pose.Position, 0, 200);
                        if (rZR != null)
                        {
                            properties.L1 = rZR.L1;
                            properties.L2 = rZR.L2;
                            properties.WS = rZR.WS;
                            properties.DistInter = rZR.distance;


                            L1Cv = rZR.L1 * properties.Scale;
                            L2Cv = rZR.L2 * properties.Scale;
                            WSCv = rZR.WS * properties.Scale;
                            DistInterCv = rZR.distance * properties.Scale;
                            //UpdateRiskAraParams(rZR.L1, rZR.L2, rZR.WS, rZR.distance);
                        }
                        else
                        {
                            UpdateRiskAraParams(DfL1, DfL2, DfWS, DfDistanceInter);
                        }
                         RobotBehavior();
                    }
                    // giám sát an toàn

                }
                catch { Console.WriteLine("TrafficRobotUnity Error in TrafficUpdate"); }
                Thread.Sleep(400);
            }

        }
        // Finding has any Robot in Zone that Robot is going to come
        public bool FindRobotInWorkingZone(Point anyPoint)
        {
            bool hasRobot = false;
            String nameZone = trafficManagementService.DetermineArea(anyPoint, 0, 200);
            if (nameZone != "")
            {
                foreach (RobotUnity r in RobotUnitylist)
                {
                    if (r.robotRegistryToWorkingZone.WorkingZone.Equals(nameZone))
                    {
                        hasRobot = true;
                        break;
                    }
                }
            }
            return hasRobot;
        }
        public RobotUnity DetermineRobotInWorkingZone(Point anyPoint)
        {
            RobotUnity robot = null;
            String nameZone = trafficManagementService.DetermineArea(anyPoint, 0, 200);
            if (nameZone != "")
            {
                foreach (RobotUnity r in RobotUnitylist)
                {
                    if (r.robotRegistryToWorkingZone.WorkingZone.Equals(nameZone))
                    {
                        robot = r;
                        break;
                    }
                }
            }
            return robot;
        }
        public bool DetermineRobotInWorkingZone(Point anyPoint, bool check = false)
        {
            String nameZone = trafficManagementService.DetermineArea(anyPoint, 0, 200);
            if (nameZone != "")
            {
                foreach (RobotUnity r in RobotUnitylist)
                {
                    if (r.robotRegistryToWorkingZone.WorkingZone.Equals(nameZone))
                    {
                        return true;
                    
                    }
                }
            }
            return false;
        }
        // set zonename Robot will working
        public void SetWorkingZone(String nameZone)
        {
            robotLogOut.ShowText("","Zone Registered : "+ nameZone);
            robotRegistryToWorkingZone.SetZone(nameZone);
        }
        // release zonename Robot out
        public void ReleaseWorkingZone()
        {
            //robotLogOut.ShowText("", "Zone Released : ");
            robotRegistryToWorkingZone.Release();
        }
        // ứng xử tai check in zone với bắt vị trí anypoint
        public bool CheckInZoneBehavior(Point anyPoint)
        {
            if (anyPoint == null)
                return true; // un available
            if (FindRobotInWorkingZone(anyPoint))
                return true;
            else
            {
                String nameZone = trafficManagementService.DetermineArea(anyPoint, 0, 200);
                if (nameZone != "")
                {
                    SetWorkingZone(nameZone);
                    return false; // available
                }
                return true;
            }
        }
        public bool CheckInGateFromReadyZoneBehavior(Point anyPoint)
        {
            if (anyPoint == null)
                return true; // un available
            bool hasrobot = DetermineRobotInWorkingZone(anyPoint,false);
            if (hasrobot)
            {
                return true;
            }
            else
            {
                String nameZone = trafficManagementService.DetermineArea(anyPoint, 0, 200);
                if (nameZone != "")
                {
                    SetWorkingZone(nameZone);
                    return false; // available
                }

            }
            return true;
        }
        public bool CheckRobotWorkinginReady()
        {
            bool hasRobotWorking = false;
            foreach (RobotUnity robot in RobotUnitylist)
            {
                if (trafficManagementService.GetTypeZone(robot.properties.pose.Position, 0, 200) == TypeZone.READY)
                {
                    if (robot.robotTag == RobotStatus.WORKING)
                    {
                        hasRobotWorking = true;
                        break;
                    }
                }
            }
            return hasRobotWorking;
        }
        // kiểm tra vong tròn an toàn quyết định điều khiển giao thông theo nguyên tắt
        // + vòng tròn nhỏ an toàn được mở on trong trường hợp robot nằm trên đường chính
        // + vòng tròn xanh tượng trưng xin vào đường chính từ đường nhỏ giao với đường chính, nếu có robot nào xuất hiện trong vòng tròn nhỏ robot vẽ vòng tròn xnh phải đứng lại ưu tiên cho robot khác làm việc
        // + vòng tròn vàng mức ưu tiên cai nhất, nó xuất hiện và được vẻ ra khi robot trong khu vực đường lớn giao và đang làm nhiệm vụ dò line. robot nào xu61t hiện trong vòng tròn này buột phải ngưng mọi hoạt động

        public String TyprPlaceStr = "";
        public void RobotBehavior()
        {
            
            RobotBahaviorAtAnyPlace robotBahaviorAtAnyPlace = RobotBahaviorAtAnyPlace.ROBOT_PLACE_IDLE;
            TypeZone _type = trafficManagementService.GetTypeZone(properties.pose.Position, 0, 200);
            TyprPlaceStr = _type + "";
            //onFlagDetectLine = true;
            if (_type== TypeZone.READY)
            {
                robotBahaviorAtAnyPlace = RobotBahaviorAtAnyPlace.ROBOT_PLACE_HIGHWAY;
                //SetSpeed(RobotSpeedLevel.ROBOT_SPEED_NORMAL);
            }
            if (_type == TypeZone.HIGHWAY && onFlagDetectLine == false)
            {
                robotBahaviorAtAnyPlace = RobotBahaviorAtAnyPlace.ROBOT_PLACE_HIGHWAY;
            }
            if (_type == TypeZone.HIGHWAY && onFlagDetectLine == true)
            {
                robotBahaviorAtAnyPlace = RobotBahaviorAtAnyPlace.ROBOT_PLACE_HIGHWAY_DETECTLINE; ;
            }
            if (_type == TypeZone.ROAD)
            {
                robotBahaviorAtAnyPlace = RobotBahaviorAtAnyPlace.ROBOT_PLACE_ROAD;
            }
            if (_type == TypeZone.BUFFER)
            {
                robotBahaviorAtAnyPlace = RobotBahaviorAtAnyPlace.ROBOT_PLACE_BUFFER;
            }
            switch (robotBahaviorAtAnyPlace)
            {
                case RobotBahaviorAtAnyPlace.ROBOT_PLACE_IDLE:
                    SetSafeOrgancircle(false);
                    SetSafeSmallcircle(true);
                    SetSafeBluecircle(false);
                    SetSafeYellowcircle(false);
                    if (CheckIntersection(true))
                        break;
                    else
                    {
                        STATE_SPEED = "ROBOT_PLACE_IDLE_NORMAL ";
                        SetSpeed(RobotSpeedLevel.ROBOT_SPEED_NORMAL);
                    }
                    break;
                case RobotBahaviorAtAnyPlace.ROBOT_PLACE_HIGHWAY:
                    if (!CheckYellowCircle())
                    {
                        // mở vòng tròn nhỏ vá kiểm tra va chạm
                        SetSafeOrgancircle(false);
                        SetSafeSmallcircle(true);
                        SetSafeBluecircle(false);
                        SetSafeYellowcircle(false);
                        if (checkOrgancCircle())
                            break;
                        else if (CheckIntersection(true))
                            break;
                        else
                        {
                            STATE_SPEED = "ROBOT_PLACE_HIGHWAY_NORMAL ";
                            SetSpeed(RobotSpeedLevel.ROBOT_SPEED_NORMAL);
                        }
                    }
                    break;
                case RobotBahaviorAtAnyPlace.ROBOT_PLACE_ROAD:
                    // kiem tra vong tròn xanh
                    SetSafeSmallcircle(false);
                    SetSafeOrgancircle(true);
                    SetSafeBluecircle(true);
                    if (CheckBlueCircle())
                        break;
                    if (CheckYellowCircle())
                        break;
                    else
                    {
                        STATE_SPEED = "ROBOT_PLACE_ROAD_NORMAL ";
                        SetSpeed(RobotSpeedLevel.ROBOT_SPEED_NORMAL);
                    }
                    break;
                case RobotBahaviorAtAnyPlace.ROBOT_PLACE_HIGHWAY_DETECTLINE:
                    // SetSafeSmallcircle(true);
                    SetSafeOrgancircle(true);
                    SetSafeBluecircle(false);
                    SetSafeYellowcircle(true);
                    SetSpeed(RobotSpeedLevel.ROBOT_SPEED_NORMAL);
                    break;
                case RobotBahaviorAtAnyPlace.ROBOT_PLACE_BUFFER:
                    SetSafeOrgancircle(false);
                    SetSafeSmallcircle(false);
                    SetSafeBluecircle(false);
                    SetSafeYellowcircle(false);
                    if (CheckInBuffer(true))
                        break;
                    else
                    {
                        STATE_SPEED = "ROBOT_PLACE_BUFFER_NORMAL ";
                        SetSpeed(RobotSpeedLevel.ROBOT_SPEED_NORMAL);
                    }
                    // tắt vòng tròn nhỏ
                    break;
            }
        }

      
        public bool checkOrgancCircle()
        {
            bool onstop = false;
            foreach (RobotUnity r in RobotUnitylist)
            {
                // kiểm tra có robot chinh  nó có nằm trong vòng tròn vàng nào không nếu có ngưng\
                if (r.properties.Label.Equals(this.properties.Label)) continue;
                if (r.onFlagSafeOrgancircle)
                {
                    Point cc = r.CenterOnLineCv(-10);
                    //Point cc = Global_Object.CoorCanvas(properties.pose.Position);
                    String robot = properties.Label;
                    String robot2 = r.properties.Label;
                    Point md = MiddleHeaderCv();
                    Point md1 = MiddleHeaderCv1();
                    Point md2 = MiddleHeaderCv2();
                    Point md3 = MiddleHeaderCv3();
                    if (FindHeaderInsideCircleArea(md,cc,  r.Radius_O) || FindHeaderInsideCircleArea(md1, cc, r.Radius_O) || FindHeaderInsideCircleArea(md2, cc, r.Radius_O))
                    {
                        STATE_SPEED = "ORGANC_STOP";
                        SetSpeed(RobotSpeedLevel.ROBOT_SPEED_STOP);
                        delay(5000);
                        onstop = true;
                        break;
                    }
                }
              /*  else
                {
                    STATE_SPEED = "ORGANC_NORMAL";
                    SetSpeed(RobotSpeedLevel.ROBOT_SPEED_NORMAL);
                }*/
            }
            return onstop;
        }
        public bool CheckBlueCircle() // khi robot bặt vòng tròn xanh. chính nó phải ngưng nếu dò ra có robot nào trong vùng vòng tròn này ngược lại với vòng tròn vàng
        {
            bool onStop = false;
            foreach (RobotUnity r in RobotUnitylist)
            {
                if (r.prioritLevel.IndexOnMainRoad == prioritLevel.IndexOnMainRoad)
                {
                    if (checkOrgancCircle())
                    {
                        onStop = true;
                        break;
                    }
                }
                else
                {
                    // kiểm tra có robot nào nằm trong vòng tròn và trạng thái đang làm việc an toàn này kg?
                    Point cB = CenterOnLineCv(Center_B); // TRONG TAM CUA NO
                    if(r.robotTag==RobotStatus.WORKING)
                    {
                         if (FindHeaderInsideCircleArea(r.MiddleHeaderCv(), cB, Radius_B) || FindHeaderInsideCircleArea(Global_Object.CoorCanvas(r.properties.pose.Position), cB, Radius_B))
                        {
                            STATE_SPEED = "BLUEC_STOP "+ r.properties.Label;
                            SetSpeed(RobotSpeedLevel.ROBOT_SPEED_STOP);
                            delay(5000);
                            onStop = true;
                            break;
                        }
                       /* else
                        {
                            STATE_SPEED = "BLUEC_WORKING_NORMAL ";
                            SetSpeed(RobotSpeedLevel.ROBOT_SPEED_NORMAL);
                        }*/

                    }
                   /* else
                    {
                        STATE_SPEED = "BLUEC_IDLE_NORMAL ";
                        SetSpeed(RobotSpeedLevel.ROBOT_SPEED_NORMAL);
                    }*/
                }
            }
            return onStop;
        }
       

        public bool CheckYellowCircle() // khi robot bặt vòng tròn vàng. tất cả robot khác ngưng nếu dò ra có robot nào trong vùng vòng tròn này
        {
            bool onstop = false;
            foreach (RobotUnity r in RobotUnitylist)
            {
                // kiểm tra có robot chinh  nó có nằm trong vòng tròn vàng nào không nếu có ngưng
                if (r.onFlagSafeYellowcircle)
                {
                    Point cY = r.CenterOnLineCv(Center_Y); // TRONG TAM ROBOT KHAC
                    if (r.FindHeaderInsideCircleArea(MiddleHeaderCv(), cY, r.Radius_Y) )
                    {
                        STATE_SPEED = "YELLOWC_STOP";
                        SetSpeed(RobotSpeedLevel.ROBOT_SPEED_STOP);
                        delay(5000);
                        onstop = true;
                        break;
                    }
                 /*   else
                    {
                        STATE_SPEED = "YELLOWC_NO_NORMAL";
                        SetSpeed(RobotSpeedLevel.ROBOT_SPEED_NORMAL);
                    }*/
                }
            }
            return onstop;
        }
        public void SetSafeYellowcircle(bool flagonoff)
        {
            if (flagonoff)
                Radius_Y = 40;
            else
                Radius_Y = 0;
            onFlagSafeYellowcircle = flagonoff;
        }
        public void SetSafeBluecircle(bool flagonoff)
        {
            if (flagonoff)
                Radius_B = 40;
            else
                Radius_B = 0;
            onFlagSafeBluecircle = flagonoff;
        }
        public void SetSafeSmallcircle(bool flagonoff)
        {
            if (flagonoff)
                Radius_S = 30;
            else
                Radius_S = 0;
            onFlagSafeSmallcircle = flagonoff;
        }

        public void SetSafeOrgancircle(bool flagonoff)
        {
            if (flagonoff)
                Radius_O = 25;
            else
                Radius_O = 0;
            onFlagSafeOrgancircle = flagonoff;
        }
        public void SwitchToDetectLine(bool flagonoff)
        {
            onFlagDetectLine = flagonoff;
        }
    }
}

﻿using SeldatMRMS;
using SeldatMRMS.Management.RobotManagent;
using SeldatUnilever_Ver1._02.Management.TrafficManager;
using SelDatUnilever_Ver1._00.Management.DeviceManagement;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using static DoorControllerService.DoorService;
using static SeldatMRMS.Management.RobotManagent.RobotManagementService;
using static SeldatMRMS.RegisterProcedureService;
using static SelDatUnilever_Ver1._00.Management.DeviceManagement.DeviceItem;

namespace SelDatUnilever_Ver1._00.Management.UnityService
{
    public class AssigmentTaskService : TaskRounterService
    {

        public AssigmentTaskService() { }
        public void FinishTask(String userName)
        {
            var item = deviceItemsList.Find(e => e.userName == userName);
            item.RemoveFirstOrder();
        }
        public void Start()
        {
            Alive = true;
            processAssignAnTaskWait = ProcessAssignAnTaskWait.PROC_ANY_GET_ANROBOT_IN_WAITTASKLIST;
            processAssignTaskReady = ProcessAssignTaskReady.PROC_READY_GET_ANROBOT_INREADYLIST;
            Thread threadprocessAssignAnTaskWait = new Thread(MainProcessAssignTask);
            threadprocessAssignAnTaskWait.Start();
      
        }
        public void Dispose()
        {
            Alive = false;
        }
        public void Stop()
        {
            Alive = false;
        }
        public void MainProcessAssignTask()
        {
            while(Alive)
            {
                OrderItem order = Gettask();
                AssignWaitTask(order);
                AssignTaskAtReady(order);
                Thread.Sleep(200);
            }
        }

        public void AssignWaitTask(OrderItem order)
        {
            processAssignAnTaskWait = ProcessAssignAnTaskWait.PROC_ANY_GET_ANROBOT_IN_WAITTASKLIST;
            OrderItem orderItem_wait = null;
            RobotUnity robotwait = null;
            while (true)
            {
                switch (processAssignAnTaskWait)
                {
                    case ProcessAssignAnTaskWait.PROC_ANY_IDLE:
                        break;
                    case ProcessAssignAnTaskWait.PROC_ANY_GET_ANROBOT_IN_WAITTASKLIST:
                        if (robotManageService.RobotUnityWaitTaskList.Count > 0)
                        {
                            ResultRobotReady result = robotManageService.GetRobotUnityWaitTaskItem0();
                            if (result != null)
                            {
                                robotwait = result.robot;
                                if (result.onReristryCharge)
                                {
                                    procedureService.Register(ProcedureItemSelected.PROCEDURE_ROBOT_TO_READY, robotwait, null);
                                    robotManageService.RemoveRobotUnityWaitTaskList(robotwait);
                                }
                                else
                                {
                                    if (order != null)
                                    {
                                        processAssignAnTaskWait = ProcessAssignAnTaskWait.PROC_ANY_CHECK_HAS_ANTASK;
                                        break;
                                    }
                                    else
                                    {
                                        processAssignAnTaskWait = ProcessAssignAnTaskWait.PROC_ANY_CHECK_ROBOT_GOTO_READY; // mở lại 
                                        break;
                                    }

                                }
                            }
                        }
                        return;
                    case ProcessAssignAnTaskWait.PROC_ANY_CHECK_HAS_ANTASK:
                        orderItem_wait = order;
                        orderItem_wait.onAssiged = true;
                        processAssignAnTaskWait = ProcessAssignAnTaskWait.PROC_ANY_ASSIGN_ANTASK;
                        orderItem_wait.robot = robotwait.properties.Label;
                        robotwait.orderItem = orderItem_wait;
                        MoveElementToEnd();
                        break;
                    case ProcessAssignAnTaskWait.PROC_ANY_CHECK_ROBOT_GOTO_READY:
                        robotwait.TurnOnSupervisorTraffic(true);
                        procedureService.Register(ProcedureItemSelected.PROCEDURE_ROBOT_TO_READY, robotwait, null);
                        robotManageService.RemoveRobotUnityWaitTaskList(robotwait);
                        return;
                    case ProcessAssignAnTaskWait.PROC_ANY_ASSIGN_ANTASK:
                        robotwait.TurnOnSupervisorTraffic(true);
                        SelectProcedureItem(robotwait, orderItem_wait);
                        deviceItemsList[0].RemoveFirstOrder();
                        robotManageService.RemoveRobotUnityWaitTaskList(robotwait);
                        processAssignAnTaskWait = ProcessAssignAnTaskWait.PROC_ANY_GET_ANROBOT_IN_WAITTASKLIST;
                        orderItem_wait.status = StatusOrderResponseCode.DELIVERING;
                        return;

                }
                Thread.Sleep(100);
            }
        }
        public void SelectProcedureItem(RobotUnity robot, OrderItem orderItem)
        {
            if (orderItem.typeReq == DeviceItem.TyeRequest.TYPEREQUEST_FORLIFT_TO_BUFFER)
            {
                procedureService.Register(ProcedureItemSelected.PROCEDURE_FORLIFT_TO_BUFFER, robot, orderItem); //yes
            }
            else if (orderItem.typeReq == DeviceItem.TyeRequest.TYPEREQUEST_BUFFER_TO_MACHINE)
            {
                procedureService.Register(ProcedureItemSelected.PROCEDURE_BUFFER_TO_MACHINE, robot, orderItem); // yes
            }
            else if (orderItem.typeReq == DeviceItem.TyeRequest.TYPEREQUEST_PALLET_EMPTY_MACHINE_TO_RETURN) // yes
            {
                procedureService.Register(ProcedureItemSelected.PROCEDURE_PALLETEMPTY_MACHINE_TO_RETURN, robot, orderItem);
            }
            else if (orderItem.typeReq == DeviceItem.TyeRequest.TYPEREQUEST_WMS_RETURN_PALLET_BUFFER_TO_GATE)  // yes
            {
                procedureService.Register(ProcedureItemSelected.PROCEDURE_BUFFER_TO_GATE, robot, orderItem);
            }
            else if (orderItem.typeReq == DeviceItem.TyeRequest.TYPEREQUEST_FORLIFT_TO_MACHINE)
            {
                procedureService.Register(ProcedureItemSelected.PROCEDURE_FORLIFT_TO_MACHINE, robot, orderItem);
            }
            else if (orderItem.typeReq == DeviceItem.TyeRequest.TYPEREQUEST_MACHINE_TO_BUFFERRETURN) // yes
            {
                procedureService.Register(ProcedureItemSelected.PROCEDURE_MACHINE_TO_BUFFER_RETURN, robot, orderItem);
            }
            else if(orderItem.typeReq == DeviceItem.TyeRequest.TYPEREQUEST_WMS_RETURN_PALLET_BUFFERRETURN_TO_BUFFER401) // yes
            {
                procedureService.Register(ProcedureItemSelected.PROCEDURE_BUFFER_TO_BUFFER, robot, orderItem);
            }
            // procedure;
        }

        public void AssignTaskAtReady(OrderItem order)
        {
            processAssignTaskReady = ProcessAssignTaskReady.PROC_READY_GET_ANROBOT_INREADYLIST;
            OrderItem orderItem_ready = null;
            RobotUnity robotatready = null;
            while (true)
            {
                switch (processAssignTaskReady)
                {
                    case ProcessAssignTaskReady.PROC_READY_IDLE:
                        break;
                    case ProcessAssignTaskReady.PROC_READY_GET_ANROBOT_INREADYLIST:
                        if (robotManageService.RobotUnityReadyList.Count > 0)
                        {
                            ResultRobotReady result = robotManageService.GetRobotUnityReadyItem0();
                            if (result != null)
                            {
                                robotatready = result.robot;
                                if (result.onReristryCharge)
                                {
                                    procedureService.Register(ProcedureItemSelected.PROCEDURE_ROBOT_TO_CHARGE, robotatready, null);
                                }
                                else
                                {
                                    if (order !=null)
                                    {
                                        if (!trafficService.HasOtherRobotUnityinArea("READY", robotatready))
                                        {
                                            if (order.typeReq == TyeRequest.TYPEREQUEST_FORLIFT_TO_BUFFER || order.typeReq == TyeRequest.TYPEREQUEST_FORLIFT_TO_MACHINE)
                                            {
                                                if (!trafficService.HasOtherRobotUnityinArea("READY-GATE", robotatready))
                                                {
                                                    MoveElementToEnd();
                                                }
                                                else
                                                {
                                                    processAssignTaskReady = ProcessAssignTaskReady.PROC_READY_CHECK_HAS_ANTASK;
                                                    break;
                                                }
                                            }
                                            else
                                            {
                                                processAssignTaskReady = ProcessAssignTaskReady.PROC_READY_CHECK_HAS_ANTASK;
                                                break;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        return;
                    case ProcessAssignTaskReady.PROC_READY_CHECK_HAS_ANTASK:
                        orderItem_ready = order;
                        orderItem_ready.onAssiged = true;
                        Console.WriteLine(processAssignTaskReady);
                        orderItem_ready.robot = robotatready.properties.Label;
                        robotatready.orderItem = orderItem_ready;
                        processAssignTaskReady = ProcessAssignTaskReady.PROC_READY_ASSIGN_ANTASK;
                        MoveElementToEnd();
                        break;
                    case ProcessAssignTaskReady.PROC_READY_ASSIGN_ANTASK:
                      robotatready.TurnOnSupervisorTraffic(true);
                      Console.WriteLine(processAssignTaskReady);
                      SelectProcedureItem(robotatready, orderItem_ready);
                      deviceItemsList[0].RemoveFirstOrder();
                      robotManageService.RemoveRobotUnityReadyList(robotatready);
                      orderItem_ready.status = StatusOrderResponseCode.DELIVERING;
                      processAssignTaskReady = ProcessAssignTaskReady.PROC_READY_CHECK_ROBOT_OUTSIDEREADY;
                      return;                       
                }
                Thread.Sleep(100);
            }
        }
        public bool FindRobotUnitySameOrderItem(String userName)
        {
            bool hasRobotSameOrderItem = false;
            foreach(RobotUnity robot in robotManageService.RobotUnityRegistedList.Values)
            {
                if(robot.orderItem!=null)
                {
                    if(robot.orderItem.userName.Equals(userName))
                    {
                        hasRobotSameOrderItem = true; ;
                        break;
                    }
                }
            }
            return hasRobotSameOrderItem;
        }
        public bool DetermineRobotWorkInGate()
        {
            if (!this.trafficService.HasRobotUnityinArea("GATE_CHECKOUT"))
            {
                Global_Object.onFlagRobotComingGateBusy = true;
                return false;
            }
            return true;           
        }
        public int DetermineAmoutOfDeviceToAssignAnTask()
        {
            try
            {
                int cntOrderWeight = 0;
                if (deviceItemsList.Count > 0)
                {
                    foreach (DeviceItem item in deviceItemsList)
                    {
                        if (item.PendingOrderList.Count > 0)
                        {
                            cntOrderWeight++;
                        }
                    }
                    if (cntOrderWeight > 0) // có nhiều device đang có task
                    {
                        return 1; // 
                    }
                }
            }
            catch
            {
                MessageBox.Show("Loi Cap Task");
            }
            return -1; // chỉ có 1 device đang có task
        }
        public void AssignTaskGoToReady(RobotUnity robot)
        {
            procedureService.Register(ProcedureItemSelected.PROCEDURE_ROBOT_TO_READY, robot, null);
        }

    }
}

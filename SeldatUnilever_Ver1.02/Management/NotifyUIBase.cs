﻿using SeldatUnilever_Ver1._02.Management.DeviceManagement;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SelDatUnilever_Ver1._00.Management
{
    public class NotifyUIBase : DeviceService, INotifyPropertyChanged
    {
        // Very minimal implementation of INotifyPropertyChanged matching msdn
        // Note that this is dependent on .net 4.5+ because of CallerMemberName
        public event PropertyChangedEventHandler PropertyChanged;
        public void RaisePropertyChanged([CallerMemberName] String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}

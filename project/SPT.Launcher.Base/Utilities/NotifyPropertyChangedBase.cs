using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SPT.Launcher.Utilities;

public abstract class NotifyPropertyChangedBase : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler PropertyChanged;

    protected void RaisePropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
	
    protected void SetProperty<T>(ref T field, T value, Action afterSetAction = null, [CallerMemberName] string propertyName = null)
    {
        if (!EqualityComparer<T>.Default.Equals(field, value))
        {
            field = value;
            RaisePropertyChanged(propertyName);
            afterSetAction?.Invoke();
        }
    }
	
    protected void SetNotNullableProperty<T>(ref T field, T value, Action afterSetAction = null, [CallerMemberName] string propertyName = null)
    {
        if (value is not null)
        {
            SetProperty(ref field, value, afterSetAction, propertyName );
        }
    }
}
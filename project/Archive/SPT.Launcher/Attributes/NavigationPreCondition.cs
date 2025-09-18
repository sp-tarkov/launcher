using SPT.Launcher.Models;
using SPT.Launcher.ViewModels;
using ReactiveUI;
using System;
using System.Threading.Tasks;

namespace SPT.Launcher.Attributes
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public abstract class NavigationPreCondition : Attribute
    {
        public abstract Task<NavigationPreConditionResult> TestPreCondition(IScreen Host);
    }
}

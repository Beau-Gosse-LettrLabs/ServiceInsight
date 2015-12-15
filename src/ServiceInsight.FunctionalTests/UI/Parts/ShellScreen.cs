﻿namespace ServiceInsight.FunctionalTests.UI.Parts
{
    using System;
    using System.Windows.Automation;
    using TestStack.White.Utility;

    public class ShellScreen : UIElement
    {
        public MainMenu MainMenu { get; set; }

        public LayoutManager LayoutManager { get; set; }

        public EndpointExplorer EndpointExplorer { get; set; }

        public StatusBar StatusBar { get; set; }

        public void WaitWhileBusy()
        {
            Retry.For(ShellIsBusy, isBusy => isBusy, TimeSpan.FromSeconds(10));
        }

        bool ShellIsBusy()
        {
            var currentPropertyValue = MainWindow.AutomationElement.GetCurrentPropertyValue(AutomationElement.HelpTextProperty);
            return currentPropertyValue != null && ((string)currentPropertyValue).Contains("Busy");
        }
    }
}
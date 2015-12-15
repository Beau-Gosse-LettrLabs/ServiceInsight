﻿namespace ServiceInsight.FunctionalTests.UI.Parts
{
    using Castle.Core.Logging;
    using TestStack.White;
    using TestStack.White.InputDevices;
    using TestStack.White.UIItems;
    using TestStack.White.UIItems.Finders;
    using TestStack.White.UIItems.WindowItems;

    public abstract class UIElement
    {
        public Window MainWindow { get; set; }
        public Application Application { get; set; }
        public ILogger Logger { get; set; }
        public IKeyboard Keyboard { get; set; }
        public IMouse Mouse { get; set; }

        protected T GetByAutomationId<T>(string id)
        {
            return (T)MainWindow.Get(SearchCriteria.ByAutomationId(id));
        }

        protected T GetByClassName<T>(string className) where T : IUIItem
        {
            return MainWindow.Get<T>(SearchCriteria.ByClassName(className));
        }
    }
}